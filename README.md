# LibUringSharp

A C# wrapper for the Linux io_uring API like [liburing](https://github.com/axboe/liburing).

## Example

```csharp
using LibUringSharp;

var ring = new Ring(4);

if (ring.TryGetNextSqe(out var sqe))
{
    sqe.PrepareNop(2023);
    ring.SubmitAndWait(1);

    if (ring.TryGetCompletion(out var cqe))
    {
        Console.WriteLine(cqe.UserData);
    }
}

// output: 2023
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details