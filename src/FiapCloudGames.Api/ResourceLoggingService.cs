
using System.Diagnostics;

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
            while (!stoppingToken.IsCancellationRequested)
            {
                process.Refresh();
                var memoryMb = process.WorkingSet64 / (1024 * 1024);
                var cpuTime = process.TotalProcessorTime.TotalSeconds;
                _logger.LogInformation("ResourceUsage | memory_mb={MemoryMB} | cpu_time_s={CpuTime}", memoryMb, cpuTime);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
