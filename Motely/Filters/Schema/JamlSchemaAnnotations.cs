namespace Motely.Filters;

/// <summary>
/// The one place the generated files' human text lives: a description per type (member
/// <c>""</c>) and per member. The owner may be the type itself, a base class, an interface, or
/// <see cref="object"/> for a member that means the same wherever it is written; the closest
/// owner wins. The model names every member without a description and every entry nothing uses
/// (<see cref="JamlSchemaModel.MissingDescriptions"/>, <see cref="JamlSchemaModel.OrphanDescriptions"/>),
/// and the generator refuses to run while either list holds anything.
/// </summary>
internal static class JamlSchemaAnnotations
{
    private static readonly Type Any = typeof(object);

    internal static readonly Dictionary<(Type Owner, string Member), string> Text = new()
    {
        // ── the filter ──
        [(typeof(JamlConfig), "")] = "A Motely seed filter: the keys, enums and nesting the loader binds.",
        [(typeof(JamlConfig), "id")] = "The filter's identifier, for tools that store filters.",
        [(typeof(JamlConfig), "name")] = "The filter's name.",
        [(typeof(JamlConfig), "description")] = "What the filter looks for, in the author's words.",
        [(typeof(JamlConfig), "author")] = "Who wrote the filter.",
        [(typeof(JamlConfig), "dateCreated")] = "When the filter was written. Metadata: the search never reads it.",
        [(typeof(JamlConfig), "deck")] = "The deck every seed is played with. Red when not written.",
        [(typeof(JamlConfig), "stake")] = "The stake every seed is played at. White when not written.",
        [(typeof(JamlConfig), "seeds")] = "Seeds kept with the filter, for replaying it.",
        [(typeof(JamlConfig), "cutoff")] =
            "A fixed score floor: only seeds that score at least this are reported. Without it the host chooses the cutoff.",
        [(typeof(JamlConfig), "must")] = "Clauses a seed has to match.",
        [(typeof(JamlConfig), "should")] = "Clauses that score a seed. A seed that matches none still passes.",
        [(typeof(JamlConfig), "mustNot")] = "Clauses a seed has to not match.",

        // ── on every clause ──
        [(typeof(IMotelyClause), "label")] = "The clause's column name in results. Derived from the clause when not written.",
        [(typeof(IMotelyClause), "min")] = "The fewest occurrences that count as a match. 1 when not written.",
        [(typeof(IMotelyClause), "max")] = "The most occurrences that still count as a match. No limit when not written.",
        [(typeof(IMotelyClause), "score")] = "The clause's weight in a seed's score under should:. 1 when not written.",
        [(typeof(IAnteScopedClause), "antes")] = "The antes the clause reads. Antes 1 to 8 when not written.",
        [(typeof(IAnteScopedClause), "ante")] = "Another spelling of antes: one ante or a list of them.",
        [(typeof(IWithScopedClause), "with")] = "What the player holds while this clause is read.",

        // ── with ──
        [(typeof(MotelyWith), "")] =
            "What the player holds while a clause is read. Each clause reads the keys that change it and ignores the others.",
        [(typeof(MotelyWith), "jokers")] =
            "Jokers the player holds. A held legendary is out of the Soul's pool; holding Showman keeps repeats in.",
        [(typeof(MotelyWith), "luck")] =
            "The Oops! All 6s multiplier on an event's odds, as a name (X1, X2, X4, ...) or its number.",
        [(typeof(MotelyWith), "vouchers")] =
            "Vouchers the player has redeemed, on top of the deck's own. The shop the clause reads is rolled with them.",

        // ── sources ──
        [(typeof(JokerSourceConfig), "")] =
            "Where a joker clause looks. Written, it replaces the clause's default sources as a whole.",
        [(typeof(LegendaryJokerSourceConfig), "")] =
            "Which packs a legendary joker clause opens Souls from. Written, it replaces the clause's default sources as a whole.",
        [(typeof(PlanetSourceConfig), "")] =
            "Where a planetCard clause looks. Written, it replaces the clause's default sources as a whole.",
        [(typeof(SpectralCardSourceConfig), "")] =
            "Where a spectralCard clause looks. Written, it replaces the clause's default sources as a whole.",
        [(typeof(TarotCardSourceConfig), "")] =
            "Where a tarotCard clause looks. Written, it replaces the clause's default sources as a whole.",
        [(typeof(StandardCardSourceConfig), "")] =
            "Where a standardCard clause looks. Written, it replaces the clause's default sources as a whole.",
        [(Any, "shopItems")] = "Shop slots to read in each ante, counted from 0 across rerolls.",
        [(Any, "boosterPacks")] = "Booster pack offers to read in each ante, counted from 0.",
        [(Any, "requireMegaPack")] = "When true, only Mega packs count.",
        [(Any, "requireMega")] = "Another spelling of requireMegaPack.",
        [(typeof(JokerSourceConfig), "judgement")] = "Rolls of the joker stream the Judgement tarot draws from.",
        [(typeof(JokerSourceConfig), "wraith")] = "Rolls of the joker stream the Wraith spectral draws from (always Rare).",
        [(typeof(JokerSourceConfig), "riffRaff")] = "Rolls of the common joker stream Riff-Raff draws from.",
        [(typeof(JokerSourceConfig), "rareTag")] = "Rolls of the Rare Tag's joker stream.",
        [(typeof(JokerSourceConfig), "uncommonTag")] = "Rolls of the Uncommon Tag's joker stream.",
        [(typeof(JokerSourceConfig), "commonShopJokers")] = "Rolls of the shop's common joker stream alone.",
        [(typeof(JokerSourceConfig), "uncommonShopJokers")] =
            "Rolls of the shop's uncommon joker stream alone. Not the same numbers as shopItems when slots mix item types.",
        [(typeof(JokerSourceConfig), "rareShopJokers")] = "Rolls of the shop's rare joker stream alone.",
        [(typeof(JokerSourceConfig), "allShopJokers")] = "Rolls of the shop's joker stream across rarities.",
        [(typeof(LegendaryJokerSourceConfig), "boosterPacks")] =
            "Pack offers where The Soul counts from either an Arcana or a Spectral pack. Not read for slots when arcanaPacks or spectralPacks is written.",
        [(typeof(LegendaryJokerSourceConfig), "arcanaPacks")] = "Pack offers to read on the Arcana pack path only.",
        [(typeof(LegendaryJokerSourceConfig), "spectralPacks")] = "Pack offers to read on the Spectral pack path only.",
        [(Any, "tagPacks")] =
            "Charm Tag Mega Arcana packs, opened at blind select before the ante's shop: 0 is the small blind's tag, 1 the big blind's.",
        [(typeof(SpectralCardSourceConfig), "sixthSense")] = "Rolls of the spectral stream Sixth Sense draws from.",
        [(typeof(SpectralCardSourceConfig), "seance")] = "Rolls of the spectral stream Seance draws from.",
        [(typeof(SpectralCardSourceConfig), "etherealTag")] = "When true, the Ethereal Tag's bonus Spectral pack counts.",
        [(typeof(SpectralCardSourceConfig), "omenGlobe")] =
            "When true, spectral cards that Omen Globe puts in Arcana packs count. Assumes Omen Globe is owned.",
        [(typeof(TarotCardSourceConfig), "emperor")] = "Rolls of the tarot stream The Emperor draws from.",
        [(typeof(TarotCardSourceConfig), "purpleSealOrEightBall")] =
            "Rolls of the tarot stream a Purple Seal or 8 Ball draws from.",
        [(typeof(TarotCardSourceConfig), "charmTag")] = "When true, the Charm Tag's bonus Arcana pack counts.",
        [(Any, "sources")] = "Where the clause looks. Written, it replaces the clause's default sources as a whole.",

        // ── jokers ──
        [(typeof(JokerClause), "")] =
            "A joker of any rarity. Naming a legendary, or no joker at all, also counts the legendaries a Soul gives.",
        [(typeof(CommonJokerClause), "")] = "A common joker.",
        [(typeof(UncommonJokerClause), "")] = "An uncommon joker.",
        [(typeof(RareJokerClause), "")] = "A rare joker.",
        [(typeof(LegendaryJokerClause), "")] = "The legendary joker a Soul card gives.",
        [(typeof(JokerClause), "joker")] = "The jokers that count: one name, a list, or Any.",
        [(typeof(CommonJokerClause), "commonJoker")] = "The common jokers that count: one name, a list, or Any.",
        [(typeof(CommonJokerClause), "commonJokers")] = "Another spelling of commonJoker.",
        [(typeof(UncommonJokerClause), "uncommonJoker")] = "The uncommon jokers that count: one name, a list, or Any.",
        [(typeof(UncommonJokerClause), "uncommonJokers")] = "Another spelling of uncommonJoker.",
        [(typeof(RareJokerClause), "rareJoker")] = "The rare jokers that count: one name, a list, or Any.",
        [(typeof(RareJokerClause), "rareJokers")] = "Another spelling of rareJoker.",
        [(typeof(LegendaryJokerClause), "legendaryJoker")] = "The legendaries that count: one name, a list, or Any.",
        [(typeof(LegendaryJokerClause), "legendaryJokers")] = "Another spelling of legendaryJoker.",
        [(typeof(LegendaryJokerClause), "soulJoker")] = "Another spelling of legendaryJoker.",
        [(Any, "jokers")] = "Another spelling of the clause's own key: the jokers that count.",
        [(Any, "edition")] = "The edition the joker or card must have.",
        [(Any, "stickers")] = "Stickers the joker must carry: one name, a list, or Any.",
        [(typeof(JokerClause), "legendarySources")] =
            "Where the clause looks for the legendaries it names, as sources does on a legendaryJoker clause.",
        [(typeof(LegendaryJokerClause), "rolls")] =
            "Which opened Souls count: 0 is the first Soul the run opens, 1 the second. Every opened Soul when not written.",
        [(typeof(LegendaryJokerClause), "soulCardOnly")] =
            "When true, The Soul appearing in a pack the clause reads is a match, whatever legendary it gives.",
        [(typeof(LegendaryJokerClause), "soulEditionRolls")] =
            "Extra edition reads per ante for the fast edition check. 0 uses the number of pack offers the clause reads.",

        // ── soulOrder ──
        [(typeof(LegendarySoulOrderClause), "")] =
            "The order in which the Souls a run opens turn into legendaries, across the whole run.",
        [(typeof(LegendarySoulOrderClause), "soulOrder")] =
            "The Souls in the order the run opens them: the first entry is the first Soul opened.",
        [(typeof(LegendarySoulOrderClause), "soulOrders")] = "Another spelling of soulOrder.",
        [(typeof(LegendarySoulOrderClause), "entries")] = "The list soulOrder fills, written directly.",
        [(typeof(LegendarySoulOrderClause), "maxAnte")] = "How far the walk runs: later antes are not read. 8 when not written.",
        [(typeof(LegendarySoulOrderClause), "boosterPacks")] =
            "Booster pack offers the walk reads Souls from. Offers 0 to 5 when not written.",
        [(typeof(SoulOrderEntry), "")] = "One Soul the run opens: the legendary it gives, and the antes it may sit in.",
        [(typeof(SoulOrderEntry), "legendary")] = "The legendary this Soul gives.",
        [(typeof(SoulOrderEntry), "antes")] = "The antes this Soul may sit in. Any ante when not written.",

        // ── consumables ──
        [(typeof(PlanetCardClause), "")] = "A planet card.",
        [(typeof(PlanetCardClause), "planetCard")] = "The planet cards that count: one name, a list, or Any.",
        [(typeof(PlanetCardClause), "planetCards")] = "Another spelling of planetCard.",
        [(typeof(PlanetCardClause), "planets")] = "Another spelling of planetCard.",
        [(typeof(SpectralCardClause), "")] = "A spectral card.",
        [(typeof(SpectralCardClause), "spectralCard")] = "The spectral cards that count: one name, a list, or Any.",
        [(typeof(SpectralCardClause), "spectralCards")] = "Another spelling of spectralCard.",
        [(typeof(SpectralCardClause), "spectrals")] = "Another spelling of spectralCard.",
        [(typeof(TarotCardClause), "")] = "A tarot card.",
        [(typeof(TarotCardClause), "tarotCard")] = "The tarot cards that count: one name, a list, or Any.",
        [(typeof(TarotCardClause), "tarotCards")] = "Another spelling of tarotCard.",
        [(typeof(TarotCardClause), "tarots")] = "Another spelling of tarotCard.",

        // ── playing cards ──
        [(typeof(StandardCardClause), "")] = "A playing card in a Standard pack or the shop.",
        [(typeof(StandardCardClause), "standardCard")] =
            "The card that counts: a whole card name, Any, or a mapping of rank, suit, enhancement, seal and edition.",
        [(typeof(StandardCardClause), "standardCards")] = "Another spelling of standardCard.",
        [(typeof(JamlStandardCardSlot), "")] = "A playing card described by its parts. A part not written matches any.",
        [(Any, "card")] = "A whole card (AceOfSpades): its rank and suit at once.",
        [(Any, "rank")] = "The rank the card must have. J, Q, K and A are read as Jack, Queen, King and Ace.",
        [(Any, "suit")] = "The suit the card must have.",
        [(Any, "enhancement")] = "The enhancement the card must have.",
        [(Any, "seal")] = "The seal the card must have.",
        [(typeof(StartingDrawClause), "")] = "A card in the hand an ante starts with.",
        [(typeof(StartingDrawClause), "startingDraw")] = "The card that counts, as a whole card name.",
        [(typeof(ErraticRankClause), "")] = "How many cards of one rank the starting deck holds. Varies by seed on the Erratic deck.",
        [(typeof(ErraticRankClause), "erraticRank")] = "The rank to count.",
        [(typeof(ErraticRankClause), "erraticRanks")] = "Another spelling of erraticRank.",
        [(typeof(ErraticRankClause), "rank")] = "The rank to count. J, Q, K and A are read as Jack, Queen, King and Ace.",
        [(typeof(ErraticSuitClause), "")] = "How many cards of one suit the starting deck holds. Varies by seed on the Erratic deck.",
        [(typeof(ErraticSuitClause), "erraticSuit")] = "The suit to count.",
        [(typeof(ErraticSuitClause), "erraticSuits")] = "Another spelling of erraticSuit.",
        [(typeof(ErraticSuitClause), "suit")] = "The suit to count.",

        // ── ante features ──
        [(typeof(VoucherClause), "")] = "A voucher an ante offers.",
        [(typeof(VoucherClause), "voucher")] = "The vouchers that count: one name, a list, or Any.",
        [(typeof(VoucherClause), "vouchers")] = "Another spelling of voucher.",
        [(typeof(VoucherClause), "rolls")] =
            "Draws of the ante's voucher stream: 0 is the voucher the ante offers, 1 and up are further draws. [0] when not written.",
        [(typeof(TagClause), "")] = "A skip tag an ante offers.",
        [(typeof(SmallBlindTagClause), "")] = "The small blind's tag offer only.",
        [(typeof(BigBlindTagClause), "")] = "The big blind's tag offer only.",
        [(typeof(TagClause), "tag")] = "The tags that count: one name, a list, or Any.",
        [(typeof(TagClause), "tags")] = "Another spelling of tag.",
        [(typeof(SmallBlindTagClause), "smallBlindTag")] = "The tags that count: one name, a list, or Any.",
        [(typeof(BigBlindTagClause), "bigBlindTag")] = "The tags that count: one name, a list, or Any.",
        [(typeof(TagClause), "rolls")] =
            "Draws of the ante's tag stream: 0 is the small blind's offer, 1 the big blind's, 2 and up are further draws.",
        [(typeof(BossClause), "")] = "An ante's boss blind.",
        [(typeof(BossClause), "boss")] = "The bosses that count: one name, a list, or Any.",
        [(typeof(BossClause), "bosses")] = "Another spelling of boss.",
        [(typeof(BoosterPackClause), "")] = "A booster pack an ante's shop offers.",
        [(typeof(BoosterPackClause), "boosterPack")] = "The packs that count, by type and size: one name, a list, or Any.",
        [(typeof(BoosterPackClause), "boosterPacks")] = "Another spelling of boosterPack.",
        [(typeof(BoosterPackClause), "packs")] = "Another spelling of boosterPack.",
        [(typeof(BoosterPackClause), "rolls")] =
            "Pack offers to read in each ante: 0 is the first offer, 1 the second. [0, 1] when not written.",
        [(typeof(PokerHandClause), "")] = "The best poker hand in the hand a blind starts with.",
        [(typeof(PokerHandClause), "pokerHand")] = "The poker hands that count: one name, a list, or Any.",
        [(typeof(PokerHandClause), "pokerHands")] = "Another spelling of pokerHand.",
        [(typeof(PokerHandClause), "rolls")] =
            "Which blinds played in the ante to read: 0 is the first blind played, 1 the second, 2 the third. [0] when not written.",

        // ── events ──
        [(typeof(EventClause), "rolls")] =
            "The pulls of the event's own stream that count, counted from 0. The clause's own key writes the same list.",
        [(typeof(LuckyMoneyClause), "")] = "A Lucky card paying $20.",
        [(typeof(LuckyMoneyClause), "luckyMoney")] = "The pulls of the Lucky card money stream that count.",
        [(typeof(LuckyMultClause), "")] = "A Lucky card giving +20 Mult.",
        [(typeof(LuckyMultClause), "luckyMult")] = "The pulls of the Lucky card Mult stream that count.",
        [(typeof(MisprintMultClause), "")] = "Misprint rolling at least mult.",
        [(typeof(MisprintMultClause), "misprintMult")] = "The pulls of Misprint's stream that count.",
        [(typeof(MisprintMultClause), "mult")] = "The least Mult a pull must show to count (Misprint rolls 0 to 23).",
        [(typeof(WheelOfFortuneClause), "")] = "The Wheel of Fortune applying an edition.",
        [(typeof(WheelOfFortuneClause), "wheelOfFortune")] = "The pulls of The Wheel of Fortune's stream that count.",
        [(typeof(WheelStaysFlippedClause), "")] = "The Wheel boss blind leaving a card face down.",
        [(typeof(WheelStaysFlippedClause), "wheelStaysFlipped")] = "The pulls of The Wheel's stream that count.",
        [(typeof(GlassDestroyClause), "")] = "A Glass card shattering.",
        [(typeof(GlassDestroyClause), "glassDestroy")] = "The pulls of the Glass card stream that count.",
        [(typeof(CavendishExtinctClause), "")] = "Cavendish going extinct.",
        [(typeof(CavendishExtinctClause), "cavendishExtinct")] = "The pulls of Cavendish's stream that count.",
        [(typeof(GrosMichelExtinctClause), "")] = "Gros Michel going extinct.",
        [(typeof(GrosMichelExtinctClause), "grosMichelExtinct")] = "The pulls of Gros Michel's stream that count.",
        [(typeof(SpaceLevelupClause), "")] = "Space Joker levelling up the hand.",
        [(typeof(SpaceLevelupClause), "spaceLevelup")] = "The pulls of Space Joker's stream that count.",
        [(typeof(BusinessPayoutClause), "")] = "Business Card paying out. A flat coin flip: luck does not change it.",
        [(typeof(BusinessPayoutClause), "businessPayout")] = "The pulls of Business Card's stream that count.",
        [(typeof(ParkingPayoutClause), "")] = "Reserved Parking paying out. A flat coin flip: luck does not change it.",
        [(typeof(ParkingPayoutClause), "parkingPayout")] = "The pulls of Reserved Parking's stream that count.",
        [(typeof(BloodstoneTriggerClause), "")] = "Bloodstone triggering. A flat coin flip: luck does not change it.",
        [(typeof(BloodstoneTriggerClause), "bloodstoneTrigger")] = "The pulls of Bloodstone's stream that count.",

        // ── and, or ──
        [(typeof(AndClause), "")] = "Matches when every clause in its list matches.",
        [(typeof(OrClause), "")] = "Matches when at least min of the clauses in its list match.",
        [(typeof(AndClause), "and")] = "The clauses that must all match.",
        [(typeof(OrClause), "or")] = "The clauses of which at least min must match.",
        [(typeof(OrClause), "min")] = "How many of the clauses must match. 1 when not written.",
        [(typeof(LogicClause), "clauses")] = "The list of clauses, written beside the key in place of under it.",
        [(typeof(LogicClause), "mode")] =
            "How an or scores under should:. Sum adds every clause that matches; Max scores only the best one.",
    };

    /// <summary>The entry that describes the member: the type's own, else the closest owner it derives from.</summary>
    internal static (Type Owner, string Member)? Find(Type type, string member)
    {
        if (Text.ContainsKey((type, member)))
            return (type, member);
        // A type's own description is its own: it is not handed down from a base type.
        if (member.Length == 0)
            return null;

        (Type Owner, string Member)? closest = null;
        foreach (var key in Text.Keys)
        {
            if (key.Member != member || !key.Owner.IsAssignableFrom(type))
                continue;
            if (closest is null || closest.Value.Owner.IsAssignableFrom(key.Owner))
                closest = key;
            else if (!key.Owner.IsAssignableFrom(closest.Value.Owner))
                throw new InvalidOperationException(
                    $"JamlSchemaAnnotations: {closest.Value.Owner.Name} and {key.Owner.Name} both describe {type.Name}.{member}.");
        }
        return closest;
    }

    /// <summary>How an entry or a member is named in a message.</summary>
    internal static string Name(Type type, string member) => member.Length == 0 ? type.Name : $"{type.Name}.{member}";
}
