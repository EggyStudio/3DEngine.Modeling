using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Modeling.Tests;

/// <summary>
/// Every model command's usage held to its parser, each command's examples matching its usage in
/// the grammar <see cref="ModelCommands"/> gives and done on a fresh model, and every word the
/// usage names taken by one of them, so a usage that drifts from what its command reads fails here.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CommandUsageTests : IDisposable
{
    private readonly TestFolder _folder = new("modeling-usage-");

    public void Dispose() => _folder.Dispose();

    // Each command's examples, the lines before a semicolon readying the model and the last the
    // example itself, every one run on a fresh store holding crate, a box of 2 meters selected
    // whole, with a joined model, a glTF and a texture beside it.
    private static readonly Dictionary<string, string[]> Examples = new()
    {
        ["model.new"] = ["model.new shed"],
        ["model.free"] = ["model.free crate"],
        ["model.list"] = ["model.list"],
        ["model.info"] = ["model.info crate"],
        ["model.recipe"] = ["model.recipe crate"],
        ["model.undo"] = ["model.undo crate"],
        ["model.redo"] = ["model.undo crate; model.redo crate"],
        ["model.save"] = ["model.save crate models/crate.model"],
        ["model.load"] = ["model.save crate models/crate.model; model.load again models/crate.model"],
        ["model.export"] = ["model.export crate models/crate.gltf"],
        ["model.show"] = ["model.show crate", "model.show crate 1 2 3"],
        ["model.box"] = ["model.box shed 1 2 3", "model.box shed 1 2 3 at 0 1 0 center"],
        ["model.plane"] = ["model.plane floor 4 4", "model.plane floor 4 4 at 1 0 1 center"],
        ["model.cylinder"] = ["model.cylinder post 0.5 2 8 center", "model.cylinder post 0.5 2 8 at 1 0 1"],
        ["model.cone"] = ["model.cone spire 1 2 6", "model.cone spire 1 2 6 at 0 2 0 center"],
        ["model.wedge"] = ["model.wedge ramp 1 1 2", "model.wedge ramp 1 1 2 at 1 0 0 center"],
        ["model.stairs"] = ["model.stairs flight 4 0.25 0.3 1", "model.stairs flight 4 0.25 0.3 1 at 0 0 2 center"],
        ["model.arch"] = ["model.arch gate 1 0.25 0.5 8", "model.arch gate 1 0.25 0.5 8 at 0 0 4 center"],
        ["model.room"] = ["model.room hall 6 3 5 0.2", "model.room hall 6 3 5 0.2 at 0 0 0 center"],
        ["model.ramp"] = ["model.ramp slope 1 3 2", "model.ramp slope 1 3 2 at 2 0 0 center"],
        ["model.tube"] = ["model.tube pipe 6 0 0 0 0.2 0 2 0 0.2", "model.tube pipe 6 wrap 2 0.5 0 0 0 0.2 0 2 0 0.2", "model.tube pipe 6 wrap 2 0.5 radius 0 0 0 0.2 0 2 0 0.1"],
        ["model.select"] =
        [
            "model.select crate all", "model.select crate none", "model.select crate invert", "model.select crate face 0 1",
            "model.select crate add face 2", "model.select crate remove +y", "model.select crate +y", "model.select crate edge 0 1",
            "model.select crate box 0 0 0 2 2 2", "model.select crate slot default",
        ],
        ["model.move"] = ["model.move crate 0 1 0"],
        ["model.rotate"] = ["model.rotate crate y 90", "model.rotate crate x 90", "model.rotate crate z 90", "model.rotate crate 0 1 0 45 at 1 1 1"],
        ["model.scale"] = ["model.scale crate 2", "model.scale crate 1 2 1 at 0 0 0"],
        ["model.grid"] = ["model.grid crate 0.25"],
        ["model.snap"] = ["model.snap crate"],
        ["model.material"] = ["model.material crate stone"],
        ["model.shade"] = ["model.shade crate smooth", "model.shade crate smooth 45", "model.shade crate flat"],
        ["model.clear"] = ["model.clear crate"],
        ["model.extrude"] = ["model.select crate +y; model.extrude crate 1", "model.select crate +y; model.extrude crate 0 1 0"],
        ["model.inset"] = ["model.select crate +y; model.inset crate 0.25"],
        ["model.delete"] = ["model.select crate +y; model.delete crate", "model.select crate +y; model.delete crate fill"],
        ["model.merge"] = ["model.merge crate 0.01"],
        ["model.flip"] = ["model.flip crate"],
        ["model.triangulate"] = ["model.triangulate crate"],
        ["model.duplicate"] = ["model.duplicate crate"],
        ["model.join"] = ["model.join crate models/pillar.model"],
        ["model.import"] = ["model.import crate models/lid.gltf"],
        ["model.bevel"] = ["model.select crate edge 0 1; model.bevel crate 0.1", "model.select crate edge 0 1; model.bevel crate 0.1 2"],
        ["model.loopcut"] = ["model.select crate edge 0 1; model.loopcut crate 0.5"],
        ["model.subdivide"] = ["model.select crate +y; model.subdivide crate", "model.select crate +y; model.subdivide crate 2"],
        ["model.bridge"] = ["model.box crate 1 1 1 at 3 0 0; model.select crate box 2 0 0 2 2 2; model.select crate add box 3 0 0 3 1 1; model.bridge crate"],
        ["model.fill"] = ["model.select crate +y; model.delete crate; model.select crate all; model.fill crate"],
        ["model.dissolve"] =
        [
            "model.select crate +y; model.subdivide crate; model.dissolve crate",
            "model.select crate edge 0 1; model.loopcut crate 0.5; model.dissolve crate edges",
        ],
        ["model.knife"] = ["model.knife crate y at 0 1 0", "model.knife crate x at 1 0 0", "model.knife crate z at 0 0 1", "model.knife crate 1 1 0"],
        ["model.mirror"] = ["model.mirror crate x", "model.mirror crate y", "model.mirror crate z", "model.mirror crate 0 0 1 at 0 0 3"],
        ["model.spin"] = ["model.select crate +y; model.spin crate y 90 4 at 5 0 0", "model.select crate +y; model.spin crate x 90 4 at 0 5 0", "model.select crate +y; model.spin crate z 90 4 at 5 0 0", "model.select crate +y; model.spin crate 0 1 0 90 4 at 5 0 0"],
        ["model.smooth"] = ["model.smooth crate 0.5", "model.smooth crate 0.5 2"],
        ["model.modifier"] =
        [
            "model.modifier crate mirror x", "model.modifier crate mirror xz at 1 0 0", "model.modifier crate array 3 2 0 0",
            "model.modifier crate array curve 0 0 0 5 0 0", "model.modifier crate bevel 0.1", "model.modifier crate bevel 0.1 2 30",
            "model.modifier crate subdivision 1", "model.modifier crate solidify 0.1", "model.modifier crate triangulate",
            "model.modifier crate boolean subtract box 1 1 1 at 0.5 0.5 0.5", "model.modifier crate boolean union models/pillar.model at 1 0 0",
            "model.modifier crate boolean intersect box 3 3 3 at -0.5 -0.5 -0.5", "model.modifier crate add triangulate",
            "model.modifier crate triangulate; model.modifier crate solidify 0.1; model.modifier crate move 1 0",
            "model.modifier crate triangulate; model.modifier crate remove 0", "model.modifier crate triangulate; model.modifier crate apply 0",
        ],
        ["model.union"] = ["model.union crate box 1 1 1 at 2 0 0", "model.union crate models/pillar.model at 3 0 0"],
        ["model.subtract"] = ["model.subtract crate box 0.5 0.5 0.5", "model.subtract crate models/pillar.model at 1 0 1"],
        ["model.intersect"] = ["model.intersect crate box 3 3 3 at -0.5 -0.5 -0.5"],
        ["model.cut"] =
        [
            "model.select crate +z; model.cut crate window 0.5 0.5 1 0.5", "model.select crate +z; model.cut crate door 0.5 1 1",
            "model.cut crate box 0.5 0.5 3 at 0.5 0.5 -0.5",
        ],
        ["model.epsilon"] = ["model.epsilon crate 0.0001"],
        ["model.uv"] =
        [
            "model.uv crate planar +y", "model.uv crate planar 0 1 0", "model.uv crate box", "model.uv crate cylinder y", "model.uv crate cylinder x", "model.uv crate cylinder z",
            "model.uv crate cylinder 0 1 0 at 1 1 1", "model.uv crate sphere", "model.uv crate sphere at 1 1 1", "model.uv crate move 0.5 0.25",
            "model.uv crate scale 2", "model.uv crate scale 2 1 each", "model.uv crate rotate 30", "model.uv crate rotate 30 each",
            "model.uv crate density 256 512", "model.slot crate texture default textures/bricks.png; model.uv crate density 256",
            "model.uv crate reset",
        ],
        ["model.slot"] =
        [
            "model.slot crate add trim", "model.slot crate add trim; model.slot crate remove trim", "model.slot crate rename default stone",
            "model.slot crate color default 0.5 0.5 0.5", "model.slot crate color default 0.5 0.5 0.5 0.8",
            "model.slot crate texture default textures/bricks.png", "model.slot crate normal-map default none",
            "model.slot crate occlusion-map default textures/bricks.png", "model.slot crate metallic-roughness-map default none",
            "model.slot crate emissive-map default none", "model.slot crate emissive default 1 1 1", "model.slot crate metallic default 0.5",
            "model.slot crate roughness default 0.5",
        ],
    };

    private ModelCommands Fresh()
    {
        var store = new ModelStore(_folder.Files);
        var commands = new ModelCommands(store, new RecordingScene());
        Assert.True(commands.Run("model.box crate 2 2 2").Done);
        return commands;
    }

    private void Fixtures()
    {
        Directory.CreateDirectory(_folder.File("models"));
        File.WriteAllText(_folder.File("models/pillar.model"), "model 1\ncylinder 0.25 2 8\n");
        var lid = _folder.Mesh();
        lid.Box(new Vector3(1, 0.1f, 1));
        lid.Export("models/lid.gltf");
        Directory.CreateDirectory(_folder.File("textures"));
        var png = new byte[29];
        new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R' }.CopyTo(png, 0);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(16), 512);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(20), 512);
        File.WriteAllBytes(_folder.File("textures/bricks.png"), png);
    }

    [Fact]
    public void Every_Commands_Examples_Match_Its_Usage_And_Are_Done()
    {
        Fixtures();
        var all = Fresh().All;
        var faults = new List<string>();
        foreach (var command in all)
        {
            if (!Examples.TryGetValue(command.Name, out var examples))
            {
                faults.Add($"{command.Name} has no example");
                continue;
            }
            var usage = Usage(command.Usage);
            foreach (var example in examples)
            {
                var lines = example.Split("; ");
                var commands = Fresh();
                foreach (var ready in lines[..^1])
                    if (commands.Run(ready) is { Done: false } readied) faults.Add($"{command.Name}, {ready} readying {lines[^1]}, {readied.Text}");
                if (!usage.IsMatch(lines[^1])) faults.Add($"{lines[^1]} is not what {command.Usage} reads");
                if (commands.Run(lines[^1]) is { Done: false } answer) faults.Add($"{lines[^1]} was refused, {answer.Text}");
            }

            // Every word the usage names stands in one of its examples, so the usage names nothing
            // the command does not read.
            var taken = examples.SelectMany(example => example.Split("; ")[^1].Split(' ')).ToHashSet();
            foreach (var word in Literals(command.Usage).Where(word => !taken.Contains(word)))
                faults.Add($"{command.Name}'s usage names {word}, which no example takes");
        }
        foreach (var name in Examples.Keys.Except(all.Select(command => command.Name))) faults.Add($"{name} has examples and no command");

        Assert.True(faults.Count == 0, string.Join("\n", faults));
    }

    [Fact]
    public void A_Line_That_Is_Not_What_Its_Usage_Reads_Is_Refused_By_The_Command_Too()
    {
        Fixtures();
        string[] wrong = ["model.box crate 1 1", "model.rotate crate y", "model.shade crate round", "model.slot crate paint default", "model.uv crate spin 3", "model.show crate 1 2"];
        foreach (var line in wrong)
        {
            var commands = Fresh();
            var command = commands.All.Single(candidate => candidate.Name == line.Split(' ')[0]);
            Assert.False(Usage(command.Usage).IsMatch(line), $"{line} matches {command.Usage}");
            Assert.Equal(CommandFailure.BadArgument, commands.Run(line).Failure);
        }
    }

    // -- The usage grammar, a word for itself, <a b> a word for each name, [...] left out or not,
    // (a | b) one of them and ... after a part once or more.

    private static Regex Usage(string usage)
    {
        var tokens = Regex.Matches(usage, @"<[^>]*>|\.\.\.|[()\[\]|]|[^\s()\[\]|<>]+").Select(match => match.Value).ToList();
        var at = 1;
        var pattern = Regex.Escape(tokens[0]) + Alternatives(tokens, ref at);
        Assert.True(at == tokens.Count, $"{usage} has a bracket closed that was not opened");
        return new Regex("^" + pattern + "$");
    }

    private static string Alternatives(List<string> tokens, ref int at)
    {
        var choices = new List<string> { Sequence(tokens, ref at) };
        while (at < tokens.Count && tokens[at] == "|")
        {
            at++;
            choices.Add(Sequence(tokens, ref at));
        }
        return choices.Count == 1 ? choices[0] : "(?:" + string.Join("|", choices) + ")";
    }

    private static string Sequence(List<string> tokens, ref int at)
    {
        var text = new StringBuilder();
        while (at < tokens.Count && tokens[at] is not ("|" or ")" or "]"))
        {
            var token = tokens[at++];
            string part;
            if (token is "(" or "[")
            {
                var inner = Alternatives(tokens, ref at);
                Assert.Equal(token == "(" ? ")" : "]", tokens[at++]);
                part = token == "(" ? "(?:" + inner + ")" : "(?:" + inner + ")?";
            }
            else if (token.StartsWith('<'))
            {
                part = string.Concat(Enumerable.Repeat(@" \S+", token.Trim('<', '>').Split(' ').Length));
            }
            else
            {
                part = " " + Regex.Escape(token);
            }
            if (at < tokens.Count && tokens[at] == "...")
            {
                at++;
                part = "(?:" + part + ")+";
            }
            text.Append(part);
        }
        return text.ToString();
    }

    private static IEnumerable<string> Literals(string usage) =>
        Regex.Matches(usage, @"<[^>]*>|\.\.\.|[()\[\]|]|[^\s()\[\]|<>]+").Select(match => match.Value).Skip(1)
            .Where(token => token is not ("(" or ")" or "[" or "]" or "|" or "...") && !token.StartsWith('<'));
}

/// <summary>A scene that keeps what it was told, for the commands' tests.</summary>
internal sealed class RecordingScene : IModelScene
{
    /// <summary>Where each shown model stands, by its name.</summary>
    public Dictionary<string, (EditMesh Mesh, Vector3 At)> Shown { get; } = [];

    /// <inheritdoc/>
    public void Show(string name, EditMesh mesh, Vector3 at) => Shown[name] = (mesh, at);

    /// <inheritdoc/>
    public void Forget(string name) => Shown.Remove(name);
}
