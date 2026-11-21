using System.Numerics;
using Materials;

namespace Modeling;

/// <summary>
/// What a slot is drawn with, a description of the Materials module's and the fields a recipe gave,
/// which a recipe's color and maps change a field at a time and an imported model's material is
/// taken whole.
/// </summary>
internal sealed record SlotMaterial(MaterialDescription Description, SlotGiven Given)
{
    /// <summary>A slot that gives nothing, the prototype material of its name.</summary>
    public static SlotMaterial Blank() => new(new MaterialDescription(), SlotGiven.None);

    /// <summary>
    /// An imported material, every value of it held and the fields it sets given, the metalness and
    /// roughness always and a color, a light and a picture where it has one.
    /// </summary>
    public static SlotMaterial Of(MaterialDescription look)
    {
        var given = SlotGiven.Metallic | SlotGiven.Roughness;
        if (look.BaseColor != Vector4.One) given |= SlotGiven.Color;
        if (look.Emissive != Vector3.Zero) given |= SlotGiven.Emissive;
        if (look.PictureOf(PictureRole.BaseColor) is not null) given |= SlotGiven.Texture;
        if (look.PictureOf(PictureRole.NormalX) is not null) given |= SlotGiven.NormalMap;
        if (look.PictureOf(PictureRole.Occlusion) is not null) given |= SlotGiven.OcclusionMap;
        if (look.PictureOf(PictureRole.Roughness) is not null || look.PictureOf(PictureRole.Metallic) is not null)
            given |= SlotGiven.MetallicRoughnessMap;
        if (look.PictureOf(PictureRole.Emissive) is not null) given |= SlotGiven.EmissiveMap;
        return new SlotMaterial(look, given);
    }

    public string? Texture => PathOf(PictureRole.BaseColor);

    public string? NormalMap => PathOf(PictureRole.NormalX);

    public string? OcclusionMap => PathOf(PictureRole.Occlusion);

    public string? MetallicRoughnessMap => PathOf(PictureRole.Roughness) ?? PathOf(PictureRole.Metallic);

    public string? EmissiveMap => PathOf(PictureRole.Emissive);

    /// <summary>
    /// The color a recipe writes, sRGB's numbers to five places, which the description holds as
    /// linear light.
    /// </summary>
    public static Vector4 Srgb(Vector4 linear) =>
        new(Round(Modeling.Srgb.Encode(linear.X)), Round(Modeling.Srgb.Encode(linear.Y)), Round(Modeling.Srgb.Encode(linear.Z)), linear.W);

    public SlotMaterial WithColor(Vector4 srgb) => new(
        Description with { BaseColor = new Vector4(Modeling.Srgb.Decode(srgb.X), Modeling.Srgb.Decode(srgb.Y), Modeling.Srgb.Decode(srgb.Z), srgb.W) },
        Given | SlotGiven.Color);

    public SlotMaterial WithEmissive(Vector3 light) => new(Description with { Emissive = light }, Given | SlotGiven.Emissive);

    public SlotMaterial WithMetallic(float metallic) => new(Description with { Metallic = metallic }, Given | SlotGiven.Metallic);

    public SlotMaterial WithRoughness(float roughness) => new(Description with { Roughness = roughness }, Given | SlotGiven.Roughness);

    /// <summary>
    /// A picture given or taken away, the roles the layout holds taken out of any picture that held
    /// them, a picture left holding none dropped, and the file added with the layout where one is
    /// given.
    /// </summary>
    public SlotMaterial WithPicture(SlotGiven flag, PictureLayout layout, string? path)
    {
        PictureRole?[] channels = [layout.R, layout.G, layout.B, layout.A];
        var roles = channels.Where(role => role is not null).Select(role => role!.Value).ToList();
        if (layout.Holds(PictureRole.NormalX)) roles.Add(PictureRole.NormalZ);

        var pictures = new List<MaterialPicture>();
        foreach (var picture in Description.Pictures)
        {
            PictureRole? Kept(PictureRole? role) => role is { } held && roles.Contains(held) ? null : role;
            var l = picture.Layout;
            PictureRole?[] left = [Kept(l.R), Kept(l.G), Kept(l.B), Kept(l.A)];
            if (left.All(role => role is null)) continue;
            pictures.Add(picture with { Layout = new PictureLayout(left[0], left[1], left[2], left[3]) });
        }

        if (path is not null) pictures.Add(new MaterialPicture(path, layout));
        return new SlotMaterial(Description with { Pictures = pictures }, path is null ? Given & ~flag : Given | flag);
    }

    /// <summary>The words that give the fields a recipe gave, none where it gave none.</summary>
    public string Words()
    {
        var words = new List<string>();
        if (Given.HasFlag(SlotGiven.Color))
        {
            var color = Srgb(Description.BaseColor);
            words.Add($"color {EditMesh.F(color.X)} {EditMesh.F(color.Y)} {EditMesh.F(color.Z)} {EditMesh.F(color.W)}");
        }

        if (Given.HasFlag(SlotGiven.Texture) && Texture is { } texture) words.Add($"texture {texture}");
        if (Given.HasFlag(SlotGiven.NormalMap) && NormalMap is { } normal) words.Add($"normal-map {normal}");
        if (Given.HasFlag(SlotGiven.OcclusionMap) && OcclusionMap is { } occlusion) words.Add($"occlusion-map {occlusion}");
        if (Given.HasFlag(SlotGiven.MetallicRoughnessMap) && MetallicRoughnessMap is { } metal) words.Add($"metallic-roughness-map {metal}");
        if (Given.HasFlag(SlotGiven.Emissive)) words.Add($"emissive {EditMesh.V(Description.Emissive)}");
        if (Given.HasFlag(SlotGiven.EmissiveMap) && EmissiveMap is { } glow) words.Add($"emissive-map {glow}");
        if (Given.HasFlag(SlotGiven.Metallic)) words.Add($"metallic {EditMesh.F(Description.Metallic)}");
        if (Given.HasFlag(SlotGiven.Roughness)) words.Add($"roughness {EditMesh.F(Description.Roughness)}");
        return string.Join(", ", words);
    }

    private string? PathOf(PictureRole role) => Description.PictureOf(role)?.Picture.Path;

    private static float Round(float value) => MathF.Round(value, 5);
}
