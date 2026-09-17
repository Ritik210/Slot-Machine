using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReelView : MonoBehaviour
{
    [SerializeField] private List<SymbolView> symbolSlots;

    private Sprite[] symbolSprites;

    private int reelIndex;

    public int SymbolCount => symbolSlots.Count;
    public List<SymbolView> GetAllSymbols() => symbolSlots;
    public int TotalSymbolTypes => symbolSprites.Length;

    public void SetReelIndex(int index)
    {
        reelIndex = index;

        for (int row = 0; row < symbolSlots.Count; row++)
        {
            symbolSlots[row].SetPosition(reelIndex, row);
        }
    }

    public void SetSymbolSprites(Sprite[] sprites)
    {
        symbolSprites = sprites;

        foreach (var symbol in symbolSlots)
        {
            symbol.SetSymbolSprites(sprites);
        }
    }

    public void SetLayoutConfig(SlotLayoutConfig config)
    {
        foreach (var symbol in symbolSlots)
        {
            symbol.SetLayoutConfig(config);
        }
    }

    public void DisplaySymbols(List<int> symbolIds)
    {
        for (int i = 0; i < symbolSlots.Count && i < symbolIds.Count; i++)
        {
            symbolSlots[i].SetSymbol(symbolIds[i]);
        }
    }

    /// <summary>
    /// Display symbols but hide any cell whose ID matches emptyId.
    /// </summary>
    public void DisplaySymbolsWithEmpty(List<int> symbolIds, int emptyId)
    {
        for (int i = 0; i < symbolSlots.Count && i < symbolIds.Count; i++)
        {
            if (symbolIds[i] == emptyId)
            {
                symbolSlots[i].SetVisible(false);
            }
            else
            {
                symbolSlots[i].SetSymbol(symbolIds[i]);
                symbolSlots[i].SetVisible(true);
            }
        }
    }

    /// <summary>
    /// Animate existing symbols dropping down after winners are removed (gravity).
    /// withHolesColumn = state with gaps, settledColumn = state after gravity.
    /// Uses duration-based EaseOutBounce for a natural landing feel.
    /// </summary>
    public IEnumerator AnimateGravityDrop(float symbolSpacing, float duration, int emptyId,
                                           List<int> withHolesColumn, List<int> settledColumn)
    {
        if (withHolesColumn == null || settledColumn == null)
            yield break;

        // Collect source rows: non-empty positions in withHoles (top → bottom)
        List<int> sourceRows = new List<int>();
        for (int i = 0; i < withHolesColumn.Count; i++)
        {
            if (withHolesColumn[i] != emptyId)
                sourceRows.Add(i);
        }

        // Collect dest rows: non-empty positions in settled (top → bottom)
        List<int> destRows = new List<int>();
        for (int i = 0; i < settledColumn.Count; i++)
        {
            if (settledColumn[i] != emptyId)
                destRows.Add(i);
        }

        // Assign settled symbols so the correct sprites are loaded
        DisplaySymbolsWithEmpty(settledColumn, emptyId);

        // Build start / target positions for symbols that actually moved
        List<int> animatingRows = new List<int>();
        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> targetPositions = new List<Vector3>();

        int count = Mathf.Min(sourceRows.Count, destRows.Count);
        for (int i = 0; i < count; i++)
        {
            int fromRow = sourceRows[i];
            int toRow = destRows[i];

            if (fromRow != toRow)
            {
                Vector3 target = new Vector3(0, -symbolSpacing * toRow, 0);
                float offset = (toRow - fromRow) * symbolSpacing;
                Vector3 start = target + Vector3.up * Mathf.Abs(offset);

                symbolSlots[toRow].transform.localPosition = start;
                symbolSlots[toRow].SetVisible(true);

                animatingRows.Add(toRow);
                startPositions.Add(start);
                targetPositions.Add(target);
            }
        }

        if (animatingRows.Count == 0)
            yield break;

        // Lerp with easing over fixed duration
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutBounce(t);

            for (int i = 0; i < animatingRows.Count; i++)
            {
                symbolSlots[animatingRows[i]].transform.localPosition =
                    Vector3.Lerp(startPositions[i], targetPositions[i], eased);
            }
            yield return null;
        }

        // Snap to final positions
        for (int i = 0; i < animatingRows.Count; i++)
            symbolSlots[animatingRows[i]].transform.localPosition = targetPositions[i];
    }

    /// <summary>
    /// Animate new symbols dropping in from above to fill empty cells.
    /// settledColumn = state with gaps at top (empty = new symbols needed).
    /// Call AFTER the merged display has been set on this reel.
    /// Uses duration-based EaseOutBack for a slight overshoot on landing.
    /// </summary>
    public IEnumerator AnimateDropIn(float symbolSpacing, float duration, int emptyId,
                                      List<int> settledColumn)
    {
        List<int> newRows = new List<int>();
        if (settledColumn != null)
        {
            for (int i = 0; i < symbolSlots.Count && i < settledColumn.Count; i++)
            {
                if (settledColumn[i] == emptyId)
                    newRows.Add(i);
            }
        }

        if (newRows.Count == 0)
            yield break;

        float totalOffset = (newRows.Count + 1) * symbolSpacing;

        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> targetPositions = new List<Vector3>();

        for (int i = 0; i < newRows.Count; i++)
        {
            int row = newRows[i];
            Vector3 target = new Vector3(0, -symbolSpacing * row, 0);
            // Slight stagger so symbols don't all move in a rigid block
            float stagger = (newRows.Count - i) * symbolSpacing * 0.2f;
            Vector3 start = target + Vector3.up * (totalOffset + stagger);

            symbolSlots[row].transform.localPosition = start;
            symbolSlots[row].SetVisible(true);

            startPositions.Add(start);
            targetPositions.Add(target);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutBack(t);

            for (int i = 0; i < newRows.Count; i++)
            {
                symbolSlots[newRows[i]].transform.localPosition =
                    Vector3.Lerp(startPositions[i], targetPositions[i], eased);
            }
            yield return null;
        }

        for (int i = 0; i < newRows.Count; i++)
            symbolSlots[newRows[i]].transform.localPosition = targetPositions[i];
    }

    /// <summary>
    /// Existing symbols gravity-drop with easing.
    /// New symbols snap into their final positions instantly (no drop-in animation).
    /// </summary>
    public IEnumerator AnimateCombinedDrop(float symbolSpacing, float duration, int emptyId,
                                            List<int> withHolesColumn, List<int> settledColumn, List<int> mergedColumn)
    {
        if (withHolesColumn == null || settledColumn == null || mergedColumn == null)
            yield break;

        // ── New symbols: snap into place immediately, hidden until gravity finishes ──
        List<int> newRows = new List<int>();
        for (int i = 0; i < settledColumn.Count; i++)
        {
            if (settledColumn[i] == emptyId)
                newRows.Add(i);
        }

        foreach (int row in newRows)
        {
            symbolSlots[row].SetSymbol(mergedColumn[row]);
            symbolSlots[row].transform.localPosition = new Vector3(0, -symbolSpacing * row, 0);
            symbolSlots[row].SetVisible(false); // hidden during gravity
        }

        // ── Gravity symbols: animate existing symbols dropping down ──
        List<int> sourceRows = new List<int>();
        for (int i = 0; i < withHolesColumn.Count; i++)
        {
            if (withHolesColumn[i] != emptyId)
                sourceRows.Add(i);
        }

        List<int> destRows = new List<int>();
        for (int i = 0; i < settledColumn.Count; i++)
        {
            if (settledColumn[i] != emptyId)
                destRows.Add(i);
        }

        List<int> animatingRows = new List<int>();
        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> targetPositions = new List<Vector3>();

        int count = Mathf.Min(sourceRows.Count, destRows.Count);
        for (int i = 0; i < count; i++)
        {
            int fromRow = sourceRows[i];
            int toRow = destRows[i];

            if (fromRow != toRow)
            {
                Vector3 target = new Vector3(0, -symbolSpacing * toRow, 0);
                float offset = Mathf.Abs((toRow - fromRow) * symbolSpacing);
                Vector3 start = target + Vector3.up * offset;

                symbolSlots[toRow].transform.localPosition = start;
                symbolSlots[toRow].SetVisible(true);

                animatingRows.Add(toRow);
                startPositions.Add(start);
                targetPositions.Add(target);
            }
        }

        // Animate gravity with easing
        if (animatingRows.Count > 0)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutBounce(t);

                for (int i = 0; i < animatingRows.Count; i++)
                {
                    symbolSlots[animatingRows[i]].transform.localPosition =
                        Vector3.Lerp(startPositions[i], targetPositions[i], eased);
                }
                yield return null;
            }

            for (int i = 0; i < animatingRows.Count; i++)
                symbolSlots[animatingRows[i]].transform.localPosition = targetPositions[i];
        }

        // ── Reveal new symbols after gravity settles ──
        foreach (int row in newRows)
            symbolSlots[row].SetVisible(true);
    }

    public void SnapToLayout(float symbolSpacing)
    {
        for (int i = 0; i < symbolSlots.Count; i++)
        {
            symbolSlots[i].transform.localPosition = new Vector3(0, -symbolSpacing * i, 0);
        }
    }

    // ── Easing Functions ──

    private float EaseOutBounce(float t)
    {
        if (t < 1f / 2.75f)
            return 7.5625f * t * t;
        else if (t < 2f / 2.75f)
        {
            t -= 1.5f / 2.75f;
            return 7.5625f * t * t + 0.75f;
        }
        else if (t < 2.5f / 2.75f)
        {
            t -= 2.25f / 2.75f;
            return 7.5625f * t * t + 0.9375f;
        }
        else
        {
            t -= 2.625f / 2.75f;
            return 7.5625f * t * t + 0.984375f;
        }
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}