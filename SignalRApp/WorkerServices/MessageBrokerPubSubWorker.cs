using Microsoft.AspNetCore.SignalR;


namespace SignalRApp//.WorkerServices
{
    public sealed class MessageBrokerPubSubWorker : BackgroundService
    {
        private SignalProcessorManager _signalProcessorManager = new SignalProcessorManager();
        private IHubContext<MessageBrokerHub> _messageBrokerHubContext;

        // Inject IHubContext- Worker service has reference to hub context to send messages to clients via SignalR
        public MessageBrokerPubSubWorker(IHubContext<MessageBrokerHub> messageBrokerHubContext, SignalProcessorManager signalProcessorManager)
        {
            _messageBrokerHubContext = messageBrokerHubContext;
            _signalProcessorManager = signalProcessorManager;
        }

        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // worker service starts and calls execute async method to loop and publish messages every sec
            ////throw new NotImplementedException();
            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    await Task.Delay(1000);
            //    var eventMessage = new EventMessage($"ID_{ Guid.NewGuid():N}", $"Title_{ Guid.NewGuid():N}", DateTime.UtcNow);
            //    // Get access to clients connected to hub and send to all
            //    await _messageBrokerHubContext.Clients.All.SendAsync("onMessagedReceived", eventMessage, stoppingToken);
            //}

            // Create instance of manager and start listening
            var signalProccessorManager = new SignalProcessorManager();
            await signalProccessorManager.StartListening(async eventMessage =>
            {
                // Publish events to browser
                await _messageBrokerHubContext.Clients.All.SendAsync("onMessageReceived", eventMessage, stoppingToken);
            });
        }
    }
}
