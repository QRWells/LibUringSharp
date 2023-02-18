using System.Runtime.CompilerServices;
using System.Text;
using QRWells.AsyncIoUring.Async;
using static QRWells.LibUringSharp.Linux.LibC;

static string GetThisFilePath([CallerFilePath] string path = null) => path;
var directory = Path.GetDirectoryName(GetThisFilePath());

var file = AsyncFile.Open($"{directory}/test.txt", OpenOption.ReadOnly, new FilePermissions());

var buffer = new byte[1024];
var count = await file.ReadAsync(buffer, 1024, 0);

file.Close();

Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, count));