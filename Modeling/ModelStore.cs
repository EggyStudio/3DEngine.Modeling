namespace Modeling;

/// <summary>
/// The models the commands reach, each by a name of one word, and the files every mesh it makes
/// reads and writes.
/// </summary>
/// <remarks>
/// The models are those the commands made and those a program named, so a game's level answers to
/// the commands by its name. A mesh a program named stays the program's when the name is forgotten,
/// and the store holds the names alone. One program shapes a model at a time, and nothing here
/// locks.
/// </remarks>
/// <param name="files">
/// What the meshes the store makes read and write, or null for
/// <see cref="ModelFolder.WorkingDirectory"/>.
/// </param>
public sealed class ModelStore(IModelFiles? files = null)
{
    private readonly List<(string Name, EditMesh Mesh)> _named = [];

    /// <summary>What the meshes the store makes read and write.</summary>
    public IModelFiles Files { get; } = files ?? ModelFolder.WorkingDirectory;

    /// <summary>The models by name, in the order they were named.</summary>
    public IReadOnlyList<(string Name, EditMesh Mesh)> All => _named;

    /// <summary>Whether a word is a model's name, one word with no space in it.</summary>
    public static bool IsName(string? name) => !string.IsNullOrEmpty(name) && !name.Any(char.IsWhiteSpace);

    /// <summary>Makes an empty mesh that reads and writes through the store's files.</summary>
    public EditMesh Make() => new(Files);

    /// <summary>Gives a mesh a name, in place of the mesh that had it.</summary>
    /// <exception cref="ArgumentException">The name is not one word.</exception>
    public void Name(string name, EditMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        if (!IsName(name)) throw new ArgumentException("A model's name is one word, with no space in it.", nameof(name));
        var at = _named.FindIndex(named => named.Name == name);
        if (at >= 0) _named[at] = (name, mesh);
        else _named.Add((name, mesh));
    }

    /// <summary>The mesh of a name, or null where no model has it.</summary>
    public EditMesh? Find(string name) => _named.Find(named => named.Name == name).Mesh;

    /// <summary>Forgets a name, and says whether a model had it.</summary>
    public bool Forget(string name) => _named.RemoveAll(named => named.Name == name) > 0;
}
