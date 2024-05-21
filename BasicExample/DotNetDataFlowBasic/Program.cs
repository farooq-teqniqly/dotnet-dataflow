namespace DotNetDataFlowBasic;

using System.Net;
using System.Threading.Tasks.Dataflow;

// Example from https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/walkthrough-creating-a-dataflow-pipeline
internal class Program
{
    internal static readonly char[] _separator = [' '];

    static void Main()
    {
        var downloadString = new TransformBlock<string, string>(async uri =>
        {
            Console.WriteLine($"Downloading {uri}...");

            var httpClient = new HttpClient(
                new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip });

            return await httpClient.GetStringAsync(uri);
        });

        var createdWordList = new TransformBlock<string, string[]>(text =>
        {
            Console.WriteLine("Creating word list...");

            var tokens = text.Select(c => char.IsLetter(c) ? c : ' ').ToArray();
            text = new string(tokens);

            //TODO: extract to static readonly field.
            return text.Split(_separator, StringSplitOptions.RemoveEmptyEntries);
        });

        var filteredWordList = new TransformBlock<string[], string[]>(words =>
        {
            Console.WriteLine("Filtering word list...");

            return words
                .Where(word => word.Length > 3)
                .Distinct()
                .ToArray();
        });

        var findReverseWords = new TransformManyBlock<string[], string>(words =>
        {
            Console.WriteLine("Finding reversed words...");

            var wordsSet = new HashSet<string>(words);

            return from word in words.AsParallel()
                   let reverse = new string(word.Reverse().ToArray())
                   where word != reverse && wordsSet.Contains(reverse)
                   select word;
        });

        var printReversedWords = new ActionBlock<string>(reversedWord =>
        {
            Console.WriteLine(
                $"Found reversed words {reversedWord}/{new string(reversedWord.Reverse().ToArray())}");
        });

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        downloadString.LinkTo(createdWordList, linkOptions);
        createdWordList.LinkTo(filteredWordList, linkOptions);
        filteredWordList.LinkTo(findReverseWords, linkOptions);
        findReverseWords.LinkTo(printReversedWords, linkOptions);

        downloadString.Post("http://www.gutenberg.org/cache/epub/16452/pg16452--x.txt");
        downloadString.Complete();
        printReversedWords.Completion.Wait();

        Console.WriteLine("Done!");
        Console.ReadKey();
    }
}
