[Devlog](https://github.com/EggyStudio/Modeling/blob/main/DEVLOG.md) [Cheatsheet](https://github.com/EggyStudio/Modeling/blob/main/CHEATSHEET.md)

# Modeling

Modeling is a library both engines, 3DEngine and BevyCSharp, take as a git submodule, for the edit
mesh shaped by operations through a selection, its recipes and the `model.*` commands. It references
no engine, and each engine implements the interfaces it declares where it binds the module.

## What it holds

| Type | Gives |
|---|---|
| `EditMesh` | A polygon mesh shaped by primitives and by operations on its selection, its faces of any count kept whole, its material slots, its modifiers, and its recipe, the line each operation writes, which is its undo and its file and makes the mesh again when it is run |
| `EditMeshBuild` | The mesh with its modifiers applied, built into the vertices and triangles of each slot, each face cut by ear clipping with its own normals and texture coordinates, smoothed within its angle |
| `MeshVertex` and `Bounds` | A built vertex, its place, normal and texture coordinates, which an engine copies into its own, and a box along the axes |
| `SlotLook` and `MaterialMap` | What a slot is drawn with, a color, a texture and the maps of a glTF material by their files, metalness, roughness and an emissive color, which an engine loads into a material of its own |
| `IModelFiles`, `ModelPart` and `ModelFolder` | The files a mesh reads and writes, its recipe's, the recipes its joins name, the model files it imports as placed triangles with their normals, texture coordinates and material's look where the file holds them, its textures' widths and the glTF it is exported as, which an engine implements with its own lookup and model reader, and the files of a folder, which read glTF, for the tests and a program with no engine |
| `SelectionMode` | Whether a selection replaces what was selected, adds to it or takes from it |

## Taking it into an engine

An engine adds the repository as a git submodule under `modules/` and references its project, here
from a project one folder below the engine's root.

```bash
git submodule add https://github.com/EggyStudio/Modeling.git modules/Modeling
```

```xml
<ProjectReference Include="..\modules\Modeling\Modeling\Modeling.csproj" />
```

A clone of the engine fetches the module with `git clone --recurse-submodules`, or with
`git submodule update --init` in a clone made without it, and a workflow checks it out with
`submodules: true` on `actions/checkout`. A change the engine uses is committed and pushed here
before the engine's commit that moves its pointer, so the engine's history never names a commit
this repository does not have.

## Binding it in your engine

The module shapes a mesh and gives out its data, and an engine draws it, collides with it and loads
its materials. Its assembly is `3DEngine.Modeling`, and its types stand in the namespace `Modeling`.
It opens no window and no graphics device, reads and writes no file but through an interface, and
declares one interface for what it needs of an engine.

The library takes the Materials module, whose `MaterialDescription` an imported model's materials
are read as. A Materials checkout beside this one is built where it stands, so a program that takes
both builds one Materials, and this repository's submodule `modules/Materials` is built where none
stands beside it.

| Interface | What the engine answers |
|---|---|
| `IModelFiles` | A text file read and written whole by its path, a recipe's, a file of bytes written whole, a glTF file's and its buffer's, a model file's meshes as triangles placed by the file's nodes, each a `ModelPart`, and an image's width in pixels, which every file a mesh's recipe names is read through |

A mesh is made with the engine's files, `new EditMesh(files)`, or read with them by
`EditMesh.Load` or `EditMesh.FromRecipe`, and every mesh a join, a boolean or a modifier makes inside
it shares them. `ModelFolder` is an `IModelFiles` over a folder, reading the width of a PNG or a KTX2
from its header and no model file. The binding reads what `EditMeshBuild.Slots` gives, a mesh a
material slot of `MeshVertex` corners and uint triangles, and each slot's `SlotLook` by
`EditMesh.LookAt`, whose maps `MaterialMap` names, and makes meshes and materials of its own. An
engine whose meshes are entities keeps an edit mesh on its entity, as BevyCSharp's modeling does, and
one with a flat API of handles keeps them by an id in a store, as 3DEngine's does.

What the module gives out stands on these conventions.

| Quantity | Convention |
|---|---|
| Length | Meters, the grid's step from a sixteenth of a meter to 64 |
| Axes | Y up and right-handed, as System.Numerics' cross product is, `select +y` picking the faces that face up |
| Triangles | Counterclockwise seen from the side a face faces, each face's corners its own in the build |
| Texture coordinates | A face's corners' places in meters seen along the axis its normal is nearest until a `uv` line gives it its own, the second running down a wall, so a picture's first row is its top |
| Angles | Degrees, a rotation counterclockwise looking back along its axis |
| Color | A slot's color as sRGB numbers from 0 to 1 with its alpha, as a color picker shows them, and its emissive color linear, past 1 for a bright light |
| Paths | One word with forward slashes, as a recipe line holds it, found by the engine's `IModelFiles` |
| A plane or a center with no `at` | Through the origin, for a knife, a mirror, a spin and a modifier's mirror, so an engine whose own lines meant the selection's middle writes the point with `at` |

An engine with Z up or left-handed axes turns the positions and normals it is given, and one that
takes clockwise triangles as their front takes each triangle's corners in the other order. A recipe's
file starts with `model 1` and ends in `.model`.

The smallest binding keeps its files in memory, builds a mesh into a mesh of its own for each slot,
and copies each slot's look into its material.

```csharp
using System.Numerics;
using Modeling;

// The engine's files, kept here in memory by their paths, which every mesh it makes reads and
// writes. An engine on a disk finds them as it finds a program's other files, and its own model
// reader gives a model file's meshes.
public sealed class HostFiles : IModelFiles
{
    private readonly Dictionary<string, string> _texts = [];
    private readonly Dictionary<string, byte[]> _bytes = [];
    private readonly Dictionary<string, IReadOnlyList<ModelPart>> _models = [];
    private readonly Dictionary<string, int> _images = [];

    public void PutText(string path, string text) => _texts[path] = text;

    public void PutModel(string path, IReadOnlyList<ModelPart> parts) => _models[path] = parts;

    public void PutImage(string path, int width) => _images[path] = width;

    public string ReadText(string path) =>
        _texts.TryGetValue(path, out var text) ? text : throw new FileNotFoundException($"There is no file at {path}.", path);

    public string WriteText(string path, string text)
    {
        _texts[path] = text;
        return path;
    }

    public string WriteBytes(string path, byte[] bytes)
    {
        _bytes[path] = bytes;
        return path;
    }

    public byte[] Bytes(string path) => _bytes[path];

    public IReadOnlyList<ModelPart> ReadModel(string path) =>
        _models.TryGetValue(path, out var parts) ? parts : throw new FileNotFoundException($"There is no model at {path}.", path);

    public int? ImageWidth(string path) => _images.TryGetValue(path, out var width) ? width : null;
}

// The engine's vertex, laid out as its renderer reads it, and its material and mesh of a slot.
public readonly record struct HostVertex(Vector3 Position, Vector3 Normal, Vector2 TexCoord);

public sealed record HostMaterial(string Name, Vector4 Color, string? Albedo, string? Normal, Vector3 Emissive);

public sealed record HostMesh(HostVertex[] Vertices, uint[] Indices, HostMaterial Material);

// An edit mesh built into the engine's meshes, one a slot in the order of its slots, each vertex
// copied into the engine's and each slot's look into its material, white where it gives no color.
public static class HostBinding
{
    public static HostMesh[] Build(EditMesh mesh)
    {
        var built = EditMeshBuild.Slots(mesh);
        var meshes = new HostMesh[built.Length];
        for (var slot = 0; slot < built.Length; slot++)
        {
            var (vertices, indices) = built[slot];
            var look = mesh.LookAt(slot);
            var material = new HostMaterial(mesh.Slots[slot], look.Color ?? Vector4.One, look.Texture, look.NormalMap, look.Emissive ?? Vector3.Zero);
            meshes[slot] = new HostMesh([.. vertices.Select(v => new HostVertex(v.Position, v.Normal, v.Uv))], indices, material);
        }
        return meshes;
    }
}
```

## Building and testing

The library takes the Materials module and the tests take the checks every module keeps from the
Tools module, git submodules under `modules/Materials` and `modules/Tools`, so a clone fetches them
with `git clone --recurse-submodules`, or with `git submodule update --init` in a clone made without
it. With the .NET 10 SDK:

```bash
dotnet build Modeling.slnx
dotnet test Modeling.Tests
```

## Textures

`Modeling/Textures` holds interior and architectural texture sets, each three KTX2 files with their
mipmaps in the formats of BevyCSharp's vegetation collection, which a GPU samples as they are.

| File | Channels | Encoding | Size |
|---|---|---|---|
| `albedo.ktx2` | color in RGB, opacity in A | BC7, sRGB | the set's |
| `normal.ktx2` | the tangent-space normal's X and Y in RG | BC5 | half the albedo's |
| `surface.ktx2` | occlusion in R, roughness in G, metallic in B, translucency in A | BC7, linear | half the albedo's |

| Set | Maker | Albedo | Files |
|---|---|---|---|
| `fleck-carpet` | Rohner | 1024 by 1024 | 1,365, 341 and 341 KB |
| `chevron-wallpaper` | RegularlyBlue | 512 by 1024 | 683, 171 and 171 KB |

The collection holds two sets in six files, 3.0 MB together, as `build/textures.py` writes their
sizes into the manifest. A set is at most 1024 square and the collection at most 32 MB, which
`dotnet test Modeling.Tests` holds, along with each set's channels decoded as a GPU decodes them.
The levels are kept without the zstd BevyCSharp's collection puts over them, since .NET's library
has none, so a reader in C# takes the files with no package added.

`textures.json` beside the sets is the recipe of each, its source files under `.ref/`, which the
repository leaves out, its size, its maps and what each channel takes, and `build/textures.py`
builds the sets from it the same byte for byte, with Python 3 and Pillow.

```bash
build/textures.py                      # build every set whose source is under .ref/
build/textures.py chevron-wallpaper    # build one
```

Neither source has a roughness map, so each recipe gives one roughness to the whole set, 0.95 for
the carpet and 0.8 for the paper, and the wallpaper's normal is flat, as a color scan holds no
relief. The carpet's normal map says nothing of which way its green points and is kept as it came,
and `flip_green` in its recipe turns it over. The wallpaper's scan does not meet itself at its
edges, so its set is built from its repeat, a half-drop of 4176 by 6243 of the scan's pixels that
`build/textures.py --repeat` found, drawn straight into 512 by 1024, each texel the mean of the
copies of its place the scan holds. A surface maps the tile to 17.7 by 26.4 cm, the size the
manifest gives from the scan's 600 dots an inch, so its texels are a third wider than they are tall.

## License

The Mozilla Public License 2.0, in [LICENSE](LICENSE).
