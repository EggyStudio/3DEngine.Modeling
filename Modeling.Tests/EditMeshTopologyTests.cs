using System.Numerics;
using Xunit;

namespace Modeling.Tests;

/// <summary>
/// The operations that change which vertices and faces an edit mesh has, extrude, inset, delete,
/// merge, flip, triangulate, duplicate and shade, on exact counts, the volume a closed mesh holds
/// and whether every edge is shared by two faces running along it in opposite directions.
/// </summary>
/// <remarks>
/// A box of two by three by four holds 24 cubic meters and its top is two by four, so pushing the
/// top up a meter adds eight and an inset of the top changes nothing it holds. A mesh left closed
/// with its faces facing out after an operation is one the build and the collider take as a solid.
/// </remarks>
[Trait("Category", "Unit")]
public sealed class EditMeshTopologyTests
{
    private const float Close = 1e-4f;


    private EditMesh Box(Vector3? size = null)
    {
        var mesh = new EditMesh();
        mesh.Box(size ?? new Vector3(2, 3, 4));
        return mesh;
    }

    private static MeshInfo Info(EditMesh mesh) => MeshInfo.Of(mesh);

    private static int FaceFacing(EditMesh mesh, Vector3 way) =>
        Enumerable.Range(0, Info(mesh).FaceCount).Single(face => (mesh.Normal(face) - way).Length() < Close);

    [Fact]
    public void The_Top_Of_A_Box_Extruded_A_Meter_Is_A_Taller_Box_Of_Twelve_Vertices_And_Ten_Faces()
    {
        var mesh = Box();
        mesh.SelectFacing(Vector3.UnitY);
        mesh.Extrude(1);

        Assert.Equal((12, 20, 10), (Info(mesh).VertexCount, Info(mesh).EdgeCount, Info(mesh).FaceCount));
        Assert.True(Info(mesh).IsClosed);
        Assert.Equal(32, Info(mesh).Volume, Close);
        Assert.Equal(new Vector3(2, 4, 4), Info(mesh).Bounds.Max);

        // The top stays selected, so a second extrude pushes it further.
        Assert.Equal((4, 1), (Info(mesh).SelectedVertexCount, Info(mesh).SelectedFaceCount));
        mesh.Extrude(1);
        Assert.Equal(40, Info(mesh).Volume, Close);

        // By a vector, the same.
        var byVector = Box();
        byVector.SelectFacing(Vector3.UnitY);
        byVector.Extrude(new Vector3(0, 1, 0));
        Assert.Equal(32, Info(byVector).Volume, Close);
    }

    [Fact]
    public void Two_Faces_Extruded_Together_Move_Each_The_Whole_Distance_Along_Its_Own_Normal()
    {
        // The top and the front of a box of two pushed out a meter as one region stand a meter out
        // each, the corner they share moved along both, and the edge between them stays joined.
        var mesh = Box(new Vector3(2));
        mesh.SelectFacing(Vector3.UnitY);
        mesh.SelectFacing(Vector3.UnitZ, SelectionMode.Add);
        mesh.Extrude(1);

        Assert.Equal(new Vector3(2, 3, 3), Info(mesh).Bounds.Max);
        Assert.True(Info(mesh).IsClosed);
        // Six corners for the region's six, and a side for each of its six rim edges.
        Assert.Equal((14, 12), (Info(mesh).VertexCount, Info(mesh).FaceCount));
        Assert.Equal(8 + 4 + 4 + 2, Info(mesh).Volume, Close);
    }

    [Fact]
    public void A_Plane_Pushed_Up_Is_A_Box_Open_Underneath()
    {
        var mesh = new EditMesh();
        mesh.Plane(2, 2);
        mesh.Extrude(1);

        Assert.Equal((8, 12, 5), (Info(mesh).VertexCount, Info(mesh).EdgeCount, Info(mesh).FaceCount));
        Assert.Equal(4, Info(mesh).OpenEdges);
        Assert.Equal(1, (Info(mesh).Bounds.Max.Y - Info(mesh).Bounds.Min.Y), Close);
    }

    [Fact]
    public void An_Inset_Top_Is_A_Smaller_Face_Inside_A_Band_And_Holds_As_Much()
    {
        var mesh = Box();
        mesh.SelectFacing(Vector3.UnitY);
        mesh.Inset(0.5f);

        Assert.Equal((12, 20, 10), (Info(mesh).VertexCount, Info(mesh).EdgeCount, Info(mesh).FaceCount));
        Assert.True(Info(mesh).IsClosed);
        Assert.Equal(24, Info(mesh).Volume, Close);

        // The smaller face is selected, a meter by three, half a meter in from each edge.
        Assert.Equal((4, 1), (Info(mesh).SelectedVertexCount, Info(mesh).SelectedFaceCount));
        var inner = Enumerable.Range(0, Info(mesh).FaceCount).Single(face => mesh.IsFaceSelected(face));
        var corners = mesh.Corners(inner).ToArray().Select(corner => mesh.Position(corner)).ToArray();
        Assert.Equal(1, (corners.Max(point => point.X) - corners.Min(point => point.X)), Close);
        Assert.Equal(3, (corners.Max(point => point.Z) - corners.Min(point => point.Z)), Close);

        // Pushed in, it is a recess a meter deep.
        mesh.Extrude(-1);
        Assert.Equal(24 - 3, Info(mesh).Volume, Close);

        var refused = () => mesh.Inset(0);
        Assert.ThrowsAny<ArgumentOutOfRangeException>(refused);
    }

    [Fact]
    public void A_Top_Deleted_Leaves_A_Hole_And_Filled_Closes_It()
    {
        var open = Box();
        open.SelectFacing(Vector3.UnitY);
        open.Delete();
        Assert.Equal((8, 12, 5), (Info(open).VertexCount, Info(open).EdgeCount, Info(open).FaceCount));
        Assert.Equal(4, Info(open).OpenEdges);
        Assert.Equal(0, Info(open).SelectedFaceCount);

        var filled = Box();
        filled.SelectFacing(Vector3.UnitY);
        filled.Delete(fill: true);
        Assert.Equal((8, 12, 6), (Info(filled).VertexCount, Info(filled).EdgeCount, Info(filled).FaceCount));
        Assert.True(Info(filled).IsClosed);
        Assert.Equal(24, Info(filled).Volume, Close);
        Assert.Equal(Vector3.UnitY, filled.Normal(Enumerable.Range(0, 6).Single(face => filled.IsFaceSelected(face))));
    }

    [Fact]
    public void A_Vertex_Deleted_Takes_The_Faces_Using_It_With_It()
    {
        var mesh = Box();
        mesh.SelectInside(new Vector3(-0.1f), new Vector3(0.1f));
        mesh.Delete();

        Assert.Equal((7, 3), (Info(mesh).VertexCount, Info(mesh).FaceCount));
        Assert.Equal(6, Info(mesh).OpenEdges);
    }

    [Fact]
    public void A_Top_Moved_Onto_The_Bottom_Merges_Into_Two_Faces_Of_Four_Corners()
    {
        var mesh = Box(Vector3.One);
        mesh.SelectFacing(Vector3.UnitY);
        mesh.Move(new Vector3(0, -1, 0));
        mesh.SelectAll();
        mesh.Merge(0.001f);

        // The four sides have two corners each left and are gone, and the top and the bottom face
        // each other on the same four vertices.
        Assert.Equal((4, 4, 2), (Info(mesh).VertexCount, Info(mesh).EdgeCount, Info(mesh).FaceCount));
        Assert.True(Info(mesh).IsClosed);
        Assert.Equal(0, Info(mesh).Volume, Close);
        Assert.Equal(4, Info(mesh).SelectedVertexCount);

        // Nothing near enough is nothing joined.
        var apart = Box(Vector3.One);
        apart.Box(Vector3.One, new Vector3(3, 0, 0));
        apart.SelectAll();
        apart.Merge(0.5f);
        Assert.Equal(16, Info(apart).VertexCount);
    }

    [Fact]
    public void Flipped_Faces_Face_In_And_Hold_A_Negative_Volume()
    {
        var mesh = Box();
        mesh.SelectAll();
        mesh.Flip();

        Assert.True(Info(mesh).IsClosed);
        Assert.Equal(-24, Info(mesh).Volume, Close);
        Assert.Equal(-Vector3.UnitY, mesh.Normal(FaceFacing(mesh, -Vector3.UnitY)));
    }

    [Fact]
    public void Triangulating_Cuts_Each_Face_Of_N_Corners_Into_N_Less_Two_And_Keeps_The_Solid()
    {
        var box = Box();
        box.SelectAll();
        box.Triangulate();
        Assert.Equal((8, 18, 12), (Info(box).VertexCount, Info(box).EdgeCount, Info(box).FaceCount));
        Assert.True(Info(box).IsClosed);
        Assert.Equal(24, Info(box).Volume, Close);
        Assert.Equal(12, Info(box).SelectedFaceCount);

        // A flight's sides are concave, ten corners each for four steps, and are cut inside their
        // outlines, so the solid holds what it did, its ten faces of four corners two triangles
        // each and its sides eight.
        var stairs = new EditMesh();
        stairs.Stairs(4, 0.25f, 0.5f, 2);
        stairs.SelectAll();
        stairs.Triangulate();
        Assert.Equal(2 * 10 + 2 * 8, Info(stairs).FaceCount);
        Assert.True(Info(stairs).IsClosed);
        Assert.Equal(2.5f, Info(stairs).Volume, Close);
        Assert.All(Enumerable.Range(0, Info(stairs).FaceCount), face => Assert.True(stairs.Corners(face).ToArray().Length == 3));
    }

    [Fact]
    public void A_Duplicate_Shares_No_Vertex_And_Moves_Alone()
    {
        var mesh = Box();
        mesh.SelectAll();
        mesh.Duplicate();
        Assert.Equal((16, 12), (Info(mesh).VertexCount, Info(mesh).FaceCount));
        Assert.Equal((8, 6), (Info(mesh).SelectedVertexCount, Info(mesh).SelectedFaceCount));

        mesh.Move(new Vector3(5, 0, 0));
        Assert.Equal(48, Info(mesh).Volume, Close);
        Assert.True(Info(mesh).IsClosed);
        Assert.Equal(new Bounds(Vector3.Zero, new Vector3(7, 3, 4)), Info(mesh).Bounds);
    }

    [Fact]
    public void Shading_Sets_The_Selected_Faces_Angle_And_Flat_Sets_None()
    {
        var mesh = new EditMesh();
        mesh.Cylinder(1, 2, 8);
        mesh.SelectAll();
        mesh.Shade(true, 45);
        var data = mesh;
        Assert.All(Enumerable.Range(0, data.FaceCount), face => Assert.True(data.SmoothOf(face) == 45));

        mesh.SelectFacing(Vector3.UnitY);
        mesh.Shade(false);
        Assert.Equal(0, data.SmoothOf(FaceFacing(mesh, Vector3.UnitY)));

        var refused = () => mesh.Shade(true, 0);
        Assert.ThrowsAny<ArgumentOutOfRangeException>(refused);
    }
}
