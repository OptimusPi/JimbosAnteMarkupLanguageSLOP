using System.Collections;
using Motely.Analysis;
using Motely.Filters;

namespace Motely.Tests;

/// <summary>
/// The scroll contract: two half windows, the second resumed from the first's StreamStates, hold
/// exactly what one full window holds. Every queue the Jamlyzer returns is checked by reflection,
/// so a stream added later is covered without touching this file.
/// </summary>
public sealed class JamlyzerResumeEqualsOnePassTests
{
    [Theory]
    [InlineData("ALEEB", "Red")]
    [InlineData("PIROCKS", "Ghost")]
    [InlineData("A", "Red")]
    [InlineData("12345678", "Anaglyph")]
    [InlineData("7LB2WVPK", "Erratic")]
    [InlineData("S4DM5V37", "Red")]
    public void TwoResumedHalves_EqualOneFullWindow(string seed, string deck)
    {
        const int Half = 12;
        var config = JamlConfig.FromJaml($"deck: {deck}\nseeds: [\"{seed}\"]\n");

        var full = Assert.Single(MotelyJamlyzer.Analyze(config, eventRolls: Half * 2, shopSlots: Half * 2));
        var first = Assert.Single(MotelyJamlyzer.Analyze(config, eventRolls: Half, shopSlots: Half));
        var second = Assert.Single(
            MotelyJamlyzer.Analyze(config, first.StreamStates, eventRolls: Half, shopSlots: Half)
        );

        AssertQueuesJoin(full.Events, first.Events, second.Events, "Events");
        Assert.Equal(full.Antes.Count, first.Antes.Count);
        Assert.Equal(full.Antes.Count, second.Antes.Count);
        for (int i = 0; i < full.Antes.Count; i++)
        {
            string at = $"ante {full.Antes[i].Ante}";
            AssertJoin(full.Antes[i].ShopItems, first.Antes[i].ShopItems, second.Antes[i].ShopItems, $"{at} ShopItems");
            AssertQueuesJoin(full.Antes[i].Pulls, first.Antes[i].Pulls, second.Antes[i].Pulls, $"{at} Pulls");
            AssertQueuesJoin(full.Antes[i].ShopStreams, first.Antes[i].ShopStreams, second.Antes[i].ShopStreams, $"{at} ShopStreams");
        }

        // The bag after two halves is the bag after one full window: a third page starts the same
        // either way. Every stream's typed state is in this comparison, shop included, since both
        // routes read the same 24 shop items.
        Assert.Equal(full.StreamStates, second.StreamStates);
        Assert.Equal(full.StreamStates.RollOffset, second.StreamStates.RollOffset);
        Assert.Equal(full.StreamStates.ShopOffset, second.StreamStates.ShopOffset);
        foreach (var p in typeof(MotelyJamlyzerStreamStates).GetProperties().Where(p => p.PropertyType == typeof(double)))
            Assert.Equal(
                BitConverter.DoubleToInt64Bits((double)p.GetValue(full.StreamStates)!),
                BitConverter.DoubleToInt64Bits((double)p.GetValue(second.StreamStates)!)
            );
    }

    private static void AssertQueuesJoin<T>(T full, T first, T second, string where)
    {
        var queues = typeof(T).GetProperties().Where(p => typeof(IEnumerable).IsAssignableFrom(p.PropertyType)).ToArray();
        Assert.NotEmpty(queues);
        foreach (var p in queues)
            AssertJoin((IEnumerable)p.GetValue(full)!, (IEnumerable)p.GetValue(first)!, (IEnumerable)p.GetValue(second)!, $"{where}.{p.Name}");
    }

    private static void AssertJoin(IEnumerable full, IEnumerable first, IEnumerable second, string where)
    {
        var expected = full.Cast<object>().ToArray();
        var joined = first.Cast<object>().Concat(second.Cast<object>()).ToArray();
        Assert.True(expected.Length > 0, $"{where}: the full window is empty");
        Assert.True(
            expected.SequenceEqual(joined),
            $"{where}: two resumed halves differ from one window.\n full:   {string.Join(",", expected)}\n joined: {string.Join(",", joined)}"
        );
    }
}
