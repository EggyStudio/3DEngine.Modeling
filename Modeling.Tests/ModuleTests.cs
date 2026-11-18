using System.Reflection;
using Tools;
using Xunit;

namespace Modeling.Tests;

/// <summary>
/// The checks every module keeps, from the Tools submodule, a test each, that the library takes no
/// engine, window or graphics API, names no engine in a public member or its documentation and has
/// every public type named in the README, and that the README's binding block is the host's code.
/// </summary>
public sealed class ModuleTests
{
    // The library's assembly, the checkout's root and the source of the host that runs the
    // README's binding block, the three a module sets.
    private const string Library = "3DEngine.Modeling";
    private const string Host = "Modeling.Tests/BindingHost.cs";

    private static string Root
    {
        get
        {
            var at = new DirectoryInfo(AppContext.BaseDirectory);
            while (at is not null && !File.Exists(Path.Combine(at.FullName, "Modeling.slnx")))
                at = at.Parent;
            return at?.FullName
                ?? throw new DirectoryNotFoundException("No Modeling.slnx above the tests.");
        }
    }

    private static readonly string Deps = Path.Combine(AppContext.BaseDirectory, Assembly.GetExecutingAssembly().GetName().Name + ".deps.json");
    private static readonly string Readme = File.ReadAllText(Path.Combine(Root, "README.md"));

    [Fact]
    public void The_Library_Takes_No_Engine_Window_Or_Graphics_Api() =>
        Assert.Null(ModuleChecks.ReferenceFault(Assembly.Load(Library), Deps));

    [Fact]
    public void No_Public_Name_Or_Its_Documentation_Names_An_Engine() =>
        Assert.Null(ModuleChecks.EngineNameFault(Assembly.Load(Library)));

    [Fact]
    public void The_Readme_Names_Every_Public_Type() =>
        Assert.Null(ModuleChecks.UnnamedFault(Assembly.Load(Library), Readme));

    [Fact]
    public void The_Readmes_Binding_Block_Stands_Word_For_Word_In_The_Host() =>
        Assert.Null(ModuleChecks.BindingBlock(Readme, File.ReadAllText(Path.Combine(Root, Host))));

    [Fact]
    public void The_Cheatsheet_Is_What_The_Built_Library_Writes()
    {
        var fault = ModuleChecks.CheatsheetFault(Assembly.Load(Library), Readme, Path.Combine(Root, "CHEATSHEET.md"));
        Assert.True(fault is null, fault);
    }
}
