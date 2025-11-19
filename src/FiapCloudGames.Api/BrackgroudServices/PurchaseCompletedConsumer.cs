using Azure.Messaging.ServiceBus;
using FiapCloudGames.Users.Application.Interfaces.Services;
using FiapCloudGames.Users.Domain.Events;
using FiapCloudGames.Users.Infrastructure.ServiceBus;
using Newtonsoft.Json;

namespace FiapCloudGames.Users.Api.BackgroundServices
{
    public class PurchaseCompletedConsumer(ServiceBusClientWrapper sb, IServiceProvider provider, IConfiguration config) : BackgroundService
    {
        private ServiceBusProcessor? _processor;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queue = config["PURCHASE_COMPLETED_QUEUE"] ?? "payments/purchases-completed";
            _processor = sb.CreateProcessor(queue);
            _processor.ProcessMessageAsync += ProcessMessageAsync;
            _processor.ProcessErrorAsync += ErrorHandler;
            await _processor.StartProcessingAsync(stoppingToken);
        }

        private Task ErrorHandler(ProcessErrorEventArgs arg)
        {
            Console.WriteLine($"PurchaseCompletedConsumer error: {arg.Exception}");
            return Task.CompletedTask;
        }

        private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            var body = args.Message.Body.ToString();
            var message = JsonConvert.DeserializeObject<PurchaseCompletedEvent>(body);
            if (message is null)
            {
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            using var scope = provider.CreateScope();
            var purchaseService = scope.ServiceProvider.GetRequiredService<IPurchaseService>();
            await purchaseService.ProcessAsync(message, args.CancellationToken);

            await args.CompleteMessageAsync(args.Message);
        }
    }
}
