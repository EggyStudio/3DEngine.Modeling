using Xunit;

namespace Modeling.Tests;

/// <summary>
/// The guard of the rule that a join's depth is counted on each thread for itself, two threads
/// held at a barrier eight deep each joining on to ten, which a count both shared would refuse at
/// sixteen, and a model reading itself still refused at sixteen on one.
/// </summary>
[Trait("Category", "Unit")]
public sealed class JoinDepthTests
{
    // Ten recipes each joining the next, the last a box, held at a barrier as the eighth is read,
    // where each load is eight deep and a count both shared would stand at sixteen.
    private sealed class DeepFiles(Barrier barrier) : IModelFiles
    {
        public string ReadText(string path)
        {
            var level = int.Parse(path["deep".Length..^".model".Length], System.Globalization.CultureInfo.InvariantCulture);
            if (level == 7 && !barrier.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException("The other load did not reach the barrier.");
            return level == 9 ? "model 1\nbox 1 1 1\n" : $"model 1\njoin deep{level + 1}.model\n";
        }

        public string WriteText(string path, string text) => throw new NotSupportedException();

        public string WriteBytes(string path, byte[] bytes) => throw new NotSupportedException();

        public IReadOnlyList<ModelPart> ReadModel(string path) => throw new NotSupportedException();

        public int? ImageWidth(string path) => null;
    }

    [Fact]
    public async Task Two_Threads_Loading_Models_That_Join_Deep_Each_Count_Their_Own_Depth()
    {
        using var barrier = new Barrier(2);
        var files = new DeepFiles(barrier);
        var loads = Enumerable.Range(0, 2).Select(_ => Task.Run(() => EditMesh.FromRecipe("join deep0.model", files: files))).ToArray();

        // Each is ten deep, under the sixteen a model reading itself reaches.
        foreach (var mesh in await Task.WhenAll(loads)) Assert.Equal(8, mesh.VertexCount);
    }

    [Fact]
    public void A_Model_Reading_Itself_Is_Still_Refused_At_Sixteen_Deep()
    {
        var files = new HostFiles();
        files.PutText("loop.model", "model 1\nbox 1 1 1\njoin loop.model\n");
        var refused = Assert.Throws<FormatException>(() => EditMesh.Load("loop.model", files));
        Assert.Contains("sixteen deep", refused.ToString());
    }
}
