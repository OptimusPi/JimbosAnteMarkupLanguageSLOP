// The filter shapes the engine's loader binds: every key, member, enum name and value rule.
// GENERATED from the loader by Motely.Tests (JamlGrammar); do not edit. Regenerate:
//   MOTELY_WRITE_JAML=1 dotnet test Motely.Tests --filter JamlGrammarTests
// A filter is plain JSON: build a JamlFilter, hand JSON.stringify(filter) to JamlConfigLoader.check or
// fromJaml, and read one back with JSON.parse.

export type MotelyDeckName = "Red" | "Blue" | "Yellow" | "Green" | "Black" | "Magic" | "Nebula" | "Ghost" | "Abandoned" | "Checkered" | "Zodiac" | "Painted" | "Anaglyph" | "Plasma" | "Erratic";
export type MotelyStakeName = "White" | "Red" | "Green" | "Black" | "Blue" | "Purple" | "Orange" | "Gold";
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

/** A whole filter document, as the engine loads it from YAML or JSON. */
export interface JamlFilter {
  author?: string | number | boolean | null;
  cutoff?: number | null;
  dateCreated?: string | number | boolean | null;
  deck?: MotelyDeckName;
  description?: string | number | boolean | null;
  id?: string | number | boolean | null;
  must?: JamlClause[] | null;
  mustNot?: JamlClause[] | null;
  name?: string | number | boolean | null;
  seeds?: (string | number | boolean)[] | null;
  should?: JamlClause[] | null;
  stake?: MotelyStakeName;
}

export interface JamlLegendaryJokerSourceConfig {
  arcanaPacks?: number | number[] | null;
  boosterPacks?: number | number[] | null;
  requireMega?: boolean | null;
  requireMegaPack?: boolean;
  spectralPacks?: number | number[] | null;
  tagPacks?: number | number[] | null;
}

export interface JamlJokerSourceConfig {
  allShopJokers?: number | number[] | null;
  boosterPacks?: number | number[] | null;
  commonShopJokers?: number | number[] | null;
  judgement?: number | number[] | null;
  rareShopJokers?: number | number[] | null;
  rareTag?: number | number[] | null;
  requireMega?: boolean | null;
  requireMegaPack?: boolean;
  riffRaff?: number | number[] | null;
  shopItems?: number | number[] | null;
  uncommonShopJokers?: number | number[] | null;
  uncommonTag?: number | number[] | null;
  wraith?: number | number[] | null;
}

export interface JamlWith {
  jokers?: MotelyJokerName | MotelyJokerName[] | "Any" | null;
  luck?: MotelyLuckName;
  vouchers?: MotelyVoucherName | MotelyVoucherName[] | "Any" | null;
}

export interface JamlPlanetSourceConfig {
  boosterPacks?: number | number[] | null;
  requireMega?: boolean | null;
  requireMegaPack?: boolean;
  shopItems?: number | number[] | null;
}

export interface JamlSpectralCardSourceConfig {
  boosterPacks?: number | number[] | null;
  etherealTag?: boolean;
  omenGlobe?: boolean;
  requireMega?: boolean | null;
  requireMegaPack?: boolean;
  seance?: number | number[] | null;
  shopItems?: number | number[] | null;
  sixthSense?: number | number[] | null;
}

export interface JamlTarotCardSourceConfig {
  boosterPacks?: number | number[] | null;
  charmTag?: boolean;
  emperor?: number | number[] | null;
  purpleSealOrEightBall?: number | number[] | null;
  requireMega?: boolean | null;
  requireMegaPack?: boolean;
  shopItems?: number | number[] | null;
  tagPacks?: number | number[] | null;
}

export interface JamlStandardCardSourceConfig {
  boosterPacks?: number | number[] | null;
  requireMega?: boolean | null;
  requireMegaPack?: boolean;
  shopItems?: number | number[] | null;
}

export interface JamlStandardCardSlot {
  edition?: MotelyItemEditionName | null;
  enhancement?: MotelyItemEnhancementName | null;
  rank?: MotelyStandardcardRankName | null;
  seal?: MotelyItemSealName | null;
  suit?: MotelyStandardcardSuitName | null;
}

export interface JamlSoulOrderEntry {
  antes?: number | number[] | null;
  legendary: MotelyJokerLegendaryName;
}

export interface JamlJokerClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  edition?: MotelyItemEditionName | null;
  joker?: MotelyJokerName | MotelyJokerName[] | "Any" | null;
  jokers?: MotelyJokerName | MotelyJokerName[] | "Any" | null;
  label?: string | number | boolean | null;
  legendarySources?: JamlLegendaryJokerSourceConfig | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  score?: number;
  sources?: JamlJokerSourceConfig | null;
  stickers?: MotelyJokerStickerName | MotelyJokerStickerName[] | "Any" | null;
  with?: JamlWith;
}

export interface JamlCommonJokerClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  commonJoker?: MotelyJokerCommonName | MotelyJokerCommonName[] | "Any" | null;
  commonJokers?: MotelyJokerCommonName | MotelyJokerCommonName[] | "Any" | null;
  edition?: MotelyItemEditionName | null;
  jokers?: MotelyJokerCommonName | MotelyJokerCommonName[] | "Any" | null;
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  score?: number;
  sources?: JamlJokerSourceConfig | null;
  stickers?: MotelyJokerStickerName | MotelyJokerStickerName[] | "Any" | null;
  with?: JamlWith;
}

export interface JamlUncommonJokerClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  edition?: MotelyItemEditionName | null;
  jokers?: MotelyJokerUncommonName | MotelyJokerUncommonName[] | "Any" | null;
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  score?: number;
  sources?: JamlJokerSourceConfig | null;
  stickers?: MotelyJokerStickerName | MotelyJokerStickerName[] | "Any" | null;
  uncommonJoker?: MotelyJokerUncommonName | MotelyJokerUncommonName[] | "Any" | null;
  uncommonJokers?: MotelyJokerUncommonName | MotelyJokerUncommonName[] | "Any" | null;
  with?: JamlWith;
}

export interface JamlRareJokerClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  edition?: MotelyItemEditionName | null;
  jokers?: MotelyJokerRareName | MotelyJokerRareName[] | "Any" | null;
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rareJoker?: MotelyJokerRareName | MotelyJokerRareName[] | "Any" | null;
  rareJokers?: MotelyJokerRareName | MotelyJokerRareName[] | "Any" | null;
  score?: number;
  sources?: JamlJokerSourceConfig | null;
  stickers?: MotelyJokerStickerName | MotelyJokerStickerName[] | "Any" | null;
  with?: JamlWith;
}

export interface JamlLegendaryJokerClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  edition?: MotelyItemEditionName | null;
  jokers?: MotelyJokerLegendaryName | MotelyJokerLegendaryName[] | "Any" | null;
  label?: string | number | boolean | null;
  legendaryJoker?: MotelyJokerLegendaryName | MotelyJokerLegendaryName[] | "Any" | null;
  legendaryJokers?: MotelyJokerLegendaryName | MotelyJokerLegendaryName[] | "Any" | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rolls?: number | number[] | null;
  score?: number;
  soulCardOnly?: boolean;
  soulEditionRolls?: number;
  soulJoker?: MotelyJokerLegendaryName | MotelyJokerLegendaryName[] | "Any" | null;
  sources?: JamlLegendaryJokerSourceConfig | null;
  with?: JamlWith;
}

export interface JamlPlanetCardClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  planetCard?: MotelyPlanetCardName | MotelyPlanetCardName[] | "Any" | null;
  planetCards?: MotelyPlanetCardName | MotelyPlanetCardName[] | "Any" | null;
  planets?: MotelyPlanetCardName | MotelyPlanetCardName[] | "Any" | null;
  score?: number;
  sources?: JamlPlanetSourceConfig | null;
  with?: JamlWith;
}

export interface JamlSpectralCardClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  score?: number;
  sources?: JamlSpectralCardSourceConfig | null;
  spectralCard?: MotelySpectralCardName | MotelySpectralCardName[] | "Any" | null;
  spectralCards?: MotelySpectralCardName | MotelySpectralCardName[] | "Any" | null;
  spectrals?: MotelySpectralCardName | MotelySpectralCardName[] | "Any" | null;
  with?: JamlWith;
}

export interface JamlTarotCardClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  score?: number;
  sources?: JamlTarotCardSourceConfig | null;
  tarotCard?: MotelyTarotCardName | MotelyTarotCardName[] | "Any" | null;
  tarotCards?: MotelyTarotCardName | MotelyTarotCardName[] | "Any" | null;
  tarots?: MotelyTarotCardName | MotelyTarotCardName[] | "Any" | null;
  with?: JamlWith;
}

export interface JamlStandardCardClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  card?: MotelyStandardCardName | null;
  edition?: MotelyItemEditionName | null;
  enhancement?: MotelyItemEnhancementName | null;
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rank?: MotelyStandardcardRankName | null;
  score?: number;
  seal?: MotelyItemSealName | null;
  sources?: JamlStandardCardSourceConfig | null;
  standardCard?: MotelyStandardCardName | JamlStandardCardSlot | "Any" | null;
  standardCards?: MotelyStandardCardName | JamlStandardCardSlot | "Any" | null;
  suit?: MotelyStandardcardSuitName | null;
  with?: JamlWith;
}

export interface JamlErraticRankClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  erraticRank?: MotelyStandardcardRankName | null;
  erraticRanks?: MotelyStandardcardRankName | null;
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rank?: MotelyStandardcardRankName;
  score?: number;
}

export interface JamlErraticSuitClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  erraticSuit?: MotelyStandardcardSuitName | null;
  erraticSuits?: MotelyStandardcardSuitName | null;
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  score?: number;
  suit?: MotelyStandardcardSuitName;
}

export interface JamlVoucherClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rolls?: number | [number, ...number[]];
  score?: number;
  voucher?: MotelyVoucherName | MotelyVoucherName[] | "Any" | null;
  vouchers?: MotelyVoucherName | MotelyVoucherName[] | "Any" | null;
}

export interface JamlTagClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rolls?: number | [number, ...number[]];
  score?: number;
  tag?: MotelyTagName | MotelyTagName[] | "Any" | null;
  tags?: MotelyTagName | MotelyTagName[] | "Any" | null;
}

export interface JamlSmallBlindTagClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rolls?: number | [number, ...number[]];
  score?: number;
  smallBlindTag?: MotelyTagName | MotelyTagName[] | "Any" | null;
  tag?: MotelyTagName | MotelyTagName[] | "Any" | null;
  tags?: MotelyTagName | MotelyTagName[] | "Any" | null;
}

export interface JamlBigBlindTagClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  bigBlindTag?: MotelyTagName | MotelyTagName[] | "Any" | null;
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rolls?: number | [number, ...number[]];
  score?: number;
  tag?: MotelyTagName | MotelyTagName[] | "Any" | null;
  tags?: MotelyTagName | MotelyTagName[] | "Any" | null;
}

export interface JamlBossClause {
  ante?: number | number[] | null;
  antes?: number | [number, ...number[]];
  boss?: MotelyBossBlindName | MotelyBossBlindName[] | "Any" | null;
  bosses?: MotelyBossBlindName | MotelyBossBlindName[] | "Any" | null;
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  score?: number;
}

export interface JamlBoosterPackClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  boosterPack?: MotelyBoosterPackName | MotelyBoosterPackName[] | "Any" | null;
  boosterPacks?: MotelyBoosterPackName | MotelyBoosterPackName[] | "Any" | null;
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  packs?: MotelyBoosterPackName | MotelyBoosterPackName[] | "Any" | null;
  rolls?: number | [number, ...number[]];
  score?: number;
}

export interface JamlPokerHandClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  pokerHand?: MotelyPokerHandName | MotelyPokerHandName[] | "Any" | null;
  pokerHands?: MotelyPokerHandName | MotelyPokerHandName[] | "Any" | null;
  rolls?: number | number[];
  score?: number;
}

export interface JamlStartingDrawClause {
  ante?: number | number[] | null;
  antes?: number | number[];
  card?: MotelyStandardCardName | null;
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rank?: MotelyStandardcardRankName | null;
  score?: number;
  startingDraw?: MotelyStandardCardName | null;
  suit?: MotelyStandardcardSuitName | null;
}

export interface JamlLuckyMoneyClause {
  label?: string | number | boolean | null;
  luckyMoney?: number | number[] | "Any" | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rolls?: number | [number, ...number[]];
  score?: number;
  with?: JamlWith;
}

export interface JamlLuckyMultClause {
  label?: string | number | boolean | null;
  luckyMult?: number | number[] | "Any" | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rolls?: number | [number, ...number[]];
  score?: number;
  with?: JamlWith;
}

export interface JamlMisprintMultClause {
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  misprintMult?: number | number[] | "Any" | null;
  mult?: number;
  rolls?: number | [number, ...number[]];
  score?: number;
}

export interface JamlWheelOfFortuneClause {
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rolls?: number | [number, ...number[]];
  score?: number;
  wheelOfFortune?: number | number[] | "Any" | null;
  with?: JamlWith;
}

export interface JamlWheelStaysFlippedClause {
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rolls?: number | [number, ...number[]];
  score?: number;
  wheelStaysFlipped?: number | number[] | "Any" | null;
  with?: JamlWith;
}

export interface JamlGlassDestroyClause {
  glassDestroy?: number | number[] | "Any" | null;
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rolls?: number | [number, ...number[]];
  score?: number;
  with?: JamlWith;
}

export interface JamlCavendishExtinctClause {
  cavendishExtinct?: number | number[] | "Any" | null;
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rolls?: number | [number, ...number[]];
  score?: number;
  with?: JamlWith;
}

export interface JamlGrosMichelExtinctClause {
  grosMichelExtinct?: number | number[] | "Any" | null;
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rolls?: number | [number, ...number[]];
  score?: number;
  with?: JamlWith;
}

export interface JamlSpaceLevelupClause {
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rolls?: number | [number, ...number[]];
  score?: number;
  spaceLevelup?: number | number[] | "Any" | null;
  with?: JamlWith;
}

export interface JamlBusinessPayoutClause {
  businessPayout?: number | number[] | "Any" | null;
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rolls?: number | [number, ...number[]];
  score?: number;
}

export interface JamlParkingPayoutClause {
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  parkingPayout?: number | number[] | "Any" | null;
  rolls?: number | [number, ...number[]];
  score?: number;
}

export interface JamlBloodstoneTriggerClause {
  bloodstoneTrigger?: number | number[] | "Any" | null;
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  min?: number;
  rolls?: number | [number, ...number[]];
  score?: number;
}

export interface JamlAndClause {
  and?: JamlClause[] | null;
  clauses?: [JamlClause, ...JamlClause[]];
  label?: string | number | boolean | null;
  max?: null;
  min?: 1;
  mode?: LogicScoreModeName;
  score?: number;
}

export interface JamlOrClause {
  clauses?: [JamlClause, ...JamlClause[]];
  label?: string | number | boolean | null;
  max?: null;
  min?: number;
  mode?: LogicScoreModeName;
  or?: JamlClause[] | null;
  score?: number;
}

export interface JamlLegendarySoulOrderClause {
  boosterPacks?: number | number[] | null;
  entries?: [JamlSoulOrderEntry, ...JamlSoulOrderEntry[]];
  label?: string | number | boolean | null;
  /** The loader also refuses: max is below min (min defaults to 1), so nothing can match. */
  max?: number | null;
  maxAnte?: number;
  min?: number;
  score?: number;
  soulOrder?: [JamlSoulOrderEntry, ...JamlSoulOrderEntry[]];
  soulOrders?: [JamlSoulOrderEntry, ...JamlSoulOrderEntry[]];
  with?: JamlWith;
}

export type JamlClause = JamlJokerClause & Required<Pick<JamlJokerClause, "joker">> | JamlJokerClause & Required<Pick<JamlJokerClause, "jokers">> | JamlCommonJokerClause & Required<Pick<JamlCommonJokerClause, "commonJoker">> | JamlCommonJokerClause & Required<Pick<JamlCommonJokerClause, "commonJokers">> | JamlUncommonJokerClause & Required<Pick<JamlUncommonJokerClause, "uncommonJoker">> | JamlUncommonJokerClause & Required<Pick<JamlUncommonJokerClause, "uncommonJokers">> | JamlRareJokerClause & Required<Pick<JamlRareJokerClause, "rareJoker">> | JamlRareJokerClause & Required<Pick<JamlRareJokerClause, "rareJokers">> | JamlLegendaryJokerClause & Required<Pick<JamlLegendaryJokerClause, "legendaryJoker">> | JamlLegendaryJokerClause & Required<Pick<JamlLegendaryJokerClause, "legendaryJokers">> | JamlLegendaryJokerClause & Required<Pick<JamlLegendaryJokerClause, "soulJoker">> | JamlPlanetCardClause & Required<Pick<JamlPlanetCardClause, "planetCard">> | JamlPlanetCardClause & Required<Pick<JamlPlanetCardClause, "planetCards">> | JamlSpectralCardClause & Required<Pick<JamlSpectralCardClause, "spectralCard">> | JamlSpectralCardClause & Required<Pick<JamlSpectralCardClause, "spectralCards">> | JamlTarotCardClause & Required<Pick<JamlTarotCardClause, "tarotCard">> | JamlTarotCardClause & Required<Pick<JamlTarotCardClause, "tarotCards">> | JamlStandardCardClause & Required<Pick<JamlStandardCardClause, "standardCard">> | JamlStandardCardClause & Required<Pick<JamlStandardCardClause, "standardCards">> | JamlErraticRankClause & Required<Pick<JamlErraticRankClause, "erraticRank">> | JamlErraticRankClause & Required<Pick<JamlErraticRankClause, "erraticRanks">> | JamlErraticSuitClause & Required<Pick<JamlErraticSuitClause, "erraticSuit">> | JamlErraticSuitClause & Required<Pick<JamlErraticSuitClause, "erraticSuits">> | JamlVoucherClause & Required<Pick<JamlVoucherClause, "voucher">> | JamlVoucherClause & Required<Pick<JamlVoucherClause, "vouchers">> | JamlTagClause & Required<Pick<JamlTagClause, "tag">> | JamlTagClause & Required<Pick<JamlTagClause, "tags">> | JamlSmallBlindTagClause & Required<Pick<JamlSmallBlindTagClause, "smallBlindTag">> | JamlBigBlindTagClause & Required<Pick<JamlBigBlindTagClause, "bigBlindTag">> | JamlBossClause & Required<Pick<JamlBossClause, "boss">> | JamlBossClause & Required<Pick<JamlBossClause, "bosses">> | JamlBoosterPackClause & Required<Pick<JamlBoosterPackClause, "boosterPack">> | JamlBoosterPackClause & Required<Pick<JamlBoosterPackClause, "boosterPacks">> | JamlPokerHandClause & Required<Pick<JamlPokerHandClause, "pokerHand">> | JamlPokerHandClause & Required<Pick<JamlPokerHandClause, "pokerHands">> | JamlStartingDrawClause & Required<Pick<JamlStartingDrawClause, "startingDraw">> | JamlLuckyMoneyClause & { luckyMoney: number | [number, ...number[]] } | JamlLuckyMoneyClause & { luckyMoney: "Any" | null; rolls: number | [number, ...number[]] } | JamlLuckyMultClause & { luckyMult: number | [number, ...number[]] } | JamlLuckyMultClause & { luckyMult: "Any" | null; rolls: number | [number, ...number[]] } | JamlMisprintMultClause & { misprintMult: number | [number, ...number[]] } | JamlMisprintMultClause & { misprintMult: "Any" | null; rolls: number | [number, ...number[]] } | JamlWheelOfFortuneClause & { wheelOfFortune: number | [number, ...number[]] } | JamlWheelOfFortuneClause & { wheelOfFortune: "Any" | null; rolls: number | [number, ...number[]] } | JamlWheelStaysFlippedClause & { wheelStaysFlipped: number | [number, ...number[]] } | JamlWheelStaysFlippedClause & { wheelStaysFlipped: "Any" | null; rolls: number | [number, ...number[]] } | JamlGlassDestroyClause & { glassDestroy: number | [number, ...number[]] } | JamlGlassDestroyClause & { glassDestroy: "Any" | null; rolls: number | [number, ...number[]] } | JamlCavendishExtinctClause & { cavendishExtinct: number | [number, ...number[]] } | JamlCavendishExtinctClause & { cavendishExtinct: "Any" | null; rolls: number | [number, ...number[]] } | JamlGrosMichelExtinctClause & { grosMichelExtinct: number | [number, ...number[]] } | JamlGrosMichelExtinctClause & { grosMichelExtinct: "Any" | null; rolls: number | [number, ...number[]] } | JamlSpaceLevelupClause & { spaceLevelup: number | [number, ...number[]] } | JamlSpaceLevelupClause & { spaceLevelup: "Any" | null; rolls: number | [number, ...number[]] } | JamlBusinessPayoutClause & { businessPayout: number | [number, ...number[]] } | JamlBusinessPayoutClause & { businessPayout: "Any" | null; rolls: number | [number, ...number[]] } | JamlParkingPayoutClause & { parkingPayout: number | [number, ...number[]] } | JamlParkingPayoutClause & { parkingPayout: "Any" | null; rolls: number | [number, ...number[]] } | JamlBloodstoneTriggerClause & { bloodstoneTrigger: number | [number, ...number[]] } | JamlBloodstoneTriggerClause & { bloodstoneTrigger: "Any" | null; rolls: number | [number, ...number[]] } | JamlAndClause & { and: [JamlClause, ...JamlClause[]] } | JamlAndClause & { and: null; clauses: [JamlClause, ...JamlClause[]] } | JamlOrClause & { or: [JamlClause, ...JamlClause[]] } | JamlOrClause & { or: null; clauses: [JamlClause, ...JamlClause[]] } | JamlLegendarySoulOrderClause & Required<Pick<JamlLegendarySoulOrderClause, "soulOrder">> | JamlLegendarySoulOrderClause & Required<Pick<JamlLegendarySoulOrderClause, "soulOrders">>;
