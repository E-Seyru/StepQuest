using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool to normalize UI prefab scales.
/// Bakes scale into sizeDelta so all RectTransforms have scale (1,1,1) while preserving visual appearance.
/// Useful for fixing layout issues caused by scaled UI elements.
/// </summary>
public class PrefabScaleNormalizer : EditorWindow
{
    private GameObject targetPrefab;
    private List<GameObject> prefabsToProcess = new List<GameObject>();
    private Vector2 scrollPosition;
    private bool showPreview = false;
    private List<ScaleInfo> previewInfo = new List<ScaleInfo>();

    private struct ScaleInfo
    {
        public string path;
        public Vector3 oldScale;
        public Vector2 oldSize;
        public Vector2 newSize;
        public bool willChange;
    }

    [MenuItem("WalkAndRPG/Prefab Scale Normalizer")]
    public static void ShowWindow()
    {
        var window = GetWindow<PrefabScaleNormalizer>("Scale Normalizer");
        window.minSize = new Vector2(400, 300);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Prefab Scale Normalizer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool converts scaled UI elements to use sizeDelta instead.\n" +
            "All RectTransforms will have scale (1,1,1) while keeping the same visual size.\n\n" +
            "This fixes layout issues where HorizontalLayoutGroup/VerticalLayoutGroup ignore scale.",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // Single prefab mode
        EditorGUILayout.LabelField("Single Prefab", EditorStyles.boldLabel);
        targetPrefab = (GameObject)EditorGUILayout.ObjectField("Target Prefab", targetPrefab, typeof(GameObject), false);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Preview Changes"))
        {
            if (targetPrefab != null)
            {
                PreviewChanges(targetPrefab);
                showPreview = true;
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign a prefab first.", "OK");
            }
        }

        GUI.enabled = targetPrefab != null;
        if (GUILayout.Button("Normalize Prefab"))
        {
            if (targetPrefab != null)
            {
                NormalizePrefab(targetPrefab);
            }
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Selection mode
        EditorGUILayout.LabelField("From Selection", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select prefabs in the Project window, then click the button below.", MessageType.None);

        if (GUILayout.Button("Normalize Selected Prefabs"))
        {
            NormalizeSelectedPrefabs();
        }

        EditorGUILayout.Space(10);

        // Preview section
        if (showPreview && previewInfo.Count > 0)
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            foreach (var info in previewInfo)
            {
                if (info.willChange)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(info.path, GUILayout.Width(200));
                    EditorGUILayout.LabelField($"Scale: {info.oldScale:F2} → (1,1,1)", GUILayout.Width(180));
                    EditorGUILayout.LabelField($"Size: {info.oldSize:F1} → {info.newSize:F1}");
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();

            int changeCount = previewInfo.FindAll(x => x.willChange).Count;
            EditorGUILayout.LabelField($"{changeCount} transforms will be modified.");
        }

        EditorGUILayout.Space(10);

        // Normalize scene selection
        EditorGUILayout.LabelField("Scene Objects", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select GameObjects in the Scene hierarchy to normalize them (won't modify prefab assets).", MessageType.None);

        if (GUILayout.Button("Normalize Selected Scene Objects"))
        {
            NormalizeSelectedSceneObjects();
        }
    }

    private void PreviewChanges(GameObject prefab)
    {
        previewInfo.Clear();

        var rectTransforms = prefab.GetComponentsInChildren<RectTransform>(true);
        foreach (var rt in rectTransforms)
        {
            var info = new ScaleInfo
            {
                path = GetPath(rt.transform, prefab.transform),
                oldScale = rt.localScale,
                oldSize = rt.sizeDelta,
                willChange = !IsScaleOne(rt.localScale)
            };

            if (info.willChange)
            {
                // Calculate cumulative scale from root to this transform
                Vector3 cumulativeScale = GetCumulativeScale(rt.transform, prefab.transform);
                info.newSize = new Vector2(
                    rt.sizeDelta.x * cumulativeScale.x,
                    rt.sizeDelta.y * cumulativeScale.y
                );
            }
            else
            {
                info.newSize = info.oldSize;
            }

            previewInfo.Add(info);
        }
    }

    private void NormalizePrefab(GameObject prefab)
    {
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(assetPath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a prefab asset, not a scene instance.", "OK");
            return;
        }

        // Load prefab contents for editing
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

        try
        {
            int changeCount = NormalizeTransforms(prefabRoot);

            // Save changes
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);

            EditorUtility.DisplayDialog("Success", $"Normalized {changeCount} transforms in {prefab.name}.", "OK");
            Logger.LogInfo($"[PrefabScaleNormalizer] Normalized {changeCount} transforms in {assetPath}", Logger.LogCategory.EditorLog);
        }
        finally
        {
            // Unload prefab contents
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        // Refresh preview
        PreviewChanges(prefab);
    }

    private void NormalizeSelectedPrefabs()
    {
        var selectedObjects = Selection.objects;
        int totalChanges = 0;
        int prefabCount = 0;

        foreach (var obj in selectedObjects)
        {
            if (obj is GameObject go)
            {
                string assetPath = AssetDatabase.GetAssetPath(go);
                if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab"))
                {
                    GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
                    try
                    {
                        int changes = NormalizeTransforms(prefabRoot);
                        totalChanges += changes;
                        prefabCount++;

                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                        Logger.LogInfo($"[PrefabScaleNormalizer] Normalized {changes} transforms in {assetPath}", Logger.LogCategory.EditorLog);
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                    }
                }
            }
        }

        if (prefabCount > 0)
        {
            EditorUtility.DisplayDialog("Success", $"Normalized {totalChanges} transforms across {prefabCount} prefabs.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("No Prefabs", "No prefab assets were selected.", "OK");
        }
    }

    private void NormalizeSelectedSceneObjects()
    {
        var selectedObjects = Selection.gameObjects;
        int totalChanges = 0;

        Undo.RecordObjects(selectedObjects, "Normalize UI Scales");

        foreach (var go in selectedObjects)
        {
            // Record all RectTransforms for undo
            var rectTransforms = go.GetComponentsInChildren<RectTransform>(true);
            Undo.RecordObjects(rectTransforms, "Normalize UI Scales");

            totalChanges += NormalizeTransforms(go);
        }

        if (totalChanges > 0)
        {
            EditorUtility.DisplayDialog("Success", $"Normalized {totalChanges} transforms in scene.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("No Changes", "No scaled transforms found to normalize.", "OK");
        }
    }

    /// <summary>
    /// Normalizes all RectTransforms under the given root.
    /// Returns the number of transforms that were modified.
    /// </summary>
    private int NormalizeTransforms(GameObject root)
    {
        int changeCount = 0;

        // Get all RectTransforms, process from deepest to shallowest
        var rectTransforms = root.GetComponentsInChildren<RectTransform>(true);
        var sortedByDepth = new List<RectTransform>(rectTransforms);
        sortedByDepth.Sort((a, b) => GetDepth(b.transform) - GetDepth(a.transform)); // Deepest first

        // First pass: calculate and store the cumulative scales before any modifications
        var cumulativeScales = new Dictionary<RectTransform, Vector3>();
        foreach (var rt in rectTransforms)
        {
            cumulativeScales[rt] = GetCumulativeScale(rt.transform, root.transform);
        }

        // Second pass: apply normalization (deepest first to avoid affecting children)
        foreach (var rt in sortedByDepth)
        {
            if (IsScaleOne(rt.localScale))
                continue;

            Vector3 cumScale = cumulativeScales[rt];

            // Bake cumulative scale into sizeDelta
            Vector2 newSize = new Vector2(
                rt.sizeDelta.x * cumScale.x,
                rt.sizeDelta.y * cumScale.y
            );

            rt.sizeDelta = newSize;
            rt.localScale = Vector3.one;

            changeCount++;
        }

        return changeCount;
    }

    /// <summary>
    /// Gets the cumulative scale from root to the given transform (product of all localScales).
    /// </summary>
    private Vector3 GetCumulativeScale(Transform target, Transform root)
    {
        Vector3 scale = Vector3.one;
        Transform current = target;

        while (current != null && current != root.parent)
        {
            scale = Vector3.Scale(scale, current.localScale);
            current = current.parent;
        }

        return scale;
    }

    private bool IsScaleOne(Vector3 scale)
    {
        return Mathf.Approximately(scale.x, 1f) &&
               Mathf.Approximately(scale.y, 1f) &&
               Mathf.Approximately(scale.z, 1f);
    }

    private int GetDepth(Transform t)
    {
        int depth = 0;
        while (t.parent != null)
        {
            depth++;
            t = t.parent;
        }
        return depth;
    }

    private string GetPath(Transform target, Transform root)
    {
        string path = target.name;
        Transform current = target.parent;

        while (current != null && current != root)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
