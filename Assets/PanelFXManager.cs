using System.Collections;
using UnityEngine;

public class PanelFXManager : MonoBehaviour
{
    [Header("Child names under Panel")]
    public string leftTopName = "LeftTop";
    public string leftBottomName = "LeftBottom";
    public string rightName = "Right";

    RectTransform lt, lb, r;

    // final layout values (supports stretch)
    Vector2 ltMin, ltMax, lbMin, lbMax, rMin, rMax;

    void Awake()
    {
        var panel = (RectTransform)transform;

        lt = panel.Find(leftTopName)?.GetComponent<RectTransform>();
        lb = panel.Find(leftBottomName)?.GetComponent<RectTransform>();
        r  = panel.Find(rightName)?.GetComponent<RectTransform>();

        CacheFinal(lt, out ltMin, out ltMax);
        CacheFinal(lb, out lbMin, out lbMax);
        CacheFinal(r,  out rMin,  out rMax);
    }

    static void CacheFinal(RectTransform rt, out Vector2 min, out Vector2 max)
    {
        if (rt == null) { min = max = default; return; }
        min = rt.offsetMin;
        max = rt.offsetMax;
    }

    static void SetOffsets(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.offsetMin = min;
        rt.offsetMax = max;
    }

    IEnumerator Slide(RectTransform rt, Vector2 fromMin, Vector2 fromMax, Vector2 toMin, Vector2 toMax, float dur)
    {
        if (rt == null) yield break;

        SetOffsets(rt, fromMin, fromMax);

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / dur);
            // simple smoothstep ease
            u = u * u * (3f - 2f * u);

            SetOffsets(
                rt,
                Vector2.LerpUnclamped(fromMin, toMin, u),
                Vector2.LerpUnclamped(fromMax, toMax, u)
            );
            yield return null;
        }
        SetOffsets(rt, toMin, toMax);
    }

    // --- Public effects (what SequenceRunner calls) ---

    public IEnumerator ShowAll(float dur, float distance)
    {
        yield return RunParallel(
            ShowLeftTop(dur, distance),
            ShowLeftBottom(dur, distance),
            ShowRight(dur, distance)
        );
    }

    public IEnumerator HideAll(float dur, float distance)
    {
        yield return RunParallel(
            HideLeftTop(dur, distance),
            HideLeftBottom(dur, distance),
            HideRight(dur, distance)
        );
    }

    public IEnumerator ShowLeftTop(float dur, float distance)
    {
        // slide from top: shift offsets upward then come down
        var fromMin = ltMin + Vector2.up * distance;
        var fromMax = ltMax + Vector2.up * distance;
        yield return Slide(lt, fromMin, fromMax, ltMin, ltMax, dur);
    }

    public IEnumerator HideLeftTop(float dur, float distance)
    {
        var toMin = ltMin + Vector2.up * distance;
        var toMax = ltMax + Vector2.up * distance;
        yield return Slide(lt, ltMin, ltMax, toMin, toMax, dur);
    }

    public IEnumerator ShowLeftBottom(float dur, float distance)
    {
        var fromMin = lbMin + Vector2.left * distance;
        var fromMax = lbMax + Vector2.left * distance;
        yield return Slide(lb, fromMin, fromMax, lbMin, lbMax, dur);
    }

    public IEnumerator HideLeftBottom(float dur, float distance)
    {
        var toMin = lbMin + Vector2.left * distance;
        var toMax = lbMax + Vector2.left * distance;
        yield return Slide(lb, lbMin, lbMax, toMin, toMax, dur);
    }

    public IEnumerator ShowRight(float dur, float distance)
    {
        var fromMin = rMin + Vector2.right * distance;
        var fromMax = rMax + Vector2.right * distance;
        yield return Slide(r, fromMin, fromMax, rMin, rMax, dur);
    }

    public IEnumerator HideRight(float dur, float distance)
    {
        var toMin = rMin + Vector2.right * distance;
        var toMax = rMax + Vector2.right * distance;
        yield return Slide(r, rMin, rMax, toMin, toMax, dur);
    }

    static IEnumerator RunParallel(params IEnumerator[] routines)
    {
        int done = 0;
        foreach (var r in routines)
            CoroutineHost.Instance.StartCoroutine(Wrap(r, () => done++));

        while (done < routines.Length)
            yield return null;
    }

    static IEnumerator Wrap(IEnumerator r, System.Action onDone)
    {
        yield return CoroutineHost.Instance.StartCoroutine(r);
        onDone?.Invoke();
    }

    // tiny hidden host so PanelFXManager can run parallel coroutines safely
    sealed class CoroutineHost : MonoBehaviour
    {
        static CoroutineHost _instance;
        public static CoroutineHost Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("~CoroutineHost");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<CoroutineHost>();
                return _instance;
            }
        }
    }
}
