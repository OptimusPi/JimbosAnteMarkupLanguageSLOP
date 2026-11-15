using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Motely.Filters;

/// <summary>Event-clause roll shorthand: <c>luckyMoney: 1</c>, <c>luckyMoney: []</c>, or a list.</summary>
[YamlSerializable]
public sealed class JamlEventRolls
{
    public int[] Rolls { get; set; } = [];

    public int[] ToArray() => Rolls;

    internal sealed class Converter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(JamlEventRolls);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer nested)
        {
            if (parser.Current is SequenceStart)
                return new JamlEventRolls { Rolls = ReadIntSequence(parser) };

            if (parser.Current is Scalar scalar)
            {
                parser.Consume<Scalar>();
                if (IsEmpty(scalar))
                    return new JamlEventRolls { Rolls = [] };
                return new JamlEventRolls { Rolls = [ReadRoll(scalar)] };
            }

            return new JamlEventRolls { Rolls = [] };
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer nested) =>
            nested(value, type);

        private static int[] ReadIntSequence(IParser parser)
        {
            parser.Consume<SequenceStart>();
            var items = new List<int>();
            while (!parser.TryConsume<SequenceEnd>(out _))
                items.Add(ReadRoll(parser.Consume<Scalar>()));

            return items.ToArray();
        }

        private static int ReadRoll(Scalar scalar)
        {
            if (!int.TryParse(scalar.Value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var roll))
                throw new YamlException(scalar.Start, scalar.End, $"`{scalar.Value}` is not a roll: write a whole number, or a list of them, as in [1, 2].");
            if (roll is < 0 or > MotelyGlobals.MaxRollIndex)
                throw new YamlException(scalar.Start, scalar.End, $"`{roll}` is out of range: a roll runs from 0 to {MotelyGlobals.MaxRollIndex}.");
            return roll;
        }

        private static bool IsEmpty(Scalar scalar)
        {
            var text = scalar.Value.Trim();
            return text.Length == 0
                || text.Equals("any", StringComparison.OrdinalIgnoreCase)
                || text.Equals("~", StringComparison.OrdinalIgnoreCase)
                || text.Equals("null", StringComparison.OrdinalIgnoreCase);
        }
    }
}
