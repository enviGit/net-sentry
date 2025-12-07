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

        private readonly ChartValues<double> _cpuValues;
        private readonly ChartValues<double> _ramValues;

        private readonly LineSeries _cpuLineSeries;
        private readonly LineSeries _ramLineSeries;

        private readonly StringBuilder _logBuilder = new StringBuilder();
        private int _tickCounter = 0;
        private int _viewMode = 0;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ScanNetworkCommand))]
        [NotifyCanExecuteChangedFor(nameof(AnalyzeProcessesCommand))]
        [NotifyCanExecuteChangedFor(nameof(RepairSystemCountersCommand))]
        private bool _isBusy;

        [ObservableProperty] private string _chartTitle = "CPU_MONITORING_REALTIME";
        [ObservableProperty] private string _mainValueLabel = "PROCESSOR LOAD";
        [ObservableProperty] private string _displayUsage = "0.0%";
        private string _currentColorName = "CYAN";

        [ObservableProperty] private string _systemStatus = "SYSTEM ONLINE";
        [ObservableProperty] private Brush _statusColor;

        [ObservableProperty] private double _cpuUsage;
        [ObservableProperty] private double _ramUsage;
        [ObservableProperty] private int _threadCount;
        [ObservableProperty] private string _uptime = "00:00:00";

        [ObservableProperty] private string _logsText = "";
        [ObservableProperty] private string _toastMessage = "";
        [ObservableProperty] private Visibility _toastVisibility = Visibility.Collapsed;

        public SeriesCollection Series { get; set; }

        public MainViewModel()
        {
            _networkService = new NetworkService();
            _processService = new ProcessService();
            _monitorService = new SystemMonitorService();

            StatusColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#00f2ff");

            _cpuValues = new ChartValues<double>();
            _ramValues = new ChartValues<double>();
            for (int i = 0; i < 60; i++)
            {
                _cpuValues.Add(0);
                _ramValues.Add(0);
            }

            _cpuLineSeries = new LineSeries
            {
                Title = "CPU",
                Values = _cpuValues,
                Stroke = StatusColor,
                PointGeometry = null,
                LineSmoothness = 1,
                Fill = Brushes.Transparent,
                StrokeThickness = 2
            };

            var ramBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#d946ef");
            _ramLineSeries = new LineSeries
            {
                Title = "RAM",
                Values = _ramValues,
                Stroke = ramBrush,
                PointGeometry = null,
                LineSmoothness = 0.5,
                Fill = Brushes.Transparent,
                StrokeThickness = 2
            };

            Series = new SeriesCollection { _cpuLineSeries };

            AddLog("SYSTEM INITIALIZED...");

            if (_monitorService.InitializeCounters())
                AddLog("SENSORS ONLINE.");
            else
                AddLog("[ERROR] SENSORS INITIALIZATION FAILED.");

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += (s, e) => UpdateMetrics();
            _timer.Start();
        }

        private void UpdateMetrics()
        {
            double rawCpu = _monitorService.GetCpuUsage();
            double rawRam = _monitorService.GetRamUsage();

            CpuUsage = (CpuUsage * 0.96) + (rawCpu * 0.04);
            RamUsage = (RamUsage * 0.96) + (rawRam * 0.04);

            _cpuValues.Add(Math.Round(CpuUsage, 1));
            _ramValues.Add(Math.Round(RamUsage, 1));

            if (_cpuValues.Count > 100) _cpuValues.RemoveAt(0);
            if (_ramValues.Count > 100) _ramValues.RemoveAt(0);

            UpdateThemeColor(CpuUsage);
            UpdateDisplayInfo();

            _tickCounter++;
            if (_tickCounter >= 10)
            {
                _tickCounter = 0;
                ThreadCount = _monitorService.GetTotalThreadCount();

                var ts = _monitorService.GetUptime();
                Uptime = $"{ts.Days}d {ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
            }
        }

        private void UpdateDisplayInfo()
        {
            switch (_viewMode)
            {
                case 0: // CPU Mode
                    DisplayUsage = $"{CpuUsage:0.0}%";
                    if (MainValueLabel != "PROCESSOR LOAD") MainValueLabel = "PROCESSOR LOAD";
                    break;

                case 1: // RAM Mode
                    DisplayUsage = $"{RamUsage:0.0}%";
                    if (MainValueLabel != "SYSTEM MEMORY LOAD") MainValueLabel = "SYSTEM MEMORY LOAD";
                    break;

                case 2: // Dual Mode
                    DisplayUsage = $"{CpuUsage:0.0}%";
                    MainValueLabel = $"CPU ({_currentColorName}) / RAM (PURPLE)";
                    break;
            }
        }

        private void UpdateThemeColor(double usage)
        {
            string hex;
            string name;

            if (usage < 40)
            {
                hex = "#00f2ff";
                name = "CYAN";
            }
            else if (usage < 80)
            {
                hex = "#ffea00";
                name = "YELLOW";
            }
            else
            {
                hex = "#ff0055";
                name = "RED";
            }

            _currentColorName = name;

            if (StatusColor.ToString() != hex)
            {
                var brush = (SolidColorBrush)new BrushConverter().ConvertFrom(hex);
                StatusColor = brush;
                _cpuLineSeries.Stroke = brush;
            }
        }

        // --- ACTIONS ---
        private bool CanInteract() => !IsBusy;

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private async Task ScanNetwork()
        {
            try
            {
                IsBusy = true;

                if (_systemStatus.Contains("SCANNING")) return;
                _systemStatus = "SCANNING..."; OnPropertyChanged(nameof(SystemStatus));

                AddLog("INITIATING NETWORK SCAN...");
                await _networkService.ScanPorts(AddLog);
                AddLog("SCAN COMPLETE.");
            }
            finally
            {
                _systemStatus = "SYSTEM ONLINE"; OnPropertyChanged(nameof(SystemStatus));
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private async Task AnalyzeProcesses()
        {
            try
            {
                IsBusy = true;
                AddLog("INITIATING PROCESS AUDIT...");
                await _processService.AnalyzeProcesses(AddLog);
                AddLog("AUDIT COMPLETE.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanInteract))]
        private void RepairSystemCounters()
        {
            IsBusy = true;
            _monitorService.TryRepairCounters(AddLog);
            IsBusy = false;
        }

        [RelayCommand]
        private void ToggleChartMode()
        {
            _viewMode++;
            if (_viewMode > 2) _viewMode = 0;

            Series.Clear();

            switch (_viewMode)
            {
                case 0:
                    Series.Add(_cpuLineSeries);
                    ChartTitle = "CPU_MONITORING_REALTIME";
                    AddLog("VIEW SWITCHED: CPU MONITOR");
                    break;

                case 1:
                    Series.Add(_ramLineSeries);
                    ChartTitle = "MEMORY_MONITORING_REALTIME";
                    AddLog("VIEW SWITCHED: RAM MONITOR");
                    break;

                case 2:
                    Series.Add(_cpuLineSeries);
                    Series.Add(_ramLineSeries);
                    ChartTitle = "SYSTEM_OVERVIEW_DUAL_LAYER";
                    AddLog("VIEW SWITCHED: DUAL METRICS");
                    break;
            }

            UpdateDisplayInfo();
        }

        // --- UTILITIES ---

        private void AddLog(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _logBuilder.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");

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
                ShowToast("SAVED TO DESKTOP");
            }
            catch (Exception ex)
            {
                ShowToast("ERROR SAVING FILE");
                AddLog($"[ERROR] SAVE FAILED: {ex.Message}");
            }
        }
    }
}