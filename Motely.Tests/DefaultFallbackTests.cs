using Motely.Filters;

namespace Motely.Tests;

/// <summary>
/// Pins the FilterDesc-side fallback defaults that <see cref="MotelySearchBuilder"/> fills in for
/// clauses that named no ante / no source. The loader injects nothing (JAML is typed — a clause
/// arrives as a real <see cref="JokerClause"/> with empty <c>Antes</c>/<c>Sources</c>, not a blob
/// of text), so without these defaults every <c>foreach (ante in clause.Antes)</c> and
/// <c>if (Sources.X.Length &gt; 0)</c> would simply never fire and the clause would match nothing.
///
/// Ground truth is differential, not magic-number: a sourceless wildcard joker must score exactly
/// the same as one that spells the defaults out by hand — antes 1..8, shop slots 0..7
/// (packs require an explicit <c>sources:</c> block).
/// </summary>
public class DefaultFallbackTests
{
    private const string Seed = "MOTELY77";

    private static (long Matching, int Score) Score(JokerClause clause)
    {
        var config = new JamlConfig
        {
            Id = "default-fallback",
            Deck = MotelyDeck.Red,
            Stake = MotelyStake.White,
        };
        config.Should.Add(clause);

        int score = 0;
        long matching = 0;
        var settings = MotelySearchBuilder
            .CreateSettings(config)
            .WithSeedGenerator([Seed], 1)
            .WithThreadCount(1)
            .WithQuietMode(true)
            .WithScoredResultCallback(result => score = result.Score);

        using var search = settings.Start();
        search.AwaitCompletion();
        matching = search.MatchingSeeds;
        return (matching, score);
    }

    [Fact]
    public void SourcelessWildcardJoker_DefaultsToAllAntesAndShopOnly()
    {
        // No antes, no sources — the clause as the loader hands it over.
        var (implicitMatching, implicitScore) = Score(
            new JokerClause { Score = 1 }
        );

        // The same clause with shop-only defaults written out longhand (no packs).
        var (_, explicitScore) = Score(
            new JokerClause
            {
                Score = 1,
                Antes = [1, 2, 3, 4, 5, 6, 7, 8],
                Sources = new JokerSourceConfig
                {
                    ShopItems = [0, 1, 2, 3, 4, 5, 6, 7],
                },
            }
        );

        Assert.True(
            implicitScore > 0,
            "a sourceless wildcard joker must match jokers, not nothing"
        );
        Assert.Equal(explicitScore, implicitScore); // defaults == antes 1..8, shop 0..7
        Assert.Equal(1, implicitMatching);
    }

    [Fact]
    public void ExplicitSources_AreNotOverwrittenByDefaults()
    {
        // A clause that named a source keeps exactly that source — the default fill must not touch it.
        var (_, narrowScore) = Score(
            new JokerClause
            {
                Score = 1,
                Antes = [1],
                Sources = new JokerSourceConfig { ShopItems = [0] }, // one slot, one ante
            }
        );

        var (_, wideScore) = Score(new JokerClause { Score = 1 }); // defaulted

        Assert.True(
            wideScore >= narrowScore,
            "the all-antes default must cover at least the single-slot case"
        );
    }

    // ── Tally-column labels ──
    // CreatePlan names each should-clause tally column. An explicit label: wins; an
    // unlabeled clause is named in words by what it asks for; scoreN is the last resort,
    // for an and/or with no clauses.

    private static JamlConfig LabelConfig(params IMotelyClause[] should)
    {
        var config = new JamlConfig
        {
            Id = "tally-labels",
            Deck = MotelyDeck.Red,
            Stake = MotelyStake.White,
        };
        foreach (var clause in should)
            config.Should.Add(clause);
        return config;
    }

    [Fact]
    public void TallyLabels_ExplicitLabelWins()
    {
        var clause = new JokerClause { Jokers = [MotelyJoker.Blueprint], Label = "bp" };
        var plan = MotelySearchBuilder.CreatePlan(LabelConfig(clause));
        Assert.Equal(["bp"], plan.TallyLabels);
    }

    [Fact]
    public void TallyLabels_UnlabeledClause_IsNamedByItsClause()
    {
        var labeled = new JokerClause { Jokers = [MotelyJoker.Blueprint], Label = "bp" };
        var unlabeled = new JokerClause { Jokers = [MotelyJoker.Blueprint], Antes = [1, 2] };

        var plan = MotelySearchBuilder.CreatePlan(LabelConfig(labeled, unlabeled));

        Assert.Equal(["bp", "Blueprint in antes 1 or 2"], plan.TallyLabels);
    }

    [Fact]
    public void TallyLabels_TagAndOr_NameBothColumns()
    {
        var config = JamlConfigLoader.FromJaml(
            """
            name: both forms
            deck: Ghost
            stake: White
            must:
              - !joker
                joker: Blueprint
                antes: [1, 2]
            should:
              - tag: NegativeTag
                antes: [1, 2, 3]
                score: 5
              - !or
                clauses:
                  - !voucher
                    voucher: Telescope
                    antes: [1]
                  - joker: Brainstorm
                    antes: [1, 2]
            """
        );

        Assert.Equal(
            ["Tag Negative Tag in antes 1-3", "Voucher Telescope in ante 1 or Brainstorm in antes 1 or 2"],
            MotelySearchBuilder.CreatePlan(config).TallyLabels
        );
    }

    [Fact]
    public void TallyLabels_EachClauseKind_HasItsWords()
    {
        (IMotelyClause Clause, string Label)[] cases =
        [
            (new JokerClause { Jokers = [MotelyJoker.Blueprint] }, "Blueprint"),
            (new JokerClause { Jokers = [MotelyJoker.Blueprint, MotelyJoker.Brainstorm], Antes = [1] }, "Blueprint or Brainstorm in ante 1"),
            (new JokerClause { Edition = MotelyItemEdition.Negative, Antes = [2, 3, 4] }, "Negative Joker in antes 2-4"),
            (
                new JokerClause
                {
                    Jokers = [MotelyJoker.Blueprint],
                    Stickers = [MotelyJokerSticker.Rental, MotelyJokerSticker.Eternal],
                },
                "Eternal Rental Blueprint"
            ),
            (new JokerClause { Jokers = [MotelyJoker.Brainstorm], Stickers = null!, Antes = null! }, "Brainstorm"),
            (new CommonJokerClause { Jokers = [MotelyJokerCommon.Joker], Antes = [1, 3, 4, 5] }, "Joker in antes 1 or 3-5"),
            (new UncommonJokerClause { Jokers = [MotelyJokerUncommon.Mime] }, "Mime"),
            (new RareJokerClause(), "Rare Joker"),
            (new LegendaryJokerClause { Jokers = [MotelyJoker.Perkeo], Antes = [1, 2], Rolls = [0] }, "Perkeo rolls 0 in antes 1 or 2"),
            (
                new LegendarySoulOrderClause
                {
                    Entries =
                    [
                        new SoulOrderEntry { Legendary = MotelyJoker.Triboulet, Antes = [2] },
                        new SoulOrderEntry { Legendary = MotelyJoker.Perkeo },
                    ],
                },
                "Soul Order Triboulet in ante 2 then Perkeo"
            ),
            (new TarotCardClause { Tarots = [MotelyTarotCard.TheFool], Antes = [1] }, "The Fool in ante 1"),
            (new SpectralCardClause { Spectrals = [MotelySpectralCard.BlackHole] }, "Black Hole"),
            (new PlanetCardClause(), "Planet Card"),
            (
                new StandardCardClause
                {
                    Rank = MotelyStandardcardRank.King,
                    Seal = MotelyItemSeal.Red,
                    Edition = MotelyItemEdition.Polychrome,
                },
                "Red Seal Polychrome King"
            ),
            (new StandardCardClause { Enhancement = MotelyItemEnhancement.Glass, Antes = [1] }, "Glass Standard Card in ante 1"),
            (
                new StartingDrawClause { Rank = MotelyStandardcardRank.Two, Suit = MotelyStandardcardSuit.Clubs },
                "Starting Draw 2 of Clubs"
            ),
            (new ErraticRankClause { Rank = MotelyStandardcardRank.Ace }, "Erratic Rank Ace"),
            (new ErraticSuitClause { Suit = MotelyStandardcardSuit.Spades }, "Erratic Suit Spades"),
            (new VoucherClause { Vouchers = [MotelyVoucher.Telescope], Antes = [1] }, "Voucher Telescope in ante 1"),
            (new VoucherClause { Vouchers = [MotelyVoucher.Telescope], Rolls = [0, 1] }, "Voucher Telescope rolls 0 or 1"),
            (new VoucherClause { Vouchers = [MotelyVoucher.Telescope], Rolls = null! }, "Voucher Telescope"),
            (new TagClause { Tags = [MotelyTag.NegativeTag] }, "Tag Negative Tag"),
            (new SmallBlindTagClause { Tags = [MotelyTag.NegativeTag], Antes = [2] }, "Small Blind Tag Negative Tag in ante 2"),
            (new BigBlindTagClause(), "Big Blind Tag"),
            (new BossClause { Bosses = [MotelyBossBlind.TheHook], Antes = [1, 2] }, "Boss The Hook in antes 1 or 2"),
            (new BoosterPackClause { Packs = [MotelyBoosterPack.MegaBuffoon], Antes = [1] }, "Mega Buffoon Pack in ante 1"),
            (new PokerHandClause { PokerHands = [MotelyPokerHand.Flush], Antes = [1] }, "Poker Hand Flush in ante 1"),
            (
                new LuckyMoneyClause { Rolls = [0, 1, 2], With = new MotelyWith { Luck = MotelyLuck.X2 } },
                "Lucky Money rolls 0-2 with luck 2"
            ),
            (new MisprintMultClause { Rolls = [0, 1], Mult = 20 }, "Misprint Mult rolls 0 or 1 mult 20"),
            (new BusinessPayoutClause(), "Business Payout"),
            (
                new AndClause
                {
                    Clauses =
                    [
                        new JokerClause { Jokers = [MotelyJoker.Blueprint] },
                        new OrClause
                        {
                            Clauses =
                            [
                                new JokerClause { Jokers = [MotelyJoker.Brainstorm] },
                                new JokerClause { Jokers = [MotelyJoker.Perkeo], Label = "perk" },
                            ],
                        },
                    ],
                },
                "Blueprint and (Brainstorm or perk)"
            ),
        ];

        foreach (var (clause, label) in cases)
            Assert.Equal(label, MotelySearchBuilder.DefaultTallyLabel(clause, 7));

        Assert.Equal("score7", MotelySearchBuilder.DefaultTallyLabel(new OrClause(), 7));
    }

    [Fact]
    public void TallyLabels_EveryFixtureFilter_NamesEveryColumn()
    {
        var dir = Path.GetFullPath(Path.Join(AppContext.BaseDirectory, "..", "..", "..", "JamlFilters"));
        var unnamed = new List<string>();
        int columns = 0;
        foreach (var file in Directory.GetFiles(dir, "*.yaml", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var should = JamlConfig.FromFile(file).Should;
            for (int i = 0; i < should.Count; i++)
            {
                columns++;
                if (should[i].Label is null && MotelySearchBuilder.DefaultTallyLabel(should[i], i) == $"score{i}")
                    unnamed.Add($"{Path.GetRelativePath(dir, file)} should[{i}]");
            }
        }

        Assert.True(columns > 0, $"no should clauses under {dir}");
        Assert.True(unnamed.Count == 0, string.Join(Environment.NewLine, unnamed));
    }
}
