using System.Numerics;
using Xunit;

namespace Modeling.Tests;

/// <summary>
/// An edit mesh's material slots added, renamed and taken away, and given a look, a color, a
/// texture and the maps of a glTF material, which the recipe writes and reads.
/// </summary>
/// <remarks>
/// The textures are the headers of PNG files 512 pixels wide, written in the test's folder, which
/// the mesh's files read below it, so a look names them by a path from there as a game's recipe
/// names its textures. A header is all <see cref="IModelFiles.ImageWidth"/> reads.
/// </remarks>
[Trait("Category", "Unit")]
public sealed class SlotTests : IDisposable
{
    private const string Bricks = "bricks.png";
    private readonly TestFolder _folder = new("modeling-slot-");

    public SlotTests()
    {
        Png(_folder.File(Bricks), 512, 512);
        Png(_folder.File("bricks_n.png"), 512, 256);
    }

    public void Dispose() => _folder.Dispose();

    // A PNG's signature and its IHDR chunk, a width and height of four bytes each, high byte first,
    // then eight bits a channel of red, green and blue, with no checksum after.
    private static void Png(string path, int width, int height)
    {
        var header = new byte[24];
        new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R' }.CopyTo(header, 0);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(16), width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(20), height);
        File.WriteAllBytes(path, [.. header, 8, 2, 0, 0, 0]);
    }

    private EditMesh Room()
    {
        var mesh = _folder.Mesh();
        mesh.Box(new Vector3(6, 3, 5));
        mesh.Material("wall");
        mesh.SelectFacing(-Vector3.UnitY);
        mesh.Material("floor");
        return mesh;
    }

    private static string[] Lines(EditMesh mesh) => [.. mesh.RecipeText.TrimEnd('\n').Split('\n').Skip(1)];

    private static int Count(EditMesh mesh, string slot) =>
        Enumerable.Range(0, MeshInfo.Of(mesh).FaceCount).Count(face => mesh.SlotOf(face) == slot);

    [Fact]
    public void A_Slot_Is_Added_Renamed_And_Taken_Away_Its_Faces_Going_To_The_First()
    {
        var mesh = Room();
        mesh.SlotAdd("trim");
        Assert.Equal(new[] { "default", "wall", "floor", "trim" }, mesh.Slots.ToArray());

        mesh.SlotRename("floor", "planks");
        Assert.Equal(new[] { "default", "wall", "planks", "trim" }, mesh.Slots.ToArray());
        Assert.Equal(1, Count(mesh, "planks"));

        mesh.SlotRemove("wall");
        Assert.Equal(new[] { "default", "planks", "trim" }, mesh.Slots.ToArray());
        // the walls and the ceiling were the wall slot's
        Assert.Equal(5, Count(mesh, "default"));
        Assert.Equal(1, Count(mesh, "planks"));
        Assert.Equal(new[] { "slot add trim", "slot rename floor planks", "slot remove wall" }, Lines(mesh)[^3..]);

        var first = () => mesh.SlotRemove("default");
        Assert.ThrowsAny<InvalidOperationException>(first);
        var taken = () => mesh.SlotRename("trim", "planks");
        Assert.ThrowsAny<ArgumentException>(taken);
        var missing = () => mesh.SlotAdd("trim");
        Assert.ThrowsAny<ArgumentException>(missing);
    }

    [Fact]
    public void The_First_Slot_Renamed_Is_Named_Default_Again_When_The_Rename_Is_Undone()
    {
        var mesh = Room();
        mesh.SlotRename("default", "ceiling");
        Assert.Equal("ceiling", mesh.Slots.ToArray()[0]);

        Assert.True(mesh.Undo());
        Assert.Equal(new[] { "default", "wall", "floor" }, mesh.Slots.ToArray());
    }

    [Fact]
    public void A_Look_Is_Lines_Of_The_Recipe_That_Make_It_Again()
    {
        var mesh = Room();
        mesh.SlotColor("wall", new Vector4(0.8f, 0.749f, 0.702f, 1));
        mesh.SlotMap("wall", MaterialMap.Albedo, Bricks);
        mesh.SlotMap("wall", MaterialMap.Normal, "bricks_n.png");
        mesh.SlotEmissive("lamp", new Vector3(4, 3.5f, 3));
        mesh.SlotMetallic("floor", 0.5f);
        mesh.SlotRoughness("floor", 0.3f);
        mesh.SlotMap("wall", MaterialMap.Normal, null);

        Assert.Equal(new[] { "slot color wall 0.8 0.749 0.702", $"slot texture wall {Bricks}", "slot normal-map wall bricks_n.png", "slot emissive lamp 4 3.5 3", "slot metallic floor 0.5", "slot roughness floor 0.3", "slot normal-map wall none" }, Lines(mesh)[^7..]);

        var again = EditMesh.FromRecipe(mesh.RecipeText, files: _folder.Files);
        Assert.Equal(new[] { "default", "wall", "floor", "lamp" }, again.Slots.ToArray());
        var wall = again.LookAt(1);
        Assert.Same(wall, again.LookOf("wall"));
        Assert.Throws<ArgumentException>(() => again.LookOf("glass"));
        Assert.Equal((new Vector4(0.8f, 0.749f, 0.702f, 1), Bricks, (string?)null), (wall.Color, wall.Texture, wall.NormalMap));
        // three places bring a byte back whole
        Assert.Equal((204, 191, 179), ((int)MathF.Round(wall.Color!.Value.X * 255), (int)MathF.Round(wall.Color.Value.Y * 255), (int)MathF.Round(wall.Color.Value.Z * 255)));

        var height = () => mesh.SlotMap("wall", (MaterialMap)99, Bricks);
        Assert.ThrowsAny<ArgumentException>(height);
        var bright = () => mesh.SlotMetallic("wall", 1.5f);
        Assert.ThrowsAny<ArgumentOutOfRangeException>(bright);
    }

    [Fact]
    public void A_Density_Given_Alone_Reads_The_Width_Of_The_Slots_Texture()
    {
        var mesh = _folder.Mesh();
        mesh.Box(new Vector3(2));
        mesh.SlotMap("default", MaterialMap.Albedo, Bricks);
        mesh.SelectFacing(Vector3.UnitY);

        Assert.Equal(512, mesh.SelectedTextureWidth());
        mesh.UvDensity(256, mesh.SelectedTextureWidth()!.Value);
        Assert.Equal("uv density 256 512", Lines(mesh)[^1]);

        mesh.SlotMap("default", MaterialMap.Albedo, null);
        // the slot has no texture to read the width of
        Assert.Null(mesh.SelectedTextureWidth());
    }
}
