namespace ProducerConsumerApp;

using System.Threading.Tasks.Dataflow;

internal class Program
{
    static async Task Main(string[] args)
    {
        var urlsToProcess = new[] { "https://www.espn.com", "https://www.microsoft.com", "https://www.google.com" };

        var consumerOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 1 };
        var consumerA = new ActionBlock<string>(url => Console.WriteLine($"Processing {url}..."), consumerOptions);
        var consumerB = new ActionBlock<string>(url => Console.WriteLine($"Processing {url}..."), consumerOptions);
        var consumerC = new ActionBlock<string>(url => Console.WriteLine($"Processing {url}..."), consumerOptions);

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
        var processQueue = new BufferBlock<string>(new DataflowBlockOptions { BoundedCapacity = urlsToProcess.Length });
        processQueue.LinkTo(consumerA, linkOptions);
        processQueue.LinkTo(consumerB, linkOptions);
        processQueue.LinkTo(consumerC, linkOptions);

        var random = new Random();

        foreach (var url in urlsToProcess)
        {
            Console.WriteLine($"Queueing {url}...");
            await Task.Delay(TimeSpan.FromSeconds(random.Next(2, 5)));
            processQueue.SendAsync(url);
        }

        processQueue.Complete();
        await Task.WhenAll([processQueue.Completion, consumerA.Completion, consumerB.Completion, consumerC.Completion]);


        Console.WriteLine("Done!");
        Console.ReadKey();
    }
}
