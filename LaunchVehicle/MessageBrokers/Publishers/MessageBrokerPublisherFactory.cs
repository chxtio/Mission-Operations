namespace LaunchVehicle
{
    internal static class MessageBrokerPublisherFactory
    {
        const string brokerConnectionStringRabbitMq = "amqp://test.vhost:test@localhost/test.vhost";
        const string brokerConnectionStringServiceBus = "<Your ServiceBus Connection string>";
        const string titleTopic = "tlm.events.topic";//"title.events.topic";

        public static PublisherBase Create(MessageBrokerType messageBrokerType)
        {
            switch (messageBrokerType)
            {
                case MessageBrokerType.RabbitMq:
                    return new PublisherRabbitMq(brokerConnectionStringRabbitMq, titleTopic);
                        
                case MessageBrokerType.ServiceBus:
                    return new PublisherServiceBus(brokerConnectionStringServiceBus, titleTopic);
            }

            throw new MessageBrokerTypeNotSupportedException($"The MessageBrokerType: {messageBrokerType}, is not supported yet");
        }
    }

    internal enum MessageBrokerType { RabbitMq, ServiceBus }
}
