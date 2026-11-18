namespace Modeling;

/// <summary>
/// A map a material slot's look names by its file, each with the word its recipe line takes.
/// </summary>
/// <remarks>
/// The maps are those of a glTF material, which both engines' materials hold, so an engine loads
/// each file into the map of its own material of the same meaning.
/// </remarks>
public enum MaterialMap
{
    /// <summary>
    /// The base color's texture, which takes the grid's place, <c>texture</c> in a recipe.
    /// </summary>
    Albedo,

    /// <summary>The normal map, in tangent space, <c>normal-map</c> in a recipe.</summary>
    Normal,

    /// <summary>The ambient occlusion map, <c>occlusion-map</c> in a recipe.</summary>
    Occlusion,

    /// <summary>
    /// The map of metalness in its blue channel and roughness in its green, as glTF lays it out,
    /// <c>metallic-roughness-map</c> in a recipe.
    /// </summary>
    MetallicRoughness,

    /// <summary>The emissive map, <c>emissive-map</c> in a recipe.</summary>
    Emission,
}
