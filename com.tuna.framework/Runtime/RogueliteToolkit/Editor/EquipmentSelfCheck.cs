using System;
using System.Collections.Generic;
using System.Reflection;
using RogueliteToolkit.Cards;
using RogueliteToolkit.Effects;
using RogueliteToolkit.Equipment;
using RogueliteToolkit.Stats;
using UnityEditor;
using UnityEngine;

namespace RogueliteToolkit.Editor
{
    public static class EquipmentSelfCheck
    {
        [MenuItem("Tools/Roguelite Toolkit/Validate Equipment")]
        public static void Run()
        {
            List<UnityEngine.Object> assets = new();
            GameObject owner = new("Equipment Check");
            try
            {
                StatSet stats = owner.AddComponent<StatSet>();
                GameplayEffectContext context = new GameplayEffectContext().Register(stats);
                StatDefinition damage = Create<StatDefinition>(assets);
                Set(damage, "_id", "damage");
                stats.SetBaseValue(damage, 100f);
                EquipmentType drill = Create<EquipmentType>(assets);
                EquipmentType engine = Create<EquipmentType>(assets);
                EquipmentSlotConfig config = Create<EquipmentSlotConfig>(assets);
                Set(config, "_slots", new[]
                {
                    new EquipmentSlotDefinition("left", drill),
                    new EquipmentSlotDefinition("right", drill),
                    new EquipmentSlotDefinition("engine", engine)
                });
                StatGameplayEffect flat = Effect(damage, StatModifierOperation.AddFlat, 10f);
                StatGameplayEffect percent = Effect(damage, StatModifierOperation.AddPercent, 20f);
                EquipmentDefinition item = Create<EquipmentDefinition>(assets);
                Set(item, "_id", "drill_a");
                Set(item, "_type", drill);
                Set(item, "_statEffects", new[] { flat });
                EquipmentDefinition motor = Create<EquipmentDefinition>(assets);
                Set(motor, "_id", "engine_a");
                Set(motor, "_type", engine);
                Set(motor, "_statEffects", new[] { percent });
                EquipmentDatabase database = Create<EquipmentDatabase>(assets);
                Set(database, "_items", new[] { item, motor });
                CardDefinition card = Create<CardDefinition>(assets);
                Set(card, "_id", "a");
                Set(card, "_statEffects", new[] { flat });
                CardCollection cards = new();
                cards.Add(card, context);
                flat.OnValueChanged(context, new EffectSource("skill", "a"), 0, 0, 1);

                EquipmentState state = new(config, context);
                EquipmentInstance a = state.Add(item, "a");
                EquipmentInstance b = state.Add(item, "b");
                EquipmentInstance c = state.Add(motor, "c");
                Require(!state.Equip(a, "engine") && !state.Equip(a, "missing"), "Invalid slot accepted.");
                Require(!a.IsEquipped && stats.GetValue(damage) == 120f, "Invalid equip changed stats.");
                Require(state.Equip(a, "left") && state.Equip(b, "right"), "Equip failed.");
                Require(stats.GetValue(damage) == 140f, "Identical items did not stack independently.");
                Require(!state.Equip(a, "left") && stats.GetValue(damage) == 140f, "Repeated equip duplicated stats.");
                Require(state.Equip(a, "right"), "Move to occupied slot failed.");
                Require(state.GetEquipped("left") == null && !b.IsEquipped && stats.GetValue(damage) == 130f,
                    "Moving between slots must return the displaced item to the bag.");
                Require(state.Equip(b, "right") && !a.IsEquipped && stats.GetValue(damage) == 130f,
                    "Replacement duplicated or lost effects.");
                Require(state.Unequip(b) && stats.GetValue(damage) == 120f,
                    "Unequip removed a card or skill effect with the same source ID.");
                Require(!state.Unequip(b), "Unequipping a bag item succeeded.");

                EquipmentState foreign = new(config, context);
                EquipmentInstance other = foreign.Add(item, "b");
                Require(!state.Equip(other, "left") && !state.Remove(other), "Foreign instance accepted.");
                ExpectFailure(() => state.Add(item, " b "));
                Require(state.Equip(a, "left") && state.Equip(c, "engine"), "Save setup failed.");
                Require(Mathf.Approximately(stats.GetValue(damage), 156f), "Percent equipment effect failed.");
                List<EquipmentSaveData> save = new();
                state.WriteSaveData(save);
                Require(state.Remove(a) && state.Remove(c) && state.Remove(b), "Remove failed.");
                Require(stats.GetValue(damage) == 120f, "Remove did not clean up equipment effects.");
                state.Restore(save, database);
                Require(state.Items.Count == 3 && state.GetEquipped("left").Id == "a" &&
                    state.GetEquipped("engine").Id == "c" && Mathf.Approximately(stats.GetValue(damage), 156f),
                    "Save restore lost ownership, slots, or stats.");
                ExpectFailure(() => state.Restore(save, database));
                while (state.Items.Count > 0)
                    state.Remove(state.Items[state.Items.Count - 1]);
                ExpectFailure(() => state.Restore(new[]
                {
                    new EquipmentSaveData("a", "drill_a", "left"),
                    new EquipmentSaveData("b", "drill_a", "left")
                }, database));
                Require(state.Items.Count == 0 && stats.GetValue(damage) == 120f,
                    "Invalid save partially mutated state.");
                ExpectFailure(() => state.Restore(new[] { new EquipmentSaveData("a", "missing", null) }, database));
                ExpectFailure(() => state.Restore(new[] { new EquipmentSaveData("a", "drill_a", "engine") }, database));
                cards.Remove(card, context);
                flat.OnValueChanged(context, new EffectSource("skill", "a"), 0, 1, 0);
                Require(stats.GetValue(damage) == 100f, "Base stat changed.");
                Debug.Log("EQUIPMENT_CHECK_PASS: types, replacement, movement, instance ownership, card/skill isolation, flat/percent stats, removal, save/restore, invalid saves.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                for (int i = assets.Count - 1; i >= 0; i--)
                    UnityEngine.Object.DestroyImmediate(assets[i]);
            }
        }

        private static T Create<T>(List<UnityEngine.Object> assets) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            assets.Add(asset);
            return asset;
        }

        private static StatGameplayEffect Effect(StatDefinition stat, StatModifierOperation operation, float value)
        {
            StatGameplayEffect effect = new();
            Set(effect, "_stat", stat);
            Set(effect, "_operation", operation);
            Set(effect, "_valuePerStack", value);
            return effect;
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void ExpectFailure(Action action)
        {
            try { action(); }
            catch (ArgumentException) { return; }
            catch (InvalidOperationException) { return; }
            throw new Exception("Expected invalid equipment operation to fail.");
        }
    }
}
