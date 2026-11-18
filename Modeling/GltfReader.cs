using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;

namespace Modeling;

/// <summary>
/// Reads the triangles of a glTF 2.0 file, a <c>.gltf</c> with its buffers beside it or inside it
/// or a <c>.glb</c>, as the meshes of each material, for <see cref="ModelFolder"/> to give an
/// import.
/// </summary>
/// <remarks>
/// <para>
/// The default scene's nodes are walked down from the root, each node's transform, a matrix or a
/// translation, rotation and scale, put on its children's, and every primitive of every node's mesh
/// read in the space of the scene, so a model of parts placed in a modeling program comes in as it
/// was placed. A node whose transform mirrors runs its triangles the other way, so they face out.
/// Triangles, strips and fans are read, and points and lines are left, since they make no faces.
/// </para>
/// <para>
/// Each material's triangles are one part named as the material is, and the triangles of no
/// material one part with no name. Accessors of every component type are read, normalized where
/// they say so, and a sparse accessor is refused.
/// </para>
/// </remarks>
internal static class GltfReader
{
    private const uint Magic = 0x46546C67, JsonChunk = 0x4E4F534A, BinaryChunk = 0x004E4942;

    /// <summary>The file's meshes in the scene's space, a part for each material.</summary> <param
    /// name="path">The file's path, which a buffer beside it is found from.</param> <param
    /// name="exists">Whether a file stands at a path.</param> <param name="read">A file's
    /// bytes.</param>
    /// <exception cref="FileNotFoundException">
    /// There is no such file, or a buffer it names is not there.
    /// </exception>
    /// <exception cref="FormatException">
    /// It is not glTF this reads, which the message says with the file's path.
    /// </exception>
    public static IReadOnlyList<ModelPart> Read(string path, Func<string, bool> exists, Func<string, byte[]> read)
    {
        if (!exists(path)) throw new FileNotFoundException($"There is no model at {path}.", path);

        var bytes = read(path);
        try
        {
            return Parse(path, bytes, exists, read);
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or KeyNotFoundException or InvalidOperationException
                                          or IndexOutOfRangeException or ArgumentException or OverflowException)
        {
            throw new FormatException($"{path} is not a glTF model this reads, as {error.Message.TrimEnd('.')}.", error);
        }
    }

    private static IReadOnlyList<ModelPart> Parse(string path, byte[] bytes, Func<string, bool> exists, Func<string, byte[]> read)
    {
        var folder = Path.GetDirectoryName(path) ?? "";
        ReadOnlyMemory<byte> json = bytes;
        byte[]? binary = null;

        if (bytes.Length >= 12 && BinaryPrimitives.ReadUInt32LittleEndian(bytes) == Magic)
        {
            // A binary file is a header and chunks, the JSON first and the buffer after it.
            json = default;
            for (var at = 12; at + 8 <= bytes.Length;)
            {
                var length = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(at));
                var type = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(at + 4));
                if (length < 0 || at + 8 + length > bytes.Length) throw new InvalidDataException("a chunk runs past the end of the file");
                if (type == JsonChunk) json = bytes.AsMemory(at + 8, length);
                else if (type == BinaryChunk) binary = bytes.AsSpan(at + 8, length).ToArray();
                at += 8 + length;
            }

            if (json.IsEmpty) throw new InvalidDataException("the binary file has no JSON chunk");
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("asset", out var asset) || !asset.TryGetProperty("version", out var version) || !(version.GetString() ?? "").StartsWith("2.", StringComparison.Ordinal))
            throw new InvalidDataException("its asset names no glTF version of 2");

        var buffers = Array(root, "buffers").Select(buffer => Buffer(buffer, folder, binary, exists, read)).ToArray();
        var file = new Source(root, buffers);
        var names = Array(root, "materials").Select(material => material.TryGetProperty("name", out var named) ? named.GetString() : null).ToArray();
        var triangles = new List<(Vector3 A, Vector3 B, Vector3 C, int Material)>();

        // The default scene's nodes, or every node no other names as a child where there is none.
        var nodes = Array(root, "nodes");
        IEnumerable<int> roots;
        var scenes = Array(root, "scenes");
        if (scenes.Length > 0)
        {
            var scene = root.TryGetProperty("scene", out var chosen) ? chosen.GetInt32() : 0;
            roots = Array(scenes[scene], "nodes").Select(node => node.GetInt32());
        }
        else
        {
            var children = nodes.SelectMany(node => Array(node, "children")).Select(child => child.GetInt32()).ToHashSet();
            roots = Enumerable.Range(0, nodes.Length).Where(node => !children.Contains(node));
        }

        foreach (var node in roots) Walk(file, nodes, node, Matrix4x4.Identity, triangles, 0);

        // A part for each material in the order the file names them, its triangles' corners each a
        // vertex of their own, which the import welds.
        return triangles.GroupBy(triangle => triangle.Material).OrderBy(group => group.Key).Select(group =>
        {
            var positions = group.SelectMany(triangle => (Vector3[])[triangle.A, triangle.B, triangle.C]).ToArray();
            var name = group.Key >= 0 && group.Key < names.Length && !string.IsNullOrWhiteSpace(names[group.Key]) ? names[group.Key] : null;
            return new ModelPart(positions, Enumerable.Range(0, positions.Length).ToArray(), name);
        }).ToArray();
    }

    /// <summary>Reads a node's mesh in the scene's space, and its children's below it.</summary>
    private static void Walk(Source file, JsonElement[] nodes, int index, Matrix4x4 parent, List<(Vector3, Vector3, Vector3, int)> triangles, int depth)
    {
        if (depth > 64) throw new InvalidDataException("its nodes are children of one another past 64 deep");

        var node = nodes[index];
        var world = Local(node) * parent;

        if (node.TryGetProperty("mesh", out var mesh))
        {
            var mirrored = world.GetDeterminant() < 0f;
            foreach (var primitive in Array(Array(file.Root, "meshes")[mesh.GetInt32()], "primitives")) Primitive(file, primitive, world, mirrored, triangles);
        }

        foreach (var child in Array(node, "children")) Walk(file, nodes, child.GetInt32(), world, triangles, depth + 1);
    }

    private static void Primitive(Source file, JsonElement primitive, Matrix4x4 world, bool mirrored, List<(Vector3, Vector3, Vector3, int)> triangles)
    {
        var mode = primitive.TryGetProperty("mode", out var given) ? given.GetInt32() : 4;
        if (mode is not (4 or 5 or 6)) return;

        var attributes = primitive.GetProperty("attributes");
        var positions = file.Floats(attributes.GetProperty("POSITION").GetInt32(), 3);
        var count = positions.Length / 3;
        var order = primitive.TryGetProperty("indices", out var indices)
            ? file.Indices(indices.GetInt32())
            : Enumerable.Range(0, count).ToArray();
        var material = primitive.TryGetProperty("material", out var named) ? named.GetInt32() : -1;

        Vector3 At(int vertex)
        {
            if ((uint)vertex >= (uint)count) throw new InvalidDataException($"an index names vertex {vertex} of {count}");
            return Vector3.Transform(new Vector3(positions[3 * vertex], positions[3 * vertex + 1], positions[3 * vertex + 2]), world);
        }

        void Add(int a, int b, int c) => triangles.Add(mirrored ? (At(a), At(c), At(b), material) : (At(a), At(b), At(c), material));

        switch (mode)
        {
            case 4:
                for (var i = 0; i + 2 < order.Length; i += 3) Add(order[i], order[i + 1], order[i + 2]);
                break;
            case 5:
                // Every other triangle of a strip runs the other way, which its first two corners
                // swapped mend.
                for (var i = 0; i + 2 < order.Length; i++)
                {
                    if (i % 2 == 0) Add(order[i], order[i + 1], order[i + 2]);
                    else Add(order[i + 1], order[i], order[i + 2]);
                }

                break;
            default:
                for (var i = 1; i + 1 < order.Length; i++) Add(order[0], order[i], order[i + 1]);
                break;
        }
    }

    /// <summary>
    /// A node's own transform, its matrix or its translation, rotation and scale.
    /// </summary>
    private static Matrix4x4 Local(JsonElement node)
    {
        // glTF lists a matrix by its columns, and the numerics' matrix holds a column as a row.
        if (node.TryGetProperty("matrix", out var matrix))
        {
            var m = matrix.EnumerateArray().Select(value => value.GetSingle()).ToArray();
            if (m.Length != 16) throw new InvalidDataException("a node's matrix is not sixteen numbers");
            return new Matrix4x4(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);
        }

        var t = Numbers(node, "translation", 0f, 0f, 0f);
        var r = Numbers(node, "rotation", 0f, 0f, 0f, 1f);
        var s = Numbers(node, "scale", 1f, 1f, 1f);
        return Matrix4x4.CreateScale(s[0], s[1], s[2]) * Matrix4x4.CreateFromQuaternion(new Quaternion(r[0], r[1], r[2], r[3])) * Matrix4x4.CreateTranslation(t[0], t[1], t[2]);
    }

    /// <summary>
    /// A buffer's bytes, from inside the file, from beside it, or from a binary file's own chunk.
    /// </summary>
    private static byte[] Buffer(JsonElement buffer, string folder, byte[]? binary, Func<string, bool> exists, Func<string, byte[]> read)
    {
        if (!buffer.TryGetProperty("uri", out var uri)) return binary ?? throw new InvalidDataException("a buffer has no URI and the file no binary chunk");

        var text = uri.GetString() ?? "";
        if (text.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = text.IndexOf(',');
            if (comma < 0 || !text[..comma].EndsWith(";base64", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("a buffer's data URI is not base64");
            return Convert.FromBase64String(text[(comma + 1)..]);
        }

        var beside = Path.Combine(folder, Uri.UnescapeDataString(text)).Replace('\\', '/');
        if (!exists(beside)) throw new FileNotFoundException($"The buffer {beside} the model names is not there.", beside);
        return read(beside);
    }

    private static JsonElement[] Array(JsonElement owner, string property) =>
        owner.ValueKind == JsonValueKind.Object && owner.TryGetProperty(property, out var list) && list.ValueKind == JsonValueKind.Array ? [.. list.EnumerateArray()] : [];

    private static float[] Numbers(JsonElement owner, string property, params float[] otherwise) =>
        owner.ValueKind == JsonValueKind.Object && owner.TryGetProperty(property, out var list) && list.ValueKind == JsonValueKind.Array
            ? [.. list.EnumerateArray().Select(value => value.GetSingle())]
            : otherwise;

    /// <summary>The file's JSON and buffers, and what reads an accessor from them.</summary>
    private sealed class Source(JsonElement root, byte[][] buffers)
    {
        public readonly JsonElement Root = root;

        /// <summary>
        /// An accessor's numbers, so many to an element, made floats and normalized where it says
        /// so.
        /// </summary>
        public float[] Floats(int index, int components)
        {
            var (bytes, start, stride, type, count, normalized) = Layout(index, components);
            var values = new float[count * components];
            for (var element = 0; element < count && bytes.Length > 0; element++)
            {
                for (var component = 0; component < components; component++)
                {
                    var at = bytes.AsSpan(start + element * stride + component * Size(type));
                    values[element * components + component] = type switch
                    {
                        5126 => BinaryPrimitives.ReadSingleLittleEndian(at),
                        5121 => normalized ? at[0] / 255f : at[0],
                        5120 => normalized ? MathF.Max((sbyte)at[0] / 127f, -1f) : (sbyte)at[0],
                        5123 => normalized ? BinaryPrimitives.ReadUInt16LittleEndian(at) / 65535f : BinaryPrimitives.ReadUInt16LittleEndian(at),
                        5122 => normalized ? MathF.Max(BinaryPrimitives.ReadInt16LittleEndian(at) / 32767f, -1f) : BinaryPrimitives.ReadInt16LittleEndian(at),
                        _ => BinaryPrimitives.ReadUInt32LittleEndian(at),
                    };
                }
            }

            return values;
        }

        /// <summary>
        /// An accessor of indices, whole numbers of one, two or four bytes, read as they are.
        /// </summary>
        public int[] Indices(int index)
        {
            var (bytes, start, stride, type, count, _) = Layout(index, 1);
            var values = new int[count];
            for (var element = 0; element < count && bytes.Length > 0; element++)
            {
                var at = bytes.AsSpan(start + element * stride);
                values[element] = type switch
                {
                    5121 => at[0],
                    5123 => BinaryPrimitives.ReadUInt16LittleEndian(at),
                    5125 => (int)Math.Min(BinaryPrimitives.ReadUInt32LittleEndian(at), int.MaxValue),
                    _ => throw new InvalidDataException($"indices of component type {type} are not whole numbers"),
                };
            }

            return values;
        }

        /// <summary>
        /// Where an accessor's elements lie in its buffer, no bytes where it has no view and its
        /// elements are zeros.
        /// </summary>
        private (byte[] Bytes, int Start, int Stride, int Type, int Count, bool Normalized) Layout(int index, int components)
        {
            var accessor = Array(Root, "accessors")[index];
            if (accessor.TryGetProperty("sparse", out _)) throw new InvalidDataException("a sparse accessor is not read");

            var count = accessor.GetProperty("count").GetInt32();
            var type = accessor.GetProperty("componentType").GetInt32();
            var width = Width(accessor.GetProperty("type").GetString());
            if (width != components) throw new InvalidDataException($"an accessor holds {width} numbers an element where {components} were looked for");
            var normalized = accessor.TryGetProperty("normalized", out var flag) && flag.GetBoolean();
            if (!accessor.TryGetProperty("bufferView", out var viewIndex)) return ([], 0, 0, type, count, normalized);

            var view = Array(Root, "bufferViews")[viewIndex.GetInt32()];
            var bytes = buffers[view.GetProperty("buffer").GetInt32()];
            var size = Size(type);
            var stride = view.TryGetProperty("byteStride", out var given) ? given.GetInt32() : size * components;
            var start = (view.TryGetProperty("byteOffset", out var viewOffset) ? viewOffset.GetInt32() : 0)
                + (accessor.TryGetProperty("byteOffset", out var offset) ? offset.GetInt32() : 0);

            if (count > 0 && start + (long)(count - 1) * stride + size * components > bytes.Length) throw new InvalidDataException("an accessor runs past its buffer");
            return (bytes, start, stride, type, count, normalized);
        }

        private static int Size(int type) => type switch
        {
            5120 or 5121 => 1,
            5122 or 5123 => 2,
            5125 or 5126 => 4,
            _ => throw new InvalidDataException($"component type {type} is no glTF type"),
        };

        private static int Width(string? type) => type switch
        {
            "SCALAR" => 1,
            "VEC2" => 2,
            "VEC3" => 3,
            "VEC4" => 4,
            _ => throw new InvalidDataException($"accessor type {type} is not read"),
        };
    }
}
