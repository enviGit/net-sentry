using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveCharts;
using LiveCharts.Wpf;
using NetSentry_Dashboard.Services;
using System.Collections.ObjectModel;
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

        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();
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
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                if (Logs.Count > 20) Logs.RemoveAt(0);
            });
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