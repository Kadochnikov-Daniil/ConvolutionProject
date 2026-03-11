using System;
using Xunit;

public class MathTests
{
    // Helper methods
    private float[,] GenerateRandomMatrix(int height, int width)
    {
        var rnd = new Random(42); // Фиксированный сид для повторяемости тестов
        float[,] matrix = new float[height, width];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                matrix[y, x] = (float)rnd.NextDouble() * 255f;
        return matrix;
    }

    private void AssertMatricesEqual(float[,] expected, float[,] actual)
    {
        Assert.Equal(expected.GetLength(0), actual.GetLength(0));
        Assert.Equal(expected.GetLength(1), actual.GetLength(1));

        for (int y = 0; y < expected.GetLength(0); y++)
        {
            for (int x = 0; x < expected.GetLength(1); x++)
            {
                // Сравниваем float с погрешностью 0.0001 (т.к. это числа с плавающей точкой)
                Assert.True(Math.Abs(expected[y, x] - actual[y, x]) < 0.0001f, 
                    $"Mismatch at [{y},{x}]: Expected {expected[y,x]}, got {actual[y,x]}");
            }
        }
    }

    private float[,] GenerateRandomKernel(int size)
    {
        var rnd = new Random(123);
        float[,] matrix = new float[size, size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                matrix[y, x] = (float)(rnd.NextDouble() * 2.0 - 1.0); // от -1.0 до 1.0
        return matrix;
    }

    // All zero filter test
    [Theory]
    [InlineData(10, 10, 3)]
    [InlineData(100, 100, 5)]
    public void ZeroFilterTest(int imgH, int imgW, int kernelSize)
    {
        float[,] source = GenerateRandomMatrix(imgH, imgW);
        float[,] zeroKernel = new float[kernelSize, kernelSize];

        float[,] result = ConvolutionCore.Sequential(source, zeroKernel);

        for (int y = 0; y < imgH; y++)
            for (int x = 0; x < imgW; x++)
                Assert.Equal(0f, result[y, x]);
    }

    // Id filter test(1 in the middle)
    [Theory]
    [InlineData(50, 50, 3)]
    [InlineData(200, 150, 5)]
    public void IdentityFilterTest(int imgH, int imgW, int kernelSize)
    {
        float[,] source = GenerateRandomMatrix(imgH, imgW);
        float[,] idKernel = new float[kernelSize, kernelSize];
        idKernel[kernelSize / 2, kernelSize / 2] = 1f;

        float[,] result = ConvolutionCore.Sequential(source, idKernel);

        AssertMatricesEqual(source, result);
    }

    // Filter extended by zeroes
    [Fact]
    public void PaddedFilterTest()
    {
        float[,] source = GenerateRandomMatrix(20, 20);
        
        float[,] smallKernel = {
            { 1, 1, 1 },
            { 1, 1, 1 },
            { 1, 1, 1 }
        };

        float[,] paddedKernel = {
            { 0, 0, 0, 0, 0 },
            { 0, 1, 1, 1, 0 },
            { 0, 1, 1, 1, 0 },
            { 0, 1, 1, 1, 0 },
            { 0, 0, 0, 0, 0 }
        };

        float[,] resultSmall = ConvolutionCore.Sequential(source, smallKernel);
        float[,] resultPadded = ConvolutionCore.Sequential(source, paddedKernel);

        AssertMatricesEqual(resultSmall, resultPadded);
    }

    // All the parallel versions compared to sequential
    [Theory]
    [InlineData(150, 150, 3)]
    [InlineData(64, 64, 5)]
    public void ParallelVersionsTest(int imgH, int imgW, int kernelSize)
    {
        float[,] source = GenerateRandomMatrix(imgH, imgW);
        float[,] randomKernel = GenerateRandomKernel(kernelSize);

        float[,] expectedSequential = ConvolutionCore.Sequential(source, randomKernel);
        
        float[,] resultByRows = ConvolutionCore.ParallelByRows(source, randomKernel);
        float[,] resultByColumns = ConvolutionCore.ParallelByColumns(source, randomKernel);
        float[,] resultByPixel = ConvolutionCore.ParallelByPixel(source, randomKernel);
        float[,] resultByGrid = ConvolutionCore.ParallelByGrid(source, randomKernel);

        AssertMatricesEqual(expectedSequential, resultByRows);
        AssertMatricesEqual(expectedSequential, resultByColumns);
        AssertMatricesEqual(expectedSequential, resultByPixel);
        AssertMatricesEqual(expectedSequential, resultByGrid);
    }
}
