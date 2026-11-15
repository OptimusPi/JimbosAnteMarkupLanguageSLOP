// The engine's own API through the published package, under Node.
// Usage: npm test   (after `dotnet publish -c Release` in Motely.Wasm)
import test from "node:test";
import assert from "node:assert/strict";
import bootsharp, {
  JamlConfigLoader,
  JamlSearchBuilder,
  MotelyJamlyzer,
  MotelyDeck,
  MotelyVoucher,
  MotelyItemType,
  MotelyItemTypeCategory,
  MotelyLuck,
  MotelyItemEdition,
  MotelyJoker,
  MotelyJokerRarity,
  MotelyJokers,
  MotelyStandardcardRank,
  MotelyStandardcardSuit,
  CancellationToken,
  Errors,
} from "../bin/motely-wasm/index.mjs";

await bootsharp.boot();

const telescope = "name: smoke\ndeck: Red\nstake: White\nmust:\n  - voucher: Telescope\n    antes: [1]\n";

test("a filter loads into the engine's config", () => {
  const config = JamlConfigLoader.fromJaml(telescope);
  assert.equal(config.name, "smoke");
  assert.equal(config.deck, MotelyDeck.Red);
  assert.equal(config.must.length, 1);
});

test("a filter that does not load says why, with its line", () => {
  const bad = "name: x\nmust:\n  - joker: Blueprint\n    antes: [1\n";
  Errors.last();
  assert.throws(() => JamlConfigLoader.fromJaml(bad));
  assert.match(Errors.last() ?? "", /line \d+/i, "the thrown reason is kept on Errors.last()");
  assert.match(JamlConfigLoader.check(bad), /line \d+/i);
  assert.equal(JamlConfigLoader.check(telescope), null);
});

test("a value the engine cannot run is refused at its line", () => {
  // Was `min: 0`, refused by JamlValidator until 3227635 removed it; the engine runs `min: 0` as
  // the "at least zero" it says (every seed matches). A number where a name goes is refused.
  assert.match(JamlConfigLoader.check("must:\n  - joker: 5\n"), /JAML line 2: '5' is not a name/);
});

test("soulOrder: entries: takes the same check as soulOrder:", () => {
  const written = (entries) => `must:\n  - soulOrder: [{ legendary: Perkeo }]\n    entries: ${entries}\n`;
  assert.match(JamlConfigLoader.check(written("[{}]")), /not a legendary/);
  assert.match(JamlConfigLoader.check(written("[{ legendary: Blueprint }]")), /not a legendary/);
  assert.match(JamlConfigLoader.check(written("[]")), /at least one soul/);
  assert.equal(JamlConfigLoader.check(written("[{ legendary: Yorick }]")), null);
});

test("a key written twice is refused on its line", () => {
  assert.equal(
    JamlConfigLoader.check("must:\n  - joker: Blueprint\n    antes: [1]\n    antes: [2]\n"),
    "JAML line 4: Encountered duplicate key antes",
  );
});

test("stickers: loads as one name, a list, Any or null, as jaml/index.d.ts types it", () => {
  for (const written of ["Eternal", "[Eternal, Rental]", "Any", "~"])
    assert.equal(JamlConfigLoader.check(`should:\n  - joker: Blueprint\n    stickers: ${written}\n`), null, written);
});

test("entries: takes the soulOrder check: a soul needs its legendary", () => {
  const clause = (entries) => `must:\n  - soulOrder:\n      - legendary: Perkeo\n    entries: ${entries}\n`;
  assert.match(JamlConfigLoader.check(clause("[{}]")), /not a legendary/);
  assert.match(JamlConfigLoader.check(clause("[]")), /at least one soul/);
  assert.equal(JamlConfigLoader.check(clause("[{ legendary: Chicot }]")), null);
});

test("a seed list is searched once each, with one callback per find", async () => {
  const finds = [];
  const search = JamlSearchBuilder.createSettings(JamlConfigLoader.fromJaml(telescope))
    .withThreadCount(1)
    .withQuietMode(true)
    .withSeedList(["ALEEB", "AAAAAAAA", "PIROCKS"])
    .withSeedMatchCallback((seed) => finds.push(seed))
    .start();
  await search.waitForCompletionAsync();
  assert.equal(search.totalSeedsSearched, 3n);
  assert.equal(BigInt(finds.length), search.matchingSeeds);
  assert.equal(new Set(finds).size, finds.length, "a find arrives once");
});

test("a sequential slice sweeps its batches and finds seeds", async () => {
  const finds = [];
  const search = JamlSearchBuilder.createSettings(JamlConfigLoader.fromJaml(telescope))
    .withThreadCount(1)
    .withQuietMode(true)
    .withSequentialSearch()
    .withBatchCharacterCount(3)
    .withEndBatchIndex(2n)
    .withSeedMatchCallback((seed) => finds.push(seed))
    .start();
  await search.waitForCompletionAsync();
  assert.ok(search.isCompleted && search.totalSeedsSearched > 0n);
  assert.ok(finds.length > 0, "a Telescope ante-1 slice must find seeds");
});

test("cancelling stops an open-ended sweep", async () => {
  const token = new CancellationToken();
  const search = JamlSearchBuilder.createSettings(JamlConfigLoader.fromJaml(telescope))
    .withThreadCount(1)
    .withQuietMode(true)
    .withSequentialSearch()
    .withBatchCharacterCount(2)
    .start(token);
  const done = search.waitForCompletionAsync();
  token.cancel();
  await done;
  assert.ok(search.totalSeedsSearched < 35n ** 8n, "cancel stops the sweep");
});

test("a search can be disposed, and has no blocking wait", async () => {
  const progress = [];
  const search = JamlSearchBuilder.createSettings(JamlConfigLoader.fromJaml(telescope))
    .withThreadCount(1)
    .withQuietMode(true)
    .withSequentialSearch()
    .withBatchCharacterCount(2)
    .withProgressReportIntervalMs(0n)
    .withProgressCallback((p) => progress.push(p.percentComplete))
    .start();
  assert.equal(typeof search.dispose, "function");
  assert.equal(search.awaitCompletion, undefined, "a blocking wait would hang the page");
  const done = search.waitForCompletionAsync();
  await new Promise((resolve) => setTimeout(resolve, 20));
  search.dispose();
  await done;
  assert.ok(search.totalSeedsSearched < 35n ** 8n, "dispose stops the sweep");
  for (const pct of progress) assert.ok(pct >= 0 && pct <= 100, `percentComplete is 0-100, got ${pct}`);
});

test("the filter's own cutoff is the floor unless the host gives one", async () => {
  const scored = "name: s\ncutoff: 1000\nmust:\n  - voucher: Telescope\n    antes: [1]\nshould:\n  - voucher: Telescope\n    antes: [1]\n";
  const config = JamlConfigLoader.fromJaml(scored);

  const withOwn = [];
  const own = JamlSearchBuilder.createSettings(config)
    .withThreadCount(1)
    .withQuietMode(true)
    .withSeedList(["P1111111"])
    .withScoredResultCallback((r) => withOwn.push(r.seed))
    .start();
  await own.waitForCompletionAsync();
  assert.deepEqual(withOwn, [], "a floor of 1000 drops a seed scoring 1");

  const withHost = [];
  const host = JamlSearchBuilder.createSettings(config, -2147483648)
    .withThreadCount(1)
    .withQuietMode(true)
    .withSeedList(["P1111111"])
    .withScoredResultCallback((r) => withHost.push(r.seed))
    .start();
  await host.waitForCompletionAsync();
  assert.deepEqual(withHost, ["P1111111"], "no floor reports the seed");
});

test("a rejected setting and a rejected range say why", () => {
  const sequential = JamlSearchBuilder.createSettings(JamlConfigLoader.fromJaml(telescope)).withThreadCount(1).withSequentialSearch();
  Errors.last();
  assert.throws(() => sequential.withBatchCharacterCount(99));
  assert.match(Errors.last() ?? "", /Batch character count must be 1-/);

  const backwards = JamlSearchBuilder.createSettings(JamlConfigLoader.fromJaml(telescope))
    .withThreadCount(1)
    .withSequentialSearch()
    .withBatchCharacterCount(3)
    .withStartBatchIndex(5n)
    .withEndBatchIndex(2n);
  Errors.last();
  assert.throws(() => backwards.start());
  assert.match(Errors.last() ?? "", /End batch \(exclusive\) is before start batch/);
});

test("createSettings leaves the config alone", () => {
  const config = JamlConfigLoader.fromJaml("name: o\nseeds: [ALEEB]\nmust:\n  - joker: Blueprint\n");
  const before = MotelyJamlyzer.analyze(config)[0].antes.map((a) => a.ante).join();
  JamlSearchBuilder.createSettings(config);
  assert.equal(MotelyJamlyzer.analyze(config)[0].antes.map((a) => a.ante).join(), before);
});

test("the Jamlyzer walks a seed, and resumes from where it stopped", () => {
  const config = JamlConfigLoader.fromJaml(telescope + "seeds: [ALEEB]\n");
  const first = MotelyJamlyzer.analyze(config);
  assert.equal(first.length, 1);
  assert.equal(first[0].seed, "ALEEB");
  const next = MotelyJamlyzer.resume(config, first[0].streamStates, 20);
  assert.equal(next.length, 1);
  assert.ok(next[0].streamStates.rollOffset > first[0].streamStates.rollOffset, "resume advances");
});

test("with: vouchers on a clause and the same with on the Jamlyzer read one shop", () => {
  // PIFREAKS, Red: 3 tarots in the first eight ante-1 shop slots, 6 with Tarot Tycoon held
  // (Motely.Tests JamlyzerWithTests pins both).
  const jaml =
    "name: w\ndeck: Red\nseeds: [PIFREAKS]\nshould:\n  - tarotCard: any\n    antes: [1]\n" +
    "    sources: { shopItems: [0,1,2,3,4,5,6,7] }\n    with: { vouchers: [TarotMerchant, TarotTycoon] }\n";
  assert.equal(JamlConfigLoader.check(jaml), null);
  assert.match(JamlConfigLoader.check(jaml.replace("{ vouchers: [TarotMerchant, TarotTycoon] }", "~")), /with: needs a mapping/);
  const config = JamlConfigLoader.fromJaml(jaml);
  const held = {
    luck: MotelyLuck.X1,
    jokers: new Int32Array(),
    vouchers: Int32Array.of(MotelyVoucher.TarotMerchant, MotelyVoucher.TarotTycoon),
  };
  const tarots = (result) =>
    result.antes
      .find((a) => a.ante === 1)
      .shopItems.slice(0, 8)
      .filter((item) => item.typeCategory === MotelyItemTypeCategory.TarotCard).length;

  const [plain] = MotelyJamlyzer.analyze(config, 0);
  const [shown] = MotelyJamlyzer.analyze(config, 0, 0, held);
  assert.equal(tarots(plain), 3, "no with: the seed's own shop");
  assert.equal(tarots(shown), 6, "the shop the vouchers build");
  assert.equal(shown.tally[0], 6, "which is the shop the clause counted");
});

test("stream states cross as typed values, and a resumed window continues every stream", () => {
  const config = JamlConfigLoader.fromJaml(telescope + "seeds: [ALEEB]\n");
  const [full] = MotelyJamlyzer.analyze(config, 20, 20);
  const [first] = MotelyJamlyzer.analyze(config, 10, 10);
  const [second] = MotelyJamlyzer.resume(config, first.streamStates, 10, 10);

  const ante = first.streamStates.antes[0];
  assert.equal(typeof ante.shop.itemType, "number");
  assert.equal(typeof ante.shop.jokers.rarity, "number");
  assert.equal(typeof ante.pulls.emperorTarots.cards.initial, "number");
  assert.ok(ante.pulls.voucherSequence.resamples.length >= 0);

  const values = (items) => items.map((item) => item.value);
  for (let i = 0; i < full.antes.length; i++) {
    const a = first.antes[i];
    const b = second.antes[i];
    assert.deepEqual(values(full.antes[i].shopItems), values([...a.shopItems, ...b.shopItems]));
    assert.deepEqual(
      values(full.antes[i].pulls.emperorTarots),
      values([...a.pulls.emperorTarots, ...b.pulls.emperorTarots]),
    );
    assert.deepEqual(
      values(full.antes[i].shopStreams.shopJokers),
      values([...a.shopStreams.shopJokers, ...b.shopStreams.shopJokers]),
    );
  }
  assert.deepEqual(second.streamStates, full.streamStates, "two halves end where one window ends");
});

test("the Jamlyzer rides a search: each find arrives with its antes", async () => {
  const analyzed = [];
  const config = JamlConfigLoader.fromJaml(telescope);
  const search = JamlSearchBuilder.createSettings(config)
    .withThreadCount(1)
    .withQuietMode(true)
    .withSeedList(["ALEEB", "PIROCKS"])
    .withSeedAnalyzeProvider(MotelyJamlyzer.createRiderDesc(config, (r) => analyzed.push(r), 0))
    .start();
  await search.waitForCompletionAsync();
  assert.equal(BigInt(analyzed.length), search.matchingSeeds);
  for (const result of analyzed) assert.ok(result.antes.length > 0);
});

test("a filter goes YAML → JSON → YAML through the engine, and the JSON loads", () => {
  const json = JamlConfigLoader.toJson(telescope);
  assert.deepEqual(JSON.parse(json), {
    name: "smoke",
    deck: "Red",
    stake: "White",
    must: [{ voucher: "Telescope", antes: [1] }],
  });
  assert.equal(JamlConfigLoader.check(json), null);
  assert.equal(JamlConfigLoader.toJson(JamlConfigLoader.toYaml(json)), json);
});

test("deep nesting is refused at its line, and the module lives on", () => {
  // 5,000 levels overflowed the stack in YamlDotNet's node building and killed the module.
  const deep = "name: d\nmust:\n  - joker: Joker\n    antes: " + "[".repeat(100_000) + "]".repeat(100_000) + "\n";
  Errors.last();
  assert.throws(() => JamlConfigLoader.toJson(deep));
  assert.match(Errors.last() ?? "", /^JAML line 4: the document nests deeper than 64 levels/);
  assert.match(JamlConfigLoader.check(deep) ?? "", /^JAML line 4: the document nests deeper than 64 levels/);
  assert.match(JamlConfigLoader.check(JamlConfigLoader.toJson("x: " + "[".repeat(63) + "]".repeat(63) + "\n")) ?? "", /Property 'x' not found/);
  assert.equal(JamlConfigLoader.check(telescope), null);
});

test("toJson keeps a seed that looks like a number as written", () => {
  const json = JamlConfigLoader.toJson("name: s\nseeds: [99E99, 11E11111, 010]\nmust:\n  - joker: Joker\n");
  assert.match(json, /"seeds": \[\n    99E99,\n    11E11111,\n    "010"\n  \]/);
  assert.deepEqual(JamlConfigLoader.fromJaml(json).seeds, ["99E99", "11E11111", "010"]);
});

test("a legendary from the Jamlyzer is an item like every other card", () => {
  // C3DW94B8 (Red/White) holds one ante-1 Soul (Motely.Tests JAMLyzerUnitTests pins its slot).
  const result = MotelyJamlyzer.analyze(JamlConfigLoader.fromJaml("name: soul\nseeds: [C3DW94B8]\n"), 1)[0];
  const [legendary] = result.legendaryOrder;
  assert.equal(legendary.ante, 1);
  assert.equal(MotelyItemType[legendary.item.type], MotelyJoker[legendary.joker]);
  assert.equal(legendary.item.edition, legendary.edition);
  assert.equal(MotelyJokers.rarity(legendary.item.type), MotelyJokerRarity.Legendary);
});

test("an ante's shop depth and pack count are published before any result", () => {
  const result = MotelyJamlyzer.analyze(JamlConfigLoader.fromJaml("name: w\nseeds: [ALEEB]\n"), 1)[0];
  for (const ante of result.antes) {
    assert.equal(ante.shopItems.length, MotelyJamlyzer.defaultShopSlots(ante.ante));
    assert.equal(ante.packs.length, MotelyJamlyzer.packCount(ante.ante));
  }
});

test("joker rarity, as an export and as the same test with no call", () => {
  assert.equal(MotelyJokers.rarity(MotelyItemType.Joker), MotelyJokerRarity.Common);
  assert.equal(MotelyJokers.rarity(MotelyItemType.Blueprint), MotelyJokerRarity.Rare);
  assert.equal(MotelyJokers.rarity(MotelyItemType.Perkeo), MotelyJokerRarity.Legendary);
  assert.equal(MotelyItemType.Perkeo & MotelyJokerRarity.Legendary, MotelyJokerRarity.Legendary);
  assert.equal(MotelyItemType.Blueprint & MotelyJokerRarity.Legendary, MotelyJokerRarity.Rare);
});

test("multi-seed Erratic Deck marshals 52 cards cleanly and matches lua51.dll rank/NaN parity", () => {
  const config = JamlConfigLoader.fromJaml(
    "name: erratic-parity\ndeck: Erratic\nseeds: [ALEEB, PIROCKS, 7LB2WVPK]\nshould:\n  - erraticRank: Ten\n    score: 1\n",
  );
  const results = MotelyJamlyzer.analyze(config, 1);
  assert.equal(results.length, 3);
  for (const r of results) {
    assert.ok(Array.isArray(r.erraticDeck));
    assert.equal(r.erraticDeck.length, 52);
  }
  // ALEEB card #1 (0-based index 1) under G.P_CARDS ASCII order is King of Spades
  assert.equal(results[0].erraticDeck[1].standardcardRank, MotelyStandardcardRank.King);
  assert.equal(results[0].erraticDeck[1].standardcardSuit, MotelyStandardcardSuit.Spades);
  // 7LB2WVPK triggers +QNaN in erratic pseudohash -> all 52 cards are Ten of Spades, scoring 52
  for (const card of results[2].erraticDeck) {
    assert.equal(card.standardcardRank, MotelyStandardcardRank.Ten);
    assert.equal(card.standardcardSuit, MotelyStandardcardSuit.Spades);
  }
  assert.equal(results[2].score, 52);
  assert.ok(results[2].tally && results[2].tally[0] === 52);
});

test("NaN seed S4DM5V37 matches Ante 1 Holographic shop joker and resumes cleanly with NaN parking stream", async () => {
  const jaml = [
    "name: nan-holo",
    "deck: Red",
    "stake: White",
    "seeds: [S4DM5V37]",
    "must:",
    "  - joker: Any",
    "    edition: Holographic",
    "    antes: [1]",
    "    sources:",
    "      shopItems: [0, 1, 2, 3, 4]",
  ].join("\n");
  const config = JamlConfigLoader.fromJaml(jaml);
  const matches = [];
  const search = JamlSearchBuilder.createSettings(config)
    .withThreadCount(1)
    .withQuietMode(true)
    .withSeedList(["S4DM5V37"])
    .withSeedMatchCallback((s) => matches.push(s))
    .start();
  await search.waitForCompletionAsync();
  assert.deepEqual(matches, ["S4DM5V37"]);

  const analyzed = MotelyJamlyzer.analyze(config, 1);
  assert.equal(analyzed.length, 1);
  assert.ok(
    analyzed[0].antes[0].shopItems.slice(0, 5).some((item) => item.edition === MotelyItemEdition.Holographic),
    "first Ante 1 shop joker on S4DM5V37 has Holographic edition",
  );
  assert.ok(Number.isNaN(analyzed[0].streamStates.parking), "parking stream on S4DM5V37 is naturally NaN");
  const resumed = MotelyJamlyzer.resume(config, analyzed[0].streamStates, 10);
  assert.equal(resumed.length, 1);
  assert.ok(resumed[0].streamStates.rollOffset > analyzed[0].streamStates.rollOffset);
});

test("Nebula Deck starts with Telescope so Observatory can appear in Ante 1", async () => {
  const config = JamlConfigLoader.fromJaml(
    "name: nebula-obs\ndeck: Nebula\nstake: White\nmust:\n  - voucher: Observatory\n    antes: [1]\n",
  );
  const finds = [];
  const search = JamlSearchBuilder.createSettings(config)
    .withThreadCount(1)
    .withQuietMode(true)
    .withSequentialSearch()
    .withBatchCharacterCount(2)
    .withEndBatchIndex(1n)
    .withSeedMatchCallback((s) => finds.push(s))
    .start();
  await search.waitForCompletionAsync();
  assert.ok(finds.length > 0, "Nebula Deck must be able to roll Observatory in Ante 1");
  const check = MotelyJamlyzer.analyze(
    JamlConfigLoader.fromJaml(`name: n\ndeck: Nebula\nseeds: [${JSON.stringify(finds[0])}]\n`),
    1,
  )[0];
  const ante1 = check.antes.find((a) => a.ante === 1);
  assert.ok(ante1);
  assert.equal(ante1.voucher, MotelyVoucher.Observatory);
});

test("digit-leading enum names like '8 Ball' parse and search cleanly in WASM", async () => {
  const jaml = 'name: eight-ball\nmust:\n  - joker: "8 Ball"\n    antes: [1]\n';
  assert.equal(JamlConfigLoader.check(jaml), null);
  const config = JamlConfigLoader.fromJaml(jaml);
  const finds = [];
  const search = JamlSearchBuilder.createSettings(config)
    .withThreadCount(1)
    .withQuietMode(true)
    .withSequentialSearch()
    .withBatchCharacterCount(2)
    .withEndBatchIndex(1n)
    .withSeedMatchCallback((s) => finds.push(s))
    .start();
  await search.waitForCompletionAsync();
  assert.ok(finds.length > 0);
});

test("0-character empty seed ('') searches and matches Balatro lua51.dll Erratic Deck in WASM", async () => {
  const erraticCfg = JamlConfigLoader.fromJaml('name: empty-erratic\ndeck: Erratic\nstake: White\nseeds: [""]\n');
  assert.deepEqual(erraticCfg.seeds, [""]);
  const [res] = MotelyJamlyzer.analyze(erraticCfg, 1);
  assert.equal(res.seed, "");
  assert.ok(Array.isArray(res.erraticDeck));
  assert.equal(res.erraticDeck.length, 52);
  // lua51.dll ground truth for seed = "": card[0] = 4 of Hearts, card[1] = 10 of Spades, card[2] = 6 of Spades
  assert.equal(res.erraticDeck[0].standardcardRank, MotelyStandardcardRank.Four);
  assert.equal(res.erraticDeck[0].standardcardSuit, MotelyStandardcardSuit.Hearts);
  assert.equal(res.erraticDeck[1].standardcardRank, MotelyStandardcardRank.Ten);
  assert.equal(res.erraticDeck[1].standardcardSuit, MotelyStandardcardSuit.Spades);
  assert.equal(res.erraticDeck[2].standardcardRank, MotelyStandardcardRank.Six);
  assert.equal(res.erraticDeck[2].standardcardSuit, MotelyStandardcardSuit.Spades);

  // Mixed list with "" + whitespace-only (ignored) + normal seeds through SIMD additional filter
  const tagFilter = JamlConfigLoader.fromJaml(
    "name: empty-coupon\ndeck: Red\nstake: White\nmust:\n  - tag: CouponTag\n    antes: [1]\n",
  );
  const matched = [];
  const search = JamlSearchBuilder.createSettings(tagFilter)
    .withThreadCount(1)
    .withQuietMode(true)
    .withSeedList(["", "   ", "ALEEB"])
    .withSeedMatchCallback((s) => matched.push(s))
    .start();
  await search.waitForCompletionAsync();
  assert.ok(matched.includes(""), "0-character seed '' must match Ante 1 CouponTag in WASM");
  assert.ok(!matched.includes("   "), "whitespace-only string must be ignored");
});

test("MotelyJamlyzer.resume handles quoted/lowercase/0-char/NaN seeds and spread config seed overrides", () => {
  const baseCfg = JamlConfigLoader.fromJaml(
    "name: spread-override\ndeck: Red\nstake: White\nshould:\n  - tag: CouponTag\n    antes: [1]\n    score: 5\n",
  );
  // Overriding seeds on a loaded JamlConfig (used by jaml-ui Jamlyzer when inspecting hits)
  const [emptyAnalyzed] = MotelyJamlyzer.analyze({ ...baseCfg, seeds: [""] }, 5);
  assert.equal(emptyAnalyzed.seed, "");
  assert.equal(emptyAnalyzed.score, 5);
  assert.equal(emptyAnalyzed.tally[0], 1);
  assert.equal(emptyAnalyzed.streamStates.rollOffset, 5);

  const resumedEmpty = MotelyJamlyzer.resume({ ...baseCfg, seeds: ['""'] }, emptyAnalyzed.streamStates, 5);
  assert.equal(resumedEmpty[0].seed, "");
  assert.equal(resumedEmpty[0].streamStates.rollOffset, 10);

  // Quoted NaN seed resume across WASM boundary (S4DM5V37 has naturally NaN parking stream)
  const quotedNanCfg = { ...baseCfg, seeds: ['"s4dm5v37"'] };
  const [nanPage1] = MotelyJamlyzer.analyze(quotedNanCfg, 5);
  assert.equal(nanPage1.seed, "S4DM5V37");
  assert.ok(Number.isNaN(nanPage1.streamStates.parking));
  const [nanPage2] = MotelyJamlyzer.resume(quotedNanCfg, nanPage1.streamStates, 5);
  assert.equal(nanPage2.streamStates.rollOffset, 10);
});
