namespace ImperishableNight.Buffers; 

public struct Bookmark {
    public int Offset;
    public int Size;

    public Bookmark(int offset) {
        Offset = offset;
        Size = 0;
    }

    public static int  operator +(Bookmark a, int b) => a.Offset + b;
    public static uint operator +(Bookmark a, uint b) => (uint) a.Offset + b;
    public static int  operator -(Bookmark a, int b) => a.Offset - b;
    public static uint operator -(Bookmark a, uint b) => (uint) a.Offset - b;
    public static bool operator ==(Bookmark a, int b) => a.Offset == b;
    public static bool operator ==(Bookmark a, uint b) => a.Offset == b;
    public static bool operator !=(Bookmark a, int b) => a.Offset != b;
    public static bool operator !=(Bookmark a, uint b) => a.Offset != b;
    public static implicit operator bool(Bookmark bookmark) => bookmark.Offset > 0;
    public static implicit operator int(Bookmark bookmark) => bookmark.Offset;
    public static implicit operator Bookmark(int x) => At(x);
    public static implicit operator Bookmark(uint x) => At((int) x);

    public void Toggle(ref SpanBuffer buffer) => (buffer.Offset, Offset) = (Offset, buffer.Offset);

    public void Jump(ref SpanBuffer buffer) => buffer.Offset = Offset;
    public void Jump(ref SpanBuffer buffer, int offset) => buffer.Offset = Offset + offset;


    public bool Equals(Bookmark other) => Offset == other.Offset && Size == other.Size;

    public override bool Equals(object obj) => obj is Bookmark other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Offset, Size);

    public static Bookmark At(int offset, int size = 0) => new Bookmark {Offset = offset, Size = size};
}