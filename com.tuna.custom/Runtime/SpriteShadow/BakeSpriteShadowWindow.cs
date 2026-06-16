#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BakeSpriteShadowWindow : EditorWindow
{
    [SerializeField] private List<GameObject> targets = new List<GameObject>();
    [SerializeField] private string shadowName = "Shadow";
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private Vector3 localOffset = new Vector3(0.08f, -0.08f, 0f);
    [SerializeField] private Vector3 localEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 localScale = Vector3.one;
    [SerializeField] private int sortingOrderOffset = -1;
    [SerializeField] private bool includeChildren = true;
    [SerializeField] private bool updateExisting = true;

    private Vector2 scroll;

    [MenuItem("Tools/Sprite Tools/Bake Sprite Shadow")]
    private static void Open()
    {
        GetWindow<BakeSpriteShadowWindow>("Sprite Shadow");
    }

    [MenuItem("Tools/Sprite Tools/Bake Shadow For Selected")]
    private static void BakeSelected()
    {
        BakeSettings settings = GetDefaultSettings();
        BakeTargets(Selection.gameObjects, settings);
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawSettings();
        EditorGUILayout.Space(8);
        DrawTargets();
        EditorGUILayout.Space(8);
        DrawActions();

        EditorGUILayout.EndScrollView();
    }

    private void DrawSettings()
    {
        GUILayout.Label("Shadow Settings", EditorStyles.boldLabel);

        shadowName = EditorGUILayout.TextField("Child Name", shadowName);
        shadowColor = EditorGUILayout.ColorField("Color", shadowColor);
        localOffset = EditorGUILayout.Vector3Field("Local Offset", localOffset);
        localEulerAngles = EditorGUILayout.Vector3Field("Local Rotation", localEulerAngles);
        localScale = EditorGUILayout.Vector3Field("Local Scale", localScale);
        sortingOrderOffset = EditorGUILayout.IntField("Sorting Order Offset", sortingOrderOffset);
        includeChildren = EditorGUILayout.Toggle("Include Children", includeChildren);
        updateExisting = EditorGUILayout.Toggle("Update Existing", updateExisting);

        EditorGUILayout.HelpBox(
            "Creates child SpriteRenderers that reuse source sprites, tint them as shadows, and sort them behind the source.",
            MessageType.None
        );
    }

    private void DrawTargets()
    {
        GUILayout.Label("Targets", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Selected", GUILayout.Height(28)))
            {
                AddSelected();
            }

            if (GUILayout.Button("Clear", GUILayout.Height(28)))
            {
                targets.Clear();
            }
        }

        int removeIndex = -1;

        for (int i = 0; i < targets.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                targets[i] = (GameObject)EditorGUILayout.ObjectField(
                    $"Target {i + 1}",
                    targets[i],
                    typeof(GameObject),
                    true
                );

                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    removeIndex = i;
                }
            }
        }

        if (removeIndex >= 0)
        {
            targets.RemoveAt(removeIndex);
        }
    }

    private void DrawActions()
    {
        int targetCount = GetValidTargetCount();
        EditorGUILayout.HelpBox($"Target objects: {targetCount}", MessageType.Info);

        using (new EditorGUI.DisabledScope(targetCount == 0))
        {
            GUI.backgroundColor = Color.green;

            if (GUILayout.Button("BAKE / UPDATE SHADOW", GUILayout.Height(36)))
            {
                BakeSettings settings = GetSettings();
                BakeTargets(targets, settings);
            }

            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("REMOVE SHADOW CHILD", GUILayout.Height(28)))
            {
                RemoveTargets(targets, GetFinalShadowName());
            }
        }
    }

    private void AddSelected()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go != null && !targets.Contains(go))
            {
                targets.Add(go);
            }
        }
    }

    private int GetValidTargetCount()
    {
        int count = 0;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private BakeSettings GetSettings()
    {
        return new BakeSettings
        {
            ShadowName = GetFinalShadowName(),
            ShadowColor = shadowColor,
            LocalOffset = localOffset,
            LocalEulerAngles = localEulerAngles,
            LocalScale = localScale,
            SortingOrderOffset = sortingOrderOffset,
            IncludeChildren = includeChildren,
            UpdateExisting = updateExisting
        };
    }

    private string GetFinalShadowName()
    {
        return string.IsNullOrWhiteSpace(shadowName) ? "Shadow" : shadowName.Trim();
    }

    private static void BakeTargets(IList<GameObject> targetObjects, BakeSettings settings)
    {
        if (targetObjects == null || targetObjects.Count == 0)
            return;

        int bakedCount = 0;
        int skippedCount = 0;
        int failedCount = 0;

        for (int i = 0; i < targetObjects.Count; i++)
        {
            GameObject target = targetObjects[i];

            if (target == null)
                continue;

            string assetPath = AssetDatabase.GetAssetPath(target);

            if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab"))
            {
                BakePrefabAsset(assetPath, settings, ref bakedCount, ref skippedCount, ref failedCount);
            }
            else
            {
                BakeSceneObject(target, settings, ref bakedCount, ref skippedCount);
            }
        }

        EditorUtility.DisplayDialog(
            "Bake Sprite Shadow Done",
            $"Baked / Updated: {bakedCount}\nSkipped: {skippedCount}\nFailed: {failedCount}",
            "OK"
        );
    }

    private static BakeSettings GetDefaultSettings()
    {
        return new BakeSettings
        {
            ShadowName = "Shadow",
            ShadowColor = new Color(0f, 0f, 0f, 0.35f),
            LocalOffset = new Vector3(0.08f, -0.08f, 0f),
            LocalEulerAngles = Vector3.zero,
            LocalScale = Vector3.one,
            SortingOrderOffset = -1,
            IncludeChildren = true,
            UpdateExisting = true
        };
    }

    private static void BakeSceneObject(
        GameObject target,
        BakeSettings settings,
        ref int bakedCount,
        ref int skippedCount)
    {
        List<SpriteRenderer> renderers = GetSpriteRenderers(target, settings.IncludeChildren);

        for (int i = 0; i < renderers.Count; i++)
        {
            SpriteRenderer source = renderers[i];

            if (source == null || source.sprite == null || IsShadowRenderer(source, settings.ShadowName))
            {
                skippedCount++;
                continue;
            }

            SpriteRenderer shadow = FindDirectShadow(source.transform, settings.ShadowName);

            if (shadow != null && !settings.UpdateExisting)
            {
                skippedCount++;
                continue;
            }

            if (shadow == null)
            {
                GameObject shadowObject = new GameObject(settings.ShadowName);
                Undo.RegisterCreatedObjectUndo(shadowObject, "Create Sprite Shadow");
                shadowObject.transform.SetParent(source.transform, false);
                shadow = shadowObject.AddComponent<SpriteRenderer>();
            }
            else
            {
                Undo.RecordObject(shadow.gameObject, "Update Sprite Shadow");
                Undo.RecordObject(shadow, "Update Sprite Shadow");
            }

            ApplyShadow(source, shadow, settings);
            PrefabUtility.RecordPrefabInstancePropertyModifications(shadow.gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(shadow);

            bakedCount++;
        }
    }

    private static void BakePrefabAsset(
        string prefabPath,
        BakeSettings settings,
        ref int bakedCount,
        ref int skippedCount,
        ref int failedCount)
    {
        GameObject prefabRoot = null;

        try
        {
            prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            List<SpriteRenderer> renderers = GetSpriteRenderers(prefabRoot, settings.IncludeChildren);
            bool changed = false;

            for (int i = 0; i < renderers.Count; i++)
            {
                SpriteRenderer source = renderers[i];

                if (source == null || source.sprite == null || IsShadowRenderer(source, settings.ShadowName))
                {
                    skippedCount++;
                    continue;
                }

                SpriteRenderer shadow = FindDirectShadow(source.transform, settings.ShadowName);

                if (shadow != null && !settings.UpdateExisting)
                {
                    skippedCount++;
                    continue;
                }

                if (shadow == null)
                {
                    GameObject shadowObject = new GameObject(settings.ShadowName);
                    shadowObject.transform.SetParent(source.transform, false);
                    shadow = shadowObject.AddComponent<SpriteRenderer>();
                }

                ApplyShadow(source, shadow, settings);
                changed = true;
                bakedCount++;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
        }
        catch (System.Exception e)
        {
            failedCount++;
            Debug.LogError($"Failed to bake sprite shadow for prefab: {prefabPath}\n{e}");
        }
        finally
        {
            if (prefabRoot != null)
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }

    private static void RemoveTargets(IList<GameObject> targetObjects, string shadowName)
    {
        if (targetObjects == null || targetObjects.Count == 0)
            return;

        int removedCount = 0;
        int failedCount = 0;

        for (int i = 0; i < targetObjects.Count; i++)
        {
            GameObject target = targetObjects[i];

            if (target == null)
                continue;

            string assetPath = AssetDatabase.GetAssetPath(target);

            if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab"))
            {
                RemoveFromPrefabAsset(assetPath, shadowName, ref removedCount, ref failedCount);
            }
            else
            {
                removedCount += RemoveFromSceneObject(target.transform, shadowName);
            }
        }

        EditorUtility.DisplayDialog(
            "Remove Sprite Shadow Done",
            $"Removed: {removedCount}\nFailed: {failedCount}",
            "OK"
        );
    }

    private static void RemoveFromPrefabAsset(string prefabPath, string shadowName, ref int removedCount, ref int failedCount)
    {
        GameObject prefabRoot = null;

        try
        {
            prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            int count = RemoveDirectShadowsRecursive(prefabRoot.transform, shadowName);

            if (count > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                removedCount += count;
            }
        }
        catch (System.Exception e)
        {
            failedCount++;
            Debug.LogError($"Failed to remove sprite shadow from prefab: {prefabPath}\n{e}");
        }
        finally
        {
            if (prefabRoot != null)
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }

    private static int RemoveFromSceneObject(Transform target, string shadowName)
    {
        int removedCount = 0;
        List<GameObject> shadows = new List<GameObject>();
        CollectShadowObjects(target, shadowName, shadows);

        for (int i = 0; i < shadows.Count; i++)
        {
            Undo.DestroyObjectImmediate(shadows[i]);
            removedCount++;
        }

        return removedCount;
    }

    private static int RemoveDirectShadowsRecursive(Transform target, string shadowName)
    {
        int removedCount = 0;

        for (int i = target.childCount - 1; i >= 0; i--)
        {
            Transform child = target.GetChild(i);

            if (child.name == shadowName && child.GetComponent<SpriteRenderer>() != null)
            {
                DestroyImmediate(child.gameObject);
                removedCount++;
                continue;
            }

            removedCount += RemoveDirectShadowsRecursive(child, shadowName);
        }

        return removedCount;
    }

    private static void CollectShadowObjects(Transform target, string shadowName, List<GameObject> results)
    {
        for (int i = 0; i < target.childCount; i++)
        {
            Transform child = target.GetChild(i);

            if (child.name == shadowName && child.GetComponent<SpriteRenderer>() != null)
            {
                results.Add(child.gameObject);
                continue;
            }

            CollectShadowObjects(child, shadowName, results);
        }
    }

    private static List<SpriteRenderer> GetSpriteRenderers(GameObject target, bool includeChildren)
    {
        List<SpriteRenderer> renderers = new List<SpriteRenderer>();

        if (includeChildren)
        {
            target.GetComponentsInChildren(true, renderers);
        }
        else
        {
            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();

            if (renderer != null)
            {
                renderers.Add(renderer);
            }
        }

        return renderers;
    }

    private static SpriteRenderer FindDirectShadow(Transform source, string shadowName)
    {
        Transform child = null;

        for (int i = 0; i < source.childCount; i++)
        {
            Transform current = source.GetChild(i);

            if (current.name == shadowName)
            {
                child = current;
                break;
            }
        }

        return child == null ? null : child.GetComponent<SpriteRenderer>();
    }

    private static bool IsShadowRenderer(SpriteRenderer renderer, string shadowName)
    {
        return renderer.transform.name == shadowName;
    }

    private static void ApplyShadow(SpriteRenderer source, SpriteRenderer shadow, BakeSettings settings)
    {
        Transform shadowTransform = shadow.transform;

        shadowTransform.SetParent(source.transform, false);
        shadowTransform.localPosition = settings.LocalOffset;
        shadowTransform.localRotation = Quaternion.Euler(settings.LocalEulerAngles);
        shadowTransform.localScale = settings.LocalScale;
        shadowTransform.SetAsFirstSibling();

        shadow.sprite = source.sprite;
        shadow.color = settings.ShadowColor;
        shadow.flipX = source.flipX;
        shadow.flipY = source.flipY;
        shadow.drawMode = source.drawMode;
        shadow.size = source.size;
        shadow.tileMode = source.tileMode;
        shadow.maskInteraction = source.maskInteraction;
        shadow.spriteSortPoint = source.spriteSortPoint;
        shadow.sortingLayerID = source.sortingLayerID;
        shadow.sortingOrder = source.sortingOrder + settings.SortingOrderOffset;
        shadow.sharedMaterial = source.sharedMaterial;
    }

    private struct BakeSettings
    {
        public string ShadowName;
        public Color ShadowColor;
        public Vector3 LocalOffset;
        public Vector3 LocalEulerAngles;
        public Vector3 LocalScale;
        public int SortingOrderOffset;
        public bool IncludeChildren;
        public bool UpdateExisting;
    }
}

#endif
