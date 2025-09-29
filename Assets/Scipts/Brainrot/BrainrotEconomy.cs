// Assets/Scripts/Brainrot/BrainrotEconomy.cs
using UnityEngine;
using System;

public class BrainrotEconomy : MonoBehaviour
{
    [Header("Выходные значения")]
    public float kg;              // сролленный вес (1..10 кг с округлением до сотых)
    public float incomePerSec;    // вычисленный доход ($/s)

    /// <summary>
    /// Основной метод: считает вес, масштаб и доход.
    /// Вызывается один раз при появлении финального бота.
    /// </summary>
    public void SetupFrom(Brainrot so, CharacterRarity rarity, Transform modelRoot,
                          float extraMult = 1f, float baseAdd = 1.0f, bool applyScale = true)
    {
        if (!so || !modelRoot) return;

        // 1) Роллим вес (можешь заменить на свой распределитель)
        kg = RollWeight(); // 1..10 кг

        // 2) Масштаб модели (линейно). Пример: +5% / кг
        if (applyScale)
        {
            float scale = so.baseScale * (1f + kg * so.scalePerKg);
            modelRoot.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        }

        // 3) Доход: (baseAdd + K*kg) * Множитель_редкости * доп. множители
        float kPerKg = Mathf.Max(so.kPerKgBasic, 0.001f);
        float rarityMult = GetRarityMult(rarity);
        float raw = (baseAdd + kPerKg * kg) * rarityMult * Mathf.Max(extraMult, 0.0001f);

        // округляем «как в UI» — до десятых
        incomePerSec = Mathf.Round(raw * 10f) / 10f;
    }

    // ---- утилиты ----

    // неофициальное правдоподобное распределение веса
    public static float RollWeight()
    {
        float u = UnityEngine.Random.value;
        float v;
        if (u < 0.40f)       v = Lerp(1.00f, 3.00f, u / 0.40f);
        else if (u < 0.75f)  v = Lerp(3.00f, 6.00f, (u - 0.40f) / 0.35f);
        else if (u < 0.95f)  v = Lerp(6.00f, 8.50f, (u - 0.75f) / 0.20f);
        else if (u < 0.99f)  v = Lerp(8.50f, 9.50f, (u - 0.95f) / 0.04f);
        else                 v = Lerp(9.50f, 10.0f, (u - 0.99f) / 0.01f);

        return (float)Math.Round(Mathf.Clamp(v, 1f, 10f), 2);
    }

    // множитель редкости (подстрой под баланс)
    static float GetRarityMult(CharacterRarity r)
    {
        switch (r)
        {
            case CharacterRarity.Epic:       return 32f;
            case CharacterRarity.Legendary:  return 64f;
            case CharacterRarity.Mythic:     return 220f;
            case CharacterRarity.God:        return 350f;
            case CharacterRarity.Secret:     return 500f;
            case CharacterRarity.Rare:       return 2f;
            default:                         return 1f;  // Common
        }
    }

    static float Lerp(float a, float b, float t) => a + (b - a) * Mathf.Clamp01(t);
}
