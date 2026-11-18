using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;
using Xunit;

namespace Modeling.Tests;

/// <summary>
/// The texture collection under Modeling/Textures, each set's three KTX2 files read and their top
/// levels decoded by the Materials module as a GPU samples them, against what the build measured as
/// it built them.
/// </summary>
public class TexturesTests
{
    private const uint Bc5 = 141;
    private const uint Bc7 = 145;
    private const uint Bc7Srgb = 146;

    private static readonly string Collection = Path.Combine(RepositoryRoot(), "Modeling", "Textures");

    public static TheoryData<string> Sets()
    {
        var names = new TheoryData<string>();
        foreach (var entry in Manifest().RootElement.GetProperty("entries").EnumerateArray())
            names.Add(entry.GetProperty("name").GetString()!);
        return names;
    }

    [Theory]
    [MemberData(nameof(Sets))]
    public void Each_Set_Loads_With_Its_Channels(string name)
    {
        var entry = Entry(name);
        var size = entry.GetProperty("size");
        int width = size[0].GetInt32(), height = size[1].GetInt32();

        // The color, opaque everywhere, its mean the one the build measured before encoding.
        Read(name, "albedo.ktx2", Bc7Srgb, width, height);
        var color = Decoded(name, "albedo.ktx2");
        for (int i = 3; i < color.Length; i += 4)
            Assert.Equal(255, color[i]);
        AssertMeans(entry.GetProperty("color"), color, 4, $"{name}'s color");

        // The normal's X and Y, each texel's a unit normal's with Z left over, flat where the
        // recipe names no normal map and varying where it does.
        int smallWidth = Math.Max(width / 2, 4), smallHeight = Math.Max(height / 2, 4);
        Read(name, "normal.ktx2", Bc5, smallWidth, smallHeight);
        var normal = Decoded(name, "normal.ktx2");
        var slopes = new HashSet<(byte, byte)>();
        for (int i = 0; i < normal.Length; i += 4)
        {
            double x = normal[i] / 127.5 - 1, y = normal[i + 1] / 127.5 - 1;
            Assert.True(x * x + y * y <= 1.0, $"{name}'s normal at texel {i / 4} has X {x:F3} and Y {y:F3}, longer than a unit normal");
            slopes.Add((normal[i], normal[i + 1]));
        }
        if (entry.TryGetProperty("normal", out _))
            Assert.True(slopes.Count > 100, $"{name}'s normal holds {slopes.Count} slopes where its recipe names a map");
        else
            Assert.Equal(((byte)128, (byte)128), Assert.Single(slopes));

        // Occlusion, roughness, metallic and translucency, their means the build's.
        Read(name, "surface.ktx2", Bc7, smallWidth, smallHeight);
        var surface = Decoded(name, "surface.ktx2");
        AssertMeans(entry.GetProperty("surface"), surface, 4, $"{name}'s surface");
    }

    [Fact]
    public void The_Collection_Keeps_To_Its_Bounds()
    {
        long total = 0;
        foreach (var entry in Manifest().RootElement.GetProperty("entries").EnumerateArray())
        {
            var name = entry.GetProperty("name").GetString()!;
            foreach (var file in new[] { "albedo.ktx2", "normal.ktx2", "surface.ktx2" })
            {
                var bytes = new FileInfo(Path.Combine(Collection, name, file)).Length;
                Assert.Equal(entry.GetProperty("files").GetProperty(file).GetInt64(), bytes);
                total += bytes;
            }

            var size = entry.GetProperty("size");
            Assert.True(size[0].GetInt32() <= 1024 && size[1].GetInt32() <= 1024, $"{name} is {size[0]} by {size[1]}");
        }

        Assert.True(total <= 32L << 20, $"the collection is {total} bytes");
    }

    // -- KTX2

    /// <summary>
    /// A set's file read as KTX2, its format and size the ones given, every level down to a texel
    /// held as its blocks are with nothing compressing them, its top level's blocks returned.
    /// </summary>
    private static byte[] Read(string name, string file, uint format, int width, int height)
    {
        var path = Path.Combine(Collection, name, file);
        var bytes = File.ReadAllBytes(path);
        byte[] identifier = [0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32, 0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A];
        Assert.Equal(identifier, bytes[..12]);
        uint At(int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));

        var levels = (int)At(40);
        Assert.Equal((format, (uint)width, (uint)height, 0u), (At(12), At(20), At(24), At(44)));
        Assert.Equal(BitOperations.Log2((uint)Math.Max(width, height)) + 1, levels);

        byte[] top = [];
        for (int level = 0; level < levels; level++)
        {
            var index = bytes.AsSpan(80 + 24 * level);
            var offset = BinaryPrimitives.ReadUInt64LittleEndian(index);
            var length = BinaryPrimitives.ReadUInt64LittleEndian(index[8..]);
            int w = Math.Max(1, width >> level), h = Math.Max(1, height >> level);
            Assert.Equal((ulong)((w + 3) / 4 * ((h + 3) / 4) * 16), length);
            Assert.Equal(length, BinaryPrimitives.ReadUInt64LittleEndian(index[16..]));
            Assert.True(offset % 16 == 0 && offset + length <= (ulong)bytes.Length, $"{path} level {level} lies at {offset}");
            if (level == 0)
                top = bytes[(int)offset..(int)(offset + length)];
        }
        return top;
    }

    // A set's file's top level decoded by the Materials module, four bytes a texel, a channel the
    // format does not hold reading zero and an alpha it does not hold 255.
    private static byte[] Decoded(string name, string file) =>
        Materials.Ktx2.Read(File.ReadAllBytes(Path.Combine(Collection, name, file))).Decode();

    private static void AssertMeans(JsonElement expected, byte[] texels, int channels, string what)
    {
        var count = texels.Length / channels;
        for (int c = 0; c < expected.GetArrayLength(); c++)
        {
            double sum = 0;
            for (int i = c; i < texels.Length; i += channels)
                sum += texels[i];
            var mean = sum / count / 255;
            Assert.True(Math.Abs(mean - expected[c].GetDouble()) <= 2.0 / 255, $"{what}'s channel {c} has a mean of {mean:F4} where the build measured {expected[c].GetDouble():F4}");
        }
    }

    private static JsonDocument Manifest() => JsonDocument.Parse(File.ReadAllText(Path.Combine(Collection, "textures.json")));

    private static JsonElement Entry(string name) =>
        Manifest().RootElement.GetProperty("entries").EnumerateArray().Single(entry => entry.GetProperty("name").GetString() == name);

    /// <summary>The folder holding Modeling.slnx, from the tests' own folder up.</summary>
    private static string RepositoryRoot()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "Modeling.slnx")))
                return folder.FullName;
        }
        throw new DirectoryNotFoundException($"No folder above {AppContext.BaseDirectory} holds Modeling.slnx.");
    }
}
