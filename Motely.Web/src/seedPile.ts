// The Seed Pile's plain logic: reading a pasted pile, building the analyzer's request, and
// naming where the things you scan for turn up. No React here, so node can check it.

import {
  MotelyBoosterPack,
  MotelyItemEdition,
  MotelyItemType,
  MotelyTag,
  MotelyVoucher,
  type MotelyItem,
  type MotelyJamlyzerSeedResult,
} from "motely-wasm";

const SEED_RE = /^[1-9A-Z]{1,8}$/;

/** A seed as the game writes it: upper case, and a typed zero is the letter O. */
function asSeed(token: string): string | null {
  const seed = token.trim().toUpperCase().replace(/0/g, "O");
  return SEED_RE.test(seed) ? seed : null;
}

/** The header of a motely results file: `seed,score,...`. The word SEED alone is a seed. */
const RESULTS_HEADER_RE = /^\s*"?seed"?\s*[,;\t]\s*"?score\b/i;

/** A score or tally column. Eight digits is a seed, not a score. */
const SCORE_RE = /^-?\d{1,7}$/;

/**
 * The seeds in a pasted pile, in the order pasted, each once.
 *
 * A motely results row is columns split by commas, semicolons or tabs: a seed, then nothing but
 * scores (`1JHBI111, 41, 0`). Its first column is the seed and the scores are not. Any other
 * line gives every seed on it, so one per line, a row of them with spaces, or a spreadsheet
 * column all work, and `ALEEB 12345678` is two seeds. A results file's header line is skipped.
 */
export function seedsIn(text: string): string[] {
  const seen = new Set<string>();
  const seeds: string[] = [];
  const take = (token: string) => {
    const seed = asSeed(token);
    if (seed !== null && !seen.has(seed)) {
      seen.add(seed);
      seeds.push(seed);
    }
  };
  for (const line of text.split(/\r?\n/)) {
    if (RESULTS_HEADER_RE.test(line)) continue;
    const columns = line.split(/[,;\t]/).map((column) => column.trim().replace(/^["']|["']$/g, ""));
    const isResultsRow =
      columns.length > 1 && asSeed(columns[0]) !== null && columns.slice(1).every((column) => SCORE_RE.test(column));
    if (isResultsRow) take(columns[0]);
    else line.split(/[\s,;|"']+/).forEach(take);
  }
  return seeds;
}

/** The analyzer's request for one seed on one deck and stake. The seed is quoted: `12345678` and `NULL` are seeds. */
export function analyzeRequest(seed: string, deck: string, stake: string): string {
  return `deck: ${deck}\nstake: ${stake}\nseeds:\n  - ${JSON.stringify(seed)}\n`;
}

/** One thing you scan for, found: which ante, where in it, and what it is. */
export type Spotted = { ante: number; where: string; name: string };

/** Letters and digits only, lower case: "Oops! All 6s", "oops all 6s" and the engine's `OopsAll6s` are one name. */
function plain(name: string): string {
  return name.toLowerCase().replace(/[^a-z0-9]/g, "");
}

/** The names in a typed watch list: separated by commas or new lines. */
export function watchNames(text: string): string[] {
  return text
    .split(/[,\n]+/)
    .map((name) => name.trim())
    .filter((name) => name.length > 0);
}

/** `OopsAll6s` as "Oops All 6s", `CouponTag` as "Coupon Tag". */
function spaced(key: string): string {
  return key.replace(/([a-z0-9])([A-Z])/g, "$1 $2").replace(/([A-Za-z])(\d)/g, "$1 $2");
}

function itemName(item: MotelyItem): string {
  const stickers = [
    item.edition !== MotelyItemEdition.None ? MotelyItemEdition[item.edition] : "",
    item.isEternal ? "Eternal" : "",
    item.isPerishable ? "Perishable" : "",
    item.isRental ? "Rental" : "",
  ].filter((s) => s.length > 0);
  return [...stickers, spaced(MotelyItemType[item.type])].join(" ");
}

/**
 * Where each watched name turns up in an analyzed seed: shops, packs, skip tags and vouchers,
 * ante by ante. A watched name matches the engine's own name for the thing, spelled any way
 * ("oops all 6s", "Coupon Tag", "coupon").
 */
export function spot(result: MotelyJamlyzerSeedResult, watch: readonly string[]): Spotted[] {
  const wanted = new Set(watch.map(plain).filter((name) => name.length > 0));
  if (wanted.size === 0) return [];
  const isWanted = (key: string) => {
    const name = plain(key);
    return wanted.has(name) || (name.endsWith("tag") && wanted.has(name.slice(0, -3)));
  };

  const found: Spotted[] = [];
  for (const ante of result.antes) {
    if (isWanted(MotelyVoucher[ante.voucher]))
      found.push({ ante: ante.ante, where: "Voucher", name: spaced(MotelyVoucher[ante.voucher]) });
    if (isWanted(MotelyTag[ante.smallBlindTag]))
      found.push({ ante: ante.ante, where: "Small Blind Skip", name: spaced(MotelyTag[ante.smallBlindTag]) });
    if (isWanted(MotelyTag[ante.bigBlindTag]))
      found.push({ ante: ante.ante, where: "Big Blind Skip", name: spaced(MotelyTag[ante.bigBlindTag]) });
    ante.shopItems.forEach((item, slot) => {
      if (isWanted(MotelyItemType[item.type]))
        found.push({ ante: ante.ante, where: `Shop ${slot + 1}`, name: itemName(item) });
    });
    ante.packs.forEach((pack, index) => {
      for (const item of pack.items) {
        if (isWanted(MotelyItemType[item.type]))
          found.push({
            ante: ante.ante,
            where: `Pack ${index + 1}, ${spaced(MotelyBoosterPack[pack.pack])}`,
            name: itemName(item),
          });
      }
    });
  }
  return found;
}
