using System.Numerics;
using System.Text.Json;
using Xunit;

namespace Modeling.Tests;

/// <summary>
/// An edit mesh written as glTF and read back by the module's reader and into an edit mesh, the
/// file held to the fields the glTF schema requires, and a known glTF read into an edit mesh and
/// counted.
/// </summary>
/// <remarks>
/// <c>two-pieces.gltf</c> holds a cube of a unit in the material "Stone Wall" and a square in
/// "Grass" on a node moved and scaled, so its counts are known.
/// </remarks>
[Trait("Category", "Unit")]
public sealed class ExchangeTests : IDisposable
{
    private const float Close = 1e-4f;
    private readonly TestFolder _folder = new("modeling-exchange-");

    public void Dispose() => _folder.Dispose();

    private static (int, int, int) Counts(EditMesh mesh) => (mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount);

    // The fixture beside the tests' sources, found from the folder the tests run in.
    private static string TwoPieces
    {
        get
        {
            var at = new DirectoryInfo(AppContext.BaseDirectory);
            while (at is not null && !File.Exists(Path.Combine(at.FullName, "Modeling.slnx"))) at = at.Parent;
            return Path.Combine(at?.FullName ?? throw new DirectoryNotFoundException("No Modeling.slnx above the tests."), "Modeling.Tests", "two-pieces.gltf");
        }
    }

    [Fact]
    public void A_Box_Written_As_GlTF_Reads_Back_With_The_Same_Counts()
    {
        var box = _folder.Mesh();
        box.Box(new Vector3(2, 3, 4));
        Assert.Equal(_folder.File("box.gltf"), box.Export("box.gltf"));

        Assert.True(File.Exists(_folder.File("box.bin")));
        Assert.Empty(Directory.GetFiles(_folder.Path, "*.part"));

        // The reader finds the box's twelve triangles in one material, between its corners.
        var part = Assert.Single(_folder.Files.ReadModel("box.gltf"));
        Assert.Equal(36, part.Indices.Count);
        Assert.Equal("default", part.Material);
        Assert.Equal((Vector3.Zero, new Vector3(2, 3, 4)), (part.Positions.Aggregate(Vector3.Min), part.Positions.Aggregate(Vector3.Max)));

        // Read into an edit mesh, it is the box's eight vertices and six faces again.
        var again = _folder.Mesh();
        again.Import("box.gltf");
        Assert.Equal((8, 12, 6), Counts(again));
        Assert.True(again.IsClosed);
        Assert.Equal(24, again.Volume, Close);
    }

    [Fact]
    public void A_Written_File_Has_Every_Field_The_GlTF_Schema_Requires()
    {
        var mesh = _folder.Mesh();
        mesh.Box(new Vector3(2, 3, 4));
        mesh.SelectFacing(Vector3.UnitY);
        mesh.Material("roof");
        mesh.Material("unused");
        mesh.SelectFacing(Vector3.UnitY);
        mesh.Material("roof");
        mesh.Export("house.gltf");

        using var document = JsonDocument.Parse(File.ReadAllText(_folder.File("house.gltf")));
        var gltf = document.RootElement;
        Assert.Equal("2.0", gltf.GetProperty("asset").GetProperty("version").GetString());

        var buffers = gltf.GetProperty("buffers").EnumerateArray().ToArray();
        var bytes = File.ReadAllBytes(_folder.File(buffers.Single().GetProperty("uri").GetString()!));
        Assert.Equal(bytes.Length, buffers.Single().GetProperty("byteLength").GetInt32());

        var views = gltf.GetProperty("bufferViews").EnumerateArray().ToArray();
        Assert.All(views, view => Assert.True(view.GetProperty("buffer").GetInt32() == 0
            && view.GetProperty("byteOffset").GetInt32() % 4 == 0
            && view.GetProperty("byteOffset").GetInt32() + view.GetProperty("byteLength").GetInt32() <= bytes.Length));

        var accessors = gltf.GetProperty("accessors").EnumerateArray().ToArray();
        foreach (var accessor in accessors)
        {
            Assert.True(accessor.GetProperty("bufferView").GetInt32() < views.Length);
            Assert.Contains(accessor.GetProperty("componentType").GetInt32(), new[] { 5125, 5126 });
            Assert.True(accessor.GetProperty("count").GetInt32() > 0);
            Assert.Contains(accessor.GetProperty("type").GetString(), new[] { "SCALAR", "VEC2", "VEC3" });
        }

        // A slot with no faces has no primitive, so the house has two, each with its material.
        var materials = gltf.GetProperty("materials").EnumerateArray().Select(material => material.GetProperty("name").GetString()).ToArray();
        Assert.Equal(new[] { "default", "roof" }, materials);
        var primitives = gltf.GetProperty("meshes").EnumerateArray().Single().GetProperty("primitives").EnumerateArray().ToArray();
        Assert.Equal(2, primitives.Length);
        foreach (var primitive in primitives)
        {
            var position = accessors[primitive.GetProperty("attributes").GetProperty("POSITION").GetInt32()];
            // a position accessor carries its bounds
            Assert.Equal(3, position.GetProperty("min").GetArrayLength());
            Assert.Equal(3, position.GetProperty("max").GetArrayLength());
            var indices = accessors[primitive.GetProperty("indices").GetInt32()].GetProperty("count").GetInt32();
            Assert.True(indices > 0 && indices % 3 == 0);
            Assert.True(primitive.GetProperty("material").GetInt32() < materials.Length);
        }
        Assert.Equal(0, gltf.GetProperty("scenes")[0].GetProperty("nodes")[0].GetInt32());
        Assert.Equal(0, gltf.GetProperty("nodes")[0].GetProperty("mesh").GetInt32());
    }

    [Fact]
    public void A_Buffer_Embedded_Leaves_No_File_Beside_And_Reads_Back()
    {
        var mesh = _folder.Mesh();
        mesh.Cylinder(1, 2, 8);
        mesh.Export("pillar.gltf", embedBuffer: true);

        Assert.False(File.Exists(_folder.File("pillar.bin")));
        Assert.Contains("data:application/octet-stream;base64,", File.ReadAllText(_folder.File("pillar.gltf")));
        var again = _folder.Mesh();
        again.Import("pillar.gltf");
        Assert.Equal((16, 24, 10), Counts(again));
    }

    [Fact]
    public void What_Is_Written_Is_The_Mesh_With_Its_Modifiers()
    {
        var half = _folder.Mesh();
        half.Box(new Vector3(1, 2, 2));
        half.SelectFacing(-Vector3.UnitX);
        half.Delete();
        half.ModifierMirror("x");
        half.Export("whole.gltf");

        var whole = _folder.Mesh();
        whole.Import("whole.gltf");
        // The two halves' faces in one plane are joined, so the box is six faces again, its
        // vertices on the plane between them left on the outlines of the faces they cross.
        Assert.Equal(6, whole.FaceCount);
        Assert.True(whole.IsClosed);
        Assert.Equal(8, whole.Volume, Close);
    }

    [Fact]
    public void A_Known_GlTF_Is_Read_Into_Faces_With_Its_Materials_As_Slots()
    {
        var mesh = new EditMesh();
        mesh.Import(TwoPieces);

        // The cube's eight vertices and six faces, and the square's four and one, its edges open.
        Assert.Equal((12, 16, 7), Counts(mesh));
        Assert.Equal(4, mesh.OpenEdges);
        Assert.Equal(1, mesh.Volume, Close);
        Assert.Equal(new Bounds(Vector3.Zero, new Vector3(5, 1, 2)), mesh.Bounds);
        Assert.Equal(new[] { "default", "Stone-Wall", "Grass" }, mesh.Slots);
        Assert.Equal(7, mesh.SelectedFaceCount);

        // Its line runs again from the file.
        Assert.Equal($"model 1\nimport {TwoPieces}\n", mesh.RecipeText);
        Assert.Equal((12, 16, 7), Counts(EditMesh.FromRecipe(mesh.RecipeText)));
    }

    [Fact]
    public void A_File_That_Is_Missing_Or_Not_A_Model_Is_Refused_And_Leaves_The_Mesh()
    {
        var mesh = _folder.Mesh();
        Assert.Throws<FileNotFoundException>(() => mesh.Import("none.gltf"));

        var bytes = new byte[512];
        new Random(7).NextBytes(bytes);
        File.WriteAllBytes(_folder.File("noise.gltf"), bytes);
        Assert.Throws<FormatException>(() => mesh.Import("noise.gltf"));
        Assert.Equal((0, 0, 0), Counts(mesh));

        // A recipe's line is refused with the line named, as any line that fails is.
        var read = Assert.Throws<FormatException>(() => EditMesh.FromRecipe("import noise.gltf", files: _folder.Files));
        Assert.StartsWith("Line 1 of the recipe", read.Message);
    }

    [Fact]
    public void A_Models_Material_Looks_Texture_Coordinates_And_Smoothing_Survive_Its_Import()
    {
        var made = _folder.Mesh();
        made.Cylinder(1, 2, 16);
        made.Shade(true);
        made.Material("stone");
        made.SlotColor("stone", new Vector4(0.8f, 0.6f, 0.4f, 1));
        made.SlotMap("stone", MaterialMap.Albedo, "textures/stone.png");
        made.SlotMap("stone", MaterialMap.Normal, "textures/stone_n.png");
        made.SlotMetallic("stone", 0.25f);
        made.SlotRoughness("stone", 0.5f);
        made.SlotEmissive("stone", new Vector3(3, 1.5f, 0));
        made.SelectFacing(Vector3.UnitY);
        made.UvRotate(30);
        made.Export("models/pillar.gltf");

        // glTF's emission is its factor times its strength, which carries no unit.
        var read = _folder.Files.ReadModel("models/pillar.gltf").Single(part => part.Material == "stone").Look!;
        Assert.Equal((new Vector3(3, 1.5f, 0), Materials.EmissiveUnit.Unitless), (read.Emissive, read.EmissiveUnit));

        var mesh = _folder.Mesh();
        mesh.Import("models/pillar.gltf");

        // the slot's look, its color through glTF's linear factor and back to five places
        var look = mesh.LookOf("stone");
        Assert.Equal(new Vector4(0.8f, 0.6f, 0.4f, 1), look.Color!.Value, new Vector4Close(1e-4f));
        Assert.Equal(("textures/stone.png", "textures/stone_n.png"), (look.Texture, look.NormalMap));
        Assert.Equal((0.25f, 0.5f), (look.Metallic, look.Roughness));
        Assert.Equal(new Vector3(3, 1.5f, 0), look.Emissive);

        // The cylinder's sixteen sides are smoothed into one another and its ends flat, and the top
        // keeps the coordinates turned on it.
        var top = Enumerable.Range(0, mesh.FaceCount).Single(face => mesh.Normal(face).Y > 0.99f);
        var side = Enumerable.Range(0, mesh.FaceCount).First(face => MathF.Abs(mesh.Normal(face).Y) < 0.01f);
        Assert.Equal((0f, EditMesh.DefaultSmoothAngle), (mesh.SmoothOf(top), mesh.SmoothOf(side)));
        var source = Enumerable.Range(0, made.FaceCount).Single(face => made.Normal(face).Y > 0.99f);
        Assert.Equal(Sorted(made, source), Sorted(mesh, top));
        Assert.Equal(18, mesh.FaceCount);
    }

    // A face's corners' places with their texture coordinates, in an order of their own, so two
    // faces of one outline compare whichever corner each starts from.
    private static (Vector3, Vector2)[] Sorted(EditMesh mesh, int face) =>
        [.. mesh.Corners(face).ToArray().Zip(mesh.UvsOf(face)).Select(pair => (Round(mesh.Position(pair.First)), Round(pair.Second)))
            .OrderBy(corner => (corner.Item1.X, corner.Item1.Y, corner.Item1.Z))];

    private static Vector3 Round(Vector3 value) => new(MathF.Round(value.X, 4), MathF.Round(value.Y, 4), MathF.Round(value.Z, 4));

    private static Vector2 Round(Vector2 value) => new(MathF.Round(value.X, 4), MathF.Round(value.Y, 4));

    private sealed class Vector4Close(float tolerance) : IEqualityComparer<Vector4>
    {
        public bool Equals(Vector4 a, Vector4 b) => Vector4.Distance(a, b) <= tolerance;

        public int GetHashCode(Vector4 value) => 0;
    }

    // A box exported with a texture, its material then edited in the file's JSON.
    private string Edited(string name, Action<System.Text.Json.Nodes.JsonObject> edit)
    {
        var box = _folder.Mesh();
        box.Box(Vector3.One);
        box.SlotMap("default", MaterialMap.Albedo, "textures/plain.png");
        box.Export($"models/{name}.gltf", embedBuffer: true);
        var path = _folder.File($"models/{name}.gltf");
        var root = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        edit(root["materials"]![0]!.AsObject());
        File.WriteAllText(path, root.ToJsonString());
        return $"models/{name}.gltf";
    }

    [Fact]
    public void A_Material_Is_Read_By_Materials_Rules_Refusing_What_GlTF_Does_Not_Allow()
    {
        // a roughness past one is refused, where it was clamped
        var rough = Edited("rough", material => material["pbrMetallicRoughness"]!["roughnessFactor"] = 1.5);
        Assert.Throws<FormatException>(() => _folder.Files.ReadModel(rough));

        // an alpha mode glTF does not name is refused, where it was read as opaque
        var alpha = Edited("alpha", material => material["alphaMode"] = "CUTOUT");
        Assert.Throws<FormatException>(() => _folder.Files.ReadModel(alpha));

        // a picture whose path holds a space keeps it, where it was left out
        var spaced = Edited("spaced", material =>
        {
            var root = material.Parent!.Parent!.AsObject();
            root["images"]![0]!["uri"] = "../textures/old%20stone.png";
        });
        var look = Assert.Single(_folder.Files.ReadModel(spaced)).Look!;
        Assert.Equal("textures/old stone.png", look.PictureOf(Materials.PictureRole.BaseColor)?.Picture.Path);
    }
}
