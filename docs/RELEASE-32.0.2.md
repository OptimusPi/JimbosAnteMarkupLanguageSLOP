# motely-wasm 32.0.2

Fixes found by an independent review of 32.0.1. Every filter that loaded on 32.0.1 still loads.

## Fixed

- **A null source slot no longer crashes the search.** `sources: { shopItems: ~ }`, an empty
  `boosterPacks:`, or any other source slot given null loaded on 32.0.1 and then threw
  `NullReferenceException`, with a stack trace, when the search built its filter. A null slot is
  now the slot left out, the same as `ante: ~` is the default antes.
- **`--cutoff max` on a filter with nothing to score ends after its one row.** Such a filter
  scores every seed 0, so the first match is the only row `max` can ever print. 32.0.1 kept
  sweeping after it, and `--cutoff max --collect 3` never stopped: it waited for a second row that
  could not come. The CLI now prints a warning and stops at the first match.
- **The missing-value message no longer mentions `~` when the file has none.** An empty value
  (`min:`), `null`, a JSON `null` and a null list item (`antes: [1, ~]`) were all reported as
  `this key needs a value; ~ is not one.` The message is now
  `JAML line N: a value is missing here. Null and empty are not values.` It also replaces
  `Index was outside the bounds of the array.` for `min: ""`.
- **A missing filter names every place the CLI looked.** `--jaml nope.x.y` reported one wrong
  path, and `--jaml Filters/nope` reported `Filters/Filters/nope.yaml`. The error is now
  `no filter file for 'nope.x.y': looked for <path> and <path under Filters/> with .yaml, .yml, .jaml.`

## Notes

- **`--cutoff max` with more than one thread is not repeatable.** It prints a seed only when it
  beats every score before it, and threads finish in a different order from run to run, so which
  seeds print can differ. That was already so in 32.0.0. `--threads 1` gives the same rows every
  time.
- **Tests for 32.0.1.** Three of its fixes had no test: the `.yaml.yaml` path, the CLI's
  `--cutoff max` gate on a filter with nothing to score, and the gate on the Jamlyzer rider. They
  have tests now, and the CLI is run as its own process in the suite.
