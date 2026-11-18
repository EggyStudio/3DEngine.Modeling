using System.Globalization;
using System.Numerics;
using Xunit;

namespace Modeling.Tests;

/// <summary>
/// The model commands run by their lines, the guide's house blocked out line by line with the
/// counts each line answers, its recipe saved and loaded again, the refusals a script reads by
/// their codes, a program's mesh reached by its name, and a model shown following its edits.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ModelCommandsTests : IDisposable
{
    private readonly TestFolder _folder = new("modeling-commands-");
    private readonly ModelStore _store;
    private readonly RecordingScene _scene = new();
    private readonly ModelCommands _commands;

    public ModelCommandsTests()
    {
        _store = new ModelStore(_folder.Files);
        _commands = new ModelCommands(_store, _scene);
    }

    public void Dispose() => _folder.Dispose();

    private string Run(string line) => _commands.Run(line).Text;

    // The guide's house, a room with a floor of its own, a window and a door in its front wall.
    private static readonly string[] House =
    [
        "model.room house 6 3 5 0.2",
        "model.material house wall",
        "model.select house box 0 0.1 0 6 0.3 5",
        "model.material house floor",
        "model.select house +z",
        "model.cut house window 1.2 1 4.5 1",
        "model.select house +z",
        "model.cut house door 1 2.1 1.5",
    ];

    [Fact]
    public void The_Guides_House_Is_Blocked_Out_Line_By_Line_Each_Answering_Its_Counts()
    {
        var answers = House.Select(Run).ToArray();

        Assert.Equal("house: 16 vertices, 24 edges, 12 faces, 12 faces and 16 vertices selected", answers[0]);
        Assert.Equal("house: 16 vertices, 24 edges, 12 faces, 1 faces and 4 vertices selected", answers[3]);
        Assert.All(answers, answer => Assert.StartsWith("house: ", answer));

        var mesh = _store.Find("house")!;
        // the openings are cut through a closed solid and leave one
        Assert.True(mesh.IsClosed);
        Assert.Equal(6 * 3 * 5 - 5.6f * 2.6f * 4.6f - (1.2f * 1 + 1 * 2.1f) * 0.2f, mesh.Volume, 1e-4f);
        Assert.Equal(new[] { "default", "wall", "floor" }, mesh.Slots);
        Assert.Equal($"house: {mesh.VertexCount} vertices, {mesh.EdgeCount} edges, {mesh.FaceCount} faces, "
            + $"{mesh.SelectedFaceCount} faces and {mesh.SelectedVertexCount} vertices selected", answers[^1]);

        Assert.Equal(new[]
        {
            answers[^1],
            "bounds (0, 0, 0) to (6, 3, 5)",
            $"volume {mesh.Volume.ToString(CultureInfo.InvariantCulture)} cubic meters, 0 open edges, closed",
            "slots default, wall, floor",
            "grid 1, epsilon 1E-05, 8 lines of recipe",
        }, Run("model.info house").Split('\n'));
        Assert.Equal(["model 1", .. House.Select(line => line["model.".Length..].Replace("house ", ""))], Run("model.recipe house").Split('\n'));
        Assert.Equal(answers[^1], Run("model.list"));
    }

    [Fact]
    public void A_Model_Saved_And_Loaded_Again_Has_The_Same_Counts_And_Recipe()
    {
        foreach (var line in House) Run(line);

        Assert.Equal($"wrote house to {_folder.File("house.model")}", Run("model.save house house.model"));
        Assert.Equal(Run("model.info house").Split('\n')[0].Replace("house:", "again:"), Run("model.load again house.model"));
        Assert.Equal(Run("model.recipe house"), Run("model.recipe again"));
        Assert.Equal(Run("model.info house").Split('\n')[1..], Run("model.info again").Split('\n')[1..]);

        // An undo after the load goes back through the file's lines, the door taken back and the
        // window kept.
        Assert.StartsWith("again: ", Run("model.undo again"));
        Assert.True(_store.Find("again")!.IsClosed);
        Run("model.redo again");
        Assert.Equal(Run("model.info house").Replace("house:", "again:"), Run("model.info again"));
    }

    [Fact]
    public void A_Script_Reads_Why_A_Line_Was_Refused_By_Its_Code()
    {
        var answer = _commands.Run("model.extrude nothing 1");
        Assert.Contains("There is no model called nothing", answer.Text);
        // an operation on a mistyped name makes no model
        Assert.Equal("NO_SUCH_MODEL", answer.Code);
        Assert.Empty(_store.All);

        answer = _commands.Run("model.box crate 1 1");
        Assert.StartsWith("box on crate, ", answer.Text);
        Assert.Equal(CommandFailure.BadArgument, answer.Failure);
        // a refused line makes no model
        Assert.Empty(_store.All);
        answer = _commands.Run("model.box crate 1 1 1");
        Assert.Equal(("crate: 8 vertices, 12 edges, 6 faces, 6 faces and 8 vertices selected", true, (string?)null), (answer.Text, answer.Done, answer.Code));

        Run("model.undo crate");
        Assert.Equal("NOTHING_TO_UNDO", _commands.Run("model.undo crate").Code);
        Assert.StartsWith("crate: 8 vertices", Run("model.redo crate"));
        Assert.Equal("NOTHING_TO_REDO", _commands.Run("model.redo crate").Code);

        Assert.Equal("NO_SUCH_FILE", _commands.Run("model.load crate missing.model").Code);
        File.WriteAllText(_folder.File("notes.model"), "box 1 1 1\n");
        answer = _commands.Run("model.load crate notes.model");
        Assert.Contains("does not start with the line model 1", answer.Text);
        Assert.Equal("BAD_FILE", answer.Code);
        // a point is three numbers
        Assert.Equal("BAD_ARGUMENT", _commands.Run("model.show crate 1 2").Code);
        Assert.Equal("NO_SUCH_COMMAND", _commands.Run("model.paint crate").Code);

        Assert.Equal("forgot crate", Run("model.free crate"));
        Assert.Equal("NO_SUCH_MODEL", _commands.Run("model.free crate").Code);
    }

    [Fact]
    public void A_Programs_Mesh_Answers_To_Its_Name_And_Stays_The_Programs()
    {
        var level = _store.Make();
        level.Box(new Vector3(8, 3, 6));
        _store.Name("level", level);

        // The version moves with what the mesh builds into and not with its selection, so a program
        // builds its model and collider again after a command changed the mesh.
        var built = level.Version;
        Run("model.select level +y");
        Assert.Equal(built, level.Version);
        Assert.Equal("level: 12 vertices, 20 edges, 10 faces, 1 faces and 4 vertices selected", Run("model.extrude level 1"));
        Assert.True(level.Version > built);
        // the commands changed the mesh the program holds
        Assert.Equal(4, level.Bounds.Max.Y);
        Assert.Same(level, _store.Find("level"));

        // A mesh the program named stays the program's when its name is forgotten.
        Run("model.free level");
        Assert.Null(_store.Find("level"));
        Assert.Equal(10, level.FaceCount);
        Assert.Throws<ArgumentException>(() => _store.Name("two words", level));
    }

    [Fact]
    public void A_Model_Shown_Is_At_Its_Point_And_Forgotten_With_Its_Name()
    {
        Run("model.box crate 1 1 1");
        Assert.Equal("showing crate at (2, 0, -3)", Run("model.show crate 2 0 -3"));
        Assert.Equal((_store.Find("crate")!, new Vector3(2, 0, -3)), _scene.Shown["crate"]);

        // The scene draws the model again as its version moves, which an edit moves.
        var drawn = _store.Find("crate")!.Version;
        Run("model.select crate +y");
        Run("model.material crate floor");
        Assert.True(_store.Find("crate")!.Version > drawn);

        // Shown again, it moves rather than another being drawn.
        Run("model.show crate");
        Assert.Equal(Vector3.Zero, Assert.Single(_scene.Shown).Value.At);

        // A name given to another mesh is the other mesh's to show.
        Assert.Equal("crate is an empty model", Run("model.new crate"));
        Assert.Equal(0, _store.Find("crate")!.FaceCount);

        Run("model.free crate");
        // what drew the model is taken away with its name
        Assert.Empty(_scene.Shown);
    }

    [Fact]
    public void A_Density_Typed_With_One_Number_Takes_The_Width_Of_Its_Slots_Texture()
    {
        Directory.CreateDirectory(_folder.File("textures"));
        var png = new byte[29];
        new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R' }.CopyTo(png, 0);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(16), 512);
        File.WriteAllBytes(_folder.File("textures/bricks.png"), png);

        Run("model.box hall 2 2 2");
        Assert.StartsWith("hall: ", Run("model.slot hall texture default textures/bricks.png"));
        Run("model.select hall +y");
        Assert.True(_commands.Run("model.uv hall density 256").Done);
        Run("model.uv hall rotate 90 each");
        Assert.Equal(new[] { "uv density 256 512", "uv rotate 90 each" }, Run("model.recipe hall").Split('\n')[^2..]);
        Assert.Equal("slots default (texture textures/bricks.png)", Run("model.info hall").Split('\n')[3]);

        Assert.Equal("BAD_ARGUMENT", _commands.Run("model.uv hall spin 3").Code);
        // the first slot stays
        Assert.Equal("BAD_ARGUMENT", _commands.Run("model.slot hall remove default").Code);
        Run("model.slot hall texture default none");
        Assert.Equal("BAD_ARGUMENT", _commands.Run("model.uv hall density 256").Code);
    }

    [Fact]
    public void An_Exported_Model_Is_Written_Through_The_Stores_Files()
    {
        Run("model.box crate 1 1 1");
        Assert.Equal($"wrote crate to {_folder.File("crate.gltf")}", Run("model.export crate crate.gltf"));
        Assert.True(File.Exists(_folder.File("crate.bin")));
        Assert.Equal("BAD_ARGUMENT", _commands.Run("model.export crate").Code);
    }
}
