using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ImperishableNight.Buffers; 

public ref struct SpanBuffer {
    public Span<byte> Buffer;
    public int Size => Buffer.Length;
    public int Offset { get; set; }
    public bool HasLeft => Offset < Size;
    public bool BigEndian { get; set; }

    public SpanBuffer(Span<byte> data, bool bigEndian = false) {
        Buffer = data;
        BigEndian = bigEndian;
        Offset = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ThrowIfEndOfBuffer(int neededSize) {
        if (Offset + neededSize > Buffer.Length)
            throw new IndexOutOfRangeException("Buffer has no more space to read from");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadU8() {
        ThrowIfEndOfBuffer(sizeof(byte));
        return Buffer[Offset++];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadI8() => unchecked((sbyte) ReadU8());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadU16() {
        ThrowIfEndOfBuffer(sizeof(ushort));
        ushort value = BigEndian ? BinaryPrimitives.ReadUInt16BigEndian(Buffer[Offset..]) : BinaryPrimitives.ReadUInt16LittleEndian(Buffer[Offset..]);
        Offset += sizeof(ushort);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadI16() {
        ThrowIfEndOfBuffer(sizeof(short));
        short value = BigEndian ? BinaryPrimitives.ReadInt16BigEndian(Buffer[Offset..]) : BinaryPrimitives.ReadInt16LittleEndian(Buffer[Offset..]);
        Offset += sizeof(short);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadU24() {
        return (uint) (BigEndian ? ReadU8() << 16 | ReadU8() << 8 | ReadU8() << 0 : ReadU8() << 0 | ReadU8() << 8 | ReadU8() << 16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadI24() {
        uint value = ReadU24();
        value |= value & 0x800;
        value &= ~0x800u;
        return unchecked((int) value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadU32() {
        ThrowIfEndOfBuffer(sizeof(uint));
        uint value = BigEndian ? BinaryPrimitives.ReadUInt32BigEndian(Buffer[Offset..]) : BinaryPrimitives.ReadUInt32LittleEndian(Buffer[Offset..]);
        Offset += sizeof(uint);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadI32() {
        ThrowIfEndOfBuffer(sizeof(int));
        int value = BigEndian ? BinaryPrimitives.ReadInt32BigEndian(Buffer[Offset..]) : BinaryPrimitives.ReadInt32LittleEndian(Buffer[Offset..]);
        Offset += sizeof(int);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadU64() {
        ThrowIfEndOfBuffer(sizeof(ulong));
        ulong value = BigEndian ? BinaryPrimitives.ReadUInt64BigEndian(Buffer[Offset..]) : BinaryPrimitives.ReadUInt64LittleEndian(Buffer[Offset..]);
        Offset += sizeof(ulong);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadI64() {
        ThrowIfEndOfBuffer(sizeof(long));
        long value = BigEndian ? BinaryPrimitives.ReadInt64BigEndian(Buffer[Offset..]) : BinaryPrimitives.ReadInt64LittleEndian(Buffer[Offset..]);
        Offset += sizeof(long);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadF32() {
        return BitConverter.Int32BitsToSingle(ReadI32());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadF64() {
        return BitConverter.Int64BitsToDouble(ReadI64());
    }

    public Span<byte> ReadBytes(int size) {
        ThrowIfEndOfBuffer(size);
        Span<byte> value = Slice(Offset, size);
        Offset += size;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString(int size, Encoding? encoding = null) {
        ThrowIfEndOfBuffer(size);
        string value = (encoding ?? Encoding.UTF8).GetString(Slice(Offset, size));
        Offset += size;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadStringNull(Encoding? encoding = null) {
        int size = 0;
        while (HasLeft && Buffer[GetOffset(SeekOrigin.Current, size)] != 0) {
            size++;
        }

        string value = (encoding ?? Encoding.UTF8).GetString(Slice(Offset, size));
        Offset += HasLeft ? ++size : size;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadStringNull(int maxSize, Encoding? encoding = null) {
        ThrowIfEndOfBuffer(maxSize);
        int size = 0;
        while (HasLeft && Buffer[GetOffset(SeekOrigin.Current, size)] != 0 && size < maxSize) {
            size++;
        }

        string value = (encoding ?? Encoding.UTF8).GetString(Slice(size));
        Offset += maxSize;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadDynamic<T>() where T : struct, IDynamicStructure {
        T structure = Activator.CreateInstance<T>();
        structure.Load(ref this);
        return structure;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Read<T>() where T : unmanaged {
        int size = Unsafe.SizeOf<T>();
        ThrowIfEndOfBuffer(size);
        T t = MemoryMarshal.Read<T>(Buffer[Offset..]);
        Offset += size;
        return t;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte PeekU8() {
        ThrowIfEndOfBuffer(sizeof(byte));
        return Buffer[Offset];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU8(byte value) {
        ThrowIfEndOfBuffer(sizeof(byte));
        Buffer[Offset++] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteRepeatedU8(byte value, int count) {
        ThrowIfEndOfBuffer(sizeof(byte) * count);
        int i = 0;
        while (i < count) {
            i++;
            WriteU8(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI8(sbyte value) {
        WriteU8(unchecked((byte) value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI16(short value) {
        ThrowIfEndOfBuffer(sizeof(short));
        if (BigEndian) BinaryPrimitives.WriteInt16BigEndian(Slice(sizeof(short)), value);
        else BinaryPrimitives.WriteInt16LittleEndian(Slice(sizeof(short)), value);
        Offset += sizeof(short);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU16(ushort value) {
        ThrowIfEndOfBuffer(sizeof(ushort));
        if (BigEndian) BinaryPrimitives.WriteUInt16BigEndian(Slice(sizeof(ushort)), value);
        else BinaryPrimitives.WriteUInt16LittleEndian(Slice(sizeof(ushort)), value);
        Offset += sizeof(ushort);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU32(uint value) {
        ThrowIfEndOfBuffer(sizeof(uint));
        if (BigEndian) BinaryPrimitives.WriteUInt32BigEndian(Slice(sizeof(uint)), value);
        else BinaryPrimitives.WriteUInt32LittleEndian(Slice(sizeof(uint)), value);
        Offset += sizeof(uint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI32(int value) {
        ThrowIfEndOfBuffer(sizeof(int));
        if (BigEndian) BinaryPrimitives.WriteInt32BigEndian(Slice(sizeof(int)), value);
        else BinaryPrimitives.WriteInt32LittleEndian(Slice(sizeof(int)), value);
        Offset += sizeof(int);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI64(long value) {
        ThrowIfEndOfBuffer(sizeof(long));
        if (BigEndian) BinaryPrimitives.WriteInt64BigEndian(Slice(sizeof(long)), value);
        else BinaryPrimitives.WriteInt64LittleEndian(Slice(sizeof(long)), value);
        Offset += sizeof(long);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU64(ulong value) {
        ThrowIfEndOfBuffer(sizeof(ulong));
        if (BigEndian) BinaryPrimitives.WriteUInt64BigEndian(Slice(sizeof(ulong)), value);
        else BinaryPrimitives.WriteUInt64LittleEndian(Slice(sizeof(ulong)), value);
        Offset += sizeof(ulong);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteF32(float value) {
        WriteI32(BitConverter.SingleToInt32Bits(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteF64(double value) {
        WriteI64(BitConverter.DoubleToInt64Bits(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(string value, Encoding encoding = null) {
        encoding ??= Encoding.UTF8;
        int size = encoding.GetByteCount(value);
        ThrowIfEndOfBuffer(size);
        encoding.GetBytes(value).CopyTo(Slice(size));
        Offset += size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteStringNull(string value, Encoding encoding = null) {
        WriteString(value + '\0');
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetOffset(SeekOrigin origin, int location) {
        return (origin switch {
            SeekOrigin.Begin => location,
            SeekOrigin.Current => Offset + location,
            SeekOrigin.End => Size - location,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        }) switch {
            < 0 => throw new ArgumentOutOfRangeException(nameof(location), location, null),
            var offset when offset > Size => throw new ArgumentOutOfRangeException(nameof(location), location, null),
            var offset => offset
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> Slice(SeekOrigin origin, int location, int size) {
        ThrowIfEndOfBuffer(size);
        int offset = GetOffset(origin, location);
        return Buffer[offset..(offset + size)];
    }

    /// <summary>
    /// Slice using a provided offset and size
    /// </summary>
    /// <param name="offset">Offset to slice at</param>
    /// <param name="size">Size of </param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> Slice(int offset, int size) {
        ThrowIfEndOfBuffer(size);
        return Buffer[offset..(offset + size)];
    }

    /// <summary>
    /// Slice using a bookmark's offset and a provided size
    /// </summary>
    /// <param name="bookmark">Bookmark with offset</param>
    /// <param name="size">Desired size of slice</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> Slice(Bookmark bookmark, int size) {
        ThrowIfEndOfBuffer(size);
        return Buffer[bookmark.Offset..(bookmark.Offset + size)];
    }

    /// <summary>
    /// Slice using a Bookmark's offset and size 
    /// </summary>
    /// <param name="bookmark">Bookmark with offset and size</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> Slice(Bookmark bookmark) {
        ThrowIfEndOfBuffer(bookmark.Size);
        return Buffer[bookmark.Offset..(bookmark.Offset + bookmark.Size)];
    }

    /// <summary>
    /// Slice the buffer at the current offset to the provided size
    /// </summary>
    /// <param name="size">Desired size of slice</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> Slice(int size) {
        ThrowIfEndOfBuffer(size);
        return Buffer[Offset..(Offset + size)];
    }
    
    /// <summary>
    /// Slice the buffer at the current offset to the size of T times the provided count
    /// </summary>
    /// <param name="size">Desired size of slice</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> Slice<T>(int count) where T : unmanaged {
        ThrowIfEndOfBuffer(Unsafe.SizeOf<T>() * count);
        return MemoryMarshal.Cast<byte, T>(Buffer[Offset..(Offset + Unsafe.SizeOf<T>() * count)]);
    }

    public Span<byte> this[Range range] => Buffer[range];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bookmark GetBookmark(SeekOrigin origin, int location, int size = 0) {
        return new Bookmark {
            Offset = GetOffset(origin, location),
            Size = size
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bookmark BookmarkLocation(int size = 0) {
        ThrowIfEndOfBuffer(size);
        Bookmark bookmark = new Bookmark {
            Offset = Offset,
            Size = size
        };
        Offset += size;
        return bookmark;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Align4() {
        while (Offset % 4 != 0) {
            ThrowIfEndOfBuffer(1);
            Offset++;
        }
    }

    /// <remarks>Causes allocations unfortunately.</remarks>
    /// <remarks>Takes endianness from left parameter.</remarks>
    public static SpanBuffer operator +(SpanBuffer a, SpanBuffer b) {
        return new SpanBuffer(new byte[a.Size + b.Size], a.BigEndian);
    }
}