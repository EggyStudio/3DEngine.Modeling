# Review

Reviewed up to `6fb25bb`, item 2, pushed at 12:31 on 2026-10-31 with `a7da906`, item 1, after the
owner's first commit, its run green at 12:35 on the three systems in about three minutes, the first
of this repository. After it `883d624`, item 5, pushed at 12:15 on 2026-11-01, its run green at
12:17, the two texture sets as Textures/fleck-carpet and Textures/chevron-wallpaper, each an albedo
in BC7 sRGB, a normal in BC5 and a surface in BC7 with the roughness in green, with textures.json
giving the recipes, sizes, error, mean color and surface, build/textures.py carried from
BevyCSharp's with the leaf and bark parts dropped, a README section with the channel and set tables,
TexturesTests and three captioned readings; the carpet's albedo 1024 square and the wallpaper's 512
by 1024, 3.0 MB together, the levels kept without zstd so the engine's reader takes them; the
wallpaper built from one scan's half-drop repeat since none of its three files tiles at its edges,
the tile 0.1768 by 0.2643 m at the file's 600 dpi; the carried BC7 encoder mended for opaque blocks
whose shared bit went to the color; the carpet's roughness 0.95 and the paper's 0.8 with a flat
normal, no source holding a roughness map; 4 of 4 tests, two builds byte for byte the same. Both
commits keep the form, the three-mark subject, one sentence, the owner's identity as author and
committer, no trailer (N 7.2 and N 7.6 kept at the first commit); the library references no engine
(N 2.1). The documents are the engines' with the engine's name replaced and the engine-only lines
dropped, COMMITS.md the game's with N 7.1's pointer rule in place of the legacy branch line,
.gitignore with the dump folder, .claude and .ref; AGENTS.md's devlog row carries no link until
DEVLOG.md exists and its table of areas gains the rows item 2 adds. The skeleton sets
ImportDirectoryBuildTargets false, since a submodule under modules/ would inherit an engine's
Directory.Build.targets, makes a missing summary an error, loads the assembly by name in its one
test, and build/version.sh prints 0.1.0; the strict build 0 warnings and 0 errors, the test passing.
After it `c72ae9c`, the norm and the review's head alone, pushed at 14:51 on 2026-11-03, with no run
since the build passes over the review's files; it keeps the form. Before it `8dece28`, the owner's
first commit alone. After it `2abb761`, the build folder's row in AGENTS.md naming textures.py,
pushed at 12:18, its run green at 12:20.

## Now

The order the owner set on 2026-10-31 for the six modules, this one holding the edit mesh shaped by
operations through a selection, its recipes and the `model.*` commands. The documents first, then
the rest as the other projects have it, then the engines taking the module. Items 1 and 2 are the
game's session's, being free while the game waits on the engine's package; item 3 is the session's
that owns the code today; item 4 is each engine's session's. Among the six this module comes second,
after Tools, since it needs no renderer and the levels and the landscape build on it.

1. **The documents, one commit.** AGENTS.md on the game's shape (one paragraph saying what the
    module is and that no engine is referenced, N 2.1; the documents table with STYLE, COMMITS,
    REVIEW, NORM and DEVLOG; building and testing; Where things are with a row a folder; the
    conventions, the commit form among them), README.md (what the module is, how an engine takes it
    as a submodule, `dotnet build` and `dotnet test`), `.github/STYLE.md`, `COMMITS.md` and
    `CODE_OF_CONDUCT.md` as the engines' with the engine's name replaced, `.gitignore` of the
    engines' shape with `/.github/dump/` in it, `.gitattributes`. The game's AGENTS.md and the
    engines' documents are the models; nothing is written that the module does not have yet. This
    commit is the first test of the commit form here, N 7.2 and N 7.6, read by the reviewing session
    before it is pushed. Done at `a7da906`.

2. **The skeleton, one commit after item 1.** `Modeling.slnx`, `Modeling/Modeling.csproj` as a class
    library on net10.0 with `Nullable` and `ImplicitUsings` in `Directory.Build.props`,
    `Modeling.Tests/` on xunit with one test that the library loads, `build/version.txt` at `0.1`
    and `build/version.sh` as the engines', and `.github/workflows/build.yml` on a push, `test (os)`
    on ubuntu-24.04, windows-latest and macos-15 with `dotnet build Modeling.slnx -warnaserror` and
    `dotnet test Modeling.Tests --no-build`, a time limit on every job with its reason,
    `permissions` read and the engines' concurrency group. Green on the three systems before item 3.
    Done at `6fb25bb`.

3. **The code's move.** The source is both engines hold it under their Modeling folders in
near-identical form; 3DEngine's is the source, read side by side with BevyCSharp's first and the
differences folded in, so that each engine's own Modeling folder becomes a binding of this one. One
commit a part, the tests coming with the code, the strict build and the suite green, a Shared line
in the engines' REVIEW.md when a part lands. Items written by the reviewing session when item 2 is
green and the owning session is free.

4. **The engines take the module.** Each engine adds this repository as a git submodule under
`modules/` and references its project, its workflows checking submodules out, its own folder for
this code becoming the binding of the module's interfaces (N 2.1) and the rest deleted; a change
used by an engine is committed here first and pushed before the engine's commit that moves the
pointer. One commit an engine, after item 3's first part.

5. **Interior textures in the module's bounded collection.** The owner put two free texture sets
   under BevyCSharp's reference folder on 2026-11-01, a carpet scan of eight JPEG files and a
   chevron wallpaper scan of three PNG files, and chose at 11:52 that textures live with the module
   whose work they serve, these with the modeling as the tree and grass sets live with the
   vegetation (Decision 8). They go into this repository as a bounded collection on BevyCSharp's
   shape, a recipe a set naming the source files and what each channel is, the built files at 1K as
   albedo, normal and roughness in the collection's format with their sizes and count said in the
   README, the README's table naming each set's maker, Rohner for the carpet and RegularlyBlue for
   the wallpaper, and nothing more said of the sets' terms anywhere, in no file and no commit, the
   owner's word of 2026-11-01, and a test that each set loads with its channels; the recipe script
   is BevyCSharp's build/textures.py read for a model, carried here until Tools holds it. The game's
   session takes it, being free. One commit, no engine referenced. Done at `883d624`, read at 12:15,
   as the head records; the build folder's row in AGENTS.md names textures.py in a commit after, a
   row's words for the folder's contents being N 1.5's.

## Verdicts

None yet.

## Decisions

1. **The repository's birth.** The owner made this repository on 2026-10-31 at 12:02 among six,
Vegetation, Levels, Landscape, RadianceCascades, Tools beside it, after the ideas recorded as
3DEngine's Decision 78 and BevyCSharp's Decision 71, to hold the edit mesh shaped by operations
through a selection, its recipes and the `model.*` commands as one library both engines use rather
than one implementation an engine. The owner set the order the same day, the documents first, then
the rest as the other projects have it, then the engines taking the module, and said the modules may
be tried at once beside the engine's global illumination work (3DEngine Decision 79).

2. **A git submodule, not a package.** The owner chose on 2026-10-31 that the engines take the
module as a git submodule built as a project of their own, since a package's upload, wait, test and
pack is slower to iterate than a commit. A change is committed here first; the reviewing session
pushes this repository before the engine's commit that moves its pointer; the workflows check
submodules out. The game keeps using the engine through the engine's package.

3. **The module knows no engine.** N 2.1. The module references no engine, package or bridge; what
it needs of a renderer, a world, a scheduler or a file system it takes through interfaces it
declares, which each engine implements where it binds the module. A type of an engine's in this
repository is a fault.

4. **The repository's terms are the owner's.** The repository was made by the owner with its terms
   file in place, which stands as the owner set it and is spoken of nowhere else.

5. **Who writes here.** The game's session writes items 1 and 2 for the six modules, being free
while the game waits on the engine's package; the session that owns the code today moves it (item
3), BevyCSharp's for Vegetation and Levels, 3DEngine's for Modeling and the cascades, the reviewing
session's scripts going into Tools; each engine's session binds the module (item 4). Every session
reads REVIEW.md's head, its items and the Replies before a batch, as N 7.7 says, and the reviewing
session pushes.

8. **Textures live with the module whose work they serve.** The owner wrote on 2026-11-01 that free
   carpet and wallpaper textures put under the reference folder may be converted and used in the
   asset library being built, and asked whether they need a repository of their own or belong with
   the modeling as the tree and grass textures belong with the vegetation, thinking the latter best;
   so it is, interior and architectural sets here, plants in Vegetation, and a Materials repository
   only if the library grows past what a module's folder should hold.

9. **The module is worked by a session of its own.** Decided at 16:43 on 2026-11-18 for the seven
    modules at once. From its first commit the module's session alone writes the module's source,
    tests and documents, and the engines' sessions write here no longer; an engine's coder that
    needs something of the module asks the module's session in a message and takes the commit by
    moving its submodule's pointer. Every open item above that names an engine's or the game's
    session as its worker is the module's session's, apart from the binding inside an engine, which
    stays that engine's. The session's first Reply reads this file against the tree and says what is
    done, what is open and the order it proposes; the review answers, and that order stands. The
    rules are NORM.md's as before, among them no engine referenced (N 2.1), tests that need no
    window and no GPU, the commit's form, a failing test before a mend, a picture on a white ground
    where a thing is seen, and any GPU program through the lock of N 7.9 with a waiting file. The
    session reports each checked commit to the reviewing session, which reads it, pushes it and
    reads its run; it does not push.

10. **The module is agnostic enough that a stranger binds it in an engine of their own from its
    README alone (2026-11-18 17:28).** It asks a README section, Binding it in your engine, with
    every interface the module declares, the conventions it stands on and the smallest binding that
    works, a small host in the tests that implements those interfaces with no engine and that the
    section's steps are run against, a test that fails where the library references an engine, a
    window or a graphics API, the form in the Tools module's ReferenceTests.cs, and no engine's name
    in a public member or its summary. The two engines appear in the README as examples. A public
    shape that fits the two engines alone is named in a Reply before it changes.

11. **The package id and the assembly are 3DEngine.Modeling and the namespace stays Modeling
    (2026-11-18 17:28).** The prefix gives a unique name. A package is published when the time is
    right and on the owner's word, for a reader who takes the module alone. The engines keep taking
    the module as a git submodule, compiled with them and carried inside each engine's one package.
    Folders and project file names stay.

## Replies

The module's session's first Reply, Decision 9, reading this file against the tree at `163b218`.
Items 1, 2 and 5 are in the tree as the head records them, the documents at `a7da906`, the skeleton
at `6fb25bb` and the two texture sets at `883d624` with the build folder's row at `2abb761`; the
strict build has no warning and the 4 tests pass (dotnet build Modeling.slnx -warnaserror, dotnet
test Modeling.Tests).

Item 3, the code's move, is open and this session's by Decision 9, and BevyCSharp's item 96 and
Vegetation's leaves wait on it for their meshes. Item 4 stays the engines', each binding the module
inside its own tree. 3DEngine's Modeling folder holds 5,358 lines of C# (cat 3DEngine/Modeling/*.cs
| wc -l). Compiled alone it misses three of the engine's types, ModelVertex in the build and the
glTF writer, BoundingBox in the mesh's bounds and MaterialMapIndex in a slot's maps, and reaches the
engine's files in four places, a recipe's file read and written, a joined model's recipe, an
imported model file and a texture's width; its flat API and its 56 `model.*` commands lie outside
it, under Api. BevyCSharp's holds 8,458, the same kernel on its own Vec3 with the commands, the
registry, the materials, the prototypes and the window inside it, and its recipe reads three
operations 3DEngine's does not, the tube primitive, the selection of edges and the boolean modifier.
Their tests number 121 and 116, counted by their Fact and Theory attributes.

The module holds the edit mesh, its recipe, its build into triangles and its exchange as data on
System.Numerics, and the `model.*` commands. An engine binds it through three things. `IModelFiles`
reads and writes a mesh's files, the text of a recipe, a model file's meshes as triangles placed by
the file's nodes, and an image's width, which each engine implements with its own lookup of a
program's files and its own model reader, the module's `ModelFolder` reading a folder for the tests
and the tools. The build gives each slot's vertices, a place, a normal and texture coordinates, and
its triangles as uint indices, which the engine copies into its own vertex. A slot's look names its
maps by the module's `MaterialMap`, and the engine loads them.

The order proposed for item 3, one commit a part, each with its tests, the strict build and the
suite green:

1. The edit mesh, 3DEngine's EditMeshData with its operations, selection, slots, modifiers, recipe
   and build, with EarClipping, FaceMerge, Csg, SelectionMode and SlotLook, as `EditMesh` in the
   namespace Modeling, with `IModelFiles`, `ModelFolder`, `Bounds`, `MaterialMap` and the vertex.
   The build comes in this part since the modifier tests read it. The tests that need no window or
   engine material come with it, each passing, the recipes they write the same to the line as the
   engine's head; those that load an engine's material or model stay in the engine.
2. BevyCSharp's differences folded in, the three operations above and whatever else the reading
   side by side finds, each said in the Reply as folded in or left with its reason, with
   BevyCSharp's tests that cover them.
3. The glTF exchange, 3DEngine's writer and BevyCSharp's reader, so `ModelFolder` imports a glTF,
   with their tests.
4. The `model.*` commands over a store of models by name, declared through an interface the module
   holds and each engine's console implements (N 2.1), its shape proposed in a few lines when part 3
   lands.

The flat API's handle, the ECS registry, the materials and prototypes that load an engine's assets
and the window stay in the engines as their bindings, with the tests that draw, and an engine can
bind from part 1 on. Item 3 asks for a Shared line in the engines' REVIEW.md as a part lands; by
Decision 9 and N 7.3 this session writes here alone, so the line goes to each engine's session and
to the reviewing session by message unless the review says otherwise.

Item 3's first part, the edit mesh, in the commit carrying this line. 3DEngine's EditMeshData moves
as `EditMesh` in the namespace Modeling, with its operations, selection, slots, modifiers, recipe
and build, and with EarClipping, FaceMerge, Csg, SelectionMode, SlotLook and EditMeshBuild. The
engine's ModelVertex, BoundingBox and MaterialMapIndex become the module's MeshVertex, Bounds and
MaterialMap, plain data, the map's Metalness and Roughness one member, MetallicRoughness, as a slot
keeps them in one file. A mesh holds an `IModelFiles` for the four places the kernel reached the
engine's files, a recipe's text read and written, a joined recipe, a model file read as placed
triangles for an import, and a texture's width. `ModelFolder` reads a folder, the width from a PNG's
or a KTX2's header, and refuses a model file until part 3 brings the glTF reader. The public methods
take the flat API's defaults. The kernel gives 3DEngine's head's results to the bit on 4,004
recipes, the three recipe files of the engines and the game and 4,000 drawn at random from the lines
the tests write, the same recipe, vertices, faces, texture coordinates and built triangles with and
without modifiers, or the same refusal for 1,955 of them, by a harness in the session's scratch that
compiles 3DEngine's Modeling folder beside the module. 96 of the 103 test methods of 3DEngine's ten
modeling files come with it on xunit's Assert, as the other modules have, each passing; five stay in
the engine, two that build an engine model or drop a ball on one, two of an engine material and one
of the flat API's unloaded handle, and two come with parts 3 and 4, a glTF file's materials and the
console's commands. SourceTests holds comment lines to 100 columns (N 4.11) and failed on 289 lines
before they were wrapped. The README's table names what the module holds, and AGENTS.md's devlog row
is a link. The strict build is clean and the tests pass, 133 where there were 4 (dotnet test
Modeling.Tests).

Item 3's second part, BevyCSharp's differences, in the commit carrying this line. A harness in the
session's scratch compiles BevyCSharp's Modeling kernel beside the module and runs 2,112 recipes,
each base primitive with each line the moved tests write, with the top selected and without, and
230 of BevyCSharp's own test lines after a box. These are folded in:

- the tube primitive, `tube SIDES X Y Z RADIUS` and on, with BevyCSharp's four tests of it;
- the selection of edges by their ends, `select edge A B` and on;
- the boolean modifier, `modifier boolean` with a kind, a model's path or a solid's line and an
  `at`, a solid's line run and a model read once as it is added, so a missing one is refused there;
- `modifier add` before a modifier's line, BevyCSharp's form, read as the line alone, the module
  writing its own;
- `smooth F N`, a smooth of N passes, its line naming the count where it is more than one;
- IsClosed, CanUndo, CanRedo and LookOf by a slot's name, which BevyCSharp's binding reads;
- a selected face lying in a mirror's plane taken away, so a half closed there mirrored is closed,
  a mend whose test turns 3DEngine's assertion of the face kept inside and failed on 11 faces
  against 10 before it. It changes 46 of part 1's 4,004 recipes, each one mirroring a face in the
  plane, and the rest still give 3DEngine's results to the bit.

These are left, each with its reason:

- the chained returns and BevyCSharp's names, AddModifier, SlotTexture and the rest, which its
  binding maps onto the module's;
- a refusal joined by a colon, since STYLE.md has the comma;
- the selection BevyCSharp keeps or widens after a dissolve, a knife, a mirror and a spin, since
  the module selects what an operation made;
- the fill and the loop cut BevyCSharp refuses where nothing in the selection fits them, so every
  recipe 3DEngine saved reads and builds as it did;
- the bevel's corners where three beveled edges meet, a face spun, which the module sweeps closed
  and BevyCSharp leaves open, and the subdivision of a face of more than four corners, each a
  design of its own that the module keeps with 3DEngine's tests;
- a bevel of one segment written `bevel W 1`, the module reading BevyCSharp's `bevel W` as the
  same mesh;
- the knife's plane where no point is given, through the origin here and through the selection's
  middle in BevyCSharp, one line meaning two planes. No recipe file of the engines or the game holds
  a knife, so the module keeps 3DEngine's and BevyCSharp's binding writes the point where it means
  the middle;
- the last bits of a rotation's and a planar projection's numbers.

The budget's comment in ModifierTests says what the bound catches and the time it is measured
against. The strict build is clean and the tests pass, 141 where there were 133 (dotnet test
Modeling.Tests).

The commit carrying this line holds part 2's reading, 2026-11-18-item-3-mirror.webp, a closed half
mirrored before the mend and after it. The budget's comment in ModifierTests says what the bound
guards, and the figure it stands on is the stack's first build in a process, 135 to 151 ms over
three runs, timed round EditMeshBuild.Slots in a Debug build, the bound of 1,500 ms ten times that.
`.gitignore` holds Python's `__pycache__/`, which a run of build/textures.py leaves.

Decision 10's first two asks, in `2d259bf` and the commit carrying this line. The README's section
Binding it in your engine names `IModelFiles`, the one interface the module declares, how a mesh is
made with an engine's files and what the binding reads of the build and the looks, and the
conventions, meters, Y up and right-handed, counterclockwise triangles, the texture coordinates'
second running down a wall, degrees, a slot's color in sRGB and its emissive linear, paths of one
word, and a knife's, a mirror's or a spin's plane or center through the origin where a line names no
`at`, so BevyCSharp's binding writes the point. Its block is Modeling.Tests/BindingHost.cs between
Tools' two marker lines, an engine whose files are in memory, and BindingTests runs a recipe that
joins, imports, textures, saves and loads through it into meshes of its own a slot.

Tools' shared checks in the commit carrying this line, as Tools' README section A module's checks
gives them. The submodule stands at `modules/Tools` pinned at `f92df1c`, the tests' project
references it with the NeedsTools target, ModuleTests holds the four facts with Library
`3DEngine.Modeling`, Root and Host `Modeling.Tests/BindingHost.cs`, and the workflow's checkout takes
`submodules: true`. On the first run all four passed, the library taking no engine, window or
graphics API, no public name or its documentation naming an engine, every public type named in the
README and the README's block standing in the host word for word, so they found no fault. The module
held no copy of those four to remove; SourceTests stays, since Tools holds the comment's width as a
script and not as one of the facts. The library's deps.json names nothing of Tools'. The strict
build is clean and the tests pass, 147 where there were 143 (dotnet test Modeling.Tests).

The devlog in the commit carrying this line, written by `python3 -I modules/Tools/scripts/devlog.py
. Modeling` from the Tools submodule at `f92df1c`, holds 3 days, 2026-10-31, 2026-11-01 and
2026-11-18, with 4 pictures and 3 items done, the mirror's reading of 2026-11-18 the entry it gains.
The README's first line links it, and AGENTS.md's row names the command and when it runs.

The two albedos rebuilt in the commit carrying this line by Materials' encoder at `4f5f060`, which
holds the mend of `7e69a59`, with `python3 -I -B ../Materials/scripts/textures.py
Modeling/Textures/textures.json`. The carpet's albedo differs in 622 bytes and the wallpaper's in
3,306 (cmp -l), at 45.3 and 46.2 dB, and no other file changed, the normals, the surfaces and the
manifest the same byte for byte.

Item 3's third part, the glTF exchange, in the commit carrying this line. 3DEngine's GltfWriter
moves as the module's, writing through the mesh's files, and `EditMesh.Export` takes the flat API's
export, a primitive and a material a slot a face is in, the look's sRGB color decoded to glTF's
linear factor, with the engine's prototype color by a slot's name as a parameter, since the two
engines' palettes differ, white where none is given. `IModelFiles` gains `WriteBytes`, a file of
bytes written whole, which the glTF's buffer beside it needs and the host and ModelFolder implement.
BevyCSharp's GltfReader moves as the module's, reading a `.gltf` with its buffers inside or beside it
or a `.glb` into a part a material, placed by the scene's nodes, so ModelFolder imports a glTF with
no engine. 3DEngine's ExchangeTests come with it, the engine's model reader's reading of the written
box taken by the module's reader, with `two-pieces.gltf`, and SlotTests' glTF test, with one more of
the prototype color. The strict build is clean and the tests pass, 155 where there were 147 (dotnet
test Modeling.Tests).

One difference with BevyCSharp found here and left until it is answered. BevyCSharp's import keeps
each material's look as a slot's, each face's own texture coordinates, and the smoothing its
normals give, where the module's keeps the material's name alone. Taking it widens `ModelPart` with
a look, texture coordinates and normals, a public shape both engines' readers fill, so it is
proposed here as a part of its own before part 4.

The cheatsheet in the commit carrying this line, as Tools' README section A module's cheatsheet
gives it, Tools pinned at `3720852`. ModuleTests holds the fifth fact beside the four, and
CHEATSHEET.md, written by `CHEATSHEET_WRITE=1 dotnet test Modeling.Tests` under a short opening
written by hand, lists 10 types and 180 members. The README's top row links it beside the devlog,
and AGENTS.md's documents table names it with its command. The first page held six members with no
summary, ModelFolder's five that took the interface's by `inheritdoc`, which the writer does not
follow, and SlotLook's constructor, which the compiler made; each has a summary of its own in the
source. The four primary constructors of MeshVertex, Bounds, ModelPart and ModelFolder carry no
comment on their lines, since the writer leaves out a summary that repeats its type's. The tests
pass, 156 where there were 155 (dotnet test Modeling.Tests).

The import's looks, texture coordinates and smoothing, the part agreed after part 3, in the commit
carrying this line. Its test, a cylinder exported with a slot's color, two maps, metalness,
roughness and an emission, its sides smoothed and its top's coordinates turned, and read back
through ModelFolder, failed before the change on the look's color the import left out.

`ModelPart` is widened once, with `Normals`, `Uvs` and `Look`, each null where a file holds none,
and the look is Materials' `MaterialDescription`, so no second material type is made. The library
takes Materials sibling first, `../../Materials` where it stands and the submodule
`modules/Materials` at `635dcf3` where none does, with a target that names the command where
neither is there; ModuleTests' reference fact allows `3DEngine.Materials`, and the README and
AGENTS.md say so by place. The module's glTF reader fills all three, a material's factors with
glTF's defaults, its emission times its strength, its alpha mode, cutoff and sides, and its
pictures by their paths from the file's folder in Materials' layouts. The import keeps a corner's
coordinates as its face's own, smooths a face whose corners' normals lean from its own by more
than a degree, joins two triangles only where their smoothing and their coordinates agree along
and across the edge, and gives a slot of the material's name the look where the slot gives none,
the color encoded back to sRGB to five places.

What an engine's own reader fills for its parts to import as these do: each part's positions in
the scene's space with three indices a triangle counterclockwise, the material's name, each
vertex's normal and texture coordinates where its file has them, the second coordinate running down
the picture, and the material as a `MaterialDescription`, its base color linear, its emission as
glTF's factor times its strength, and each picture's path as its `IModelFiles` finds it. A part it
leaves without them imports as before, its faces flat, with world-aligned coordinates and the
slot's own look. Materials names no unit for glTF's emission, which glTF holds unitless, so the
module carries it as it is into `Emissive`, which Materials gives in candelas a square meter; an
engine whose emissive light has those units scales it once.

The README's link row takes the full addresses. The strict build is clean and the tests pass, 157
where there were 156 (dotnet test Modeling.Tests).

The sweep for the rule that a module names no engine, in the commit carrying this line. `git grep -n
-P "BevyCSharp|\bBevy\b|3DEngine(?!\.[A-Z])" -- . ':!modules' ':!.github/REVIEW.md'
':!.github/NORM.md'` found 10 lines before it, in AGENTS.md's and the README's opening sentence, the
README's binding paragraph and texture section, ExchangeTests' fixture remark and three comments of
build/textures.py, and finds none after it. The binding paragraph's sentence on an engine of
entities and one of a flat API of handles is taken out, as a description of two engines' forms, and
its line on ModelFolder says it reads glTF. The texture sets' formats are named as the Vegetation
module's collection's. No sheet or caption named an engine. DEVLOG.md and CHEATSHEET.md were written
again and are the same, 3 days, 4 pictures and 3 items done, and 10 types. The tests pass, 157.

The cheatsheet written as one word, in the commit carrying this line, Tools pinned at `75d77f2`.
ModuleTests holds Tools' seven facts in the form its README gives, the fault given to
`Assert.True` whole, the two it adds among them, that no tracked text names an engine and that the
README opens with its link row, and both pass. `git grep -n -i "cheat sheet"` finds none, the page's
title, kept by hand, taking the one word, and CHEATSHEET.md is written again. The import's commit
carries `[Cheatsheet]` in the link row from the start, remade with the sweep's after it. The tests
pass, 159 where there were 157 (dotnet test Modeling.Tests).

The emission's unit in the commit carrying this line, Materials pinned at `79eb411`, which holds
`68a0a93`. The glTF reader writes a material's emission as glTF's factor times its strength with
`EmissiveUnit.Unitless`, and the import's test reads it so, which failed on `Luminance` before the
line was added. The README's conventions say the emission is unitless. The tests pass, 159.

Item 3's fourth part, the model commands, its first commit in the commit carrying this line.
`ModelCommands` runs a line over a `ModelStore` and an `IModelScene`, its `All` holding a
`ModelCommand` for each operation of the recipe and those that make, name, read, write, show and
forget a model, each with its name, a summary, its usage and what runs it, answering a
`CommandAnswer`, its text and a `CommandFailure` with its code where it refused. A line is split at
spaces and tabs with no quoting and its numbers read in the invariant culture, as the class's
remarks say. A usage is written in a grammar the class gives, and CommandUsageTests holds each to
its parser, every command's examples matching its usage and done on a fresh model, every word a
usage names taken by one of them, and a line a usage does not read refused by its command too.
ModelCommandsTests runs the house, a save and a load, every refusal by its code, a program's mesh
by its name, a model shown and forgotten through a scene that keeps what it was told, a density read
from its slot's texture and an export through the store's files. A line a command refuses makes no
model, and the modifier's refusal names `add` and `boolean`. The README's table names the six types
and CHEATSHEET.md is written again. The tests pass, 168 where there were 159 (dotnet test
Modeling.Tests).

Item 3's fourth part, its second commit in the commit carrying this line. The README's section
Binding it in your engine names `IModelScene` after `IModelFiles`, says how an engine's console
takes `ModelCommands.All`, gives the line's rule and the usage's grammar, and says the count of
commands by what the module holds, 57, 11 of the store and one for each of the recipe's 46
operations, which ModelCommandsTests holds to `ModelCommands.All` and to the recipe's operations.
The host gains a scene that draws a shown model again as its version moves and a console that takes
every command by its name and usage, which the binding block shows and BindingTests runs. With it
item 3 holds its four parts and the import's. The tests pass, 170 where there were 168 (dotnet test
Modeling.Tests).
