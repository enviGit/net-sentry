using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveCharts;
using LiveCharts.Wpf;
using NetSentry_Dashboard.Services;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace NetSentry_Dashboard.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly NetworkService _networkService;
        private readonly ProcessService _processService;
        private readonly SystemMonitorService _monitorService;
        private readonly DispatcherTimer _timer;

        [ObservableProperty] private string _systemStatus = "SYSTEM ONLINE";
        [ObservableProperty] private double _cpuUsage;
        [ObservableProperty] private Brush _statusColor;

        private readonly StringBuilder _logBuilder = new StringBuilder();

        [ObservableProperty]
        private string _logsText = "";

        // --- TOAST NOTIFICATION ---
        [ObservableProperty] private string _toastMessage = "";
        [ObservableProperty] private Visibility _toastVisibility = Visibility.Collapsed;

        public SeriesCollection Series { get; set; }
        private readonly ChartValues<double> _chartValues;
        private readonly LineSeries _lineSeries;

        public MainViewModel()
        {
            _networkService = new NetworkService();
            _processService = new ProcessService();
            _monitorService = new SystemMonitorService();

            StatusColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#00f2ff");
            _chartValues = new ChartValues<double>();
            for (int i = 0; i < 50; i++) _chartValues.Add(0);

            _lineSeries = new LineSeries
            {
                Values = _chartValues,
                PointGeometry = null,
                LineSmoothness = 0.5,
                StrokeThickness = 2,
                Stroke = StatusColor,
                Fill = Brushes.Transparent
            };
            Series = new SeriesCollection { _lineSeries };

            AddLog("SYSTEM INITIALIZED...");

            if (_monitorService.InitializeCpuCounter())
                AddLog("SENSORS ONLINE.");
            else
            {
                AddLog("[ERROR] CPU SENSORS CORRUPTED.");
                SystemStatus = "SENSOR ERROR";
            }

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _timer.Tick += (s, e) => UpdateMetrics();
            _timer.Start();
        }

        private void UpdateMetrics()
        {
            double rawValue = _monitorService.GetCpuUsage();

            CpuUsage = (CpuUsage * 0.95) + (rawValue * 0.05);

            UpdateThemeColor(CpuUsage);

            _chartValues.Add(Math.Round(CpuUsage, 1));
            if (_chartValues.Count > 100) _chartValues.RemoveAt(0);
        }

        private void UpdateThemeColor(double usage)
        {
            string hexColor;
            if (usage < 40) hexColor = "#00f2ff";
            else if (usage < 80) hexColor = "#ffea00";
            else hexColor = "#ff0055";

            if (StatusColor.ToString() != hexColor)
            {
                var newBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(hexColor);
                StatusColor = newBrush;
                _lineSeries.Stroke = newBrush;
            }
        }

        private void AddLog(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                string line = $"[{time}] {message}";

                _logBuilder.AppendLine(line);

                if (_logBuilder.Length > 10000)
                    _logBuilder.Remove(0, _logBuilder.Length - 8000);

                LogsText = _logBuilder.ToString();
            });
        }

        private async void ShowToast(string message)
        {
            ToastMessage = message;
            ToastVisibility = Visibility.Visible;
            await Task.Delay(2000);
            ToastVisibility = Visibility.Collapsed;
        }

        [RelayCommand]
        private void CopyLogs()
        {
            if (string.IsNullOrEmpty(LogsText)) return;
            Clipboard.SetText(LogsText);
            ShowToast("LOGS COPIED TO CLIPBOARD");
        }

        [RelayCommand]
        private void ExportLogs()
        {
            if (string.IsNullOrEmpty(LogsText)) return;
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "netsentry_logs.txt");
                File.WriteAllText(path, LogsText);
                ShowToast($"SAVED TO DESKTOP");
            }
            catch (Exception ex)
            {
                ShowToast("ERROR SAVING FILE");
                AddLog($"[ERROR] SAVE FAILED: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ScanNetwork()
        {
            if (_systemStatus.Contains("SCANNING")) return;
            _systemStatus = "SCANNING...";
            OnPropertyChanged(nameof(SystemStatus));

            AddLog("INITIATING NETWORK SCAN...");

            await _networkService.ScanPorts(AddLog);

            AddLog("SCAN COMPLETE.");
            _systemStatus = "SYSTEM ONLINE";
            OnPropertyChanged(nameof(SystemStatus));
        }

        [RelayCommand]
        private async Task AnalyzeProcesses()
        {
            AddLog("INITIATING PROCESS AUDIT...");
            await _processService.AnalyzeProcesses(AddLog);
            AddLog("AUDIT COMPLETE.");
        }

        [RelayCommand]
        private void RepairSystemCounters()
        {
            _monitorService.TryRepairCounters(AddLog);
        }
    }
}