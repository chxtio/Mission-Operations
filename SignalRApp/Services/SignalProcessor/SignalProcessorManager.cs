﻿using System;
using System.Threading.Tasks;

namespace SignalRApp
{
    // Represents the system
    public sealed class SignalProcessorManager : IDisposable
    {
        private bool _disposed;
        // Message broker constants
        private const string EventMessageTopicName = "tlm.events.topic";//"title.events.topic";
        private const string EventMessageQueueName = "tlm.events.queue";//"title.events.queue";
        private const string CommandTopicName = "command.events.topic";

        // Configuration settings
        private readonly ConfigurationProvider _configurationProvider;

        private SubscriberBase _subscriberEventMessage;

        private MessageBrokerSettings _messageBrokerSettings;
        private MessageBrokerSettings MessageBrokerSettings { get { return _messageBrokerSettings ??= _configurationProvider.GetMessageBrokerSettings(); } }

        private PublisherCommandMessageBase _publisherCommandMessage;
        private PublisherCommandMessageBase PublisherCommandMessage { get { return _publisherCommandMessage ??= MakePublisherCommandMessage(MessageBrokerSettings, CommandTopicName); } }


        public SignalProcessorManager()
        {
            _configurationProvider = new ConfigurationProvider();
        }

        // Start system- listen for and pass messages to callback
        public async Task StartListening(Func<EventMessage, Task> onMessageCallback)
        {
            _subscriberEventMessage = MakeSubscriberEventMessage(MessageBrokerSettings.MessageBrokerType);
            await _subscriberEventMessage.Initialize(MessageBrokerSettings.MessageBrokerConnectionString, EventMessageTopicName, EventMessageQueueName + "." + Environment.MachineName);
            _subscriberEventMessage.Subscribe(OnEventMessageReceived, onMessageCallback);
        }

        private async Task OnEventMessageReceived(SubscriberBase subscriberEventMessage, MessageReceivedEventArgs messageReceivedEventArgs, Func<EventMessage, Task> onMessageCallback)
        {
            var message = messageReceivedEventArgs.Message;
            var eventMessage = MessageProcessor.DeserializeMessage(message);
            await onMessageCallback(eventMessage);
            await subscriberEventMessage.Acknowledge(messageReceivedEventArgs.AcknowledgeToken);
        }

        public async Task PublishCommandMessage(CommandMessage commandMessage)
        {
            await PublisherCommandMessage.Publish(commandMessage);
        }

        public async Task PublishCmdMessage(cmdMessage commandMessage)
        {
            await PublisherCommandMessage.Publish(commandMessage);
        }

        private static SubscriberBase MakeSubscriberEventMessage(MessageBrokerType messageBrokerType)
        {
            return messageBrokerType switch
            {
                MessageBrokerType.RabbitMq => new SubscriberRabbitMq(),
                //var mbt when
                //    mbt == MessageBrokerType.ServiceBus ||
                //    mbt == MessageBrokerType.Console => new SubscriberServiceBus(),
                //_ => throw new ConfigurationSettingInvalidException($"The Message Broker Type of: {messageBrokerType} is not a valid or supported Message Broker Type")
            };
        }

        private static PublisherCommandMessageBase MakePublisherCommandMessage(MessageBrokerSettings messageBrokerSettings, string orchestrationTopicName)
        {
            return messageBrokerSettings.MessageBrokerType switch
            {
                MessageBrokerType.RabbitMq => new PublisherCommandMessageRabbitMq(messageBrokerSettings.MessageBrokerConnectionString, orchestrationTopicName),
                //var mbt when
                //    mbt == MessageBrokerType.ServiceBus ||
                //    mbt == MessageBrokerType.Console => new PublisherCommandMessageServiceBus(messageBrokerSettings.MessageBrokerConnectionString, orchestrationTopicName),
                //_ => throw new ConfigurationSettingInvalidException($"The Message Broker Type of: {messageBrokerSettings.MessageBrokerType} is not a valid or supported Message Broker Type")
            };
        }

        public Task StopListening()
        {
            _subscriberEventMessage?.Dispose();
            _subscriberEventMessage = null;
            return Task.CompletedTask;
        }

        private void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _subscriberEventMessage?.Dispose();
                _disposed = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}