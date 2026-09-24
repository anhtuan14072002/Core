# Equipment

Module equipment của RogueliteToolkit, cùng cấp với Cards, SkillTree và Stats.
Runtime không phụ thuộc UI hay code của game. UI uGUI nằm trong assembly riêng.

## Cấu hình

Tạo asset tại **Create > Roguelite Toolkit > Equipment**:

1. **Type**: mỗi loại một asset, ví dụ Drill, Engine, Armor. Item và slot phải tham chiếu cùng asset type.
2. **Definition**: ID ổn định, tên, mô tả, icon, type và Stat Effects. Dùng chính `StatGameplayEffect` của framework: chọn Stat, Operation và Value Per Stack. Khi lắp, effect nhận giá trị 1; khi tháo nhận 0. Để Gains Per Level trống nếu chỉ cần một giá trị cố định. Theo quy ước Stat hiện tại, `AddPercent = 15` là +15%.
3. **Slot Config**: danh sách slot có ID duy nhất và type. Số phần tử chính là số slot. Hai slot `drill_left` và `drill_right` có thể cùng nhận type Drill.
4. **Database**: danh sách các Definition dùng để khôi phục save theo ID.

Không đổi ID hoặc sửa cấu hình asset trong khi state đang hoạt động. Mỗi nhân vật/đối tượng có một `EquipmentState` trên StatSet của nó; mọi UI của đối tượng đó bind cùng state.

## Khởi tạo trong luồng hiện có của game

Gọi sau khi StatSet đã Awake (ví dụ trong Start của code khởi tạo game). Dùng cùng `GameplayEffectContext` đang phục vụ Card/Skill Tree:

```csharp
EquipmentState equipment = new(slotConfig, context);
// Bind your project view to this state.

EquipmentInstance drill = equipment.Add(drillDefinition);
equipment.Equip(drill, "drill_left");
equipment.Unequip(drill);
```

Context phải đăng ký StatSet: `new GameplayEffectContext().Register(stats)`.

`Add` sinh instance ID riêng, hoặc nhận ID ổn định từ save. Hai món cùng Definition vẫn là hai instance độc lập. Bag là những phần tử `Items` có `IsEquipped == false`, không có giới hạn sức chứa trong phiên bản này.

- Bag → slot đúng type: lắp và áp dụng stat.
- Slot đang có đồ: món cũ về bag.
- Slot → slot cùng type: chuyển món; không cộng lại modifier. Nếu slot đích có đồ, món đó về bag.
- Slot → bag: gỡ đúng modifier của món đó.
- Sai type, sai slot, instance không thuộc state: trả `false`, không đổi dữ liệu.
- `Remove(item)` xóa món khỏi sở hữu và gỡ effect nếu đang lắp.

Nguồn effect có dạng `equipment:<instanceId>`, độc lập với Card/Skill Tree. Base stat không bị chỉnh sửa. Không gọi `StatSet.Rebuild()` khi các module đang có effect; phương thức đó xóa toàn bộ modifier của StatSet.

## Save / restore

```csharp
List<EquipmentSaveData> savedItems = new();
equipment.WriteSaveData(savedItems);
// Lưu savedItems qua hệ thống persistence hiện tại của game.

EquipmentState restored = new(slotConfig, context);
restored.Restore(savedItems, equipmentDatabase);
// Bind your project view to restored.
```

Restore cần state rỗng và StatSet chưa mang modifier của một EquipmentState khác. Khôi phục lúc tạo nhân vật/session, không giữ đồng thời state cũ và state mới trên cùng StatSet. Dữ liệu save được kiểm tra trước khi thay đổi: ID trùng, Definition không tồn tại, slot sai type hoặc hai món cùng slot sẽ báo lỗi, không âm thầm bỏ mất món. Nếu đổi ID/type/slot giữa các phiên bản game, migrate save trong lớp persistence trước khi Restore.

## Kiểm tra

Chạy **Tools > Roguelite Toolkit > Validate Equipment** trong Editor. Kiểm tra equip sai type, thay/chuyển món, hai món giống nhau, instance của state khác, flat/percent, không ảnh hưởng Card/Skill và save/restore lỗi.

## Port to another project

1. Copy RogueliteToolkit with its Stats/Effects dependencies and assembly definitions.
2. Create game-owned stat, type, item, slot config and database assets. Keep specific stat mappings and balance values in the project.
3. Project bootstrap creates StatSet and GameplayEffectContext, then one EquipmentState per owner. This is the public equipment API; no extra manager is needed.
4. The project view reads Items, Config.Slots and GetEquipped(slotId), subscribes to Changed, and calls Equip/Unequip. Unsubscribe when unbinding. Read final numbers through StatSet.GetValue(statDefinition).
5. Framework automatically applies/removes modifiers on equip, replacement and removal. Views must not add/subtract stats themselves. AddPercent uses 15 for 15%.
6. Connect WriteSaveData/Restore to the project save system. Implement UI and gameplay-specific integration in the project.

Dependency direction: project UI/bootstrap/gameplay -> framework Equipment -> framework Stats/Effects. Equipment contains no project scripts, scenes, prefabs or UI assembly references.
