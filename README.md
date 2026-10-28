# Tuna Packages

Each package has one responsibility. Keep reusable gameplay code separate from
UI, persistence, data import and Editor extensions.

| Package | Purpose | Main folders |
| --- | --- | --- |
| `com.tuna.core` | Lifecycle, managers, signals, shared utilities | `Runtime`, `Editor` |
| `com.tuna.ui` | uGUI controls, layouts, menus, loading and tween helpers | `Runtime`, `Plugins/ParticleImage` |
| `com.tuna.framework` | Roguelite stats, effects, cards, equipment and skill trees | `Runtime`, `Editor`, `Tests`, `Samples~`, `Documentation~` |
| `com.tuna.save` | Local persistence with Easy Save 3 | `Plugins/Easy Save 3` |
| `com.tuna.tools` | Google Sheets/CSV import and runtime config lookup | `Runtime/Sheets`, `Editor/Sheets` |
| `com.tuna.haptics` | Mobile haptics with Nice Vibrations | `Plugins/NiceVibrations` |
| `com.tuna.v` | vHierarchy and vTabs Editor extensions | `Editor`, `Plugins` |
| `com.tuna.package-installer` | Package installation window | `Editor` |

## Layout conventions

- `Runtime`: code and assets used by the game.
- `Editor`: Editor tools; their assemblies target Editor only.
- `Plugins`: bundled third-party assets. Preserve vendor names and assembly IDs.
- `Tests`: test assemblies, separate from runtime code.
- `Samples~` and `Documentation~`: opt-in examples and documentation, excluded
  from normal Unity import.
- Move Unity assets together with their `.meta` files.

UI folders are grouped by purpose: `Buttons`, `Camera`, `Diagnostics`, `Layout`,
`Loading`, `Menu`, `Popups`, `Prefabs`, `Text` and `Tween`.
Core utilities remain under `Runtime/Utils` as agreed.

## Package rename migration

| Previous package | Current package |
| --- | --- |
| `com.tuna.comon` | `com.tuna.haptics` |
| `com.tuna.data` | `com.tuna.save` |
| `com.tuna.custom` | Merged into `com.tuna.core/Runtime/Utils` and `Editor/Utils` |

Update both the package ID and its local/Git subfolder path in consuming projects.
The sibling `UnityCore` project uses local references to these packages, including
Framework, so it validates the source in this checkout.

Existing C# namespaces, class names, vendor assemblies and legacy Tuna assembly
IDs are retained. Some assembly filenames therefore still use the previous
package names. Empty wrapper assemblies are also retained for compatibility;
new scripts should go into the folder for their actual responsibility.

## Existing prerequisites

Tuna UI uses Tuna Core, uGUI/TextMeshPro, PrimeTween and UniTask. Its bundled
ParticleImage also references Burst, Collections and Mathematics. Install those
prerequisites in the consuming project; the current manifests do not declare
every external dependency. UniTask currently uses a direct Git entry in the
UnityCore manifest.

Gameplay Framework uses uGUI for skill-tree UI and authoring. Haptics, Save and
V bundle their vendor implementations. Tools has no third-party
runtime dependency.

`Samples~/Legacy/Game.cs` is an old bootstrap reference, not a ready-to-run
sample: it expects project-owned `GameConfig` and `GameData` classes.
