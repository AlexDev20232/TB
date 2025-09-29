// Assets/Editor/BrainrotBatchEditor.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BrainrotBatchEditor : EditorWindow
{
    // храним ссылки на ScriptableObject‑ы
    private readonly List<Brainrot> list = new List<Brainrot>();

    private Vector2 scroll;
    private string  search = "";
    private Group   filter = Group.All;

    private enum Group { All, Standard, Gold, Diamond, Candy }

    // ───────── меню ────────────────────────────────────────────────────
    [MenuItem("Tools/Brainrot/Batch Editor")]
    private static void Open() => GetWindow<BrainrotBatchEditor>("Brainrot Batch Editor");

    // ───────── корневой GUI ───────────────────────────────────────────
    private void OnGUI()
    {
        DrawToolbar();
        DrawTable();
        DrawBottomBar();
    }

    // ── верхняя панель: фильтры + поиск ──────────────────────────────
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        filter = (Group)GUILayout.Toolbar((int)filter,
                 new[] { "All", "Standard", "Gold", "Diamond", "Candy" },
                 EditorStyles.toolbarButton, GUILayout.MaxWidth(340));

        GUILayout.FlexibleSpace();

#if UNITY_2021_2_OR_NEWER
     //   search = EditorGUILayout.ToolbarSearchField(search);
#else
        // простое текстовое поле для версий до 2021.2
        search = GUILayout.TextField(search, EditorStyles.toolbarTextField,
                                     GUILayout.MaxWidth(200));
#endif
        EditorGUILayout.EndHorizontal();
    }

    // ── таблица объектов ──────────────────────────────────────────────
    private void DrawTable()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Asset",      EditorStyles.boldLabel, GUILayout.Width(180));
        GUILayout.Label("Rarity",     EditorStyles.boldLabel, GUILayout.Width(80));
        GUILayout.Label("Income/s",   EditorStyles.boldLabel, GUILayout.Width(90));
        GUILayout.Label("Price",      EditorStyles.boldLabel, GUILayout.Width(70));
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (Brainrot br in Filtered())
        {
            if (!br) continue;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.ObjectField(br, typeof(Brainrot), false, GUILayout.Width(180));
            GUILayout.Label(br.rarity.ToString(), GUILayout.Width(80));

            int income = EditorGUILayout.IntField(br.incomePerSecond, GUILayout.Width(90));
            int price  = EditorGUILayout.IntField(br.price,           GUILayout.Width(70));

            if (income != br.incomePerSecond || price != br.price)
            {
                Undo.RecordObject(br, "Edit Brainrot");
                br.incomePerSecond = Mathf.Max(0, income);
                br.price           = Mathf.Max(0, price);
                EditorUtility.SetDirty(br);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    // ── нижняя панель: drag’n’drop / кнопки ───────────────────────────
    private void DrawBottomBar()
    {
        GUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Selected", GUILayout.Width(110)))
            foreach (Object o in Selection.objects)
                if (o is Brainrot br && !list.Contains(br))
                    list.Add(br);

        if (GUILayout.Button("Clear", GUILayout.Width(60)))
            list.Clear();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Apply FIRST to Selection", GUILayout.Width(200)))
            ApplyFirstToSelection();

        EditorGUILayout.EndHorizontal();

        // зона для перетягивания файлов
        Rect drop = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
        GUI.Box(drop, "Drag Brainrot assets here", EditorStyles.helpBox);
        HandleDrag(drop);
    }

    // ───────── drag’n’drop реализация ────────────────────────────────
    private void HandleDrag(Rect area)
    {
        Event e = Event.current;
        if (!area.Contains(e.mousePosition)) return;

        if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (Object o in DragAndDrop.objectReferences)
                    if (o is Brainrot br && !list.Contains(br))
                        list.Add(br);
            }
            e.Use();
        }
    }

    // ───────── фильтрация по папке и поиску ───────────────────────────
    private IEnumerable<Brainrot> Filtered()
    {
        foreach (Brainrot br in list)
        {
            if (!br) continue;

            if (filter != Group.All)
            {
                string path = AssetDatabase.GetAssetPath(br).ToLower();
                if (!path.Contains(filter.ToString().ToLower())) continue;
            }

            if (!string.IsNullOrEmpty(search) &&
                !br.characterName.ToLower().Contains(search.ToLower()))
                continue;

            yield return br;
        }
    }

    // ───────── копирование значений из первого выделенного ────────────
    private void ApplyFirstToSelection()
    {
        if (Selection.objects.Length == 0) return;

        Brainrot reference = Selection.objects[0] as Brainrot;
        if (!reference) { Debug.LogWarning("First selected object is not a Brainrot"); return; }

        foreach (Object o in Selection.objects)
            if (o is Brainrot br)
            {
                Undo.RecordObject(br, "Batch apply");
                br.incomePerSecond = reference.incomePerSecond;
                br.price           = reference.price;
                EditorUtility.SetDirty(br);
            }

        Debug.Log($"Applied income={reference.incomePerSecond}, price={reference.price} to {Selection.objects.Length} assets");
    }
}
#endif
