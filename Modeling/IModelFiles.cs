using System.Numerics;

namespace Modeling;

/// <summary>
/// The files an <see cref="EditMesh"/> reads and writes, its recipe's own file, the recipes its
/// <c>join</c> lines name, the model files its <c>import</c> lines name, the textures its slots
/// name and the glTF file it is exported as, found by a path as the program that holds the mesh
/// finds its files.
/// </summary>
/// <remarks>
/// An engine implements it with its own lookup of a program's files and its own model reader, and
/// gives it to each mesh it makes. <see cref="ModelFolder"/> reads and writes a folder, for the
/// tests and for a program with no engine. A path is one word, as a recipe line holds it, with
/// forward slashes on every system.
/// </remarks>
public interface IModelFiles
{
    /// <summary>Reads a text file whole, a recipe's.</summary> <exception
    /// cref="FileNotFoundException">There is no such file.</exception> <exception
    /// cref="IOException">The file could not be read.</exception>
    string ReadText(string path);

    /// <summary>
    /// Writes a text file whole, so a program reading it as it is written reads the old file or the
    /// new one whole, and answers the full path it was written to.
    /// </summary>
    /// <exception cref="IOException">The file could not be written.</exception>
    /// <exception cref="UnauthorizedAccessException">
    /// The file or its folder may not be written.
    /// </exception>
    string WriteText(string path, string text);

    /// <summary>
    /// Writes a file of bytes whole, as <see cref="WriteText"/> writes text, a glTF file's and its
    /// buffer's, and answers the full path it was written to.
    /// </summary>
    /// <exception cref="IOException">The file could not be written.</exception>
    /// <exception cref="UnauthorizedAccessException">
    /// The file or its folder may not be written.
    /// </exception>
    string WriteBytes(string path, byte[] bytes);

    /// <summary>Reads a model file's meshes, each placed as the file's nodes place it.</summary>
    /// <exception cref="FileNotFoundException">There is no such file.</exception> <exception
    /// cref="FormatException">The file is not a model this reader takes.</exception>
    IReadOnlyList<ModelPart> ReadModel(string path);

    /// <summary>
    /// The width in pixels of an image file, or null where there is no such file or its header
    /// cannot be read.
    /// </summary>
    int? ImageWidth(string path);
}

/// <summary>
/// One mesh of a model file, its triangles placed as the file's nodes place it, and its material's
/// name.
/// </summary>
/// <param name="Positions">The vertices' places, in meters, in the file's space.</param>
/// <param name="Indices">
/// Three vertices a triangle, counterclockwise seen from the side it faces.
/// </param>
/// <param name="Material">
/// The name of the mesh's material as the file gives it, or null where it has none.
/// </param>
public sealed record ModelPart(IReadOnlyList<Vector3> Positions, IReadOnlyList<int> Indices, string? Material);
