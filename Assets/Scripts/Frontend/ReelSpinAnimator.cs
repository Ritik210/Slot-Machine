using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReelSpinAnimator : MonoBehaviour
{
    [Header("Linked Reel View")]
    [SerializeField, ReadOnly] private ReelView reelView;

    [Header("Spin Settings")]
    [SerializeField] private float spinSpeed = 2f;          // units/second
    [SerializeField] private float symbolSpacing = 1.5f;    // symbol height
    [SerializeField] private float snapDelay = 0.1f;        // wait before landing
    [SerializeField] private float stopSpeed = 30f;

    private Coroutine spinRoutine;
    private bool isSpinning;

    private void Awake()
    {
        reelView = GetComponent<ReelView>();
    }

    public void StartSpinning()
    {
        if (spinRoutine != null)
            StopCoroutine(spinRoutine);

        spinRoutine = StartCoroutine(SpinLoop());
    }

    public void StopSpinning(List<int> finalSymbols)
    {
        isSpinning = false;

        if (spinRoutine != null)
            StopCoroutine(spinRoutine);

        StartCoroutine(SmoothSnapToResult(finalSymbols));
    }

    private IEnumerator SpinLoop()
    {
        isSpinning = true;
        float totalHeight = symbolSpacing * reelView.SymbolCount;

        while (isSpinning)
        {
            foreach (var symbol in reelView.GetAllSymbols())
            {
                Transform t = symbol.transform;
                t.localPosition -= Vector3.up * spinSpeed * Time.deltaTime;

                float bottomY = -symbolSpacing * (reelView.SymbolCount - 1);
                //float bottomY = -symbolSpacing * (2);

                if (t.localPosition.y < bottomY - symbolSpacing)
                {
                    float topY = t.localPosition.y + symbolSpacing * reelView.SymbolCount;
                    t.localPosition = new Vector3(0, topY, 0);

                    // Random paying symbol for the spin blur — skip B (0) and EMPTY (last).
                    symbol.SetSymbol(Random.Range(Server.ScatterSymbolId, Server.EmptySymbolId));
                }
            }

            yield return null;
        }
    }

    private IEnumerator SmoothSnapToResult(List<int> finalSymbols)
    {
        yield return new WaitForSeconds(snapDelay);

        var symbols = reelView.GetAllSymbols();

        List<Vector3> endPositions = new List<Vector3>();

        for (int i = 0; i < symbols.Count; i++)
        {
            Vector3 start = symbols[i].transform.localPosition;

            // force reel to move one full symbol step
            endPositions.Add(start + Vector3.down * symbolSpacing);
        }

        bool moving = true;

        while (moving)
        {
            moving = false;

            for (int i = 0; i < symbols.Count; i++)
            {
                Transform t = symbols[i].transform;

                t.localPosition = Vector3.MoveTowards(
                    t.localPosition,
                    endPositions[i],
                    stopSpeed * Time.deltaTime
                );

                if (Vector3.Distance(t.localPosition, endPositions[i]) > 0.001f)
                    moving = true;
            }

            yield return null;
        }

        // apply correct symbols after movement
        reelView.DisplaySymbols(finalSymbols);

        // ensure perfect grid alignment
        reelView.SnapToLayout(symbolSpacing);
    }
    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
