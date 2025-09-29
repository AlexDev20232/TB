using UnityEngine;

public class PurhaseDetector : MonoBehaviour
{
    [SerializeField] private GameObject purchasePromptPrefab;
    public Vector3 positionOffset;
    public bool makeChild = true;

    private GameObject currentPrompt;  // объект яйца под прицелом триггера
    private GameObject lastCanvas;     // инстанс UI-подсказки

    private void Update()
    {
        if (currentPrompt == null) return;

        var mover = currentPrompt.GetComponent<BrainrotMover>();
        var egg   = currentPrompt.GetComponent<EggController>();
        if (mover == null || egg == null) return;

        // ──────────────────────────────────────────────
        // ВАЖНО: если яйцо уже ПОСАЖЕНО на слот — продажу тут НЕ делаем и НЕ обрабатываем E
        // ──────────────────────────────────────────────
        if (mover.currentState == BrainrotMover.MoveState.Positioning)
            return;

        // покупка (яйцо ещё НЕ на слоте)
        if (Input.GetKeyDown(KeyCode.E))
        {
            var so = egg.GetStats();
            if (so == null) return;

            // проверка денег
            if (so.price > GameManager.Instance.GetMoney())
            {
                GameManager.Instance.ErrorMessage("Не хватает денег на покупку!");
                return;
            }

            // проверка инвентаря
            if (EggInventory.Instance == null)
            {
                Debug.LogError("[Purchase] EggInventory.Instance == null — повесь EggInventory в сцену!");
                GameManager.Instance.ErrorMessage("Инвентарь недоступен");
                return;
            }

            // сначала пробуем положить в инвентарь
            bool added = EggInventory.Instance.AddEgg(egg.egg, egg.eggType, 1);
            Debug.Log($"[Purchase] AddEgg({egg.egg?.name}, {egg.eggType}) => {added}");
            if (!added)
            {
                GameManager.Instance.ErrorMessage("Инвентарь заполнен (макс. 5 слотов)");
                return;
            }

            // списываем деньги
            GameManager.Instance.AddMoney(-so.price);
            egg.MarkBought();

            // убираем подсказку и объект яйца из мира
            if (lastCanvas) { Destroy(lastCanvas); lastCanvas = null; }
            var boughtObj = currentPrompt;
            currentPrompt = null;
            Destroy(boughtObj);

            // сейв
            SaveBridge.SnapshotAndSave(force: true);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // работаем только с ЯЙЦОМ
        if (!other.TryGetComponent<EggController>(out _)) return;

        var mover = other.GetComponent<BrainrotMover>();
        if (mover == null) return;

        // Если уже ПОСАЖЕНО (Positioning) — подсказку НЕ показываем вообще (и чистим старую)
        if (mover.currentState == BrainrotMover.MoveState.Positioning)
        {
            if (currentPrompt == other.gameObject && lastCanvas)
            { Destroy(lastCanvas); lastCanvas = null; }

            // сбрасываем ссылку, чтобы Update не реагировал на E в этом состоянии
            if (currentPrompt == other.gameObject) currentPrompt = null;
            return;
        }

        // Если едет к базе — тоже не показываем (как и раньше)
        if (mover.currentState == BrainrotMover.MoveState.MovingToBase)
        {
            if (lastCanvas) { Destroy(lastCanvas); lastCanvas = null; }
            return;
        }

        // Иначе (Walking / статика витрины) — это КУПИТЬ: показываем подсказку
        currentPrompt = other.gameObject;
        SpawnAtCenter(isPositioning: false); // ← чаше всего покупка = не позиционирование
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent<EggController>(out _)) return;
        if (currentPrompt == other.gameObject)
        {
            currentPrompt = null;
            if (lastCanvas) { Destroy(lastCanvas); lastCanvas = null; }
        }
    }

    private void SpawnAtCenter(bool isPositioning)
    {
        if (currentPrompt == null || purchasePromptPrefab == null) return;

        // вычисляем центр модели
        MeshRenderer[] mesh   = currentPrompt.GetComponentsInChildren<MeshRenderer>();
        SkinnedMeshRenderer[] skin = currentPrompt.GetComponentsInChildren<SkinnedMeshRenderer>();
        Renderer[] renderers = new Renderer[mesh.Length + skin.Length];
        mesh.CopyTo(renderers, 0);
        skin.CopyTo(renderers, mesh.Length);

        if (renderers.Length == 0)
        {
            Debug.LogError("Не найдено рендереров для определения центра!");
            return;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);

        Vector3 spawnPosition = bounds.center + positionOffset;

        if (lastCanvas == null)
            lastCanvas = Instantiate(purchasePromptPrefab, spawnPosition, Quaternion.identity);
        else
            lastCanvas.transform.position = spawnPosition;

        if (makeChild) lastCanvas.transform.SetParent(currentPrompt.transform, true);

        // настраиваем текст подсказки как «покупка»
        var pp    = lastCanvas.GetComponentInChildren<PurchasePrompt>();
        var stats = currentPrompt.GetComponent<EggController>().GetStats();
        if (pp != null && stats != null)
        {
            pp.SetName(stats.EggName);
            pp.SetWalkingMode(stats.price);   // показываем «Купить: N$»
            // ВАЖНО: подсказка ПРОДАЖИ здесь не нужна — не вызываем SetPositioningMode
        }
    }
}
