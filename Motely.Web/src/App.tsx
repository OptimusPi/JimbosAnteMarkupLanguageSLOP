import { useCallback, useEffect, useState } from "react";
import {
  JimboApp,
  JimboBackground,
  JimboBalatroFooter,
  JimboBadge,
  JimboWordmark,
  SeedLab,
  STARTER_JAML,
} from "jaml-ui";
import { JimboTabs } from "jaml-ui/ui";
import { AgentPanel } from "./AgentPanel.js";
import { FilterFolder, type OpenedFilter } from "./FilterFolder.js";
import { SeedPile } from "./SeedPile.js";
import { useSeedLabWebMCP } from "./webmcp.js";

const SEED_RE = /^[1-9A-Z]{0,8}$/;

/** The pile is the front page; the lab is the filter editor; the agent talks to a server. */
type TabId = "pile" | "lab" | "agent";

function tabFrom(param: string | undefined, fallback: TabId): TabId {
  if (param === "agent" || param === "server") return "agent";
  if (param === "lab") return "lab";
  if (param === "pile") return "pile";
  return fallback;
}

function normalizeSeed(raw: string | null | undefined): string | null {
  if (raw === null || raw === undefined) return null;
  if (raw.length === 0) return "";
  const trimmed = raw.trim();
  if (trimmed === '""' || trimmed === "''") return "";
  if (trimmed.length === 0) return null;
  const upper = trimmed.toUpperCase().replace(/0/g, "O");
  return SEED_RE.test(upper) ? upper : null;
}

function getQueryParams(): {
  tab: TabId;
  initialOpened: OpenedFilter | null;
  agentInput: string;
} {
  if (typeof window === "undefined") {
    return { tab: "pile", initialOpened: null, agentInput: "" };
  }
  try {
    const params = new URLSearchParams(window.location.search);
    const tabParam = params.get("tab")?.toLowerCase();
    // A link that carries a seed or a filter was made for the lab, so it still opens there.
    const tab = tabFrom(tabParam, params.has("seed") || params.has("filter") ? "lab" : "pile");
    const seed = params.has("seed") ? normalizeSeed(params.get("seed")) : null;
    const seedDisplay = seed === "" ? '""' : seed;
    const filter = params.get("filter")?.trim();
    const prompt = params.get("prompt")?.trim();

    let initialOpened: OpenedFilter | null = null;
    if (seed !== null && filter) {
      const hasSeeds = /^\s*seeds\s*:/m.test(filter);
      const yaml = hasSeeds ? filter : `seeds:\n  - ${JSON.stringify(seed)}\n${filter}`;
      initialOpened = { name: `Seed: ${seedDisplay}`, yaml };
    } else if (seed !== null) {
      initialOpened = {
        name: `Seed: ${seedDisplay}`,
        yaml: `seeds:\n  - ${JSON.stringify(seed)}\n${STARTER_JAML}`,
      };
    } else if (filter) {
      initialOpened = { name: "Custom Filter", yaml: filter };
    }

    const agentInput = prompt || (seed !== null ? `analyze ${seedDisplay}` : "");
    return { tab, initialOpened, agentInput };
  } catch {
    return { tab: "pile", initialOpened: null, agentInput: "" };
  }
}

export function App({ folders }: { folders: boolean }) {
  const [initialParams] = useState(getQueryParams);
  const [opened, setOpened] = useState<OpenedFilter | null>(initialParams.initialOpened);
  const [tab, setTab] = useState<TabId>(initialParams.tab);
  const [agentInput, setAgentInput] = useState<string>(initialParams.agentInput);

  // Sync state if URL changes externally (e.g. browser back/forward)
  useEffect(() => {
    if (typeof window === "undefined") return;
    const onPopState = () => {
      const q = getQueryParams();
      setTab(q.tab);
      setOpened(q.initialOpened);
      setAgentInput(q.agentInput);
    };
    window.addEventListener("popstate", onPopState);
    return () => window.removeEventListener("popstate", onPopState);
  }, []);

  const handleTabChange = useCallback((id: string) => {
    const nextTab = tabFrom(id, "pile");
    setTab(nextTab);
    if (typeof window !== "undefined") {
      const url = new URL(window.location.href);
      url.searchParams.set("tab", nextTab);
      window.history.replaceState({}, "", url);
    }
  }, []);

  const handleInspect = useCallback((rawSeed: string) => {
    const seed = normalizeSeed(rawSeed);
    if (seed === null) return;
    const seedDisplay = seed === "" ? '""' : seed;
    setOpened({
      name: `Seed: ${seedDisplay}`,
      yaml: `seeds:\n  - ${JSON.stringify(seed)}\n${STARTER_JAML}`,
    });
    setTab("lab");
    if (typeof window !== "undefined") {
      const url = new URL(window.location.href);
      url.searchParams.set("tab", "lab");
      url.searchParams.set("seed", seed);
      window.history.pushState({}, "", url);
    }
  }, []);

  // WebMCP tools are registered for as long as the lab is mounted; the badge says an
  // in-page agent (native WebMCP) can drive seed_validate / seed_search directly.
  const agentReady = useSeedLabWebMCP();

  return (
    <div style={{ minHeight: "100dvh", display: "flex", flexDirection: "column", paddingBottom: 48 }}>
      <JimboBackground />
      <JimboApp variant="page" style={{ flex: 1 }}>
        <div style={{ display: "flex", alignItems: "center", gap: "0.6rem", flexWrap: "wrap" }}>
          <JimboWordmark title="Motely" sub="Balatro seed search, in your browser, at full speed" />
          {agentReady ? (
            <JimboBadge
              size="sm"
              tone="grey"
              title="WebMCP ready: this page exposes seed_validate and seed_search to in-page agents"
            >
              🃏 agent-ready
            </JimboBadge>
          ) : null}
        </div>
        <JimboTabs
          tabs={[
            { id: "pile", label: "Seed Pile" },
            { id: "lab", label: "Seed Lab" },
            { id: "agent", label: "Server Agent" },
          ]}
          activeTab={tab}
          onTabChange={handleTabChange}
        />
        {tab === "pile" ? (
          <SeedPile />
        ) : tab === "agent" ? (
          <AgentPanel onInspect={handleInspect} initialInput={agentInput} />
        ) : (
          <>
            {folders ? <FilterFolder opened={opened} onOpen={setOpened} /> : null}
            {/* Opening a filter remounts the lab with that text: a new filter is a new search. */}
            <SeedLab key={opened?.name ?? "starter"} defaultJaml={opened?.yaml ?? STARTER_JAML} />
          </>
        )}
      </JimboApp>
      {/* Pinned to the viewport bottom, like Balatro's page chrome — content scrolls under
          the 48px of padding above, so nothing ever hides behind it. */}
      <JimboBalatroFooter style={{ position: "fixed", bottom: 0, left: 0, right: 0, marginTop: 0 }} />
    </div>
  );
}
