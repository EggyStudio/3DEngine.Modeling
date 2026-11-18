using System.Numerics;

namespace Modeling;

/// <summary>
/// What the model commands ask of an engine's world, to draw a model and to take away what draws
/// it.
/// </summary>
/// <remarks>
/// A shown model is drawn as its mesh builds and drawn again as the commands change it, which the
/// engine tells by the mesh's <see cref="EditMesh.Version"/>, a count that moves on every change to
/// what it builds into, so no call says that a mesh changed.
/// </remarks>
public interface IModelScene
{
    /// <summary>
    /// Draws a model at a point in the world, in meters, or moves it there where it is drawn
    /// already.
    /// </summary>
    void Show(string name, EditMesh mesh, Vector3 at);

    /// <summary>Takes away what draws a model, where anything does.</summary>
    void Forget(string name);
}
