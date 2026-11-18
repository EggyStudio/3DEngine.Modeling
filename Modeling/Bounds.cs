using System.Numerics;

namespace Modeling;

/// <summary>A box along the axes, its lowest corner and its highest, in meters.</summary>
/// <param name="Min">The corner with the lowest X, Y and Z.</param>
/// <param name="Max">The corner with the highest X, Y and Z.</param>
public readonly record struct Bounds(Vector3 Min, Vector3 Max);
