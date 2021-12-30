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
                    //Console.WriteLine("lvId: " + lvId);
                }
                Console.WriteLine("Target " + cmd_target + " | command: " + command);

                if (command == "StartTelemetry")
                {
                    t = Task.Run(() => seedData(task_num, messageBrokerPublisher, cmd_type, lvId, token), token);
                    Console.WriteLine("Target {0} | Task {1} executing", cmd_target, t.Id);
                    tasks.Add(t);
                    task_num += 1;
                    await Task.Delay(1000);
                    //tokenSource.Cancel(); 
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



            ////Generate telemetry data
            //var altitude = 400.0;
            //var longitude = -45.34;
            //var latitude = -25.34;
            //var temperature = 340.0;
            //var timeToOrbit = 10.0;

            //// Uncomment for debugging
            ////publishMessages = true;

            //var cts = new CancellationTokenSource();
            //var ct = cts.Token;

            //var tokenSource = new CancellationTokenSource();
            //var token = tokenSource.Token;
            //Task t;
            //var tasks = new ConcurrentBag<Task>();

            //Console.WriteLine("Press any key to begin tasks...");
            //Console.ReadKey(true);
            //Console.WriteLine("To terminate the example, press 'c' to cancel and exit...");
            //Console.WriteLine();

            //Console.WriteLine("publishMessages = " + publishMessages);
            do
            {
                while (publishMessages)
                {
                    Console.WriteLine("Keep alive");

                    await Task.Delay(5000);
                }
            }
            while (true);

            //if (publishMessages)
            //{
            //    Console.WriteLine("test!");
            //    t = Task.Run(() => DoSomeWork(1, messageBrokerPublisher, token), token);
            //    Console.WriteLine("Task {0} executing", t.Id);
            //    tasks.Add(t);
            //}            

            //// Request cancellation from the UI thread.
            //char ch = Console.ReadKey().KeyChar;
            //if (ch == 'c' || ch == 'C' || command == "StopTelemetry")
            //{
            //    tokenSource.Cancel();
            //    Console.WriteLine("\nTask cancellation requested.");

            //    // Optional: Observe the change in the Status property on the task.
            //    // It is not necessary to wait on tasks that have canceled. However,
            //    // if you do wait, you must enclose the call in a try-catch block to
            //    // catch the TaskCanceledExceptions that are thrown. If you do
            //    // not wait, no exception is thrown if the token that was passed to the
            //    // Task.Run method is the same token that requested the cancellation.
            //}

            //// Display status of all tasks.
            //foreach (var task in tasks)
            //    Console.WriteLine("Task {0} status is now {1}", task.Id, task.Status);



            //do
            //{
            //    while (publishMessages)
            //    {
            //        // Simulate telemetry 

            //        var messageId = Guid.NewGuid().ToString("N");
            //        var teleMessage = new TeleMessage(cmd_type, messageId, lvId, altitude, longitude, latitude, temperature, timeToOrbit, DateTime.UtcNow);
            //        var json = JsonSerializer.Serialize(teleMessage);
            //        var eventMessage = new EventMessage(messageId, json, DateTime.UtcNow);
            //        var eventMessageJson = JsonSerializer.Serialize(eventMessage); // Serialize message to Json
            //        var messageBytes = Encoding.UTF8.GetBytes(eventMessageJson);
            //        var message = new Message(messageBytes, messageId, "application/json"); // Adapter design pattern
            //        await messageBrokerPublisher.Publish(message);
            //        Console.WriteLine($"{messageId}: {eventMessageJson}\n");

            //        await Task.Delay(1000);

            //        // Update with random values
            //        messageId = Guid.NewGuid().ToString("N");
            //        altitude += 10d;
            //        longitude -= 10d;
            //        latitude -= 10d;
            //        temperature -= 2d;
            //        if (timeToOrbit > 0d)
            //        {
            //            timeToOrbit -= 1d;
            //        }
            //        else if (timeToOrbit == 0d)
            //        {
            //            //deployable = true;
            //            Console.WriteLine("Ready to deploy payload for launch vehicle: " + cmd_target);
            //        }

            //        if (!publishMessages)
            //        {
            //            break;
            //        }
            //    }
            //}
            //while (true);


        }

        static void seedData(int taskNum, PublisherBase messageBrokerPublisher, string cmd_type, int lvId, CancellationToken ct)
        {
            // Was cancellation already requested?
            if (ct.IsCancellationRequested)
            {
                Console.WriteLine("Task {0} was cancelled before it got started.",
                                  taskNum);
                ct.ThrowIfCancellationRequested();
            }

            int maxIterations = 10;
            var altitude = 400.0;
            var longitude = -45.34;
            var latitude = -25.34;
            var temperature = 340.0;
            var timeToOrbit = 10.0;

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
                altitude += 10d;
                longitude -= 10d;
                latitude -= 10d;
                temperature -= 2d;
                if (timeToOrbit > 0d)
                {
                    timeToOrbit -= 1d;
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