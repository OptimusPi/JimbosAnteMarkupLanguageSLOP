# motely-wasm 32.0.1

Four fixes found by reviewing 32.0.0. Nothing that loaded on 32.0.0 stops loading.

## Fixed

- **The score gate applies to a filter with nothing to score.** `withScoredResultGate` was
  skipped when a filter had no clauses, or only `mustNot` clauses the vector pass decides
  exactly: every seed was reported whatever the gate said. Such seeds score 0 and the gate now
  sees that 0. It gates the seed-match callback, the match count, `stopAfter` and the Jamlyzer
  rider. In the CLI, `--cutoff max` on such a filter printed every match; it now prints one.
- **A null where a value goes says so.** `min: ~`, `max: ~` or `score: ~` on a clause reported
  "Object reference not set to an instance of an object". It now reports
  `JAML line N: this key needs a value; ~ is not one.`
- **`--jaml` with a path that does not exist** reported a path ending `.yaml.yaml`. A name that
  already has an extension is reported as written.
- **`motely-wasm/jaml` types and schema** allowed an empty `and: []` or `or: []`, and `and:` or
  `or:` left empty with no `clauses:` beside it. The loader refuses all of them, and the
  declarations now do too.
