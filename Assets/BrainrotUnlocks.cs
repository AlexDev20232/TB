// Assets/Scripts/Brainrot/BrainrotUnlocks.cs
using System.Collections.Generic;
using YG;
using UnityEngine; // <-- добавь для Debug.Log

public static class BrainrotUnlocks
{
    public static string InternalKey(Brainrot b) => b ? $"{b.name}_{b.type}" : string.Empty;

    private static List<string> List
    {
        get
        {
            if (YG2.saves.unlocked == null)
                YG2.saves.unlocked = new List<string>();
            return YG2.saves.unlocked;
        }
    }

    public static bool IsUnlocked(Brainrot b)
    {
        if (!b) return false;
        return List.Contains(InternalKey(b));
    }

    public static bool TryUnlockNew(Brainrot b)
    {
        if (!b) { Debug.Log("[NEWPET] TryUnlockNew: Brainrot is null"); return false; }
        var key = InternalKey(b);
        var list = List;

        bool already = list.Contains(key);
        Debug.Log($"[NEWPET] TryUnlockNew: key={key} already={already} listCount={list.Count}");

        if (already) return false;

        list.Add(key);
        SaveBridge.Save();
        Debug.Log($"[NEWPET] TryUnlockNew: ADDED key={key} -> total={list.Count}");
        OnUnlocked?.Invoke(key);
        return true;
    }

    public static bool ForceUnlock(Brainrot b)
    {
        if (!b) return false;
        var key = InternalKey(b);
        var list = List;
        if (list.Contains(key))
        {
            Debug.Log($"[NEWPET] ForceUnlock: key={key} ALREADY; skip");
            return false;
        }
        list.Add(key);
        SaveBridge.Save();
        Debug.Log($"[NEWPET] ForceUnlock: ADDED key={key} -> total={list.Count}");
        OnUnlocked?.Invoke(key);
        return true;
    }

    public static event System.Action<string> OnUnlocked;
}
