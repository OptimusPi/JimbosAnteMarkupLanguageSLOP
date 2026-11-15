# motely-wasm 32.1.0

`motely-wasm/jaml` is now written by the engine instead of by hand, and gains a vocabulary file
for editors. One loader fix. A filter that loaded on 32.0.3 still loads unless it is named under
"Now refused" below.

## New

- **`motely-wasm/jaml/vocab.json`.** The clause keys, the members each clause and block accepts,
  what kind of value each member holds, and every enum's names. It is 31 KB, against 280 KB for
  the schema, and is meant for an editor's completion list:

  ```js
  import vocab from "motely-wasm/jaml/vocab.json" with { type: "json" };
  vocab.clauseKeys.joker;                  // "jokerClause"
  vocab.clauses.jokerClause.members.antes; // "numbers"
  vocab.enums.MotelyJoker;                 // ["Joker", "GreedyJoker", ...]
  ```

- **`motely schema`.** `dotnet run --project Motely.CLI -- schema`, from the repository root,
  writes `index.d.ts`, `motely-filter.schema.json` and `vocab.json` from the filter loader. A test
  fails when a committed file is not what the command writes.

## Fixed

- **`entries:` on a `soulOrder` clause takes the same check as `soulOrder:`.** The three spellings
  `soulOrder:`, `soulOrders:` and `entries:` fill one list, and only the first two were checked.

## Now refused

- **A `soulOrder` clause whose `entries:` is empty, null, or holds a soul that names no legendary**
  (`entries: []`, `entries: [{}]`, `entries: [{ legendary: Blueprint }]`). Each loaded on 32.0.3
  and could never match as written. Name a legendary in every entry.

## Changed

- **The clause rules live in one table that the loader enforces and the schema is written from.**
  The loader refuses what it refused on 32.0.3, in the same order.
- **Four loader messages say "accepts" or "holds" where they said "takes":**
  - `an and accepts no min or max: set them on its clauses.`
  - `an or accepts no max: set it on its clauses.`
  - `'…' is not a legendary joker (legendaryJoker accepts Canio, Triboulet, Yorick, Chicot or Perkeo).`
  - `should: holds … clauses; a filter holds at most 256.`

## Schema and types

The generated files follow the loader where the hand-kept ones did not:

- The twelve event keys (`luckyMoney` and the rest) need at least one roll and do not accept `Any`
  or null.
- `soulOrder`, `soulOrders` and `entries` need at least one entry, and an entry requires
  `legendary`.
- The schema states `should` at most 256 clauses, boss antes 1 to 39, and number lists 0 to 999.
- Null is accepted wherever the loader returns a value for it: `id`, `seeds`, `ante`, `must`,
  `should`, `mustNot`, and the number lists under `sources:`.

## Notes

- The files are narrower than the loader in one place: `and` has no `min` or `max` and `or` has no
  `max`. The loader still accepts `min: 1` or `max: ~` written out on an `and`.
- The schema cannot express `max` below `min`. The loader refuses it, and the description of
  `max` says so.
