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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace LaunchVehicle
{
    internal static class Program
    {
        static readonly HttpClient client = new HttpClient();
        static async Task Main()
        {
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
                    var testLaunch = RunProcessAsync(@"..\..\..\..\Seeder\bin\Debug\net6.0\Seeder.exe", lvId);                    
                    updateStatus(cmd_type, lvId, "Launch", messageBrokerPublisher); // Alert DSN about launch                   
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

                case "StopData":
                    int token_numb = lvId + 6;
                    Console.WriteLine("Task cancellation requested.");
                    tokenSources[token_numb].Cancel();
                    break;

                case "Decommission":
                    //To do
                    break;

                case "Deorbit":
                    //To do
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
        static async Task<String> GetNasaPhotoAsync()
        {
            var imgurl = "";
            Random rand = new Random();
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                var uri = "https://api.nasa.gov/mars-photos/api/v1/rovers/curiosity/photos?sol=1000&api_key=DEMO_KEY";
                var responseBody = await client.GetStringAsync(uri);
                dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseBody);

                var randNum = rand.Next(100);
                imgurl = Convert.ToString(obj.photos[randNum]["img_src"]);
                Console.WriteLine(imgurl);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

            return imgurl;
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

        private static double UpdateMeasurement()
        {
            Random random = new Random();
            var dir = random.NextDouble() > .5;
            var change = dir ? random.NextDouble() : -random.NextDouble();

            return change;
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
                    altitude += 10d + random.NextDouble();
                    longitude -= 10d + random.NextDouble(); 
                    latitude -= 10d - random.NextDouble();
                    temperature += UpdateMeasurement();

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
            var settings = GetConfiguration(lvId + 3);
            var type = settings["Type"];

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
            var type = settings["Type"];
            var messageId = Guid.NewGuid().ToString("N");
            var interval = Int32.Parse(settings["Data-interval"]) * 1000;
            var rainfall = 34.0;
            var humidity = 56.0;
            var snow = 3.0;
            //Console.WriteLine("interval: " + interval);

            while (!ct.IsCancellationRequested)
            {
                for (int i = 0; i <= maxIterations; i++)
                {                   
                    var time = DateTime.UtcNow;
                    var scidata = new SciData(type, messageId, lvId, rainfall, humidity, snow, time);
                    var json = JsonSerializer.Serialize(scidata);
                    Publish(messageId, json, time, messageBrokerPublisher);

                    Thread.Sleep(interval);
                    // Update with random values
                    messageId = Guid.NewGuid().ToString("N");
                    rainfall += UpdateMeasurement();
                    humidity += UpdateMeasurement();
                    snow += UpdateMeasurement();

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
            var messageId = Guid.NewGuid().ToString("N");
            var type = settings["Type"];
            var interval = Int32.Parse(settings["Data-interval"]) * 1000;
            var uplink = 40.0;
            var downlink = 6700.0;
            var activeTransponders = 65.0;

            while (!ct.IsCancellationRequested)
            {
                for (int i = 0; i <= maxIterations; i++)
                {
                    var time = DateTime.UtcNow;
                    var commdata = new CommData(type, messageId, lvId, uplink, downlink, activeTransponders, time);
                    var json = JsonSerializer.Serialize(commdata);
                    Publish(messageId, json, time, messageBrokerPublisher);

                    Thread.Sleep(interval);
                    // Update with random values
                    messageId = Guid.NewGuid().ToString("N");
                    uplink += UpdateMeasurement();
                    downlink += UpdateMeasurement();
                    activeTransponders += UpdateMeasurement();

                    if (ct.IsCancellationRequested)
                    {
                        Console.WriteLine("Task {0} cancelled", taskNum);
                        break;
                    }
                }
            }
        }

        private static async void SimulateSpyData(int taskNum, PublisherBase messageBrokerPublisher, int lvId, CancellationToken ct, NameValueCollection settings)
        {
            var maxIterations = 500;
            Random random = new Random();
            var messageId = Guid.NewGuid().ToString("N");
            var type = settings["Type"];
            var interval = Int32.Parse(settings["Data-interval"]) * 1000;
            var imgUrl = await GetNasaPhotoAsync();//"https://mars.nasa.gov/msl-raw-images/proj/msl/redops/ods/surface/sol/01000/opgs/edr/rcam/RLB_486265291EDR_F0481570RHAZ00323M_.JPG";
            //Console.WriteLine("interval: " + interval);

            while (!ct.IsCancellationRequested)
            {
                for (int i = 0; i <= maxIterations; i++)
                {
                    var time = DateTime.UtcNow;
                    var spydata = new SpyData(type, messageId, lvId, imgUrl, time);
                    var json = JsonSerializer.Serialize(spydata);
                    Publish(messageId, json, time, messageBrokerPublisher);

                    Thread.Sleep(interval);
                    // Update with random values
                    messageId = Guid.NewGuid().ToString("N");
                    imgUrl = await GetNasaPhotoAsync();

                    if (ct.IsCancellationRequested)
                    {
                        Console.WriteLine("Task {0} cancelled", taskNum);
                        break;
                    }
                }
            }
        }        
    }
}