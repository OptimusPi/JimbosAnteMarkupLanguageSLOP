# motely-wasm 32.0.0

`with:` reaches the shop, the Jamlyzer scrolls with no depth limit, some results are different,
and the loader refuses spellings and values that 31.2.0 took. Read Removed and Changed results
before you upgrade.

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
- **Values outside the loader's limits:**
  - `max` below `min` on any clause, a bare `max: 0` included (`min` defaults to 1), and a
    negative `min` or `max`
  - an ante above 39
  - a number outside 0 to 999 in `shopItems` or `soulEditionRolls`
  - an empty `and: []` or `or: []`
  - an empty `rolls: []` on a voucher, tag or event clause
  - a `boss` clause with ante 0
  - more than 256 `should` clauses
- **A YAML alias** (`*name`), and **a key written twice** in one mapping.

Also gone from the API:

- `withGpuSearch` and `withSourceConfig` on the search settings.
- `JamlClauseDefaults` and `JamlConfig.defaults` from the generated types.
- From `motely-wasm/jaml`: `JamlClauseDefaults`, `JamlFilter.defaults`, `JamlAndPayload`,
  `JamlOrPayload`, the string form of `JamlClause`, and `null` as a value of `with`. `min` and
  `max` on `JamlAndClause` and `max` on `JamlOrClause` are typed as the loader takes them.

## Changed results

The same filter on the same seeds gives different results on 31.2.0 and 32.0.0 for the clauses
below. The 377 filter files in this repository score the same on both engines over 2,000 seeds;
over 40,000 seeds `Filters/spectral-medium.yaml` loses one seed (`JY5KZZSF`), in which neither
engine's Jamlyzer shows a Medium.

- **Omen Globe.** A clause with `sources: { omenGlobe: true }`, and the Jamlyzer's
  `events.omenGlobe`. 32.0.0 takes a Spectral when the roll is above 0.8, as the game does. On
  one such filter 1,684 of 2,000 seeds score differently.
- **`pokerHand`.** Counts differ on some seeds, for one hand and for a list of hands.
- **Voucher `rolls` after the first.** `rolls: [0]` is unchanged.
- **The Magic and Zodiac decks.** `boosterPack` counts differ on some seeds on both, and
  `standardCard` counts on Magic.
- **Ante 0 in the Jamlyzer** names a boss. 31.2.0 returned a meaningless negative number there.

## New

- **`with: { vouchers: [...] }`** on `joker`, `commonJoker`, `uncommonJoker`, `rareJoker`,
  `tarotCard`, `planetCard`, `spectralCard` and `standardCard`. The shop the clause reads is
  rolled as if those vouchers were redeemed on top of the deck's own.

  ```yaml
  must:
    - voucher: TarotMerchant
      antes: [1]
    - tarotCard: Death
      antes: [2, 3]
      with:
        vouchers: [TarotMerchant]
  ```

  The roll reads five vouchers: Tarot Merchant, Tarot Tycoon, Planet Merchant, Planet Tycoon and
  Magic Trick. Any other voucher in the list changes nothing; Hone and Glow Up do not raise
  edition odds. Magic Trick matters only on a clause that reads shop slots, so a `standardCard`
  clause needs `sources: { shopItems: [...] }` for it.
- **The Jamlyzer takes the same block.** `analyze`, `resume` and `createRiderDesc` have an
  optional last argument `with`, a `MotelyWith` of `luck`, `jokers` and `vouchers`. `MotelyLuck`
  is exported.
- **The Jamlyzer scroll has no depth limit.** `result.streamStates` holds a typed state per
  stream of each ante. Hand it back to `resume` unchanged and the next window reads on from
  there; nothing is rolled again. `eventRolls` (10,000) and `shopSlots` (100,000) bound one
  call, not the scroll.
- **`withScoredResultGate(gate)`** on the search settings: a predicate on a seed's score that
  gates the scored callback, the match count and `stopAfter`. It applies when the filter has
  something to score. A filter with no clauses, or with only `mustNot` clauses the vector pass
  decides exactly (a voucher, a boss), is not gated.
- **`stickers:`** on joker clauses loads. On 31.2.0 it was a load error.
- **Tag-form clauses** (`- !joker`) load. On 31.2.0 the tag was unresolved.
- **`or:` with the list under `clauses:`** beside an empty key loads. On 31.2.0 it threw.

## Fixed

- **A cutoff above zero now holds when nothing scores.** A filter with no clauses, or with only
  `mustNot` clauses the vector pass decides exactly, reported every seed it kept whatever its
  `cutoff:`. Only `should` clauses score, so those seeds score 0 and a cutoff above zero now
  reports none. Such a search rejects each vector before any per-seed work and allocates
  nothing per seed.

## Notes

- **Load errors read differently.**
  - An unknown key is now `Property 'luckMoney' not found on type 'Motely.Filters.JokerClause'`;
    31.2.0 said ``'luckMoney' is not a key of a `joker` clause, which takes …``.
  - The "did you mean" hints of 31.1.0 are gone.
  - A value outside the limits is reported without a line number
    (`JAML: a and clause is out of range: …`). The 31.2.0 errors sampled all carried one.
- **`jaml-ui` 10.0.0 and earlier declare `motely-wasm ^31` as a peer.** A fresh `npm install` of
  one of them beside this engine fails on the peer conflict; an install from an existing
  lockfile gives a warning.
- **`resume` range-checks the bag it is given and nothing more.** It does not check that the bag
  came from the same seed or the same filter. Hand back the `streamStates` of a previous result
  for that seed, unchanged.
