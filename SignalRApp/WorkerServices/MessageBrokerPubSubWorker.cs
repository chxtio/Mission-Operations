using Microsoft.AspNetCore.SignalR;


namespace SignalRApp//.WorkerServices
{
    public sealed class MessageBrokerPubSubWorker : BackgroundService
    {
        private SignalProcessorManager _signalProcessorManager = new SignalProcessorManager();
        private IHubContext<MessageBrokerHub> _messageBrokerHubContext;

        // Constructor for background service injects IHubContext to access hub and provides access to singleton SignalProcessorManager instance
        // Worker service will now have a reference to SignalR Hub context to broadcast to clients
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
                // SignalR will send method name and message object to the client; Will publish events to clients w/ matching method name
                await _messageBrokerHubContext.Clients.All.SendAsync("onMessageReceived", eventMessage, stoppingToken);
            });
        }
    }
}
