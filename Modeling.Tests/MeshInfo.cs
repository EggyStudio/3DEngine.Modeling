namespace Modeling.Tests;

/// <summary>
/// A mesh's counts, volume, bounds and grid as they stand when it is taken, so a test compares a
/// mesh before an operation with after.
/// </summary>
internal readonly record struct MeshInfo(
    int VertexCount,
    int EdgeCount,
    int FaceCount,
    int OpenEdges,
    int SelectedVertexCount,
    int SelectedEdgeCount,
    int SelectedFaceCount,
    float Volume,
    Bounds Bounds,
    float GridStep)
{
    /// <summary>
    /// Whether the mesh has faces and every edge is shared by two faces running along it in
    /// opposite directions.
    /// </summary>
    public bool IsClosed => FaceCount > 0 && OpenEdges == 0;

    /// <summary>The mesh's info as it stands.</summary>
    public static MeshInfo Of(EditMesh mesh) => new(mesh.VertexCount, mesh.EdgeCount, mesh.FaceCount, mesh.OpenEdges, mesh.SelectedVertexCount,
        mesh.SelectedEdgeCount, mesh.SelectedFaceCount, mesh.Volume, mesh.Bounds, mesh.GridStep);
}
