class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("ERROR: Please specify the execution mode!");
            Console.WriteLine("=================================================");
            Console.WriteLine("Available commands:");
            Console.WriteLine("1. Run parallel implementation with benchmarks:");
            Console.WriteLine("   dotnet run -c Release -- benchmark");
            Console.WriteLine("");
            Console.WriteLine("2. Run pipeline processing:");
            Console.WriteLine("   dotnet run -- pipeline");
            Console.WriteLine("=================================================");
            return;
        }

        string mode = args[0].ToLower();

        if (mode == "benchmark")
        {
            Console.WriteLine("Running parallel implementation with benchmarks.");

            Environment.SetEnvironmentVariable("PROJECT_ROOT", Directory.GetCurrentDirectory());
            
            BenchmarkDotNet.Running.BenchmarkRunner.Run<ConvolutionBenchmark>();
        }
        else if (mode == "pipeline")
        {
            Console.WriteLine("Running pipeline implementation.");
            await PipelineRunner.StartAsync();
        }
        else
        {
            Console.WriteLine($"Unknown command: {mode}");
        }
    }
}
