// File: Assets/Editor/BrainrotBatchRenamer.cs   (v1.1)
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BrainrotBatchRenamer : EditorWindow
{
    private readonly List<Brainrot> brainrots = new List<Brainrot>();
    private Vector2 listScroll;
    private string  namesRaw = string.Empty;

    [MenuItem("Tools/Brainrot/Batch Renamer")]
    private static void ShowWindow()
    {
        var wnd = GetWindow<BrainrotBatchRenamer>("Brainrot Renamer");
        wnd.minSize = new Vector2(350, 300);
    }

    private void OnGUI()
    {
        DrawObjectsList();
        DrawNamesArea();

        int nameLines = CountLines(namesRaw);
        int objCount  = brainrots.Count;

        bool singleNameMode = nameLines == 1 && objCount > 1;
        int  diff           = singleNameMode ? 0 : objCount - nameLines;

        GUI.color = diff == 0 ? Color.green
                 : diff > 0  ? Color.yellow
                              : Color.red;

        EditorGUILayout.LabelField(
            $"Assets: {objCount}   Names: {nameLines}" +
            (singleNameMode ? "  (single‑name mode)" :
             diff == 0      ? "  ✓ Ready" :
             diff > 0       ? $"  ⚠ Недостаёт {diff} имён" :
                              $"  ⚠ Лишних {Mathf.Abs(diff)} имён") );

        GUI.color   = Color.white;
        GUI.enabled = (diff == 0) && objCount > 0;

        if (GUILayout.Button("Apply", GUILayout.Height(30)))
            ApplyNames(singleNameMode);

        GUI.enabled = true;
    }

    // ───────── UI helpers ──────────────────────────────────────────────
    private void DrawObjectsList()
    {
        EditorGUILayout.LabelField("Brainrot Assets", EditorStyles.boldLabel);
        listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.Height(120));
        for (int i = 0; i < brainrots.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            brainrots[i] = (Brainrot)EditorGUILayout.ObjectField(brainrots[i],
                                typeof(Brainrot), false);
            if (GUILayout.Button("−", GUILayout.Width(20)))
            {
                brainrots.RemoveAt(i); i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        Rect drop = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        GUI.Box(drop, "Drag Brainrot assets here", EditorStyles.helpBox);
        HandleDragAndDrop(drop);

        if (GUILayout.Button("+ Add Selected", GUILayout.Height(22)))
            foreach (var o in Selection.objects)
                if (o is Brainrot br && !brainrots.Contains(br))
                    brainrots.Add(br);

        EditorGUILayout.Space(10);
    }

    private void DrawNamesArea()
    {
        EditorGUILayout.LabelField("Names (one per line)", EditorStyles.boldLabel);
        namesRaw = EditorGUILayout.TextArea(namesRaw, GUILayout.MinHeight(80));
    }

    private void HandleDragAndDrop(Rect area)
    {
        var e = Event.current;
        if (!area.Contains(e.mousePosition)) return;

        if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var o in DragAndDrop.objectReferences)
                    if (o is Brainrot br && !brainrots.Contains(br))
                        brainrots.Add(br);
            }
            e.Use();
        }
    }

    // ───────── Core logic ──────────────────────────────────────────────
    private int CountLines(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        return s.Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private void ApplyNames(bool singleNameMode)
    {
        string[] names = namesRaw
            .Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        Undo.RecordObjects(brainrots.ToArray(), "Rename Brainrots");

        for (int i = 0; i < brainrots.Count; i++)
        {
            string baseName = singleNameMode ? names[0].Trim()
                                             : names[i].Trim();

            // 1) поле characterName
            brainrots[i].characterName = baseName;
            EditorUtility.SetDirty(brainrots[i]);

            // 2) переименование asset‑файла (уникальные имена)
            string path = AssetDatabase.GetAssetPath(brainrots[i]);
            string finalName = singleNameMode && brainrots.Count > 1
                               ? $"{baseName} ({i + 1})"
                               : baseName;
            AssetDatabase.RenameAsset(path, finalName);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Brainrot Renamer: переименовано {brainrots.Count} объектов.");
    }
}
#endif
