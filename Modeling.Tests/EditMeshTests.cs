using System.Numerics;
using Xunit;

namespace Modeling.Tests;

/// <summary>
/// The edit mesh's primitives, its selection and what moves it, on exact counts of vertices, edges
/// and faces, the volume a closed mesh holds, and whether every edge is shared by two faces running
/// along it in opposite directions.
/// </summary>
/// <remarks>
/// A closed mesh whose faces all face out holds a positive volume, so the volume tells a primitive
/// turned inside out from one that is right, and the open edges tell one with a hole or a face
/// turned the wrong way. A solid pushed out from an outline of n corners has 2n vertices, 3n edges
/// and n + 2 faces, which the primitives built that way are held to.
/// </remarks>
[Trait("Category", "Unit")]
public sealed class EditMeshTests
{
    private const float Close = 1e-4f;


    private EditMesh New()
    {
        var mesh = new EditMesh();
        return mesh;
    }

    public static TheoryData<string, int, int, int, float> Primitives => new()
    {
        // name, vertices, edges, faces, volume
        { "box", 8, 12, 6, 24f },
        { "cylinder", 16, 24, 10, 8f / 2f * MathF.Sin(MathF.Tau / 8f) * 2f },
        { "square cylinder", 8, 12, 6, 4f },
        { "cone", 7, 12, 7, 6f / 2f * MathF.Sin(MathF.Tau / 6f) * 3f / 3f },
        { "wedge", 6, 9, 5, 12f },
        { "stairs", 20, 30, 12, 2f * 0.25f * 0.5f * (1 + 2 + 3 + 4) },
        { "arch", 36, 54, 20, 8f / 2f * MathF.Sin(MathF.PI / 8f) * (4f - 2.25f) * 1f },
    };

    private EditMesh Make(string name)
    {
        var mesh = New();
        switch (name)
        {
            case "box": mesh.Box(new Vector3(2, 3, 4)); break;
            case "cylinder": mesh.Cylinder(1, 2, 8); break;
            case "square cylinder": mesh.Cylinder(1, 2, 4); break;
            case "cone": mesh.Cone(1, 3, 6); break;
            case "wedge": mesh.Wedge(new Vector3(2, 3, 4)); break;
            case "stairs": mesh.Stairs(4, 0.25f, 0.5f, 2); break;
            case "arch": mesh.Arch(2, 0.5f, 1, 8); break;
            default: throw new ArgumentOutOfRangeException(nameof(name));
        }
        return mesh;
    }

    // The faces of a mesh whose normal is exactly one direction.
    private static int[] Facing(EditMesh mesh, Vector3 normal) =>
        Enumerable.Range(0, MeshInfo.Of(mesh).FaceCount).Where(face => mesh.Normal(face) == normal).ToArray();

    [Theory]
    [MemberData(nameof(Primitives))]
    public void Each_Solid_Primitive_Has_Its_Counts_Is_Closed_Faces_Out_And_Is_Selected_Whole(string name, int vertices, int edges, int faces, float volume)
    {
        var info = MeshInfo.Of(Make(name));

        Assert.Equal((vertices, edges, faces), (info.VertexCount, info.EdgeCount, info.FaceCount));
        Assert.Equal(0, info.OpenEdges);
        Assert.True(info.IsClosed);
        Assert.Equal(volume, info.Volume, Close);

        // Selected whole, so the operation after it acts on it, and placed by its lowest corner at
        // the origin where nothing says otherwise.
        Assert.Equal((vertices, edges, faces), (info.SelectedVertexCount, info.SelectedEdgeCount, info.SelectedFaceCount));
        Assert.Equal(Vector3.Zero, info.Bounds.Min);
    }

    [Fact]
    public void A_Plane_Is_One_Face_Facing_Up_With_Four_Open_Edges()
    {
        var mesh = New();
        mesh.Plane(2, 3);
        var info = MeshInfo.Of(mesh);

        Assert.Equal((4, 4, 1), (info.VertexCount, info.EdgeCount, info.FaceCount));
        Assert.Equal(4, info.OpenEdges);
        Assert.False(info.IsClosed);
        Assert.Equal(Vector3.UnitY, mesh.Normal(0));
        Assert.Equal(new Bounds(Vector3.Zero, new Vector3(2, 0, 3)), info.Bounds);
    }

    [Fact]
    public void A_Primitive_Is_Placed_By_Its_Lowest_Corner_Or_By_Its_Middle()
    {
        var cornered = New();
        cornered.Box(new Vector3(2, 3, 4), new Vector3(1, 0, -2));
        Assert.Equal(new Bounds(new Vector3(1, 0, -2), new Vector3(3, 3, 2)), MeshInfo.Of(cornered).Bounds);

        var centered = New();
        centered.Box(new Vector3(2, 3, 4), new Vector3(0, 1.5f, 0), centered: true);
        Assert.Equal(new Bounds(new Vector3(-1, 0, -2), new Vector3(1, 3, 2)), MeshInfo.Of(centered).Bounds);

        // A cylinder of eight sides stands its corners half a side round from the axes, so it is as
        // wide along X as along Z, and placed by its corner it starts at the origin all the same.
        var round = New();
        round.Cylinder(1, 2, 8);
        var bounds = MeshInfo.Of(round).Bounds;
        Assert.Equal(Vector3.Zero, bounds.Min);
        Assert.Equal(bounds.Max.Z, bounds.Max.X, Close);
        Assert.Equal(2, bounds.Max.Y);
    }

    [Fact]
    public void Stairs_Rise_Along_Z_From_Their_First_Step_To_A_Back_As_Tall_As_The_Flight()
    {
        var mesh = New();
        mesh.Stairs(4, 0.25f, 0.5f, 2);
        Assert.Equal(new Bounds(Vector3.Zero, new Vector3(2, 1, 2)), MeshInfo.Of(mesh).Bounds);

        // Four treads face up, one a step, each a step higher and a run further along.
        mesh.SelectFacing(Vector3.UnitY);
        Assert.Equal(4, MeshInfo.Of(mesh).SelectedFaceCount);
        var treads = Enumerable.Range(0, MeshInfo.Of(mesh).FaceCount).Where(face => mesh.IsFaceSelected(face))
            .Select(face => mesh.Corners(face).ToArray().Aggregate(Vector3.Zero, (sum, corner) => sum + mesh.Position(corner)) / 4)
            .OrderBy(center => center.Z).ToArray();
        for (int step = 0; step < 4; step++)
        {
            Assert.Equal((step + 1) * 0.25f, treads[step].Y, Close);
            Assert.Equal((step + 0.5f) * 0.5f, treads[step].Z, Close);
        }

        // Four risers face toward the start, and the back faces the other way, as tall as the
        // flight.
        mesh.SelectFacing(-Vector3.UnitZ);
        Assert.Equal(4, MeshInfo.Of(mesh).SelectedFaceCount);
        mesh.SelectFacing(Vector3.UnitZ);
        Assert.Equal(1, MeshInfo.Of(mesh).SelectedFaceCount);
    }

    [Fact]
    public void A_Wedge_Rises_Along_Z_And_Its_Slope_At_Forty_Five_Degrees_Is_A_Floor()
    {
        var mesh = New();
        mesh.Wedge(new Vector3(2, 3, 4));
        foreach (var vertex in Enumerable.Range(0, MeshInfo.Of(mesh).VertexCount).Where(vertex => mesh.Position(vertex).Y > 0))
            Assert.Equal(4, mesh.Position(vertex).Z);

        // A slope that rises as far as it runs is taken as facing up, where a character walks it.
        var even = New();
        even.Wedge(new Vector3(2, 2, 2));
        even.SelectFacing(Vector3.UnitY);
        Assert.Equal(1, MeshInfo.Of(even).SelectedFaceCount);
        even.SelectFacing(-Vector3.UnitZ);
        Assert.Equal(0, MeshInfo.Of(even).SelectedFaceCount);
    }

    [Fact]
    public void Two_Primitives_In_One_Mesh_Are_Counted_Together_And_The_Second_Is_What_Is_Selected()
    {
        var mesh = New();
        mesh.Box(Vector3.One);
        mesh.Box(Vector3.One, new Vector3(3, 0, 0));
        var info = MeshInfo.Of(mesh);

        Assert.Equal((16, 24, 12), (info.VertexCount, info.EdgeCount, info.FaceCount));
        Assert.Equal(2, info.Volume, Close);
        Assert.True(info.IsClosed);
        Assert.Equal((8, 6), (info.SelectedVertexCount, info.SelectedFaceCount));
        Assert.DoesNotContain(Enumerable.Range(0, 8), vertex => mesh.IsVertexSelected(vertex));
    }

    [Fact]
    public void The_Top_Of_A_Box_Selected_And_Moved_Up_Makes_It_Taller()
    {
        var mesh = New();
        mesh.Box(new Vector3(2, 3, 4));
        mesh.SelectFacing(Vector3.UnitY);
        var info = MeshInfo.Of(mesh);
        Assert.Equal((4, 4, 1), (info.SelectedVertexCount, info.SelectedEdgeCount, info.SelectedFaceCount));

        mesh.Move(new Vector3(0, 1, 0));

        info = MeshInfo.Of(mesh);
        Assert.Equal(32, info.Volume, Close);
        Assert.Equal(new Vector3(2, 4, 4), info.Bounds.Max);
        Assert.True(info.IsClosed);
    }

    [Fact]
    public void Inverting_The_Top_Selects_The_Other_Five_Faces_And_Every_Corner()
    {
        var mesh = New();
        mesh.Box(Vector3.One);
        mesh.SelectFacing(Vector3.UnitY);
        mesh.SelectInvert();
        var info = MeshInfo.Of(mesh);

        Assert.Equal((8, 12, 5), (info.SelectedVertexCount, info.SelectedEdgeCount, info.SelectedFaceCount));
        Assert.False(mesh.IsFaceSelected(Facing(mesh, Vector3.UnitY).Single()));

        mesh.SelectAll();
        mesh.SelectInvert();
        Assert.Equal((0, 0), (MeshInfo.Of(mesh).SelectedVertexCount, MeshInfo.Of(mesh).SelectedFaceCount));
    }

    [Fact]
    public void Faces_Are_Added_And_Taken_Away_With_The_Corners_No_Other_Selected_Face_Has()
    {
        var mesh = New();
        mesh.Box(Vector3.One);
        var top = Facing(mesh, Vector3.UnitY).Single();
        var front = Facing(mesh, Vector3.UnitZ).Single();

        mesh.SelectFaces([top]);
        mesh.SelectFaces([front], SelectionMode.Add);
        Assert.Equal((6, 2), (MeshInfo.Of(mesh).SelectedVertexCount, MeshInfo.Of(mesh).SelectedFaceCount));

        // The two corners the top and the front share stay with the front.
        mesh.SelectFaces([top], SelectionMode.Remove);
        Assert.Equal((4, 1), (MeshInfo.Of(mesh).SelectedVertexCount, MeshInfo.Of(mesh).SelectedFaceCount));
        Assert.True(mesh.IsFaceSelected(front));

        var past = () => mesh.SelectFaces([6]);
        Assert.ThrowsAny<ArgumentOutOfRangeException>(past);
    }

    [Fact]
    public void A_Box_Drawn_Round_Half_A_Mesh_Picks_Its_Vertices_And_The_Faces_Wholly_Inside()
    {
        var mesh = New();
        mesh.Box(new Vector3(2, 1, 1));
        mesh.SelectInside(new Vector3(1, -1, -1), new Vector3(3, 2, 2));

        // The far half's four corners, on the box's side, and the one face all of whose corners are
        // there.
        var info = MeshInfo.Of(mesh);
        Assert.Equal((4, 4, 1), (info.SelectedVertexCount, info.SelectedEdgeCount, info.SelectedFaceCount));
        Assert.Equal(Vector3.UnitX, mesh.Normal(Enumerable.Range(0, 6).Single(face => mesh.IsFaceSelected(face))));

        // Taking away the corners at the top takes the face with them.
        mesh.SelectInside(new Vector3(-1, 0.5f, -1), new Vector3(3, 2, 2), SelectionMode.Remove);
        Assert.Equal((2, 0), (MeshInfo.Of(mesh).SelectedVertexCount, MeshInfo.Of(mesh).SelectedFaceCount));
    }

    [Fact]
    public void Faces_Put_In_A_Slot_Are_Selected_By_It()
    {
        var mesh = New();
        mesh.Box(new Vector3(4, 3, 4));
        mesh.SelectFacing(Vector3.UnitY);
        mesh.Material("ceiling");
        mesh.SelectFacing(-Vector3.UnitY);
        mesh.Material("floor");

        Assert.Equal(new[] { "default", "ceiling", "floor" }, mesh.Slots.ToArray());
        mesh.SelectSlot("floor");
        Assert.Equal(1, MeshInfo.Of(mesh).SelectedFaceCount);
        Assert.Equal("floor", mesh.SlotOf(Facing(mesh, -Vector3.UnitY).Single()));
        mesh.SelectSlot("default");
        Assert.Equal(4, MeshInfo.Of(mesh).SelectedFaceCount);
        mesh.SelectSlot("ceiling", SelectionMode.Add);
        Assert.Equal(5, MeshInfo.Of(mesh).SelectedFaceCount);
        mesh.SelectSlot("roof");
        Assert.Equal(0, MeshInfo.Of(mesh).SelectedFaceCount);

        var twoWords = () => mesh.Material("two words");
        Assert.ThrowsAny<ArgumentException>(twoWords);
    }

    [Fact]
    public void A_Quarter_Turn_About_Y_Turns_A_Box_In_Place()
    {
        var mesh = New();
        mesh.Box(new Vector3(2, 3, 4));
        mesh.Rotate(Vector3.UnitY, 90);
        var info = MeshInfo.Of(mesh);

        // The middle of the box holding it stays, at (1, 1.5, 2), and its width and depth trade.
        Assert.Equal(4, (info.Bounds.Max.X - info.Bounds.Min.X), Close);
        Assert.Equal(2, (info.Bounds.Max.Z - info.Bounds.Min.Z), Close);
        Assert.Equal(1, ((info.Bounds.Min.X + info.Bounds.Max.X) / 2), Close);
        Assert.Equal(2, ((info.Bounds.Min.Z + info.Bounds.Max.Z) / 2), Close);
        Assert.Equal(24, info.Volume, Close);
        Assert.True(info.IsClosed);

        // About a point given, the corner at the origin, a quarter turn about Y takes +X to -Z.
        var pivoted = New();
        pivoted.Box(new Vector3(2, 1, 1));
        pivoted.Rotate(Vector3.UnitY, 90, Vector3.Zero);
        Assert.Equal(-2, MeshInfo.Of(pivoted).Bounds.Min.Z, Close);
        Assert.Equal(1, MeshInfo.Of(pivoted).Bounds.Max.X, Close);
    }

    [Fact]
    public void Scaling_Stretches_About_The_Middle_And_A_Mirror_Still_Faces_Out()
    {
        var mesh = New();
        mesh.Box(new Vector3(2));
        mesh.Scale(new Vector3(2, 1, 1));
        Assert.Equal(new Bounds(new Vector3(-1, 0, 0), new Vector3(3, 2, 2)), MeshInfo.Of(mesh).Bounds);
        Assert.Equal(16, MeshInfo.Of(mesh).Volume, Close);

        mesh.Scale(new Vector3(-1, 1, 1));
        Assert.Equal(16, MeshInfo.Of(mesh).Volume, Close);
        Assert.True(MeshInfo.Of(mesh).IsClosed);

        // About a point given, which stays where it is.
        var pivoted = New();
        pivoted.Box(Vector3.One);
        pivoted.Scale(new Vector3(3), Vector3.Zero);
        Assert.Equal(new Bounds(Vector3.Zero, new Vector3(3)), MeshInfo.Of(pivoted).Bounds);
    }

    [Fact]
    public void The_Grid_Is_A_Power_Of_Two_Of_A_Meter_And_Snap_Puts_The_Selection_On_It()
    {
        var mesh = New();
        mesh.Box(Vector3.One, new Vector3(0.3f, 0.6f, -0.2f));
        Assert.Equal(1, MeshInfo.Of(mesh).GridStep);

        foreach (var step in new[] { 0.3f, 1f / 32, 128f })
        {
            var refused = () => mesh.Grid(step);
            Assert.ThrowsAny<ArgumentOutOfRangeException>(refused);
        }

        mesh.Grid(0.5f);
        mesh.Snap();
        Assert.Equal(0.5f, MeshInfo.Of(mesh).GridStep);
        Assert.Equal(new Bounds(new Vector3(0.5f, 0.5f, 0), new Vector3(1.5f, 1.5f, 1)), MeshInfo.Of(mesh).Bounds);

        // A typed measure is taken as given, off the grid.
        mesh.Move(new Vector3(0.2f, 0, 0));
        Assert.Equal(0.7f, MeshInfo.Of(mesh).Bounds.Min.X, Close);
    }

    [Fact]
    public void What_Is_Not_Selected_Stays_Where_It_Is()
    {
        var mesh = New();
        mesh.Box(Vector3.One);
        mesh.Box(Vector3.One, new Vector3(3, 0, 0));
        mesh.Move(new Vector3(0, 5, 0));

        Assert.Equal(new Bounds(Vector3.Zero, new Vector3(4, 6, 1)), MeshInfo.Of(mesh).Bounds);
        Assert.Equal(1, Enumerable.Range(0, 8).Max(vertex => mesh.Position(vertex).Y), Close);

        mesh.SelectNone();
        mesh.Move(Vector3.One);
        Assert.Equal(0, MeshInfo.Of(mesh).SelectedVertexCount);
        Assert.Equal(new Bounds(Vector3.Zero, new Vector3(4, 6, 1)), MeshInfo.Of(mesh).Bounds);
    }

    [Fact]
    public void A_Measure_Of_A_Primitive_Is_Above_Zero_And_A_Refused_One_Adds_Nothing()
    {
        var mesh = New();

        var flat = () => mesh.Box(new Vector3(1, 0, 1));
        var twoSides = () => mesh.Cylinder(1, 1, 2);
        var solidArch = () => mesh.Arch(1, 1, 1, 4);
        var noSteps = () => mesh.Stairs(0, 1, 1, 1);
        var notANumber = () => mesh.Plane(float.NaN, 1);
        foreach (var refused in new[] { flat, twoSides, solidArch, noSteps, notANumber })
            Assert.ThrowsAny<ArgumentOutOfRangeException>(refused);

        Assert.Equal(0, MeshInfo.Of(mesh).VertexCount);
    }

    [Fact]
    public void Each_Operation_Writes_Its_Line_In_The_Recipes_Words()
    {
        var mesh = New();
        mesh.Box(new Vector3(8, 3, 6));
        mesh.Box(Vector3.One, new Vector3(0, 5, 0), centered: true);
        mesh.SelectFacing(Vector3.UnitY);
        mesh.SelectFaces([0, 3], SelectionMode.Add);
        mesh.SelectInside(Vector3.Zero, new Vector3(4, 3, 4), SelectionMode.Remove);
        mesh.Move(new Vector3(0, 0.5f, 0));
        mesh.Rotate(Vector3.UnitY, 90, new Vector3(1, 0, 1));
        mesh.Scale(new Vector3(2));
        mesh.Scale(new Vector3(1, 2, 1));
        mesh.Grid(0.25f);
        mesh.Material("wall");
        mesh.Shade(true);

        var data = mesh;
        Assert.Equal(new[] { "box 8 3 6", "box 1 1 1 at 0 5 0 center", "select +y", "select add face 0 3", "select remove box 0 0 0 4 3 4", "move 0 0.5 0", "rotate y 90 at 1 0 1", "scale 2", "scale 1 2 1", "grid 0.25", "material wall", "shade smooth 30" }, data.Recipe);
    }
}
