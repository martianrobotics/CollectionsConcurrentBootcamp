using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CollectionsConcurrentBootcamp
{
    class Program
    {
        static void Main(string[] args)
        {
            ProducerConsumerExample();
            Console.WriteLine("Press Any key to exit.");
            Console.ReadKey();
        }

        private static void ProducerConsumerExample()
        {
            var count = 100;

            var productionTasks = new ConcurrentBag<Task>();
            var consumptionTasks = new ConcurrentBag<Task>();

            // Creating a blocking collection with bounding to free up threads for consumption
            var blockingCollection = new BlockingCollection<int>(10);

            var consumerMain = new TaskFactory().StartNew(() => {
                while(true)
                {
                    try
                    {
                        // for indeterminable operations, use cancellation token
                        var consumed = blockingCollection.Take();

                        consumptionTasks.Add(new TaskFactory().StartNew(() => {
                            // expensive operation, i.e. calling API based on data
                            Thread.Sleep(100);
                            Console.WriteLine($"Consumed {consumed} from blocking collection");
                        },TaskCreationOptions.LongRunning));
                    }
                    catch(InvalidOperationException)
                    {
                        // Done.
                        Console.WriteLine("Completed consumption task generation");
                        break;
                    }
                };

            },TaskCreationOptions.LongRunning);

            Parallel.For(0, count, (i) => {
                productionTasks.Add(new TaskFactory().StartNew(() => {
                    // Expensive operation, ie database retrieval
                    Thread.Sleep(1000);
                    blockingCollection.Add(i);
                    Console.WriteLine($"Added {i} to blocking collection.");
                },TaskCreationOptions.LongRunning));
            });

            // if tasks are determinable like this example....
            Task.WaitAll(productionTasks.ToArray());
            blockingCollection.CompleteAdding();

            // wait on the main consumer to finish adding tasks, then wait on tasks....
            consumerMain.Wait();
            Task.WaitAll(consumptionTasks.ToArray());
        }
    }
}
