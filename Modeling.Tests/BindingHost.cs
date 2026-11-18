using System.Numerics;

namespace Modeling.Tests;

// An engine of the smallest kind, which binds the module as the README's section Binding it in your
// engine says, so the tests run the section's steps. Its files are kept in memory, its vertex has a
// layout of its own and its material holds what a slot's look names. The part between the two
// marker lines is the code the section shows, word for word.

// The README's block from here

/// <summary>
/// The engine's files, kept in memory by their paths, which every mesh it makes reads and writes.
/// </summary>
public sealed class HostFiles : IModelFiles
{
    private readonly Dictionary<string, string> _texts = [];
    private readonly Dictionary<string, IReadOnlyList<ModelPart>> _models = [];
    private readonly Dictionary<string, int> _images = [];

    /// <summary>Puts a text file, a recipe, at a path.</summary>
    public void PutText(string path, string text) => _texts[path] = text;

    /// <summary>
    /// Puts a model file's meshes at a path, as the engine's model reader gives them.
    /// </summary>
    public void PutModel(string path, IReadOnlyList<ModelPart> parts) => _models[path] = parts;

    /// <summary>Puts an image of a width at a path.</summary>
    public void PutImage(string path, int width) => _images[path] = width;

    /// <inheritdoc/>
    public string ReadText(string path) =>
        _texts.TryGetValue(path, out var text) ? text : throw new FileNotFoundException($"There is no file at {path}.", path);

    /// <inheritdoc/>
    public string WriteText(string path, string text)
    {
        _texts[path] = text;
        return path;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ModelPart> ReadModel(string path) =>
        _models.TryGetValue(path, out var parts) ? parts : throw new FileNotFoundException($"There is no model at {path}.", path);

    /// <inheritdoc/>
    public int? ImageWidth(string path) => _images.TryGetValue(path, out var width) ? width : null;
}

/// <summary>The engine's vertex, laid out as its renderer reads it.</summary>
public readonly record struct HostVertex(Vector3 Position, Vector3 Normal, Vector2 TexCoord);

/// <summary>
/// The engine's material, made from a slot's look, white where the look gives no color.
/// </summary>
public sealed record HostMaterial(string Name, Vector4 Color, string? Albedo, string? Normal, Vector3 Emissive);

/// <summary>
/// The engine's mesh of one material slot, its vertices, its triangles and its material.
/// </summary>
public sealed record HostMesh(HostVertex[] Vertices, uint[] Indices, HostMaterial Material);

/// <summary>What the engine does with an edit mesh, a mesh of its own for each slot.</summary>
public static class HostBinding
{
    /// <summary>
    /// Builds an edit mesh into the engine's meshes, one a slot, in the order of its slots.
    /// </summary>
    public static HostMesh[] Build(EditMesh mesh)
    {
        var built = EditMeshBuild.Slots(mesh);
        var meshes = new HostMesh[built.Length];
        for (var slot = 0; slot < built.Length; slot++)
        {
            var (vertices, indices) = built[slot];
            var look = mesh.LookAt(slot);
            var material = new HostMaterial(mesh.Slots[slot], look.Color ?? Vector4.One, look.Texture, look.NormalMap, look.Emissive ?? Vector3.Zero);
            meshes[slot] = new HostMesh([.. vertices.Select(v => new HostVertex(v.Position, v.Normal, v.Uv))], indices, material);
        }
        return meshes;
    }
}

// The end marker.
