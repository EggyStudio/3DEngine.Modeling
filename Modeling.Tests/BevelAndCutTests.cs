using System.Numerics;
using Xunit;

namespace Modeling.Tests;

/// <summary>
/// The bevel, the loop cut and the subdivision, on exact counts of vertices, edges and faces, the
/// volume a closed mesh holds and whether every edge is shared by two faces running along it in
/// opposite directions.
/// </summary>
/// <remarks>
/// A closed mesh of one piece with no hole through it has two more vertices and faces together than
/// edges, which every count here keeps. A bevel takes from a box what lies outside its round bands,
/// so a round bevel holds less than the box and more than the flat chamfer of the same width, whose
/// bands are the round ones' chords.
/// </remarks>
[Trait("Category", "Unit")]
public sealed class BevelAndCutTests
{
    private const float Close = 1e-4f;


    private EditMesh Box(Vector3 size)
    {
        var mesh = new EditMesh();
        mesh.Box(size);
        return mesh;
    }

    private static MeshInfo Info(EditMesh mesh) => MeshInfo.Of(mesh);

    private static (int, int, int) Counts(EditMesh mesh) => (Info(mesh).VertexCount, Info(mesh).EdgeCount, Info(mesh).FaceCount);

    [Fact]
    public void A_Boxs_Twelve_Edges_Beveled_Round_In_Two_Segments_Are_Closed_And_Hold_Between_The_Box_And_Its_Chamfer()
    {
        var round = Box(new Vector3(2));
        round.Bevel(0.25f, 2);
        var chamfer = Box(new Vector3(2));
        chamfer.Bevel(0.25f);

        // Each face keeps a corner at each of its four, each edge a band of two faces with a vertex
        // between them at each end, and each corner of the box a face of six corners.
        Assert.Equal((48, 84, 38), Counts(round));
        Assert.True(Info(round).IsClosed);
        Assert.Equal((24, 48, 26), Counts(chamfer));
        Assert.True(Info(chamfer).IsClosed);

        Assert.True(Info(round).Volume < 8);
        Assert.True(Info(round).Volume > Info(chamfer).Volume);
        // The chamfer takes a prism of half a square of the width's side along each edge, less
        // where the edges meet, and the corners.
        Assert.True(Info(chamfer).Volume < 8 - 12 * (0.25f * 0.25f / 2) * (2 - 2 * 0.25f) + Close);
        // The bands and the corners are selected after.
        Assert.Equal(12 * 2 + 8, Info(round).SelectedFaceCount);
        // The box keeps its size, the bands within it.
        Assert.Equal(new Bounds(Vector3.Zero, new Vector3(2)), Info(round).Bounds);
    }

    [Fact]
    public void One_Edge_Beveled_Takes_A_Prism_Along_It_And_Leaves_The_Rest()
    {
        var mesh = Box(new Vector3(2));
        // The edge along X at the top of the front, from (0, 2, 0) to (2, 2, 0).
        mesh.SelectInside(new Vector3(0, 2, 0), new Vector3(2, 2, 0));
        mesh.Bevel(0.25f);

        Assert.Equal((10, 15, 7), Counts(mesh));
        Assert.True(Info(mesh).IsClosed);
        Assert.Equal(8 - 0.25f * 0.25f / 2 * 2, Info(mesh).Volume, Close);

        // Rounded in three segments, the band's end at each side of the box closed by a face beside
        // it.
        var round = Box(new Vector3(2));
        round.SelectInside(new Vector3(0, 2, 0), new Vector3(2, 2, 0));
        round.Bevel(0.25f, 3);
        Assert.Equal((14, 23, 11), Counts(round));
        Assert.True(Info(round).IsClosed);
        Assert.True(Info(round).Volume > Info(mesh).Volume);
        Assert.True(Info(round).Volume < 8);

        // The band is a quarter circle of the width's radius about the line the width in from both
        // faces, so every vertex where the edge was is that far from it.
        var band = Enumerable.Range(0, Info(round).VertexCount).Select(vertex => round.Position(vertex))
            .Where(point => point.Y > 1.75f - Close && point.Z < 0.25f + Close).ToArray();
        Assert.Equal(8, band.Count());
        Assert.All(band, point => Assert.True(MathF.Abs(new Vector2(point.Y - 1.75f, point.Z - 0.25f).Length() - 0.25f) < Close));
    }

    [Fact]
    public void A_Boxs_Top_Edges_Beveled_Leave_Its_Sides_Edges_Standing()
    {
        var mesh = Box(new Vector3(2, 3, 4));
        mesh.SelectFacing(Vector3.UnitY);
        mesh.Bevel(0.5f, 2);

        Assert.True(Info(mesh).IsClosed);
        Assert.True(Info(mesh).Volume < 24);
        Assert.True(Info(mesh).Volume > 24 - 2 * (2 + 4) * 0.5f * 0.5f);
        // The top, smaller by the width all round, and the bottom keep their places.
        Assert.Equal(new Bounds(Vector3.Zero, new Vector3(2, 3, 4)), Info(mesh).Bounds);
        var info = Info(mesh);
        Assert.Equal(2, (info.VertexCount + info.FaceCount - info.EdgeCount));
    }

    [Fact]
    public void A_Loop_Cut_Across_A_Boxs_Upright_Edge_Rings_Its_Four_Sides()
    {
        var mesh = Box(new Vector3(2, 3, 4));
        mesh.SelectInside(Vector3.Zero, new Vector3(0, 3, 0));
        mesh.LoopCut(0.25f);

        Assert.Equal((12, 20, 10), Counts(mesh));
        Assert.True(Info(mesh).IsClosed);
        Assert.Equal(24, Info(mesh).Volume, Close);

        // The loop's four vertices are a quarter up from the bottom, measured from the edge's lower
        // end, and selected with the loop's edges.
        Assert.Equal((4, 4), (Info(mesh).SelectedVertexCount, Info(mesh).SelectedEdgeCount));
        Assert.All(Enumerable.Range(0, Info(mesh).VertexCount).Where(vertex => mesh.IsVertexSelected(vertex)),
            vertex => Assert.True(MathF.Abs(mesh.Position(vertex).Y - 0.75f) < Close));
    }

    [Fact]
    public void A_Loop_Cut_Round_A_Cylinder_Splits_Each_Side_And_Leaves_Its_Ends_Whole()
    {
        var mesh = new EditMesh();
        mesh.Cylinder(1, 2, 8);
        // An upright edge, its two ends the first corner of the bottom and of the top.
        mesh.SelectInside(mesh.Position(0), mesh.Position(8));
        mesh.LoopCut(0.5f);

        Assert.Equal((24, 40, 18), Counts(mesh));
        Assert.True(Info(mesh).IsClosed);
    }

    [Fact]
    public void A_Box_Subdivided_Once_Is_Twenty_Six_Vertices_And_Twice_Fifty_Six()
    {
        var once = Box(new Vector3(2, 3, 4));
        once.SelectAll();
        once.Subdivide(1);
        Assert.Equal((26, 48, 24), Counts(once));
        Assert.True(Info(once).IsClosed);
        Assert.Equal(24, Info(once).Volume, Close);
        Assert.Equal(24, Info(once).SelectedFaceCount);

        var twice = Box(new Vector3(2, 3, 4));
        twice.SelectAll();
        twice.Subdivide(2);
        Assert.Equal((56, 108, 54), Counts(twice));
        Assert.True(Info(twice).IsClosed);
    }

    [Fact]
    public void A_Subdivided_Top_Puts_Its_New_Vertices_Into_The_Sides_Beside_It()
    {
        var mesh = Box(new Vector3(2, 3, 4));
        mesh.SelectFacing(Vector3.UnitY);
        mesh.Subdivide();

        // Four faces on the top, and each side a face of five corners with the top's new vertex.
        Assert.Equal((13, 20, 9), Counts(mesh));
        Assert.True(Info(mesh).IsClosed);
        Assert.Equal(4, Enumerable.Range(0, 9).Count(face => mesh.Corners(face).ToArray().Length == 5));
    }

    [Fact]
    public void Triangles_And_Faces_Of_More_Corners_Subdivide_Into_Triangles()
    {
        // A cone's base of four is a grid of four, and each triangle of its sides a grid of four.
        var cone = new EditMesh();
        cone.Cone(1, 2, 4);
        cone.SelectAll();
        cone.Subdivide();
        Assert.Equal((14, 32, 20), Counts(cone));
        Assert.True(Info(cone).IsClosed);

        // A cylinder's end of six corners is a fan of twelve triangles from its middle.
        var cylinder = new EditMesh();
        cylinder.Cylinder(1, 2, 6);
        cylinder.SelectFacing(Vector3.UnitY);
        cylinder.Subdivide();
        Assert.Equal((19, 36, 19), Counts(cylinder));
        Assert.True(Info(cylinder).IsClosed);
    }

    [Theory]
    [InlineData("bevel 0.25 2")]
    [InlineData("bevel 0.1 1")]
    [InlineData("loopcut 0.5")]
    [InlineData("subdivide 2")]
    public void A_Cuts_Line_Run_Is_Written_Back_As_It_Was_Typed(string line)
    {
        var mesh = Box(new Vector3(2));
        mesh.SelectAll();
        mesh.Run(line);

        Assert.Equal(line, mesh.RecipeText.Split('\n', StringSplitOptions.RemoveEmptyEntries)[^1]);
        Assert.True(Info(mesh).IsClosed);
    }

    [Fact]
    public void A_Measure_Out_Of_Range_Is_Refused()
    {
        var mesh = Box(Vector3.One);
        var refused = new Action[]
        {
            () => mesh.Bevel(0),
            () => mesh.Bevel(0.1f, 0),
            () => mesh.LoopCut(1),
            () => mesh.Subdivide(0),
        };
        foreach (var call in refused) Assert.ThrowsAny<ArgumentOutOfRangeException>(call);
        Assert.Equal((8, 12, 6), Counts(mesh));
    }
}
