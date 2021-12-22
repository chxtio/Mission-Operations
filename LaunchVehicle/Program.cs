// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LaunchVehicle
{
    internal static class Program
    {
        static async Task Main()
        {
            //Console.WriteLine("Hello, World!");
            var messageBrokerType = MessageBrokerType.RabbitMq;
            // Create subscriber based on message broker type
            var subscriber = MessageBrokerSubscriberFactory.Create(messageBrokerType);

            var publishMessages = false;

            subscriber.Subscribe(async (subs, messageReceivedEventArgs) =>
            {
                var body = messageReceivedEventArgs.ReceivedMessage.Body;
                var commandMessage = SubscriberServiceBus.Deserialize<CommandMessage>(body);
                publishMessages = commandMessage.State;
                Console.WriteLine("Received command: " + commandMessage.State);
                await subs.Acknowledge(messageReceivedEventArgs.AcknowledgeToken);
            });

            var messageBrokerPublisher = MessageBrokerPublisherFactory.Create(messageBrokerType);

            Console.WriteLine("Waiting for Start Publishing Message");
            //Console.WriteLine("Press Enter to start Publishing Messages");
            //Console.ReadLine();

            //publishMessages = true;

            do
            {
                while (publishMessages)
                {
                    //Console.WriteLine("Publishing messages");
                    // Publish titles to publisher
                    foreach (var title in GetTitles(@"..\..\..\RandomTitles.txt"))
                    {
                        var messageId = Guid.NewGuid().ToString("N");
                        var eventMessage = new EventMessage(messageId, title, DateTime.UtcNow);
                        var eventMessageJson = JsonSerializer.Serialize(eventMessage); // Serialize message to Json
                        var messageBytes = Encoding.UTF8.GetBytes(eventMessageJson); 
                        var message = new Message(messageBytes, messageId, "application/json"); // Adapter design pattern
                        await messageBrokerPublisher.Publish(message);
                        await Task.Delay(1000);
                        if (!publishMessages)
                        {
                            break;
                        }
                    }
                }
            }
            while (true);
        }

        private static IEnumerable<string> GetTitles(string filename)
        {
            var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var streamReader = new StreamReader(fileStream);

            string line = null;
            // Stream lines, only maintaining one line in memory at a time
            while ((line = streamReader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }
}


