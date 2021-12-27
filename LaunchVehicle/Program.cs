﻿// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
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

            Dictionary<string, int> lvDict = new Dictionary<string, int>(3);
            lvDict.Add("Bird-9", 1);
            lvDict.Add("Bird-Heavy", 2);
            lvDict.Add("Hawk-Heavy", 3);
            var publishMessages = false;
            var deployable = false;
            var target = "";
            var lvId = 2;

            subscriber.Subscribe(async (subs, messageReceivedEventArgs) =>
            {
                var body = messageReceivedEventArgs.ReceivedMessage.Body;
                var commandMessage = SubscriberServiceBus.Deserialize<cmdMessage>(body);
                target = commandMessage.Target;

                var cmd = commandMessage.Cmd;                
                Console.WriteLine("Received command: (Target: " + target + ") " + cmd);

                if (target != "")
                {
                    if (lvDict.ContainsKey(target))
                    {
                        lvId = lvDict[target];
                    }
                }

                switch (cmd)
                {
                    case "StartTelemetry":
                        publishMessages = true;
                        Console.WriteLine("Sending telemetry data back to Deep Space Network");
                        break;

                    case "StopTelemetry":
                        publishMessages = false;
                        Console.WriteLine("Finished sending telemetry data");
                        break;
                    case "DeployPayload":
                        if (deployable)
                        {
                            Console.WriteLine("Releasing Payload...");
                            //var p = new Process();
                            ////p.StartInfo.FileName = @"..\..\..\..\..\..\text.txt";
                            //p.StartInfo.FileName = @"C:\Program Files\Notepad++\notepad++.exe"; //"D:\OneDrive\Projects\csharp\Deep-Space-Network\LaunchVehicle\bin\Debug\net6.0\LaunchVehicle.exe";
                            //p.Start();
                        } else
                        {
                            Console.WriteLine("Unable to deploy payload until launch vehicle is in orbit.");
                        }
                        break;
                    default:
                        Console.WriteLine("Command not recognized: " + cmd);
                        break;
                }

                await subs.Acknowledge(messageReceivedEventArgs.AcknowledgeToken);
            });

            var messageBrokerPublisher = MessageBrokerPublisherFactory.Create(messageBrokerType);

            Console.WriteLine("Waiting to Start Publishing Messages");
            //Console.ReadLine();

            // Generate telemetry data
            
            var altitude = 400.0;
            var longitude = -45.34;
            var latitude = -25.34;
            var temperature = 340.0;
            var timeToOrbit = 5.0;

            // Uncomment for debugging
            //publishMessages = true;

            do
            {
                while (publishMessages)
                {
                    // Seed simulated telemetry 

                    var messageId = Guid.NewGuid().ToString("N");
                    var teleMessage = new TeleMessage(messageId, lvId, altitude, longitude, latitude, temperature, timeToOrbit, DateTime.UtcNow);
                    var json = JsonSerializer.Serialize(teleMessage);
                    var eventMessage = new EventMessage(messageId, json, "test", DateTime.UtcNow);
                    var eventMessageJson = JsonSerializer.Serialize(eventMessage); // Serialize message to Json
                    var messageBytes = Encoding.UTF8.GetBytes(eventMessageJson);
                    Console.WriteLine(messageBytes);
                    var message = new Message(messageBytes, messageId, "application/json"); // Adapter design pattern
                    await messageBrokerPublisher.Publish(message);
                    Console.WriteLine($"{messageId}: {eventMessageJson}\n");

                    await Task.Delay(1000);

                    // Update with random values
                    messageId = Guid.NewGuid().ToString("N");
                    altitude += 10d;
                    longitude -= 10d;
                    latitude -= 10d;
                    temperature -= 2d;
                    if (timeToOrbit > 0d)
                    {
                        timeToOrbit -= 1d;
                    } else if (timeToOrbit == 0d)
                    {
                        deployable = true;
                        Console.WriteLine("Ready to deploy payload for launch vehicle: " + target);
                    } 

                    if (!publishMessages)
                    {
                        break;
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


