namespace ImperishableNight.Buffers;

public ref struct SpanBitBuffer {
    private uint ByteCount = 0;
    private uint CurBits = 0;
    private uint CurByte = 0;

    public SpanBitBuffer() { }

    public bool ReadBool(ref SpanBuffer buffer) {
        return Read(ref buffer, 1) > 0;
    }

    public uint Read(ref SpanBuffer buffer, uint bits) {
        if (bits > 25) {
            uint r = Read(ref buffer, 24);
            bits -= 24;
            return (r << (int) bits) | Read(ref buffer, bits);
        }

        while (bits > CurBits) {
            byte c = buffer.ReadU8();
            CurByte = (CurByte << 8) | c;
            CurBits += 8;
            ByteCount++;
        }

        CurBits -= bits;
        return (uint) ((CurByte >> (int) CurBits) & ((1 << (int) bits) - 1));
    }
}