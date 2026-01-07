using System.Collections;
using TMPro;
using UnityEngine;

public class BubbleFXManager : MonoBehaviour
{
    public string textNodeName = "Text (TMP)";

    CanvasGroup cg;
    TMP_Text tmp;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        tmp = transform.Find(textNodeName)?.GetComponent<TMP_Text>();
        if (tmp == null) tmp = GetComponentInChildren<TMP_Text>(true);

        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        if (tmp != null) tmp.text = "";
    }

    public IEnumerator Show(float dur)
    {
        cg.blocksRaycasts = true;
        yield return Fade(0f, 1f, dur);
    }

    public IEnumerator Hide(float dur)
    {
        yield return Fade(1f, 0f, dur);
        cg.blocksRaycasts = false;
    }

    public IEnumerator TypeText(string text, float charsPerSec)
    {
        if (tmp == null) yield break;

        tmp.text = text;
        tmp.ForceMeshUpdate();

        int total = tmp.textInfo.characterCount;
        tmp.maxVisibleCharacters = 0;

        if (charsPerSec <= 0f)
        {
            tmp.maxVisibleCharacters = total;
            yield break;
        }

        float t = 0f;
        while (tmp.maxVisibleCharacters < total)
        {
            t += Time.unscaledDeltaTime * charsPerSec;
            tmp.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(t));
            yield return null;
        }
    }

    IEnumerator Fade(float a, float b, float dur)
    {
        cg.alpha = a;
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
