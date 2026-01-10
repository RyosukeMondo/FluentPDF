```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 5 5600X, 1 CPU, 12 logical and 6 physical cores
.NET SDK 8.0.122
  [Host]     : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  Job-AXZLFX : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2


```
| Mean | StdDev | Median | Min | Max | P95 |
|-----:|-------:|-------:|----:|----:|----:|
|   NA |     NA |     NA |  NA |  NA |  NA |
|   NA |     NA |     NA |  NA |  NA |  NA |
|   NA |     NA |     NA |  NA |  NA |  NA |
|   NA |     NA |     NA |  NA |  NA |  NA |

Benchmarks with issues:
  MemoryBenchmarks.LoadAndDispose_Document: Job-AXZLFX(Platform=X64, IterationCount=10, WarmupCount=3)
  MemoryBenchmarks.RenderAndDispose_Page: Job-AXZLFX(Platform=X64, IterationCount=10, WarmupCount=3)
  MemoryBenchmarks.LoadRender_100Pages: Job-AXZLFX(Platform=X64, IterationCount=10, WarmupCount=3)
  MemoryBenchmarks.LoadDocument_Baseline: Job-AXZLFX(Platform=X64, IterationCount=10, WarmupCount=3)
