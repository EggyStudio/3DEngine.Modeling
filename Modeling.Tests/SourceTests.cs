using System.Text.RegularExpressions;
using Xunit;

namespace Modeling.Tests;

/// <summary>
/// The library's and the tests' sources, held to the width the repository's prose is wrapped at.
/// </summary>
public sealed partial class SourceTests
{
    private const int Width = 100;

    private static string Root
    {
        get
        {
            // The repository's root is the folder above the tests that holds the solution.
            var at = new DirectoryInfo(AppContext.BaseDirectory);
            while (at is not null && !File.Exists(Path.Combine(at.FullName, "Modeling.slnx")))
                at = at.Parent;
            return at?.FullName
                ?? throw new DirectoryNotFoundException("No Modeling.slnx above the tests.");
        }
    }

    // A comment line that holds an address and nothing else, which no wrapping shortens.
    [GeneratedRegex(@"^\s*///?\s*\S+://\S+\s*$")]
    private static partial Regex AddressAlone();

    [Fact]
    public void Every_Comment_Line_Holds_To_A_Hundred_Columns()
    {
        var root = Root;
        var over = new List<string>();
        foreach (var project in new[] { "Modeling", "Modeling.Tests" })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, project), "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, file);
                var parts = relative.Split(Path.DirectorySeparatorChar);
                if (parts.Contains("bin") || parts.Contains("obj"))
                    continue;
                var lines = File.ReadAllLines(file);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.TrimStart().StartsWith("//", StringComparison.Ordinal) && line.Length > Width && !AddressAlone().IsMatch(line))
                        over.Add($"{relative}:{i + 1} ({line.Length})");
                }
            }
        }

        Assert.True(over.Count == 0, $"{over.Count} comment lines past {Width} columns:\n" + string.Join("\n", over));
    }
}
