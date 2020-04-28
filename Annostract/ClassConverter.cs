using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

public abstract class ClassConverter<T> : JsonConverter<T> where T : class
{
    // private enum TypeDiscriminator
    // {
    //     BaseClass = 0,
    //     DerivedA = 1,
    //     DerivedB = 2
    // }

    public override bool CanConvert(Type type)
    {
        return typeof(T).IsAssignableFrom(type);
    }

    public abstract List<Type> GetPossibleTypes(); 

    public override T Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || reader.GetString() != "TypeDiscriminator")
        {
            throw new JsonException();
        }

        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException();
        }

        T baseClass;
        var typeDiscriminator = reader.GetInt32();
        var possibleTypes = GetPossibleTypes().Prepend(typeof(T)).ToArray();

        if(typeDiscriminator < 0 && typeDiscriminator <= possibleTypes.Length) {
            throw new NotSupportedException();
        }

        Type t = possibleTypes[typeDiscriminator];

        if (!reader.Read() || reader.GetString() != "TypeValue")
        {
            throw new JsonException();
        }
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        baseClass = JsonSerializer.Deserialize(ref reader, t) as T;

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
        {
            throw new JsonException();
        }

        return baseClass;
    }

    public override void Write(
        Utf8JsonWriter writer,
        T value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        var possibleTypes = GetPossibleTypes().Prepend(typeof(T)).ToList();
        writer.WriteNumber("TypeDiscriminator", possibleTypes.IndexOf(value.GetType()));
        writer.WritePropertyName("TypeValue");
        dynamic dyn = value;
        JsonSerializer.Serialize(writer, (object) dyn);        
        writer.WriteEndObject();
    }
}