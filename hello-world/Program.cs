using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;

namespace hello_world;

class Personne
{
    public string Nom { get; set; }
    public int Age { get; set; }

    public string Hello(bool isLowercase)
    {
        string message = $"hello {Nom}, you are {Age}";
        return isLowercase ? message : message.ToUpper();
    }
}

class Program
{
    static void Main(string[] args)
    {
        // JSON part
        Personne p = new Personne();
        p.Nom = "Alice";
        p.Age = 25;

        string json = JsonConvert.SerializeObject(p, Formatting.Indented);
        Console.WriteLine(json);

        // Image part
        string inputPath = "../image.png";
        int count = 100;
        List<string> inputPaths = Enumerable.Range(0, count).Select(_ => inputPath).ToList();

        // --- Sequential ---
        Console.WriteLine("\n=== Sequential ===");
        long seqFirst = 0;
        Stopwatch swItem = Stopwatch.StartNew();
        Stopwatch swTotal = Stopwatch.StartNew();

        for (int i = 0; i < inputPaths.Count; i++)
        {
            swItem.Restart();
            using Image img = Image.Load(inputPaths[i]);
            img.Mutate(x => x.Resize(200, 200));

            if (i == 0)
            {
                img.Save("../image_resized.png");
                seqFirst = swItem.ElapsedMilliseconds;
                Console.WriteLine($"First resize saved → {seqFirst}ms");
            }
            // rest are discarded
        }

        swTotal.Stop();
        Console.WriteLine($"Total time:   {swTotal.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average/op:   {swTotal.ElapsedMilliseconds / count}ms");
        Console.WriteLine($"Throughput:   {count / (swTotal.ElapsedMilliseconds / 1000.0):F1} images/sec");

        // --- Parallel ---
        Console.WriteLine("\n=== Parallel ===");
        long parFirst = 0;
        int firstDone = 0;
        Stopwatch swPar = Stopwatch.StartNew();

        Parallel.ForEach(inputPaths.Select((path, i) => (path, i)), item =>
        {
            Stopwatch swLocal = Stopwatch.StartNew();
            using Image img = Image.Load(item.path);
            img.Mutate(x => x.Resize(200, 200));

            if (Interlocked.Exchange(ref firstDone, 1) == 0)
            {
                img.Save("../image_resized_parallel.png");
                parFirst = swLocal.ElapsedMilliseconds;
                Console.WriteLine($"First resize saved → {parFirst}ms");
            }
            // rest are discarded
        });

        swPar.Stop();
        Console.WriteLine($"Total time:   {swPar.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average/op:   {swPar.ElapsedMilliseconds / count}ms");
        Console.WriteLine($"Throughput:   {count / (swPar.ElapsedMilliseconds / 1000.0):F1} images/sec");

        // --- Summary ---
        Console.WriteLine("\n=== Summary ===");
        Console.WriteLine($"{"":20} {"Sequential",12} {"Parallel",12}");
        Console.WriteLine($"{"First resize (ms)",-20} {seqFirst,12} {parFirst,12}");
        Console.WriteLine($"{"Total time (ms)",-20} {swTotal.ElapsedMilliseconds,12} {swPar.ElapsedMilliseconds,12}");
        Console.WriteLine($"{"Avg per op (ms)",-20} {swTotal.ElapsedMilliseconds / count,12} {swPar.ElapsedMilliseconds / count,12}");
        Console.WriteLine($"{"Speedup",-20} {"1.0x",12} {(double)swTotal.ElapsedMilliseconds / swPar.ElapsedMilliseconds,12:F2}x");
    }
}
