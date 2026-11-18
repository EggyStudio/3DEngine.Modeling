using System.Buffers.Binary;
using System.Text;

namespace Modeling;

/// <summary>
/// The files of a folder, a relative path read below it and a rooted one as it is, for the tests
/// and for a program with no engine.
/// </summary>
/// <remarks>
/// It reads no model file, since the module holds no model reader, so a recipe's <c>import</c> line
/// is refused here and read through an engine's <see cref="IModelFiles"/>. An image's width is read
/// from the header of a PNG or a KTX2 file.
/// </remarks>
/// <param name="root">
/// The folder a relative path is read below, or null for the working directory at each call.
/// </param>
public sealed class ModelFolder(string? root = null) : IModelFiles
{
    /// <summary>
    /// The working directory's files, which a mesh made with no files of its own reads.
    /// </summary>
    public static ModelFolder WorkingDirectory { get; } = new();

    /// <summary>The full path a path names, below the folder where it is relative.</summary>
    public string Full(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return Path.GetFullPath(root is null ? path : Path.Combine(root, path));
    }

    /// <inheritdoc/>
    public string ReadText(string path)
    {
        var full = Full(path);
        if (!File.Exists(full)) throw new FileNotFoundException($"There is no file at {path}.", path);
        return File.ReadAllText(full);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Written under a name of its own and moved into place, without a byte order mark.
    /// </remarks>
    public string WriteText(string path, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var full = Full(path);
        if (Path.GetDirectoryName(full) is { Length: > 0 } folder) Directory.CreateDirectory(folder);
        var part = full + ".part";
        File.WriteAllText(part, text, new UTF8Encoding(false));
        File.Move(part, full, overwrite: true);
        return full;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ModelPart> ReadModel(string path)
    {
        if (!File.Exists(Full(path))) throw new FileNotFoundException($"There is no model at {path}.", path);
        throw new FormatException($"{path} is a model file, which a folder of the module does not read, since the module holds no model reader.");
    }

    /// <inheritdoc/>
    public int? ImageWidth(string path)
    {
        Span<byte> header = stackalloc byte[24];
        try
        {
            using var stream = File.OpenRead(Full(path));
            if (stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) < header.Length) return null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        // A PNG's eight byte signature, then its IHDR chunk's length and type, then the width in
        // four bytes, high byte first.
        if (header[..8].SequenceEqual((ReadOnlySpan<byte>)[0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]))
            return (int)BinaryPrimitives.ReadUInt32BigEndian(header[16..20]);

        // A KTX2's twelve byte identifier, then its format and its type's size, then the width in
        // four bytes, low byte first.
        if (header[..12].SequenceEqual((ReadOnlySpan<byte>)[0xAB, (byte)'K', (byte)'T', (byte)'X', (byte)' ', (byte)'2', (byte)'0', 0xBB, 0x0D, 0x0A, 0x1A, 0x0A]))
            return (int)BinaryPrimitives.ReadUInt32LittleEndian(header[20..24]);

        return null;
    }
}
