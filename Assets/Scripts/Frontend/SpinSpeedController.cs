using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Central speed controller for all slot animations.
/// Attach to a GameObject and assign a TMP_Dropdown in the Inspector.
/// The dropdown is auto-populated with 1x / 2x / 4x / 8x options.
///
/// Every timing-aware script calls SpinSpeedController.Apply(duration)
/// to scale its waits by the current multiplier.
/// </summary>
public class SpinSpeedController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown speedDropdown;

    [Header("Default Speed")]
    [SerializeField] private int defaultIndex = 0; // 0 = 1x, 1 = 2x, 2 = 4x, 3 = 8x

    private static float multiplier = 1f;
    private static readonly int[] speedValues = { 1, 2, 4 };

    /// <summary> Current speed multiplier (1, 2, 4, or 8). </summary>
    public static float Multiplier => multiplier;

    /// <summary> Divide any duration by the current speed. </summary>
    public static float Apply(float duration)
    {
        return duration / multiplier;
    }

    private void Awake()
    {
        SetupDropdown();
        ApplyIndex(defaultIndex);
    }

    private void SetupDropdown()
    {
        if (speedDropdown == null) return;

        speedDropdown.ClearOptions();

        List<string> options = new List<string>();
        for (int i = 0; i < speedValues.Length; i++)
            options.Add(speedValues[i] + "x");

        speedDropdown.AddOptions(options);
        speedDropdown.value = defaultIndex;
        speedDropdown.RefreshShownValue();

        speedDropdown.onValueChanged.AddListener(OnDropdownChanged);
    }

    private void OnDropdownChanged(int index)
    {
        ApplyIndex(index);
    }

    private void ApplyIndex(int index)
    {
        index = Mathf.Clamp(index, 0, speedValues.Length - 1);
        multiplier = speedValues[index];
    }

    private void OnDestroy()
    {
        if (speedDropdown != null)
            speedDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
    }
}
