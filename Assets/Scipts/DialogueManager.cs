// Assets/Scripts/NPCDialogue/DialogueManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("Тексты над головами")]
    public TextMeshProUGUI playerText;   // TMP над игроком
    public TextMeshProUGUI npcText;      // TMP над NPC

    [Header("Длительности, сек")]
    [Min(0f)] public float playerDuration = 2f; // сколько держим реплику игрока
    [Min(0f)] public float npcDuration    = 4f; // сколько держим реплику NPC

    [Header("API к хотбару (кто в руках/на поводке)")]
    public HotbarBrainrotAPI hotbar;     // обёртка для выбранного брайрота

    /// <summary>Событие: диалог/действие полностью завершено (можно вернуть подсказку E-Talk).</summary>
    public System.Action OnConversationFinished;

    Coroutine _seq;

    void Awake()
    {
        if (!hotbar) hotbar = FindObjectOfType<HotbarBrainrotAPI>(true);
    }

    /// <summary>
    /// Вызывается из кнопок. Для 1/2/3 — спец-логика, иначе обычная последовательность игрок→NPC.
    /// </summary>
    public void ShowDialogue(int choiceIndex, string playerLine, string defaultNpcLine)
    {
        if (_seq != null) StopCoroutine(_seq);

        switch (choiceIndex)
        {
            case 1: // продать весь инвентарь
                _seq = StartCoroutine(SellInventorySequence(playerLine));
                break;

            case 2: // продать только то, что в руках/на поводке
                _seq = StartCoroutine(SellEquippedSequence(playerLine));
                break;

            case 3: // оценить то, что в руках/на поводке (без продажи)
                _seq = StartCoroutine(AppraiseEquippedSequence(playerLine));
                break;

            default: // обычный вариант: игрок → NPC
                _seq = StartCoroutine(SimpleSequence(playerLine, defaultNpcLine));
                break;
        }
    }

    // ───────────────────────── базовая последовательность ─────────────────────────
    IEnumerator SimpleSequence(string pLine, string nLine)
    {
        SetActiveSafe(playerText, false);
        SetActiveSafe(npcText,   false);

        if (playerText) { playerText.text = pLine ?? ""; SetActiveSafe(playerText, true); }
        yield return new WaitForSeconds(playerDuration);
        SetActiveSafe(playerText, false);

        if (npcText) { npcText.text = nLine ?? ""; SetActiveSafe(npcText, true); }
        yield return new WaitForSeconds(npcDuration);
        SetActiveSafe(npcText, false);

        _seq = null;
        OnConversationFinished?.Invoke(); // ← сигнал «всё закончено»
    }

    // ───────────────────────── кнопка 1: продать весь инвентарь ─────────────────────────
    IEnumerator SellInventorySequence(string playerLine)
    {
        SetActiveSafe(playerText, false);
        SetActiveSafe(npcText,   false);

        if (playerText) { playerText.text = playerLine ?? ""; SetActiveSafe(playerText, true); }
        yield return new WaitForSeconds(playerDuration);
        SetActiveSafe(playerText, false);

        long total = ComputeSellPriceFromInventory(out int brainsCount);
        string reply = (brainsCount == 0 || total <= 0) ? "Nothing to buy..." : $"Cost: {FormatMoney(total)}";

        if (brainsCount > 0 && total > 0)
            ApplySellInventory(total); // начислить деньги + удалить всех брайротов

        if (npcText) { npcText.text = reply; SetActiveSafe(npcText, true); }
        yield return new WaitForSeconds(npcDuration);
        SetActiveSafe(npcText, false);

        _seq = null;
        OnConversationFinished?.Invoke();
    }

    // ───────────────────────── кнопка 2: продать «в руках» ─────────────────────────
    IEnumerator SellEquippedSequence(string playerLine)
    {
        SetActiveSafe(playerText, false);
        SetActiveSafe(npcText,   false);

        if (playerText) { playerText.text = playerLine ?? ""; SetActiveSafe(playerText, true); }
        yield return new WaitForSeconds(playerDuration);
        SetActiveSafe(playerText, false);

        if (!TryGetEquipped(out var so, out var type, out var kg, out var inst))
        {
            if (npcText) { npcText.text = "Bring me a brainrot first..."; SetActiveSafe(npcText, true); }
            yield return new WaitForSeconds(npcDuration);
            SetActiveSafe(npcText, false);

            _seq = null;
            OnConversationFinished?.Invoke();
            yield break;
        }

        long price = ComputePriceFor(so, type, kg);
        if (price > 0) ApplySellEquipped(so, type, kg, price); // деньги + удалить из инвентаря + убрать визуал/поводок

        string reply = (price > 0) ? $"Cost: {FormatMoney(price)}" : "Nothing to buy...";
        if (npcText) { npcText.text = reply; SetActiveSafe(npcText, true); }
        yield return new WaitForSeconds(npcDuration);
        SetActiveSafe(npcText, false);

        _seq = null;
        OnConversationFinished?.Invoke();
    }

    // ───────────────────────── кнопка 3: оценить «в руках» (без продажи) ─────────────────────────
    IEnumerator AppraiseEquippedSequence(string playerLine)
    {
        SetActiveSafe(playerText, false);
        SetActiveSafe(npcText,   false);

        if (playerText) { playerText.text = playerLine ?? ""; SetActiveSafe(playerText, true); }
        yield return new WaitForSeconds(playerDuration);
        SetActiveSafe(playerText, false);

        if (!TryGetEquipped(out var so, out var type, out var kg, out var inst))
        {
            if (npcText) { npcText.text = "Bring me a brainrot first..."; SetActiveSafe(npcText, true); }
            yield return new WaitForSeconds(npcDuration);
            SetActiveSafe(npcText, false);

            _seq = null;
            OnConversationFinished?.Invoke();
            yield break;
        }

        long price = ComputePriceFor(so, type, kg);
        string reply = (price > 0) ? $"Worth: {FormatMoney(price)}" : "Can't appraise this...";
        if (npcText) { npcText.text = reply; SetActiveSafe(npcText, true); }
        yield return new WaitForSeconds(npcDuration);
        SetActiveSafe(npcText, false);

        _seq = null;
        OnConversationFinished?.Invoke();
    }

    // ========== расчёты/применение ==========

    // Цена продажи одного брайрота (только того, кто в руках)
    long ComputePriceFor(Brainrot so, StandardType type, float kg)
    {
        if (!so) return 0;

        float incomePerSec = EvaluateIncome(so, type, kg);  // твоя формула дохода
        float mult = type switch
        {
            StandardType.Gold    => 20f,
            StandardType.Diamond => 15f,
            // StandardType.Candy   => 10f, // если понадобится — добавить
            _                    => 30f,   // Standard
        };
        return Mathf.RoundToInt(incomePerSec * mult);
    }

    long ComputeSellPriceFromInventory(out int brainsCount)
    {
        brainsCount = 0;
        var inv = EggInventory.Instance;
        if (inv == null) return 0;

        List<EggStack> items = inv.GetItemsSnapshot();
        long total = 0;

        foreach (var s in items)
        {
            if (!s.IsBrainrot) continue;
            brainsCount++;

            float incomePerSec = EvaluateIncome(s.brainrot, s.type, s.weightKg);
            float mult = s.type switch
            {
                StandardType.Gold    => 20f,
                StandardType.Diamond => 15f,
                // StandardType.Candy   => 10f,
                _                    => 30f,
            };
            total += Mathf.RoundToInt(incomePerSec * mult);
        }
        return total;
    }

    void ApplySellInventory(long price)
    {
        var gm = GameManager.Instance;
        if (gm != null) gm.AddMoney((int)price);

        var inv = EggInventory.Instance;
        if (inv == null) return;

        var snap = inv.GetItemsSnapshot();
        foreach (var s in snap)
            if (s.IsBrainrot) inv.RemoveBrainrot(s.brainrot, s.type, s.weightKg);
    }

    void ApplySellEquipped(Brainrot so, StandardType type, float kg, long price)
    {
        var gm = GameManager.Instance;
        if (gm != null) gm.AddMoney((int)price);

        var inv = EggInventory.Instance;
        if (inv) inv.RemoveBrainrot(so, type, kg);

        if (hotbar != null) hotbar.HideEquipped(); // убрать визуал из руки/поводка и сбросить состояние хотбара
    }

    // твоя же формула дохода (как в TradeGenerator)
    float EvaluateIncome(Brainrot so, StandardType type, float kg)
    {
        if (!so) return 0f;
        float baseAdd = 1.0f;
        float k = Mathf.Max(so.kPerKgBasic, 0.0001f);
        float typeMult = type switch
        {
            StandardType.Gold    => 1.25f,
            StandardType.Diamond => 1.5f,
            StandardType.Candy   => 4f,
            _                    => 1f,
        };
        float rarMult = so.rarity switch
        {
            CharacterRarity.Rare      => 2f,
            CharacterRarity.Epic      => 32f,
            CharacterRarity.Legendary => 64f,
            CharacterRarity.Mythic    => 220f,
            CharacterRarity.God       => 350f,
            CharacterRarity.Secret    => 500f,
            _                         => 1f,
        };
        return Mathf.Max(0.1f, (baseAdd + k * kg) * rarMult * typeMult);
    }

    string FormatMoney(long value)
    {
        if (value >= 1_000_000_000) return (value / 1_000_000_000f).ToString("0.##") + "$B";
        if (value >= 1_000_000)     return (value / 1_000_000f).ToString("0.##") + "$M";
        if (value >= 1_000)         return (value / 1_000f).ToString("0.##") + "$K";
        return value.ToString("N0") + "$";
    }

    // безопасное включение/выключение TMP
    void SetActiveSafe(TextMeshProUGUI t, bool v)
    {
        if (!t) return;
        if (t.gameObject.activeSelf != v)
            t.gameObject.SetActive(v);
    }

    void OnDisable()
    {
        if (_seq != null) StopCoroutine(_seq);
        _seq = null;
        SetActiveSafe(playerText, false);
        SetActiveSafe(npcText,   false);
    }

    // проверка — есть ли пэт в руках; допускаем SO или (инстанс + осмысленный вес)
    bool TryGetEquipped(out Brainrot so, out StandardType type, out float kg, out GameObject inst)
    {
        so = null; type = StandardType.Standard; kg = 0f; inst = null;
        if (!hotbar) return false;

        so   = hotbar.EquippedBrainrotSO();
        type = hotbar.EquippedBrainrotType();
        kg   = hotbar.EquippedBrainrotKg();
        inst = hotbar.CurrentPetInstance();

        return (so != null) || (inst != null && kg > 0.0001f);
    }
}
