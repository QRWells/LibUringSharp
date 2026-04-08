# LibUringSharp

`QRWells.LibUringSharp` is a low-level, Linux-only C# wrapper over `io_uring`.

## Requirements

- Linux only
- Kernel support depends on the operation you submit
- `IORING_SETUP_NO_MMAP` requires a recent kernel (`Linux 6.5+`)

## Install

```bash
dotnet add package QRWells.LibUringSharp
```

## Basic usage

```csharp
using QRWells.LibUringSharp;

using var ring = new Ring(4);

if (!ring.TryGetNextSubmission(out var submission))
    throw new InvalidOperationException("No SQE available");

submission.PrepareNop(2026);
ring.Prepared(submission);

ring.Submit();

if (ring.TryGetCompletion(out var completion))
{
    Console.WriteLine($"{completion.UserData}: {completion.Result}");
}
```

## External ring memory

```csharp
using QRWells.LibUringSharp;

using var memory = RingMemory.Allocate(256);
using var ring = new Ring(256, memory);
```

Use `Ring.GetRequiredMemorySize(...)` when you need to plan memory usage up front.

## Development

```bash
dotnet build LibUringSharp.sln -c Debug
dotnet test test/LibUringSharp.Test/LibUringSharp.Test.csproj -c Debug -f net10.0
```

## License

MIT
