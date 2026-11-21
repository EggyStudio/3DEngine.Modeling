namespace Modeling;

/// <summary>
/// What a recipe gave a material slot, one flag a field, so a slot that gave nothing is drawn with
/// the prototype material of its name and one that gave a color or a map is drawn with it.
/// </summary>
/// <remarks>
/// A slot's description holds a value for every field, which a field no recipe gave holds as the
/// description's default, so the flags are what tell a color a recipe set from white.
/// </remarks>
[Flags]
public enum SlotGiven
{
    /// <summary>Nothing, so the slot is drawn with the prototype material of its name.</summary>
    None = 0,

    /// <summary>The base color, which a recipe writes in sRGB.</summary>
    Color = 1,

    /// <summary>The base color's picture.</summary>
    Texture = 2,

    /// <summary>The normal map's picture.</summary>
    NormalMap = 4,

    /// <summary>The occlusion map's picture.</summary>
    OcclusionMap = 8,

    /// <summary>The picture that holds roughness and metalness.</summary>
    MetallicRoughnessMap = 16,

    /// <summary>The emitted light.</summary>
    Emissive = 32,

    /// <summary>The emission's picture.</summary>
    EmissiveMap = 64,

    /// <summary>The metalness.</summary>
    Metallic = 128,

    /// <summary>The roughness.</summary>
    Roughness = 256,
}
