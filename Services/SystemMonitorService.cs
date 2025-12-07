using System.Diagnostics;
using System.ComponentModel;

namespace NetSentry_Dashboard.Services
{
    public class SystemMonitorService
    {
        private PerformanceCounter? _cpuCounter;

        public bool InitializeCpuCounter()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue();
                return true;
            }
            catch
            {
                _cpuCounter = null;
                return false;
            }
        }

        public double GetCpuUsage()
        {
            if (_cpuCounter == null) return 0;
            try
            {
                return _cpuCounter.NextValue();
            }
            catch
            {
                return 0;
            }
        }

        public void TryRepairCounters(Action<string> logger)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c lodctr /r",
                Verb = "runas",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            try
            {
                logger("[ADMIN] REQUESTING ELEVATED PERMISSIONS...");
                var p = Process.Start(processInfo);
                p?.WaitForExit();
                logger("[SUCCESS] COMMAND EXECUTED. RESTART APP.");
            }
            catch (Win32Exception)
            {
                logger("[ERROR] PERMISSION DENIED.");
            }
            catch (Exception ex)
            {
                logger($"[ERROR] FAILED: {ex.Message}");
            }
        }
    }
}