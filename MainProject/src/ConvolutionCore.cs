public static class ConvolutionCore
{
    public static float[,] Sequential(float[,] source, float[,] kernel)
    {
        int height = source.GetLength(0);
        int width = source.GetLength(1);
        int radiusY = kernel.GetLength(0) / 2;
        int radiusX = kernel.GetLength(1) / 2;
        float[,] result = new float[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[y, x] = CalculatePixel(source, kernel, y, x, height, width, radiusY, radiusX);
            }
        }
        return result;
    }

    public static float[,] ParallelByRows(float[,] source, float[,] kernel)
    {
        int height = source.GetLength(0);
        int width = source.GetLength(1);
        int radiusY = kernel.GetLength(0) / 2;
        int radiusX = kernel.GetLength(1) / 2;
        float[,] result = new float[height, width];

        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                result[y, x] = CalculatePixel(source, kernel, y, x, height, width, radiusY, radiusX);
            }
        });
        return result;
    }

    public static float[,] ParallelByColumns(float[,] source, float[,] kernel)
    {
        int height = source.GetLength(0);
        int width = source.GetLength(1);
        int radiusY = kernel.GetLength(0) / 2;
        int radiusX = kernel.GetLength(1) / 2;
        float[,] result = new float[height, width];

        Parallel.For(0, width, x =>
        {
            for (int y = 0; y < height; y++)
            {
                result[y, x] = CalculatePixel(source, kernel, y, x, height, width, radiusY, radiusX);
            }
        });
        return result;
    }

    public static float[,] ParallelByPixel(float[,] source, float[,] kernel)
    {
        int height = source.GetLength(0);
        int width = source.GetLength(1);
        int radiusY = kernel.GetLength(0) / 2;
        int radiusX = kernel.GetLength(1) / 2;
        float[,] result = new float[height, width];
        int totalPixels = height * width;

        Parallel.For(0, totalPixels, i =>
        {
            int y = i / width;
            int x = i % width;
            result[y, x] = CalculatePixel(source, kernel, y, x, height, width, radiusY, radiusX);
        });
        return result;
    }

    public static float[,] ParallelByGrid(float[,] source, float[,] kernel, int blockSize = 64)
    {
        int height = source.GetLength(0);
        int width = source.GetLength(1);
        int radiusY = kernel.GetLength(0) / 2;
        int radiusX = kernel.GetLength(1) / 2;
        float[,] result = new float[height, width];

        int blocksY = (int)Math.Ceiling((double)height / blockSize);
        int blocksX = (int)Math.Ceiling((double)width / blockSize);

        Parallel.For(0, blocksY * blocksX, b =>
        {
            int by = b / blocksX;
            int bx = b % blocksX;
            int startY = by * blockSize;
            int endY = Math.Min(startY + blockSize, height);
            int startX = bx * blockSize;
            int endX = Math.Min(startX + blockSize, width);

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    result[y, x] = CalculatePixel(source, kernel, y, x, height, width, radiusY, radiusX);
                }
            }
        });
        return result;
    }

    private static float CalculatePixel(float[,] source, float[,] kernel, int y, int x, int height, int width, int radiusY, int radiusX)
    {
        float sum = 0;
        for (int ky = -radiusY; ky <= radiusY; ky++)
        {
            for (int kx = -radiusX; kx <= radiusX; kx++)
            {
                int py = y + ky;
                int px = x + kx;
                float pixelValue = (py >= 0 && py < height && px >= 0 && px < width) ? source[py, px] : 0;
                sum += pixelValue * kernel[ky + radiusY, kx + radiusX];
            }
        }
        return sum;
    }
}
