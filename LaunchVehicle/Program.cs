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
            //var settings = TestConfiguration();
            //foreach (var key in settings.AllKeys)
            //{
            //    Console.WriteLine(key + " = " + settings[key]);
            //}

            Dictionary<int, string> TLmSource = new Dictionary<int, string>(6);
            TLmSource.Add(1, "Bird-9");
            TLmSource.Add(2, "Bird-Heavy");
            TLmSource.Add(3, "Hawk-Heavy");
            TLmSource.Add(4, "GPM");
            TLmSource.Add(5, "TDRS-11");
            TLmSource.Add(6, "RO-245");


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
                    //int token_num = lvId + 6;
                    //t = Task.Run(() => seedData(task_num, messageBrokerPublisher, lvId, tokens[token_num]), tokens[token_num]);
                    //task_num = t.Id;
                    //Console.WriteLine("Target {0} | Task {1} executing", cmd_target, t.Id);
                    //tasks.Add(t);
                    //await Task.Delay(1000);
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

        private static void updateStatus(string cmd_type, int lvId, string update, PublisherBase messageBrokerPublisher)
        {
            var messageId = Guid.NewGuid().ToString("N");
            var teleMessage = new StatusMessage(cmd_type, messageId, lvId, "Launched", DateTime.UtcNow);
            var json = JsonSerializer.Serialize(teleMessage);
            var eventMessage = new EventMessage(messageId, json, DateTime.UtcNow);
            var eventMessageJson = JsonSerializer.Serialize(eventMessage); // Serialize message to Json
            var messageBytes = Encoding.UTF8.GetBytes(eventMessageJson);
            var message = new Message(messageBytes, messageId, "application/json"); // Adapter design pattern
            messageBrokerPublisher.Publish(message);            
        }

        private static void seedTlm(int taskNum, PublisherBase messageBrokerPublisher, string cmd_type, int lvId, CancellationToken ct)
        {
            int id = lvId < 4 ? lvId : lvId + 3;
            var settings = GetConfiguration(lvId);

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

        //private static void seedData(int taskNum, PublisherBase messageBrokerPublisher,int lvId, CancellationToken ct)
        //{
        //    if (ct.IsCancellationRequested)
        //    {
        //        Console.WriteLine("Task {0} was cancelled before it got started.", taskNum);
        //        ct.ThrowIfCancellationRequested();
        //    }

        //    Dictionary<int, string> TLmSource = new Dictionary<int, string>(6);
        //    TLmSource.Add(1, "Bird-9");
        //    TLmSource.Add(2, "Bird-Heavy");
        //    TLmSource.Add(3, "Hawk-Heavy");
        //    TLmSource.Add(4, "GPM");
        //    TLmSource.Add(5, "TDRS-11");
        //    TLmSource.Add(6, "RO-245");

        //    var maxIterations = 500;
        //    var settings = GetConfiguration(lvId, TLmSource);

        //    Random random = new Random();
        //    var interval = Convert.ToDouble(settings["data-interval"]) * 1000;
        //    var type = settings["type"];
        //    var messageId = Guid.NewGuid().ToString("N");

        //    while (!ct.IsCancellationRequested)
        //    {
        //        for (int i = 0; i <= maxIterations; i++)
        //        {
        //            switch (type)
        //            {
        //                case "Scientific":
        //                    SimulateScienceData();

        //                    var rainfall = 34.0;
        //                    var humidity = 56.0;
        //                    var snow = 3.0;
        //                    var time = DateTime.UtcNow;
        //                    var scidata = new SciData(messageId, rainfall, humidity, snow, time);
        //                    var json = JsonSerializer.Serialize(messageID, scidata, DateTime.UtcNow);
        //                    Publish(messageId, json, time, messageBrokerPublisher);

        //                    break;

        //                case "Communication":
        //                    var messageId = Guid.NewGuid().ToString("N");
        //                    var uplink = 40.0;
        //                    var downlink = 6700.0;
        //                    var activetransponders = 65.0;
        //                    var commdata = new CommData(messageId, uplink, downlink, activetransponders, DateTime.UtcNow);
        //                    json = JsonSerializer.Serialize(commdata);
        //                    eventMessage = new EventMessage(messageId, json, DateTime.UtcNow);
        //                    eventMessageJson = JsonSerializer.Serialize(eventMessage); // Serialize message to Json
        //                    messageBytes = Encoding.UTF8.GetBytes(eventMessageJson);
        //                    message = new Message(messageBytes, messageId, "application/json"); // Adapter design pattern
        //                    messageBrokerPublisher.Publish(message);
        //                    //Console.WriteLine($"{messageId}: {eventMessageJson}\n");
        //                    break;


        //                case "Spy":
        //                    var messageId = Guid.NewGuid().ToString("N");
        //                    var imgurl = "google.com";
        //                    var spydata = new SpyData(messageId, imgurl, DateTime.UtcNow);
        //                    json = JsonSerializer.Serialize(spydata);
        //                    eventMessage = new EventMessage(messageId, json, DateTime.UtcNow);
        //                    eventMessageJson = JsonSerializer.Serialize(eventMessage); // Serialize message to Json
        //                    messageBytes = Encoding.UTF8.GetBytes(eventMessageJson);
        //                    message = new Message(messageBytes, messageId, "application/json"); // Adapter design pattern
        //                    messageBrokerPublisher.Publish(message);
        //                    //Console.WriteLine($"{messageId}: {eventMessageJson}\n");
        //                    break;
        //            }




        //            var messageId = Guid.NewGuid().ToString("N");
        //            var teleMessage = new TeleMessage(cmd_type, messageId, lvId, count, altitude, longitude, latitude, temperature, timeToOrbit, DateTime.UtcNow);
        //            var json = JsonSerializer.Serialize(teleMessage);
        //            var eventMessage = new EventMessage(messageId, json, DateTime.UtcNow);
        //            var eventMessageJson = JsonSerializer.Serialize(eventMessage); // Serialize message to Json
        //            var messageBytes = Encoding.UTF8.GetBytes(eventMessageJson);
        //            var message = new Message(messageBytes, messageId, "application/json"); // Adapter design pattern
        //            messageBrokerPublisher.Publish(message);
        //            //Console.WriteLine($"{messageId}: {eventMessageJson}\n");

        //            Thread.Sleep(1000);
        //            // Update with random values
        //            messageId = Guid.NewGuid().ToString("N");
        //            altitude += random.NextDouble();
        //            longitude -= 10d + random.NextDouble(); ;
        //            latitude -= 10d - random.NextDouble(); ;
        //            temperature -= random.NextDouble();

        //            if (lvId < 4)
        //            {
        //                if (timeToOrbit > 0d)
        //                    timeToOrbit -= 1d;
        //                else if (timeToOrbit <= 0d)
        //                {
        //                    if (!alerted) // Send alert to DSN  
        //                    {
        //                        Console.WriteLine("Sending reached orbit alert");
        //                        updateStatus("Reached Orbit Alert", lvId, "Reached Orbit Alert", messageBrokerPublisher);
        //                        alerted = true;
        //                    }
        //                }
        //            }

        //            if (ct.IsCancellationRequested)
        //            {
        //                Console.WriteLine("Task {0} cancelled", taskNum);
        //                break;
        //            }
        //        }
        //    }
        //}

        //private static void SimulateScienceData(string messageId, string jsonMsg, DateTime time, PublisherBase messageBrokerPublisher)
        //{

        //}

        //private static void Publish(string messageId, string jsonMsg, DateTime time, PublisherBase messageBrokerPublisher)
        //{
        //    var eventMessage = new EventMessage(messageId, jsonMsg, time);
        //    var eventMessageJson = JsonSerializer.Serialize(eventMessage); // Serialize message to Json
        //    var messageBytes = Encoding.UTF8.GetBytes(eventMessageJson);
        //    var message = new Message(messageBytes, messageId, "application/json"); // Adapter design pattern
        //    messageBrokerPublisher.Publish(message);
        //    //Console.WriteLine($"{messageId}: {eventMessageJson}\n");
        //}

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

        private static NameValueCollection GetConfiguration(int lvId)
        {
            Dictionary<int, string> source = new Dictionary<int, string>(6);
            source.Add(1, "Bird-9");
            source.Add(2, "Bird-Heavy");
            source.Add(3, "Hawk-Heavy");
            source.Add(4, "GPM");
            source.Add(5, "TDRS-11");
            source.Add(6, "RO-245");

            string group = lvId < 4 ? "LaunchGroup" : "PayloadGroup";            
            var settings = ConfigurationManager.GetSection(group + "/" + source[lvId] + "-Settings") as NameValueCollection;

            return settings;
        }

        private static NameValueCollection TestConfiguration()
        {
            ////// Read a particular key from the config file 
            var Bird9Settings = ConfigurationManager.GetSection("LaunchGroup/Bird-9-Settings") as NameValueCollection;
            var BirdHeavySettings = ConfigurationManager.GetSection("LaunchGroup/Bird-Heavy-Settings") as NameValueCollection;
            var HawkHeavySettings = ConfigurationManager.GetSection("LaunchGroup/Hawk-Heavy-Settings") as NameValueCollection;
            var GPMSettings = ConfigurationManager.GetSection("PayloadGroup/GPM-Settings") as NameValueCollection;
            var TDRS11Settings = ConfigurationManager.GetSection("PayloadGroup/TDRS-11-Settings") as NameValueCollection;
            var RO245Settings = ConfigurationManager.GetSection("PayloadGroup/RO-245-Settings") as NameValueCollection;

            //Console.WriteLine(Bird9Settings["Orbit"]);
            return GPMSettings;

            //foreach (var key in Bird9Settings.AllKeys)
            //{
            //    Console.WriteLine(key + " = " + Bird9Settings[key]);
            //}
            //foreach (var key in BirdHeavySettings.AllKeys)
            //{
            //    Console.WriteLine(key + " = " + BirdHeavySettings[key]);
            //}
            //foreach (var key in HawkHeavySettings.AllKeys)
            //{
            //    Console.WriteLine(key + " = " + HawkHeavySettings[key]);
            //}
            //foreach (var key in GPMSettings.AllKeys)
            //{
            //    Console.WriteLine(key + " = " + GPMSettings[key]);
            //}
            //foreach (var key in TDRS11Settings.AllKeys)
            //{
            //    Console.WriteLine(key + " = " + TDRS11Settings[key]);
            //}
            //foreach (var key in RO245Settings.AllKeys)
            //{
            //    Console.WriteLine(key + " = " + RO245Settings[key]);
            //}
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