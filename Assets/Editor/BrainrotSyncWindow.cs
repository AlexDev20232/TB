#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BrainrotStatSync : EditorWindow
{
    // ====== настраиваемые папки ======
    private DefaultAsset fldStandard;
    private DefaultAsset fldGold;
    private DefaultAsset fldDiamond;
    private DefaultAsset fldCandy;

    // ====== коэфф‑ты дохода ======
    private const float GOLD_K     = 1.25f;
    private const float DIAMOND_K  = 1.50f;
    private const float CANDY_K    = 4.0f;

    // кэш эталонов (если нужны точечные правки)
    private readonly List<Brainrot> manualStandards = new List<Brainrot>();
    private Vector2 scroll;

    // ------------------------------------------------------------------
    [MenuItem("Tools/Brainrot/Stat Sync")]
    private static void Open() => GetWindow<BrainrotStatSync>("Brainrot Stat Sync");

    private void OnGUI()
    {
        DrawFoldersBlock();
        EditorGUILayout.Space(4);

        DrawManualStandards();
        EditorGUILayout.Space(6);

        GUI.enabled = HasAnySource();
        if (GUILayout.Button("SYNC", GUILayout.Height(32)))
            SyncStats();
        GUI.enabled = true;

        DrawDragArea();
    }

    // ──────────────────────────────────────────────────────────────────
    #region GUI blocks
    private void DrawFoldersBlock()
    {
        EditorGUILayout.LabelField("Group Folders", EditorStyles.boldLabel);

        fldStandard = (DefaultAsset)EditorGUILayout.ObjectField(
            "Standard folder", fldStandard, typeof(DefaultAsset), false);
        fldGold     = (DefaultAsset)EditorGUILayout.ObjectField(
            "Gold folder",     fldGold,     typeof(DefaultAsset), false);
        fldDiamond  = (DefaultAsset)EditorGUILayout.ObjectField(
            "Diamond folder",  fldDiamond,  typeof(DefaultAsset), false);
        fldCandy    = (DefaultAsset)EditorGUILayout.ObjectField(
            "Candy folder",    fldCandy,    typeof(DefaultAsset), false);
    }

    private void DrawManualStandards()
    {
        EditorGUILayout.LabelField("Manual Standard assets (optional)",
                                   EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(120));

        for (int i = 0; i < manualStandards.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            manualStandards[i] = (Brainrot)EditorGUILayout.ObjectField(
                                     manualStandards[i], typeof(Brainrot), false);
            if (GUILayout.Button("−", GUILayout.Width(20)))
            { manualStandards.RemoveAt(i); i--; }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("+ Add Selected", GUILayout.Width(120)))
            foreach (Object o in Selection.objects)
                if (o is Brainrot br && !manualStandards.Contains(br))
                    manualStandards.Add(br);
    }

    private void DrawDragArea()
    {
        Rect r = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
        GUI.Box(r, "Drag folders or Brainrot assets here", EditorStyles.helpBox);

        Event e = Event.current;
        if (!r.Contains(e.mousePosition)) return;

        if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (Object o in DragAndDrop.objectReferences)
                {
                    string path = AssetDatabase.GetAssetPath(o);

                    if (AssetDatabase.IsValidFolder(path))
                        AssignFolderByName(o as DefaultAsset, path.ToLower());
                    else if (o is Brainrot br && !manualStandards.Contains(br))
                        manualStandards.Add(br);
                }
            }
            e.Use();
        }
    }
    #endregion

    // ------------------------------------------------------------------
    #region Core logic
    private bool HasAnySource()
    {
        return fldStandard || manualStandards.Count > 0;
    }

    private void SyncStats()
    {
        List<Brainrot> standards = GatherStandards();
        if (standards.Count == 0)
        {
            Debug.LogWarning("No Standard Brainrots found");
            return;
        }

        int updated = 0;
        foreach (Brainrot src in standards)
        {
            if (!src) continue;
            updated += ApplyToGroup(src, fldGold,    GOLD_K);
            updated += ApplyToGroup(src, fldDiamond, DIAMOND_K);
            updated += ApplyToGroup(src, fldCandy,   CANDY_K);
        }

        if (updated > 0)
            AssetDatabase.SaveAssets();

        Debug.Log($"Stat Sync finished: updated {updated} assets.");
    }

    private List<Brainrot> GatherStandards()
    {
        var result = new List<Brainrot>();

        if (fldStandard)
            result.AddRange(FindBrainrotsInFolder(fldStandard));

        foreach (var br in manualStandards)
            if (br && !result.Contains(br))
                result.Add(br);

        return result;
    }

    private int ApplyToGroup(Brainrot src, DefaultAsset folder, float k)
    {
        if (!folder) return 0;

        string folderPath = AssetDatabase.GetAssetPath(folder);
        foreach (Brainrot dst in FindBrainrotsInFolder(folder))
        {
            if (dst.characterName != src.characterName) continue;

            Undo.RecordObject(dst, "Sync stats");
            dst.rarity          = src.rarity;
            dst.price           = src.price;
            dst.incomePerSecond = Mathf.RoundToInt(src.incomePerSecond * k);
            EditorUtility.SetDirty(dst);
            return 1; // обновили одного – выходим
        }
        return 0;
    }

    private static List<Brainrot> FindBrainrotsInFolder(DefaultAsset folderAsset)
    {
        var list = new List<Brainrot>();
        if (!folderAsset) return list;

        string folderPath = AssetDatabase.GetAssetPath(folderAsset);
        string[] guids = AssetDatabase.FindAssets("t:Brainrot", new[] { folderPath });

        foreach (string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            Brainrot br = AssetDatabase.LoadAssetAtPath<Brainrot>(path);
            if (br) list.Add(br);
        }
        return list;
    }

    private void AssignFolderByName(DefaultAsset folder, string pathLower)
    {
        if (pathLower.Contains("standart") || pathLower.Contains("standard"))
            fldStandard = folder;
        else if (pathLower.Contains("gold"))
            fldGold = folder;
        else if (pathLower.Contains("diamant") || pathLower.Contains("diamond"))
            fldDiamond = folder;
        else if (pathLower.Contains("candy"))
            fldCandy = folder;
    }
    #endregion
}
#endif
