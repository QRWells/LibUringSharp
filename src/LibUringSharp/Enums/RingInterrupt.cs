namespace QRWells.LibUringSharp.Enums;

[Flags]
public enum RingInterrupt : byte
{
    RegRing = 1 << 0,
    RegRegRing = 1 << 1,
    AppMem = 1 << 2,
}