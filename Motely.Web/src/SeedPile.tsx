import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  DeckStakePicker,
  JamlyzerView,
  JimboBadge,
  JimboButton,
  JimboErrorBlock,
  JimboPanel,
  JimboSeedCopyChip,
  JimboText,
  JimboTextArea,
  JimboTextInput,
} from "jaml-ui";
import {
  Errors,
  JamlConfigLoader,
  MotelyDeck,
  MotelyJamlyzer,
  MotelyStake,
  type MotelyJamlyzerSeedResult,
} from "motely-wasm";
import { analyzeRequest, seedsIn, spot, watchNames } from "./seedPile.js";

const PILE_KEY = "motely.seedPile";
const PLACE_KEY = "motely.seedPile.place";
const WATCH_KEY = "motely.seedPile.watch";
const DEFAULT_WATCH = "Oops All 6s, Ankh, Blueprint, Brainstorm, Coupon Tag";

/** A finger that travels this far sideways, and less than half as far up or down, is a swipe. */
const SWIPE_PX = 48;

function remembered(key: string): string | null {
  try {
    return window.localStorage.getItem(key);
  } catch {
    return null;
  }
}

function remember(key: string, value: string): void {
  try {
    window.localStorage.setItem(key, value);
  } catch {
    // Private windows refuse storage; the pile then lasts as long as the page.
  }
}

type Analyzed = { result: MotelyJamlyzerSeedResult } | { error: string };

function analyze(seed: string, deck: string, stake: string): Analyzed {
  try {
    const results = MotelyJamlyzer.analyze(JamlConfigLoader.fromJaml(analyzeRequest(seed, deck, stake)));
    return results.length > 0 ? { result: results[0] } : { error: `The engine returned nothing for ${seed}.` };
  } catch (thrown) {
    return { error: Errors.last() || (thrown instanceof Error ? thrown.message : String(thrown)) };
  }
}

/**
 * Paste a pile of seeds, then step through them one at a time: arrow keys, a swipe, or the
 * Previous and Next bar pinned at the bottom. Deck and stake flip on the seed in front of you,
 * and the names on the watch list are called out wherever they turn up. Nothing to write.
 */
export function SeedPile() {
  const [seeds, setSeeds] = useState<string[]>(() => seedsIn(remembered(PILE_KEY) ?? ""));
  // Your place in the pile survives a reload: a thousand seeds in, a refresh should not start over.
  const [index, setIndex] = useState(() => Number.parseInt(remembered(PLACE_KEY) ?? "0", 10) || 0);
  const [deck, setDeck] = useState("Red");
  const [stake, setStake] = useState("White");
  const [watchText, setWatchText] = useState(() => remembered(WATCH_KEY) ?? DEFAULT_WATCH);
  const [pasting, setPasting] = useState(false);
  const [draft, setDraft] = useState(() => remembered(PILE_KEY) ?? "");

  const count = seeds.length;
  const seed = count > 0 ? seeds[Math.min(index, count - 1)] : null;

  useEffect(() => remember(PLACE_KEY, String(index)), [index]);

  const step = useCallback(
    (by: number) => setIndex((at) => Math.max(0, Math.min(count - 1, at + by))),
    [count],
  );

  const takePile = useCallback((pile: string[]) => {
    setSeeds(pile);
    setIndex(0);
    setPasting(false);
    setDraft(pile.join("\n"));
    remember(PILE_KEY, pile.join("\n"));
  }, []);

  // Arrow keys step unless you are typing in a field. Home and End jump to the ends.
  useEffect(() => {
    if (count === 0) return;
    const onKey = (event: KeyboardEvent) => {
      const target = event.target as HTMLElement | null;
      if (target && (target.tagName === "INPUT" || target.tagName === "TEXTAREA" || target.isContentEditable)) return;
      if (event.metaKey || event.ctrlKey || event.altKey) return;
      if (event.key === "ArrowRight") step(1);
      else if (event.key === "ArrowLeft") step(-1);
      else if (event.key === "Home") setIndex(0);
      else if (event.key === "End") setIndex(count - 1);
      else return;
      event.preventDefault();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [count, step]);

  // Swipe left for the next seed, right for the one before. A mostly up-and-down drag is a scroll.
  const touch = useRef<{ x: number; y: number } | null>(null);
  const onTouchStart = useCallback((event: React.TouchEvent) => {
    // A row of cards that scrolls sideways keeps its own drag: a touch that starts on one is not a step.
    for (let el = event.target as HTMLElement | null; el && el !== event.currentTarget; el = el.parentElement) {
      const scrolls = /(auto|scroll)/.test(getComputedStyle(el).overflowX);
      if (scrolls && el.scrollWidth > el.clientWidth + 1) {
        touch.current = null;
        return;
      }
    }
    const finger = event.touches[0];
    touch.current = { x: finger.clientX, y: finger.clientY };
  }, []);
  const onTouchEnd = useCallback(
    (event: React.TouchEvent) => {
      const from = touch.current;
      touch.current = null;
      if (!from) return;
      const finger = event.changedTouches[0];
      const dx = finger.clientX - from.x;
      const dy = finger.clientY - from.y;
      if (Math.abs(dx) >= SWIPE_PX && Math.abs(dy) < Math.abs(dx) / 2) step(dx < 0 ? 1 : -1);
    },
    [step],
  );

  const analyzed = useMemo(() => (seed === null ? null : analyze(seed, deck, stake)), [seed, deck, stake]);
  const watch = useMemo(() => watchNames(watchText), [watchText]);
  const spotted = useMemo(
    () => (analyzed !== null && "result" in analyzed ? spot(analyzed.result, watch) : []),
    [analyzed, watch],
  );

  if (count === 0 || pasting) {
    // The library's batch input keeps only 8-character codes; shorter seeds such as ALEEB are
    // real seeds, so the pile reads the paste itself.
    const found = seedsIn(draft);
    return (
      <JimboPanel title="Seed Pile">
        <JimboText size="sm" tone="grey">
          Paste seeds here: one per line, a row of them, or a Motely results file. Then step through them.
        </JimboText>
        <JimboTextArea
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          rows={8}
          autoFocus
          spellCheck={false}
          autoCapitalize="characters"
          aria-label="Seeds to step through"
          placeholder={"ALEEB\nPIROCKS\nWEEJOKER"}
        />
        <JimboText size="sm" tone={found.length > 0 ? "green" : "grey"} aria-live="polite">
          {found.length === 0
            ? "No seeds found yet. A seed is 1 to 8 letters and digits."
            : found.length === 1
              ? "1 seed found."
              : `${found.length.toLocaleString()} seeds found.`}
        </JimboText>
        <JimboButton tone="green" size="lg" fullWidth disabled={found.length === 0} onClick={() => takePile(found)}>
          {found.length === 0 ? "Paste Seeds To Start" : "Step Through The Pile"}
        </JimboButton>
        {count > 0 ? (
          <JimboButton tone="grey" size="sm" onClick={() => setPasting(false)}>
            Back To The Pile
          </JimboButton>
        ) : null}
      </JimboPanel>
    );
  }

  const at = Math.min(index, count - 1);
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "0.6rem" }}>
      <JimboPanel>
        <div style={{ display: "flex", flexWrap: "wrap", alignItems: "center", gap: "0.6rem" }}>
          <JimboText size="lg" tone="gold" aria-live="polite">
            Seed {(at + 1).toLocaleString()} of {count.toLocaleString()}
          </JimboText>
          <JimboSeedCopyChip value={seed ?? ""} style={{ flex: "1 1 12rem", minWidth: "10rem" }} />
          <JimboButton tone="blue" size="sm" onClick={() => setPasting(true)}>
            New Pile
          </JimboButton>
        </div>
        <DeckStakePicker deck={deck} stake={stake} onDeckChange={setDeck} onStakeChange={setStake} layout="row" />
        <label style={{ display: "flex", flexDirection: "column", gap: "0.25rem" }}>
          <JimboText size="xs" tone="grey">
            Watch for (names, separated by commas)
          </JimboText>
          <JimboTextInput
            value={watchText}
            onChange={(event) => {
              setWatchText(event.target.value);
              remember(WATCH_KEY, event.target.value);
            }}
            placeholder={DEFAULT_WATCH}
            aria-label="Watch for"
          />
        </label>
        <div style={{ display: "flex", flexWrap: "wrap", gap: "0.35rem", alignItems: "center" }}>
          <JimboText size="sm" tone={spotted.length > 0 ? "green" : "grey"}>
            {spotted.length > 0 ? "Spotted:" : watch.length > 0 ? "Nothing watched is in this seed." : "Nothing is being watched."}
          </JimboText>
          {spotted.map((hit, i) => (
            <JimboBadge key={i} tone="green" size="md">
              Ante {hit.ante} {hit.where}: {hit.name}
            </JimboBadge>
          ))}
        </div>
      </JimboPanel>

      {/* The card canvases are drawn wider than their cards. Clipped here, they cannot widen the
          page past the screen, which on a phone would carry the bar below off-screen. */}
      <div onTouchStart={onTouchStart} onTouchEnd={onTouchEnd} style={{ minWidth: 0, overflowX: "clip" }}>
        {analyzed === null ? null : "error" in analyzed ? (
          <JimboErrorBlock>{analyzed.error}</JimboErrorBlock>
        ) : (
          <JamlyzerView
            result={analyzed.result}
            deck={MotelyDeck[deck as keyof typeof MotelyDeck]}
            stake={MotelyStake[stake as keyof typeof MotelyStake]}
          />
        )}
      </div>

      {/* Pinned above the page footer, under the thumb on a phone. Flanking buttons beside the
          seed view get covered by it at phone width, so the stepping lives here. */}
      <nav
        aria-label="Step through the pile"
        className="j-panel"
        style={{
          position: "sticky",
          bottom: 52,
          zIndex: 5,
          gridAutoFlow: "column",
          gridTemplateColumns: "1fr auto 1fr",
          alignItems: "center",
          gap: "0.5rem",
          padding: "0.5rem",
        }}
      >
        <JimboButton tone="red" size="lg" fullWidth disabled={at === 0} onClick={() => step(-1)}>
          Previous
        </JimboButton>
        <JimboText size="sm" tone="grey" style={{ whiteSpace: "nowrap" }}>
          {(at + 1).toLocaleString()} / {count.toLocaleString()}
        </JimboText>
        <JimboButton tone="green" size="lg" fullWidth disabled={at === count - 1} onClick={() => step(1)}>
          Next
        </JimboButton>
      </nav>
    </div>
  );
}
