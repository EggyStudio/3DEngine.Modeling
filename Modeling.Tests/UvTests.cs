using System.Numerics;
using Xunit;

namespace Modeling.Tests;

/// <summary>
/// The texture coordinates of an edit mesh's faces, each projection on a box's known face, the
/// moves, scales and turns about their middle, the density of a two meter face for a texture of 512
/// pixels, the recipe making them again, and the coordinates carried to the corners an inset, a
/// subdivision and a window's cut give a face.
/// </summary>
/// <remarks>
/// A box of two by three by four has its corner at the origin, so a corner's default coordinates
/// are its place in meters along the axes its face is seen along, the top's X and Z and the front's
/// X and Y down.
/// </remarks>
[Trait("Category", "Unit")]
public sealed class UvTests
{
    private const float Close = 1e-4f;


    private EditMesh Box(Vector3? size = null, bool centered = false)
    {
        var mesh = new EditMesh();
        mesh.Box(size ?? new Vector3(2, 3, 4), centered: centered);
        return mesh;
    }

    private static int[] Facing(EditMesh mesh, Vector3 way) =>
        [.. Enumerable.Range(0, MeshInfo.Of(mesh).FaceCount).Where(face => (mesh.Normal(face) - way).Length() < Close)];

    // Each corner of a face with its coordinates.
    private static (Vector3 Position, Vector2 Uv)[] Corners(EditMesh mesh, int face) =>
        [.. mesh.Corners(face).ToArray().Zip(mesh.UvsOf(face), (vertex, uv) => (mesh.Position(vertex), uv))];

    private static void ShouldBe(Vector2 actual, Vector2 expected) => Assert.True(Vector2.Distance(actual, expected) < Close, $"{actual} is {expected}");

    private static string[] Lines(EditMesh mesh) => [.. mesh.RecipeText.TrimEnd('\n').Split('\n').Skip(1)];

    [Fact]
    public void A_Planar_Projection_Along_The_Front_Gives_The_Front_Its_Default_And_Along_The_Side_Runs_Along_Z()
    {
        var mesh = Box();
        var front = Facing(mesh, Vector3.UnitZ).Single();
        mesh.SelectFaces([front]);

        mesh.UvPlanar(Vector3.UnitZ);
        foreach (var (position, uv) in Corners(mesh, front)) ShouldBe(uv, new Vector2(position.X, -position.Y));

        // Seen from the left, the picture's right runs along Z and its down down Y.
        mesh.UvPlanar(-Vector3.UnitX);
        foreach (var (position, uv) in Corners(mesh, front)) ShouldBe(uv, new Vector2(position.Z, -position.Y));
        Assert.Equal(new[] { "uv planar +z", "uv planar -x" }, Lines(mesh)[^2..]);
    }

    [Fact]
    public void A_Box_Projection_Gives_Each_Face_The_Default_Of_Its_Own_Axis_To_Keep_As_It_Moves()
    {
        var mesh = Box();
        mesh.SelectAll();
        mesh.UvBox();
        var top = Facing(mesh, Vector3.UnitY).Single();
        foreach (var (position, uv) in Corners(mesh, top)) ShouldBe(uv, new Vector2(position.X, position.Z));

        // Moved, the top keeps its own coordinates on its corners, where the default would follow.
        mesh.SelectFaces([top]);
        mesh.Move(new Vector3(0.5f, 0, 0));
        foreach (var (position, uv) in Corners(mesh, top)) ShouldBe(uv, new Vector2(position.X - 0.5f, position.Z));

        mesh.UvReset();
        foreach (var (position, uv) in Corners(mesh, top)) ShouldBe(uv, new Vector2(position.X, position.Z));
    }

    [Fact]
    public void A_Cylindrical_Projection_Wraps_Each_Side_A_Turn_Of_The_Radius_Around_And_Its_Height_Down()
    {
        var mesh = new EditMesh();
        mesh.Cylinder(1, 2, 8);
        mesh.UvCylinder(Vector3.UnitY);

        // The middle is halfway up, so the bottom's corners are a meter down and the top's a meter
        // up.
        var sides = Enumerable.Range(0, MeshInfo.Of(mesh).FaceCount).Where(face => MathF.Abs(mesh.Normal(face).Y) < Close).ToArray();
        Assert.Equal(8, sides.Count());
        foreach (var side in sides)
        {
            var corners = Corners(mesh, side);
            Assert.Equal(MathF.Tau / 8, (corners.Max(corner => corner.Uv.X) - corners.Min(corner => corner.Uv.X)), Close);
            foreach (var (position, uv) in corners) Assert.Equal(1 - position.Y, uv.Y, Close);
        }
        Assert.Equal("uv cylinder y", Lines(mesh)[^1]);
    }

    [Fact]
    public void A_Spherical_Projection_Puts_A_Cubes_Top_Corners_On_One_Latitude()
    {
        var mesh = Box(new Vector3(2), centered: true);
        mesh.UvSphere();

        // A corner of a cube about its middle is above the equator by the angle whose tangent is
        // one over the square root of two, along a globe of the corners' distance, the square root
        // of three.
        var expected = -MathF.Atan(1 / MathF.Sqrt(2)) * MathF.Sqrt(3);
        foreach (var (_, uv) in Corners(mesh, Facing(mesh, Vector3.UnitY).Single())) Assert.Equal(expected, uv.Y, Close);
        foreach (var (_, uv) in Corners(mesh, Facing(mesh, -Vector3.UnitY).Single())) Assert.Equal(-expected, uv.Y, Close);
    }

    [Fact]
    public void A_Move_A_Scale_And_A_Turn_Change_The_Coordinates_About_Their_Middle()
    {
        var mesh = Box();
        var top = Facing(mesh, Vector3.UnitY).Single();
        mesh.SelectFaces([top]);

        // The top's default runs from 0 to 2 along X and 0 to 4 along Z, its middle at (1, 2).
        mesh.UvMove(0.5f, 0.25f);
        foreach (var (position, uv) in Corners(mesh, top)) ShouldBe(uv, new Vector2(position.X + 0.5f, position.Z + 0.25f));

        mesh.UvScale(2, 2, false);
        foreach (var (position, uv) in Corners(mesh, top)) ShouldBe(uv, new Vector2(1.5f, 2.25f) + (new Vector2(position.X, position.Z) - new Vector2(1, 2)) * 2);

        mesh.UvReset();
        mesh.UvRotate(90);
        foreach (var (position, uv) in Corners(mesh, top))
        {
            var (x, y) = (position.X - 1, position.Z - 2);
            ShouldBe(uv, new Vector2(1 - y, 2 + x));
        }
        Assert.Equal(new[] { "uv move 0.5 0.25", "uv scale 2", "uv reset", "uv rotate 90" }, Lines(mesh)[^4..]);
    }

    [Fact]
    public void Each_Face_Scaled_About_Its_Own_Middle_Keeps_Its_Middle_Where_The_Whole_Selection_Moves_Them_Apart()
    {
        var mesh = Box(new Vector3(2, 1, 1));
        mesh.SelectFacing(Vector3.UnitY);
        mesh.Subdivide();
        var halves = Facing(mesh, Vector3.UnitY);
        var before = halves.Select(face => mesh.UvsOf(face).Aggregate(Vector2.Zero, (sum, uv) => sum + uv) / 4).ToArray();

        mesh.UvScale(2, 2, each: true);
        for (int i = 0; i < halves.Length; i++)
            ShouldBe(mesh.UvsOf(halves[i]).Aggregate(Vector2.Zero, (sum, uv) => sum + uv) / 4, before[i]);
        Assert.Equal("uv scale 2 each", Lines(mesh)[^1]);
    }

    [Fact]
    public void A_Two_Meter_Face_Set_To_256_Pixels_A_Meter_Of_A_512_Pixel_Texture_Spans_One_Texture()
    {
        var mesh = Box(new Vector3(2));
        var top = Facing(mesh, Vector3.UnitY).Single();
        mesh.SelectFaces([top]);

        mesh.UvDensity(256, 512);

        // Two meters at 256 pixels a meter are 512 pixels, the texture's width once.
        var uvs = mesh.UvsOf(top);
        Assert.Equal(1, (uvs.Max(uv => uv.X) - uvs.Min(uv => uv.X)), Close);
        Assert.Equal(1, (uvs.Max(uv => uv.Y) - uvs.Min(uv => uv.Y)), Close);
        Assert.Equal("uv density 256 512", Lines(mesh)[^1]);

        var nothing = () => mesh.UvDensity(0, 512);
        Assert.ThrowsAny<ArgumentOutOfRangeException>(nothing);
    }

    [Fact]
    public void The_Recipe_Makes_The_Coordinates_Again_And_An_Undo_Takes_Them_Back()
    {
        var mesh = Box();
        mesh.SelectFacing(Vector3.UnitY);
        mesh.UvPlanar(new Vector3(1, 1, 0));
        mesh.UvRotate(30, each: true);
        mesh.UvCylinder(Vector3.UnitX, new Vector3(0, 1, 2));

        var again = EditMesh.FromRecipe(mesh.RecipeText);
        for (int face = 0; face < MeshInfo.Of(mesh).FaceCount; face++)
            Assert.Equal(mesh.UvsOf(face), again.UvsOf(face));
        Assert.Equal(new[] { "uv planar 1 1 0", "uv rotate 30 each", "uv cylinder x at 0 1 2" }, Lines(mesh)[^3..]);

        var top = Facing(mesh, Vector3.UnitY).Single();
        Assert.True(mesh.Undo());
        Assert.True(mesh.Undo());
        Assert.True(mesh.Undo());
        foreach (var (position, uv) in Corners(mesh, top)) ShouldBe(uv, new Vector2(position.X, position.Z));
    }

    [Fact]
    public void An_Inset_And_A_Subdivision_Carry_A_Faces_Own_Coordinates_To_The_Corners_They_Make()
    {
        var mesh = Box();
        var top = Facing(mesh, Vector3.UnitY).Single();
        mesh.SelectFaces([top]);
        mesh.UvPlanar(Vector3.UnitY);
        mesh.UvRotate(90);
        mesh.UvMove(3, 0);

        // The coordinates are a turned projection, so each new corner's are the turn of its place.
        static Vector2 Turned(Vector3 position) => new(3 + 1 - (position.Z - 2), 2 + (position.X - 1));
        mesh.Inset(0.25f);
        mesh.SelectFacing(Vector3.UnitY);
        mesh.Subdivide(2);

        var pieces = Facing(mesh, Vector3.UnitY);
        Assert.True(pieces.Length > 10, "the inset's ring and the inner face are each cut");
        foreach (var piece in pieces)
            foreach (var (position, uv) in Corners(mesh, piece)) ShouldBe(uv, Turned(position));
    }

    [Fact]
    public void A_Window_Cut_Through_A_Wall_Of_Its_Own_Coordinates_Leaves_The_Wall_Its_Coordinates()
    {
        var wall = Box(new Vector3(6, 3, 0.2f));
        wall.SelectFacing(Vector3.UnitZ);
        wall.UvPlanar(Vector3.UnitZ);
        wall.UvMove(0.25f, 0.5f);
        wall.SelectFacing(Vector3.UnitZ);
        wall.Window(1, 1, 3, 1);

        var front = Facing(wall, Vector3.UnitZ).Where(face => wall.Corners(face).ToArray().All(vertex => wall.Position(vertex).Z > 0.19f)).ToArray();
        Assert.NotEmpty(front);
        foreach (var face in front)
            foreach (var (position, uv) in Corners(wall, face)) ShouldBe(uv, new Vector2(position.X + 0.25f, -position.Y + 0.5f));
    }

    [Fact]
    public void A_Flipped_Face_Keeps_Each_Corners_Coordinates()
    {
        var mesh = Box();
        var top = Facing(mesh, Vector3.UnitY).Single();
        mesh.SelectFaces([top]);
        mesh.UvMove(0.5f, 0);
        var before = Corners(mesh, top).ToDictionary(corner => corner.Position, corner => corner.Uv);

        mesh.Flip();
        foreach (var (position, uv) in Corners(mesh, top)) ShouldBe(uv, before[position]);
    }
}
