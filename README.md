# LibUringSharp

A C# wrapper for the Linux io_uring API like [liburing](https://github.com/axboe/liburing).

## Features

- [x] io_uring probe
- [x] Basic Operations
- [x] Select Buffers
- [ ] Async/Await Support
- [ ] Kernel version check

## Example

```csharp
using LibUringSharp;

using var ring = new Ring(4);
using var file = Open("test.txt", OpenOption.Create | OpenOption.Truncate | OpenOption.ReadWrite, new FilePermissions());

var str = "Hello World!";

Submission sub;

// Write to the file
if (ring.TryGetNextSqe(out sub))
{
    var bytes = Encoding.UTF8.GetBytes(str);
    sub.Option |= SubmissionOption.IoLink;
    sub.PrepareWrite(file, bytes, 0);
}

// Read the file
if (ring.TryGetNextSqe(out sub))
{
    var buffer = new byte[str.Length];
    sub.PrepareRead(file, buffer, 0);
}

ring.Submit();

while (ring.TryGetCompletion(out var com))
{
    Console.WriteLine(com.Result); // output: 12
}
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details