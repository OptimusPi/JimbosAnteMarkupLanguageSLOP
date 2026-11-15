// Seed Pile check (run: node scripts/seed-pile-check.mjs).
//
// Runs the pile's plain logic (src/seedPile.ts) against the real motely-wasm engine: the same
// request the page sends, analyzed by the same WASM code, and the same "Spotted" matching.

import assert from "node:assert/strict";
import bootsharp, { JamlConfigLoader, MotelyJamlyzer } from "motely-wasm";

const { seedsIn, analyzeRequest, watchNames, spot } = await import("../src/seedPile.ts");

// ── Reading a pasted pile ─────────────────────────────────────────────────────
assert.deepEqual(seedsIn("ALEEB\nPIROCKS\n\nWEEJOKER\n"), ["ALEEB", "PIROCKS", "WEEJOKER"], "one per line");
assert.deepEqual(seedsIn("aleeb pirocks, weejoker"), ["ALEEB", "PIROCKS", "WEEJOKER"], "a row, any case");
assert.deepEqual(
  seedsIn("seed,score,Blueprint\n1JHBI111, 41, 0\nFVSII111,32,1\n"),
  ["1JHBI111", "FVSII111"],
  "a motely CSV gives its first column, not its scores",
);
assert.deepEqual(seedsIn("ALEEB\t41\t0\nPIROCKS;7;1"), ["ALEEB", "PIROCKS"], "tabs and semicolons split a results row too");
assert.deepEqual(seedsIn("ALEEB 12345678"), ["ALEEB", "12345678"], "a seed made of digits, beside another, is a seed");
assert.deepEqual(seedsIn("ALEEB,12345678"), ["ALEEB", "12345678"], "eight digits is a seed, not a score");
assert.deepEqual(
  seedsIn("seed,score,count\nALEEB,12345678,1\nPIROCKS,99999999,0"),
  ["ALEEB", "PIROCKS"],
  "under a results header, a long score is still a score",
);
assert.deepEqual(seedsIn("SEED"), ["SEED"], "the word SEED alone is a seed");
assert.deepEqual(seedsIn("SEED ALEEB"), ["SEED", "ALEEB"], "and so is SEED beside another");
assert.deepEqual(seedsIn("ALEEB\nALEEB\nPIROCKS"), ["ALEEB", "PIROCKS"], "repeats count once");
assert.deepEqual(seedsIn("AB0CD"), ["ABOCD"], "a typed zero is the letter O");
assert.deepEqual(seedsIn("TOOLONGSEED ok! ALEEB"), ["ALEEB"], "what cannot be a seed is left out");
assert.deepEqual(seedsIn(""), [], "nothing pasted, nothing found");

// ── The watch list ────────────────────────────────────────────────────────────
assert.deepEqual(watchNames("Oops All 6s, Ankh,\n Coupon Tag ,"), ["Oops All 6s", "Ankh", "Coupon Tag"]);

// ── The request, analyzed by the real engine ──────────────────────────────────
assert.equal(analyzeRequest("12345678", "Ghost", "Black"), 'deck: Ghost\nstake: Black\nseeds:\n  - "12345678"\n');

await bootsharp.boot();
const analyze = (seed, deck, stake) => MotelyJamlyzer.analyze(JamlConfigLoader.fromJaml(analyzeRequest(seed, deck, stake)));

const [aleeb] = analyze("ALEEB", "Red", "White");
assert.equal(aleeb.seed, "ALEEB");
assert.ok(aleeb.antes.length >= 8, "at least eight antes come back");

// ALEEB's ante 1 voucher is Magic Trick; the engine's own CI checks the same fact.
assert.deepEqual(
  spot(aleeb, ["magic trick"]).filter((hit) => hit.ante === 1),
  [{ ante: 1, where: "Voucher", name: "Magic Trick" }],
);
assert.deepEqual(spot(aleeb, []), [], "an empty watch list spots nothing");
assert.deepEqual(spot(aleeb, ["No Such Card"]), [], "an unknown name spots nothing");

// Known finds on ALEEB, one for each place a watched name can sit. Each is asserted as present,
// so a branch of spot() that stops reporting fails here.
const early = (names) => spot(aleeb, names).filter((hit) => hit.ante <= 2);
assert.deepEqual(
  early(["Speed Tag"]),
  [
    { ante: 0, where: "Small Blind Skip", name: "Speed Tag" },
    { ante: 0, where: "Big Blind Skip", name: "Speed Tag" },
    { ante: 1, where: "Small Blind Skip", name: "Speed Tag" },
    { ante: 1, where: "Big Blind Skip", name: "Speed Tag" },
  ],
  "skip tags, both blinds",
);
assert.deepEqual(early(["speed"]), early(["Speed Tag"]), 'a tag is spotted with or without the word "tag"');
assert.deepEqual(early(["Blueprint"]), [{ ante: 2, where: "Shop 8", name: "Blueprint" }], "a shop slot");
assert.deepEqual(
  early(["Temperance"]),
  [
    { ante: 0, where: "Pack 1, Jumbo Arcana", name: "Temperance" },
    { ante: 1, where: "Pack 2, Arcana", name: "Temperance" },
  ],
  "packs, by number and kind",
);

// A seed made of digits, and one that reads as a YAML word, both analyze as seeds.
assert.equal(analyze("12345678", "Red", "White")[0].seed, "12345678");
assert.equal(analyze("NULL", "Red", "White")[0].seed, "NULL");

// Deck and stake reach the engine: the request for another deck is another request.
assert.notEqual(analyzeRequest("ALEEB", "Ghost", "White"), analyzeRequest("ALEEB", "Red", "White"));
assert.equal(analyze("ALEEB", "Ghost", "Black")[0].seed, "ALEEB");

console.log("seed pile: pasted piles read, requests analyze on the real engine, watched names are spotted");
process.exit(0);
