// JamlFiles over the real @rewaffle/bootsharp-file-system package, mounted on in-memory
// directory handles: the same code path the site runs, with the browser's picker and observer
// stood in by handles.mjs. Usage: npm test   (after `dotnet publish -c Release`)
import test from "node:test";
import assert from "node:assert/strict";
import * as fs from "@rewaffle/bootsharp-file-system";
import bootsharp, { JamlFiles, Errors } from "../bin/motely-wasm/index.mjs";
import { IFileMounter } from "../bin/motely-wasm/generated/modules/bootsharp/file-system.g.mjs";
import { DirMock, FileMock, installBrowserGlobals } from "./handles.mjs";

const folder = new DirMock("filters", [
  new FileMock("perkeo.yaml", "name: perkeo\n"),
  new DirMock("sub", [new FileMock("cola.yml", "name: cola\n")]),
  new FileMock("notes.txt", "not a filter"),
]);
let picked = () => folder;
installBrowserGlobals(() => picked());

// The mounter must be bound before boot; debounce 0 so each change reports at once.
fs.init(IFileMounter, 0);
await bootsharp.boot();

const changes = [];
JamlFiles.onChange.subscribe((change) => changes.push(change));

test("this build has folder access and starts with nothing mounted", () => {
  assert.equal(JamlFiles.isSupported(), true);
  assert.equal(JamlFiles.isMounted(), false);
  assert.deepEqual([...JamlFiles.list()], []);
});

test("a cancelled picker is false, not an error", async () => {
  picked = () => {
    throw new Error("cancelled");
  };
  assert.equal(await JamlFiles.pickFolder(), false);
  assert.equal(JamlFiles.isMounted(), false);
  picked = () => folder;
});

test("picking a folder lists its filters, yaml extension dropped, other files left out", async () => {
  assert.equal(await JamlFiles.pickFolder(), true);
  assert.equal(JamlFiles.isMounted(), true);
  assert.deepEqual([...JamlFiles.list()], ["perkeo", "sub/cola.yml"]);
  assert.deepEqual(
    changes.map((c) => [c.kind, c.name]),
    [["added", "perkeo"], ["added", "sub/cola.yml"]],
  );
});

test("a filter loads by name", async () => {
  assert.equal(await JamlFiles.load("perkeo"), "name: perkeo\n");
  assert.equal(await JamlFiles.load("sub/cola.yml"), "name: cola\n");
});

test("saving creates or rewrites, and the list and events follow", async () => {
  changes.length = 0;
  await JamlFiles.save("new", "name: new\n");
  assert.equal(await JamlFiles.load("new"), "name: new\n");
  assert.deepEqual([...JamlFiles.list()], ["new", "perkeo", "sub/cola.yml"]);

  await JamlFiles.save("perkeo", "name: perkeo\nseeds: [ALEEB]\n");
  assert.equal(await JamlFiles.load("perkeo"), "name: perkeo\nseeds: [ALEEB]\n");
  assert.deepEqual(
    changes.map((c) => [c.kind, c.name]),
    [["added", "new"], ["modified", "perkeo"]],
  );
});

test("renaming moves the file and reports where it came from", async () => {
  changes.length = 0;
  await JamlFiles.rename("new", "sub/new");
  assert.deepEqual([...JamlFiles.list()], ["perkeo", "sub/cola.yml", "sub/new"]);
  assert.equal(await JamlFiles.load("sub/new"), "name: new\n");
  assert.deepEqual(changes, [{ kind: "moved", name: "sub/new", fromName: "new" }]);
});

test("removing deletes the file", async () => {
  changes.length = 0;
  await JamlFiles.remove("sub/new");
  assert.deepEqual([...JamlFiles.list()], ["perkeo", "sub/cola.yml"]);
  assert.deepEqual(changes, [{ kind: "removed", name: "sub/new" }]);
});

test("a file call that fails says why on Errors.last()", async () => {
  Errors.last();
  await assert.rejects(JamlFiles.load("missing"));
  assert.match(Errors.last() ?? "", /missing/);
});

test("renaming onto a name that is taken is refused, and both files stay as they were", async () => {
  changes.length = 0;
  Errors.last();
  await assert.rejects(JamlFiles.rename("perkeo", "sub/cola.yml"));
  assert.match(Errors.last() ?? "", /already in the folder/);
  assert.deepEqual([...JamlFiles.list()], ["perkeo", "sub/cola.yml"]);
  assert.equal(await JamlFiles.load("perkeo"), "name: perkeo\nseeds: [ALEEB]\n");
  assert.equal(await JamlFiles.load("sub/cola.yml"), "name: cola\n");
  assert.deepEqual(changes, []);
});

test("an empty name is refused before it reaches the folder", async () => {
  Errors.last();
  await assert.rejects(JamlFiles.save("", "name: nothing\n"));
  assert.match(Errors.last() ?? "", /not a filter name/);
});

test("a subscriber that throws does not stop later changes from arriving", async () => {
  const thrower = () => {
    throw new Error("subscriber bug");
  };
  JamlFiles.onChange.subscribe(thrower);
  await JamlFiles.save("first", "name: first\n");
  JamlFiles.onChange.unsubscribe(thrower);
  changes.length = 0;
  await JamlFiles.save("second", "name: second\n");
  assert.deepEqual(changes.map((c) => [c.kind, c.name]), [["added", "second"]]);
  await JamlFiles.remove("first");
  await JamlFiles.remove("second");
  Errors.last();
});

test("a folder that cannot be mounted rejects and leaves the current one mounted", async () => {
  changes.length = 0;
  const denied = new DirMock("locked", [new FileMock("secret.yaml", "name: secret\n")]);
  denied.queryPermission = async () => "prompt";
  denied.requestPermission = async () => "denied";
  picked = () => denied;
  Errors.last();
  await assert.rejects(JamlFiles.pickFolder());
  assert.match(Errors.last() ?? "", /denied access/);
  picked = () => folder;
  assert.equal(JamlFiles.isMounted(), true);
  assert.deepEqual([...JamlFiles.list()], ["perkeo", "sub/cola.yml"]);
  assert.equal(await JamlFiles.load("perkeo"), "name: perkeo\nseeds: [ALEEB]\n");
  assert.deepEqual(changes, []);
});

test("picking another folder swaps the list: the old names leave, the new ones arrive", async () => {
  changes.length = 0;
  const other = new DirMock("other", [new FileMock("cola.yaml", "name: other cola\n")]);
  picked = () => other;
  assert.equal(await JamlFiles.pickFolder(), true);
  assert.deepEqual([...JamlFiles.list()], ["cola"]);
  assert.equal(await JamlFiles.load("cola"), "name: other cola\n");
  assert.deepEqual(
    changes.map((c) => [c.kind, c.name]),
    [["removed", "perkeo"], ["removed", "sub/cola.yml"], ["added", "cola"]],
  );
  picked = () => folder;
});

test("unmounting clears the list, and file calls then say no folder is mounted", async () => {
  changes.length = 0;
  await JamlFiles.unmount();
  assert.deepEqual(changes, [{ kind: "removed", name: "cola" }]);
  assert.equal(JamlFiles.isMounted(), false);
  assert.deepEqual([...JamlFiles.list()], []);
  Errors.last();
  await assert.rejects(JamlFiles.load("perkeo"));
  assert.match(Errors.last() ?? "", /No folder is mounted/);
});
