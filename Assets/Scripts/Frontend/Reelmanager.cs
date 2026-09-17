using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reelmanager : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private SlotLayoutConfig layoutConfig;

    [Header("Symbols")]
    [SerializeField] private Sprite[] symbolSprites;

    [Header("Reels")]
    [SerializeField, ReadOnly] private List<ReelView> reels;

    [Header("Animators")]
    [SerializeField, ReadOnly] private List<ReelSpinAnimator> reelAnimators;

    [Header("Tumble Animation")]
    [SerializeField] private float symbolSpacing = 1.5f;
    [SerializeField] private float gravityDropDuration = 0.35f;
    [SerializeField] private float newSymbolDropDuration = 0.3f;


    private void Awake()
    {
        reels = new List<ReelView>(GetComponentsInChildren<ReelView>());
        reelAnimators = new List<ReelSpinAnimator>(GetComponentsInChildren<ReelSpinAnimator>());

        for (int i = 0; i < reels.Count; i++)
        {
            reels[i].SetReelIndex(i);
            reels[i].SetSymbolSprites(symbolSprites);
            reels[i].SetLayoutConfig(layoutConfig);
        }
    }

    private void OnEnable()
    {
        SlotEventHub.Instance.Subscribe<SpinStartEvent>(OnSpinStart);
        SlotEventHub.Instance.Subscribe<ReelStopEvent>(OnReelStop);
    }

    private void OnDisable()
    {
        SlotEventHub.Instance.Unsubscribe<SpinStartEvent>(OnSpinStart);
        SlotEventHub.Instance.Unsubscribe<ReelStopEvent>(OnReelStop);
    }

    private void OnSpinStart(SpinStartEvent evt)
    {
        foreach (var animator in reelAnimators)
            animator.StartSpinning();
    }

    private void OnReelStop(ReelStopEvent evt)
    {
        if (evt.reelIndex >= 0 && evt.reelIndex < reels.Count)
        {
            reelAnimators[evt.reelIndex].StopSpinning(evt.symbols);
        }
    }

    public void UpdateReelSymbols(SpinResult result)
    {
        if (result.screen == null) return;

        for (int i = 0; i < reels.Count; i++)
        {
            reels[i].DisplaySymbols(result.screen[i]);
        }
    }

    // ── Tumble Display Methods ──

    /// <summary>
    /// Display a full screen instantly. All cells visible.
    /// </summary>
    public void DisplayScreen(List<List<int>> screen)
    {
        if (screen == null) return;
        for (int i = 0; i < reels.Count && i < screen.Count; i++)
            reels[i].DisplaySymbols(screen[i]);
    }

    /// <summary>
    /// Display a screen with empty cells hidden (holes after win removal).
    /// </summary>
    public void DisplayScreenWithEmpty(List<List<int>> screen, int emptyId)
    {
        if (screen == null) return;
        for (int i = 0; i < reels.Count && i < screen.Count; i++)
            reels[i].DisplaySymbolsWithEmpty(screen[i], emptyId);
    }

    /// <summary>
    /// Animate gravity drop: symbols fall from withHoles positions to settled positions.
    /// All reels animate in parallel.
    /// </summary>
    public IEnumerator AnimateGravityDrop(List<List<int>> withHoles, List<List<int>> settled, int emptyId)
    {
        DisplayScreenWithEmpty(settled, emptyId);

        float duration = SpinSpeedController.Apply(gravityDropDuration);
        List<Coroutine> running = new List<Coroutine>();
        for (int i = 0; i < reels.Count && i < withHoles.Count; i++)
        {
            Coroutine c = StartCoroutine(
                reels[i].AnimateGravityDrop(symbolSpacing, duration, emptyId, withHoles[i], settled[i])
            );
            running.Add(c);
        }

        foreach (var c in running)
            yield return c;
    }

    /// <summary>
    /// Animate new symbols dropping in from above to fill empty cells.
    /// </summary>
    public IEnumerator AnimateNewSymbolsDropIn(List<List<int>> settled, List<List<int>> merged, int emptyId)
    {
        DisplayScreen(merged);

        float duration = SpinSpeedController.Apply(newSymbolDropDuration);
        List<Coroutine> running = new List<Coroutine>();
        for (int i = 0; i < reels.Count && i < settled.Count; i++)
        {
            Coroutine c = StartCoroutine(
                reels[i].AnimateDropIn(symbolSpacing, duration, emptyId, settled[i])
            );
            running.Add(c);
        }

        foreach (var c in running)
            yield return c;
    }

    /// <summary>
    /// Combined animation: gravity drop + new symbol refill happen at the same time.
    /// </summary>
    public IEnumerator AnimateCombinedDrop(List<List<int>> withHoles, List<List<int>> settled, List<List<int>> merged, int emptyId)
    {
        DisplayScreen(merged);

        float duration = SpinSpeedController.Apply(Mathf.Max(gravityDropDuration, newSymbolDropDuration));
        List<Coroutine> running = new List<Coroutine>();

        for (int i = 0; i < reels.Count && i < withHoles.Count; i++)
        {
            Coroutine c = StartCoroutine(
                reels[i].AnimateCombinedDrop(symbolSpacing, duration, emptyId, withHoles[i], settled[i], merged[i])
            );
            running.Add(c);
        }

        foreach (var c in running)
            yield return c;
    }
}