// Filter files in a folder the user picks, through Bootsharp.FileSystem (guide: extensions/file-system).
// An exported interface module (guide: interop-modules); Bootsharp.Inject hands it IFileMounter
// (guide: dependency-injection). Built with the package by default; -p:MotelyFileSystem=false
// builds the same surface with isSupported() false.

using System.Text;
#if MOTELY_FILESYSTEM
using Bootsharp.FileSystem;
#endif

/// <summary>.yaml, .yml and .jaml filter files in a folder the user picks. Needs the Bootsharp.FileSystem
/// build; without it <see cref="IsSupported"/> is false and every file call rejects.</summary>
public interface IJamlFiles
{
    /// <summary>A filter file joined, left, changed or moved within the folder's filter list.</summary>
    event Action<JamlFileChange> OnChange;

    /// <summary>True when this build has Bootsharp.FileSystem and can mount a folder.</summary>
    bool IsSupported();

    /// <summary>True while a picked folder is mounted.</summary>
    bool IsMounted();

    /// <summary>Every filter file under the folder, sorted.</summary>
    string[] List();

    /// <summary>Asks the user for a folder. False when they cancel. A folder that cannot be
    /// mounted rejects and leaves the current one mounted.</summary>
    Task<bool> PickFolder();

    /// <summary>Releases the mounted folder and clears the list, each name leaving through
    /// <see cref="OnChange"/> as removed. Nothing when none is mounted.</summary>
    Task Unmount();

    /// <summary>The filter file's text.</summary>
    /// <param name="name">Name as <see cref="List"/> gives it.</param>
    Task<string> Load(string name);

    /// <summary>Writes the filter file, creating it when missing.</summary>
    /// <param name="name">Name as <see cref="List"/> gives it.</param>
    /// <param name="yaml">The filter text.</param>
    Task Save(string name, string yaml);

    /// <summary>Deletes the filter file.</summary>
    /// <param name="name">Name as <see cref="List"/> gives it.</param>
    Task Remove(string name);

    /// <summary>Moves the filter file to a new name. Rejects when that name is already taken.</summary>
    /// <param name="fromName">Current name.</param>
    /// <param name="toName">New name.</param>
    Task Rename(string fromName, string toName);
}

#if MOTELY_FILESYSTEM
/// <summary>JamlFiles over Bootsharp.FileSystem. JS calls <c>fs.init(IFileMounter)</c> from
/// <c>@rewaffle/bootsharp-file-system</c> before <c>boot()</c>. Every call that can fail runs
/// through <see cref="Guard"/>, so its reason reaches <c>Errors.last()</c>.</summary>
/// <param name="mounter">Bootsharp.FileSystem's mounter, injected by AddBootsharp.</param>
public sealed class JamlFilesModule(IFileMounter mounter) : IJamlFiles
{
    private Folder? _folder;

    /// <inheritdoc/>
    public event Action<JamlFileChange>? OnChange;

    /// <inheritdoc/>
    public bool IsSupported() => true;

    /// <inheritdoc/>
    public bool IsMounted() => _folder is not null;

    /// <inheritdoc/>
    public string[] List() => _folder is { } folder ? [.. folder.Names] : [];

    /// <inheritdoc/>
    public Task<bool> PickFolder() => Guard.RunAsync(async () =>
    {
        string? root = await mounter.PickRoot(new PickOptions { Id = "yaml-filters", Mode = PermissionMode.ReadWrite });
        if (root is null)
            return false;
        // The picked folder mounts before the current one is let go, so a mount that fails
        // (access denied, a folder that cannot be read) leaves the current folder as it was.
        // Mount reports the folder's existing files to its watcher; they are announced below,
        // once the folder is the mounted one.
        var next = new Folder(this, root);
        try
        {
            next.Fs = await mounter.Mount(
                root,
                next,
                new MountOptions { Mode = PermissionMode.ReadWrite, Ignore = [".git", "node_modules"] }
            );
        }
        catch
        {
            try
            {
                await mounter.Unmount(root);
            }
            catch
            {
                // It never got as far as mounting.
            }
            throw;
        }
        var previous = _folder;
        _folder = next;
        if (previous is not null)
        {
            Announce("removed", previous.Names);
            try
            {
                await mounter.Unmount(previous.Root);
            }
            catch (Exception e)
            {
                // The new folder is mounted either way.
                Errors.Record(e);
            }
        }
        Announce("added", next.Names);
        return true;
    });

    /// <inheritdoc/>
    public Task Unmount() => Guard.RunAsync(async () =>
    {
        if (_folder is not { } folder)
            return;
        _folder = null;
        Announce("removed", folder.Names);
        await mounter.Unmount(folder.Root);
    });

    /// <inheritdoc/>
    public Task<string> Load(string name) =>
        Guard.RunAsync(async () => Encoding.UTF8.GetString(await Fs.ReadFile(JamlFileNames.ToUri(name))));

    /// <inheritdoc/>
    public Task Save(string name, string yaml) =>
        Guard.RunAsync(() => Fs.WriteFile(JamlFileNames.ToUri(name), Encoding.UTF8.GetBytes(yaml)));

    /// <inheritdoc/>
    public Task Remove(string name) => Guard.RunAsync(() => Fs.DeleteFile(JamlFileNames.ToUri(name)));

    /// <inheritdoc/>
    public Task Rename(string fromName, string toName) => Guard.RunAsync(async () =>
    {
        var fs = Fs;
        string from = JamlFileNames.ToUri(fromName);
        string to = JamlFileNames.ToUri(toName);
        if (from == to)
            return;
        // Bootsharp.FileSystem moves by delete-then-write and writes over whatever is there.
        // A change of case alone is the same file where the folder ignores case, so it may go on.
        string target = JamlFileNames.ToName(to)!;
        bool caseOnly = string.Equals(from, to, StringComparison.OrdinalIgnoreCase);
        if (!caseOnly && _folder!.Names.Any(n => string.Equals(n, target, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A filter named '{target}' is already in the folder.");
        byte[] content = await fs.ReadFile(from);
        try
        {
            await fs.MoveFile(from, to);
        }
        catch
        {
            // The source may already be deleted: put it back rather than lose the filter.
            try
            {
                await fs.WriteFile(from, content);
            }
            catch (Exception e)
            {
                Errors.Record(e);
            }
            throw;
        }
    });

    private IFileSystem Fs =>
        _folder?.Fs ?? throw new InvalidOperationException("No folder is mounted: call JamlFiles.pickFolder() first.");

    private void Announce(string kind, IEnumerable<string> names)
    {
        foreach (string name in names.ToArray())
            Raise(new JamlFileChange(kind, name, null));
    }

    /// <summary>A subscriber that throws must not stop the folder's later changes from arriving.</summary>
    private void Raise(JamlFileChange change)
    {
        try
        {
            OnChange?.Invoke(change);
        }
        catch (Exception e)
        {
            Errors.Record(e);
        }
    }

    /// <summary>One mounted folder and its own filter list. Each mount has its own watcher, so a
    /// change that arrives late from a folder already let go cannot land in the next one's list.</summary>
    private sealed class Folder(JamlFilesModule owner, string root) : IFileWatcher
    {
        public readonly string Root = root;
        public readonly SortedSet<string> Names = new(NameOrder.Instance);
        public IFileSystem? Fs;

        /// <summary>Keeps <see cref="Names"/> in step with the folder and, while this is the
        /// mounted folder, raises <see cref="OnChange"/>.</summary>
        public Task HandleFileChanges(IReadOnlyList<Change> changes)
        {
            foreach (var change in changes)
            {
                if (!change.File)
                    continue;
                var type = change.Type switch
                {
                    ChangeType.Added => JamlFileEvent.Added,
                    ChangeType.Removed => JamlFileEvent.Removed,
                    ChangeType.Moved => JamlFileEvent.Moved,
                    _ => JamlFileEvent.Modified,
                };
                if (JamlFileNames.Change(type, change.Entry.Uri, change.Moved ? change.FromUri : null) is not { } seen)
                    continue;
                if (seen.Kind == "removed")
                    Names.Remove(seen.Name);
                else
                {
                    if (seen.FromName is { } from)
                        Names.Remove(from);
                    Names.Add(seen.Name);
                }
                if (owner._folder == this)
                    owner.Raise(seen);
            }
            return Task.CompletedTask;
        }
    }

    /// <summary>Sorted without regard to case, yet two names that differ only by case are two
    /// files where the folder tells them apart.</summary>
    private sealed class NameOrder : IComparer<string>
    {
        public static readonly NameOrder Instance = new();

        public int Compare(string? x, string? y)
        {
            int byLetters = StringComparer.OrdinalIgnoreCase.Compare(x, y);
            return byLetters != 0 ? byLetters : StringComparer.Ordinal.Compare(x, y);
        }
    }
}
#else
/// <summary>The build without Bootsharp.FileSystem: the same surface, and nothing can mount.</summary>
public sealed class JamlFilesModule : IJamlFiles
{
#pragma warning disable CS0067 // never raised here; declared so both builds export it
    /// <inheritdoc/>
    public event Action<JamlFileChange>? OnChange;
#pragma warning restore CS0067

    /// <inheritdoc/>
    public bool IsSupported() => false;

    /// <inheritdoc/>
    public bool IsMounted() => false;

    /// <inheritdoc/>
    public string[] List() => [];

    /// <inheritdoc/>
    public Task<bool> PickFolder() => Task.FromException<bool>(NoFileSystem());

    /// <inheritdoc/>
    public Task Unmount() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<string> Load(string name) => Task.FromException<string>(NoFileSystem());

    /// <inheritdoc/>
    public Task Save(string name, string yaml) => Task.FromException(NoFileSystem());

    /// <inheritdoc/>
    public Task Remove(string name) => Task.FromException(NoFileSystem());

    /// <inheritdoc/>
    public Task Rename(string fromName, string toName) => Task.FromException(NoFileSystem());

    /// <summary>The rejection, its message recorded for Errors.last(): a faulted task reaches JS
    /// without passing Guard.</summary>
    private static NotSupportedException NoFileSystem()
    {
        var e = new NotSupportedException(
            "This motely-wasm build has no folder access (built without Bootsharp.FileSystem).");
        Errors.Record(e);
        return e;
    }
}
#endif
