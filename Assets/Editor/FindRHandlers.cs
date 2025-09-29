#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public static class FindRHandlers
{
    [MenuItem("Tools/Input/Find 'R' handlers")]
    public static void FindR()
    {
        string[] codePatterns = {
            "KeyCode.R", "GetKeyDown(KeyCode.R)", "GetKeyUp(KeyCode.R)", "GetKey(KeyCode.R)",
            "Keyboard.current.rKey"
        };

        foreach (var guid in AssetDatabase.FindAssets("t:Script"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".cs")) continue;
            var text = File.ReadAllText(path);
            if (codePatterns.Any(p => text.Contains(p)))
                Debug.Log($"R-handler candidate in: {path}");
        }

        // Поиск бинда R в Input Actions
        foreach (var guid in AssetDatabase.FindAssets("t:InputActionAsset"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var text = File.ReadAllText(path);
            if (text.Contains("\"<Keyboard>/r\""))
                Debug.Log($"R binding found in Input Actions: {path}");
        }
    }
}
#endif
