using UnityEngine;
using TMPro;
using System.Collections;

public class IncomeDisplay : MonoBehaviour
{
    [Header("Настройки")]
    public int incomePerSecond;

    [Header("UI")]
    public TextMeshProUGUI UpdateIncome;   // "$123"

    private Coroutine _loop;
    private int _currentIncome;
    public int CurrentIncome => _currentIncome;

    public void Init(int income)
    {
        incomePerSecond = income;
        if (_loop != null) StopCoroutine(_loop);
        UpdateIncomeText();
        _loop = StartCoroutine(IncomeLoop());
    }

    public void SetIncome(int value)
    {
        _currentIncome = Mathf.Max(0, value);
        UpdateIncomeText();
    }

    public void ResetAmount()
    {
        _currentIncome = 0;
        UpdateIncomeText();
    }

    public static void ResetAllInScene()
    {
        foreach (var id in FindObjectsOfType<IncomeDisplay>(true))
            //id.ResetAmount();
            Destroy(id.gameObject); // удаляем IncomeDisplay, чтобы не было лишних объектов
    }

    public void ResetIncome()
    {
        _currentIncome = 0;
        UpdateIncomeText();
        SaveBridge.MarkDirty();
    }

    public int GetCurrentIncome() => _currentIncome;

    /// <summary>ВЗЯТЬ всю сумму и мгновенно обнулить (атомарно).</summary>
    public int TakeIncome()
    {
        int value = _currentIncome;
        if (value <= 0) return 0;

        _currentIncome = 0;
        UpdateIncomeText();
        SaveBridge.MarkDirty();
        return value;
    }

    private void AddIncome()
    {
        float mult = MoneyBoostManager.Instance ? MoneyBoostManager.Instance.CurrentMultiplier : 1f;
        int delta = Mathf.CeilToInt(incomePerSecond * mult);
        _currentIncome += delta;
        UpdateIncomeText(delta);
        SaveBridge.MarkDirty();
    }

    private IEnumerator IncomeLoop()
    {
        var wait = new WaitForSeconds(1f);
        while (true)
        {
            yield return wait;
            AddIncome();
        }
    }

    private void OnDisable()
    {
        if (_loop != null) StopCoroutine(_loop);
        _loop = null;
    }

    private void UpdateIncomeText(int deltaPerSec = -1)
    {
        if (deltaPerSec < 0)
        {
            float mult = MoneyBoostManager.Instance ? MoneyBoostManager.Instance.CurrentMultiplier : 1f;
            deltaPerSec = Mathf.CeilToInt(incomePerSecond * mult);
        }

        if (UpdateIncome)
            UpdateIncome.text = "$" + MoneyFormatter.Short(_currentIncome);
    }
}
