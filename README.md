# Parallel Image Convolution

## Overview
This project implements mathematical image convolution (filters) on grayscale images. 
It explores various multithreading approaches, memory access patterns (cache locality), and a pipeline implementation.

**Tech Stack:** C#, .NET 8, ImageSharp, BenchmarkDotNet, xUnit.

## Prerequisites

To build and run this project, you need to install the **.NET 8.0 SDK**.
* [Download .NET 8.0 SDK (Official Site)](https://dotnet.microsoft.com/ru-ru/download/dotnet/8.0)
* [Direct Download Link for Windows x64](https://dotnet.microsoft.com/ru-ru/download/dotnet/thank-you/sdk-8.0.419-windows-x64-installer)

---

## How to Run

Navigate to the root directory of the project (where the `.sln` file is located) and execute the following native `dotnet` commands:

**1. Run Performance Benchmarks:**
```bash
dotnet run --project MainProject/ConvolutionProject.csproj -c Release benchmark
```

**2. Run the Pipeline (Batch Image Processing):**
```bash
dotnet run --project MainProject/ConvolutionProject.csproj pipeline
```

**3. Run Mathematical Unit Tests (xUnit):**
```bash
dotnet test
```

---

## 1. Performance Analysis (Single Image)
Tests were executed on a 225x224 grayscale image using a 3x3 Edge Detection kernel.

**Hardware:**
```
BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
12th Gen Intel Core i5-1235U 1.30GHz, 1 CPU, 12 logical and 10 physical cores
.NET SDK 8.0.419
  [Host]     : .NET 8.0.25 (8.0.25, 8.0.2526.11203), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 8.0.25 (8.0.25, 8.0.2526.11203), X64 RyuJIT x86-64-v3
```

**Benchmarks results**

| Method            | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------ |---------:|---------:|---------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| Sequential        | 893.9 μs | 17.60 μs | 18.83 μs |  1.00 |    0.03 | 61.5234 | 61.5234 | 61.5234 | 196.99 KB |        1.00 |
| ParallelByRows    | 493.7 μs |  9.70 μs | 20.02 μs |  0.55 |    0.02 | 62.0117 | 62.0117 | 62.0117 | 200.97 KB |        1.02 |
| ParallelByColumns | 564.0 μs | 11.27 μs | 22.50 μs |  0.63 |    0.03 | 61.5234 | 61.5234 | 61.5234 | 200.99 KB |        1.02 |
| ParallelByPixel   | 414.8 μs |  8.79 μs | 25.63 μs |  0.46 |    0.03 | 61.5234 | 61.5234 | 61.5234 | 200.79 KB |        1.02 |
| ParallelByGrid    | 419.3 μs |  8.31 μs |  8.53 μs |  0.47 |    0.01 | 62.0117 | 62.0117 | 62.0117 | 201.12 KB |        1.02 |


**Legend**
```
  Mean        : Arithmetic mean of all measurements
  Error       : Half of 99.9% confidence interval
  StdDev      : Standard deviation of all measurements
  Ratio       : Mean of the ratio distribution ([Current]/[Baseline])
  Gen0        : GC Generation 0 collects per 1000 operations
  Gen1        : GC Generation 1 collects per 1000 operations
  Gen2        : GC Generation 2 collects per 1000 operations
  Allocated   : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  Alloc Ratio : Allocated memory ratio distribution ([Current]/[Baseline])
  1 us        : 1 Microsecond (0.000001 sec)
```


### Analysis & Conclusions
**1. Cache Locality (Rows vs Columns):** 
The "By Columns" partitioning method (564.0 μs) is ~14% slower than the "By Rows" method (493.7 μs). This perfectly demonstrates the penalty of **Cache Misses**. The `float[,]` 2D array is stored in memory in row-major order. Iterating column by column violates **Spatial Locality**, forcing the CPU to fetch data from the slower main RAM repeatedly instead of utilizing the fast L1/L2 cache.

**2. Grid/Tiling Approach:**
The `ParallelByGrid` method (419.3 μs) provides excellent performance (~2.1x speedup). Splitting the image into 64x64 chunks strikes a perfect balance between thread workload distribution and cache friendliness.

**3. The TPL Partitioner (By Pixel):**
Theoretically, parallelizing at the single-pixel level should cause massive overhead. However, `ParallelByPixel` performed exceptionally well (414.8 μs). This showcases the optimization of the **.NET TPL (Task Parallel Library)**. Under the hood, the default `Partitioner` dynamically batches multiple tiny iterations into larger chunks, preventing thread-creation overhead and effectively turning the pixel-by-pixel loop into a grid-like execution automatically.

---

## 2. Pipeline Batch Processing
To process large datasets of images efficiently, a pipeline was implemented using `System.Threading.Channels`.
* **1 Producer (Reader):** Asynchronously reads files from the disk and pushes them into the queue.
* **12 Workers:** Pull matrices from the queue and perform the mathematical convolution heavily utilizing the CPU.
* **1 Consumer (Writer):** Pulls the processed matrices, encodes them to `.jpg`, and saves them to the disk.

### Load Balancing & Backpressure
To prevent memory leaks (`OutOfMemoryException`) when the disk read speed exceeds the CPU processing speed, a `BoundedChannel` with a capacity of 5 was used. This creates a natural **Backpressure** mechanism: the Producer thread is suspended (`await`) if the channel is full, avoiding RAM overflow.

**Bottleneck Analysis:** Log observations show that the convolution math on a modern CPU executes almost instantly, making the Writer (saving/encoding the image to the disk) the primary bottleneck of the system.