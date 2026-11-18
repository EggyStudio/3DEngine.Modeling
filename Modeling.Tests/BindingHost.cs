// An engine of the smallest kind, which binds the module as the README's section Binding it in your
// engine says, so the tests run the section's steps. The part between the two marker lines is the
// code the section shows, word for word.

// The README's block from here
using System.Numerics;
using Modeling;

// The engine's files, kept here in memory by their paths, which every mesh it makes reads and
// writes. An engine on a disk finds them as it finds a program's other files, and its own model
// reader gives a model file's meshes.
public sealed class HostFiles : IModelFiles
{
    private readonly Dictionary<string, string> _texts = [];
    private readonly Dictionary<string, byte[]> _bytes = [];
    private readonly Dictionary<string, IReadOnlyList<ModelPart>> _models = [];
    private readonly Dictionary<string, int> _images = [];

    public void PutText(string path, string text) => _texts[path] = text;

    public void PutModel(string path, IReadOnlyList<ModelPart> parts) => _models[path] = parts;

    public void PutImage(string path, int width) => _images[path] = width;

    public string ReadText(string path) =>
        _texts.TryGetValue(path, out var text) ? text : throw new FileNotFoundException($"There is no file at {path}.", path);

    public string WriteText(string path, string text)
    {
        _texts[path] = text;
        return path;
    }

    public string WriteBytes(string path, byte[] bytes)
    {
        _bytes[path] = bytes;
        return path;
    }

    public byte[] Bytes(string path) => _bytes[path];

    public IReadOnlyList<ModelPart> ReadModel(string path) =>
        _models.TryGetValue(path, out var parts) ? parts : throw new FileNotFoundException($"There is no model at {path}.", path);

    public int? ImageWidth(string path) => _images.TryGetValue(path, out var width) ? width : null;
}

// The engine's vertex, laid out as its renderer reads it, and its material and mesh of a slot.
public readonly record struct HostVertex(Vector3 Position, Vector3 Normal, Vector2 TexCoord);

public sealed record HostMaterial(string Name, Vector4 Color, string? Albedo, string? Normal, Vector3 Emissive);

public sealed record HostMesh(HostVertex[] Vertices, uint[] Indices, HostMaterial Material);

// An edit mesh built into the engine's meshes, one a slot in the order of its slots, each vertex
// copied into the engine's and each slot's look into its material, white where it gives no color.
public static class HostBinding
{
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
