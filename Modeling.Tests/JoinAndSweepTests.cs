using System.Numerics;
using Xunit;

namespace Modeling.Tests;

/// <summary>
/// The bridge, the fill, the dissolves, the knife, the mirror, the spin and the smooth, on exact
/// counts of vertices, edges and faces, the volume a closed mesh holds and whether every edge is
/// shared by two faces running along it in opposite directions.
/// </summary>
/// <remarks>
/// A closed mesh of one piece has two more vertices and faces together than edges, and a ring, with
/// its hole through it, as many, which the counts here keep.
/// </remarks>
[Trait("Category", "Unit")]
public sealed class JoinAndSweepTests
{
    private const float Close = 1e-4f;


    private EditMesh New()
    {
        var mesh = new EditMesh();
        return mesh;
    }

    private static MeshInfo Info(EditMesh mesh) => MeshInfo.Of(mesh);

    private static (int, int, int) Counts(EditMesh mesh) => (Info(mesh).VertexCount, Info(mesh).EdgeCount, Info(mesh).FaceCount);

    private static Vector3 Center(EditMesh mesh, int face) =>
        mesh.Corners(face).ToArray().Aggregate(Vector3.Zero, (sum, corner) => sum + mesh.Position(corner)) / mesh.Corners(face).ToArray().Length;

    // The face whose middle is at a point.
    private static int FaceAt(EditMesh mesh, Vector3 middle) =>
        Enumerable.Range(0, Info(mesh).FaceCount).Single(face => (Center(mesh, face) - middle).Length() < Close);

    // A face of a meter standing upright from x 1 to 2 and y -1 to 0, facing +z, beside the Y axis.
    private EditMesh Upright()
    {
        var mesh = New();
        mesh.Plane(1, 1);
        mesh.Rotate(Vector3.UnitX, 90, Vector3.Zero);
        mesh.Move(new Vector3(1, 0, 0));
        return mesh;
    }

    [Fact]
    public void Two_Boxes_Facing_Faces_Bridged_Are_One_Solid_Holding_The_Gap()
    {
        var mesh = New();
        mesh.Box(Vector3.One);
        mesh.Box(Vector3.One, new Vector3(2, 0, 0));
        mesh.SelectFaces([FaceAt(mesh, new Vector3(1, 0.5f, 0.5f)), FaceAt(mesh, new Vector3(2, 0.5f, 0.5f))]);
        mesh.Bridge();

        Assert.Equal((16, 28, 14), Counts(mesh));
        Assert.True(Info(mesh).IsClosed);
        Assert.Equal(3, Info(mesh).Volume, Close);
        Assert.Equal(4, Info(mesh).SelectedFaceCount);

        // One face alone has nothing to bridge to.
        mesh.SelectFaces([0]);
        var refused = () => mesh.Bridge();
        Assert.ThrowsAny<InvalidOperationException>(refused);
    }

    [Fact]
    public void A_Hole_Whose_Edges_Are_Selected_Is_Filled_With_One_Face()
    {
        var mesh = New();
        mesh.Box(new Vector3(2, 3, 4));
        mesh.SelectFacing(Vector3.UnitY);
        mesh.Delete();
        mesh.SelectAll();
        mesh.Fill();

        Assert.Equal((8, 12, 6), Counts(mesh));
        Assert.True(Info(mesh).IsClosed);
        Assert.Equal(24, Info(mesh).Volume, Close);
        Assert.Equal(Vector3.UnitY, mesh.Normal(Enumerable.Range(0, 6).Single(face => mesh.IsFaceSelected(face))));
    }

    [Fact]
    public void A_Subdivided_Top_Dissolved_Is_One_Face_Of_Eight_Corners()
    {
        var mesh = New();
        mesh.Box(new Vector3(2, 3, 4));
        mesh.SelectAll();
        mesh.Subdivide();
        mesh.SelectFacing(Vector3.UnitY);
        mesh.Dissolve();

        // The top's middle vertex goes with the edges inside it, and its edges' middles stay on its
        // outline.
        Assert.Equal((25, 44, 21), Counts(mesh));
        Assert.True(Info(mesh).IsClosed);
        Assert.Equal(24, Info(mesh).Volume, Close);
        Assert.Equal(8, mesh.Corners(Enumerable.Range(0, 21).Single(face => mesh.IsFaceSelected(face))).ToArray().Count());
    }

    [Fact]
    public void A_Loop_Cuts_Edges_Dissolved_Join_The_Faces_It_Split()
    {
        var mesh = New();
        mesh.Box(new Vector3(2, 3, 4));
        mesh.SelectInside(Vector3.Zero, new Vector3(0, 3, 0));
        mesh.LoopCut(0.5f);
        mesh.DissolveEdges();

        // Each side one face again, keeping the loop's vertex on each upright edge.
        Assert.Equal((12, 16, 6), Counts(mesh));
        Assert.True(Info(mesh).IsClosed);
        Assert.Equal(24, Info(mesh).Volume, Close);
    }

    [Fact]
    public void A_Knife_Through_A_Whole_Box_Cuts_A_Loop_And_Through_Its_Top_Alone_Splits_The_Top()
    {
        var mesh = New();
        mesh.Box(new Vector3(2, 3, 4));
        mesh.SelectAll();
        mesh.Knife(Vector3.UnitY, new Vector3(0, 1.5f, 0));

        Assert.Equal((12, 20, 10), Counts(mesh));
        Assert.True(Info(mesh).IsClosed);
        Assert.Equal(24, Info(mesh).Volume, Close);
        Assert.Equal(4, Info(mesh).SelectedVertexCount);

        var top = New();
        top.Box(new Vector3(2, 3, 4));
        top.SelectFacing(Vector3.UnitY);
        top.Knife(Vector3.UnitX, new Vector3(1, 0, 0));
        Assert.Equal((10, 15, 7), Counts(top));
        Assert.True(Info(top).IsClosed);
    }

    [Fact]
    public void A_Half_Mirrored_Across_Its_Open_Side_Is_A_Whole_With_No_Vertex_Twice()
    {
        var mesh = New();
        mesh.Box(new Vector3(1, 2, 2));
        mesh.SelectFacing(-Vector3.UnitX);
        mesh.Delete();
        mesh.SelectAll();
        mesh.Mirror(Vector3.UnitX);

        Assert.Equal((12, 20, 10), Counts(mesh));
        Assert.True(Info(mesh).IsClosed);
        Assert.Equal(8, Info(mesh).Volume, Close);
        Assert.Equal(new Bounds(new Vector3(-1, 0, 0), new Vector3(1, 2, 2)), Info(mesh).Bounds);

        // A face in the plane is not copied, so a half left closed there keeps that face inside.
        var closed = New();
        closed.Box(new Vector3(1, 2, 2));
        closed.Mirror(Vector3.UnitX);
        Assert.Equal(11, Info(closed).FaceCount);
        Assert.False(Info(closed).IsClosed);
    }

    [Fact]
    public void A_Face_Spun_A_Whole_Turn_Round_An_Axis_Beside_It_Is_A_Closed_Ring()
    {
        var mesh = Upright();
        mesh.Spin(Vector3.UnitY, 360, 8);

        // Four corners on each of eight steps, and a face for each edge each step, a ring through
        // which the axis runs.
        Assert.Equal((32, 64, 32), Counts(mesh));
        Assert.True(Info(mesh).IsClosed);
        // An octagon's ring between radii one and two, a meter high.
        Assert.Equal(8 / 2f * MathF.Sin(MathF.Tau / 8) * (4 - 1), MathF.Abs(Info(mesh).Volume), Close);

        // A quarter turn in four steps keeps the face, turned, and is open where it started.
        var bent = Upright();
        bent.Spin(Vector3.UnitY, 90, 4);
        Assert.Equal((20, 17, 4), (Info(bent).VertexCount, Info(bent).FaceCount, Info(bent).OpenEdges));
        Assert.Equal(1, bent.Normal(Enumerable.Range(0, 17).Single(face => bent.IsFaceSelected(face))).X, Close);
    }

    [Fact]
    public void An_Edge_Spun_Alone_Sweeps_A_Band_Of_Its_Own()
    {
        var mesh = Upright();
        mesh.SelectInside(new Vector3(2, -1, 0), new Vector3(2, 0, 0));
        mesh.Spin(Vector3.UnitY, 360, 8);

        // The face, and a band of eight faces round the axis, its last step joined to its first.
        Assert.Equal((18, 27, 9), Counts(mesh));
        Assert.Equal(2, Info(mesh).SelectedVertexCount);
    }

    [Fact]
    public void A_Box_Smoothed_By_A_Half_Draws_In_To_A_Smaller_Box()
    {
        var mesh = New();
        mesh.Box(new Vector3(2), Vector3.Zero, centered: true);
        mesh.SelectAll();
        mesh.Smooth(0.5f);

        // Each corner moves half way to the mean of its three neighbors, a third of the way in.
        Assert.Equal(new Bounds(new Vector3(-2f / 3), new Vector3(2f / 3)), Info(mesh).Bounds);
        Assert.Equal(64f / 27, Info(mesh).Volume, Close);
        Assert.Equal((8, 12, 6), Counts(mesh));
    }

    [Theory]
    [InlineData("fill")]
    [InlineData("dissolve")]
    [InlineData("dissolve edges")]
    [InlineData("knife y at 0 1 0")]
    [InlineData("knife 0 -1 0")]
    [InlineData("mirror x at 3 0 0")]
    [InlineData("spin y 90 4 at 5 0 0")]
    [InlineData("spin z -45 2")]
    [InlineData("smooth 0.25")]
    public void A_Line_Run_Is_Written_Back_As_It_Was_Typed(string line)
    {
        var mesh = New();
        mesh.Box(new Vector3(2));
        mesh.SelectAll();
        mesh.Run(line);

        Assert.Equal(line, mesh.RecipeText.Split('\n', StringSplitOptions.RemoveEmptyEntries)[^1]);
    }

    [Fact]
    public void A_Measure_Out_Of_Range_Is_Refused()
    {
        var mesh = New();
        mesh.Box(Vector3.One);
        var refused = new Action[]
        {
            () => mesh.Knife(Vector3.Zero),
            () => mesh.Mirror(Vector3.Zero),
            () => mesh.Spin(Vector3.Zero, 90, 4),
            () => mesh.Spin(Vector3.UnitY, 0, 4),
            () => mesh.Spin(Vector3.UnitY, 400, 4),
            () => mesh.Spin(Vector3.UnitY, 90, 0),
            () => mesh.Smooth(2),
        };
        foreach (var call in refused) Assert.ThrowsAny<ArgumentException>(call);
        Assert.Equal((8, 12, 6), Counts(mesh));
    }
}
