namespace ImperishableNight.Buffers;

public ref struct SpanBitBuffer {
    private uint byteCount = 0;
    private uint curBits = 0;
    private uint curByte = 0;

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

        while (bits > curBits) {
            byte c = buffer.ReadU8();
            curByte = (curByte << 8) | c;
            curBits += 8;
            byteCount++;
        }

        curBits -= bits;
        return (uint) ((curByte >> (int) curBits) & ((1 << (int) bits) - 1));
    }
}