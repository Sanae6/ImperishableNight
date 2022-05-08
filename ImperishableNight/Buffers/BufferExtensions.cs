namespace ImperishableNight.Buffers;

public static class BufferExtensions {
    public static void Decrypt(this ref SpanBuffer buffer, int size, byte key, byte step, int block, int limit) {
        unchecked {
            Span<byte> temp = stackalloc byte[block];
            int oldSize = size;
            int increment = (block >> 1) + (block & 1);
        
            if (size < block >> 2)
                size = 0;
            else
                size -= (size % block < block >> 2 ? 1 : 0) * size % block + size % 2;
        
            if (limit % block != 0)
                limit += block - limit % block;
        
            int end = size < limit ? size : limit;
            Span<byte> data = buffer.Slice(end);
        
            int s = 0;
            while (data.Length > 0) {
                if (data.Length < block) {
                    block = data.Length;
                    increment = (block >> 1) + (block & 1);
                }
        
                int inIdx = 0, outIdx;
                for (outIdx = block - 1; outIdx > 0;) {
                    temp[outIdx--] = (byte) (data[inIdx] ^ key);
                    temp[outIdx--] = (byte) (data[inIdx + increment] ^ (key + step * increment));
                    inIdx++;
                    key += step;
                }
        
                if ((block & 1) != 0) {
                    temp[outIdx] = (byte) (data[inIdx] ^ key);
                    key += step;
                }
        
                key += (byte)(step * increment);
                temp[..block].CopyTo(data[..]);
                data = data[block..];
            }
        }
    }

    public static SpanBuffer Decompress(this ref SpanBuffer buffer, int outSize) {
        const int size = 0x2000;
        const int mask = 0x1fff;
        const int minMatch = 3;

        SpanBitBuffer bs = new SpanBitBuffer();
        SpanBuffer outBuf = new SpanBuffer(new byte[outSize], buffer.BigEndian);
        int written = 0;
        Span<byte> dict = stackalloc byte[size];
        int dictHead = 1;

        while (written < outSize) {
            if (bs.ReadBool(ref buffer)) {
                byte c = (byte) bs.Read(ref buffer, 8);
                outBuf.WriteU8(c);
                written++;
                dict[dictHead++] = c;
                dictHead &= mask;
            } else {
                uint matchOffset = bs.Read(ref buffer, 13);
                if (matchOffset == 0)
                    break;

                uint matchLen = bs.Read(ref buffer, 4) + minMatch;
                for (int i = 0; i < matchLen; i++) {
                    byte c = dict[(int) ((matchOffset + i) & mask)];
                    outBuf.WriteU8(c);
                    written++;
                    dict[dictHead++] = c;
                    dictHead &= mask;
                }
            }
        }

        outBuf.Offset = 0;

        return outBuf;
    }
}