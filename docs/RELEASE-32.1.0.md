# motely-wasm 32.1.0

`motely-wasm/jaml` is now generated from the loader instead of kept by hand, and gains a
vocabulary file for editors. One loader fix. A filter that loaded on 32.0.3 still loads unless it
is named under "Now refused" below.

## New

- **`motely-wasm/jaml/vocab.json`.** The filter grammar without the value rules, for an editor or
  a form: the root keys, each clause key and the type it loads, each type's members and what each
  one takes (a name, a list of names, numbers, a nested mapping), and every enum's names in the
  engine's order. It is 38 KB against the schema's 270 KB.

  ```js
  import vocab from "motely-wasm/jaml/vocab.json" with { type: "json" };
  vocab.clauses.legendaryJoker;                       // "legendaryJokerClause"
  vocab.types.legendaryJokerClause.legendaryJoker;    // { form: "Names", enum: "MotelyJokerLegendary" }
  vocab.enums.MotelyJokerLegendary;                   // ["Canio", "Triboulet", "Yorick", "Chicot", "Perkeo"]
  ```

## Changed

- **`jaml/index.d.ts` and `jaml/schema.json` are generated.** `Motely.Tests` writes them from what
  the loader binds, and `dotnet test` fails when a committed file is not what the generator
  writes or when the schema and the loader give different verdicts. The exported type names, the
  schema's definitions and every member name are the same as in 32.0.3. The engine's load path
  is not changed by this.
- **The schema now gives the loader's verdict where the hand-kept one did not.** On 16,344
  generated documents the 32.0.3 schema took 318 the loader refuses and refused 182 the loader
  takes; this one differs on none. What moved:
  - Slot lists under `sources:` (`shopItems`, `boosterPacks` and the rest) run from 0 to 999, as
    the loader reads them.
  - An event clause (`luckyMoney`, `wheelOfFortune`, ...) needs at least one roll, on its key or
    under `rolls:`. `luckyMoney: Any` alone is refused.
  - `jokers:` under a `legendaryJoker` clause takes legendaries only.
  - `soulOrders: []` and `entries: []` are refused, like `soulOrder: []`.
  - `max` below 1 is refused when `min` is left out (`min` defaults to 1).
  - `null` is taken where the loader takes it: the `sources:` lists, `ante`, `must`, `should`,
    `mustNot`, `seeds`.
  - `Any` is taken blank or with spaces around it, and a name with a doubled "the"
    (`the the fool`) is taken, as the loader reads both.
- **Lists that may not be empty are typed non-empty**: `[T, ...T[]]` for `rolls` on event,
  voucher, tag and booster pack clauses, a boss's `antes`, `clauses`, and `soulOrder`. A variable
  typed `number[]` no longer assigns to those; build the list in place or type it non-empty.
- **Where the schema and the loader still part ways** is listed in the package README, all four
  cases: a `max` below a written `min`, one list written by two keys, a number or boolean written
  as text, and a letter outside A-Z inside a name.

## Now refused

- **`entries:` on a `soulOrder` clause with no legendary in it.** `entries:` binds the same list
  as `soulOrder:` but skipped its check, so `entries: [{}]`, `entries: [{ legendary: Blueprint }]`
  and `entries: []` loaded a clause that could never match. They are refused with the same words
  as `soulOrder:`.
