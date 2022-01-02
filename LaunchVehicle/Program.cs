// See https://aka.ms/new-console-template for more information
using System;
using System.Configuration;
using static System.Configuration.ConfigurationManager;
using System.Collections.Specialized;
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
            //var settings = GetConfiguration(4);
            //foreach (var key in settings.AllKeys)
            //{
            //    Console.WriteLine(key + " = " + settings[key]);
            //}   

            // Create subscriber and publisher based on message broker type
            var messageBrokerType = MessageBrokerType.RabbitMq;
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
            int threads = 3 * 3;
            Dictionary<int, CancellationTokenSource> tokenSources = new Dictionary<int, CancellationTokenSource>(threads);
            Dictionary<int, CancellationToken> tokens = new Dictionary<int, CancellationToken>(threads);

            for (int i = 1; i <= threads; i++)
            {
                var tokenSource = new CancellationTokenSource();
                var token = tokenSource.Token;
                tokenSources.Add(i, tokenSource);
                tokens.Add(i, token);
            }

            var tasks = new ConcurrentBag<Task>();
            var task_num = 1;

            // Subscribe to commands initiated by client
            subscriber.Subscribe(async (subs, messageReceivedEventArgs) =>
            {
            var body = messageReceivedEventArgs.ReceivedMessage.Body;
            var commandMsg = SubscriberServiceBus.Deserialize<cmdMessage>(body);
            cmd_target = commandMsg.Target;
            cmd_type = commandMsg.Type;
            command = commandMsg.Cmd;
            Console.WriteLine("Target: " + cmd_target + " | command: " + command);

            if (lvDict.ContainsKey(cmd_target))            
                lvId = lvDict[cmd_target];                     

            switch (command)
            {
                case "Launch":
                    //Console.WriteLine("Launching vehicle: " + cmd_target);
                    var testLaunch = RunProcessAsync(@"..\..\..\..\Seeder\bin\Debug\net6.0\Seeder.exe", lvId);
                    // Alert DSN about launch
                    updateStatus(cmd_type, lvId, "Launch", messageBrokerPublisher);                    
                    break;

                case "StartTelemetry":
                    int tokenNum = lvId < 4 ? lvId : lvId + 3;                                    
                    Task t;
                    t = Task.Run(() => seedTlm(task_num, messageBrokerPublisher, cmd_type, lvId, tokens[tokenNum]), tokens[tokenNum]);
                    task_num = t.Id;
                    Console.WriteLine("Target {0} | Task {1} executing", cmd_target, t.Id);
                    tasks.Add(t);
                    await Task.Delay(1000);
                    break;

                case "StopTelemetry":
                    int tokenNumb = lvId < 4 ? lvId : lvId + 3;
                    Console.WriteLine("Task cancellation requested.");
                    tokenSources[tokenNumb].Cancel();
                    break;

                case "DeployPayload":
                    Console.WriteLine("Releasing Payload...");
                    break;

                case "StartData":
                    int token_num = lvId + 6;
                    t = Task.Run(() => seedData(task_num, messageBrokerPublisher, lvId, tokens[token_num]), tokens[token_num]);
                    task_num = t.Id;
                    Console.WriteLine("Target {0} | Task {1} executing", cmd_target, t.Id);
                    tasks.Add(t);
                    await Task.Delay(1000);
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
                await Task.Delay(5000); // Keep program running
            }
            while (true);
        }

        // Update launch status
        private static void updateStatus(string cmd_type, int lvId, string update, PublisherBase messageBrokerPublisher)
        {
            var messageId = Guid.NewGuid().ToString("N");
            var time = DateTime.UtcNow;
            var teleMessage = new StatusMessage(cmd_type, messageId, lvId, "Launched", time);
            var json = JsonSerializer.Serialize(teleMessage);
            Publish(messageId, json, time, messageBrokerPublisher);         
        }

        // Start executable with command line arguments
        private static Task<int> RunProcessAsync(string fileName, int lvId)
        {
            var config = GetConfiguration(lvId);
            var launchVehicle = config["Name"];
            var orbit = config["Orbit"];
            var tcs = new TaskCompletionSource<int>();

            var process = new Process
            {
                StartInfo = { FileName = fileName, Arguments = $"{launchVehicle} {orbit}" },
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

        private static NameValueCollection GetConfiguration(int id)
        {
            Dictionary<int, string> source = new Dictionary<int, string>(6);
            source.Add(1, "Bird-9");
            source.Add(2, "Bird-Heavy");
            source.Add(3, "Hawk-Heavy");
            source.Add(4, "GPM");
            source.Add(5, "TDRS-11");
            source.Add(6, "RO-245");

            string group = id < 4 ? "LaunchGroup" : "PayloadGroup";
            var settings = ConfigurationManager.GetSection(group + "/" + source[id] + "-Settings") as NameValueCollection;

            return settings;
        }

        private static void Publish(string messageId, string jsonMsg, DateTime time, PublisherBase messageBrokerPublisher)
        {
            var eventMessage = new EventMessage(messageId, jsonMsg, time);
            var eventMessageJson = JsonSerializer.Serialize(eventMessage); // Serialize message to Json
            var messageBytes = Encoding.UTF8.GetBytes(eventMessageJson);
            var message = new Message(messageBytes, messageId, "application/json"); // Adapter design pattern
            messageBrokerPublisher.Publish(message);
            //Console.WriteLine($"{messageId}: {eventMessageJson}\n");
        }

        private static void seedTlm(int taskNum, PublisherBase messageBrokerPublisher, string cmd_type, int lvId, CancellationToken ct)
        {
            int id = lvId < 4 ? lvId : lvId + 3;
            var settings = GetConfiguration(id);

            Random random = new Random();
            int count;
            var alerted = false;
            var maxIterations = 500;
            var altitude = 400.0;
            var longitude = -45.34;
            var latitude = -25.34;
            var temperature = 340.0;
            var orbitRadius = id < 4 ? Convert.ToDouble(settings["Orbit"]) : -99.0;
            var timeToOrbit = orbitRadius / 3600 + 10;
            var messageId = Guid.NewGuid().ToString("N");

            while (!ct.IsCancellationRequested)
            {
                for (int i = 0; i <= maxIterations; i++)
                {
                    var time = DateTime.UtcNow;
                    count = i + 1;
                    var teleMessage = new TeleMessage(cmd_type, messageId, lvId, count, altitude, longitude, latitude, temperature, timeToOrbit, time);
                    var json = JsonSerializer.Serialize(teleMessage);
                    Publish(messageId, json, time, messageBrokerPublisher);

                    Thread.Sleep(1000);
                    // Update with random values
                    messageId = Guid.NewGuid().ToString("N");
                    altitude += random.NextDouble();
                    longitude -= 10d + random.NextDouble(); ;
                    latitude -= 10d - random.NextDouble(); ;
                    temperature -= random.NextDouble();

                    if (lvId < 4)
                    {
                        if (timeToOrbit > 0d)
                            timeToOrbit -= 1d;
                        else if (timeToOrbit <= 0d)
                        {
                            if (!alerted) // Send alert to DSN  
                            {
                                Console.WriteLine("Sending reached orbit alert");
                                updateStatus("Reached Orbit Alert", lvId, "Reached Orbit Alert", messageBrokerPublisher);
                                alerted = true;
                            }
                        }
                    }

                    if (ct.IsCancellationRequested)
                    {
                        Console.WriteLine("Task {0} cancelled", taskNum);
                        break;
                    }
                }
            }
        }

        private static void seedData(int taskNum, PublisherBase messageBrokerPublisher, int lvId, CancellationToken ct)
        {
            Console.WriteLine("seed data");
            var settings = GetConfiguration(lvId + 3);
            var type = settings["Type"];
            Console.WriteLine("Type: " + type);

            switch (type)
            {
                case "Scientific":
                    SimulateScienceData(taskNum, messageBrokerPublisher, lvId, ct, settings);
                    break;

                case "Communication":
                    SimulateCommData(taskNum, messageBrokerPublisher, lvId, ct, settings);
                    break;

                case "Spy":
                    SimulateSpyData(taskNum, messageBrokerPublisher, lvId, ct, settings);
                    break;
            }
        }

        private static void SimulateScienceData(int taskNum, PublisherBase messageBrokerPublisher, int lvId, CancellationToken ct, NameValueCollection settings)
        {
            var maxIterations = 500;
            Random random = new Random();
            var interval = Int32.Parse(settings["Data-interval"]) * 1000;
            var rainfall = 34.0;
            var humidity = 56.0;
            var snow = 3.0;
            Console.WriteLine("interval: " + interval);

            while (!ct.IsCancellationRequested)
            {
                for (int i = 0; i <= maxIterations; i++)
                {
                    Console.WriteLine(i);
                    var messageId = Guid.NewGuid().ToString("N");
                    var time = DateTime.UtcNow;
                    var scidata = new SciData(messageId, rainfall, humidity, snow, time);
                    var json = JsonSerializer.Serialize(scidata);
                    Publish(messageId, json, time, messageBrokerPublisher);

                    Thread.Sleep(interval);
                    // Update with random values
                    messageId = Guid.NewGuid().ToString("N");
                    //altitude += random.NextDouble();
                    //longitude -= 10d + random.NextDouble(); ;
                    //latitude -= 10d - random.NextDouble(); ;
                    //temperature -= random.NextDouble();

                    if (ct.IsCancellationRequested)
                    {
                        Console.WriteLine("Task {0} cancelled", taskNum);
                        break;
                    }
                }
            }
        }
        private static void SimulateCommData(int taskNum, PublisherBase messageBrokerPublisher, int lvId, CancellationToken ct, NameValueCollection settings)
        {
            var maxIterations = 500;
            Random random = new Random();
            var interval = Int32.Parse(settings["Data-interval"]) * 1000;
            var uplink = 40.0;
            var downlink = 6700.0;
            var activeTransponders = 65.0;
            Console.WriteLine("interval: " + interval);

            while (!ct.IsCancellationRequested)
            {
                for (int i = 0; i <= maxIterations; i++)
                {
                    Console.WriteLine(i);
                    var messageId = Guid.NewGuid().ToString("N");
                    var time = DateTime.UtcNow;
                    var commdata = new CommData(messageId, uplink, downlink, activeTransponders, time);
                    var json = JsonSerializer.Serialize(commdata);
                    Publish(messageId, json, time, messageBrokerPublisher);

                    Thread.Sleep(interval);
                    // Update with random values
                    messageId = Guid.NewGuid().ToString("N");
                    //altitude += random.NextDouble();
                    //longitude -= 10d + random.NextDouble(); ;
                    //latitude -= 10d - random.NextDouble(); ;
                    //temperature -= random.NextDouble();

                    if (ct.IsCancellationRequested)
                    {
                        Console.WriteLine("Task {0} cancelled", taskNum);
                        break;
                    }
                }
            }
        }

        private static void SimulateSpyData(int taskNum, PublisherBase messageBrokerPublisher, int lvId, CancellationToken ct, NameValueCollection settings)
        {
            var maxIterations = 500;
            Random random = new Random();
            var interval = Int32.Parse(settings["Data-interval"]) * 1000;
            var imgUrl = "https://mars.nasa.gov/msl-raw-images/proj/msl/redops/ods/surface/sol/01000/opgs/edr/rcam/RLB_486265291EDR_F0481570RHAZ00323M_.JPG";
            Console.WriteLine("interval: " + interval);

            while (!ct.IsCancellationRequested)
            {
                for (int i = 0; i <= maxIterations; i++)
                {
                    Console.WriteLine(i);
                    var messageId = Guid.NewGuid().ToString("N");
                    var time = DateTime.UtcNow;
                    var spydata = new SpyData(messageId, imgUrl, time);
                    var json = JsonSerializer.Serialize(spydata);
                    Publish(messageId, json, time, messageBrokerPublisher);

                    Thread.Sleep(interval);
                    // Update with random values
                    messageId = Guid.NewGuid().ToString("N");
                    //altitude += random.NextDouble();
                    //longitude -= 10d + random.NextDouble(); ;
                    //latitude -= 10d - random.NextDouble(); ;
                    //temperature -= random.NextDouble();

                    if (ct.IsCancellationRequested)
                    {
                        Console.WriteLine("Task {0} cancelled", taskNum);
                        break;
                    }
                }
            }
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