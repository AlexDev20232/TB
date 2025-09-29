// Assets/Editor/BrainrotIconPrefabCreator.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class BrainrotIconPrefabCreator : EditorWindow
{
    // ─────────── UI-поля ───────────
    [SerializeField] private GameObject baseIconPrefab; // базовый образец
    [SerializeField] private DefaultAsset targetFolder; // куда сохранять
    [SerializeField] private List<Brainrot> brainrots = new(); // drag‑список

    private Vector2 _scroll;

    // ─────────── меню ───────────
    [MenuItem("Tools/Brainrot/Icon Prefab Creator")]
    private static void Open() => GetWindow<BrainrotIconPrefabCreator>("Brainrot Icons");

    // ─────────── GUI ───────────
    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        DrawDragArea();
        EditorGUILayout.Space(6);

        baseIconPrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Base icon prefab", "Префаб‑шаблон c Image + BrainrotIconSet"),
            baseIconPrefab, typeof(GameObject), false);

        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            new GUIContent("Target folder", "Папка для .prefab и поиск при Assign"),
            targetFolder, typeof(DefaultAsset), false);

        // ───── кнопки ─────
        GUILayout.Space(10);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = CanCreate();
            if (GUILayout.Button("Create Prefabs", GUILayout.Height(30)))
                CreatePrefabs();

            GUI.enabled = CanAssign();
            if (GUILayout.Button("Assign Prefabs", GUILayout.Height(30)))
                AssignPrefabs();
            GUI.enabled = true;
        }
    }

    // ─────────── Drag‑&‑Drop список ───────────
    private void DrawDragArea()
    {
        GUILayout.Label("Drag Brainrot assets here:", EditorStyles.boldLabel);

        Rect box = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));
        GUI.Box(box, "");

        Event evt = Event.current;
        if (box.Contains(evt.mousePosition))
        {
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (Object o in DragAndDrop.objectReferences)
                    {
                        var br = o as Brainrot;
                        if (br != null && !brainrots.Contains(br))
                            brainrots.Add(br);
                    }
                }
                evt.Use();
            }
        }

        // список
        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(80));
        for (int i = brainrots.Count - 1; i >= 0; i--)
        {
            GUILayout.BeginHorizontal();
            brainrots[i] = (Brainrot)EditorGUILayout.ObjectField(brainrots[i], typeof(Brainrot), false);
            if (GUILayout.Button("X", GUILayout.Width(20))) brainrots.RemoveAt(i);
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }

    // ─────────── валидация ───────────
    private bool CanCreate()  => baseIconPrefab && targetFolder && brainrots.Count > 0;
    private bool CanAssign()  => targetFolder && brainrots.Count > 0;

    // ─────────── создание префабов ───────────
    private void CreatePrefabs()
    {
        string folderPath = AssetDatabase.GetAssetPath(targetFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("Ошибка", "Выберите существующую папку Assets!", "OK");
            return;
        }

        int created = 0;
        foreach (var br in brainrots)
        {
            if (!br) continue;
            string newPath = Path.Combine(folderPath, br.name + ".prefab").Replace("\\", "/");

            if (File.Exists(newPath) &&
                !EditorUtility.DisplayDialog("Файл существует",
                        $"Файл {newPath} уже есть. Перезаписать?", "Да", "Пропустить"))
                continue;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(baseIconPrefab);
            PrefabUtility.SaveAsPrefabAssetAndConnect(instance, newPath, InteractionMode.UserAction);
            Object.DestroyImmediate(instance);
            created++;
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Создание завершено", $"Создано префабов: {created}", "OK");
    }

    // ─────────── привязка префабов к Brainrot ───────────
    private void AssignPrefabs()
    {
        string folderPath = AssetDatabase.GetAssetPath(targetFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("Ошибка", "Неверная целевая папка!", "OK");
            return;
        }

        int assigned = 0;
        foreach (var br in brainrots)
        {
            if (!br) continue;

            // ищем prefab по части имени
            string[] guids = AssetDatabase.FindAssets($"{br.characterName} t:prefab", new[] { folderPath });
            if (guids.Length == 0)                           // пробуем по полному имени SO
                guids = AssetDatabase.FindAssets($"{br.name} t:prefab", new[] { folderPath });

            if (guids.Length == 0)
            {
                Debug.LogWarning($"Prefab для {br.name} не найден.");
                continue;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (!prefab)
            {
                Debug.LogWarning($"Префаб по пути {path} не загрузился.");
                continue;
            }

            if (br.iconPrefab != prefab)
            {
                br.iconPrefab = prefab;
                EditorUtility.SetDirty(br);
                assigned++;
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Привязка завершена", $"Назначено: {assigned}", "OK");
    }
}
#endif
