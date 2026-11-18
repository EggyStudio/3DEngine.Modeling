using System.Numerics;
using Xunit;

namespace Modeling.Tests;

/// <summary>
/// The README's binding run against <see cref="HostFiles"/> and <see cref="HostBinding"/>, an
/// engine with no window, no GPU and its files in memory, from a recipe that reads each of the
/// files a mesh reads to the engine's own meshes and materials.
/// </summary>
[Trait("Category", "Unit")]
public sealed class BindingTests
{
    private static HostFiles Files()
    {
        var files = new HostFiles();
        files.PutText("models/pillar.model", "model 1\ncylinder 0.5 3 8 at 5 0 5\nmaterial stone\n");
        // A crate's lid as a model reader gives it, a square of two triangles in a material of two
        // words.
        files.PutModel("models/lid.gltf", [new ModelPart([new(0, 4, 0), new(1, 4, 0), new(1, 4, 1), new(0, 4, 1)], [0, 2, 1, 0, 3, 2], "crate lid")]);
        files.PutImage("textures/bricks.png", 512);
        return files;
    }

    [Fact]
    public void A_Mesh_Reads_Every_File_Its_Recipe_Names_Through_The_Engines_Files()
    {
        var files = Files();
        var mesh = new EditMesh(files);
        mesh.Run("box 10 3 10");
        mesh.Run("join models/pillar.model");
        mesh.Run("import models/lid.gltf");
        mesh.Run("slot texture default textures/bricks.png");
        mesh.Run("select +y");

        // the joined pillar's slot and the imported lid's, its words joined by a hyphen
        Assert.Equal(new[] { "default", "stone", "crate-lid" }, mesh.Slots);
        Assert.Equal(512, mesh.SelectedTextureWidth());

        Assert.Equal("models/hall.model", mesh.Save("models/hall.model"));
        var again = EditMesh.Load("models/hall.model", files);
        Assert.Equal(mesh.Recipe, again.Recipe);
        Assert.Equal((mesh.VertexCount, mesh.FaceCount), (again.VertexCount, again.FaceCount));
        Assert.Throws<FileNotFoundException>(() => EditMesh.Load("models/none.model", files));
    }

    [Fact]
    public void A_Mesh_Builds_Into_The_Engines_Meshes_A_Slot_With_Their_Materials()
    {
        var mesh = new EditMesh(Files());
        mesh.Run("box 10 3 10");
        mesh.Run("join models/pillar.model");
        mesh.Run("slot texture default textures/bricks.png");
        mesh.Run("slot color stone 0.5 0.5 0.5");
        mesh.Run("slot emissive stone 2 1 0");

        var built = HostBinding.Build(mesh);
        Assert.Equal(new[] { "default", "stone" }, built.Select(part => part.Material.Name));
        Assert.Equal((Vector4.One, "textures/bricks.png"), (built[0].Material.Color, built[0].Material.Albedo));
        Assert.Equal((new Vector4(0.5f, 0.5f, 0.5f, 1), new Vector3(2, 1, 0)), (built[1].Material.Color, built[1].Material.Emissive));

        // A box's six faces of four corners each, two triangles a face, and the pillar's eight
        // sides and two ends of eight corners.
        Assert.Equal((24, 36), (built[0].Vertices.Length, built[0].Indices.Length));
        Assert.Equal(3 * (8 * 2 + 2 * 6), built[1].Indices.Length);
        foreach (var part in built)
        {
            Assert.All(part.Indices, index => Assert.InRange(index, 0u, (uint)part.Vertices.Length - 1));
            Assert.All(part.Vertices, vertex => Assert.Equal(1, vertex.Normal.Length(), 1e-4f));
        }
    }
}
