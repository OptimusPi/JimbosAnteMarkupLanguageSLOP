# motely-wasm 32.0.0

`with:` reaches the shop, the Jamlyzer scrolls with no depth limit, and the loader reads fewer
spellings than 31.2.0 did. Check your filters against the Removed list before you upgrade.

## Removed

Each of these loads on 31.2.0 and is a load error on 32.0.0.

- **Shorthand clause lines** (`- Perkeo in ante 1 or 2`). Every clause is a mapping with a clause
  key: `- legendaryJoker: Perkeo` with `antes: [1, 2]`.
- **`defaults:`** at the top of a filter. Write `antes` and `score` on each clause.
- **`and:` / `or:` as a mapping** (`or: { clauses: [...] }`). Write the list under the key
  (`or: [...]`), or leave the key empty and put the list under `clauses:` beside it.
- **`max` on an `and:` or an `or:`, a `min` other than 1 on an `and:`, and `min: 0` on an
  `or:`.** Set them on the child clauses.
- **`with: ~`.** Leave `with:` out, or give it a mapping.

Also gone from the API:

- `withGpuSearch` and `withSourceConfig` on the search settings.
- From `motely-wasm/jaml`: `JamlClauseDefaults`, `JamlFilter.defaults`, `JamlAndPayload`,
  `JamlOrPayload`, the string form of `JamlClause`, and `null` as a value of `with`. `min` and
  `max` on `JamlAndClause` and `max` on `JamlOrClause` are typed as the loader takes them.

## New

- **`with: { vouchers: [...] }`** on `joker`, `commonJoker`, `uncommonJoker`, `rareJoker`,
  `tarotCard`, `planetCard`, `spectralCard` and `standardCard`. The shop the clause reads is
  rolled with those vouchers redeemed on top of the deck's own.

  ```yaml
  must:
    - voucher: TarotMerchant
      antes: [1]
    - tarotCard: Death
      antes: [2, 3]
      with:
        vouchers: [TarotMerchant]
  ```

- **The Jamlyzer takes the same block.** `analyze`, `resume` and `createRiderDesc` have an
  optional last argument `with`, a `MotelyWith` of `luck`, `jokers` and `vouchers`, and show the
  shop, Souls and luck events a clause with that `with:` reads. `MotelyLuck` is exported.
- **The Jamlyzer scroll has no depth limit.** `result.streamStates` holds a typed state per
  stream of each ante. Hand it back to `resume` unchanged and the next window reads on from
  there; nothing is rolled again. `eventRolls` (10,000) and `shopSlots` (100,000) bound one
  call, not the scroll.
- **`withScoredResultGate(gate)`** on the search settings: a predicate on a seed's score that
  gates the scored callback, the match count and `stopAfter`.

## Fixed

- **A cutoff above zero now holds when nothing scores.** A filter with no clauses, or with only
  `mustNot` clauses the vector pass decides exactly (a voucher, a boss), reported every seed it
  kept whatever its `cutoff:`. Only `should` clauses score, so those seeds score 0 and a cutoff
  above zero now reports none. Such a search rejects each vector before any per-seed work and
  allocates nothing per seed.

## Notes

- **Load errors read differently.** An unknown key is now
  `Property 'luckMoney' not found on type 'Motely.Filters.JokerClause'`; 31.2.0 said
  ``'luckMoney' is not a key of a `joker` clause, which takes …``. Code that matches the old
  text needs the new one.
- **`jaml-ui` 10.1.0 and earlier declare `motely-wasm ^31`.** They need a release that allows 32
  before npm installs this engine beside them without a peer warning.
- `resume` checks the bag it is given and refuses one that is not a previous result's
  `streamStates`, unchanged.
