using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PanelController : MonoBehaviour
{
    public enum InEffect
    {
        None,
        SlideFromLeft,
        SlideFromRight,
        SlideFromTop,
        SlideFromBottom,
        FadeIn,
        ScaleIn,
        PunchScale
    }

    [Serializable]
    public class Panel
    {
        public string id;
        public GameObject go;

        [Header("Effect")]
        public InEffect inEffect = InEffect.SlideFromLeft;
        public float duration = 0.35f;
        public Ease ease = Ease.OutCubic;
        public float delay = 0f;

        [Header("Params")]
        public Vector2 slideOffset = new Vector2(500f, 500f); // used by slide effects
        public float startScale = 0.85f;                      // ScaleIn
        public float punchStrength = 0.12f;                   // PunchScale
        public int punchVibrato = 10;
        public float punchElasticity = 1f;

        [NonSerialized] public RectTransform rt;
        [NonSerialized] public CanvasGroup cg;
        [NonSerialized] public Vector2 baseAnchoredPos;
        [NonSerialized] public Vector3 baseScale;
    }

    [Header("Panels")]
    public List<Panel> panels = new();

    private readonly Dictionary<string, Panel> _map = new();
    private Sequence _seq;

    public bool IsBusy => _seq != null && _seq.IsActive() && _seq.IsPlaying();

    private void Awake()
    {
        Cache();
    }

    private void Cache()
    {
        _map.Clear();

        foreach (var p in panels)
        {
            if (p == null || p.go == null || string.IsNullOrWhiteSpace(p.id)) continue;

            p.rt = p.go.GetComponent<RectTransform>();
            p.cg = p.go.GetComponent<CanvasGroup>();
            if (p.cg == null) p.cg = p.go.AddComponent<CanvasGroup>();

            if (p.rt != null) p.baseAnchoredPos = p.rt.anchoredPosition;
            p.baseScale = p.go.transform.localScale;

            _map[p.id] = p;
        }
    }

    public void ResetAll(bool immediate = true)
    {
        Kill();

        foreach (var p in panels)
        {
            if (p?.go == null) continue;

            p.go.SetActive(true);
            if (p.cg != null) p.cg.alpha = 0f;
            if (p.rt != null) p.rt.anchoredPosition = p.baseAnchoredPos;
            p.go.transform.localScale = p.baseScale;

            if (immediate) p.go.SetActive(false);
        }
    }

    public void Hide(string id)
    {
        if (!_map.TryGetValue(id, out var p) || p.go == null) return;
        p.go.SetActive(false);
        if (p.cg != null) p.cg.alpha = 0f;
    }

    public void PlayIn(string id)
    {
        if (!_map.TryGetValue(id, out var p) || p.go == null) return;
        PlayIn(p);
    }

    private void PlayIn(Panel p)
    {
        Kill();

        p.go.SetActive(true);

        // Kill per-target tweens
        p.rt?.DOKill();
        p.go.transform.DOKill();
        p.cg?.DOKill();

        // Reset baseline (shown pose)
        if (p.rt != null) p.rt.anchoredPosition = p.baseAnchoredPos;
        p.go.transform.localScale = p.baseScale;
        if (p.cg != null) p.cg.alpha = 1f;

        _seq = DOTween.Sequence();
        if (p.delay > 0f) _seq.AppendInterval(p.delay);

        switch (p.inEffect)
        {
            case InEffect.None:
                if (p.cg != null) p.cg.alpha = 1f;
                break;

            case InEffect.FadeIn:
                if (p.cg != null)
                {
                    p.cg.alpha = 0f;
                    _seq.Append(p.cg.DOFade(1f, p.duration).SetEase(p.ease));
                }
                break;

            case InEffect.ScaleIn:
                if (p.cg != null) p.cg.alpha = 1f;
                p.go.transform.localScale = p.baseScale * p.startScale;
                _seq.Append(p.go.transform.DOScale(p.baseScale, p.duration).SetEase(p.ease));
                break;

            case InEffect.PunchScale:
                if (p.cg != null) p.cg.alpha = 1f;
                _seq.Append(p.go.transform.DOPunchScale(
                    Vector3.one * p.punchStrength,
                    p.duration,
                    p.punchVibrato,
                    p.punchElasticity
                ));
                break;

            case InEffect.SlideFromLeft:
                SlideFrom(p, new Vector2(-Mathf.Abs(p.slideOffset.x), 0f));
                break;

            case InEffect.SlideFromRight:
                SlideFrom(p, new Vector2(Mathf.Abs(p.slideOffset.x), 0f));
                break;

            case InEffect.SlideFromTop:
                SlideFrom(p, new Vector2(0f, Mathf.Abs(p.slideOffset.y)));
                break;

            case InEffect.SlideFromBottom:
                SlideFrom(p, new Vector2(0f, -Mathf.Abs(p.slideOffset.y)));
                break;
        }
    }

    private void SlideFrom(Panel p, Vector2 offset)
    {
        if (p.rt == null)
        {
            var t = p.go.transform;
            var basePos = t.localPosition;
            t.localPosition = basePos + (Vector3)offset;
            _seq.Append(t.DOLocalMove(basePos, p.duration).SetEase(p.ease));
            return;
        }

        var baseA = p.baseAnchoredPos;
        p.rt.anchoredPosition = baseA + offset;
        _seq.Append(p.rt.DOAnchorPos(baseA, p.duration).SetEase(p.ease));
    }

    private void Kill()
    {
        if (_seq != null && _seq.IsActive()) _seq.Kill(false);
        _seq = null;
    }
}
