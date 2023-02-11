using System.Runtime.InteropServices;

namespace Linux;

public static partial class LibC
{
    public enum SocketDomain
    {
        AF_UNSPEC = 0,
        AF_UNIX = 1,
        AF_LOCAL = 1,
        AF_FILE = 1,
        AF_INET = 2,
        AF_AX25 = 3,
        AF_IPX = 4,
        AF_APPLETALK = 5,
        AF_NETROM = 6,
        AF_BRIDGE = 7,
        AF_ATMPVC = 8,
        AF_X25 = 9,
        AF_INET6 = 10,
        AF_ROSE = 11,
        AF_DECnet = 12,
        AF_NETBEUI = 13,
        AF_SECURITY = 14,
        AF_KEY = 15,
        AF_NETLINK = 16,
        AF_ROUTE = AF_NETLINK,
        AF_PACKET = 17,
        AF_ASH = 18,
        AF_ECONET = 19,
        AF_ATMSVC = 20,
        AF_RDS = 21,
        AF_SNA = 22,
        AF_IRDA = 23,
        AF_PPPOX = 24,
        AF_WANPIPE = 25,
        AF_LLC = 26,
        AF_IB = 27,
        AF_MPLS = 28,
        AF_CAN = 29,
        AF_TIPC = 30,
        AF_BLUETOOTH = 31,
        AF_IUCV = 32,
        AF_RXRPC = 33,
        AF_ISDN = 34,
        AF_PHONET = 35,
        AF_IEEE802154 = 36,
        AF_CAIF = 37,
        AF_ALG = 38,
        AF_NFC = 39,
        AF_VSOCK = 40,
        AF_KCM = 41,
        AF_QIPCRTR = 42,
        AF_SMC = 43,
        AF_XDP = 44,
        AF_MCTP = 45
    }

    public enum SocketProtocol
    {
        Ip = 0,
        Icmp = 1,
        Igmp = 2,
        Ggp = 3,
        IpEncap = 4,
        St = 5,
        Tcp = 6,
        Egp = 8,
        Igp = 9,
        Pup = 12,
        Udp = 17,
        Hmp = 20,
        XnsIdp = 22,
        Rdp = 27,
        IsoTp4 = 29,
        Dccp = 33,
        Xtp = 36,
        Ddp = 37,
        IdprCmtp = 38,
        IpV6 = 41,
        Ipv6Route = 43,
        Ipv6Frag = 44,
        Idrp = 45,
        Rsvp = 46,
        Gre = 47,
        Esp = 50,
        Ah = 51,
        Skip = 57,
        Ipv6Icmp = 58,
        Ipv6NoNxt = 59,
        Ipv6Opts = 60,
        RspfIgp = 73,
        Sctp = 132
    }

    public enum SocketType
    {
        SOCK_STREAM = 1,
        SOCK_DGRAM = 2,
        SOCK_RAW = 3,
        SOCK_RDM = 4,
        SOCK_SEQPACKET = 5,
        SOCK_DCCP = 6,
        SOCK_PACKET = 10,

        SOCK_CLOEXEC = 0x80000,
        SOCK_NONBLOCK = 0x800
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct sockaddr
    {
        public ushort sa_family;
        public unsafe fixed byte sa_data[14];
    }
}