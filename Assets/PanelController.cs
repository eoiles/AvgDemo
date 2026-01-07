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
    public class PanelEntry
    {
        public string name;
        public GameObject panelObject;

        [Header("Effect")]
        public InEffect inEffect = InEffect.SlideFromLeft;
        public float duration = 0.35f;
        public Ease ease = Ease.OutCubic;
        public float delay = 0f;

        [Header("Effect Params")]
        [Tooltip("Slide distance in pixels for UI (RectTransform).")]
        public Vector2 slideOffset = new Vector2(500f, 0f);

        [Tooltip("Start scale for ScaleIn.")]
        public float startScale = 0.85f;

        [Tooltip("Punch strength for PunchScale.")]
        public float punchStrength = 0.12f;

        [Tooltip("Punch vibrato for PunchScale.")]
        public int punchVibrato = 10;

        [Tooltip("Punch elasticity for PunchScale.")]
        public float punchElasticity = 1f;

        [NonSerialized] public RectTransform rect;
        [NonSerialized] public CanvasGroup cg;
        [NonSerialized] public Vector2 anchoredPos;
        [NonSerialized] public Vector3 baseScale;
    }

    [Header("Panels (order matters: click reveals next)")]
    public PanelEntry leftTop;
    public PanelEntry leftBottom;
    public PanelEntry right;

    [Header("Behavior")]
    public bool revealOnLeftClick = true;
    public KeyCode revealKey = KeyCode.Space;

    [Tooltip("If true, after all panels revealed, next click resets and starts again.")]
    public bool loopSequence = true;

    [Tooltip("If true, hide panels on start.")]
    public bool hideOnStart = true;

    private List<PanelEntry> _sequence;
    private int _index = 0;
    private Sequence _playing;

    private void Awake()
    {
        _sequence = new List<PanelEntry> { leftTop, leftBottom, right };
        CacheAndValidate();
    }

    private void Start()
    {
        if (hideOnStart)
            ResetAll(immediate: true);
    }

    private void Update()
    {
        if (InputTriggeredReveal())
            RevealNext();
    }

    private bool InputTriggeredReveal()
    {
        if (revealOnLeftClick && Input.GetMouseButtonDown(0)) return true;
        if (Input.GetKeyDown(revealKey)) return true;
        return false;
    }

    private void CacheAndValidate()
    {
        foreach (var p in _sequence)
        {
            if (p == null || p.panelObject == null) continue;

            p.rect = p.panelObject.GetComponent<RectTransform>();
            p.cg = p.panelObject.GetComponent<CanvasGroup>();
            if (p.cg == null) p.cg = p.panelObject.AddComponent<CanvasGroup>();

            // Cache "shown" state transform
            if (p.rect != null) p.anchoredPos = p.rect.anchoredPosition;
            p.baseScale = p.panelObject.transform.localScale;
        }
    }

    public void RevealNext()
    {
        if (_playing != null && _playing.IsActive() && _playing.IsPlaying())
            return;

        if (_index >= _sequence.Count)
        {
            if (!loopSequence) return;
            ResetAll(immediate: true);
            _index = 0;
        }

        var p = _sequence[_index];
        _index++;

        if (p == null || p.panelObject == null) return;

        _playing = PlayIn(p);
    }

    public void ResetAll(bool immediate)
    {
        if (_playing != null && _playing.IsActive())
            _playing.Kill(complete: false);

        foreach (var p in _sequence)
            HidePanel(p, immediate);

        _index = 0;
    }

    private void HidePanel(PanelEntry p, bool immediate)
    {
        if (p == null || p.panelObject == null) return;

        p.panelObject.SetActive(true); // keep active so we can set transforms
        if (p.cg != null) p.cg.alpha = 0f;

        // Reset to "shown" base values
        if (p.rect != null) p.rect.anchoredPosition = p.anchoredPos;
        p.panelObject.transform.localScale = p.baseScale;

        // If you prefer: disable object entirely when hidden
        if (immediate)
            p.panelObject.SetActive(false);
        else
            p.panelObject.SetActive(false);
    }

    private Sequence PlayIn(PanelEntry p)
    {
        p.panelObject.SetActive(true);

        // Kill any tweens targeting this panel to avoid stacking
        if (p.rect != null) p.rect.DOKill();
        p.panelObject.transform.DOKill();
        if (p.cg != null) p.cg.DOKill();

        // Ensure baseline
        if (p.rect != null) p.rect.anchoredPosition = p.anchoredPos;
        p.panelObject.transform.localScale = p.baseScale;
        if (p.cg != null) p.cg.alpha = 1f;

        var seq = DOTween.Sequence();
        if (p.delay > 0f) seq.AppendInterval(p.delay);

        switch (p.inEffect)
        {
            case InEffect.None:
                // No animation; just ensure visible
                if (p.cg != null) p.cg.alpha = 1f;
                break;

            case InEffect.FadeIn:
                if (p.cg != null)
                {
                    p.cg.alpha = 0f;
                    seq.Append(p.cg.DOFade(1f, p.duration).SetEase(p.ease));
                }
                break;

            case InEffect.ScaleIn:
                if (p.cg != null) p.cg.alpha = 1f;
                p.panelObject.transform.localScale = p.baseScale * p.startScale;
                seq.Append(p.panelObject.transform.DOScale(p.baseScale, p.duration).SetEase(p.ease));
                break;

            case InEffect.PunchScale:
                if (p.cg != null) p.cg.alpha = 1f;
                seq.Append(p.panelObject.transform.DOPunchScale(
                    Vector3.one * p.punchStrength,
                    p.duration,
                    p.punchVibrato,
                    p.punchElasticity
                ));
                break;

            case InEffect.SlideFromLeft:
                SlideFrom(p, seq, new Vector2(-Mathf.Abs(p.slideOffset.x), 0f));
                break;

            case InEffect.SlideFromRight:
                SlideFrom(p, seq, new Vector2(Mathf.Abs(p.slideOffset.x), 0f));
                break;

            case InEffect.SlideFromTop:
                SlideFrom(p, seq, new Vector2(0f, Mathf.Abs(p.slideOffset.y == 0 ? 500 : p.slideOffset.y)));
                break;

            case InEffect.SlideFromBottom:
                SlideFrom(p, seq, new Vector2(0f, -Mathf.Abs(p.slideOffset.y == 0 ? 500 : p.slideOffset.y)));
                break;
        }

        // Optional: enable raycasts after reveal
        seq.OnComplete(() =>
        {
            if (p.cg != null)
            {
                p.cg.interactable = true;
                p.cg.blocksRaycasts = true;
            }
        });

        return seq;
    }

    private void SlideFrom(PanelEntry p, Sequence seq, Vector2 offset)
    {
        if (p.rect == null)
        {
            // Fallback for non-UI objects: move local position
            var t = p.panelObject.transform;
            var basePos = t.localPosition;
            t.localPosition = basePos + (Vector3)offset;
            if (p.cg != null) p.cg.alpha = 1f;

            seq.Append(t.DOLocalMove(basePos, p.duration).SetEase(p.ease));
            return;
        }

        var baseAnchored = p.anchoredPos;
        p.rect.anchoredPosition = baseAnchored + offset;

        if (p.cg != null) p.cg.alpha = 1f;

        seq.Append(p.rect.DOAnchorPos(baseAnchored, p.duration).SetEase(p.ease));
    }
}
