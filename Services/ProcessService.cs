using System.Diagnostics;

namespace NetSentry_Dashboard.Services
{
    public class ProcessService
    {
        private readonly string[] _systemWhitelist =
        {
            // Windows
            "Registry", "Memory Compression", "System", "smss", "csrss", "wininit",
            "services", "lsass", "svchost", "fontdrvhost", "winlogon", "dwm", "spoolsv",
            "sihost", "taskhostw", "RuntimeBroker", "explorer", "PresentationFontCache",
            
            // UWP
            "StartMenuExperienceHost", "SearchApp", "SearchHost",
            "PhoneExperienceHost", "ApplicationFrameHost",
            "TextInputHost", "ShellExperienceHost", "MoUsoCoreWorker",
            
            // Security
            "MsMpEng", "NisSrv", "SecurityHealthService",
            
            // Drivers
            "IGCCTray", "igfxEM", "NVIDIA Web Helper", "RadeonSoftware", "AMDRSServ",
            
            // --- VISUAL STUDIO ---
            "devenv",
            "ServiceHub.RoslynCodeAnalysisService",
            "ServiceHub.IntellicodeModelService",
            "ServiceHub.IdentityHost",
            "ServiceHub.Host.CLR",
            "copilot-language-server",
            "PerfWatson2",
            "DevHub",
            "VBCSCompiler",
            "MSBuild",
            "StandardCollector.Service",

            // --- WEB BROWSERS (Multiprocess Architecture) ---
            "msedge",
            "chrome",
            "firefox",
            "brave",
            "opera",
            "operagx"
        };

        public async Task AnalyzeProcesses(Action<string> logger)
        {
            await Task.Run(() =>
            {
                var processes = Process.GetProcesses();

                var heavy = processes.OrderByDescending(p => p.WorkingSet64).Take(5);

                foreach (var p in heavy)
                {
                    double memMb = p.WorkingSet64 / 1024.0 / 1024.0;
                    string level = memMb > 500 ? "[HEAVY]" : "[NORMAL]";
                    logger($"{level} {p.ProcessName.ToUpper()} : {memMb:0} MB");
                    Thread.Sleep(100);
                }

                var ghosts = processes
                    .Where(p => p.MainWindowHandle == IntPtr.Zero
                             && p.WorkingSet64 > 50 * 1024 * 1024)
                    .Where(p => !_systemWhitelist.Contains(p.ProcessName, StringComparer.OrdinalIgnoreCase))
                    .Take(5);

                if (ghosts.Any())
                {
                    logger("--- DETECTED GHOST PROCESSES (NON-SYSTEM) ---");
                    foreach (var g in ghosts)
                    {
                        double memMb = g.WorkingSet64 / 1024.0 / 1024.0;
                        logger($"[SUSPICIOUS] {g.ProcessName} : {memMb:0} MB (Hidden Window)");
                    }
                }
                else
                {
                    logger("NO SUSPICIOUS GHOST PROCESSES DETECTED.");
                }
            });
        }
    }
}