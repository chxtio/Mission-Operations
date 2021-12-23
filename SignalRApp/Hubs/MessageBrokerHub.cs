using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace SignalRApp
{
    public sealed class MessageBrokerHub : Hub
    {
        private readonly SignalProcessorManager _signalProcessorManager;

        public MessageBrokerHub(SignalProcessorManager signalProcessorManager)
        {
            _signalProcessorManager = signalProcessorManager;
        }

        // Publish message to (topic/exchange) to broker to control launch vehicle
        public async Task CommandReceived(string lightColor, bool state)
        {
            await _signalProcessorManager.PublishCommandMessage(new CommandMessage(
                id: Guid.NewGuid().ToString("N"),
                lightColor: lightColor,
                state: state,
                createdDateTime: DateTime.UtcNow));
        }

        public async Task cmdReceived(string target, string command)
        {
            await _signalProcessorManager.PublishCmdMessage(new cmdMessage(
                id: Guid.NewGuid().ToString("N"),
                cmd: command,
                target: target,
                createdDateTime: DateTime.UtcNow));
        }
    }
}