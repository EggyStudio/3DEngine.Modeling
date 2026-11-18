using System.Reflection;
using Xunit;

namespace Modeling.Tests;

public class LibraryTests
{
    [Fact]
    public void The_Library_Loads()
    {
        // By its name, so the test reads that the project reference puts the assembly beside the
        // tests under its name, and that the module's types are in it.
        var library = Assembly.Load("3DEngine.Modeling");
        Assert.Equal("3DEngine.Modeling", library.GetName().Name);
        Assert.Same(library, typeof(EditMesh).Assembly);
    }
}
