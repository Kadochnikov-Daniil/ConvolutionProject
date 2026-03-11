using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

[MemoryDiagnoser]
public class ConvolutionBenchmark
{
    private float[,] _sourceMatrix = null!;
    private float[,] _kernel = null!;

    [GlobalSetup]
    public void Setup()
    {
        string projectRoot = Environment.GetEnvironmentVariable("PROJECT_ROOT") ?? Directory.GetCurrentDirectory();
        
        string imgPath = Path.Combine(projectRoot, "test_data", "input.jpg");

        using var image = Image.Load<L8>(imgPath);
        
        _sourceMatrix = new float[image.Height, image.Width];
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<L8> pixelRow = accessor.GetRowSpan(y);
                for (int x = 0; x < pixelRow.Length; x++)
                {
                    _sourceMatrix[y, x] = pixelRow[x].PackedValue;
                }
            }
        });

        _kernel = new float[,]
        {
            {  0, -1,  0 },
            { -1,  4, -1 },
            {  0, -1,  0 }
        };
    }

    [Benchmark(Baseline = true)]
    public float[,] Sequential() => ConvolutionCore.Sequential(_sourceMatrix, _kernel);

    [Benchmark]
    public float[,] ParallelByRows() => ConvolutionCore.ParallelByRows(_sourceMatrix, _kernel);

    [Benchmark]
    public float[,] ParallelByColumns() => ConvolutionCore.ParallelByColumns(_sourceMatrix, _kernel);

    [Benchmark]
    public float[,] ParallelByPixel() => ConvolutionCore.ParallelByPixel(_sourceMatrix, _kernel);

    [Benchmark]
    public float[,] ParallelByGrid() => ConvolutionCore.ParallelByGrid(_sourceMatrix, _kernel);
}
