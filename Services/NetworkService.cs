using System.Net.Sockets;

namespace NetSentry_Dashboard.Services
{
    public class NetworkService
    {
        public async Task ScanPorts(Action<string> logger)
        {
            int[] ports = { 21, 22, 80, 443, 3306, 8080, 8000 };

            await Task.Run(async () =>
            {
                foreach (var port in ports)
                {
                    bool isOpen = await CheckPort(port);
                    if (isOpen)
                        logger($"[!] PORT {port} OPEN (RISK)");
                    else
                        logger($"PORT {port} CLOSED");

                    await Task.Delay(100);
                }
            });
        }

        private async Task<bool> CheckPort(int port)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    var task = client.ConnectAsync("127.0.0.1", port);
                    if (await Task.WhenAny(task, Task.Delay(200)) == task)
                    {
                        return client.Connected;
                    }
                    return false;
                }
                catch { return false; }
            }
        }
    }
}