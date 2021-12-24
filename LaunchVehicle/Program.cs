// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
//using Newtonsoft.Json;

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
                //Console.WriteLine("Received cmd: " + commandMessage.Cmd);
                publishMessages = commandMessage.State;
                Console.WriteLine("Received command: " + commandMessage.State);
                await subs.Acknowledge(messageReceivedEventArgs.AcknowledgeToken);
            });

            //subscriber.Subscribe(async (subs, messageReceivedEventArgs) =>
            //{

            //    var body = messageReceivedEventArgs.ReceivedMessage.Body;
            //    var commandMessage = SubscriberServiceBus.Deserialize<cmdMessage>(body);
            //    var target = commandMessage.Target;
            //    var cmd = commandMessage.Cmd;
            //    Console.WriteLine("Received command: (Target: " + target + ") " + cmd);
            //    switch (cmd)
            //    {
            //        case "StartTelemetry":
            //            publishMessages = true;
            //            Console.WriteLine("Sending telemetry data back to Deep Space Network");
            //            break;

            //        case "StopTelemetry":
            //            publishMessages = false;
            //            Console.WriteLine("Finished sending telemetry data");
            //            break;

            //    }

            //    await subs.Acknowledge(messageReceivedEventArgs.AcknowledgeToken);
            //});

            var messageBrokerPublisher = MessageBrokerPublisherFactory.Create(messageBrokerType);

            Console.WriteLine("Waiting to Start Publishing Messages");
            //Console.ReadLine();

            // Generate telemetry data
            //var messageId = Guid.NewGuid().ToString("N");
            var altitude = 400.0;
            var longitude = -45.34;
            var latitude = -25.34;
            var temperature = 340.0;
            var timeToOrbit = 45.0;

            publishMessages = true;

            do
            {
                while (publishMessages)
                {
                    // Seed simulated telemetry 
                    string json = JsonSerializer.Serialize(new { altitude = altitude, longitude = longitude, latitude = latitude, temperature = temperature, timeToOrbit = timeToOrbit, createdDateTime = DateTime.UtcNow.ToString() });
                    var messageId = Guid.NewGuid().ToString("N");
                    var eventMessage = new EventMessage(messageId, json, "test", DateTime.UtcNow);
                    var eventMessageJson = JsonSerializer.Serialize(eventMessage); // Serialize message to Json
                    var messageBytes = Encoding.UTF8.GetBytes(eventMessageJson);
                    var message = new Message(messageBytes, messageId, "application/json"); // Adapter design pattern
                    await messageBrokerPublisher.Publish(message);
                    Console.WriteLine($"{messageId}: {json}\n");
                    await Task.Delay(1000);


                    ////var teleMessage = new TeleMessage(messageId, altitude, longitude, latitude, temperature, timeToOrbit, DateTime.UtcNow);
                    //var eventMessageJson = JsonSerializer.Serialize(teleMessage); // Serialize message to Json
                    //var messageBytes = Encoding.UTF8.GetBytes(eventMessageJson);
                    //Console.WriteLine(messageBytes);
                    //var message = new Message(messageBytes, messageId, "application/json"); // Adapter design pattern
                    //await messageBrokerPublisher.Publish(message);
                    ////await messageBrokerPublisher.Publish(eventMessageJson);
                    //Console.WriteLine($"{messageId}: {eventMessageJson}\n");

                    //await Task.Delay(1000);

                    // Update with random values
                    messageId = Guid.NewGuid().ToString("N");
                    altitude += 10d;
                    longitude -= 10d;
                    latitude -= 10d;
                    temperature -= 2d;
                    timeToOrbit -= 1d;

                    if (!publishMessages)
                    {
                        break;
                    }
                }
            }
            while (true);



            //    do
            //    {
            //        while (publishMessages)
            //        {
            //            //Console.WriteLine("Publishing messages");
            //            // Publish titles to publisher
            //            //foreach (var title in GetTitles(@"..\..\..\RandomTitles.txt"))
            //            foreach (var title in GetTitles(@"..\..\..\TelemetryData.txt"))
            //            {
            //                //string json = JsonConvert.SerializeObject(new { PropertyA = "JSONtest" });
            //                string json = JsonSerializer.Serialize(new { PropertyA = "JSONtest", longitude = "longTest" });

            //                var messageId = Guid.NewGuid().ToString("N");
            //                var eventMessage = new EventMessage(messageId, json, "test", DateTime.UtcNow);
            //                var eventMessageJson = JsonSerializer.Serialize(eventMessage); // Serialize message to Json
            //                var messageBytes = Encoding.UTF8.GetBytes(eventMessageJson);
            //                var message = new Message(messageBytes, messageId, "application/json"); // Adapter design pattern


            //                //string jsonTest = JsonConvert.SerializeObject(eventMessage);
            //                //var messageBytes = Encoding.UTF8.GetBytes(jsonTest);
            //                //var message = new Message(messageBytes, messageId, "application/json");

            //                await messageBrokerPublisher.Publish(message);
            //                await Task.Delay(1000);
            //                if (!publishMessages)
            //                {
            //                    break;
            //                }
            //            }
            //        }
            //    }
            //    while (true);


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


