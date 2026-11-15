using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.BufferedDeserialization;

namespace Motely.Filters;

/// <summary>
/// Clause slots are <see cref="object"/>. The first key that names a clause (<c>joker:</c>,
/// <c>tag:</c>, <c>or:</c>) picks the concrete class, and the mapping is read again as that class.
/// </summary>
internal sealed class ClauseKeyDeserializer : INodeDeserializer
{
    internal const string NeedsKey = "a clause needs a key such as joker:, rareJoker:, voucher:, boss:, and: or or:.";

    public bool Deserialize(
        IParser reader,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value,
        ObjectDeserializer rootDeserializer
    )
    {
        value = null;
        // Only clause slots are object-typed, so a scalar or a list read as one is a clause that is not a mapping.
        if (expectedType == typeof(object) && reader.Current is Scalar or SequenceStart)
            throw new YamlException(reader.Current.Start, reader.Current.End, NeedsKey);
        // YamlDotNet's container resolver has already turned an object-typed mapping into a dictionary.
        if (expectedType != typeof(Dictionary<object, object>) || reader.Current is not MappingStart start)
            return false;

        var buffer = new ParserBuffer(reader, maxDepth: 64, maxLength: 100_000);
        buffer.Consume<MappingStart>();
        Type? clauseType = null;
        while (clauseType is null && !buffer.TryConsume<MappingEnd>(out _))
        {
            if (buffer.TryConsume<Scalar>(out var key))
                JamlClauseBinding.ClauseTypeByKey.TryGetValue(key.Value, out clauseType);
            else
                buffer.SkipThisAndNestedEvents();
            buffer.SkipThisAndNestedEvents();
        }
        if (clauseType is null)
            throw new YamlException(start.Start, start.End, NeedsKey);

        buffer.Reset();
        value = nestedObjectDeserializer(buffer, clauseType);
        return true;
    }
}
