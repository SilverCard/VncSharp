namespace VncSharp
{
    public enum RfbEncodingType : int
    {
        Raw =       0x00000000,
        CopyRect =  0x00000001,
        RRE =       0x00000002,
        CoRRE =     0x00000004,
        Hextile =   0x00000005,
        Zlib =      0x00000006,
        Tight =     0x00000007,
        ZlibHex =   0x00000008,
        Ultra =     0x00000009,
        ZRLE =      0x00000010,
        ZYWRLE =    0x00000011
    }
}
