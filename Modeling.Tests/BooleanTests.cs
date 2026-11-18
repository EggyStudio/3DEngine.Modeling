using System.Numerics;
using Xunit;

namespace Modeling.Tests;

/// <summary>
/// The edit mesh's booleans, a box cut through a wall, two boxes joined, two kept where they
/// overlap, a flight of stairs joined by its primitive's line, and the room, the window, the door
/// and the ramp made with them, each closed with every edge shared by two faces, holding the volume
/// the operation leaves, and joined back into few faces.
/// </summary>
/// <remarks>
/// The partition leaves convex pieces, which are joined back where they lie in one plane and one
/// slot, so a cut through a wall leaves the wall's two faces in a few pieces round the hole, the
/// hole's four sides, and the wall's other faces as they were.
/// </remarks>
[Trait("Category", "Unit")]
public sealed class BooleanTests : IDisposable
{
    private const float Close = 1e-3f;
    private readonly TestFolder _folder = new("modeling-boolean-");

    public void Dispose() => _folder.Dispose();

    private EditMesh New()
    {
        var mesh = new EditMesh();
        return mesh;
    }

    private EditMesh Box(Vector3 size, Vector3 at = default)
    {
        var mesh = New();
        mesh.Box(size, at);
        return mesh;
    }

    private static MeshInfo Info(EditMesh mesh) => MeshInfo.Of(mesh);

    private static string[] Lines(EditMesh mesh) => mesh.RecipeText.Split('\n', StringSplitOptions.RemoveEmptyEntries)[1..];

    private static Vector3 Center(EditMesh mesh, int face) =>
        mesh.Corners(face).ToArray().Aggregate(Vector3.Zero, (sum, corner) => sum + mesh.Position(corner)) / mesh.Corners(face).ToArray().Length;

    private string Saved(EditMesh mesh, string name)
    {
        var path = _folder.File(name);
        mesh.Save(path);
        return path;
    }

    [Fact]
    public void A_Box_Cut_Through_A_Wall_Leaves_It_Closed_And_Holding_The_Wall_Less_The_Hole()
    {
        var wall = Box(new Vector3(4, 3, 0.5f));
        wall.Cut("box 1 1 1 at 1.5 1 -0.25");

        Assert.True(Info(wall).IsClosed);
        Assert.Equal(6 - 0.5f, Info(wall).Volume, Close);

        // The hole's four sides are selected. The wall's front and back are two faces each round
        // the hole, which one loop cannot go round, and its top, bottom and ends one each, twelve
        // faces in all, with a hole through, so vertices less edges plus faces is none.
        Assert.Equal(4, Info(wall).SelectedFaceCount);
        Assert.Equal((20, 32, 12), (Info(wall).VertexCount, Info(wall).EdgeCount, Info(wall).FaceCount));
        Assert.Equal(new[] { "box 4 3 0.5", "cut box 1 1 1 at 1.5 1 -0.25" }, Lines(wall));
    }

    [Fact]
    public void Two_Boxes_Sharing_A_Face_Unite_Into_One_Box()
    {
        var other = Saved(Box(Vector3.One), "cube.model");
        var joined = Box(Vector3.One);
        joined.Union(other, new Vector3(1, 0, 0));

        Assert.Equal((8, 12, 6), (Info(joined).VertexCount, Info(joined).EdgeCount, Info(joined).FaceCount));
        Assert.True(Info(joined).IsClosed);
        Assert.Equal(2, Info(joined).Volume, Close);
        Assert.Equal(new Bounds(Vector3.Zero, new Vector3(2, 1, 1)), Info(joined).Bounds);
        Assert.Equal(new[] { "box 1 1 1", $"union {other} at 1 0 0" }, Lines(joined));
    }

    [Fact]
    public void Two_Overlapping_Boxes_Intersect_In_Their_Overlap_And_Subtract_To_The_Rest()
    {
        var other = Saved(Box(new Vector3(2)), "block.model");

        var both = Box(new Vector3(2));
        both.Intersect(other, Vector3.One);
        Assert.Equal((8, 6), (Info(both).VertexCount, Info(both).FaceCount));
        Assert.True(Info(both).IsClosed);
        Assert.Equal(1, Info(both).Volume, Close);

        var rest = Box(new Vector3(2));
        rest.Subtract(other, Vector3.One);
        Assert.True(Info(rest).IsClosed);
        Assert.Equal(7, Info(rest).Volume, Close);
    }

    [Fact]
    public void A_Room_Is_Two_Closed_Shells_Holding_Its_Walls()
    {
        var room = New();
        room.Room(new Vector3(6, 3, 5), 0.2f);

        Assert.Equal((16, 24, 12), (Info(room).VertexCount, Info(room).EdgeCount, Info(room).FaceCount));
        Assert.True(Info(room).IsClosed);
        Assert.Equal(6 * 3 * 5 - 5.6f * 2.6f * 4.6f, Info(room).Volume, Close);
    }

    [Fact]
    public void A_Window_Goes_Through_The_Wall_Selected_And_Not_The_Wall_Across()
    {
        var room = New();
        room.Room(new Vector3(6, 3, 5), 0.2f);
        var before = Info(room).Volume;

        // The front wall's outside, the first face facing out along +Z, the inside of the back wall
        // facing that way too, after it.
        room.SelectFacing(Vector3.UnitZ);
        room.Window(1, 1, 3, 1);

        Assert.True(Info(room).IsClosed);
        Assert.Equal(before - 1 * 1 * 0.2f, Info(room).Volume, Close);
        Assert.Equal(4, Info(room).SelectedFaceCount);
        foreach (var face in Enumerable.Range(0, Info(room).FaceCount).Where(face => room.IsFaceSelected(face)))
        {
            Assert.InRange(Center(room, face).Z, 4.8f - Close, 5 + Close);
            Assert.InRange(Center(room, face).X, 2.5f - Close, 3.5f + Close);
        }

        room.SelectFacing(Vector3.UnitY);
        var floor = () => room.Window(1, 1, 1, 1);
        Assert.ThrowsAny<InvalidOperationException>(floor);
        room.SelectNone();
        var none = () => room.Window(1, 1, 1, 1);
        Assert.ThrowsAny<InvalidOperationException>(none);
    }

    [Fact]
    public void A_Door_Stands_On_The_Walls_Bottom_And_A_Second_Opening_Is_Measured_On_The_Whole_Wall()
    {
        var wall = Box(new Vector3(6, 3, 0.2f));
        wall.SelectFacing(Vector3.UnitZ);
        wall.Window(1, 1, 4, 1);

        // Every piece the window left of the front, so the door is measured from the wall's left.
        wall.SelectInside(new Vector3(-1, -1, 0.199f), new Vector3(7, 4, 0.201f));
        wall.Door(1, 2.1f, 1.5f);

        Assert.True(Info(wall).IsClosed);
        Assert.Equal(6 * 3 * 0.2f - 1 * 0.2f - 1 * 2.1f * 0.2f, Info(wall).Volume, Close);

        // The door's sides from its foot at the wall's bottom up to its head, between one and two
        // along X.
        var sides = Enumerable.Range(0, Info(wall).FaceCount).Where(face => wall.IsFaceSelected(face)).Select(face => Center(wall, face)).ToArray();
        Assert.Equal(3, sides.Length);
        Assert.All(sides, center => Assert.True(center.X >= 1 - Close && center.X <= 2 + Close));
        Assert.Equal(new[] { "box 6 3 0.2", "select +z", "cut window 1 1 4 1", "select box -1 -1 0.199 7 4 0.201", "cut door 1 2.1 1.5" }, Lines(wall));
    }

    [Fact]
    public void A_Door_Cut_Down_Through_A_Rooms_Slab_Leaves_The_Floor_Behind_The_Wall_Whole()
    {
        var room = New();
        room.Room(new Vector3(6, 3, 5), 0.2f);
        var before = Info(room).Volume;
        room.SelectFacing(Vector3.UnitZ);
        room.Door(1, 2.1f, 1.5f);

        // The door goes through the wall from the ground, a step up to the floor inside, and the
        // cutter's centimeter past the wall stops above the floor, so no slit goes down through the
        // slab along the threshold.
        Assert.True(Info(room).IsClosed);
        Assert.Equal(before - 1 * 2.1f * 0.2f, Info(room).Volume, Close);
        var floor = Enumerable.Range(0, Info(room).FaceCount)
            .Where(face => room.Normal(face) == Vector3.UnitY && MathF.Abs(Center(room, face).Y - 0.2f) < Close).ToArray();
        // the floor inside is one face, uncut
        Assert.Single(floor);
        // its edge along the front wall takes a corner where each of the door's sides meets it
        Assert.Equal(6, room.Corners(floor[0]).ToArray().Count());
    }

    [Fact]
    public void A_Flight_Joined_By_Its_Line_Is_One_Solid_With_The_Floor_And_The_Wall_It_Stands_On()
    {
        // A hall's floor and wall the flight stands on and against.
        EditMesh Hall()
        {
            var mesh = Box(new Vector3(6, 3, 6));
            mesh.Cut("box 5.6 2.6 5.6 at 0.2 0.2 0.2");
            return mesh;
        }
        var shell = 6 * 3 * 6 - 5.6f * 2.6f * 5.6f;

        var hall = Hall();
        hall.Union("stairs 8 0.2 0.3 1 at 0.2 0.2 1", default);
        Assert.True(Info(hall).IsClosed);
        Assert.Equal("union stairs 8 0.2 0.3 1 at 0.2 0.2 1", Lines(hall)[^1]);
        Assert.Equal(shell + 0.3f * 1 * 0.2f * 36, Info(hall).Volume, Close);

        // A window cut after it leaves every edge shared by two faces, where a flight added as a
        // second solid touching the floor and the wall has its edges welded to theirs by the cut,
        // four faces along each, and every one of them counted open.
        static void Window(EditMesh mesh)
        {
            mesh.SelectInside(new Vector3(5.8f, 0.2f, 0.2f), new Vector3(5.8f, 2.8f, 5.8f));
            mesh.Window(1, 1, 2.8f, 1);
        }
        Window(hall);
        Assert.Equal(0, Info(hall).OpenEdges);

        var added = Hall();
        added.Stairs(8, 0.2f, 0.3f, 1, new Vector3(0.2f, 0.2f, 1));
        Window(added);
        Assert.True(Info(added).OpenEdges > 0);
    }

    [Fact]
    public void A_Primitives_Line_In_A_Boolean_Takes_The_Meshs_First_Slot_And_Reads_Back()
    {
        var mesh = Box(new Vector3(2));
        mesh.Union("box 1 1 1 at 2 0 0", default);
        mesh.Subtract("box 0.5 0.5 0.5", default);
        mesh.Intersect("box 3 3 3 at -0.5 -0.5 -0.5", default);

        Assert.Equal(new[] { "default" }, mesh.Slots.ToArray());
        Assert.True(Info(mesh).IsClosed);
        Assert.Equal(new[] { "box 2 2 2", "union box 1 1 1 at 2 0 0", "subtract box 0.5 0.5 0.5", "intersect box 3 3 3 at -0.5 -0.5 -0.5" }, Lines(mesh));
        var again = EditMesh.FromRecipe(mesh.RecipeText);
        Assert.Equal(Lines(mesh), Lines(again));

        // A primitive's line places it with its own at, which a boolean's move of a file's model is
        // not.
        var moved = () => mesh.Union("box 1 1 1", Vector3.One);
        Assert.ThrowsAny<ArgumentException>(moved);
        // A file that is missing is refused, the mesh as it was.
        Assert.ThrowsAny<FileNotFoundException>(() => mesh.Union(_folder.File("none.model"), default));
        Assert.True(Info(mesh).IsClosed);
    }

    [Fact]
    public void A_Ramp_Is_A_Wedge_In_The_Stairs_Words_And_The_Lines_Read_Back()
    {
        var ramp = New();
        ramp.Ramp(1, 3, 2);
        Assert.Equal((6, 5), (Info(ramp).VertexCount, Info(ramp).FaceCount));
        Assert.Equal(3, Info(ramp).Volume, Close);
        Assert.Equal(new Bounds(Vector3.Zero, new Vector3(2, 1, 3)), Info(ramp).Bounds);

        var made = New();
        made.Epsilon(1e-4f);
        made.Room(new Vector3(6, 3, 5), 0.2f);
        made.SelectFacing(Vector3.UnitZ);
        made.Window(1, 1, 3, 1);
        made.Cut("cylinder 0.2 4 8 at 1 -0.5 1");
        made.Ramp(0.5f, 2, 1, new Vector3(1, 0.2f, 1));
        var again = EditMesh.FromRecipe(made.RecipeText);

        Assert.Equal(Lines(made), Lines(again));
        Assert.Equal((Info(made).VertexCount, Info(made).FaceCount), (Info(again).VertexCount, Info(again).FaceCount));
        Assert.Equal(Info(made).Volume, Info(again).Volume, Close);

        var plane = () => Box(Vector3.One).Cut("plane 1 1");
        Assert.ThrowsAny<ArgumentException>(plane);
        var solidRoom = () => New().Room(Vector3.One, 0.5f);
        Assert.ThrowsAny<ArgumentOutOfRangeException>(solidRoom);
    }
}
