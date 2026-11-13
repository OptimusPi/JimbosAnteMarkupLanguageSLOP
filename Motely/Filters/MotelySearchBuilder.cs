using System;
using System.Linq;

namespace Motely.Filters;

public sealed class JamlSearchPlan
{
    public IMotelySearchSettings Settings { get; set; } = null!;
    public int ScoreTallyColumnCount { get; set; }
    public IReadOnlyList<string> TallyLabels { get; set; } = [];
}

public static class MotelySearchBuilder
{
    /// <inheritdoc cref="CreateSettings"/>
    public static JamlSearchPlan CreatePlan(JamlConfig config, int? engineCutoff = null)
    {
        var settings = CreateSettings(config, engineCutoff);
        return new JamlSearchPlan
        {
            Settings = settings,
            ScoreTallyColumnCount = config.Should.Count,
            TallyLabels = [.. config.Should.Select(DefaultTallyLabel)],
        };
    }

    /// <summary>
    /// The tally-column label for a should clause: the author's <c>label:</c> when given, otherwise
    /// the clause in words ("Blueprint in antes 1 or 2"). "score{index}" is left for an
    /// <c>and</c>/<c>or</c> with no clauses, and for a clause type <see cref="ClauseName"/> has no arm for.
    /// </summary>
    public static string DefaultTallyLabel(IMotelyClause clause, int index) =>
        clause.Label ?? ClauseName(clause) ?? $"score{index}";

    private static string? ClauseName(IMotelyClause clause) =>
        clause switch
        {
            JokerClause c => Joker(c.Stickers, c.Edition, Names(c.Jokers) ?? "Joker", c.Antes),
            CommonJokerClause c => Joker(c.Stickers, c.Edition, Names(c.Jokers) ?? "Common Joker", c.Antes),
            UncommonJokerClause c => Joker(c.Stickers, c.Edition, Names(c.Jokers) ?? "Uncommon Joker", c.Antes),
            RareJokerClause c => Joker(c.Stickers, c.Edition, Names(c.Jokers) ?? "Rare Joker", c.Antes),
            LegendaryJokerClause c => Words(
                Written(c.Edition),
                Names(c.Jokers) ?? "Legendary Joker",
                Rolls(c.Rolls, []),
                Antes(c.Antes)
            ),
            LegendarySoulOrderClause c => Words(
                "Soul Order",
                string.Join(" then ", c.Entries.Select(static e => Words(Display(e.Legendary), Antes(e.Antes))))
            ),
            TarotCardClause c => Words(Names(c.Tarots) ?? "Tarot Card", Antes(c.Antes)),
            SpectralCardClause c => Words(Names(c.Spectrals) ?? "Spectral Card", Antes(c.Antes)),
            PlanetCardClause c => Words(Names(c.Planets) ?? "Planet Card", Antes(c.Antes)),
            StandardCardClause c => Words(
                Written(c.Seal) is { } seal ? seal + " Seal" : null,
                Written(c.Edition),
                Written(c.Enhancement),
                Card(c.Rank, c.Suit) ?? "Standard Card",
                Antes(c.Antes)
            ),
            StartingDrawClause c => Words("Starting Draw", Card(c.Rank, c.Suit), Antes(c.Antes)),
            ErraticRankClause c => Words("Erratic Rank", Rank(c.Rank), Antes(c.Antes)),
            ErraticSuitClause c => Words("Erratic Suit", c.Suit.ToString(), Antes(c.Antes)),
            VoucherClause c => Words(
                "Voucher",
                Names(c.Vouchers, FormatUtils.FormatVoucher),
                Rolls(c.Rolls, [0]),
                Antes(c.Antes)
            ),
            TagClause { Rolls: [0] } c => Words("Small Blind Tag", Names(c.Tags, FormatUtils.FormatTag), Antes(c.Antes)),
            TagClause { Rolls: [1] } c => Words("Big Blind Tag", Names(c.Tags, FormatUtils.FormatTag), Antes(c.Antes)),
            TagClause c => Words("Tag", Names(c.Tags, FormatUtils.FormatTag), Rolls(c.Rolls, [0, 1]), Antes(c.Antes)),
            BossClause c => Words("Boss", Names(c.Bosses, FormatUtils.FormatBoss), Antes(c.Antes)),
            BoosterPackClause c => Words(
                Names(c.Packs, FormatUtils.FormatPackName) ?? "Booster Pack",
                Rolls(c.Rolls, [0, 1]),
                Antes(c.Antes)
            ),
            PokerHandClause c => Words(
                "Poker Hand",
                Names(c.PokerHands, static hand => hand.ToString()),
                Rolls(c.Rolls, [0]),
                Antes(c.Antes)
            ),
            LuckyMoneyClause c => Event("Lucky Money", c),
            LuckyMultClause c => Event("Lucky Mult", c),
            MisprintMultClause c => Words(Event("Misprint Mult", c), $"mult {c.Mult}"),
            WheelOfFortuneClause c => Event("Wheel of Fortune", c),
            WheelStaysFlippedClause c => Event("Wheel Stays Flipped", c),
            GlassDestroyClause c => Event("Glass Destroy", c),
            CavendishExtinctClause c => Event("Cavendish Extinct", c),
            GrosMichelExtinctClause c => Event("Gros Michel Extinct", c),
            SpaceLevelupClause c => Event("Space Levelup", c),
            BusinessPayoutClause c => Event("Business Payout", c),
            ParkingPayoutClause c => Event("Parking Payout", c),
            BloodstoneTriggerClause c => Event("Bloodstone Trigger", c),
            LogicClause c => Logic(c),
            _ => null,
        };

    private static string Joker(MotelyJokerSticker[]? stickers, MotelyItemEdition? edition, string names, int[] antes) =>
        Words(
            string.Join(' ', (stickers ?? []).Where(static s => s != MotelyJokerSticker.None).Distinct().Order()),
            Written(edition),
            names,
            Antes(antes)
        );

    private static string Event(string name, EventClause clause) =>
        Words(
            name,
            Rolls(clause.Rolls, []),
            clause is LuckEventClause { With.Luck: not MotelyLuck.X1 and var luck } ? $"with luck {(int)luck}" : null
        );

    private static string? Logic(LogicClause clause)
    {
        List<string> names = [];
        foreach (var child in clause.Clauses)
        {
            if ((child.Label ?? ClauseName(child)) is not { } name)
                return null;
            names.Add(child is LogicClause { Label: null } ? $"({name})" : name);
        }
        return names.Count == 0 ? null : string.Join(clause is AndClause ? " and " : " or ", names);
    }

    private static string? Card(MotelyStandardcardRank? rank, MotelyStandardcardSuit? suit) =>
        rank is null && suit is null
            ? null
            : Words(rank is { } r ? Rank(r) : null, rank is not null && suit is not null ? "of" : null, suit?.ToString());

    private static string Rank(MotelyStandardcardRank rank) =>
        rank <= MotelyStandardcardRank.Ten ? ((int)rank + 2).ToString() : rank.ToString();

    private static string Display<T>(T value)
        where T : struct, Enum => FormatUtils.FormatDisplayName(value.ToString());

    private static string? Names<T>(T[] values)
        where T : struct, Enum => Names(values, Display);

    private static string? Names<T>(T[] values, Func<T, string> name) =>
        values.Length == 0 ? null : string.Join(" or ", values.Select(name));

    private static string? Written<T>(T? value)
        where T : struct, Enum =>
        value is { } v && !EqualityComparer<T>.Default.Equals(v, default) ? v.ToString() : null;

    private static string? Antes(int[]? antes) =>
        antes is not { Length: > 0 } || antes.AsSpan().SequenceEqual([1, 2, 3, 4, 5, 6, 7, 8]) ? null
        : antes.Length == 1 ? $"in ante {antes[0]}"
        : "in antes " + Numbers(antes);

    private static string? Rolls(int[]? rolls, ReadOnlySpan<int> unwritten) =>
        rolls is not { Length: > 0 } || rolls.AsSpan().SequenceEqual(unwritten) ? null : "rolls " + Numbers(rolls);

    private static string Numbers(int[] values)
    {
        List<string> runs = [];
        for (int i = 0; i < values.Length; )
        {
            int start = i;
            while (i + 1 < values.Length && values[i + 1] == values[i] + 1)
                i++;
            runs.Add(
                i == start ? $"{values[start]}"
                : i == start + 1 ? $"{values[start]} or {values[i]}"
                : $"{values[start]}-{values[i]}"
            );
            i++;
        }
        return string.Join(" or ", runs);
    }

    private static string Words(params string?[] parts) =>
        string.Join(' ', parts.Where(static part => !string.IsNullOrEmpty(part)));

    /// <summary>
    /// The engine's search settings for a filter: its deck and stake, its must and exact mustNot
    /// clauses as the SIMD chain, and the scoring pass when anything scores.
    /// </summary>
    /// <param name="engineCutoff">
    /// The score floor the scorer drops seeds below, before any callback. Null takes the filter's
    /// own <c>cutoff:</c>, or no floor when it has none; <see cref="int.MinValue"/> is no floor
    /// whatever the filter says (a host whose own cutoff mode wins, as the CLI's --cutoff does).
    /// </param>
    public static IMotelySearchSettings CreateSettings(JamlConfig config, int? engineCutoff = null)
    {
        int floor = engineCutoff ?? config.Cutoff ?? int.MinValue;

        // A JAML with no must/should/mustNot clauses is a valid, real search: deck/stake/seeds
        // and nothing else, with the host's own predicate free to drive the whole decision.
        IMotelySearchSettings settings =
            new MotelySearchSettings<PassthroughFilterDesc.PassthroughFilter>(
                new PassthroughFilterDesc()
            );

        // The JAML document names its own deck and stake; the settings carry them from here so
        // every caller searches what the filter says. Callers may still override afterwards
        // (the CLI applies --deck/--stake on top).
        settings = settings.WithDeck(config.Deck).WithStake(config.Stake);

        // A mustNot may only be negated in SIMD when its prefilter is the exact match law: a
        // coarse prefilter passes every lane that might hold the item, so its negation would
        // drop those seeds (a bare legendaryJoker passes all lanes and would reject everything).
        // Coarse mustNot clauses skip the chain and are rejected by the scalar scoring pass.
        var simdMustNot = config.MustNot.Where(ClauseScoring.IsExactFilterConfirm).ToArray();
        var scoredMustNot = config.MustNot.Where(c => !ClauseScoring.IsExactFilterConfirm(c)).ToArray();

        // SIMD filter chain: must + mustNot cheapest-first so trivial clauses reject lanes before
        // expensive SearchIndividualSeeds arms. Does not mutate the config; scoring still sees
        // authored must/should order (tally columns + must re-eval). A must that zero occurrences
        // satisfy (min: 0) stays out: the scoring pass re-evaluates every must, so it still holds.
        foreach (
            var (clause, negate) in config
                .Must.Where(ClauseScoring.CanChainInSimd)
                .Select(c => (clause: c, negate: false))
                .Concat(simdMustNot.Select(c => (clause: c, negate: true)))
        )
        {
            var desc = clause.CreateFilterDesc();
            settings = settings.WithAdditionalFilter(
                negate ? new NegationFilterDesc(desc) : desc
            );
        }

        if (config.Must.Count + config.Should.Count + scoredMustNot.Length > 0)
        {
            settings = settings.WithSeedScoreProvider(
                new JamlShouldScoreDesc(
                    [.. config.Must],
                    [.. config.Should],
                    minimumTotalScore: floor,
                    mustNotClauses: scoredMustNot
                )
            );
        }

        return settings;
    }
}
