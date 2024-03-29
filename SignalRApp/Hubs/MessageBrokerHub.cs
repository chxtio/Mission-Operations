﻿using Microsoft.AspNetCore.SignalR;
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
        public async Task cmdReceived(string type, string target, string command)
        {
            await _signalProcessorManager.PublishCmdMessage(new cmdMessage(
                id: Guid.NewGuid().ToString("N"),
                type: type,
                cmd: command,
                target: target,
                createdDateTime: DateTime.UtcNow));
        }
    }
}