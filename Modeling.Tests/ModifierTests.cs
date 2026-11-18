using System.Diagnostics;
using System.Numerics;
using Xunit;

namespace Modeling.Tests;

/// <summary>
/// The modifier stack, each modifier's counts and volume on what the mesh builds into, the mesh
/// itself left as it was shaped, the stack reordered, taken from and applied, its lines in the
/// recipe, and the time a stack takes to build.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ModifierTests
{
    private const float Close = 1e-4f;


    private EditMesh Box(Vector3 size)
    {
        var mesh = new EditMesh();
        mesh.Box(size);
        return mesh;
    }

    private static (int, int, int) Counts(MeshInfo info) => (info.VertexCount, info.EdgeCount, info.FaceCount);

    private static string[] Lines(EditMesh mesh) => mesh.RecipeText.Split('\n', StringSplitOptions.RemoveEmptyEntries)[1..];

    [Fact]
    public void A_Half_Box_With_A_Mirror_Builds_Whole_And_Stays_A_Half()
    {
        var mesh = Box(new Vector3(1, 2, 2));
        mesh.SelectFacing(-Vector3.UnitX);
        mesh.Delete();
        mesh.ModifierMirror("x");

        var built = MeshInfo.Of(mesh.Modified());
        Assert.Equal((12, 20, 10), Counts(built));
        Assert.True(built.IsClosed);
        Assert.Equal(8, built.Volume, Close);

        // The half itself is as it was, open, and an edit to it is built mirrored.
        Assert.Equal((8, 12, 5), Counts(MeshInfo.Of(mesh)));
        mesh.SelectFacing(Vector3.UnitY);
        mesh.Move(new Vector3(0, 1, 0));
        Assert.Equal(12, MeshInfo.Of(mesh.Modified()).Volume, Close);
        Assert.Equal(new[] { "mirror x" }, mesh.Modifiers.ToArray());
    }

    [Fact]
    public void An_Array_Of_Three_Repeats_The_Mesh_By_Its_Offset_Or_Along_A_Curve()
    {
        var mesh = Box(Vector3.One);
        mesh.ModifierArray(3, new Vector3(2, 0, 0));
        var built = MeshInfo.Of(mesh.Modified());
        Assert.Equal((24, 36, 18), Counts(built));
        Assert.Equal(3, built.Volume, Close);
        Assert.Equal(new Bounds(Vector3.Zero, new Vector3(5, 1, 1)), built.Bounds);

        var curved = Box(Vector3.One);
        curved.ModifierArrayCurve([new Vector3(1, 0, 0), new Vector3(1, 0, 3), new Vector3(5, 2, 0)]);
        Assert.Equal(new Bounds(Vector3.Zero, new Vector3(5, 3, 4)), MeshInfo.Of(curved.Modified()).Bounds);
        Assert.Equal(3, MeshInfo.Of(curved.Modified()).Volume, Close);
    }

    [Fact]
    public void A_Box_Subdivided_To_Level_One_Has_26_Vertices_And_To_Level_Two_98()
    {
        var mesh = Box(new Vector3(2));
        mesh.ModifierSubdivision(1);
        var once = MeshInfo.Of(mesh.Modified());
        Assert.Equal((26, 48, 24), Counts(once));
        Assert.True(once.IsClosed);

        mesh.ModifierRemove(0);
        mesh.ModifierSubdivision(2);
        var twice = MeshInfo.Of(mesh.Modified());
        Assert.Equal((98, 192, 96), Counts(twice));
        Assert.True(twice.IsClosed);

        // Each level draws the box in toward a rounded shape, holding less.
        Assert.True(twice.Volume < once.Volume);
        Assert.True(twice.Volume > 0);
        Assert.True(once.Volume < 8);
    }

    [Fact]
    public void A_Solidified_Plane_Is_A_Closed_Slab_Out_Or_In()
    {
        var mesh = new EditMesh();
        mesh.Plane(2, 2);
        mesh.ModifierSolidify(0.1f);

        var outward = MeshInfo.Of(mesh.Modified());
        Assert.Equal((8, 12, 6), Counts(outward));
        Assert.True(outward.IsClosed);
        Assert.Equal(0.4f, outward.Volume, Close);
        Assert.Equal(new Bounds(Vector3.Zero, new Vector3(2, 0.1f, 2)), outward.Bounds);

        mesh.ModifierRemove(0);
        mesh.ModifierSolidify(-0.1f);
        var inward = MeshInfo.Of(mesh.Modified());
        Assert.True(inward.IsClosed);
        Assert.Equal(0.4f, inward.Volume, Close);
        Assert.Equal(-0.1f, inward.Bounds.Min.Y, Close);
    }

    [Fact]
    public void A_Bevel_Modifier_Rounds_Every_Edge_Or_Those_Past_An_Angle()
    {
        var every = Box(new Vector3(2));
        every.ModifierBevel(0.1f);
        Assert.Equal((24, 48, 26), Counts(MeshInfo.Of(every.Modified())));
        Assert.True(MeshInfo.Of(every.Modified()).IsClosed);

        // A top cut in four keeps the edges inside it, which lie between faces in one plane.
        var past = Box(new Vector3(2));
        past.SelectFacing(Vector3.UnitY);
        past.Subdivide();
        past.ModifierBevel(0.1f, 2, 30);
        var built = MeshInfo.Of(past.Modified());
        Assert.True(built.IsClosed);
        Assert.Equal(new Bounds(Vector3.Zero, new Vector3(2)), built.Bounds);
        var data = past;
        var top = data.Modified();
        Assert.Equal(4, Enumerable.Range(0, top.FaceCount).Count(face => top.Normal(face) == Vector3.UnitY));
    }

    [Fact]
    public void The_Stack_Applies_In_Order_And_Is_Moved_Taken_From_And_Applied()
    {
        var mesh = Box(new Vector3(2));
        mesh.ModifierTriangulate();
        mesh.ModifierSubdivision(1);
        // Twelve triangles, each three faces of four corners.
        Assert.Equal(36, MeshInfo.Of(mesh.Modified()).FaceCount);

        mesh.ModifierMove(1, 0);
        Assert.Equal(new[] { "subdivision 1", "triangulate" }, mesh.Modifiers.ToArray());
        // Twenty-four faces of four corners, each two triangles.
        Assert.Equal(48, MeshInfo.Of(mesh.Modified()).FaceCount);

        mesh.ModifierRemove(0);
        Assert.Equal(12, MeshInfo.Of(mesh.Modified()).FaceCount);

        mesh.ModifierApply(0);
        Assert.Empty(mesh.Modifiers.ToArray());
        Assert.Equal((8, 18, 12), Counts(MeshInfo.Of(mesh)));
        Assert.Equal(8, MeshInfo.Of(mesh).Volume, Close);

        // The apply is a line of the recipe, so an undo brings the modifier back and the box whole.
        Assert.True(mesh.Undo());
        Assert.Equal(new[] { "triangulate" }, mesh.Modifiers.ToArray());
        Assert.Equal(6, MeshInfo.Of(mesh).FaceCount);
        Assert.Equal(new[] { "box 2 2 2", "modifier triangulate", "modifier subdivision 1", "modifier move 1 0", "modifier remove 0" }, Lines(mesh));
    }

    [Fact]
    public void A_Stack_Read_Back_From_The_Recipe_Builds_The_Same()
    {
        var mesh = Box(new Vector3(1, 2, 2));
        mesh.SelectFacing(-Vector3.UnitX);
        mesh.Delete();
        mesh.ModifierMirror("xz", new Vector3(0, 0, -1));
        mesh.ModifierArray(2, new Vector3(0, 3, 0));
        mesh.ModifierArrayCurve([Vector3.Zero, new Vector3(5, 0, 0)]);
        mesh.ModifierBevel(0.05f, 2, 45);
        mesh.ModifierSolidify(0.1f);

        var again = EditMesh.FromRecipe(mesh.RecipeText);
        Assert.Equal(new[] { "mirror xz at 0 0 -1", "array 2 0 3 0", "array curve 0 0 0 5 0 0", "bevel 0.05 2 45", "solidify 0.1" }, again.Modifiers.ToArray());
        Assert.Equal(MeshInfo.Of(mesh.Modified()), MeshInfo.Of(again.Modified()));
    }

    [Fact]
    public void What_Is_Built_Is_The_Mesh_With_Its_Modifiers()
    {
        var mesh = Box(Vector3.One);
        mesh.ModifierArray(3, new Vector3(2, 0, 0));
        var data = mesh;

        Assert.Equal(3 * 12 * 3, EditMeshBuild.Slots(data).Single().Indices.Length);
    }

    [Fact]
    public void A_Stack_Of_A_Subdivision_A_Mirror_And_An_Array_Builds_Within_Its_Budget()
    {
        // 6,144 faces of four corners after the subdivision, twice that mirrored and four times
        // that repeated, 49,152 faces and 98,304 triangles built.
        var mesh = Box(new Vector3(1, 2, 2));
        mesh.ModifierSubdivision(5);
        mesh.ModifierMirror("x", new Vector3(-0.5f, 0, 0));
        mesh.ModifierArray(4, new Vector3(3, 0, 0));
        var data = mesh;

        var clock = Stopwatch.StartNew();
        var built = EditMeshBuild.Slots(data);
        clock.Stop();

        Assert.Equal(6144 * 2 * 4 * 6, built.Single().Indices.Length);
        Assert.True(clock.ElapsedMilliseconds < Budget, $"a stack this size builds within {Budget} ms");
    }

    // A build whose work grows with the faces once over is in time, and one whose work for each
    // face grows with the count of faces, as a corner's normal found by searching every face would,
    // is past it. The stack's first build in a process takes 135 to 151 ms, timed round
    // EditMeshBuild.Slots in a Debug build three times, so the bound stands at ten times that.
    private const long Budget = 1500;

    [Fact]
    public void A_Boolean_Modifier_Takes_A_Solid_Away_At_Each_Build_And_Leaves_The_Mesh_As_Shaped()
    {
        var mesh = Box(new Vector3(2));
        mesh.ModifierBoolean("subtract", "box 1 1 1 at 1.5 1.5 1.5");

        var built = mesh.Modified();
        Assert.True(MeshInfo.Of(built).IsClosed);
        Assert.Equal(8f - 0.125f, built.Volume, Close);
        Assert.Equal(6, mesh.FaceCount);
        Assert.Equal(new[] { "boolean subtract box 1 1 1 at 1.5 1.5 1.5" }, mesh.Modifiers);
        Assert.Equal("modifier boolean subtract box 1 1 1 at 1.5 1.5 1.5", Lines(mesh)[^1]);
        Assert.Equal(built.Volume, EditMesh.FromRecipe(mesh.RecipeText).Modified().Volume, Close);

        // A boolean with a model that is not there, or of no kind, is refused as it is added, the
        // stack as it was.
        Assert.Throws<FileNotFoundException>(() => mesh.ModifierBoolean("union", "models/not-there.model"));
        Assert.Throws<ArgumentException>(() => mesh.ModifierBoolean("join", "box 1 1 1"));
        Assert.Throws<FormatException>(() => mesh.ModifierBoolean("union", "box 1 1"));
        Assert.Throws<FormatException>(() => mesh.Run("modifier boolean join box 1 1 1"));
        Assert.Single(mesh.Modifiers);
    }

    [Fact]
    public void A_Modifier_Line_After_Add_Is_Read_As_The_Line_Alone()
    {
        var mesh = Box(new Vector3(2));
        mesh.Run("modifier add subdivision 1");
        mesh.Run("modifier add triangulate");

        Assert.Equal(new[] { "subdivision 1", "triangulate" }, mesh.Modifiers);
        Assert.Equal(new[] { "modifier subdivision 1", "modifier triangulate" }, Lines(mesh)[^2..]);
        Assert.Throws<FormatException>(() => mesh.Run("modifier add"));
    }

    [Fact]
    public void A_Modifier_Out_Of_Range_Is_Refused()
    {
        var mesh = Box(Vector3.One);
        var refused = new Action[]
        {
            () => mesh.ModifierMirror("w"),
            () => mesh.ModifierMirror("xx"),
            () => mesh.ModifierArray(0, Vector3.UnitX),
            () => mesh.ModifierArrayCurve([Vector3.Zero]),
            () => mesh.ModifierBevel(0),
            () => mesh.ModifierSubdivision(0),
            () => mesh.ModifierSolidify(0),
            () => mesh.ModifierMove(0, 0),
            () => mesh.ModifierRemove(0),
            () => mesh.ModifierApply(0),
        };
        foreach (var call in refused) Assert.ThrowsAny<ArgumentException>(call);
        Assert.Empty(mesh.Modifiers.ToArray());
    }
}
