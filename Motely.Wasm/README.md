# motely-wasm

The Motely engine in one JS package, compiled by NativeAOT-LLVM through
[Bootsharp](https://bootsharp.com). JS gets the engine's own API: the same classes, methods and
types the CLI uses, with no wrapper layer in between.

```js
import bootsharp, { JamlConfigLoader, JamlSearchBuilder, MotelyJamlyzer, CancellationToken } from "motely-wasm";

await bootsharp.boot();

JamlConfigLoader.check(text);                       // null, or the reason with its line
const config = JamlConfigLoader.fromJaml(text);     // the engine's JamlConfig, as a plain object

const token = new CancellationToken();
const search = JamlSearchBuilder.createSettings(config)
  .withThreadCount(1)
  .withSequentialSearch()
  .withSeedMatchCallback((seed) => console.log(seed))
  .withSeedAnalyzeProvider(MotelyJamlyzer.createRiderDesc(config, (r) => console.log(r.antes)))
  .start(token);                                    // token.cancel() stops it
await search.waitForCompletionAsync();
search.dispose();                                   // frees the search's native buffers

MotelyJamlyzer.analyze(config);                     // one result per seed in the config's `seeds:` list
```

## JAML in TypeScript

A filter is plain JSON, typed by `motely-wasm/jaml`: `JamlFilter`, `JamlClause`, every clause and
source block, and every enum as its canonical names (`MotelyJokerName`, `MotelyDeckName`, …).
The JSON Schema ships beside them as `motely-wasm/jaml/schema.json`, and `motely-wasm/jaml/vocab.json`
lists the clause keys, the members each clause accepts and every enum's names, for an editor to offer.

```ts
import type { JamlFilter } from "motely-wasm/jaml";

const filter: JamlFilter = {
  name: "Perkeo",
  deck: "Ghost",
  must: [{ legendaryJoker: "Perkeo", antes: [1], sources: { arcanaPacks: [1], spectralPacks: [1] } }],
};

JamlConfigLoader.check(JSON.stringify(filter));      // the loader takes JSON as it takes YAML
const fromFile: JamlFilter = JSON.parse(JamlConfigLoader.toJson(yamlText)); // a .yaml filter as JSON
const yaml = JamlConfigLoader.toYaml(JSON.stringify(filter));                // and back, to show or save
```

`toJson` keeps the document as written (keys, order, values), so the loader reads the JSON as it
reads the YAML. A number stays a number only when it is spelled as JSON spells one, digits kept;
YAML's other spellings (`010`, `0x1F`, `+5`) become strings, which the loader reads as it reads
them unquoted (`010` is 8). `JSON.parse` still reads a number as a double, so a seed written
unquoted as `99E99` arrives in JavaScript as `9.9e+100`: quote a seed that reads as a number.
Comments do not survive, since JSON has none. Either direction throws on what it cannot carry (a
second document, nesting past 64 levels, a tag or key JSON would drop, aliases that expand past
100,000 values, an unpaired surrogate), and `Errors.last()` names the line.

## The Jamlyzer's shape

- `MotelyJamlyzer.defaultShopSlots(ante)` and `MotelyJamlyzer.packCount(ante)` are the shop depth
  (15 for antes 0–1, 50 beyond) and pack count (4 for ante 1, 6 for every other ante, ante 0
  included) every result carries. Pack indices in `sources:` count these packs.
- `result.legendaryOrder` is every Soul of the run in the order it is opened: its ante, its pack
  slot, the legendary and its edition, and `item`, that legendary as a `MotelyItem` like every
  shop and pack card.
- `result.streamStates` is where every stream stands when the window ends: a `double` per event
  stream, and under `antes` a typed state per shop, pull and shop-source stream of each ante.
  Hand it back to `resume` unchanged and the next window reads on from there. Nothing is rolled
  again, so a page costs the same at any depth and the scroll has no end; `eventRolls` and
  `shopSlots` bound one call (10,000 and 100,000), not the scroll. `rollOffset`, `shopOffset`
  and `shopDefaultWindows` only count how far it has gone.
- `analyze`, `resume` and `createRiderDesc` take an optional last argument `with`, the engine's
  `MotelyWith` as a plain object with all three of `luck`, `jokers` and `vouchers`. The two lists
  are `Int32Array`s of the engine's enum values, which is how Bootsharp types an enum array. It
  is a clause's `with:` block: the vouchers build each ante's shop, luck weighs the luck events,
  held legendaries leave the Soul pool (Showman keeps them in). A resumed scroll takes the same
  `with` it started under.

  ```js
  MotelyJamlyzer.analyze(config, 20, 0, {
    luck: MotelyLuck.X1,
    jokers: new Int32Array(),
    vouchers: Int32Array.of(MotelyVoucher.TarotMerchant),
  });
  ```

## How the engine crosses

- Records (`JamlConfig`, `MotelyJamlyzerSeedResult`, `MotelyProgress`, ...) cross by value as
  plain objects. Classes and interfaces (`IMotelySearchSettings`, `IMotelySearch`, clauses,
  filter descs, the Jamlyzer rider) cross by reference. `long` is `bigint`.
- Members whose signature holds a ref, a pointer or a ref struct are erased by one rule in
  `Program.cs` (`Names`): that's the SIMD plumbing, which has no JS form. Their types still cross
  as handles, so an engine-made filter desc or rider moves between engine calls as itself.
  `Specializations.cs` makes a JS-made SIMD hook throw instead of pretending to run.
- `withJimmolate` and `withSeedGenerator` are erased. The engine documents both as native-only:
  a per-seed predicate would cross once per seed, and a lazy sequence has no value to serialize
  (`withSeedList` is the crossing form). `awaitCompletion` is erased too: it blocks the thread the
  search runs on, which in the browser would hang the page.
- A search holds native buffers until `dispose()` (otherwise until the GC finalizes it). Dispose
  every search you start; disposing a running one stops it and settles `waitForCompletionAsync`.
- `withSeedList` runs the whole list inside `start()`, with no yield to the event loop. Keep
  lists small in a page, or use a sequential/keyword search, which yields between batches.
- `MotelyProgress.percentComplete` is 0-100.
- The NativeAOT runtime gives a thrown C# exception to JS as "C# exception from NativeAOT",
  without its message. After any call throws (a filter that does not load, a rejected `with*`
  value, a `start` that fails), `Errors.last()` returns its message. `JamlConfigLoader.check`
  returns the loader's reason without throwing.

## Hosts

Any current browser, and Node with WebAssembly. The package decodes its embedded assemblies with
`Uint8Array.fromBase64`, which only newer runtimes have natively, so the entry module installs a
fallback when it is missing (`Buffer` on Node, `atob` in browsers). Nothing to shim yourself.
No COOP/COEP headers and no SharedArrayBuffer are needed.

## Build

```
dotnet workload install wasm-tools
npm install                      # @rewaffle/bootsharp-file-system, for the folder tests
npm run build                    # dotnet publish -c Release into bin/motely-wasm, then the base64 fallback
npm test                         # node --test "tests/*.test.mjs"
```

`npm run build` is `dotnet publish` plus `scripts/patch-dist-base64-polyfill.mjs`; a bare
`dotnet publish` leaves the fallback out. `npm pack` and `npm publish` run the build first.

Each GitHub release from 32.0.1 on carries `motely-wasm-<version>.tgz`: the package CI built
with `wasm-opt`, tested and packed at the tag's commit. `npm publish motely-wasm-<version>.tgz`
publishes that file as it is; npm runs no script for a tarball, so nothing is built.

macOS: NativeAOT-LLVM publishes no `runtime.osx-arm64.Microsoft.DotNet.ILCompiler.LLVM` on any
public feed (Bootsharp 0.9 bundles only the win/linux host compilers), so `dotnet publish` can't
run the AOT step on a Mac — CI publishes on ubuntu-latest and uploads `bin/motely-wasm` as an
artifact. To develop the web app on macOS, drop the published npm package's `bin/motely-wasm`
into `Motely.Wasm/bin/` (matching version; the `jaml/` types in the repo are identical to the
published ones).

`jaml/index.d.ts`, `jaml/motely-filter.schema.json` and `jaml/vocab.json` are generated from the
filter loader and never edited. After a change to a clause class, a rule or a description, run
this from the repository root and commit what it writes:

```sh
dotnet run --project Motely.CLI -- schema
```

`dotnet test Motely.Tests` fails while a committed file differs from what the command writes.

Install [Binaryen](https://github.com/WebAssembly/binaryen/releases) **version 130** (131 at
most) and put `wasm-opt` on PATH first. Bootsharp runs it on every Release publish; when it's
missing it logs a warning and ships an unoptimized binary. CI installs 130 and fails the build
when `wasm-opt` did not run. Binaryen 132 and later write the
"compact imports" encoding under the `--all-features` flag Bootsharp passes, and no engine
loads that yet (Node: `unknown import kind 0x7f`).

The default build includes `JamlFiles` (folder access through Bootsharp.FileSystem). That package
is not on nuget.org: add the rewaffle sponsor feed as a `rewaffle` source in your user-level
NuGet.Config (the repo's nuget.config already maps `Bootsharp.FileSystem` to it). JS then calls
`fs.init(IFileMounter)` (`fs` from `@rewaffle/bootsharp-file-system`, `IFileMounter` from
`motely-wasm/bootsharp/file-system`) before `boot()`.

Without the feed, build with the flag off:

```
npm run build:nofs
```

`JamlFiles` keeps the same exports there: `isSupported()` is false, `list()` is empty, and the
file calls reject.

The engine's JAML loader is YamlDotNet's static (source-generated) deserializer through the
Vecc generator (`Vecc.YamlDotNet.Analyzers.StaticGenerator`): no reflection, so NativeAOT
trims it cleanly.
