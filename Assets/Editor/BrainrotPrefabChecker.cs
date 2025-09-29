// Assets/Editor/BrainrotPrefabChecker.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Проверяет, указывает ли каждый Brainrot на префаб,
/// расположенный в правильной папке.
/// </summary>
public class BrainrotPrefabChecker : EditorWindow
{
    // ────────── ссылки на папки (выбираются в окне) ──────────
    [SerializeField] private Object brainrotFolder;   // где лежат *.asset
    [SerializeField] private Object prefabFolder;     // где должны лежать префабы

    // ────────── служебные поля ──────────
    private readonly List<Item> items = new();        // результат сканирования
    private Vector2             scroll;

    private class Item
    {
        public Brainrot  br;          // сам ScriptableObject
        public GameObject prefab;     // что прописано в characterPrefab
        public bool  missing  => prefab == null;           // вообще не назначен
        public bool  inFolder;                             // лежит в нужной папке?
        public bool  wrongFolder => !missing && !inFolder; // назначен, но не там
    }

    // ────────── меню ──────────
    [MenuItem("Tools/Brainrot/Prefab Checker")]
    private static void Open() => GetWindow<BrainrotPrefabChecker>("Brainrot ⇄ Prefab");

    // ────────── GUI ──────────
    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        DrawFolders();
        EditorGUILayout.Space(6);

        if (GUILayout.Button("Scan", GUILayout.Height(25)))
            Scan();

        if (items.Count > 0)
            DrawTable();
    }

    private void DrawFolders()
    {
        EditorGUI.BeginChangeCheck();
        brainrotFolder = EditorGUILayout.ObjectField("Brainrot folder", brainrotFolder, typeof(DefaultAsset), false);
        prefabFolder   = EditorGUILayout.ObjectField("Prefab folder",   prefabFolder,   typeof(DefaultAsset), false);
        if (EditorGUI.EndChangeCheck())
            items.Clear(); // сменили путь – очищаем результаты
    }

    // ────────── сканирование ──────────
    private void Scan()
    {
        items.Clear();
        if (!ValidateFolders()) return;

        string brPath = AssetDatabase.GetAssetPath(brainrotFolder);
        string pfPath = AssetDatabase.GetAssetPath(prefabFolder);
        if (!pfPath.EndsWith("/")) pfPath += "/";     // чтобы StartsWith работал надёжно

        // все Brainrot‑ы под папкой
        string[] brGUIDs = AssetDatabase.FindAssets("t:Brainrot", new[] { brPath });

        foreach (string guid in brGUIDs)
        {
            var br = AssetDatabase.LoadAssetAtPath<Brainrot>(AssetDatabase.GUIDToAssetPath(guid));
            var pf = br ? br.characterPrefab : null;

            bool inside = false;
            if (pf)
            {
                string path = AssetDatabase.GetAssetPath(pf);
                inside = path.StartsWith(pfPath);
            }

            items.Add(new Item { br = br, prefab = pf, inFolder = inside });
        }

        // лог в консоль
        foreach (var it in items)
        {
            if (it.missing)
                Debug.LogWarning($"[Checker] {it.br.name}: префаб НЕ назначен.");
            else if (it.wrongFolder)
                Debug.LogWarning($"[Checker] {it.br.name}: префаб «{it.prefab.name}» лежит НЕ в папке {pfPath}");
        }

        int ok = items.Count(i => !i.missing && !i.wrongFolder);
        Debug.Log($"[Checker] Сканирование завершено. OK: {ok}, Missing: {items.Count - ok}");
    }

    private bool ValidateFolders()
    {
        if (brainrotFolder == null || prefabFolder == null)
        {
            EditorUtility.DisplayDialog("Folders not set", "Укажите обе папки!", "OK");
            return false;
        }
        return true;
    }

    // ────────── таблица результатов ──────────
    private void DrawTable()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Brainrot", GUILayout.Width(200));
        GUILayout.Label("Prefab",   GUILayout.Width(200));
        GUILayout.Label("Status",   GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(250));

        foreach (var it in items)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.ObjectField(it.br, typeof(Brainrot), false, GUILayout.Width(200));
            EditorGUILayout.ObjectField(it.prefab, typeof(GameObject), false, GUILayout.Width(200));

            string status = it.missing      ? "Ø" :
                            it.wrongFolder  ? "⚠" :
                                              "✔";
            GUILayout.Label(status, GUILayout.Width(60));

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }
}
#endif
