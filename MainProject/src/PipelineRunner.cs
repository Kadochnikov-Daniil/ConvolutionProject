using System.Threading.Channels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class ImageContext
{
    public string FileName { get; set; } = string.Empty;
    public float[,] Matrix { get; set; } = null!;
    public int Width { get; set; }
    public int Height { get; set; }
}

public static class PipelineRunner
{
    public static async Task StartAsync()
    {
        string inputDir = "input_folder";
        string outputDir = "output_folder";

        Directory.CreateDirectory(outputDir);

        string[] files = Directory.GetFiles(inputDir, "*.jpg");
        if (files.Length == 0)
        {
            Console.WriteLine($"[Error] No files .jpg in the directory {inputDir}!");
            return;
        }

        Console.WriteLine($"Files found: {files.Length}. Running pipeline...\n");

        // Limit channels to 5 items
        var options = new BoundedChannelOptions(5) { FullMode = BoundedChannelFullMode.Wait };
        var readToProcessChannel = Channel.CreateBounded<ImageContext>(options);
        var processToWriteChannel = Channel.CreateBounded<ImageContext>(options);

        float[,] kernel = {
            {  0, -1,  0 },
            { -1,  4, -1 },
            {  0, -1,  0 }
        };

        // Set reader
        var readerTask = Task.Run(async () =>
        {
            foreach (var file in files)
            {
                using var image = await Image.LoadAsync<L8>(file);
                var context = new ImageContext
                {
                    FileName = Path.GetFileName(file),
                    Width = image.Width,
                    Height = image.Height,
                    Matrix = new float[image.Height, image.Width]
                };

                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<L8> pixelRow = accessor.GetRowSpan(y);
                        for (int x = 0; x < pixelRow.Length; x++)
                        {
                            context.Matrix[y, x] = pixelRow[x].PackedValue;
                        }
                    }
                });

                await readToProcessChannel.Writer.WriteAsync(context);
                Console.WriteLine($"[READER] Loaded: {context.FileName}");
            }
            readToProcessChannel.Writer.Complete();
        });

        // Set workers
        int workerCount = Environment.ProcessorCount;
        var workerTasks = new Task[workerCount];

        for (int i = 0; i < workerCount; i++)
        {
            int workerId = i + 1;
            workerTasks[i] = Task.Run(async () =>
            {
                await foreach (var context in readToProcessChannel.Reader.ReadAllAsync())
                {
                    Console.WriteLine($"   [WORKER] {workerId}] Conv: {context.FileName}");
                    context.Matrix = ConvolutionCore.Sequential(context.Matrix, kernel);
                    await processToWriteChannel.Writer.WriteAsync(context);
                }
            });
        }

        var allWorkersTask = Task.WhenAll(workerTasks).ContinueWith(_ => processToWriteChannel.Writer.Complete());

        // Set writers
        var writerTask = Task.Run(async () =>
        {
            await foreach (var context in processToWriteChannel.Reader.ReadAllAsync())
            {
                using var outputImage = new Image<L8>(context.Width, context.Height);
                outputImage.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<L8> pixelRow = accessor.GetRowSpan(y);
                        for (int x = 0; x < pixelRow.Length; x++)
                        {
                            byte byteVal = (byte)Math.Clamp(context.Matrix[y, x], 0, 255);
                            pixelRow[x] = new L8(byteVal);
                        }
                    }
                });

                string outPath = Path.Combine(outputDir, context.FileName);
                await outputImage.SaveAsync(outPath);
                Console.WriteLine($"[WRITER] Saved: {context.FileName}");
            }
        });

        await Task.WhenAll(readerTask, allWorkersTask, writerTask);
        Console.WriteLine("\n[SUCCESS] Pipeline finished!");
    }
}
