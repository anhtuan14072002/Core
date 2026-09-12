using RogueliteToolkit.Cards;
using RogueliteToolkit.Effects;
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(CardDefinition))]
[CanEditMultipleObjects]
sealed class CardDefinitionEditor : Editor
{
    private const float IconSize = 96f;
    private const float DescriptionHeight = 80f;

    private SerializedProperty _id;
    private SerializedProperty _displayName;
    private SerializedProperty _description;
    private SerializedProperty _icon;
    private SerializedProperty _rarity;
    private SerializedProperty _weight;
    private SerializedProperty _maxStacks;
    private SerializedProperty _stats;
    private SerializedProperty _effects;
    private ReorderableList _statList;
    private ReorderableList _effectList;

    private void OnEnable()
    {
        _id = serializedObject.FindProperty("_id");
        _displayName = serializedObject.FindProperty("_displayName");
        _description = serializedObject.FindProperty("_description");
        _icon = serializedObject.FindProperty("_icon");
        _rarity = serializedObject.FindProperty("_rarity");
        _weight = serializedObject.FindProperty("_weight");
        _maxStacks = serializedObject.FindProperty("_maxStacks");
        _stats = serializedObject.FindProperty("_statEffects");
        _effects = serializedObject.FindProperty("_effects");

        _statList = new ReorderableList(
            serializedObject, _stats, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Stats"),
            drawElementCallback = DrawStat,
            elementHeightCallback = _ =>
                (EditorGUIUtility.singleLineHeight +
                 EditorGUIUtility.standardVerticalSpacing) * 4f + 4f
        };

        _effectList = new ReorderableList(
            serializedObject, _effects, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Effects"),
            drawElementCallback = DrawEffect,
            elementHeightCallback = GetEffectHeight,
            onAddDropdownCallback = (rect, _) => ShowTypeMenu(rect, -1)
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawIdentity();
        DrawDescription();
        EditorGUILayout.Space();
        _statList.DoLayoutList();
        EditorGUILayout.Space();
        _effectList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawIdentity()
    {
        EditorGUILayout.BeginHorizontal();
        Rect iconRect = GUILayoutUtility.GetRect(
            IconSize, IconSize, GUILayout.Width(IconSize), GUILayout.Height(IconSize));
        EditorGUI.ObjectField(iconRect, _icon, GUIContent.none);
        DrawIconPreview(iconRect);

        EditorGUILayout.BeginVertical();
        EditorGUILayout.PropertyField(_id);
        EditorGUILayout.PropertyField(_displayName);
        DrawDropSettings();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawIconPreview(Rect iconRect)
    {
        Sprite sprite = _icon.objectReferenceValue as Sprite;
        if (sprite == null)
            return;

        Rect previewRect = new(
            iconRect.x + 3f,
            iconRect.y + 3f,
            iconRect.width - 24f,
            iconRect.height - 6f);

        Texture2D preview = AssetPreview.GetAssetPreview(sprite);
        if (preview != null)
        {
            GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit, true);
            return;
        }

        if (AssetPreview.IsLoadingAssetPreview(sprite.GetInstanceID()))
            Repaint();

        Texture2D texture = sprite.texture;
        Rect spriteRect = sprite.textureRect;
        Rect uv = new(
            spriteRect.x / texture.width,
            spriteRect.y / texture.height,
            spriteRect.width / texture.width,
            spriteRect.height / texture.height);
        GUI.DrawTextureWithTexCoords(
            FitRect(previewRect, spriteRect.width / spriteRect.height), texture, uv, true);
    }

    private static Rect FitRect(Rect bounds, float aspect)
    {
        float width = bounds.width;
        float height = width / aspect;
        if (height > bounds.height)
        {
            height = bounds.height;
            width = height * aspect;
        }

        return new Rect(
            bounds.x + (bounds.width - width) * 0.5f,
            bounds.y + (bounds.height - height) * 0.5f,
            width,
            height);
    }

    private void DrawDescription()
    {
        EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
        _description.stringValue = EditorGUILayout.TextArea(
            _description.stringValue, GUILayout.MinHeight(DescriptionHeight));
    }

    private void DrawDropSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Drop", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_rarity);
        EditorGUILayout.PropertyField(_weight);
        EditorGUILayout.PropertyField(_maxStacks);
        EditorGUILayout.EndVertical();
    }

    private void DrawStat(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty stat = _stats.GetArrayElementAtIndex(index);
        rect.y += 2f;
        rect.height = EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(rect, stat.FindPropertyRelative("_stat"));
        rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
        EditorGUI.PropertyField(rect, stat.FindPropertyRelative("_operation"));
        rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
        EditorGUI.PropertyField(rect, stat.FindPropertyRelative("_valuePerStack"));
        rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
        EditorGUI.PropertyField(rect, stat.FindPropertyRelative("_priority"));
    }

    private void DrawEffect(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty effect = _effects.GetArrayElementAtIndex(index);
        rect.y += 2f;
        rect.height = EditorGUIUtility.singleLineHeight;

        string label = effect.managedReferenceValue == null
            ? "Select Effect"
            : ObjectNames.NicifyVariableName(effect.managedReferenceValue.GetType().Name);
        if (EditorGUI.DropdownButton(rect, new GUIContent(label), FocusType.Keyboard))
            ShowTypeMenu(rect, index);

        rect.y += EditorGUIUtility.singleLineHeight + 4f;
        DrawChildren(rect, effect);
    }

    private float GetEffectHeight(int index)
    {
        SerializedProperty effect = _effects.GetArrayElementAtIndex(index);
        return EditorGUIUtility.singleLineHeight + 6f + GetChildrenHeight(effect);
    }

    private void ShowTypeMenu(Rect rect, int index)
    {
        GenericMenu menu = new();
        foreach (Type type in TypeCache.GetTypesDerivedFrom<GameplayEffect>())
        {
            if (type == typeof(StatGameplayEffect) || type.IsAbstract || type.IsGenericType ||
                !type.IsSerializable || (!type.IsPublic && !type.IsNestedPublic) ||
                type.GetConstructor(Type.EmptyTypes) == null)
                continue;

            string path = string.IsNullOrEmpty(type.Namespace)
                ? ObjectNames.NicifyVariableName(type.Name)
                : $"{type.Namespace.Replace('.', '/')}/{ObjectNames.NicifyVariableName(type.Name)}";
            menu.AddItem(new GUIContent(path), false, () => SetEffectType(index, type));
        }

        if (menu.GetItemCount() == 0)
            menu.AddDisabledItem(new GUIContent("No custom GameplayEffect found"));
        menu.DropDown(rect);
    }

    private void SetEffectType(int index, Type type)
    {
        serializedObject.Update();
        if (index < 0)
        {
            index = _effects.arraySize;
            _effects.arraySize++;
        }

        _effects.GetArrayElementAtIndex(index).managedReferenceValue =
            Activator.CreateInstance(type);
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private static void DrawChildren(Rect rect, SerializedProperty property)
    {
        if (property.managedReferenceValue == null)
            return;

        SerializedProperty child = property.Copy();
        SerializedProperty end = child.GetEndProperty();
        bool enterChildren = true;
        while (child.NextVisible(enterChildren) &&
               !SerializedProperty.EqualContents(child, end))
        {
            rect.height = EditorGUI.GetPropertyHeight(child, true);
            EditorGUI.PropertyField(rect, child, true);
            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
            enterChildren = false;
        }
    }

    private static float GetChildrenHeight(SerializedProperty property)
    {
        if (property.managedReferenceValue == null)
            return 0f;

        float height = 0f;
        SerializedProperty child = property.Copy();
        SerializedProperty end = child.GetEndProperty();
        bool enterChildren = true;
        while (child.NextVisible(enterChildren) &&
               !SerializedProperty.EqualContents(child, end))
        {
            height += EditorGUI.GetPropertyHeight(child, true) +
                      EditorGUIUtility.standardVerticalSpacing;
            enterChildren = false;
        }
        return height;
    }
}
