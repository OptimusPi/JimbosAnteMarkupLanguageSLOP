# MotelyJAML

Balatro seed search. You write a filter in JAML (YAML-shaped), and a SIMD engine sweeps seeds against it.

| Project | What it is |
|---|---|
| `Motely` | The engine and the JAML filter loader |
| `Motely.CLI` | The command-line search. The build names it `Motely.CLI`; the examples below call it `motely` |
| `Motely.Tests` | The test suite |
| `Motely.Wasm` | Browser build via WebAssembly |
| `Motely.Web` | The seed lab, a web app on `motely-wasm` and `jaml-ui` |
| `vscode-jaml` | The JAML extension for VS Code, as a packaged `.vsix` |
| `Filters/` | Filter files |
| `docs/` | Release notes |

## Filters

A filter lists `must`, `should` and `mustNot` clauses. Each clause is a mapping whose key is the clause type (`joker`, `rareJoker`, `legendaryJoker`, `voucher`, `boss`, `and`, `or`, …), and the loader builds the matching class from that key. The type may also be written as a tag on the clause (`!joker`). A clause with neither does not load.

```yaml
must:
  - rareJoker: Blueprint
    antes: [1, 2, 3, 4]
  - or:
    mode: max
    clauses:
      - joker: Blueprint
      - joker: Brainstorm
should:
  - boss: The Eye
    antes: [3]
```

`and` and `or` hold their children as a list, either directly (`or: [...]`) or under `clauses:`. A filter written as JSON loads the same way.

Some keys and names have a second spelling that loads the same clause:

| Written | Means |
|---|---|
| `ante: 2` | `antes: [2]` (any list of numbers may be one number) |
| `requireMega: true` | `requireMegaPack: true` (under `sources:`) |
| `soulJoker: Perkeo` | `legendaryJoker: Perkeo` |
| `voucher: Reroll Surplus`, `boss: The Hook`, `tarotCard: Fool` | `RerollSurplus`, `TheHook`, `TheFool`: any case, spaces optional, "The" optional, when exactly one name fits |
| `rank: K` | `rank: King` (J, Q, K, A) |
| `dateCreated: 2025-12-25` | metadata, kept and ignored by the search |

**What a clause looks at without `sources:`.** A joker, tarot, planet or spectral clause reads the first eight shop cards of each ante (`shopItems: [0..7]`, the opening shop and about three rerolls); spectral also reads every pack, the others don't. A `joker` clause that names a legendary, or names no joker at all, also counts legendaries as a `legendaryJoker` clause does. A `standardCard` clause reads packs only until it names shop slots. The analyzer (`MotelyJamlyzer`) lists 15 shop cards for antes 0–1 and 50 after, so a card it shows at position 9 or later scores 0 unless the clause names the slot: the 11th card is `sources: { shopItems: [10] }`.

**What the player holds: `with:`.** A clause can say what the player has while it is read. The block is a mapping; each clause type reads the keys that change it and ignores the others.

```yaml
must:
  - voucher: TarotMerchant
    antes: [1]
  - tarotCard: Death
    antes: [2, 3]
    with:
      vouchers: [TarotMerchant]
```

| Key | Read by | What it changes |
|---|---|---|
| `vouchers: [...]` | `joker`, `commonJoker`, `uncommonJoker`, `rareJoker`, `tarotCard`, `planetCard`, `spectralCard`, `standardCard` | The shop the clause reads is rolled with these vouchers redeemed on top of the deck's own. Tarot Merchant/Tycoon and Planet Merchant/Tycoon change how often tarots and planets appear, which moves every other card's slot too. Magic Trick adds playing cards. No other voucher changes the roll: Hone and Glow Up, for one, do nothing here |
| `jokers: [...]` | `legendaryJoker`, `soulOrder` | A held legendary is out of the Soul's pool; holding `Showman` keeps repeats in |
| `luck: X2` | `luckyMoney`, `luckyMult`, `wheelOfFortune`, `wheelStaysFlipped`, `glassDestroy`, `cavendishExtinct`, `grosMichelExtinct`, `spaceLevelup` | The Oops! All 6s multiplier on the odds (X1, X2, X4, X5, X8, X16, X32, X64, or the number) |

Without `with:` a shop clause reads the shop the deck alone gives, whatever vouchers the seed offers. `with:` on any other clause is an unknown property, and `with: ~` does not load. The analyzer takes the same block as an argument (`MotelyJamlyzer.Analyze(config, with: ...)`) and then shows the shop, Souls and luck events a clause with that `with:` reads.

A wrong key or name fails to load with one line, on its own line of the file. An unknown property is named (`Property 'luckMoney' not found`). A name of the wrong kind says what it is not (`'Showman' is not a common joker`). `shopItems:` written beside `joker:` is an unknown property; it belongs under `sources:`.

Limits, each a load error: `*name` aliases aren't read, so write the value out where it's used. A key written twice in one mapping is refused. Every number in a list (`antes`, `rolls`, `shopItems`, `boosterPacks`, other source slots, event rolls) runs from 0 to 999. Antes run from 0 to 39. `min` and `max` are 0 or more, and `max` is not below `min`. An `and` takes no `min` or `max`, and an `or` takes no `max` and needs `min: 1` or more. An `and` or `or` needs at least one clause, and a filter takes at most 256 `should:` clauses. Event, voucher and tag `rolls` need at least one roll, and boss `antes` run from 1.

## CLI

```sh
dotnet run --project Motely.CLI -- --jaml <filter> [mode] [options]
```

`<filter>` is a path, or a bare name that is looked up as `Filters/<name>.yaml` under the current directory.

stdout carries only results (CSV: `seed,score,<tallies>`). Progress, warnings and the summary go to stderr, so `> out.csv` saves a clean file.
Exit codes: `0` finished, `1` bad input, `130` interrupted (the resume point is printed).

**Choosing seeds** (pick one mode; sequential is the default):

| Flag | Searches |
|---|---|
| *(none)* | Sequential sweep of every seed. Resume with `--startBatch`/`--startSeed`/`--startPercent`, end with `--endBatch`/`--stopSeed`. `--batchCharCount` sets how many trailing characters one batch sweeps (default 3) |
| `--seeds A1B2C3D4,E5F6G7H8` | Exactly these seeds |
| `--seeds file.csv` | Every seed in a file: one per line, first column. A motely CSV, a plain list, or a spreadsheet export all work. Repeats run once, and bad lines are warned about and skipped. Up to 65,536 seeds print in file order; a longer list is split across the threads and prints as found |
| `--seeds -` | Seeds read from stdin |
| `--replay` | The filter file's own `seeds:` block |
| `--keyword WORD` / `--keywords A,B` | Seeds that contain the word(s). `--padding` names the characters that fill the other slots (default: all of 1-9 and A-Z) |
| `--random N` | N random seeds |
| `--aesthetic NAME\|all` | Pattern families such as palindromes |

**Other options:** `--deck`, `--stake` (override the filter's values), `--threads`, `--collect N` (stop after N matches), `--cutoff N|auto|max|off`, `--save` (merge the printed seeds into the filter's `seeds:` block), `-q`.

**Cutoff.** `--cutoff N` prints seeds that score at least N. `auto`, the default, prints every match until finds flood in, then only seeds that tie or beat the best score so far. `max` prints only a seed that beats every score before it. `off` prints every match. A filter's own top-level `cutoff: N` is the floor when the flag is not given. Only `should` clauses score, so a filter without them scores every seed 0 and a cutoff above zero prints nothing.

### Researching a pile of seeds

There is no database. A seed list is just a file, and replaying one takes milliseconds, so keep the CSVs and re-run filters over them:

```sh
motely --jaml longColaDanker --cutoff off > pile.csv       # collect (Ctrl+C whenever)
motely --jaml otherFilter --seeds pile.csv --cutoff off    # re-score the pile with another filter
motely --jaml a --seeds pile.csv -q | motely --jaml b --seeds - -q   # chain filters
motely --jaml longColaDanker --seeds pile.csv --save       # keep the winners in the filter
```

## Build and test

```sh
dotnet test Motely.Tests       # builds the engine, the CLI and the suite, and runs it
dotnet build Motely.slnx       # every project, Motely.Wasm included
```

`Motely.slnx` holds `Motely.Wasm`, which needs `dotnet workload install wasm-tools` and, for its folder access, the Bootsharp.FileSystem sponsor feed. Without the feed, add `-p:MotelyFileSystem=false`. `Motely.Wasm/README.md` has the rest.
