#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Окно поиска всех упоминаний нужной клавиши
/// (KeyCode.X, Input.GetKey*("x")) в C#-скриптах проекта.
/// </summary>
public class KeyUsageFinder : EditorWindow
{
    [MenuItem("Tools/Key Usage Finder")]
    private static void Open() => GetWindow<KeyUsageFinder>("Key Usage Finder");

    // ───────── UI state ─────────
    private string _key = "R";                       // какой символ ищем
    private Vector2 _scroll;
    private readonly List<Match> _matches = new();   // результаты

    private struct Match
    {
        public string path;
        public int    line;
        public string snippet;
    }

    // ───────── UI ─────────
    private void OnGUI()
    {
        GUILayout.Label("Поиск обработчиков клавиш", EditorStyles.boldLabel);
        _key = EditorGUILayout.TextField("Key (single char)", _key).Trim();

        if (GUILayout.Button("Scan")) Scan();

        EditorGUILayout.Space();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var m in _matches)
        {
            if (GUILayout.Button($"{Path.GetFileName(m.path)}:{m.line}    {m.snippet.Trim()}",
                                  EditorStyles.miniButton))
            {
                // откроет файл и поставит курсор на строку
                AssetDatabase.OpenAsset(
                    AssetDatabase.LoadAssetAtPath<MonoScript>(m.path), m.line);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    // ───────── core ─────────
    private void Scan()
    {
        _matches.Clear();
        if (string.IsNullOrEmpty(_key)) return;

        string upper = _key.ToUpper();
        string lower = _key.ToLower();

        // шаблоны поиска
        string[] patterns =
        {
            $"KeyCode.{upper}",
            $".GetKeyDown(\"{upper}\")",
            $".GetKeyDown(\"{lower}\")",
            $".GetKey(\"{upper}\")",
            $".GetKey(\"{lower}\")",
            $".GetKeyUp(\"{upper}\")",
            $".GetKeyUp(\"{lower}\")"
        };

        foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".cs")) continue;

            var lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (patterns.Any(p => line.Contains(p)))
                    _matches.Add(new Match { path = path, line = i + 1, snippet = line });
            }
        }

        if (_matches.Count == 0)
            Debug.Log($"[KeyUsageFinder] «{_key}» не найдена ни в одном .cs файле.");
    }
}
#endif
