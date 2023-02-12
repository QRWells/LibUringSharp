namespace LibUringSharp.Linux;

public static partial class LibC
{
    public unsafe struct cpu_set_t
    {
        public fixed int __bits[32];
    }
}