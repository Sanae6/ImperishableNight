namespace ImperishableNight.Buffers;

public interface IDynamicStructure {
    void Load(ref SpanBuffer slice);
    void Save(ref SpanBuffer slice);
}
