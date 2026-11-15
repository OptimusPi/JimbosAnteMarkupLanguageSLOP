// The filter shapes the engine's loader binds. Generated from the loader by
//   dotnet run --project Motely.CLI -- schema
// Do not edit: change the clause classes in Motely/Filters (and the descriptions in
// Motely/Filters/Schema/JamlSchemaAnnotations.cs), then run the command. A filter is plain JSON: build a
// JamlFilter, hand JSON.stringify(filter) to JamlConfigLoader.check or fromJaml, and read one back with JSON.parse.

export type MotelyItemEditionName = "None" | "Foil" | "Holographic" | "Polychrome" | "Negative";
export type MotelyJokerName = "Joker" | "GreedyJoker" | "LustyJoker" | "WrathfulJoker" | "GluttonousJoker" | "JollyJoker" | "ZanyJoker" | "MadJoker" | "CrazyJoker" | "DrollJoker" | "SlyJoker" | "WilyJoker" | "CleverJoker" | "DeviousJoker" | "CraftyJoker" | "HalfJoker" | "CreditCard" | "Banner" | "MysticSummit" | "EightBall" | "Misprint" | "RaisedFist" | "ChaostheClown" | "ScaryFace" | "AbstractJoker" | "DelayedGratification" | "GrosMichel" | "EvenSteven" | "OddTodd" | "Scholar" | "BusinessCard" | "Supernova" | "RideTheBus" | "Egg" | "Runner" | "IceCream" | "Splash" | "BlueJoker" | "FacelessJoker" | "GreenJoker" | "Superposition" | "ToDoList" | "Cavendish" | "RedCard" | "SquareJoker" | "RiffRaff" | "Photograph" | "ReservedParking" | "MailInRebate" | "Hallucination" | "FortuneTeller" | "Juggler" | "Drunkard" | "GoldenJoker" | "Popcorn" | "WalkieTalkie" | "SmileyFace" | "GoldenTicket" | "Swashbuckler" | "HangingChad" | "ShootTheMoon" | "JokerStencil" | "FourFingers" | "Mime" | "CeremonialDagger" | "MarbleJoker" | "LoyaltyCard" | "Dusk" | "Fibonacci" | "SteelJoker" | "Hack" | "Pareidolia" | "SpaceJoker" | "Burglar" | "Blackboard" | "SixthSense" | "Constellation" | "Hiker" | "CardSharp" | "Madness" | "Seance" | "Vampire" | "Shortcut" | "Hologram" | "Cloud9" | "Rocket" | "MidasMask" | "Luchador" | "GiftCard" | "TurtleBean" | "Erosion" | "ToTheMoon" | "StoneJoker" | "LuckyCat" | "Bull" | "DietCola" | "TradingCard" | "FlashCard" | "SpareTrousers" | "Ramen" | "Seltzer" | "Castle" | "MrBones" | "Acrobat" | "SockAndBuskin" | "Troubadour" | "Certificate" | "SmearedJoker" | "Throwback" | "RoughGem" | "Bloodstone" | "Arrowhead" | "OnyxAgate" | "GlassJoker" | "Showman" | "FlowerPot" | "MerryAndy" | "OopsAll6s" | "TheIdol" | "SeeingDouble" | "Matador" | "Satellite" | "Cartomancer" | "Astronomer" | "Bootstraps" | "DNA" | "Vagabond" | "Baron" | "Obelisk" | "BaseballCard" | "AncientJoker" | "Campfire" | "Blueprint" | "WeeJoker" | "HitTheRoad" | "TheDuo" | "TheTrio" | "TheFamily" | "TheOrder" | "TheTribe" | "Stuntman" | "InvisibleJoker" | "Brainstorm" | "DriversLicense" | "BurntJoker" | "Canio" | "Triboulet" | "Yorick" | "Chicot" | "Perkeo";
export type MotelyJokerStickerName = "None" | "Eternal" | "Perishable" | "Rental";
export type MotelyLuckName = "X1" | "X2" | "X4" | "X5" | "X8" | "X16" | "X32" | "X64" | 1 | 2 | 4 | 5 | 8 | 16 | 32 | 64;
export type MotelyVoucherName = "Overstock" | "OverstockPlus" | "ClearanceSale" | "Liquidation" | "Hone" | "GlowUp" | "RerollSurplus" | "RerollGlut" | "CrystalBall" | "OmenGlobe" | "Telescope" | "Observatory" | "Grabber" | "NachoTong" | "Wasteful" | "Recyclomancy" | "TarotMerchant" | "TarotTycoon" | "PlanetMerchant" | "PlanetTycoon" | "SeedMoney" | "MoneyTree" | "Blank" | "Antimatter" | "MagicTrick" | "Illusion" | "Hieroglyph" | "Petroglyph" | "DirectorsCut" | "Retcon" | "PaintBrush" | "Palette";
export type MotelyJokerCommonName = "Joker" | "GreedyJoker" | "LustyJoker" | "WrathfulJoker" | "GluttonousJoker" | "JollyJoker" | "ZanyJoker" | "MadJoker" | "CrazyJoker" | "DrollJoker" | "SlyJoker" | "WilyJoker" | "CleverJoker" | "DeviousJoker" | "CraftyJoker" | "HalfJoker" | "CreditCard" | "Banner" | "MysticSummit" | "EightBall" | "Misprint" | "RaisedFist" | "ChaostheClown" | "ScaryFace" | "AbstractJoker" | "DelayedGratification" | "GrosMichel" | "EvenSteven" | "OddTodd" | "Scholar" | "BusinessCard" | "Supernova" | "RideTheBus" | "Egg" | "Runner" | "IceCream" | "Splash" | "BlueJoker" | "FacelessJoker" | "GreenJoker" | "Superposition" | "ToDoList" | "Cavendish" | "RedCard" | "SquareJoker" | "RiffRaff" | "Photograph" | "ReservedParking" | "MailInRebate" | "Hallucination" | "FortuneTeller" | "Juggler" | "Drunkard" | "GoldenJoker" | "Popcorn" | "WalkieTalkie" | "SmileyFace" | "GoldenTicket" | "Swashbuckler" | "HangingChad" | "ShootTheMoon";
export type MotelyJokerUncommonName = "JokerStencil" | "FourFingers" | "Mime" | "CeremonialDagger" | "MarbleJoker" | "LoyaltyCard" | "Dusk" | "Fibonacci" | "SteelJoker" | "Hack" | "Pareidolia" | "SpaceJoker" | "Burglar" | "Blackboard" | "SixthSense" | "Constellation" | "Hiker" | "CardSharp" | "Madness" | "Seance" | "Vampire" | "Shortcut" | "Hologram" | "Cloud9" | "Rocket" | "MidasMask" | "Luchador" | "GiftCard" | "TurtleBean" | "Erosion" | "ToTheMoon" | "StoneJoker" | "LuckyCat" | "Bull" | "DietCola" | "TradingCard" | "FlashCard" | "SpareTrousers" | "Ramen" | "Seltzer" | "Castle" | "MrBones" | "Acrobat" | "SockAndBuskin" | "Troubadour" | "Certificate" | "SmearedJoker" | "Throwback" | "RoughGem" | "Bloodstone" | "Arrowhead" | "OnyxAgate" | "GlassJoker" | "Showman" | "FlowerPot" | "MerryAndy" | "OopsAll6s" | "TheIdol" | "SeeingDouble" | "Matador" | "Satellite" | "Cartomancer" | "Astronomer" | "Bootstraps";
export type MotelyJokerRareName = "DNA" | "Vagabond" | "Baron" | "Obelisk" | "BaseballCard" | "AncientJoker" | "Campfire" | "Blueprint" | "WeeJoker" | "HitTheRoad" | "TheDuo" | "TheTrio" | "TheFamily" | "TheOrder" | "TheTribe" | "Stuntman" | "InvisibleJoker" | "Brainstorm" | "DriversLicense" | "BurntJoker";
export type MotelyJokerLegendaryName = "Canio" | "Triboulet" | "Yorick" | "Chicot" | "Perkeo";
export type MotelyPlanetCardName = "Mercury" | "Venus" | "Earth" | "Mars" | "Jupiter" | "Saturn" | "Uranus" | "Neptune" | "Pluto" | "PlanetX" | "Ceres" | "Eris";
export type MotelySpectralCardName = "Familiar" | "Grim" | "Incantation" | "Talisman" | "Aura" | "Wraith" | "Sigil" | "Ouija" | "Ectoplasm" | "Immolate" | "Ankh" | "DejaVu" | "Hex" | "Trance" | "Medium" | "Cryptid" | "TheSoul" | "BlackHole";
export type MotelyTarotCardName = "TheFool" | "TheMagician" | "TheHighPriestess" | "TheEmpress" | "TheEmperor" | "TheHierophant" | "TheLovers" | "TheChariot" | "Justice" | "TheHermit" | "TheWheelOfFortune" | "Strength" | "TheHangedMan" | "Death" | "Temperance" | "TheDevil" | "TheTower" | "TheStar" | "TheMoon" | "TheSun" | "Judgement" | "TheWorld";
export type MotelyStandardCardName = "TwoOfClubs" | "ThreeOfClubs" | "FourOfClubs" | "FiveOfClubs" | "SixOfClubs" | "SevenOfClubs" | "EightOfClubs" | "NineOfClubs" | "TenOfClubs" | "JackOfClubs" | "QueenOfClubs" | "KingOfClubs" | "AceOfClubs" | "TwoOfDiamonds" | "ThreeOfDiamonds" | "FourOfDiamonds" | "FiveOfDiamonds" | "SixOfDiamonds" | "SevenOfDiamonds" | "EightOfDiamonds" | "NineOfDiamonds" | "TenOfDiamonds" | "JackOfDiamonds" | "QueenOfDiamonds" | "KingOfDiamonds" | "AceOfDiamonds" | "TwoOfHearts" | "ThreeOfHearts" | "FourOfHearts" | "FiveOfHearts" | "SixOfHearts" | "SevenOfHearts" | "EightOfHearts" | "NineOfHearts" | "TenOfHearts" | "JackOfHearts" | "QueenOfHearts" | "KingOfHearts" | "AceOfHearts" | "TwoOfSpades" | "ThreeOfSpades" | "FourOfSpades" | "FiveOfSpades" | "SixOfSpades" | "SevenOfSpades" | "EightOfSpades" | "NineOfSpades" | "TenOfSpades" | "JackOfSpades" | "QueenOfSpades" | "KingOfSpades" | "AceOfSpades";
export type MotelyItemEnhancementName = "None" | "Bonus" | "Mult" | "Wild" | "Glass" | "Steel" | "Stone" | "Gold" | "Lucky";
export type MotelyStandardcardRankName = "Two" | "Three" | "Four" | "Five" | "Six" | "Seven" | "Eight" | "Nine" | "Ten" | "Jack" | "Queen" | "King" | "Ace";
export type MotelyItemSealName = "None" | "Gold" | "Red" | "Blue" | "Purple";
export type MotelyStandardcardSuitName = "Clubs" | "Diamonds" | "Hearts" | "Spades";
export type MotelyTagName = "UncommonTag" | "RareTag" | "NegativeTag" | "FoilTag" | "HolographicTag" | "PolychromeTag" | "InvestmentTag" | "VoucherTag" | "BossTag" | "StandardTag" | "CharmTag" | "MeteorTag" | "BuffoonTag" | "HandyTag" | "GarbageTag" | "EtherealTag" | "CouponTag" | "DoubleTag" | "JuggleTag" | "D6Tag" | "TopupTag" | "SpeedTag" | "OrbitalTag" | "EconomyTag";
export type MotelyBossBlindName = "TheClub" | "TheGoad" | "TheHead" | "TheHook" | "TheManacle" | "ThePillar" | "ThePsychic" | "TheWindow" | "TheArm" | "TheFish" | "TheFlint" | "TheHouse" | "TheMark" | "TheMouth" | "TheNeedle" | "TheWall" | "TheWater" | "TheWheel" | "TheEye" | "TheTooth" | "ThePlant" | "TheSerpent" | "TheOx" | "AmberAcorn" | "CeruleanBell" | "CrimsonHeart" | "VerdantLeaf" | "VioletVessel";
export type MotelyBoosterPackName = "Arcana" | "JumboArcana" | "MegaArcana" | "Celestial" | "JumboCelestial" | "MegaCelestial" | "Standard" | "JumboStandard" | "MegaStandard" | "Buffoon" | "JumboBuffoon" | "MegaBuffoon" | "Spectral" | "JumboSpectral" | "MegaSpectral";
export type MotelyPokerHandName = "HighCard" | "Pair" | "TwoPair" | "ThreeOfAKind" | "Straight" | "Flush" | "FullHouse" | "FourOfAKind" | "StraightFlush";
export type LogicScoreModeName = "Sum" | "Max";
export type MotelyDeckName = "Red" | "Blue" | "Yellow" | "Green" | "Black" | "Magic" | "Nebula" | "Ghost" | "Abandoned" | "Checkered" | "Zodiac" | "Painted" | "Anaglyph" | "Plasma" | "Erratic";
export type MotelyStakeName = "White" | "Red" | "Green" | "Black" | "Blue" | "Purple" | "Orange" | "Gold";

/** A Motely seed filter: the keys, enums and nesting the loader binds. */
export interface JamlFilter {
  /** The filter's identifier, for tools that store filters. */
  id?: string | number | boolean | null;
  /** The filter's name. */
  name?: string | number | boolean | null;
  /** What the filter looks for, in the author's words. */
  description?: string | number | boolean | null;
  /** Who wrote the filter. */
  author?: string | number | boolean | null;
  /** When the filter was written. Metadata: the search never reads it. */
  dateCreated?: string | number | boolean | null;
  /** The deck every seed is played with. Red when not written. */
  deck?: MotelyDeckName;
  /** The stake every seed is played at. White when not written. */
  stake?: MotelyStakeName;
  /** Seeds kept with the filter, for replaying it. */
  seeds?: (string | number | boolean)[] | null;
  /** A fixed score floor: only seeds that score at least this are reported. Without it the host chooses the cutoff. */
  cutoff?: number | null;
  /** Clauses a seed has to match. */
  must?: JamlClause[] | null;
  /** Clauses that score a seed. A seed that matches none still passes. */
  should?: JamlClause[] | null;
  /** Clauses a seed has to not match. */
  mustNot?: JamlClause[] | null;
}

/** Which packs a legendary joker clause opens Souls from. Written, it replaces the clause's default sources as a whole. */
export interface JamlLegendaryJokerSourceConfig {
  /** Pack offers to read on the Arcana pack path only. */
  arcanaPacks?: number | number[] | null;
  /** Pack offers where The Soul counts from either an Arcana or a Spectral pack. Not read for slots when arcanaPacks or spectralPacks is written. */
  boosterPacks?: number | number[] | null;
  /** Another spelling of requireMegaPack. */
  requireMega?: boolean | null;
  /** When true, only Mega packs count. */
  requireMegaPack?: boolean;
  /** Pack offers to read on the Spectral pack path only. */
  spectralPacks?: number | number[] | null;
  /** Charm Tag Mega Arcana packs, opened at blind select before the ante's shop: 0 is the small blind's tag, 1 the big blind's. */
  tagPacks?: number | number[] | null;
}

/** Where a joker clause looks. Written, it replaces the clause's default sources as a whole. */
export interface JamlJokerSourceConfig {
  /** Rolls of the shop's joker stream across rarities. */
  allShopJokers?: number | number[] | null;
  /** Booster pack offers to read in each ante, counted from 0. */
  boosterPacks?: number | number[] | null;
  /** Rolls of the shop's common joker stream alone. */
  commonShopJokers?: number | number[] | null;
  /** Rolls of the joker stream the Judgement tarot draws from. */
  judgement?: number | number[] | null;
  /** Rolls of the shop's rare joker stream alone. */
  rareShopJokers?: number | number[] | null;
  /** Rolls of the Rare Tag's joker stream. */
  rareTag?: number | number[] | null;
  /** Another spelling of requireMegaPack. */
  requireMega?: boolean | null;
  /** When true, only Mega packs count. */
  requireMegaPack?: boolean;
  /** Rolls of the common joker stream Riff-Raff draws from. */
  riffRaff?: number | number[] | null;
  /** Shop slots to read in each ante, counted from 0 across rerolls. */
  shopItems?: number | number[] | null;
  /** Rolls of the shop's uncommon joker stream alone. Not the same numbers as shopItems when slots mix item types. */
  uncommonShopJokers?: number | number[] | null;
  /** Rolls of the Uncommon Tag's joker stream. */
  uncommonTag?: number | number[] | null;
  /** Rolls of the joker stream the Wraith spectral draws from (always Rare). */
  wraith?: number | number[] | null;
}

/** What the player holds while a clause is read. Each clause reads the keys that change it and ignores the others. */
export interface JamlWith {
  /** Jokers the player holds. A held legendary is out of the Soul's pool; holding Showman keeps repeats in. */
  jokers?: MotelyJokerName | "Any" | MotelyJokerName[] | null;
  /** The Oops! All 6s multiplier on an event's odds, as a name (X1, X2, X4, ...) or its number. */
  luck?: MotelyLuckName;
  /** Vouchers the player has redeemed, on top of the deck's own. The shop the clause reads is rolled with them. */
  vouchers?: MotelyVoucherName | "Any" | MotelyVoucherName[] | null;
}

/** Where a planetCard clause looks. Written, it replaces the clause's default sources as a whole. */
export interface JamlPlanetSourceConfig {
  /** Booster pack offers to read in each ante, counted from 0. */
  boosterPacks?: number | number[] | null;
  /** Another spelling of requireMegaPack. */
  requireMega?: boolean | null;
  /** When true, only Mega packs count. */
  requireMegaPack?: boolean;
  /** Shop slots to read in each ante, counted from 0 across rerolls. */
  shopItems?: number | number[] | null;
}

/** Where a spectralCard clause looks. Written, it replaces the clause's default sources as a whole. */
export interface JamlSpectralCardSourceConfig {
  /** Booster pack offers to read in each ante, counted from 0. */
  boosterPacks?: number | number[] | null;
  /** When true, the Ethereal Tag's bonus Spectral pack counts. */
  etherealTag?: boolean;
  /** When true, spectral cards that Omen Globe puts in Arcana packs count. Assumes Omen Globe is owned. */
  omenGlobe?: boolean;
  /** Another spelling of requireMegaPack. */
  requireMega?: boolean | null;
  /** When true, only Mega packs count. */
  requireMegaPack?: boolean;
  /** Rolls of the spectral stream Seance draws from. */
  seance?: number | number[] | null;
  /** Shop slots to read in each ante, counted from 0 across rerolls. */
  shopItems?: number | number[] | null;
  /** Rolls of the spectral stream Sixth Sense draws from. */
  sixthSense?: number | number[] | null;
}

/** Where a tarotCard clause looks. Written, it replaces the clause's default sources as a whole. */
export interface JamlTarotCardSourceConfig {
  /** Booster pack offers to read in each ante, counted from 0. */
  boosterPacks?: number | number[] | null;
  /** When true, the Charm Tag's bonus Arcana pack counts. */
  charmTag?: boolean;
  /** Rolls of the tarot stream The Emperor draws from. */
  emperor?: number | number[] | null;
  /** Rolls of the tarot stream a Purple Seal or 8 Ball draws from. */
  purpleSealOrEightBall?: number | number[] | null;
  /** Another spelling of requireMegaPack. */
  requireMega?: boolean | null;
  /** When true, only Mega packs count. */
  requireMegaPack?: boolean;
  /** Shop slots to read in each ante, counted from 0 across rerolls. */
  shopItems?: number | number[] | null;
  /** Charm Tag Mega Arcana packs, opened at blind select before the ante's shop: 0 is the small blind's tag, 1 the big blind's. */
  tagPacks?: number | number[] | null;
}

/** Where a standardCard clause looks. Written, it replaces the clause's default sources as a whole. */
export interface JamlStandardCardSourceConfig {
  /** Booster pack offers to read in each ante, counted from 0. */
  boosterPacks?: number | number[] | null;
  /** Another spelling of requireMegaPack. */
  requireMega?: boolean | null;
  /** When true, only Mega packs count. */
  requireMegaPack?: boolean;
  /** Shop slots to read in each ante, counted from 0 across rerolls. */
  shopItems?: number | number[] | null;
}

/** A playing card described by its parts. A part not written matches any. */
export interface JamlStandardCardSlot {
  /** The edition the joker or card must have. */
  edition?: MotelyItemEditionName | null;
  /** The enhancement the card must have. */
  enhancement?: MotelyItemEnhancementName | null;
  /** The rank the card must have. J, Q, K and A are read as Jack, Queen, King and Ace. */
  rank?: MotelyStandardcardRankName | null;
  /** The seal the card must have. */
  seal?: MotelyItemSealName | null;
  /** The suit the card must have. */
  suit?: MotelyStandardcardSuitName | null;
}

/** One Soul the run opens: the legendary it gives, and the antes it may sit in. */
export interface JamlSoulOrderEntry {
  /** The antes this Soul may sit in. Any ante when not written. */
  antes?: number | number[] | null;
  /** The legendary this Soul gives. */
  legendary: MotelyJokerLegendaryName;
}

/** A joker of any rarity. Naming a legendary, or no joker at all, also counts the legendaries a Soul gives. */
export interface JamlJokerClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** The edition the joker or card must have. */
  edition?: MotelyItemEditionName | null;
  /** The jokers that count: one name, a list, or Any. */
  joker?: MotelyJokerName | "Any" | MotelyJokerName[] | null;
  /** Another spelling of the clause's own key: the jokers that count. */
  jokers?: MotelyJokerName | "Any" | MotelyJokerName[] | null;
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** Where the clause looks for the legendaries it names, as sources does on a legendaryJoker clause. */
  legendarySources?: JamlLegendaryJokerSourceConfig | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** Where the clause looks. Written, it replaces the clause's default sources as a whole. */
  sources?: JamlJokerSourceConfig | null;
  /** Stickers the joker must carry: one name, a list, or Any. */
  stickers?: MotelyJokerStickerName | "Any" | MotelyJokerStickerName[] | null;
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** A common joker. */
export interface JamlCommonJokerClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** The common jokers that count: one name, a list, or Any. */
  commonJoker?: MotelyJokerCommonName | "Any" | MotelyJokerCommonName[] | null;
  /** Another spelling of commonJoker. */
  commonJokers?: MotelyJokerCommonName | "Any" | MotelyJokerCommonName[] | null;
  /** The edition the joker or card must have. */
  edition?: MotelyItemEditionName | null;
  /** Another spelling of the clause's own key: the jokers that count. */
  jokers?: MotelyJokerCommonName | "Any" | MotelyJokerCommonName[] | null;
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** Where the clause looks. Written, it replaces the clause's default sources as a whole. */
  sources?: JamlJokerSourceConfig | null;
  /** Stickers the joker must carry: one name, a list, or Any. */
  stickers?: MotelyJokerStickerName | "Any" | MotelyJokerStickerName[] | null;
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** An uncommon joker. */
export interface JamlUncommonJokerClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** The edition the joker or card must have. */
  edition?: MotelyItemEditionName | null;
  /** Another spelling of the clause's own key: the jokers that count. */
  jokers?: MotelyJokerUncommonName | "Any" | MotelyJokerUncommonName[] | null;
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** Where the clause looks. Written, it replaces the clause's default sources as a whole. */
  sources?: JamlJokerSourceConfig | null;
  /** Stickers the joker must carry: one name, a list, or Any. */
  stickers?: MotelyJokerStickerName | "Any" | MotelyJokerStickerName[] | null;
  /** The uncommon jokers that count: one name, a list, or Any. */
  uncommonJoker?: MotelyJokerUncommonName | "Any" | MotelyJokerUncommonName[] | null;
  /** Another spelling of uncommonJoker. */
  uncommonJokers?: MotelyJokerUncommonName | "Any" | MotelyJokerUncommonName[] | null;
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** A rare joker. */
export interface JamlRareJokerClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** The edition the joker or card must have. */
  edition?: MotelyItemEditionName | null;
  /** Another spelling of the clause's own key: the jokers that count. */
  jokers?: MotelyJokerRareName | "Any" | MotelyJokerRareName[] | null;
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The rare jokers that count: one name, a list, or Any. */
  rareJoker?: MotelyJokerRareName | "Any" | MotelyJokerRareName[] | null;
  /** Another spelling of rareJoker. */
  rareJokers?: MotelyJokerRareName | "Any" | MotelyJokerRareName[] | null;
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** Where the clause looks. Written, it replaces the clause's default sources as a whole. */
  sources?: JamlJokerSourceConfig | null;
  /** Stickers the joker must carry: one name, a list, or Any. */
  stickers?: MotelyJokerStickerName | "Any" | MotelyJokerStickerName[] | null;
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** The legendary joker a Soul card gives. */
export interface JamlLegendaryJokerClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** The edition the joker or card must have. */
  edition?: MotelyItemEditionName | null;
  /** Another spelling of the clause's own key: the jokers that count. */
  jokers?: MotelyJokerLegendaryName | "Any" | MotelyJokerLegendaryName[] | null;
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The legendaries that count: one name, a list, or Any. */
  legendaryJoker?: MotelyJokerLegendaryName | "Any" | MotelyJokerLegendaryName[] | null;
  /** Another spelling of legendaryJoker. */
  legendaryJokers?: MotelyJokerLegendaryName | "Any" | MotelyJokerLegendaryName[] | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** Which opened Souls count: 0 is the first Soul the run opens, 1 the second. Every opened Soul when not written. */
  rolls?: number | number[] | null;
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** When true, The Soul appearing in a pack the clause reads is a match, whatever legendary it gives. */
  soulCardOnly?: boolean;
  /** Extra edition reads per ante for the fast edition check. 0 uses the number of pack offers the clause reads. */
  soulEditionRolls?: number;
  /** Another spelling of legendaryJoker. */
  soulJoker?: MotelyJokerLegendaryName | "Any" | MotelyJokerLegendaryName[] | null;
  /** Where the clause looks. Written, it replaces the clause's default sources as a whole. */
  sources?: JamlLegendaryJokerSourceConfig | null;
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** A planet card. */
export interface JamlPlanetCardClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The planet cards that count: one name, a list, or Any. */
  planetCard?: MotelyPlanetCardName | "Any" | MotelyPlanetCardName[] | null;
  /** Another spelling of planetCard. */
  planetCards?: MotelyPlanetCardName | "Any" | MotelyPlanetCardName[] | null;
  /** Another spelling of planetCard. */
  planets?: MotelyPlanetCardName | "Any" | MotelyPlanetCardName[] | null;
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** Where the clause looks. Written, it replaces the clause's default sources as a whole. */
  sources?: JamlPlanetSourceConfig | null;
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** A spectral card. */
export interface JamlSpectralCardClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** Where the clause looks. Written, it replaces the clause's default sources as a whole. */
  sources?: JamlSpectralCardSourceConfig | null;
  /** The spectral cards that count: one name, a list, or Any. */
  spectralCard?: MotelySpectralCardName | "Any" | MotelySpectralCardName[] | null;
  /** Another spelling of spectralCard. */
  spectralCards?: MotelySpectralCardName | "Any" | MotelySpectralCardName[] | null;
  /** Another spelling of spectralCard. */
  spectrals?: MotelySpectralCardName | "Any" | MotelySpectralCardName[] | null;
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** A tarot card. */
export interface JamlTarotCardClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** Where the clause looks. Written, it replaces the clause's default sources as a whole. */
  sources?: JamlTarotCardSourceConfig | null;
  /** The tarot cards that count: one name, a list, or Any. */
  tarotCard?: MotelyTarotCardName | "Any" | MotelyTarotCardName[] | null;
  /** Another spelling of tarotCard. */
  tarotCards?: MotelyTarotCardName | "Any" | MotelyTarotCardName[] | null;
  /** Another spelling of tarotCard. */
  tarots?: MotelyTarotCardName | "Any" | MotelyTarotCardName[] | null;
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** A playing card in a Standard pack or the shop. */
export interface JamlStandardCardClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** A whole card (AceOfSpades): its rank and suit at once. */
  card?: MotelyStandardCardName | null;
  /** The edition the joker or card must have. */
  edition?: MotelyItemEditionName | null;
  /** The enhancement the card must have. */
  enhancement?: MotelyItemEnhancementName | null;
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The rank the card must have. J, Q, K and A are read as Jack, Queen, King and Ace. */
  rank?: MotelyStandardcardRankName | null;
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** The seal the card must have. */
  seal?: MotelyItemSealName | null;
  /** Where the clause looks. Written, it replaces the clause's default sources as a whole. */
  sources?: JamlStandardCardSourceConfig | null;
  /** The card that counts: a whole card name, Any, or a mapping of rank, suit, enhancement, seal and edition. */
  standardCard?: MotelyStandardCardName | "Any" | JamlStandardCardSlot | null;
  /** Another spelling of standardCard. */
  standardCards?: MotelyStandardCardName | "Any" | JamlStandardCardSlot | null;
  /** The suit the card must have. */
  suit?: MotelyStandardcardSuitName | null;
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** How many cards of one rank the starting deck holds. Varies by seed on the Erratic deck. */
export interface JamlErraticRankClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** The rank to count. */
  erraticRank?: MotelyStandardcardRankName | null;
  /** Another spelling of erraticRank. */
  erraticRanks?: MotelyStandardcardRankName | null;
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The rank to count. J, Q, K and A are read as Jack, Queen, King and Ace. */
  rank?: MotelyStandardcardRankName;
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
}

/** How many cards of one suit the starting deck holds. Varies by seed on the Erratic deck. */
export interface JamlErraticSuitClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** The suit to count. */
  erraticSuit?: MotelyStandardcardSuitName | null;
  /** Another spelling of erraticSuit. */
  erraticSuits?: MotelyStandardcardSuitName | null;
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** The suit to count. */
  suit?: MotelyStandardcardSuitName;
}

/** A voucher an ante offers. */
export interface JamlVoucherClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** Draws of the ante's voucher stream: 0 is the voucher the ante offers, 1 and up are further draws. [0] when not written. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** The vouchers that count: one name, a list, or Any. */
  voucher?: MotelyVoucherName | "Any" | MotelyVoucherName[] | null;
  /** Another spelling of voucher. */
  vouchers?: MotelyVoucherName | "Any" | MotelyVoucherName[] | null;
}

/** A skip tag an ante offers. */
export interface JamlTagClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** Draws of the ante's tag stream: 0 is the small blind's offer, 1 the big blind's, 2 and up are further draws. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** The tags that count: one name, a list, or Any. */
  tag?: MotelyTagName | "Any" | MotelyTagName[] | null;
  /** Another spelling of tag. */
  tags?: MotelyTagName | "Any" | MotelyTagName[] | null;
}

/** The small blind's tag offer only. */
export interface JamlSmallBlindTagClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** Draws of the ante's tag stream: 0 is the small blind's offer, 1 the big blind's, 2 and up are further draws. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** The tags that count: one name, a list, or Any. */
  smallBlindTag?: MotelyTagName | "Any" | MotelyTagName[] | null;
  /** The tags that count: one name, a list, or Any. */
  tag?: MotelyTagName | "Any" | MotelyTagName[] | null;
  /** Another spelling of tag. */
  tags?: MotelyTagName | "Any" | MotelyTagName[] | null;
}

/** The big blind's tag offer only. */
export interface JamlBigBlindTagClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** The tags that count: one name, a list, or Any. */
  bigBlindTag?: MotelyTagName | "Any" | MotelyTagName[] | null;
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** Draws of the ante's tag stream: 0 is the small blind's offer, 1 the big blind's, 2 and up are further draws. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** The tags that count: one name, a list, or Any. */
  tag?: MotelyTagName | "Any" | MotelyTagName[] | null;
  /** Another spelling of tag. */
  tags?: MotelyTagName | "Any" | MotelyTagName[] | null;
}

/** An ante's boss blind. */
export interface JamlBossClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** The bosses that count: one name, a list, or Any. */
  boss?: MotelyBossBlindName | "Any" | MotelyBossBlindName[] | null;
  /** Another spelling of boss. */
  bosses?: MotelyBossBlindName | "Any" | MotelyBossBlindName[] | null;
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
}

/** A booster pack an ante's shop offers. */
export interface JamlBoosterPackClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** The packs that count, by type and size: one name, a list, or Any. */
  boosterPack?: MotelyBoosterPackName | "Any" | MotelyBoosterPackName[] | null;
  /** Another spelling of boosterPack. */
  boosterPacks?: MotelyBoosterPackName | "Any" | MotelyBoosterPackName[] | null;
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** Another spelling of boosterPack. */
  packs?: MotelyBoosterPackName | "Any" | MotelyBoosterPackName[] | null;
  /** Pack offers to read in each ante: 0 is the first offer, 1 the second. [0, 1] when not written. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
}

/** The best poker hand in the hand a blind starts with. */
export interface JamlPokerHandClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The poker hands that count: one name, a list, or Any. */
  pokerHand?: MotelyPokerHandName | "Any" | MotelyPokerHandName[] | null;
  /** Another spelling of pokerHand. */
  pokerHands?: MotelyPokerHandName | "Any" | MotelyPokerHandName[] | null;
  /** Which blinds played in the ante to read: 0 is the first blind played, 1 the second, 2 the third. [0] when not written. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
}

/** A card in the hand an ante starts with. */
export interface JamlStartingDrawClause {
  /** Another spelling of antes: one ante or a list of them. */
  ante?: number | number[] | null;
  /** The antes the clause reads. Antes 1 to 8 when not written. */
  antes?: number | number[];
  /** A whole card (AceOfSpades): its rank and suit at once. */
  card?: MotelyStandardCardName | null;
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The rank the card must have. J, Q, K and A are read as Jack, Queen, King and Ace. */
  rank?: MotelyStandardcardRankName | null;
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** The card that counts, as a whole card name. */
  startingDraw?: MotelyStandardCardName | null;
  /** The suit the card must have. */
  suit?: MotelyStandardcardSuitName | null;
}

/** A Lucky card paying $20. */
export interface JamlLuckyMoneyClause {
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The pulls of the Lucky card money stream that count. */
  luckyMoney?: number | number[];
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The pulls of the event's own stream that count, counted from 0. The clause's own key writes the same list. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** A Lucky card giving +20 Mult. */
export interface JamlLuckyMultClause {
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The pulls of the Lucky card Mult stream that count. */
  luckyMult?: number | number[];
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The pulls of the event's own stream that count, counted from 0. The clause's own key writes the same list. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** Misprint rolling at least mult. */
export interface JamlMisprintMultClause {
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The pulls of Misprint's stream that count. */
  misprintMult?: number | number[];
  /** The least Mult a pull must show to count (Misprint rolls 0 to 23). */
  mult?: number;
  /** The pulls of the event's own stream that count, counted from 0. The clause's own key writes the same list. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
}

/** The Wheel of Fortune applying an edition. */
export interface JamlWheelOfFortuneClause {
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The pulls of the event's own stream that count, counted from 0. The clause's own key writes the same list. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** The pulls of The Wheel of Fortune's stream that count. */
  wheelOfFortune?: number | number[];
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** The Wheel boss blind leaving a card face down. */
export interface JamlWheelStaysFlippedClause {
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The pulls of the event's own stream that count, counted from 0. The clause's own key writes the same list. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** The pulls of The Wheel's stream that count. */
  wheelStaysFlipped?: number | number[];
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** A Glass card shattering. */
export interface JamlGlassDestroyClause {
  /** The pulls of the Glass card stream that count. */
  glassDestroy?: number | number[];
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The pulls of the event's own stream that count, counted from 0. The clause's own key writes the same list. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** Cavendish going extinct. */
export interface JamlCavendishExtinctClause {
  /** The pulls of Cavendish's stream that count. */
  cavendishExtinct?: number | number[];
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The pulls of the event's own stream that count, counted from 0. The clause's own key writes the same list. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** Gros Michel going extinct. */
export interface JamlGrosMichelExtinctClause {
  /** The pulls of Gros Michel's stream that count. */
  grosMichelExtinct?: number | number[];
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The pulls of the event's own stream that count, counted from 0. The clause's own key writes the same list. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** Space Joker levelling up the hand. */
export interface JamlSpaceLevelupClause {
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The pulls of the event's own stream that count, counted from 0. The clause's own key writes the same list. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** The pulls of Space Joker's stream that count. */
  spaceLevelup?: number | number[];
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** Business Card paying out. A flat coin flip: luck does not change it. */
export interface JamlBusinessPayoutClause {
  /** The pulls of Business Card's stream that count. */
  businessPayout?: number | number[];
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The pulls of the event's own stream that count, counted from 0. The clause's own key writes the same list. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
}

/** Reserved Parking paying out. A flat coin flip: luck does not change it. */
export interface JamlParkingPayoutClause {
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The pulls of Reserved Parking's stream that count. */
  parkingPayout?: number | number[];
  /** The pulls of the event's own stream that count, counted from 0. The clause's own key writes the same list. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
}

/** Bloodstone triggering. A flat coin flip: luck does not change it. */
export interface JamlBloodstoneTriggerClause {
  /** The pulls of Bloodstone's stream that count. */
  bloodstoneTrigger?: number | number[];
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The pulls of the event's own stream that count, counted from 0. The clause's own key writes the same list. */
  rolls?: number | number[];
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
}

/** Matches when every clause in its list matches. */
export interface JamlAndClause {
  /** The clauses that must all match. */
  and?: [JamlClause, ...JamlClause[]] | null;
  /** The list of clauses, written beside the key in place of under it. */
  clauses?: [JamlClause, ...JamlClause[]];
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** How an or scores under should:. Sum adds every clause that matches; Max scores only the best one. */
  mode?: LogicScoreModeName;
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
}

/** Matches when at least min of the clauses in its list match. */
export interface JamlOrClause {
  /** The list of clauses, written beside the key in place of under it. */
  clauses?: [JamlClause, ...JamlClause[]];
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** How many of the clauses must match. 1 when not written. */
  min?: number;
  /** How an or scores under should:. Sum adds every clause that matches; Max scores only the best one. */
  mode?: LogicScoreModeName;
  /** The clauses of which at least min must match. */
  or?: [JamlClause, ...JamlClause[]] | null;
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
}

/** The order in which the Souls a run opens turn into legendaries, across the whole run. */
export interface JamlLegendarySoulOrderClause {
  /** Booster pack offers the walk reads Souls from. Offers 0 to 5 when not written. */
  boosterPacks?: number | number[] | null;
  /** The list soulOrder fills, written directly. */
  entries?: JamlSoulOrderEntry[];
  /** The clause's column name in results. Derived from the clause when not written. */
  label?: string | number | boolean | null;
  /** The most occurrences that still count as a match. No limit when not written. The loader also refuses a value below min, which this schema cannot check: "max is below min (min defaults to 1), so nothing can match." */
  max?: number | null;
  /** How far the walk runs: later antes are not read. 8 when not written. */
  maxAnte?: number;
  /** The fewest occurrences that count as a match. 1 when not written. */
  min?: number;
  /** The clause's weight in a seed's score under should:. 1 when not written. */
  score?: number;
  /** The Souls in the order the run opens them: the first entry is the first Soul opened. */
  soulOrder?: JamlSoulOrderEntry[];
  /** Another spelling of soulOrder. */
  soulOrders?: JamlSoulOrderEntry[];
  /** What the player holds while this clause is read. */
  with?: JamlWith;
}

/** One clause: the member that names its kind is required. */
export type JamlClause = JamlJokerClause & Required<Pick<JamlJokerClause, "joker">> | JamlJokerClause & Required<Pick<JamlJokerClause, "jokers">> | JamlCommonJokerClause & Required<Pick<JamlCommonJokerClause, "commonJoker">> | JamlCommonJokerClause & Required<Pick<JamlCommonJokerClause, "commonJokers">> | JamlUncommonJokerClause & Required<Pick<JamlUncommonJokerClause, "uncommonJoker">> | JamlUncommonJokerClause & Required<Pick<JamlUncommonJokerClause, "uncommonJokers">> | JamlRareJokerClause & Required<Pick<JamlRareJokerClause, "rareJoker">> | JamlRareJokerClause & Required<Pick<JamlRareJokerClause, "rareJokers">> | JamlLegendaryJokerClause & Required<Pick<JamlLegendaryJokerClause, "legendaryJoker">> | JamlLegendaryJokerClause & Required<Pick<JamlLegendaryJokerClause, "legendaryJokers">> | JamlLegendaryJokerClause & Required<Pick<JamlLegendaryJokerClause, "soulJoker">> | JamlPlanetCardClause & Required<Pick<JamlPlanetCardClause, "planetCard">> | JamlPlanetCardClause & Required<Pick<JamlPlanetCardClause, "planetCards">> | JamlSpectralCardClause & Required<Pick<JamlSpectralCardClause, "spectralCard">> | JamlSpectralCardClause & Required<Pick<JamlSpectralCardClause, "spectralCards">> | JamlTarotCardClause & Required<Pick<JamlTarotCardClause, "tarotCard">> | JamlTarotCardClause & Required<Pick<JamlTarotCardClause, "tarotCards">> | JamlStandardCardClause & Required<Pick<JamlStandardCardClause, "standardCard">> | JamlStandardCardClause & Required<Pick<JamlStandardCardClause, "standardCards">> | JamlErraticRankClause & Required<Pick<JamlErraticRankClause, "erraticRank">> | JamlErraticRankClause & Required<Pick<JamlErraticRankClause, "erraticRanks">> | JamlErraticSuitClause & Required<Pick<JamlErraticSuitClause, "erraticSuit">> | JamlErraticSuitClause & Required<Pick<JamlErraticSuitClause, "erraticSuits">> | JamlVoucherClause & Required<Pick<JamlVoucherClause, "voucher">> | JamlVoucherClause & Required<Pick<JamlVoucherClause, "vouchers">> | JamlTagClause & Required<Pick<JamlTagClause, "tag">> | JamlTagClause & Required<Pick<JamlTagClause, "tags">> | JamlSmallBlindTagClause & Required<Pick<JamlSmallBlindTagClause, "smallBlindTag">> | JamlBigBlindTagClause & Required<Pick<JamlBigBlindTagClause, "bigBlindTag">> | JamlBossClause & Required<Pick<JamlBossClause, "boss">> | JamlBossClause & Required<Pick<JamlBossClause, "bosses">> | JamlBoosterPackClause & Required<Pick<JamlBoosterPackClause, "boosterPack">> | JamlBoosterPackClause & Required<Pick<JamlBoosterPackClause, "boosterPacks">> | JamlPokerHandClause & Required<Pick<JamlPokerHandClause, "pokerHand">> | JamlPokerHandClause & Required<Pick<JamlPokerHandClause, "pokerHands">> | JamlStartingDrawClause & Required<Pick<JamlStartingDrawClause, "startingDraw">> | JamlLuckyMoneyClause & Required<Pick<JamlLuckyMoneyClause, "luckyMoney">> | JamlLuckyMultClause & Required<Pick<JamlLuckyMultClause, "luckyMult">> | JamlMisprintMultClause & Required<Pick<JamlMisprintMultClause, "misprintMult">> | JamlWheelOfFortuneClause & Required<Pick<JamlWheelOfFortuneClause, "wheelOfFortune">> | JamlWheelStaysFlippedClause & Required<Pick<JamlWheelStaysFlippedClause, "wheelStaysFlipped">> | JamlGlassDestroyClause & Required<Pick<JamlGlassDestroyClause, "glassDestroy">> | JamlCavendishExtinctClause & Required<Pick<JamlCavendishExtinctClause, "cavendishExtinct">> | JamlGrosMichelExtinctClause & Required<Pick<JamlGrosMichelExtinctClause, "grosMichelExtinct">> | JamlSpaceLevelupClause & Required<Pick<JamlSpaceLevelupClause, "spaceLevelup">> | JamlBusinessPayoutClause & Required<Pick<JamlBusinessPayoutClause, "businessPayout">> | JamlParkingPayoutClause & Required<Pick<JamlParkingPayoutClause, "parkingPayout">> | JamlBloodstoneTriggerClause & Required<Pick<JamlBloodstoneTriggerClause, "bloodstoneTrigger">> | JamlAndClause & { and: [JamlClause, ...JamlClause[]] } | JamlAndClause & { and: null; clauses: [JamlClause, ...JamlClause[]] } | JamlOrClause & { or: [JamlClause, ...JamlClause[]] } | JamlOrClause & { or: null; clauses: [JamlClause, ...JamlClause[]] } | JamlLegendarySoulOrderClause & Required<Pick<JamlLegendarySoulOrderClause, "soulOrder">> | JamlLegendarySoulOrderClause & Required<Pick<JamlLegendarySoulOrderClause, "soulOrders">>;
