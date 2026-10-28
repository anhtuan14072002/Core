# Roguelite Toolkit Expansion Plan

## 1. Mục tiêu tổng thể

Mở rộng `RogueliteToolkit` theo hướng module độc lập, có thể sử dụng:

- Chỉ `Card`
- Chỉ `SkillTree`
- Hoặc cả `Card` và `SkillTree` cùng lúc

Hai hệ thống không phụ thuộc trực tiếp vào nhau.

Cả `Card` và `SkillTree` cùng dùng chung hệ thống `GameplayEffect`.

```text
RogueliteToolkit
│
├── Stats
├── Effects
├── Cards
└── SkillTree
```

Dependency:

```text
                  Stats
                    ▲
                    │
                 Effects
                ▲       ▲
                │       │
             Cards   SkillTree
```

Nguyên tắc:

```text
Cards không biết SkillTree tồn tại.
SkillTree không biết Cards tồn tại.
GameplayEffect không biết effect đến từ Card hay SkillTree.
Stats không biết modifier là permanent hay temporary.
```

---

# 2. Lifecycle của Card và Skill Tree

## Skill Tree

Skill Tree là permanent progression.

```text
Skill Tree
→ lưu trong Profile Save
→ tồn tại qua nhiều run
→ load game sẽ restore
→ không reset khi kết thúc run
```

Ví dụ:

```text
Damage Mastery Lv3
→ +30% Damage vĩnh viễn
```

Save:

```text
damage_mastery = 3
```

Không save trực tiếp final stat:

```text
damage = 130
```

Khi load:

```text
Load SkillTreeState
→ restore level
→ apply GameplayEffect
→ reconstruct StatModifier
```

## Card

Card là run progression.

```text
Card
→ chỉ tồn tại trong active run
→ được save nếu user thoát giữa chừng
→ restore khi user quay lại run
→ reset khi run kết thúc
```

Temporary nghĩa là:

```text
temporary theo lifetime của run
```

không có nghĩa là:

```text
không được save
```

### Suspend Run

```text
User quit game
User về Main Menu
Game bị đóng
```

Thì:

```text
Save ActiveRun
→ giữ CardCollection state
→ vào lại restore card
```

### End Run

```text
Win
Lose
Abandon
```

Thì:

```text
Clear CardCollection
Remove card modifiers
Delete ActiveRunSave
```

Skill Tree không bị ảnh hưởng.

---

# 3. Refactor Effect System

Hệ thống Effect hiện tại thuộc Card nên cần chuyển thành hệ thống chung.

## Rename

```text
CardEffect
→ GameplayEffect

CardEffectContext
→ GameplayEffectContext

StatCardEffect
→ StatGameplayEffect

OnStacksChanged
→ OnValueChanged

previousStacks
→ previousValue

currentStacks
→ currentValue
```

Folder:

```text
Effects/
├── GameplayEffect.cs
├── GameplayEffectContext.cs
├── EffectSource.cs
└── StatGameplayEffect.cs
```

---

# 4. GameplayEffect độc lập với Card

Không để `GameplayEffect` nhận `CardDefinition`.

API mục tiêu:

```csharp
public abstract class GameplayEffect
{
    public abstract void OnValueChanged(
        GameplayEffectContext context,
        EffectSource source,
        int effectIndex,
        int previousValue,
        int currentValue);
}
```

`previousValue/currentValue` có thể là:

```text
Card Stack
hoặc
Skill Level
```

Effect không cần biết nguồn progression thuộc hệ thống nào.

---

# 5. EffectSource

Tạo `EffectSource` để xác định nguồn effect và tạo ID deterministic.

```csharp
public readonly struct EffectSource
{
    public string Type { get; }
    public string Id { get; }

    public EffectSource(string type, string id)
    {
        Type = type;
        Id = id;
    }
}
```

Ví dụ Card:

```text
Type = card
Id = rapid_fire
```

Ví dụ Skill:

```text
Type = skill
Id = damage_mastery
```

Modifier ID:

```text
card:rapid_fire:effect:0
skill:damage_mastery:effect:0
```

Không dùng random GUID cho runtime modifier.

---

# 6. StatGameplayEffect

`StatGameplayEffect` dùng chung cho Card và Skill Tree.

Card:

```text
Rapid Fire
+10% Attack Speed mỗi stack
```

```text
0 → 1
1 → 2
2 → 3
```

Skill:

```text
Damage Mastery
+10% Damage mỗi level
```

```text
0 → 1
1 → 2
2 → 3
```

Cả hai đều gọi:

```text
GameplayEffect.OnValueChanged(...)
```

---

# 7. Refactor Card System

`CardDefinition`:

```text
List<CardEffect>
```

đổi thành:

```text
List<GameplayEffect>
```

`CardCollection` giữ trách nhiệm:

```text
Add
Remove
Stack
Clear
WriteSaveData
Restore
```

Khi stack thay đổi:

```text
CardCollection
→ GameplayEffect.OnValueChanged()
```

---

# 8. Verify Card sau refactor

Trước khi làm Skill Tree phải verify:

```text
Add Card
Remove Card
Stack Card
Clear Card
Stat Modifier
Custom GameplayEffect
WriteSaveData
Restore
Không duplicate modifier
StatChanged vẫn hoạt động
```

Chỉ tiếp tục khi Card stable.

---

# 9. Skill Tree Runtime Module

Structure:

```text
SkillTree/
├── Runtime/
│   ├── SkillTreeDefinition.cs
│   ├── SkillNodeDefinition.cs
│   ├── SkillConnectionDefinition.cs
│   ├── SkillTreeState.cs
│   ├── SkillRequirement.cs
│   ├── NodeUnlockedRequirement.cs
│   └── NodeLevelRequirement.cs
│
└── Editor/
    ├── SkillTreeAuthoring.cs
    ├── SkillNodeAuthoring.cs
    ├── SkillConnectionAuthoring.cs
    ├── SkillTreeAuthoringEditor.cs
    └── SkillTreeAuthoringWindow.cs
```

---

# 10. SkillTreeDefinition

`SkillTreeDefinition` là runtime config asset.

```text
SkillTreeDefinition
{
    Id
    Nodes
    Connections
}
```

Không chứa runtime state.

---

# 11. SkillNodeDefinition

Mỗi node cần:

```text
Id
DisplayName
Position
MaxLevel
Costs
IncomingRequirementMode
Effects
```

Ví dụ:

```text
Id = damage_mastery
MaxLevel = 3
Costs = [1, 1, 2]
IncomingRequirementMode = All
Effects = Damage +10% / level
```

Không lưu trong definition:

```text
CurrentLevel
IsPurchased
IsUnlocked
```

---

# 12. SkillTreeState

Runtime state quản lý:

```text
NodeId → CurrentLevel
```

API V1:

```csharp
GetLevel(node)
IsPurchased(node)
CanPurchase(node)
Purchase(node, context)
```

Sau V1 mới thêm:

```text
CanRefund
Refund
Reset
```

---

# 13. SkillConnectionDefinition

Connection runtime:

```csharp
SkillConnectionDefinition
{
    FromNodeId
    ToNodeId
    Requirement
}
```

Ví dụ:

```text
Damage I
   │
   │ Requires Level >= 2
   ▼
Damage II
```

Data:

```text
From = damage_01
To = damage_02
Requirement = NodeLevel >= 2
```

---

# 14. Requirement System

V1 chỉ cần:

```text
NodeUnlockedRequirement
NodeLevelRequirement
```

Base:

```csharp
public abstract class SkillRequirement
{
    public abstract bool IsMet(
        SkillTreeState state,
        GameplayEffectContext context);
}
```

Có thể mở rộng sau:

```text
PointsSpentRequirement
PlayerLevelRequirement
BossRequirement
CustomRequirement
```

---

# 15. Incoming RequirementMode

Một node có thể có nhiều connection đi vào.

```text
A ─────┐
       ▼
       C
       ▲
B ─────┘
```

Node `C` có:

```csharp
RequirementMode
{
    All,
    Any
}
```

`All`:

```text
A condition AND B condition
```

`Any`:

```text
A condition OR B condition
```

Không làm boolean expression tree phức tạp ở V1.

---

# 16. Purchase Skill

Flow:

```text
Player click Node
↓
SkillTreeState.CanPurchase()
↓
Check incoming requirements
↓
Check MaxLevel
↓
Check Cost
↓
Level +1
↓
GameplayEffect.OnValueChanged()
↓
Raise SkillChanged event
```

---

# 17. Skill Tree Save

Chỉ save state.

```text
damage_mastery = 3
attack_speed = 2
explosion = 1
```

Không save final modifiers.

Restore:

```text
Load SkillTree save
↓
Restore SkillTreeState
↓
Apply GameplayEffects
↓
Reconstruct StatModifiers
```

---

# 18. Save Architecture

```text
Save System
│
├── ProfileSave
│   └── SkillTree
│
└── ActiveRunSave
    └── Cards
```

---

# 19. Load Order

Khi resume:

```text
1. Create runtime StatSet

2. Load Profile Save

3. Restore SkillTree
   → apply permanent modifiers

4. Check ActiveRunSave

5. Nếu có active run:
   Restore Cards
   → apply run modifiers

6. Restore gameplay state
   HP / Wave / Level / Map...

7. Resume game
```

Không restore `CurrentHP` trước khi reconstruct `MaxHP`.

---

# 20. Run Lifecycle

## SuspendRun

```text
Save ActiveRun
Save CardCollection
Không reset card progression
```

## EndRun

```text
Clear CardCollection
Remove card modifiers
Delete ActiveRunSave
```

---

# 21. Mục tiêu Scene Authoring Tool

Skill Tree phải được thiết kế trực tiếp trong Unity Scene.

Designer/dev có thể:

```text
Create Node
↓
Kéo node đến vị trí mong muốn trong Scene
↓
Chọn node nguồn
↓
Chọn điều kiện connection
↓
Bật chế độ Connect
↓
Click node đích
↓
Tool tạo connection
↓
Preview đường nối ngay trong Scene
↓
Bake toàn bộ tree thành SkillTreeDefinition
```

Không nhập tọa độ X/Y bằng tay.

Không cần GraphView ở V1.

Scene chính là visual editor của Skill Tree.

---

# 22. SkillTreeAuthoring Root

Tạo GameObject root:

```text
SkillTreeAuthoring
```

Khuyến nghị sử dụng `RectTransform`.

Hierarchy:

```text
SkillTreeAuthoring
│
├── Connections
│
└── Nodes
    ├── Damage01
    ├── Damage02
    ├── Speed01
    └── Explosion
```

`SkillTreeAuthoring` chứa:

```text
Tree ID
Output SkillTreeDefinition
Node Prefab tùy chọn
Connection Prefab tùy chọn
Bake settings
```

Scene position dùng:

```csharp
RectTransform.anchoredPosition
```

Không dùng world position để bake.

---

# 23. SkillNodeAuthoring

Mỗi node Scene là một GameObject UI có:

```text
RectTransform
SkillNodeAuthoring
```

Inspector:

```text
Id
Display Name
Max Level
Costs
Incoming Requirement Mode
Gameplay Effects
```

Có thể có preview text/icon nhưng runtime data không phụ thuộc UI preview.

Designer kéo trực tiếp node trong Scene View để thay đổi layout.

---

# 24. Create Node Tool

Trong `SkillTreeAuthoringWindow` hoặc custom inspector có button:

```text
[Create Node]
```

Khi bấm:

```text
1. Tạo GameObject dưới Nodes root
2. Add RectTransform
3. Add SkillNodeAuthoring
4. Generate temporary unique ID nếu cần
5. Select node vừa tạo
6. Cho phép kéo node ngay trong Scene
```

Có thể có:

```text
[Duplicate Node]
[Delete Node]
```

Duplicate phải tạo ID mới, không copy nguyên ID.

---

# 25. Scene Connection Tool — Workflow chính

Tool phải support workflow sau:

```text
1. Select Node A

2. Trong Skill Tree Tool chọn:
   Requirement Type

3. Nếu requirement cần parameter:
   nhập parameter

4. Bấm:
   [Start Connect]

5. Tool chuyển sang Connect Mode

6. Click Node B trong Scene

7. Tool tạo:
   SkillConnectionAuthoring

8. Tạo preview line A → B

9. Thoát Connect Mode
```

Ví dụ Inspector/Window:

```text
Selected Source:
Damage_01

Requirement:
[ Node Unlocked ▼ ]

Required Level:
[ 1 ]

[ Start Connect ]

[ Cancel Connect ]
```

Nếu chọn `NodeLevelRequirement`:

```text
Requirement:
Node Level

Required Level:
2
```

Sau đó click `Damage_02`:

```text
Damage_01 ── Lv >= 2 ──→ Damage_02
```

---

# 26. Connect Mode State

`SkillTreeAuthoringWindow` cần giữ editor-only state:

```text
SelectedSourceNode
SelectedRequirementType
RequirementParameters
IsConnecting
```

Không lưu editor selection state vào runtime data.

Khi:

```text
IsConnecting = true
```

Scene Tool chờ click một `SkillNodeAuthoring`.

Click node hợp lệ:

```text
Create Connection
```

Click vùng trống:

```text
không làm gì
```

Escape hoặc `Cancel Connect`:

```text
cancel connect mode
```

---

# 27. Chọn điều kiện trước khi nối

Requirement được chọn trước khi tạo connection.

V1:

```text
Node Unlocked
Node Level >= X
```

Ví dụ:

```text
Selected Source = Damage01

Condition:
Node Level >= 2

Start Connect

Click Explosion
```

Kết quả:

```text
Damage01 ── Level >= 2 ──→ Explosion
```

Connection lưu condition độc lập.

Không hard-code condition dựa trên loại node.

---

# 28. SkillConnectionAuthoring

Scene connection có component/editor data:

```text
SkillConnectionAuthoring
{
    From
    To
    Requirement
}
```

Editor có thể giữ trực tiếp reference:

```csharp
SkillNodeAuthoring From;
SkillNodeAuthoring To;
```

Nhưng khi Bake phải chuyển thành:

```text
FromNodeId
ToNodeId
```

Runtime không reference Scene GameObject.

---

# 29. ALL / ANY nằm ở Node đích

Condition nằm trên connection.

Cách combine nhiều incoming connection nằm ở node đích.

Ví dụ:

```text
A ── Lv >= 2 ──┐
               ▼
               C
               ▲
B ── Unlocked ──┘
```

`C`:

```text
Incoming Requirement Mode = ALL
```

nghĩa là:

```text
A >= Lv2
AND
B unlocked
```

Nếu:

```text
Incoming Requirement Mode = ANY
```

thì:

```text
A >= Lv2
OR
B unlocked
```

---

# 30. Preview đường nối trong Scene

Không dùng `LineRenderer`.

Dùng UI `Image` hoặc một editor-compatible connection view.

Connection cần:

```text
From RectTransform
To RectTransform
Thickness
```

Tính:

```csharp
Vector2 direction = to - from;
float distance = direction.magnitude;
float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
```

Set:

```text
anchoredPosition = midpoint
width = distance
rotation = angle
```

Sprite có thể chỉ là một ảnh thanh đơn giản.

---

# 31. Connection Preview phải auto update

Khi kéo node trong Scene:

```text
Node A position thay đổi
↓
Connection preview update ngay
```

Không yêu cầu bấm Refresh.

Editor connection renderer phải update trong Edit Mode.

Ví dụ dùng:

```text
OnValidate
ExecuteAlways
Editor update
hoặc custom Scene GUI
```

Ưu tiên implementation đơn giản và ổn định.

---

# 32. Arrow / Direction

Skill connection là directed:

```text
From → To
```

Preview nên thể hiện chiều.

V1 có thể dùng:

```text
Line Image
+
Arrow Head Image
```

Hoặc nếu chưa cần arrow visual thì ít nhất Inspector phải hiển thị rõ:

```text
From: Damage01
To: Damage02
```

Runtime logic luôn coi connection là một chiều.

---

# 33. Select Connection trong Scene

Connection phải có thể select để chỉnh sửa.

Khi select:

```text
From
To
Requirement Type
Required Level
```

Có button:

```text
[Delete Connection]
```

Có thể sửa requirement sau khi nối mà không cần delete/reconnect.

---

# 34. Duplicate Connection Validation

Không cho tạo duplicate:

```text
A → B
A → B
```

Nếu connection giống nhau đã tồn tại:

```text
Cancel creation
Show warning
```

V1 cũng không cần support nhiều connection A → B với nhiều condition riêng.

Một pair `From → To` chỉ có một connection.

---

# 35. Self Connection Validation

Không cho:

```text
A → A
```

Nếu user click chính source node:

```text
Reject
Show warning
Remain/exit connect mode tùy implementation
```

Khuyến nghị exit connect mode sau warning để tránh thao tác nhầm.

---

# 36. Cycle Validation

Skill Tree V1 nên phát hiện cycle:

```text
A → B
B → C
C → A
```

Ít nhất khi Bake phải report error.

Tốt hơn:

```text
check cycle ngay khi tạo connection
```

Nếu cycle không được support trong gameplay thì không cho Bake.

---

# 37. Missing Node Validation

Không cho Bake nếu:

```text
Connection.From == null
Connection.To == null
```

Hoặc node ID bị mất.

---

# 38. Duplicate Node ID Validation

Node ID phải stable và unique.

Không cho Bake:

```text
damage_01
damage_01
```

Tool phải highlight node lỗi.

---

# 39. Empty Node ID Validation

Không cho Bake node có:

```text
Id = ""
```

Có thể hỗ trợ button:

```text
[Generate ID]
```

Nhưng ID không được tự đổi mỗi Bake.

---

# 40. Node Layout

Position bake từ:

```text
RectTransform.anchoredPosition
```

Ví dụ Scene:

```text
[A]          [B]
       [C]
```

Baked definition lưu đúng position.

Runtime UI spawn lại:

```text
[A]          [B]
       [C]
```

Không auto-layout ở V1.

---

# 41. Bake Skill Tree

Trong Inspector/Window:

```text
[Bake Skill Tree]
```

Bake flow:

```text
Validate Tree
↓
Collect all SkillNodeAuthoring
↓
Collect all SkillConnectionAuthoring
↓
Convert scene references thành IDs
↓
Copy node positions
↓
Copy effects
↓
Copy requirements
↓
Create/update SkillTreeDefinition.asset
↓
Mark asset dirty
↓
Save assets
```

---

# 42. Bake Node Data

Mỗi authoring node bake:

```text
Id
DisplayName
Position
MaxLevel
Costs
IncomingRequirementMode
GameplayEffects
```

Không bake:

```text
GameObject reference
RectTransform reference
Editor preview object
Current selection state
```

---

# 43. Bake Connection Data

Mỗi authoring connection bake:

```text
FromNodeId
ToNodeId
Requirement
```

Không bake Scene reference.

---

# 44. Scene Authoring không phải runtime dependency

Runtime build không cần authoring Scene.

Runtime chỉ cần:

```text
SkillTreeDefinition.asset
```

Scene Authoring chỉ là editor tool.

Rule bắt buộc:

```text
SkillTree Runtime assembly
không reference
SkillTree Editor assembly
```

---

# 45. Runtime Skill Tree UI

Runtime nhận:

```text
SkillTreeDefinition
```

Flow:

```text
Spawn SkillNodeView cho từng node
↓
Set anchoredPosition từ Definition
↓
Map NodeId → SkillNodeView
↓
Spawn SkillConnectionView
↓
Lookup From/To views
↓
Draw connection
↓
Bind SkillTreeState
```

---

# 46. Runtime Node Visual State

V1:

```text
Locked
Available
Purchased
Maxed
```

State do runtime logic quyết định.

UI chỉ hiển thị.

---

# 47. Runtime Connection Visual State

V1:

```text
Locked
Available
Active
```

Connection View không chứa gameplay rule.

Nó query/bind state từ SkillTreeState.

---

# 48. Reuse Connection Renderer

Nếu hợp lý, dùng chung connection layout math giữa:

```text
Scene Preview
Runtime UI
```

Ví dụ tách:

```text
SkillConnectionGeometry
```

hoặc helper:

```text
UpdateConnectionRect(from, to, rect)
```

Không bắt buộc reuse MonoBehaviour nếu editor/runtime lifecycle khác nhau.

---

# 49. Save Data

## Profile Save

```text
SkillTree state
```

Ví dụ:

```text
damage_mastery = 3
attack_speed = 2
```

## Active Run Save

```text
Card stacks
Level ID
Current HP
Wave
Map progress
...
```

Toolkit chỉ chịu trách nhiệm serialize state của module mình.

---

# 50. Expected Behavior khi dùng cả Card và Skill Tree

Ví dụ:

```text
Base Damage = 100

Skill Tree:
+30% Damage

Card:
+20% Damage
```

Trong run:

```text
Damage = 150
```

User quit giữa run:

```text
Profile Save
→ Skill Tree Lv3

ActiveRun Save
→ Card stack
```

Vào lại:

```text
Restore Skill Tree
Restore Card
Damage = 150
```

End Run:

```text
Clear Card
Delete ActiveRunSave
Damage = 130
```

Skill Tree vẫn giữ.

---

# 51. Naming cuối cùng

Effect:

```text
CardEffect
→ GameplayEffect

CardEffectContext
→ GameplayEffectContext

StatCardEffect
→ StatGameplayEffect

OnStacksChanged
→ OnValueChanged
```

Skill Tree Runtime:

```text
SkillTreeData
→ SkillTreeDefinition

SkillData
→ SkillNodeDefinition

SkillLink
→ SkillConnectionDefinition

SkillRuntime
→ SkillTreeState

Condition
→ SkillRequirement

ConnectionMode
→ RequirementMode
```

Scene Editor:

```text
SkillTreeEditor
→ SkillTreeAuthoring

SkillNodeEditor
→ SkillNodeAuthoring

SkillLinkEditor
→ SkillConnectionAuthoring
```

Tool window:

```text
SkillTreeAuthoringWindow
```

Runtime UI:

```text
SkillTreeView
SkillNodeView
SkillConnectionView
```

---

# 52. Folder Structure

```text
RogueliteToolkit/
│
├── Stats/
│
├── Effects/
│   ├── GameplayEffect.cs
│   ├── GameplayEffectContext.cs
│   ├── EffectSource.cs
│   └── StatGameplayEffect.cs
│
├── Cards/
│   ├── CardDefinition.cs
│   ├── CardCollection.cs
│   ├── CardDatabase.cs
│   └── CardStackData.cs
│
└── SkillTree/
    │
    ├── Runtime/
    │   ├── SkillTreeDefinition.cs
    │   ├── SkillNodeDefinition.cs
    │   ├── SkillConnectionDefinition.cs
    │   ├── SkillTreeState.cs
    │   ├── SkillRequirement.cs
    │   ├── NodeUnlockedRequirement.cs
    │   └── NodeLevelRequirement.cs
    │
    ├── UI/
    │   ├── SkillTreeView.cs
    │   ├── SkillNodeView.cs
    │   └── SkillConnectionView.cs
    │
    └── Editor/
        ├── SkillTreeAuthoring.cs
        ├── SkillNodeAuthoring.cs
        ├── SkillConnectionAuthoring.cs
        ├── SkillTreeAuthoringEditor.cs
        └── SkillTreeAuthoringWindow.cs
```

---

# 53. Implementation Order

## Phase 1 — Refactor Effect

```text
1. Verify current Card behavior
2. CardEffect → GameplayEffect
3. CardEffectContext → GameplayEffectContext
4. StatCardEffect → StatGameplayEffect
5. OnStacksChanged → OnValueChanged
6. Add EffectSource
7. Deterministic modifier IDs
8. Update CardDefinition
9. Update CardCollection
10. Verify Card save/restore
```

## Phase 2 — Skill Tree Runtime

```text
11. SkillTreeDefinition
12. SkillNodeDefinition
13. SkillConnectionDefinition
14. SkillRequirement
15. NodeUnlockedRequirement
16. NodeLevelRequirement
17. RequirementMode All/Any
18. SkillTreeState
19. CanPurchase
20. Purchase
21. GameplayEffect integration
22. SkillTree save data
23. SkillTree Restore
24. Verify permanent modifiers
```

## Phase 3 — Scene Authoring Core

```text
25. SkillTreeAuthoring root
26. SkillNodeAuthoring
27. SkillConnectionAuthoring
28. SkillTreeAuthoringWindow
29. Create Node button
30. Delete/Duplicate Node
31. Node position authoring
```

## Phase 4 — Scene Connection Tool

```text
32. Source node selection
33. Requirement dropdown
34. Requirement parameter UI
35. Start Connect mode
36. Click target node in Scene
37. Create SkillConnectionAuthoring
38. Cancel Connect mode
39. Select/edit existing connection
40. Delete connection
41. Prevent self connection
42. Prevent duplicate connection
43. Validate cycle
```

## Phase 5 — Scene Connection Preview

```text
44. Connection Image renderer
45. Auto update line on node move
46. Direction/arrow preview
47. Reuse geometry logic where possible
```

## Phase 6 — Bake

```text
48. Validate duplicate node IDs
49. Validate empty IDs
50. Validate missing references
51. Validate cycles
52. Bake Nodes
53. Bake Connections
54. Convert references to stable IDs
55. Create/update SkillTreeDefinition.asset
```

## Phase 7 — Runtime UI

```text
56. SkillTreeView
57. SkillNodeView
58. SkillConnectionView
59. Spawn nodes
60. Restore positions
61. Spawn connections
62. Locked state
63. Available state
64. Purchased state
65. Maxed state
66. Refresh on SkillTreeState changed
```

---

# 54. V1 Scope

V1 phải support:

```text
✓ Existing Card system still works

✓ Card stack

✓ Card active-run save/restore

✓ Permanent Skill Tree progression

✓ Multi-level nodes

✓ Skill cost

✓ GameplayEffect shared by Card and Skill Tree

✓ StatGameplayEffect shared

✓ Custom GameplayEffect

✓ Scene-based node placement

✓ Create Node tool

✓ Select source node

✓ Choose requirement before connecting

✓ Connect Node A → Node B by clicking in Scene

✓ Node Unlocked requirement

✓ Node Level >= X requirement

✓ ALL / ANY incoming mode

✓ Editable existing connections

✓ Scene connection preview

✓ Auto update connection preview when moving nodes

✓ Prevent duplicate/self connections

✓ Validate cycles

✓ Bake Scene → SkillTreeDefinition.asset

✓ Runtime UI mirrors Scene layout

✓ Skill Tree save/restore

✓ Deterministic effect/modifier IDs
```

Không làm ở V1:

```text
✗ Refund / Respec
✗ Exclusive branch
✗ Hidden nodes
✗ Auto unlock
✗ Boss requirement
✗ Player Level requirement
✗ Cross-tree requirement
✗ Complex boolean expression tree
✗ Auto layout
✗ GraphView editor
```

---

# 55. Architectural Rules

Bắt buộc giữ:

```text
StatDefinition không chứa runtime values.

SkillTreeDefinition không chứa runtime state.

CardDefinition không chứa runtime stack.

Scene Authoring chỉ là editor-time tool.

Runtime SkillTree không reference Scene GameObjects.

GameplayEffect không phụ thuộc CardDefinition.

GameplayEffect không phụ thuộc SkillNodeDefinition để quyết định lifecycle.

Cards không reference SkillTree.

SkillTree không reference Cards.

Skill Tree progression được save trong Profile Save.

Card progression được save trong Active Run Save.

Run bị suspend thì card được restore.

Run kết thúc thì card bị clear.

Save chỉ lưu state.

Runtime modifiers được reconstruct khi Restore.
```

Mục tiêu cuối cùng là giữ `RogueliteToolkit` game-agnostic, có thể export sang project Unity khác mà không phụ thuộc player, weapon, mining, UI, save system hoặc singleton của game hiện tại.
