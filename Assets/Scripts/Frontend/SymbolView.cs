using UnityEngine;
using System.Collections;

/// <summary>One grid cell: sprite display plus win-highlight VFX.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SymbolView : MonoBehaviour
{
    [Header("Symbol Settings")]
    [SerializeField, ReadOnly] private SpriteRenderer icon;

    private Sprite[] symbolSprites;
    private SlotLayoutConfig layoutConfig;

    [Header("Position Info (set per-symbol)")]
    [SerializeField, ReadOnly] int reelIndex;
    [SerializeField, ReadOnly] int rowIndex;

    [Header("Highlight Effect")]
    [SerializeField] private GameObject highlightEffectPrefab;

    [Header("Highlight Animation")]
    [SerializeField] private float highlightFadeInDuration = 0.15f;
    [SerializeField] private float highlightFadeOutDuration = 0.2f;

    private GameObject activeHighlight;
    private SpriteRenderer activeHighlightRenderer;
    private Coroutine highlightAnimRoutine;
    private int currentSymbolId = 0;

    private void Awake()
    {
        icon = GetComponent<SpriteRenderer>();
    }

    public void SetPosition(int reel, int row)
    {
        this.reelIndex = reel;
        this.rowIndex = row;
    }

    // Set by ReelView during initialization
    public void SetSymbolSprites(Sprite[] sprites)
    {
        symbolSprites = sprites;
    }

    public void SetLayoutConfig(SlotLayoutConfig config)
    {
        layoutConfig = config;
    }

    public void SetSymbol(int symbolId)
    {
        currentSymbolId = symbolId;
        if (symbolSprites == null || symbolId < 0 || symbolId >= symbolSprites.Length)
        {
            Debug.LogWarning($"[SymbolView] Invalid symbol ID: {symbolId}");
            return;
        }

        icon.sprite = symbolSprites[symbolId];
        SetVisible(true);
    }

    /// <summary>
    /// Show or hide this symbol cell. Used during tumble cascades
    /// to blank out winning/empty positions.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (icon != null)
            icon.enabled = visible;
    }

    public int GetCurrentSymbolId()
    {
        return currentSymbolId;
    }

    private void OnEnable()
    {
        SlotEventHub.Instance.Subscribe<WinLineEvent>(OnWinLine);
        SlotEventHub.Instance.Subscribe<SpinStartEvent>(OnSpinStart);
        SlotEventHub.Instance.Subscribe<TumbleClearEvent>(OnTumbleClear);
    }

    private void OnDisable()
    {
        SlotEventHub.Instance.Unsubscribe<WinLineEvent>(OnWinLine);
        SlotEventHub.Instance.Unsubscribe<SpinStartEvent>(OnSpinStart);
        SlotEventHub.Instance.Unsubscribe<TumbleClearEvent>(OnTumbleClear);
    }

    private void OnSpinStart(SpinStartEvent evt)
    {
        ClearHighlightImmediate(); // instant clear at the start of every new spin
    }

    private void OnTumbleClear(TumbleClearEvent evt)
    {
        FadeOutHighlight(); // smooth fade between tumble cascade steps
    }

    private void OnWinLine(WinLineEvent evt)
    {
        if (layoutConfig == null) return;

        int myFlatIndex = layoutConfig.GetFlatIndex(reelIndex, rowIndex);

        foreach (int flatIndex in evt.winLine.Positions)
        {
            if (flatIndex == myFlatIndex)
            {
                ShowHighlight();
                return;
            }
        }
    }

    // ── Highlight Show (with scale-in) ──

    private void ShowHighlight()
    {
        if (activeHighlight != null) return;
        SpawnHighlight(highlightEffectPrefab);
        AnimateHighlightIn();
    }

    private void SpawnHighlight(GameObject prefab)
    {
        // Kill any running fade-out so it doesn't destroy the new highlight
        if (highlightAnimRoutine != null)
            StopCoroutine(highlightAnimRoutine);

        activeHighlight = Instantiate(prefab, transform);
        activeHighlight.transform.localPosition = Vector3.zero;

        activeHighlightRenderer = activeHighlight.GetComponent<SpriteRenderer>();
        if (activeHighlightRenderer != null)
        {
            activeHighlightRenderer.sortingLayerID = icon.sortingLayerID;
            activeHighlightRenderer.sortingOrder = icon.sortingOrder - 1;
        }
    }

    private void AnimateHighlightIn()
    {
        if (highlightAnimRoutine != null)
            StopCoroutine(highlightAnimRoutine);

        highlightAnimRoutine = StartCoroutine(HighlightScaleIn());
    }

    private IEnumerator HighlightScaleIn()
    {
        if (activeHighlight == null) yield break;

        Transform ht = activeHighlight.transform;
        ht.localScale = Vector3.zero;

        float duration = SpinSpeedController.Apply(highlightFadeInDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutBack(t);
            ht.localScale = Vector3.one * eased;
            yield return null;
        }

        ht.localScale = Vector3.one;
        highlightAnimRoutine = null;
    }

    // ── Highlight Clear (with fade-out) ──

    /// <summary>
    /// Smooth fade-out used between tumble steps.
    /// </summary>
    private void FadeOutHighlight()
    {
        if (activeHighlight == null) return;

        if (highlightAnimRoutine != null)
            StopCoroutine(highlightAnimRoutine);

        highlightAnimRoutine = StartCoroutine(HighlightFadeOut());
    }

    private IEnumerator HighlightFadeOut()
    {
        if (activeHighlight == null) yield break;

        if (activeHighlightRenderer != null)
        {
            Color startColor = activeHighlightRenderer.color;
            float duration = SpinSpeedController.Apply(highlightFadeOutDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (activeHighlight != null)
                {
                    activeHighlight.transform.localScale = Vector3.one * (1f - t * 0.3f);
                    activeHighlightRenderer.color = new Color(
                        startColor.r, startColor.g, startColor.b, startColor.a * (1f - t)
                    );
                }
                yield return null;
            }
        }

        if (activeHighlight != null)
            Destroy(activeHighlight);

        activeHighlight = null;
        activeHighlightRenderer = null;
        highlightAnimRoutine = null;
    }

    /// <summary>
    /// Instant clear — used on spin start where we don't need a transition.
    /// </summary>
    private void ClearHighlightImmediate()
    {
        if (highlightAnimRoutine != null)
        {
            StopCoroutine(highlightAnimRoutine);
            highlightAnimRoutine = null;
        }

        if (activeHighlight != null)
            Destroy(activeHighlight);

        activeHighlight = null;
        activeHighlightRenderer = null;
    }

    // ── Easing ──

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}