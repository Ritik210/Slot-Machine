using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Bet selection dropdown. Dollar amounts are configured in the Inspector.
/// Updates Server.bet in credits (dollarAmount × 20).
/// </summary>
public class BetController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown betDropdown;
    [SerializeField] private Server server;

    [Header("Bet Options (in dollars)")]
    [SerializeField] private float[] betOptions = { 0.5f, 1f, 2f, 5f, 10f, 20f };

    [Header("Default")]
    [SerializeField] private int defaultIndex = 1; // 1$ by default

    private void Awake()
    {
        SetupDropdown();
        ApplyBet(defaultIndex);
    }

    private void SetupDropdown()
    {
        if (betDropdown == null) return;

        betDropdown.ClearOptions();

        List<string> options = new List<string>();
        for (int i = 0; i < betOptions.Length; i++)
            options.Add("$" + betOptions[i].ToString("F2"));

        betDropdown.AddOptions(options);
        betDropdown.value = defaultIndex;
        betDropdown.RefreshShownValue();

        betDropdown.onValueChanged.AddListener(OnDropdownChanged);
    }

    private void OnDropdownChanged(int index)
    {
        ApplyBet(index);
    }

    private void ApplyBet(int index)
    {
        index = Mathf.Clamp(index, 0, betOptions.Length - 1);
        float dollarBet = betOptions[index];
        server.SetBet(dollarBet);
    }

    private void OnDestroy()
    {
        if (betDropdown != null)
            betDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
    }
}
