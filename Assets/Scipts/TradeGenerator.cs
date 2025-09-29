using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class TradeGenerator
{
    static float RarityMult(CharacterRarity r) => r switch
    {
        CharacterRarity.Common    => 1f,
        CharacterRarity.Rare      => 2f,
        CharacterRarity.Epic      => 32f,
        CharacterRarity.Legendary => 64f,
        CharacterRarity.Mythic    => 220f,
        CharacterRarity.God       => 350f,
        CharacterRarity.Secret    => 500f,
        _                         => 1f
    };

    public static float EvaluateValue(Brainrot so, StandardType type, float kg, NPCTradePool pool = null)
    {
        float baseAdd = 1.0f;
        float k = Mathf.Max(so.kPerKgBasic, 0.0001f);
        float typeMult = pool ? pool.TypeMult(type) : 1f;
        float rarMult  = RarityMult(so.rarity);
        return Mathf.Max(0.1f, (baseAdd + k * kg) * rarMult * typeMult);
    }

    static Sprite IconFrom(Brainrot so)
    {
        if (!so) return null;
        if (so.icon) return so.icon;
        if (so.iconPrefab)
        {
            var set = so.iconPrefab.GetComponent<BrainrotIconSet>();
            if (set && set.skins != null && set.skins.Length > 0) return set.skins[0];
        }
        return null;
    }

    // Базовая генерация — выберем коридор случайно
    public static TradeOffer CreateOffer(EggInventory inv, NPCTradePool pool, int maxPerSide, float npcMarginIgnored)
        => CreateOfferInBand(inv, pool, maxPerSide, ChooseBandRandom());

    // Создать оффер внутри заданного коридора относительной разницы (например, +0.20..+0.40)
    public static TradeOffer CreateOfferInBand(EggInventory inv, NPCTradePool pool, int maxPerSide, Vector2 relBand)
    {
        // подрезаем на всякий случай к [-0.20 .. +0.40]
        relBand.x = Mathf.Clamp(relBand.x, -0.20f, 0.40f);
        relBand.y = Mathf.Clamp(relBand.y, -0.20f, 0.40f);

        var offer = new TradeOffer();
        if (inv == null || pool == null || pool.candidates == null || pool.candidates.Length == 0)
            return offer;

        var playerStacks = inv.GetItemsSnapshot().Where(s => s.IsBrainrot).ToList();
        if (playerStacks.Count == 0) return offer;

        // --- Левая сторона (игрок) ---
        int pc = Random.Range(1, Mathf.Min(maxPerSide, playerStacks.Count) + 1);
        var tmp = new List<EggStack>(playerStacks);
        for (int i = 0; i < pc; i++)
        {
            int id = Random.Range(0, tmp.Count);
            var s = tmp[id]; tmp.RemoveAt(id);

            float val = EvaluateValue(s.brainrot, s.type, s.weightKg, pool);
            offer.playerGive.Add(new TradeItem { so = s.brainrot, type = s.type, weightKg = s.weightKg, value = val, icon = s.icon });
            offer.playerValue += val;
        }

        var maxPlayerRarity = offer.playerGive.Max(t => t.so.rarity);
        var npcCands = pool.candidates
                           .Where(so => so && (int)so.rarity <= (int)maxPlayerRarity + pool.maxRarityDelta)
                           .ToArray();
        if (npcCands.Length == 0) npcCands = pool.candidates.Where(so => so).ToArray();

        float Pv = Mathf.Max(1f, offer.playerValue);

        float lowerNv = Pv * (1f + relBand.x);
        float upperNv = Pv * (1f + relBand.y);
        float targetNv = Random.Range(lowerNv, upperNv);

        float tol = 0.06f * Pv; // допуск

        // --- Правая сторона (NPC) ---
        int nc = Random.Range(1, maxPerSide + 1);
        float sum = 0f;
        int guard = 0;

        while (offer.npcGive.Count < nc && guard++ < 250)
        {
            var so = npcCands[Random.Range(0, npcCands.Length)];
            var type = (StandardType)Random.Range(0, 4);
            float kg = BrainrotEconomy.RollWeight();
            float val = EvaluateValue(so, type, kg, pool);

            if (sum + val > upperNv + tol) continue;

            offer.npcGive.Add(new TradeItem { so = so, type = type, weightKg = kg, value = val, icon = IconFrom(so) });
            sum += val;

            if (sum >= lowerNv && sum <= upperNv && Mathf.Abs(sum - targetNv) <= tol) break;
        }

        // добор, если недотянули
        if ((sum < lowerNv || offer.npcGive.Count == 0) && npcCands.Length > 0)
        {
            int tries = 0;
            while (sum < lowerNv && tries++ < 200)
            {
                var so = npcCands[Random.Range(0, npcCands.Length)];
                var type = (StandardType)Random.Range(0, 4);
                float kg = BrainrotEconomy.RollWeight();
                float val = EvaluateValue(so, type, kg, pool);

                if (sum + val <= upperNv + tol)
                {
                    offer.npcGive.Add(new TradeItem { so = so, type = type, weightKg = kg, value = val, icon = IconFrom(so) });
                    sum += val;
                }
                if (offer.npcGive.Count >= nc) break;
            }

            if (offer.npcGive.Count == 0)
            {
                var so = npcCands[Random.Range(0, npcCands.Length)];
                var type = (StandardType)Random.Range(0, 4);
                float kg = BrainrotEconomy.RollWeight();
                float v  = EvaluateValue(so, type, kg, pool);
                offer.npcGive.Add(new TradeItem { so = so, type = type, weightKg = kg, value = Mathf.Min(v, upperNv * 0.95f), icon = IconFrom(so) });
                sum = offer.npcGive[0].value;
            }
        }

        offer.npcValue = sum;
        Debug.Log($"OFFER gen: Pv={offer.playerValue:0.000}  Nv={offer.npcValue:0.000}  rel={(offer.FairnessRel*100f):0.00}%");
        return offer;
    }

    // Fair ~60%, Bad ~30%, Good ~10%; крайние подрезаны до -20%..+40%
    static Vector2 ChooseBandRandom()
    {
        float r = Random.value;
        Vector2 band;
        if      (r < 0.60f) band = new Vector2(+0.00f, +0.03f);  // Fair:   0..+3%
        else if (r < 0.90f) band = new Vector2(-0.20f, -0.01f);  // Bad:  -20..-1%
        else                band = new Vector2(+0.03f, +0.40f);  // Good: +3..+40%

        if (band.x >= +0.03f)
        {
            float g = Random.value;
            band = (g < 0.70f) ? new Vector2(+0.20f, +0.40f) : new Vector2(+0.03f, +0.20f);
        }
        return band;
    }
}
