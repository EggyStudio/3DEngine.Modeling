using System.Numerics;

namespace Modeling;

// A model file read into the mesh through the mesh's files, its triangles joined back into faces,
// and the mesh written as a glTF file through them.
public sealed partial class EditMesh
{
    /// <summary>
    /// Adds the meshes of a model file, read by <see cref="IModelFiles.ReadModel"/>, as faces of
    /// this mesh, and selects them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A file's meshes are placed as its nodes place them, as a loaded model's are. Their vertices
    /// in one place are joined, since a file splits a vertex where its faces' normals or texture
    /// coordinates differ, and triangles in one plane and one material that share an edge are
    /// joined back into one face round them, so a box a file holds as twelve triangles is six
    /// faces. A material is a slot of its name, the words of a name joined by hyphens, and a mesh
    /// with none is in the first slot.
    /// </para>
    /// <para>
    /// The recipe keeps the file's path, so the mesh is read again from the file each time the
    /// recipe is run, as a join reads its model's recipe.
    /// </para>
    /// </remarks>
    /// <exception cref="FileNotFoundException">There is no such file.</exception> <exception
    /// cref="FormatException">The file is not a model the mesh's files read.</exception>
    public void Import(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (path.Any(char.IsWhiteSpace)) throw new ArgumentException("An imported model's path is one word, with no space in it.", nameof(path));
        var parts = Files.ReadModel(path);

        var first = _faces.Count;
        // A vertex for each place, within a hundredth of a millimeter.
        var welded = new Dictionary<(long, long, long), int>();
        int Weld(Vector3 point)
        {
            var key = ((long)MathF.Round(point.X * 1e5f), (long)MathF.Round(point.Y * 1e5f), (long)MathF.Round(point.Z * 1e5f));
            if (!welded.TryGetValue(key, out var vertex)) welded[key] = vertex = AddVertex(point);
            return vertex;
        }

        foreach (var part in parts)
        {
            var slot = part.Material is { } named ? SlotIndex(SlotName(named)) : 0;
            var vertices = part.Positions.Select(Weld).ToArray();
            for (int i = 0; i + 2 < part.Indices.Count; i += 3)
            {
                var (a, b, c) = (vertices[part.Indices[i]], vertices[part.Indices[i + 1]], vertices[part.Indices[i + 2]]);
                if (a != b && b != c && a != c) AddFace([a, b, c], slot);
            }
        }

        // Triangles in one plane and one slot sharing an edge, each group one face round it where
        // one loop goes round it.
        var imported = Enumerable.Range(first, _faces.Count - first).ToHashSet();
        var planes = imported.ToDictionary(face => face, face => (Normal: Normal(face), Offset: Vector3.Dot(Normal(face), _positions[_faces[face].Corners[0]])));
        bool InOnePlane(int a, int b) => _faces[a].Slot == _faces[b].Slot
            && Vector3.Dot(planes[a].Normal, planes[b].Normal) > 1 - 1e-5f && MathF.Abs(planes[a].Offset - planes[b].Offset) < 1e-4f;

        var going = new HashSet<int>();
        var joined = new List<Face>();
        foreach (var group in Groups(imported, InOnePlane))
        {
            if (group.Length < 2 || Loops(group) is not [var loop]) continue;
            going.UnionWith(group);
            joined.Add(new Face([.. loop], _faces[group[0]].Slot));
        }
        RemoveFaces(going);
        _faces.AddRange(joined);

        _selectedFaces.Clear();
        _selectedFaces.UnionWith(Enumerable.Range(first, _faces.Count - first));
        _selectedVertices.Clear();
        foreach (var face in _selectedFaces) _selectedVertices.UnionWith(_faces[face].Corners);
        Compact([]);
        Did($"import {path}");
    }

    /// <summary>
    /// Writes the mesh with its modifiers applied as a glTF 2.0 file through the mesh's files, a
    /// primitive and a material for each slot a face is in, and answers the file's full path.
    /// </summary>
    /// <param name="path">The <c>.gltf</c> file's path, as the mesh's files find it.</param>
    /// <param name="embedBuffer">
    /// Whether the buffer goes into the file as a data URI rather than a <c>.bin</c> file beside
    /// it.
    /// </param>
    /// <param name="prototypeColor">
    /// The color, as sRGB numbers from zero to one with its alpha, of a slot whose look gives
    /// neither a color nor a texture, by the slot's name, as the engine's prototype material tints
    /// its grid, or null for white.
    /// </param>
    /// <remarks>
    /// <para>
    /// Each material is named as its slot is, with the look's color decoded from sRGB to the linear
    /// factor glTF holds, its metalness, its roughness, 0.85 where the look gives none, its
    /// emission and its maps, each image named by its path from the file's folder. A slot no face
    /// is in is left out, as is a slot's grid, which is the engine's.
    /// </para>
    /// <para>
    /// The vertices are those <see cref="EditMeshBuild.Slots"/> gives, so a program that opens the
    /// file sees the faces' own normals and texture coordinates as the engine draws them.
    /// </para>
    /// </remarks>
    /// <exception cref="IOException">A file could not be written.</exception>
    /// <exception cref="UnauthorizedAccessException">
    /// A file or its folder may not be written.
    /// </exception>
    public string Export(string path, bool embedBuffer = false, Func<string, Vector4>? prototypeColor = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var modified = Modified();
        var used = Enumerable.Range(0, modified.FaceCount).Select(modified.SlotIndexOf).ToHashSet();
        var built = EditMeshBuild.Slots(this);
        var primitives = Enumerable.Range(0, built.Length).Where(used.Contains)
            .Select(slot => Primitive(slot, built[slot].Vertices, built[slot].Indices, prototypeColor)).ToArray();
        return GltfWriter.Write(Files, path, Path.GetFileNameWithoutExtension(path), primitives, embedBuffer);
    }

    private GltfWriter.Primitive Primitive(int slot, MeshVertex[] vertices, uint[] indices, Func<string, Vector4>? prototypeColor)
    {
        var (name, look) = (_slots[slot], _looks[slot]);
        var srgb = look.Color ?? (look.Texture is null && prototypeColor is not null ? prototypeColor(name) : Vector4.One);
        return new GltfWriter.Primitive(name, new Vector4(Srgb.Decode(srgb.X), Srgb.Decode(srgb.Y), Srgb.Decode(srgb.Z), srgb.W), vertices, indices)
        {
            Metallic = look.Metallic ?? 0,
            Roughness = look.Roughness ?? 0.85f,
            Emissive = look.Emissive ?? Vector3.Zero,
            Texture = look.Texture,
            NormalMap = look.NormalMap,
            OcclusionMap = look.OcclusionMap,
            MetallicRoughnessMap = look.MetallicRoughnessMap,
            EmissiveMap = look.EmissiveMap,
        };
    }

    /// <summary>
    /// A material's name as a slot's, one word, its words joined by hyphens, or the first slot's
    /// where it has none.
    /// </summary>
    private static string SlotName(string name)
    {
        var words = name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return words.Length == 0 ? DefaultSlot : string.Join('-', words);
    }
}
