using System.Collections;
using UnityEngine;

public class PanelFlowManager : MonoBehaviour
{
    [Header("Phase A: Comic")]
    public UISequenceRunner comicRunner;

    [Tooltip("ONLY the first panel root (the thing you want to fade out). Do NOT set this to Canvas or a parent that contains GalPanel.")]
    public GameObject comicRoot;

    public float comicFadeOut = 0.25f;

    [Header("Phase B: Gal")]
    public GalPanelController gal;

    [Tooltip("Root GameObject of the GalPanel (usually gal.gameObject). This will be forced active.")]
    public GameObject galRoot;

    public float galDelayAfterSwitch = 0.05f;

    CanvasGroup comicCg;

    void Awake()
    {
        if (comicRoot != null)
        {
            comicCg = comicRoot.GetComponent<CanvasGroup>();
            if (comicCg == null) comicCg = comicRoot.AddComponent<CanvasGroup>();
            comicCg.alpha = 1f;
            comicCg.blocksRaycasts = true;
            comicCg.interactable = true;
        }

        if (gal != null)
        {
            if (galRoot == null) galRoot = gal.gameObject;
            gal.HideInstant();
        }
    }

    void OnEnable()
    {
        if (comicRunner != null)
            comicRunner.Finished += OnComicFinished;
    }

    void OnDisable()
    {
        if (comicRunner != null)
            comicRunner.Finished -= OnComicFinished;
    }

    void OnComicFinished()
    {
        StartCoroutine(SwitchToGal());
    }

    IEnumerator SwitchToGal()
    {
        // Debug guards (so you instantly see mis-wiring)
        if (comicRoot == null)
            Debug.LogWarning("[PanelFlowManager] comicRoot is NULL. Assign the first panel root only (not Canvas).");

        if (gal == null)
        {
            Debug.LogError("[PanelFlowManager] gal is NULL. Assign GalPanelController.");
            yield break;
        }

        if (galRoot == null)
            galRoot = gal.gameObject;

        // 1) Fade out comic (do NOT disable the object to avoid disabling GalPanel by accident)
        if (comicCg != null)
            yield return Fade(comicCg, 1f, 0f, comicFadeOut);

        // Make comic non-interactive (still active)
        if (comicCg != null)
        {
            comicCg.blocksRaycasts = false;
            comicCg.interactable = false;
        }

        yield return new WaitForSecondsRealtime(galDelayAfterSwitch);

        // 2) Force Gal root active, then begin
        if (galRoot != null && !galRoot.activeInHierarchy)
            galRoot.SetActive(true);

        gal.Begin();
    }

    static IEnumerator Fade(CanvasGroup cg, float a, float b, float dur)
    {
        if (cg == null) yield break;

        cg.alpha = a;
        cg.blocksRaycasts = true;
        cg.interactable = true;

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / dur);
            cg.alpha = Mathf.Lerp(a, b, u);
            yield return null;
        }

        cg.alpha = b;
    }
}
