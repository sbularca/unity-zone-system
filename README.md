# Jovian Zone System

A polygon-based zone system for defining map regions with encounter difficulty, modifiers, and safe areas. No physics engine required.

## Package Structure

```
Packages/com.jovian.zonesystem/
├── Runtime/
│   ├── ZoneTypes.cs           ← Enums (ZoneRole, ZoneShape, DifficultyTier), ZoneContext struct
│   ├── ZoneData.cs            ← ScriptableObject: per-zone config + polygon
│   ├── ZoneInstance.cs        ← MonoBehaviour: scene object, owns polygon + bounds cache
│   ├── ZonesObjectHolder.cs   ← Scene manager: registers zones, holds map plane
│   ├── ZoneSystemApi.cs       ← Query API: resolve zones at world positions
│   ├── ZoneResolver.cs        ← Pure logic: overlapping zones → ZoneContext
│   ├── MapPlane.cs            ← MapPlane enum + projection/unprojection utilities
│   ├── PolygonUtils.cs        ← Pure math: point-in-polygon, centroid, AABB, triangulation
│   ├── ShapeFactory.cs        ← Default shape generation (square, circle, polygon)
│   └── ZoneExporter.cs        ← Serialization to JSON
├── Editor/
│   ├── ZoneEditorWindow.cs    ← Main editor window (Window → Zone System → Zone Editor)
│   ├── ZoneEditorSettings.cs  ← Configurable settings: folder path, role colors
│   ├── ZoneInstanceEditor.cs  ← Custom inspector + scene handles for shape editing
│   └── ZoneDataEditor.cs      ← Role-aware ZoneData inspector
└── Documentation~/
    └── index.html             ← Full HTML documentation
```

## Quick Start

1. Add the package to your project (local package in `Packages/`).
2. Create a **ZonesObjectHolder** GameObject and set **Map Plane** to match your map (e.g. `XZ`).
3. Open **Window → Zone System → Zone Editor**.
4. Click **Create New Zone**, set a name and shape, then click **Create & Edit**.
5. Edit all zone data fields in the editor, then click **Save Zone**.
6. Use scene handles to adjust the polygon shape.

## Key Features

- **Three zone roles**: Base (encounter table + difficulty), Modifier (multiplicative stacking), Override (safe zones, story events)
- **Visual polygon editing**: Drag vertices, Ctrl+Click to insert, Shift+Click to delete, Esc to stop
- **Concave polygon support**: Ear-clipping triangulation for correct rendering of any shape
- **Multi-plane support**: XY, XZ, or YZ — one setting controls everything
- **No physics dependency**: Pure math ray-casting with AABB pre-rejection
- **Save workflow**: Create → Edit → Save with duplicate ID/name validation
- **Role-based colors**: Configured in ZoneEditorSettings, auto-applied on role change
- **Zone duplication**: Independent copies with unique IDs and assets
- **JSON export**: For runtime loading or external tools
- **UPM package**: Standard Unity Package Manager layout with Runtime and Editor assemblies

## Menu Items

| Menu Path | Description |
|-----------|-------------|
| Window → Zone System → Zone Editor | Main editor window |
| Window → Zone System → Settings | Select or create ZoneEditorSettings asset |
| Window → Zone System → Documentation | Open HTML documentation |

## Runtime API

```csharp
ZoneSystemApi api = new ZoneSystemApi(zonesObjectHolder);

// Query zone at a world position
ZoneContext ctx = api.QueryZone(partyWorldPosition);
if(!ctx.isSafe && Random.value < ctx.finalEncounterChance)
    TriggerEncounter(ctx.encounterTableId, ctx.finalDifficultyTier);

// Quick safe-zone check
if(api.IsInSafeZone(partyWorldPosition))
    return;

// Raw overlapping zones (sorted by priority)
List<ZoneData> zones = api.GetOverlappingZones(partyWorldPosition);
```

## Documentation

Full documentation is available at `Documentation~/index.html`. Open it via **Window → Zone System → Documentation**.
