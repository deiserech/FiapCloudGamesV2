
using System.Diagnostics;
using System.Management;

namespace FiapCloudGames.Api
{
    public class ResourceLoggingService : BackgroundService
    {
        private readonly ILogger<ResourceLoggingService> _logger;

        public ResourceLoggingService(ILogger<ResourceLoggingService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var process = Process.GetCurrentProcess();
            var lastCpuTime = process.TotalProcessorTime;
            var lastTime = DateTime.UtcNow;
            int processorCount = Environment.ProcessorCount;

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                process.Refresh();
                var memoryMb = process.WorkingSet64 / (1024.0 * 1024.0);
                var cpuTime = process.TotalProcessorTime.TotalSeconds;

                // Calcular CPU usage (%)
                var now = DateTime.UtcNow;
                var cpuUsedMs = (process.TotalProcessorTime - lastCpuTime).TotalMilliseconds;
                var elapsedMs = (now - lastTime).TotalMilliseconds;
                var cpuUsage = (elapsedMs > 0) ? (cpuUsedMs / (elapsedMs * processorCount)) * 100.0 : 0.0;

                double memUsagePct;
                double memLimitMb;
                try
                {
                    var memLimitStr = File.ReadAllText("/sys/fs/cgroup/memory/memory.limit_in_bytes");
                    if (long.TryParse(memLimitStr, out var memLimitBytes) && memLimitBytes > 0 && memLimitBytes < long.MaxValue)
                    {
                        memLimitMb = memLimitBytes / (1024.0 * 1024.0);
                        memUsagePct = (memLimitMb > 0) ? (memoryMb / memLimitMb) * 100.0 : (memoryMb / GetTotalMemoryMb()) * 100.0;
                    }
                    else
                    {
                        memLimitMb = GetTotalMemoryMb();
                        memUsagePct = (memLimitMb > 0) ? (memoryMb / memLimitMb) * 100.0 : 0.0;
                    }
                }
                catch
                {
                    memLimitMb = GetTotalMemoryMb();
                    memUsagePct = (memLimitMb > 0) ? (memoryMb / memLimitMb) * 100.0 : 0.0;
                }

                lastCpuTime = process.TotalProcessorTime;
                lastTime = now;
                var logMsg = $"ResourceUsage | cpu_time_s={cpuTime:F2} | cpu_usage_pct={cpuUsage:F2} | memory_mb={memoryMb:F2} | memory_limit_mb={memLimitMb:F2} | mem_usage_pct={memUsagePct:F2}";
                _logger.LogInformation(logMsg);
            }
        }

        private static double GetTotalMemoryMb()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    // Usa System.Management para obter memória física total
                    var searcher = new System.Management.ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                    foreach (var obj in searcher.Get())
                    {
                        if (obj["TotalPhysicalMemory"] != null && double.TryParse(obj["TotalPhysicalMemory"].ToString(), out var bytes))
                            return bytes / (1024.0 * 1024.0);
                    }
                }
                else if (OperatingSystem.IsLinux())
                {
                    var memInfo = File.ReadAllLines("/proc/meminfo");
                    var memTotalLine = memInfo.FirstOrDefault(l => l.StartsWith("MemTotal:"));
                    if (memTotalLine != null)
                    {
                        var parts = memTotalLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && double.TryParse(parts[1], out var kb))
                            return kb / 1024.0;
                    }
                }
            }
            catch { }

            return 0.0;
        }
    }
}
