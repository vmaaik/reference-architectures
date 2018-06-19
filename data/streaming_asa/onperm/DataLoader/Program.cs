namespace taxi
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;
    using Newtonsoft.Json;
    using System.Threading.Tasks.Dataflow;


    class Program
    {
        private static async Task ReadDataNew<T>(ICollection<string> pathList, Func<string, T> factory,
            EventHubClient client, int randomSeed, AsyncConsole console, CancellationToken cancellationToken)
            where T : Taxi
        {
            if (pathList == null)
            {
                throw new ArgumentNullException(nameof(pathList));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (console == null)
            {
                throw new ArgumentNullException(nameof(console));
            }

            string typeName = typeof(T).Name;
            Random random = new Random(randomSeed);
            BufferBlock<T> buffer = new BufferBlock<T>(new DataflowBlockOptions()
            {
                BoundedCapacity = 100
            });
            var consumer = new ActionBlock<T>(
                async (t) =>
                {
                    await client.SendAsync(new EventData(Encoding.UTF8.GetBytes(
                        JsonConvert.SerializeObject(t))), t.PartitionKey);
                },
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 10000,
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = 10,
                }
            );
            buffer.LinkTo(consumer, new DataflowLinkOptions()
            {
                PropagateCompletion = true
            });

            foreach (var path in pathList)
            {
                ZipArchive archive = new ZipArchive(
                    File.OpenRead(path),
                    ZipArchiveMode.Read);
                foreach (var entry in archive.Entries)
                {
                    using (var reader = new StreamReader(entry.Open()))
                    {
                        // Start consumer
                        var lines = reader.ReadLines()
                             .Skip(1);

                        foreach (var line in lines)
                        {
                            await buffer.SendAsync(factory(line));
                        }
                    }
                }
            }

            buffer.Complete();
            await Task.WhenAll(buffer.Completion, consumer.Completion);
        }

        private static async Task ReadData<T>(ICollection<string> pathList, Func<string, T> factory,
            EventHubClient client, int randomSeed, AsyncConsole console, CancellationToken cancellationToken)
        {

            if (pathList == null)
            {
                throw new ArgumentNullException(nameof(pathList));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (console == null)
            {
                throw new ArgumentNullException(nameof(console));
            }

            string typeName = "";
            Random random = new Random(randomSeed);
            foreach (var path in pathList)
            {
                typeName = typeof(T).Name;

                ZipArchive archive = new ZipArchive(
                    File.OpenRead(path),
                    ZipArchiveMode.Read);
                foreach (var entry in archive.Entries)
                {
                    var actionBlock = new ActionBlock<String>(
                            async (s) =>
                            {
                                var rideContents = s.Split(',');
                                var key = String.Format("{0}_{1}_{2}", rideContents[0], rideContents[1], rideContents[2]);
                                await client.SendAsync(new EventData(Encoding.UTF8.GetBytes(
                                   JsonConvert.SerializeObject(factory(s)))), key);
                            },
                            new ExecutionDataflowBlockOptions
                            {
                                BoundedCapacity = 10000,
                                CancellationToken = cancellationToken,
                                MaxDegreeOfParallelism = 10,
                            }
                        );
                    // int i = 0;
                    using (var reader = new StreamReader(entry.Open()))
                    {

                        var lines = reader.ReadLines()
                             .Skip(1)
                             .AsParallel().WithDegreeOfParallelism(10).WithMergeOptions(ParallelMergeOptions.NotBuffered)
                             .Select(s => s);

                        int messages = 0;
                        foreach (var line in lines)
                        {

                            await actionBlock.SendAsync(line).ConfigureAwait(false);
                            if (++messages % 10000 == 0)
                            {
                                await console.WriteLine($"Created {messages} records for {typeName}").ConfigureAwait(false);
                            }
                        }


                        actionBlock.Complete();
                        await actionBlock.Completion;
                        await console.WriteLine($"Created total  {messages} records for {typeName}").ConfigureAwait(false);
                    }
                }
            }
        }






        private static (string RideConnectionString,
                        string FareConnectionString,
                        ICollection<String> RideDataFiles,
                        ICollection<String> TripDataFiles,
                        int MillisecondsToRun) ParseArguments()
        {

            var rideConnectionString = Environment.GetEnvironmentVariable("RIDE_EVENT_HUB");
            var fareConnectionString = Environment.GetEnvironmentVariable("FARE_EVENT_HUB");
            var rideDataFilePath = Environment.GetEnvironmentVariable("RIDE_DATA_FILE_PATH");
            var numberOfMillisecondsToRun = (int.TryParse(Environment.GetEnvironmentVariable("SECONDS_TO_RUN"), out int temp) ? temp : 0) * 1000;


            // rideConnectionString = "Endpoint=sb://pnp-asa-eh.servicebus.windows.net/;SharedAccessKeyName=custom;SharedAccessKey=t0RI95jN/QuacPSzvqjLs2mfUg49w7701iae9Xg9EsE=;EntityPath=taxiride";

            rideConnectionString = "Endpoint=sb://pnpasarg.servicebus.windows.net/;SharedAccessKeyName=custom;SharedAccessKey=eQkKpdqu2PkHy8kGoFSOppbomucsacWJNvAwBzgcfI4=;EntityPath=taxiride";
            fareConnectionString = "Endpoint=sb://pnpasarg.servicebus.windows.net/;SharedAccessKeyName=custom;SharedAccessKey=1CBjSvMRnAjcLUGELWPI4pqNFzi+Ybu6g9jA0Y0mOUI=;EntityPath=taxifare";
            // fareConnectionString = "Endpoint=sb://pnp-asa-eh.servicebus.windows.net/;SharedAccessKeyName=Custom;SharedAccessKey=+98V1Kvd8Ob6PwH1dFIZ8SkJxt/jOhXpl5Q/EdOU7A4=;EntityPath=taxifare";
            rideDataFilePath = "D:\\reference-architectures\\data\\streaming_asa\\onperm\\DataFile";
            if (string.IsNullOrWhiteSpace(rideConnectionString))
            {
                throw new ArgumentException("rideConnectionString must be provided");
            }

            if (string.IsNullOrWhiteSpace(fareConnectionString))
            {
                throw new ArgumentException("fareConnectionString must be provided");
            }

            if (string.IsNullOrWhiteSpace(rideDataFilePath))
            {
                throw new ArgumentException("rideDataFilePath must be provided");
            }

            var rideDataFiles = Directory.EnumerateFiles(rideDataFilePath)
                                    .Where(p => Path.GetFileNameWithoutExtension(p).Contains("trip_data"))
                                    .OrderBy(p =>
                                    {
                                        var filename = Path.GetFileNameWithoutExtension(p);
                                        var indexString = filename.Substring(filename.LastIndexOf('_') + 1);
                                        var index = int.TryParse(indexString, out int i) ? i : throw new ArgumentException("tripdata file must be named in format trip_data_*.zip");
                                        return index;
                                    }).ToArray();


            var fareDataFiles = Directory.EnumerateFiles(rideDataFilePath)
                            .Where(p => Path.GetFileNameWithoutExtension(p).Contains("trip_fare"))
                            .OrderBy(p =>
                            {
                                var filename = Path.GetFileNameWithoutExtension(p);
                                var indexString = filename.Substring(filename.LastIndexOf('_') + 1);
                                var index = int.TryParse(indexString, out int i) ? i : throw new ArgumentException("tripfare file must be named in format trip_fare_*.zip");
                                return index;
                            }).ToArray();

            if (rideDataFiles.Length == 0)
            {
                throw new ArgumentException($"trip data files at {rideDataFilePath} does not exist");
            }

            if (fareDataFiles.Length == 0)
            {
                throw new ArgumentException($"fare data files at {rideDataFilePath} does not exist");
            }

            return (rideConnectionString, fareConnectionString, rideDataFiles, fareDataFiles, numberOfMillisecondsToRun);
        }

        private class AsyncConsole
        {
            private BlockingCollection<string> _blockingCollection = new BlockingCollection<string>();
            private CancellationToken _cancellationToken;
            private Task _writerTask;

            public AsyncConsole(CancellationToken cancellationToken = default(CancellationToken))
            {
                _cancellationToken = cancellationToken;
                _writerTask = Task.Factory.StartNew((state) =>
                {
                    var token = (CancellationToken)state;
                    string msg;
                    while (!token.IsCancellationRequested)
                    {
                        if (_blockingCollection.TryTake(out msg, 500))
                        {
                            Console.WriteLine(msg);
                        }
                    }

                    while (_blockingCollection.TryTake(out msg, 100))
                    {
                        Console.WriteLine(msg);
                    }
                }, _cancellationToken, TaskCreationOptions.LongRunning);
            }

            public Task WriteLine(string toWrite)
            {
                _blockingCollection.Add(toWrite);
                return Task.FromResult(0);
            }

            public Task WriterTask
            {
                get { return _writerTask; }
            }
        }
        public static async Task<int> Main(string[] args)
        {
            try
            {
                var arguments = ParseArguments();
                var rideClient = EventHubClient.CreateFromConnectionString(
                    arguments.RideConnectionString
                );
                var fareClient = EventHubClient.CreateFromConnectionString(
                    arguments.FareConnectionString
                );


                CancellationTokenSource cts = arguments.MillisecondsToRun == 0 ? new CancellationTokenSource() :
                    new CancellationTokenSource(arguments.MillisecondsToRun);
                Console.CancelKeyPress += (s, e) =>
                {
                    //Console.WriteLine("Cancelling data generation");
                    cts.Cancel();
                    e.Cancel = true;
                };


                AsyncConsole console = new AsyncConsole(cts.Token);

                var rideTask = ReadData<TaxiRide>(arguments.RideDataFiles,
                    TaxiRide.FromString, rideClient, 100, console, cts.Token);
                var fareTask = ReadData<TaxiFare>(arguments.TripDataFiles,
                    TaxiFare.FromString, fareClient, 200, console, cts.Token);
                await Task.WhenAll(rideTask, fareTask, console.WriterTask);

                Console.WriteLine("Data generation complete");
            }
            catch (ArgumentException ae)
            {
                Console.WriteLine(ae.Message);
                return 1;
            }

            return 0;
        }
    }
}


