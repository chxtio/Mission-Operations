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
            // Create subscriber and publisher based on message broker type
            var subscriber = MessageBrokerSubscriberFactory.Create(messageBrokerType);
            var messageBrokerPublisher = MessageBrokerPublisherFactory.Create(messageBrokerType);

            Dictionary<string, int> lvDict = new Dictionary<string, int>(3);
            lvDict.Add("Bird-9", 1);
            lvDict.Add("Bird-Heavy", 2);
            lvDict.Add("Hawk-Heavy", 3);
            var lvId = 0;

            var cmd_target = "";
            var cmd_type = "";
            var command = "";           

            // Create Cancellation tokens for cancelable tasks (for launch vehicle and payload threads)
            var tokenSource1 = new CancellationTokenSource();
            var tokenSource2 = new CancellationTokenSource();
            var tokenSource3 = new CancellationTokenSource();
            var tokenSourceP1 = new CancellationTokenSource();
            var tokenSourceP2 = new CancellationTokenSource();
            var tokenSourceP3 = new CancellationTokenSource();

            Dictionary<int, CancellationTokenSource> tokenSources = new Dictionary<int, CancellationTokenSource>(3);
            tokenSources.Add(1, tokenSource1);
            tokenSources.Add(2, tokenSource2);
            tokenSources.Add(3, tokenSource3);
            tokenSources.Add(4, tokenSourceP1);
            tokenSources.Add(5, tokenSourceP2);
            tokenSources.Add(6, tokenSourceP3);

            Dictionary<int, CancellationToken> tokens = new Dictionary<int, CancellationToken>(3);
            tokens.Add(1, tokenSource1.Token);
            tokens.Add(2, tokenSource2.Token);
            tokens.Add(3, tokenSource3.Token);
            tokens.Add(4, tokenSourceP1.Token);
            tokens.Add(5, tokenSourceP2.Token);
            tokens.Add(6, tokenSourceP3.Token);

            var tasks = new ConcurrentBag<Task>();
            var task_num = 1;

            subscriber.Subscribe(async (subs, messageReceivedEventArgs) =>
            {
            var body = messageReceivedEventArgs.ReceivedMessage.Body;
            var commandMsg = SubscriberServiceBus.Deserialize<cmdMessage>(body);
            cmd_target = commandMsg.Target;
            cmd_type = commandMsg.Type;
            command = commandMsg.Cmd;
            Console.WriteLine("Target: " + cmd_target + " | command: " + command);

            if (lvDict.ContainsKey(cmd_target))
            {
                lvId = lvDict[cmd_target];
            }            

            switch (command)
            {
                case "Launch":
                    Console.WriteLine("Launching vehicle: " + cmd_target);
                    //var process = new Process();
                    //p.StartInfo.FileName = @"..\..\..\..\..\..\text.txt";
                    //process.StartInfo.FileName = @"C:\Program Files\Notepad++\notepad++.exe"; //"D:\OneDrive\Projects\csharp\Deep-Space-Network\LaunchVehicle\bin\Debug\net6.0\LaunchVehicle.exe";
                    //process.Start();
                    //process.WaitForExit();
                    var testSeed = RunProcessAsync(@"D:\OneDrive\Projects\csharp\Deep-Space-Network\Seeder\bin\Debug\net6.0\Seeder.exe");
                    // Alert DSN about launch
                    var messageId = Guid.NewGuid().ToString("N");
                    var teleMessage = new LaunchMessage(cmd_type, messageId, lvId, "Launched", DateTime.UtcNow);
                    var json = JsonSerializer.Serialize(teleMessage);
                    var eventMessage = new EventMessage(messageId, json, DateTime.UtcNow);
                    var eventMessageJson = JsonSerializer.Serialize(eventMessage); // Serialize message to Json
                    var messageBytes = Encoding.UTF8.GetBytes(eventMessageJson);
                    var message = new Message(messageBytes, messageId, "application/json"); // Adapter design pattern
                    await messageBrokerPublisher.Publish(message);
                    Console.WriteLine(cmd_target + " launched successfully");
                    break;
                case "StartTelemetry":
                    int tokenNum;
                    if (cmd_type == "command")
                    {
                        tokenNum = lvId; // Launch Vehicle
                    } 
                    else
                    {
                        tokenNum = lvId + 3; // Payload
                    }
                    Task t;
                    t = Task.Run(() => seedData(task_num, messageBrokerPublisher, cmd_type, lvId, tokens[tokenNum]), tokens[tokenNum]);
                    task_num = t.Id;
                    Console.WriteLine("Target {0} | Task {1} executing", cmd_target, t.Id);
                    tasks.Add(t);
                    await Task.Delay(1000);
                    break;
                case "StopTelemetry":
                    int tokenNumb;
                    if (cmd_type == "command")
                    {
                        tokenNumb = lvId;
                    }
                    else
                    {
                        tokenNumb = lvId + 3;
                    }
                    Console.WriteLine("Task cancellation requested.");
                    tokenSources[tokenNumb].Cancel();
                    break;
                case "DeployPayload":
                    Console.WriteLine("Releasing Payload...");
                    break;
                default:
                    Console.WriteLine("Command not recognized: " + command);
                    break;
                }

                // Display status of all tasks.
                foreach (var task in tasks)
                    Console.WriteLine("Task {0} status is now {1}", task.Id, task.Status);

                await subs.Acknowledge(messageReceivedEventArgs.AcknowledgeToken);
            });

            Console.WriteLine("Waiting to Start Publishing Messages");

            do
            {
                // Keep program running
                await Task.Delay(5000);
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
            int count;
            var maxIterations = 500;
            var altitude = 400.0;
            var longitude = -45.34;
            var latitude = -25.34;
            var temperature = 340.0;
            var orbitRadius = 36000.0;
            var timeToOrbit = orbitRadius / 3600 + 10;
            //Console.WriteLine("Estimated time to reach orbit: " + timeToOrbit);

            while (!ct.IsCancellationRequested)
            {
                for (int i = 0; i <= maxIterations; i++)
                {
                    count = i + 1;
                    var messageId = Guid.NewGuid().ToString("N");
                    var teleMessage = new TeleMessage(cmd_type, messageId, lvId, count, altitude, longitude, latitude, temperature, timeToOrbit, DateTime.UtcNow);
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
                        // To do: Send alert to DSN
                    }

                    if (ct.IsCancellationRequested)
                    {
                        Console.WriteLine("Task {0} cancelled", taskNum);
                        break;
                        //ct.ThrowIfCancellationRequested();
                    }
                }
            }
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