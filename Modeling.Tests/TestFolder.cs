using System.Diagnostics;

namespace Modeling.Tests;

/// <summary>
/// A folder under the system's temporary folder that a test class writes its files into, made when
/// the class is and deleted when it is disposed, with the files a mesh reads below it.
/// </summary>
/// <remarks>
/// The test class holds it and disposes it, rather than a test deleting its folder in a
/// <c>finally</c>, because xUnit reports what a class's <c>Dispose</c> throws beside the test's own
/// failure, where an exception from a <c>finally</c> takes the place of the one the test threw.
/// Windows refuses to delete a file that is open, so a delete is tried again for two seconds, for a
/// handle a moment from being closed.
/// </remarks>
internal sealed class TestFolder : IDisposable
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(2);

    /// <summary>Makes a new folder, its name starting with <paramref name="prefix"/>.</summary>
    public TestFolder(string prefix)
    {
        Path = Directory.CreateTempSubdirectory(prefix).FullName;
        Files = new ModelFolder(Path);
    }

    /// <summary>The folder's full path.</summary>
    public string Path { get; }

    /// <summary>
    /// The folder's files as a mesh reads them, a relative path below the folder.
    /// </summary>
    public ModelFolder Files { get; }

    /// <summary>The full path of <paramref name="name"/> in the folder.</summary>
    public string File(string name) => System.IO.Path.Combine(Path, name);

    /// <summary>An empty mesh that reads and writes below the folder.</summary>
    public EditMesh Mesh() => new(Files);

    /// <inheritdoc />
    public void Dispose()
    {
        var waited = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
                return;
            }
            catch (Exception ex) when ((ex is IOException or UnauthorizedAccessException) && waited.Elapsed < Patience)
            {
                Thread.Sleep(50);
            }
        }
    }
}
