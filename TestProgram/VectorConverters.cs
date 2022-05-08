using Newtonsoft.Json;
using OpenTK.Mathematics;

namespace TestProgram;

public class Vector2iConverter : JsonConverter<Vector2i> {
    public override void WriteJson(JsonWriter writer, Vector2i value, JsonSerializer serializer) {
        writer.WriteStartObject();
        writer.WritePropertyName("X");
        writer.WriteValue(value.X);
        writer.WritePropertyName("Y");
        writer.WriteValue(value.Y);
        writer.WriteEndObject();
    }

    public override Vector2i ReadJson(JsonReader reader, Type objectType, Vector2i existingValue, bool hasExistingValue, JsonSerializer serializer) {
        throw new NotImplementedException();
    }
}

public class Vector2Converter : JsonConverter<Vector2> {
    public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer) {
        writer.WriteStartObject();
        writer.WritePropertyName("X");
        writer.WriteValue(value.X);
        writer.WritePropertyName("Y");
        writer.WriteValue(value.Y);
        writer.WriteEndObject();
    }

    public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer) {
        throw new NotImplementedException();
    }
}

public class Vector3Converter : JsonConverter<Vector3> {
    public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer) {
        writer.WriteStartObject();
        writer.WritePropertyName("X");
        writer.WriteValue(value.X);
        writer.WritePropertyName("Y");
        writer.WriteValue(value.Y);
        writer.WritePropertyName("Z");
        writer.WriteValue(value.Z);
        writer.WriteEndObject();
    }

    public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer) {
        throw new NotImplementedException();
    }
}