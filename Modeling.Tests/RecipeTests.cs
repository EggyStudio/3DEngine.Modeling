using System.Numerics;
using Xunit;

namespace Modeling.Tests;

/// <summary>
/// An edit mesh's recipe, the lines its operations write, the mesh made again from them, undo and
/// redo by making it again, the file it is saved as and loaded from, a model joined in by its file,
/// and the errors a line that is not an operation is named by.
/// </summary>
/// <remarks>
/// Both engines read and write a recipe through the module, so a recipe written in one builds the
/// same mesh in the other.
/// </remarks>
[Trait("Category", "Unit")]
public sealed class RecipeTests : IDisposable
{
    private const float Close = 1e-4f;
    private readonly TestFolder _folder = new("modeling-recipe-");

    public void Dispose() => _folder.Dispose();

    private static string[] Lines(EditMesh mesh) => mesh.RecipeText.Split('\n', StringSplitOptions.RemoveEmptyEntries)[1..];

    private EditMesh Room()
    {
        var mesh = new EditMesh();
        mesh.Box(new Vector3(8, 3, 6));
        mesh.SelectFacing(Vector3.UnitY);
        mesh.Material("ceiling");
        mesh.Inset(1);
        mesh.Extrude(0.5f);
        mesh.SelectAll();
        mesh.Rotate(Vector3.UnitY, 90, Vector3.Zero);
        mesh.Grid(0.5f);
        mesh.Snap();
        return mesh;
    }

    [Fact]
    public void Each_Operation_Writes_Its_Line_In_The_Words_A_Recipe_Takes()
    {
        Assert.Equal(new[] { "box 8 3 6", "select +y", "material ceiling", "inset 1", "extrude 0.5", "select all", "rotate y 90 at 0 0 0", "grid 0.5", "snap" }, Lines(Room()));

        var mesh = new EditMesh();
        mesh.Stairs(4, 0.25f, 0.3f, 2, new Vector3(1, 0, -2));
        mesh.Cylinder(0.4f, 3, 8, new Vector3(0, 1.5f, 0), centered: true);
        mesh.SelectFaces([0, 2], SelectionMode.Add);
        mesh.SelectFacing(-Vector3.UnitX, SelectionMode.Remove);
        mesh.SelectInside(Vector3.Zero, Vector3.One);
        mesh.SelectSlot("default", SelectionMode.Add);
        mesh.Scale(new Vector3(1, 2, 1));
        mesh.Rotate(new Vector3(1, 0, 1), 45);
        mesh.Shade(true);
        mesh.Delete(fill: true);
        mesh.SelectAll();
        mesh.Merge(0.01f);
        mesh.Flip();
        mesh.Triangulate();
        mesh.Duplicate();
        mesh.Extrude(new Vector3(0, 1, 0));
        mesh.Clear();

        Assert.Equal(new[] { "stairs 4 0.25 0.3 2 at 1 0 -2", "cylinder 0.4 3 8 at 0 1.5 0 center", "select add face 0 2", "select remove -x", "select box 0 0 0 1 1 1", "select add slot default", "scale 1 2 1", "rotate 1 0 1 45", "shade smooth 30", "delete fill", "select all", "merge 0.01", "flip", "triangulate", "duplicate", "extrude 0 1 0", "clear" }, Lines(mesh));
    }

    [Fact]
    public void A_Recipe_Run_From_Nothing_Makes_The_Same_Mesh()
    {
        var made = Room();
        var again = EditMesh.FromRecipe(made.RecipeText);

        Assert.Equal(Lines(made), Lines(again));
        var (a, b) = (MeshInfo.Of(made), MeshInfo.Of(again));
        Assert.Equal((a.VertexCount, a.EdgeCount, a.FaceCount), (b.VertexCount, b.EdgeCount, b.FaceCount));
        Assert.Equal(made.Slots.ToArray(), again.Slots.ToArray());
        Assert.Equal(a.Volume, b.Volume, Close);
        Assert.All(Enumerable.Range(0, a.VertexCount), vertex => Assert.True(made.Position(vertex) == again.Position(vertex)));
        Assert.True(b.IsClosed);
    }

    [Fact]
    public void Undo_Makes_The_Mesh_Again_Without_The_Last_Line_And_Redo_Puts_It_Back()
    {
        var mesh = new EditMesh();
        mesh.Box(new Vector3(2, 3, 4));
        mesh.SelectFacing(Vector3.UnitY);
        mesh.Move(new Vector3(0, 1, 0));
        Assert.Equal(32, MeshInfo.Of(mesh).Volume, Close);

        Assert.True(mesh.Undo());
        Assert.Equal(24, MeshInfo.Of(mesh).Volume, Close);
        Assert.Equal(new[] { "box 2 3 4", "select +y" }, Lines(mesh));
        // The selection is the one the lines before left, the top.
        Assert.Equal(1, MeshInfo.Of(mesh).SelectedFaceCount);

        Assert.True(mesh.Redo());
        Assert.Equal(32, MeshInfo.Of(mesh).Volume, Close);
        Assert.False(mesh.Redo());

        // An operation after an undo forgets what was taken back.
        mesh.Undo();
        mesh.Move(new Vector3(0, 2, 0));
        Assert.False(mesh.Redo());
        Assert.Equal(40, MeshInfo.Of(mesh).Volume, Close);

        while (mesh.Undo()) { }
        Assert.Equal(0, MeshInfo.Of(mesh).VertexCount);
        Assert.Empty(Lines(mesh));
    }

    [Fact]
    public void A_Saved_Recipe_Loads_Back_As_The_Same_Mesh()
    {
        var made = Room();
        var path = _folder.File("models/room.model");
        made.Save(path);

        Assert.StartsWith("model 1\n", File.ReadAllText(path));
        Assert.False(File.Exists(path + ".part"));

        var loaded = EditMesh.Load(path);
        Assert.Equal(Lines(made), Lines(loaded));
        Assert.Equal((MeshInfo.Of(made).VertexCount, MeshInfo.Of(made).FaceCount), (MeshInfo.Of(loaded).VertexCount, MeshInfo.Of(loaded).FaceCount));
        Assert.True(loaded.Undo(), "a loaded model's undo goes back through its recipe");
    }

    [Fact]
    public void A_Joined_Model_Is_Added_By_Its_File_With_Its_Slots_By_Name()
    {
        var pillar = new EditMesh();
        pillar.Cylinder(0.5f, 3, 8);
        pillar.Material("stone");
        var pillarPath = _folder.File("pillar.model");
        pillar.Save(pillarPath);

        var hall = new EditMesh();
        hall.Box(new Vector3(10, 3, 10));
        hall.Join(pillarPath);
        hall.Move(new Vector3(5, 0, 5));

        var info = MeshInfo.Of(hall);
        Assert.Equal((8 + 16, 6 + 10), (info.VertexCount, info.FaceCount));
        Assert.Equal(new[] { "default", "stone" }, hall.Slots.ToArray());
        Assert.Equal((16, 10), (info.SelectedVertexCount, info.SelectedFaceCount));
        Assert.Equal($"join {pillarPath}", Lines(hall)[1]);

        // A model that joins itself is refused rather than read until the stack runs out.
        var loop = _folder.File("loop.model");
        File.WriteAllText(loop, $"model 1\nbox 1 1 1\njoin {loop}\n");
        Assert.ThrowsAny<FormatException>(() => hall.Join(loop));
        Assert.ThrowsAny<FileNotFoundException>(() => hall.Join(_folder.File("none.model")));
        // a join refused leaves the mesh as it was
        Assert.Equal(6 + 10, MeshInfo.Of(hall).FaceCount);
        Assert.ThrowsAny<FormatException>(() => EditMesh.Load(loop));
    }

    [Fact]
    public void A_Read_Recipe_Skips_Comments_And_Blank_Lines_And_Says_Which_Line_Is_Wrong()
    {
        var mesh = EditMesh.FromRecipe("model 1\n# a room\n\nbox 8 3 6   # the walls\nselect +y\nextrude 1\n");
        Assert.Equal(new[] { "box 8 3 6", "select +y", "extrude 1" }, Lines(mesh));
        Assert.Equal(8 * 4 * 6, MeshInfo.Of(mesh).Volume, Close);

        string Refusal(string recipe)
        {
            var read = () => EditMesh.FromRecipe(recipe);
            return Assert.ThrowsAny<FormatException>(read).Message;
        }

        Assert.StartsWith("Line 3 of the recipe, boxx 1 1 1, boxx is not an operation", Refusal("model 1\nbox 1 1 1\nboxx 1 1 1\n"));
        Assert.Contains("needs another number", Refusal("box 1 1"));
        Assert.Contains("is not a number", Refusal("move 1 up 0"));
        Assert.Contains("words after", Refusal("flip twice"));
        Assert.Contains("the module reads model 1", Refusal("model 2\nbox 1 1 1"));
        Assert.Contains("above zero", Refusal("box 1 0 1"));
    }

    [Fact]
    public void A_File_That_Is_Not_A_Models_Recipe_Is_Refused()
    {
        var noHeader = _folder.File("plain.model");
        File.WriteAllText(noHeader, "box 1 1 1\n");
        // A recipe's file starts with model 1.
        Assert.ThrowsAny<FormatException>(() => EditMesh.Load(noHeader));
        Assert.ThrowsAny<FileNotFoundException>(() => EditMesh.Load(_folder.File("none.model")));
    }

    [Theory]
    [InlineData("rotate y 90 at 1 0 1")]
    [InlineData("rotate 0 -1 0 45")]
    [InlineData("scale 2")]
    [InlineData("scale 1 2 1 at 0 0 0")]
    [InlineData("select add face 0 1")]
    [InlineData("select remove +y")]
    [InlineData("select box 0 0 0 1 1 1")]
    [InlineData("select slot default")]
    [InlineData("extrude 1")]
    [InlineData("extrude 0 1 0")]
    [InlineData("inset 0.25")]
    [InlineData("delete fill")]
    [InlineData("shade smooth 45")]
    [InlineData("shade flat")]
    [InlineData("wedge 1 2 3 center")]
    [InlineData("arch 2 0.5 1 8 at 0 0 4")]
    public void A_Line_Run_Is_Written_Back_As_It_Was_Typed(string line)
    {
        var mesh = new EditMesh();
        mesh.Box(new Vector3(2));
        mesh.SelectAll();
        mesh.Run(line);

        Assert.Equal(line, Lines(mesh)[^1]);
    }
}
