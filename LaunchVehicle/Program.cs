// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LaunchVehicle
{
    internal static class Program
    {
        static async Task Main()
        {
            var messageBrokerType = MessageBrokerType.RabbitMq;
            // Create subscriber based on message broker type
            var subscriber = MessageBrokerSubscriberFactory.Create(messageBrokerType);

            Dictionary<string, int> lvDict = new Dictionary<string, int>(3);
            lvDict.Add("Bird-9", 1);
            lvDict.Add("Bird-Heavy", 2);
            lvDict.Add("Hawk-Heavy", 3);
            var lvId = 0;

            var publishMessages = false;
            var cmd_target = "";
            var cmd_type = "";
            var command = "";

            var messageBrokerPublisher = MessageBrokerPublisherFactory.Create(messageBrokerType);
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            Task t;
            var tasks = new ConcurrentBag<Task>();
            var task_num = 1;

            subscriber.Subscribe(async (subs, messageReceivedEventArgs) =>
            {
                var body = messageReceivedEventArgs.ReceivedMessage.Body;
                var commandMsg = SubscriberServiceBus.Deserialize<cmdMessage>(body);
                cmd_target = commandMsg.Target;
                cmd_type = commandMsg.Type;
                command = commandMsg.Cmd;
                
                if (lvDict.ContainsKey(cmd_target))
                {
                    lvId = lvDict[cmd_target];
                }
                Console.WriteLine("Target: " + cmd_target + " | command: " + command);

                switch (command)
                {
                    case "Launch":
                        Console.WriteLine("Launching vehicle: " + cmd_target);
                        //var process = new Process();
                        //p.StartInfo.FileName = @"..\..\..\..\..\..\text.txt";
                        //process.StartInfo.FileName = @"C:\Program Files\Notepad++\notepad++.exe"; //"D:\OneDrive\Projects\csharp\Deep-Space-Network\LaunchVehicle\bin\Debug\net6.0\LaunchVehicle.exe";
                        //process.Start();
                        //process.WaitForExit();

                        var notepad = await RunProcessAsync(@"C:\Program Files\Notepad++\notepad++.exe");

                        Console.WriteLine("Started process");
                        break;
                    case "StartTelemetry":
                        t = Task.Run(() => seedData(task_num, messageBrokerPublisher, cmd_type, lvId, token), token);
                        Console.WriteLine("Target {0} | Task {1} executing", cmd_target, t.Id);
                        tasks.Add(t);
                        task_num += 1;
                        await Task.Delay(1000);
                        //tokenSource.Cancel();
                        break;
                    default:
                        Console.WriteLine("Command not recognized: " + command);
                        break;
                }

                //// Request cancellation from the UI thread.
                //char ch = Console.ReadKey().KeyChar;
                //if (ch == 'c' || command == "StopTelemetry")
                //{
                //    Console.WriteLine("Stop task!");
                //    tokenSource.Cancel();
                //    Console.WriteLine("\nTask cancellation requested.");

                //try
                //{
                //    await Task.WhenAll(tasks.ToArray());
                //}
                //catch (OperationCanceledException)
                //{
                //    Console.WriteLine($"\n{nameof(OperationCanceledException)} thrown\n");
                //}
                //finally
                //{
                //    tokenSource.Dispose();
                //}

                // Display status of all tasks.
                foreach (var task in tasks)
                    Console.WriteLine("Task {0} status is now {1}", task.Id, task.Status);

                await subs.Acknowledge(messageReceivedEventArgs.AcknowledgeToken);
            });

            //var messageBrokerPublisher = MessageBrokerPublisherFactory.Create(messageBrokerType);
            Console.WriteLine("Waiting to Start Publishing Messages");

            do
            {
                while (publishMessages)
                {
                    Console.WriteLine("Keep alive");

                    await Task.Delay(5000);
                }
            }
            while (true);
        }

        static void seedData(int taskNum, PublisherBase messageBrokerPublisher, string cmd_type, int lvId, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                Console.WriteLine("Task {0} was cancelled before it got started.",
                                  taskNum);
                ct.ThrowIfCancellationRequested();
            }

            Random random = new Random();
            int maxIterations = 500;
            var altitude = 400.0;
            var longitude = -45.34;
            var latitude = -25.34;
            var temperature = 340.0;
            var orbitRadius = 36000.0;
            var timeToOrbit = orbitRadius / 3600 + 10;
            Console.WriteLine("Estimated time to reach orbit: " + timeToOrbit);

            for (int i = 0; i <= maxIterations; i++)
            {
                var messageId = Guid.NewGuid().ToString("N");
                var teleMessage = new TeleMessage(cmd_type, messageId, lvId, altitude, longitude, latitude, temperature, timeToOrbit, DateTime.UtcNow);
                var json = JsonSerializer.Serialize(teleMessage);
                var eventMessage = new EventMessage(messageId, json, DateTime.UtcNow);
                var eventMessageJson = JsonSerializer.Serialize(eventMessage); // Serialize message to Json
                var messageBytes = Encoding.UTF8.GetBytes(eventMessageJson);
                var message = new Message(messageBytes, messageId, "application/json"); // Adapter design pattern
                messageBrokerPublisher.Publish(message);
                //Console.WriteLine($"{messageId}: {eventMessageJson}\n");

                Thread.Sleep(1000);

                // Update with random values
                messageId = Guid.NewGuid().ToString("N");
                altitude += random.NextDouble();
                longitude -= 10d + random.NextDouble(); ;
                latitude -= 10d - random.NextDouble(); ;
                temperature -= random.NextDouble();
                if (timeToOrbit > 0d)
                {
                    timeToOrbit -= 1d;
                    // Send alert to DSN
                }

                if (ct.IsCancellationRequested)
                {
                    Console.WriteLine("Task {0} cancelled", taskNum);
                    ct.ThrowIfCancellationRequested();
                }
            }
        }


        private static bool processCommand(string target, string type, string cmd, bool publish)
        {
            Console.WriteLine("Received command (Type: " + type + "; Target: " + target + "): " + cmd);
            var toPublish = false;

            switch (cmd)
            {
                case "Launch":
                    Console.WriteLine("Launching vehicle: " + target);
                    //var p = new Process();
                    ////p.StartInfo.FileName = @"..\..\..\..\..\..\text.txt";
                    //p.StartInfo.FileName = @"C:\Program Files\Notepad++\notepad++.exe"; //"D:\OneDrive\Projects\csharp\Deep-Space-Network\LaunchVehicle\bin\Debug\net6.0\LaunchVehicle.exe";
                    //p.Start();
                    toPublish = publish;
                    break;
                case "StartTelemetry":
                    Console.WriteLine("Sending telemetry data back to Deep Space Network");
                    toPublish = true;
                    break;

                case "StopTelemetry":
                    Console.WriteLine("Finished sending telemetry data");
                    toPublish = false;
                    break;
                case "DeployPayload":
                    Console.WriteLine("Releasing Payload...");
                    //var p = new Process();
                    ////p.StartInfo.FileName = @"..\..\..\..\..\..\text.txt";
                    //p.StartInfo.FileName = @"C:\Program Files\Notepad++\notepad++.exe"; //"D:\OneDrive\Projects\csharp\Deep-Space-Network\LaunchVehicle\bin\Debug\net6.0\LaunchVehicle.exe";
                    //p.Start();
                    toPublish = publish;
                    break;
                default:
                    Console.WriteLine("Command not recognized: " + cmd);
                    toPublish = publish;
                    break;
            }

            return toPublish;

        }

        private static Task<int> RunProcessAsync(string fileName)
        {
            var tcs = new TaskCompletionSource<int>();

            var process = new Process
            {
                StartInfo = { FileName = fileName },
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();

            return tcs.Task;
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