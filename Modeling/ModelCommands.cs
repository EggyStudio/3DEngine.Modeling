using System.Globalization;
using System.Numerics;

namespace Modeling;

/// <summary>
/// One model command, its name, what it does, the words it takes and what runs it.
/// </summary>
/// <param name="Name">The command's name, as <c>model.box</c>, the line's first word.</param>
/// <param name="Summary">What the command does, a sentence a console's help shows.</param>
/// <param name="Usage">
/// The words the command takes, its name first, in the grammar <see cref="ModelCommands"/> gives.
/// </param>
/// <param name="Run">Runs the command on the words of a line after its name.</param>
public sealed record ModelCommand(string Name, string Summary, string Usage, Func<string, CommandAnswer> Run);

/// <summary>
/// The model commands, one for each operation of an edit mesh's recipe and those that make, name,
/// read, write, show and forget a model, run on the models of a <see cref="ModelStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// A line is the command's name and then its words, split at spaces and tabs, with no quoting,
/// since every name, path and number a command takes is one word; a number is read in the invariant
/// culture, a point before its fraction. An operation's command takes the model's name first and
/// then the operation's words as the recipe has them, so <c>model.box room 8 3 6</c> runs
/// <c>box 8 3 6</c> on the model called room, and answers with the model's counts after it, so a
/// script reads what it did. A primitive, a join, an import, a tube or a union makes the model
/// where there is none of the name, and any other operation on a name no model has is refused, so
/// a mistyped name makes no model.
/// </para>
/// <para>
/// A usage is the command's name and its words. A word stands for itself, <c>&lt;x y z&gt;</c>
/// takes a word for each name in it, <c>[...]</c> may be left out, <c>(a | b)</c> takes one of what
/// it holds, and <c>...</c> after a part takes it once or more.
/// </para>
/// </remarks>
public sealed class ModelCommands
{
    private const string Point = "<x y z>";
    private const string Placed = "[at <x y z>] [center]";
    private const string Axis = "(x | y | z | <ax ay az>)";
    private const string Solid = "<solid line>...";

    private readonly ModelStore _store;
    private readonly IModelScene _scene;
    private readonly Func<string, Vector4>? _prototypeColor;
    private readonly Dictionary<string, ModelCommand> _byName;

    /// <summary>Makes the commands over a store's models and an engine's world.</summary>
    /// <param name="store">The models the commands reach by name.</param>
    /// <param name="scene">What draws a shown model and takes it away.</param>
    /// <param name="prototypeColor">
    /// The color an export gives a slot with neither a color nor a texture, by the slot's name, as
    /// <see cref="EditMesh.Export"/> takes it, or null for white.
    /// </param>
    public ModelCommands(ModelStore store, IModelScene scene, Func<string, Vector4>? prototypeColor = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _prototypeColor = prototypeColor;
        All = Commands().ToArray();
        _byName = All.ToDictionary(command => command.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// Every command, those of the store first and then one for each operation of the recipe.
    /// </summary>
    public IReadOnlyList<ModelCommand> All { get; }

    /// <summary>
    /// Runs a line, its command's name and then its words, and answers what the command answers.
    /// </summary>
    public CommandAnswer Run(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        var (name, rest) = FirstAndRest(line);
        return _byName.TryGetValue(name, out var command)
            ? command.Run(rest)
            : Refuse(CommandFailure.NoSuchCommand, $"{name} is no model command.");
    }

    private IEnumerable<ModelCommand> Commands()
    {
        yield return new("model.new", "Makes an empty model.", "model.new <name>", New);
        yield return new("model.free", "Forgets a model and takes away what draws it.", "model.free <name>", Free);
        yield return new("model.list", "Lists the models with their counts.", "model.list", List);
        yield return new("model.info", "Says a model's counts, bounds, volume, open edges, slots and grid.", "model.info <name>", Info);
        yield return new("model.recipe", "Prints a model's recipe as its file holds it.", "model.recipe <name>", Recipe);
        yield return new("model.undo", "Takes back a model's last operation.", "model.undo <name>", Undo);
        yield return new("model.redo", "Does again a model's operation last taken back.", "model.redo <name>", Redo);
        yield return new("model.save", "Writes a model's recipe to a file.", "model.save <name> <path>", Save);
        yield return new("model.load", "Makes a model from a recipe's file, in place of one of its name.", "model.load <name> <path>", Load);
        yield return new("model.export", "Writes a model as a glTF file another program opens.", "model.export <name> <path>", Export);
        yield return new("model.show", "Draws a model where a point says, following its edits.", "model.show <name> [<x y z>]", Show);

        ModelCommand Operation(string verb, string summary, string words, bool makes = false) =>
            new($"model.{verb}", summary, $"model.{verb} <name>{(words.Length == 0 ? "" : " " + words)}", rest => RunOperation(verb, rest, makes));

        yield return Operation("box", "Adds a box.", $"{Point} {Placed}", makes: true);
        yield return Operation("plane", "Adds a plane facing up.", $"<width> <depth> {Placed}", makes: true);
        yield return Operation("cylinder", "Adds a cylinder.", $"<radius> <height> <sides> {Placed}", makes: true);
        yield return Operation("cone", "Adds a cone.", $"<radius> <height> <sides> {Placed}", makes: true);
        yield return Operation("wedge", "Adds a wedge rising along Z.", $"{Point} {Placed}", makes: true);
        yield return Operation("stairs", "Adds a flight of stairs.", $"<steps> <rise> <run> <width> {Placed}", makes: true);
        yield return Operation("arch", "Adds an arch.", $"<radius> <thickness> <depth> <sides> {Placed}", makes: true);
        yield return Operation("room", "Adds a hollow room.", $"{Point} <thickness> {Placed}", makes: true);
        yield return Operation("ramp", "Adds a ramp rising along Z.", $"<rise> <run> <width> {Placed}", makes: true);
        yield return Operation("tube", "Adds a tube along a chain of points, round about each by its radius.", "<sides> [wrap <around> <along> [radius]] (<x y z radius>)...", makes: true);
        yield return Operation("select", "Selects faces, edges or vertices by what they are or where they lie.",
            "(all | none | invert | [(add | remove)] (face (<face>)... | edge (<a b>)... | <side> | box <x0 y0 z0 x1 y1 z1> | slot <slot>))");
        yield return Operation("move", "Moves the selection.", Point);
        yield return Operation("rotate", "Turns the selection.", $"{Axis} <degrees> [at <x y z>]");
        yield return Operation("scale", "Scales the selection.", "(<factor> | <x y z>) [at <x y z>]");
        yield return Operation("grid", "Sets the grid step.", "<step>");
        yield return Operation("snap", "Puts the selection on the grid.", "");
        yield return Operation("material", "Puts the selected faces in a slot.", "<slot>");
        yield return Operation("shade", "Draws the selected faces smoothed or flat.", "(smooth [<degrees>] | flat)");
        yield return Operation("clear", "Empties a model.", "");
        yield return Operation("extrude", "Pushes the selected faces out.", "(<distance> | <x y z>)");
        yield return Operation("inset", "Insets each selected face.", "<distance>");
        yield return Operation("delete", "Takes away the selection.", "[fill]");
        yield return Operation("merge", "Joins the selected vertices within a distance.", "<distance>");
        yield return Operation("flip", "Turns the selected faces the other way.", "");
        yield return Operation("triangulate", "Cuts the selected faces into triangles.", "");
        yield return Operation("duplicate", "Copies the selected faces in place.", "");
        yield return Operation("join", "Adds another model from its recipe's file.", "<path>", makes: true);
        yield return Operation("import", "Adds the meshes of a model file as faces.", "<path>", makes: true);
        yield return Operation("bevel", "Bevels the selected edges.", "<width> [<segments>]");
        yield return Operation("loopcut", "Cuts a ring of faces through the selected edge.", "<fraction>");
        yield return Operation("subdivide", "Cuts each selected face into smaller ones.", "[<cuts>]");
        yield return Operation("bridge", "Joins two selected loops of edges by a band of faces.", "");
        yield return Operation("fill", "Closes the selected loop of open edges with a face.", "");
        yield return Operation("dissolve", "Joins the selected faces into one, or takes away the selected edges.", "[edges]");
        yield return Operation("knife", "Cuts the selected faces along a plane.", $"{Axis} [at <x y z>]");
        yield return Operation("mirror", "Adds the selection mirrored across a plane.", $"{Axis} [at <x y z>]");
        yield return Operation("spin", "Sweeps the selection round an axis.", $"{Axis} <degrees> <steps> [at <x y z>]");
        yield return Operation("smooth", "Moves the selected vertices toward their neighbors.", "<factor> [<passes>]");
        yield return Operation("modifier", "Changes the modifier stack.",
            "([add] (mirror <axes> [at <x y z>] | array <count> <x y z> | array curve (<x y z>)... | bevel <width> [<segments>] [<degrees>] | subdivision <level> | solidify <thickness> | triangulate | boolean (union | subtract | intersect) (<path> [at <x y z>] | " + Solid + ")) | move <from> <to> | remove <index> | apply <index>)");
        yield return Operation("union", "Adds another solid.", $"(<path> [at <x y z>] | {Solid})", makes: true);
        yield return Operation("subtract", "Takes another solid away.", $"(<path> [at <x y z>] | {Solid})");
        yield return Operation("intersect", "Keeps what both solids hold.", $"(<path> [at <x y z>] | {Solid})");
        yield return Operation("cut", "Cuts a window, a door or a solid through the selected faces.", $"(window <width> <height> <along> <sill> | door <width> <height> <along> | {Solid})");
        yield return Operation("epsilon", "Sets how near a point is to a plane to lie in it.", "<distance>");
        yield return new("model.uv", "Shapes the selected faces' texture coordinates.",
            "model.uv <name> (planar (<side> | <x y z>) | box | cylinder " + Axis + " [at <x y z>] | sphere [at <x y z>] | move <u v> | scale <s> [<t>] [each] | rotate <degrees> [each] | density <pixels> [<width>] | reset)",
            Uv);
        yield return Operation("slot", "Changes a material slot.",
            "(add <slot> | remove <slot> | rename <slot> <new> | color <slot> <r g b> [<a>] | (texture | normal-map | occlusion-map | metallic-roughness-map | emissive-map) <slot> (<path> | none) | emissive <slot> <r g b> | (metallic | roughness) <slot> <value>)");
    }

    // -- The store's commands

    private CommandAnswer New(string rest)
    {
        if (!ModelStore.IsName(rest)) return Refuse(CommandFailure.BadArgument, "model.new takes a name of one word.");
        _store.Name(rest, _store.Make());
        return new($"{rest} is an empty model");
    }

    private CommandAnswer Free(string rest)
    {
        if (!_store.Forget(rest)) return Missing(rest);
        _scene.Forget(rest);
        return new($"forgot {rest}");
    }

    private CommandAnswer List(string rest) =>
        _store.All.Count == 0 ? new("no models") : new(string.Join("\n", _store.All.Select(named => Counts(named.Name, named.Mesh))));

    private CommandAnswer Info(string rest)
    {
        if (_store.Find(rest) is not { } mesh) return Missing(rest);
        var (low, high) = (mesh.Bounds.Min, mesh.Bounds.Max);
        var culture = CultureInfo.InvariantCulture;
        return new(string.Join("\n",
            Counts(rest, mesh),
            string.Create(culture, $"bounds ({low.X}, {low.Y}, {low.Z}) to ({high.X}, {high.Y}, {high.Z})"),
            string.Create(culture, $"volume {mesh.Volume} cubic meters, {mesh.OpenEdges} open edges, {(mesh.IsClosed ? "closed" : "open")}"),
            $"slots {string.Join(", ", mesh.Slots.Select((slot, index) => mesh.GivenAt(index) == SlotGiven.None ? slot : $"{slot} ({mesh.GivenWords(index)})"))}",
            string.Create(culture, $"grid {mesh.GridStep}, epsilon {mesh.BooleanEpsilon}, {mesh.Recipe.Count} lines of recipe")));
    }

    private CommandAnswer Recipe(string rest) =>
        _store.Find(rest) is { } mesh ? new(mesh.RecipeText.TrimEnd('\n')) : Missing(rest);

    private CommandAnswer Undo(string rest)
    {
        if (_store.Find(rest) is not { } mesh) return Missing(rest);
        return mesh.Undo() ? new(Counts(rest, mesh)) : Refuse(CommandFailure.NothingToUndo, $"{rest} has no operation to take back.");
    }

    private CommandAnswer Redo(string rest)
    {
        if (_store.Find(rest) is not { } mesh) return Missing(rest);
        return mesh.Redo() ? new(Counts(rest, mesh)) : Refuse(CommandFailure.NothingToRedo, $"{rest} has no operation taken back to do again.");
    }

    private CommandAnswer Save(string rest)
    {
        var (name, path) = FirstAndRest(rest);
        if (path.Length == 0) return Refuse(CommandFailure.BadArgument, "model.save takes a model's name and a path to write it to.");
        if (_store.Find(name) is not { } mesh) return Missing(name);
        try
        {
            return new($"wrote {name} to {mesh.Save(path)}");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return Refuse(CommandFailure.NotWritten, $"{name} was not written to {path}. {error.Message}");
        }
    }

    private CommandAnswer Load(string rest)
    {
        var (name, path) = FirstAndRest(rest);
        if (!ModelStore.IsName(name) || path.Length == 0) return Refuse(CommandFailure.BadArgument, "model.load takes a model's name of one word and the path of its file.");
        try
        {
            var mesh = EditMesh.Load(path, _store.Files);
            _store.Name(name, mesh);
            return new(Counts(name, mesh));
        }
        catch (FileNotFoundException)
        {
            return Refuse(CommandFailure.NoSuchFile, $"There is no model at {path}.");
        }
        catch (Exception error) when (error is FormatException or IOException or UnauthorizedAccessException)
        {
            return Refuse(CommandFailure.BadFile, error.Message);
        }
    }

    private CommandAnswer Export(string rest)
    {
        var (name, path) = FirstAndRest(rest);
        if (path.Length == 0) return Refuse(CommandFailure.BadArgument, "model.export takes a model's name and a path to write it to.");
        if (_store.Find(name) is not { } mesh) return Missing(name);
        try
        {
            return new($"wrote {name} to {mesh.Export(path, prototypeColor: _prototypeColor)}");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return Refuse(CommandFailure.NotWritten, $"{name} was not written to {path}. {error.Message}");
        }
    }

    private CommandAnswer Show(string rest)
    {
        var (name, words) = FirstAndRest(rest);
        var numbers = Words(words);
        var at = Vector3.Zero;
        if (numbers.Length != 0 && (numbers.Length != 3 || !Coordinate(numbers[0], out at.X) || !Coordinate(numbers[1], out at.Y) || !Coordinate(numbers[2], out at.Z)))
            return Refuse(CommandFailure.BadArgument, "model.show takes a model's name and where to draw it, three numbers, or none for the origin.");
        if (_store.Find(name) is not { } mesh) return Missing(name);
        _scene.Show(name, mesh, at);
        return new(string.Create(CultureInfo.InvariantCulture, $"showing {name} at ({at.X}, {at.Y}, {at.Z})"));
    }

    // -- The operations' commands

    // A density typed with one number takes the width of the texture of the first selected face's
    // slot, which the line run is given, so the recipe makes the same coordinates without it.
    private CommandAnswer Uv(string rest)
    {
        var (name, words) = FirstAndRest(rest);
        if (Words(words) is ["density", var density] && _store.Find(name) is { } mesh)
        {
            if (mesh.SelectedTextureWidth() is not { } width)
                return Refuse(CommandFailure.BadArgument, $"uv density on {name}, the first selected face's slot has no texture to read the width of, so the width is given after the density.");
            return RunOperation("uv", $"{name} density {density} {width.ToString(CultureInfo.InvariantCulture)}", makes: false);
        }
        return RunOperation("uv", rest, makes: false);
    }

    /// <summary>
    /// Runs an operation's line on a model, made first where the operation makes one, and answers
    /// its counts.
    /// </summary>
    private CommandAnswer RunOperation(string verb, string rest, bool makes)
    {
        var (name, words) = FirstAndRest(rest);
        if (!ModelStore.IsName(name)) return Refuse(CommandFailure.BadArgument, $"model.{verb} takes a model's name first.");

        var mesh = _store.Find(name);
        if (mesh is null && !makes) return Missing(name);
        var made = mesh is null;
        mesh ??= _store.Make();

        try
        {
            mesh.Run(words.Length == 0 ? verb : $"{verb} {words}");
        }
        catch (Exception error) when (error is FormatException or ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            return Refuse(CommandFailure.BadArgument, $"{verb} on {name}, {Lowered(error.Message)}");
        }

        // A model is named once its first line is done, so a refused line makes none.
        if (made) _store.Name(name, mesh);
        return new(Counts(name, mesh));
    }

    /// <summary>
    /// What a model holds and what of it is selected, the answer of every operation.
    /// </summary>
    private static string Counts(string name, EditMesh mesh) =>
        $"{name}: {mesh.VertexCount} vertices, {mesh.EdgeCount} edges, {mesh.FaceCount} faces, "
        + $"{mesh.SelectedFaceCount} faces and {mesh.SelectedVertexCount} vertices selected";

    private static (string First, string Words) FirstAndRest(string line)
    {
        var trimmed = line.Trim();
        var space = trimmed.IndexOfAny([' ', '\t']);
        return space < 0 ? (trimmed, "") : (trimmed[..space], string.Join(' ', Words(trimmed[(space + 1)..])));
    }

    private static string[] Words(string text) => text.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);

    private static bool Coordinate(string word, out float value) =>
        float.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && float.IsFinite(value);

    private static string Lowered(string message) => message.Length > 0 ? char.ToLowerInvariant(message[0]) + message[1..] : message;

    private static CommandAnswer Missing(string name) =>
        Refuse(CommandFailure.NoSuchModel, $"There is no model called {name}. model.list says which there are.");

    private static CommandAnswer Refuse(CommandFailure failure, string message) => new(message, failure);
}
