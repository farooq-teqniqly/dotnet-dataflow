namespace ProducerConsumerApp;

using System.Threading.Tasks.Dataflow;

internal class Program
{
    static async Task Main(string[] args)
    {
        var urlsToProcess = new[] { "https://www.espn.com", "https://www.microsoft.com", "https://www.google.com" };
        var processQueue = new BufferBlock<string>(new DataflowBlockOptions { BoundedCapacity = urlsToProcess.Length });
        var random = new Random();

        foreach (var url in urlsToProcess)
        {
            await Task.Delay(TimeSpan.FromSeconds(random.Next(5, 10)));
            Console.WriteLine($"Queueing {url}...");
            await processQueue.SendAsync(url);
        }

        var consumerOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 1 };

        var consumerA = new ActionBlock<string>(url => Console.WriteLine($"Processing {url}..."), consumerOptions);
        var consumerB = new ActionBlock<string>(url => Console.WriteLine($"Processing {url}..."), consumerOptions);
        var consumerC = new ActionBlock<string>(url => Console.WriteLine($"Processing {url}..."), consumerOptions);

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        processQueue.LinkTo(consumerA, linkOptions);
        processQueue.LinkTo(consumerB, linkOptions);
        processQueue.LinkTo(consumerC, linkOptions);

        await Task.WhenAll([processQueue.Completion, consumerA.Completion, consumerB.Completion, consumerC.Completion]);
        processQueue.Complete();

        Console.WriteLine("Done!");
    }
}
