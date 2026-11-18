using System.Numerics;

namespace Modeling;

/// <summary>
/// A corner of a built mesh's triangle, its place, its normal and its texture coordinates.
/// </summary>
/// <remarks>
/// <see cref="EditMeshBuild.Slots"/> gives these, and an engine copies each into a vertex of its
/// own. No tangent is given, since a normal map's frame follows from the texture coordinates, and
/// an engine works it out where it draws, from the coordinates as it draws or as tangents it
/// generates when it takes the mesh.
/// </remarks>
/// <param name="Position">The place, in the mesh's space, in meters.</param> <param
/// name="Normal">The way the surface faces at the corner, a length of one.</param> <param
/// name="Uv">The texture coordinates.</param>
public readonly record struct MeshVertex(Vector3 Position, Vector3 Normal, Vector2 Uv);
