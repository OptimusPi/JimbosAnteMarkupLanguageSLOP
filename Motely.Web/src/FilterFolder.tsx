import { useSyncExternalStore, useCallback, useReducer, useState } from "react";
import { JamlFiles, Errors } from "motely-wasm";
import { JimboButton, JimboPanel, JimboRow, JimboStack, JimboBadge } from "jaml-ui";
import { JimboErrorBlock } from "jaml-ui/ui";

export type OpenedFilter = { name: string; yaml: string };

export interface FilterFolderProps {
  opened: OpenedFilter | null;
  onOpen: (filter: OpenedFilter | null) => void;
}

// list() is a new array per call; useSyncExternalStore needs a stable snapshot.
let snapshot: readonly string[] | null = null;

function subscribeNames(cb: () => void) {
  const sub = () => { snapshot = null; cb(); };
  JamlFiles.onChange.subscribe(sub);
  return () => JamlFiles.onChange.unsubscribe(sub);
}

function getNames(): readonly string[] {
  return (snapshot ??= JamlFiles.list());
}

/** The engine's reason for a rejected file call; the rejection itself carries no message. */
function reason(fallback: string): string {
  return Errors.last() ?? fallback;
}

export function FilterFolder({ opened, onOpen }: FilterFolderProps) {
  const names = useSyncExternalStore(subscribeNames, getNames);
  // A folder with no filters mounts without a single onChange, so a pick re-renders by itself.
  const [, rerender] = useReducer((n: number) => n + 1, 0);
  const [error, setError] = useState<string | null>(null);
  const mounted = JamlFiles.isMounted();

  const pick = useCallback(async () => {
    setError(null);
    try {
      await JamlFiles.pickFolder();
    } catch {
      setError(reason("That folder could not be opened."));
    }
    snapshot = null;
    rerender();
  }, []);

  const open = useCallback(async (name: string) => {
    setError(null);
    try {
      const yaml = await JamlFiles.load(name);
      onOpen({ name, yaml });
    } catch {
      setError(reason(`${name} could not be read.`));
    }
  }, [onOpen]);

  if (!JamlFiles.isSupported()) return null;

  return (
    <JimboPanel title="Filters">
      <JimboStack gap="sm">
        <JimboRow gap="sm" align="center">
          <JimboButton size="sm" tone="blue" onClick={pick}>
            {mounted ? "Change folder" : "Open folder"}
          </JimboButton>
          {mounted && (
            <JimboBadge size="sm" tone="grey">
              {names.length} filter{names.length !== 1 ? "s" : ""}
            </JimboBadge>
          )}
        </JimboRow>
        {error && <JimboErrorBlock>{error}</JimboErrorBlock>}
        {names.map((name) => (
          <JimboButton
            key={name}
            size="sm"
            tone={opened?.name === name ? "orange" : "grey"}
            onClick={() => open(name)}
          >
            {name}
          </JimboButton>
        ))}
      </JimboStack>
    </JimboPanel>
  );
}
