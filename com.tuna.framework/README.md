# Roguelite Toolkit

Standalone, game-agnostic Unity modules for stats, gameplay effects, run-scoped cards,
and profile-scoped skill trees.

```text
Stats <- Effects <- Cards
                <- SkillTree
```

Cards and SkillTree never reference each other. Effects receive an `EffectSource`
(`card:<id>` or `skill:<id>`) and an integer value, so the same effect supports both
card stacks and skill levels.

## Effects and stats

`GameplayEffectContext` is a small service registry owned by the consuming game.
`StatGameplayEffect` retrieves a `StatSet` from it and creates deterministic modifier
IDs such as `card:rapid_fire:effect:0` and `skill:damage_mastery:effect:0`.

```csharp
GameplayEffectContext context = new GameplayEffectContext()
    .Register(playerStatSet);
```

Stat values use:

```text
(base + flat) * (1 + totalPercent / 100) * productOfMultipliers
```

The highest-priority override wins, then the `StatDefinition` limits are applied.
Never store runtime values in a `StatDefinition` asset.

## Cards (active-run state)

`CardDefinition` stores configuration and effects. `CardCollection` owns stack state
and supports `Add`, `Remove`, `Clear`, `WriteSaveData`, and `Restore`.

```csharp
CardCollection cards = new();
cards.Add(selectedCard, context);

List<CardStackData> activeRunCards = new();
cards.WriteSaveData(activeRunCards); // suspend run

cards.Clear(cardDatabase, context);  // end run; removes effects
```

Save `CardStackData` inside the game's ActiveRun save. Restoring calls each effect
from `0 -> saved stacks`, reconstructing modifiers rather than saving final stats.

## Skill tree (profile state)

`SkillTreeDefinition` contains nodes, connections, positions, requirements, costs,
and effects. `SkillTreeState` contains only runtime levels.

For paid nodes, register an implementation of `ISkillPointWallet` in the context:

```csharp
GameplayEffectContext context = new GameplayEffectContext()
    .Register(playerStatSet)
    .Register<ISkillPointWallet>(profileCurrency);

SkillTreeState skills = new(skillTreeDefinition);
if (skills.CanPurchase(node, context))
    skills.Purchase(node, context);
```

Connections support `NodeUnlockedRequirement` and `NodeLevelRequirement`. Each target
node combines incoming connections with `RequirementMode.All` or `RequirementMode.Any`.
Save levels with `WriteSaveData`; restore a new, empty state with `Restore`.

## Scene authoring

1. Open **Tools > Roguelite Toolkit > Skill Tree Authoring**.
2. Create an authoring root, then use **Create Node**. Drag node cards directly on the
   graph grid; their positions synchronize to the scene RectTransforms. Duplicate
   creates a fresh stable ID, and deleting a node also deletes its connections.
3. Choose `Node Unlocked` or `Minimum Level` in the **New Edge** toolbar control.
4. Drag from node A's output port to node B's input port. Select an edge to inspect its
   scene connection data, or press Delete to remove nodes/edges.
5. Press **Bake Skill Tree** to create or update a `SkillTreeDefinition` asset.

Each graph node contains inline identity, progression, cost, incoming-mode, stat-effect,
and custom-effect fields. Right-click the canvas to create/duplicate/delete, frame,
refresh, or bake. The corner minimap previews the full tree; mouse-wheel/toolbar zoom,
panning, window resizing, and per-node resize are supported.

Bake validates tree/node IDs, per-level costs, duplicate IDs/connections, missing
references, self-connections, cycles, and references outside the current tree. Self,
duplicate, and cyclic connections are also rejected immediately during Scene authoring.
Runtime data stores anchored positions and IDs only; it never references scene
GameObjects.

## Runtime UI

Add `SkillTreeView`, assign the definition, node/connection roots and prefabs, then
call `Bind(existingState, context)` or `CreateState(context, profileSave)`. The view
spawns the baked layout and refreshes `Locked`, `Available`, `Purchased`, and `Maxed`
node states whenever `SkillTreeState.SkillChanged` fires.

## Save/load order

1. Create runtime `StatSet` objects.
2. Load profile data and restore `SkillTreeState` (permanent modifiers).
3. Load ActiveRun data and restore `CardCollection` (run modifiers).
4. Restore HP, wave, map, and the remaining gameplay state.

On suspend, save cards without clearing them. On win, loss, or abandon, clear the card
collection and delete the ActiveRun save. Skill tree levels remain in Profile save.
