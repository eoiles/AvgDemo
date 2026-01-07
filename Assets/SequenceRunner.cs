// =========================
// 1) UISequenceRunner.cs  (UPDATED: exposes IsFinished + Finished event)
// =========================

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UISequenceRunner : MonoBehaviour
{
    public enum ActionType
    {
        SlideIn, SlideOut,
        FadeIn, FadeOut,
        TypeText,
        WaitSeconds,
        WaitClick
    }

    public enum Dir { Left, Right, Up, Down }

    public enum PlayMode
    {
        AfterPrevious,   // starts as a new "click step" (group leader)
        WithPrevious     // runs together with the group leader
    }

    [Serializable]
    public class Step
    {
        public GameObject target;
        public ActionType action = ActionType.SlideIn;

        public PlayMode playMode = PlayMode.AfterPrevious;

        [Header("Motion/Fade")]
        public Dir direction = Dir.Left;
        public float duration = 0.35f;
        public float distance = 500f;

        [Header("Text")]
        [TextArea] public string text;
        public float charsPerSecond = 35f;

        [Header("Wait")]
        public float seconds = 0.5f;
    }

    public Step[] steps;

    [Header("Input")]
    public bool clickToAdvance = true; // if false, call AdvanceOneClick() manually

    public bool IsFinished => (steps == null || stepIndex >= steps.Length) && !isBusy;
    public event Action Finished;

    // ---- internal state ----
    struct LayoutState
    {
        public bool useOffsets;          // for stretch RectTransforms
        public Vector2 anchoredPosition; // non-stretch
        public Vector2 offsetMin;        // stretch
        public Vector2 offsetMax;        // stretch
    }

    readonly Dictionary<RectTransform, LayoutState> finals = new();
    readonly List<IEnumerator> running = new();

    int stepIndex = 0;
    bool isBusy = false;

    // click pulse for both "advance" and WaitClick steps
    bool clickPulse = false;

    void Awake()
    {
        CacheFinalLayouts();
        InitializeHiddenToMatchFirstShow();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            clickPulse = true;

        if (!clickToAdvance) return;

        if (clickPulse && !isBusy && !IsFinished)
        {
            clickPulse = false; // consume the click that triggered advance
            StartCoroutine(AdvanceGroup());
        }
        else if (IsFinished && clickPulse)
        {
            // consume stray click pulses once done
            clickPulse = false;
        }
    }

    public void AdvanceOneClick()
    {
        if (isBusy || IsFinished) return;
        StartCoroutine(AdvanceGroup());
    }

    public void ResetSequence()
    {
        stepIndex = 0;
        running.Clear();
        isBusy = false;
        clickPulse = false;

        CacheFinalLayouts();
        InitializeHiddenToMatchFirstShow();
    }

    void CacheFinalLayouts()
    {
        finals.Clear();
        if (steps == null) return;

        foreach (var s in steps)
        {
            if (s == null || s.target == null) continue;

            if (NeedsRect(s.action))
            {
                var rt = s.target.GetComponent<RectTransform>();
                if (rt == null || finals.ContainsKey(rt)) continue;

                finals[rt] = Capture(rt);
            }
        }
    }

    void InitializeHiddenToMatchFirstShow()
    {
        var seen = new HashSet<GameObject>();
        if (steps == null) return;

        foreach (var s in steps)
        {
            if (s == null || s.target == null) continue;
            if (seen.Contains(s.target)) continue;
            seen.Add(s.target);

            if (s.action == ActionType.SlideIn)
            {
                var rt = s.target.GetComponent<RectTransform>();
                if (rt == null) continue;
                if (!finals.TryGetValue(rt, out var fin)) fin = Capture(rt);

                var off = fin;
                Shift(ref off, s.direction, s.distance);
                Apply(rt, off);
            }
            else if (s.action == ActionType.FadeIn)
            {
                var cg = EnsureCanvasGroup(s.target);
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
            }
        }
    }

    IEnumerator AdvanceGroup()
    {
        if (steps == null || steps.Length == 0) yield break;
        if (stepIndex >= steps.Length) yield break;

        isBusy = true;
        running.Clear();

        var leader = steps[stepIndex];
        stepIndex++;

        var leaderRoutine = RunStep(leader);
        running.Add(leaderRoutine);
        StartCoroutine(WrapRunning(leaderRoutine));

        while (stepIndex < steps.Length && steps[stepIndex] != null && steps[stepIndex].playMode == PlayMode.WithPrevious)
        {
            var s = steps[stepIndex];
            stepIndex++;

            var r = RunStep(s);
            running.Add(r);
            StartCoroutine(WrapRunning(r));
        }

        yield return WaitRunning();
        isBusy = false;

        if (IsFinished)
            Finished?.Invoke();
    }

    IEnumerator WrapRunning(IEnumerator r)
    {
        yield return r;
        running.Remove(r);
    }

    IEnumerator WaitRunning()
    {
        while (running.Count > 0)
            yield return null;
    }

    IEnumerator RunStep(Step s)
    {
        if (s == null) yield break;

        switch (s.action)
        {
            case ActionType.SlideIn:
                yield return Slide(s.target, true, s.direction, s.distance, s.duration);
                break;

            case ActionType.SlideOut:
                yield return Slide(s.target, false, s.direction, s.distance, s.duration);
                break;

            case ActionType.FadeIn:
                yield return Fade(s.target, 0f, 1f, s.duration);
                break;

            case ActionType.FadeOut:
                yield return Fade(s.target, 1f, 0f, s.duration);
                break;

            case ActionType.TypeText:
                yield return TypeText(s.target, s.text, s.charsPerSecond);
                break;

            case ActionType.WaitSeconds:
                yield return new WaitForSecondsRealtime(s.seconds);
                break;

            case ActionType.WaitClick:
                yield return WaitForClickPulse();
                break;
        }
    }

    static bool NeedsRect(ActionType a) => a == ActionType.SlideIn || a == ActionType.SlideOut;

    static Vector2 DirVec(Dir d) => d switch
    {
        Dir.Left => Vector2.left,
        Dir.Right => Vector2.right,
        Dir.Up => Vector2.up,
        Dir.Down => Vector2.down,
        _ => Vector2.zero
    };

    static LayoutState Capture(RectTransform rt)
    {
        bool stretchedX = !Mathf.Approximately(rt.anchorMin.x, rt.anchorMax.x);
        bool stretchedY = !Mathf.Approximately(rt.anchorMin.y, rt.anchorMax.y);

        return new LayoutState
        {
            useOffsets = stretchedX || stretchedY,
            anchoredPosition = rt.anchoredPosition,
            offsetMin = rt.offsetMin,
            offsetMax = rt.offsetMax
        };
    }

    static void Apply(RectTransform rt, LayoutState st)
    {
        if (st.useOffsets)
        {
            rt.offsetMin = st.offsetMin;
            rt.offsetMax = st.offsetMax;
        }
        else
        {
            rt.anchoredPosition = st.anchoredPosition;
        }
    }

    static void Shift(ref LayoutState st, Dir d, float dist)
    {
        var delta = DirVec(d) * dist;
        st.anchoredPosition += delta;
        st.offsetMin += delta;
        st.offsetMax += delta;
    }

    IEnumerator Slide(GameObject target, bool slideIn, Dir dir, float dist, float dur)
    {
        if (target == null) yield break;

        var rt = target.GetComponent<RectTransform>();
        if (rt == null) yield break;

        if (!finals.TryGetValue(rt, out var fin))
        {
            fin = Capture(rt);
            finals[rt] = fin;
        }

        var off = fin;
        Shift(ref off, dir, dist);

        var from = slideIn ? off : fin;
        var to = slideIn ? fin : off;

        Apply(rt, from);

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / dur);
            u = u * u * (3f - 2f * u);

            if (from.useOffsets)
            {
                rt.offsetMin = Vector2.LerpUnclamped(from.offsetMin, to.offsetMin, u);
                rt.offsetMax = Vector2.LerpUnclamped(from.offsetMax, to.offsetMax, u);
            }
            else
            {
                rt.anchoredPosition = Vector2.LerpUnclamped(from.anchoredPosition, to.anchoredPosition, u);
            }

            yield return null;
        }

        Apply(rt, to);
    }

    static CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    IEnumerator Fade(GameObject target, float a, float b, float dur)
    {
        if (target == null) yield break;

        var cg = EnsureCanvasGroup(target);
        cg.alpha = a;
        cg.blocksRaycasts = b > 0.001f;

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / dur);
            cg.alpha = Mathf.Lerp(a, b, u);
            yield return null;
        }

        cg.alpha = b;
        cg.blocksRaycasts = b > 0.001f;
    }

    IEnumerator TypeText(GameObject target, string text, float cps)
    {
        if (target == null) yield break;

        var tmp = target.GetComponent<TMP_Text>();
        if (tmp == null) tmp = target.GetComponentInChildren<TMP_Text>(true);
        if (tmp == null) yield break;

        tmp.text = text ?? "";
        tmp.ForceMeshUpdate();

        int total = tmp.textInfo.characterCount;
        tmp.maxVisibleCharacters = 0;

        if (cps <= 0f)
        {
            tmp.maxVisibleCharacters = total;
            yield break;
        }

        float count = 0f;
        while (tmp.maxVisibleCharacters < total)
        {
            count += Time.unscaledDeltaTime * cps;
            tmp.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(count));
            yield return null;
        }
    }

    IEnumerator WaitForClickPulse()
    {
        while (!clickPulse)
            yield return null;

        clickPulse = false;
    }
}
