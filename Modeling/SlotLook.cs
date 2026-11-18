using System.Numerics;

namespace Modeling;

/// <summary>
/// What a material slot of an edit mesh is drawn with, a color, a texture and the maps of a glTF
/// material, each null until the recipe gives it.
/// </summary>
/// <remarks>
/// <para>
/// A look with nothing given is drawn with the prototype material of the slot's name, a grid tinted
/// by a color the name gives, so a level blocked out reads by color before it has textures. A color
/// alone tints the grid in its place, and a texture takes the grid's place, tinted by the color
/// where one is given. The engine that draws the mesh holds the prototype materials.
/// </para>
/// <para>
/// A path is read through the mesh's <see cref="EditMesh.Files"/>. Every texture repeats, since a
/// face's coordinates run past one as far as it is meters wide.
/// </para>
/// </remarks>
public sealed record SlotLook
{
    /// <summary>
    /// The color, as sRGB numbers from zero to one as a color picker shows them, with its alpha, or
    /// null.
    /// </summary>
    public Vector4? Color { get; init; }

    /// <summary>The picture the faces show, the material's base color texture, or null.</summary>
    public string? Texture { get; init; }

    /// <summary>The normal map, or null.</summary>
    public string? NormalMap { get; init; }

    /// <summary>The ambient occlusion map, or null.</summary>
    public string? OcclusionMap { get; init; }

    /// <summary>
    /// The map of metalness in its blue channel and roughness in its green, as glTF lays it out, or
    /// null.
    /// </summary>
    public string? MetallicRoughnessMap { get; init; }

    /// <summary>The emissive color, in linear numbers that may pass one, or null.</summary>
    public Vector3? Emissive { get; init; }

    /// <summary>The emissive map, or null.</summary>
    public string? EmissiveMap { get; init; }

    /// <summary>How metal the faces are, from zero to one, or null.</summary>
    public float? Metallic { get; init; }

    /// <summary>How rough the faces are, from zero to one, or null.</summary>
    public float? Roughness { get; init; }

    /// <summary>
    /// Whether nothing is given, so the slot is drawn with the prototype material of its name.
    /// </summary>
    public bool IsPrototype =>
        Color is null && Texture is null && NormalMap is null && OcclusionMap is null && MetallicRoughnessMap is null
        && Emissive is null && EmissiveMap is null && Metallic is null && Roughness is null;

    /// <summary>
    /// What is given, in the recipe's words, as <c>color 1 0 0 1, texture bricks.png</c>, or
    /// nothing for a prototype.
    /// </summary>
    public string Words()
    {
        var given = new List<string>();
        if (Color is { } color) given.Add($"color {EditMesh.F(color.X)} {EditMesh.F(color.Y)} {EditMesh.F(color.Z)} {EditMesh.F(color.W)}");
        if (Texture is { } texture) given.Add($"texture {texture}");
        if (NormalMap is { } normal) given.Add($"normal-map {normal}");
        if (OcclusionMap is { } occlusion) given.Add($"occlusion-map {occlusion}");
        if (MetallicRoughnessMap is { } metal) given.Add($"metallic-roughness-map {metal}");
        if (Emissive is { } light) given.Add($"emissive {EditMesh.V(light)}");
        if (EmissiveMap is { } glow) given.Add($"emissive-map {glow}");
        if (Metallic is { } metallic) given.Add($"metallic {EditMesh.F(metallic)}");
        if (Roughness is { } roughness) given.Add($"roughness {EditMesh.F(roughness)}");
        return string.Join(", ", given);
    }
}
