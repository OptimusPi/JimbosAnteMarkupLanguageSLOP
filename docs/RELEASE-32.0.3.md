# motely-wasm 32.0.3

Fixes found by an audit of 32.0.2. A filter that loaded on 32.0.2 still loads unless it is named
under "Now refused" below.

## Fixed

- **`--save` no longer writes `seeds:` entries a replay cannot read.** A seed list holding `~` or
  `null` was copied back as-is, and the 0-character seed and the seed `NULL` were written unquoted,
  which YAML reads as null. The rewrite now skips nulls and quotes `""` and `"NULL"`.
- **A `--save` that cannot write says so and exits 1.** A read-only or locked file used to end in
  a stack trace.
- **`JamlFiles`: picking a second folder that fails keeps the first.** The new folder is mounted
  before the old one is let go, so a refused or failed pick leaves the folder you had.
- **`JamlFiles`: re-picking and unmounting announce their changes.** Subscribers get `removed` for
  every filter of the folder that went away and `added` for every filter of the new one.
- **`JamlFiles.rename` no longer overwrites.** Renaming onto a name already in the folder is
  refused with `A filter named '…' is already in the folder.` A case-only rename is still allowed,
  and a move that fails puts the original file back.
- **`JamlFiles`: one throwing subscriber no longer stops the others** from hearing a change.
- **Filter names round-trip.** `x.yml.yaml`, `.yaml` and `x.YAML` come back from the folder under
  the name they were saved with. An empty name or a folder name is refused.

## Now refused

- **A null entry in `seeds:`** (`seeds: [~]`, or an unquoted `null`). It loaded on 32.0.2 and
  crashed the replay. Quote the seed if it is the word `NULL`; write `""` for the 0-character seed.
- **A joker in `legendaryJoker` that is not legendary** (`legendaryJoker: Blueprint`). The Soul
  only gives Canio, Triboulet, Yorick, Chicot or Perkeo, so the clause could never match.

## Schema and types

- `jaml/schema.json` now states the bounds the engine enforces: antes 0 to 39, boss antes from 1,
  rolls 0 to 999, `min` and `max` from 0, and at least one entry where the engine needs one.
- `legendaryJoker`, `legendaryJokers`, `soulJoker` and `soulOrder` entries accept only the five
  legendaries, in the schema and in `index.d.ts` (`MotelyJokerLegendaryName`).
- `standardCard` no longer lists a `card` key; the engine never read one.
- A `soulOrder` entry requires `legendary`.

## Notes

- The schema cannot express two engine rules: `max` below `min`, and an event clause that needs
  `rolls`. The engine still refuses both at load.
