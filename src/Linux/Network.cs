using System.Runtime.InteropServices;

namespace Linux;

public static partial class LibC
{
    [StructLayout(LayoutKind.Sequential)]
    public struct sockaddr
    {
        public ushort sa_family;
        public unsafe fixed byte sa_data[14];
    }
}