using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GalPanelController : MonoBehaviour
{
    [Serializable]
    public class Line
    {
        public Sprite characterSprite;      // optional
        [TextArea] public string text;
    }

    [Header("Root (whole gal panel)")]
    public GameObject root;
    public float rootFadeIn = 0.35f;

    [Header("Background (optional)")]
    public GameObject background;          // must be different object than root
    public float bgFadeIn = 0.25f;

    [Header("Character (optional)")]
    public Image characterImage;
    public float characterFadeIn = 0.25f;

    [Header("Dialogue Box (optional)")]
    public GameObject dialogueBox;
    public float boxFadeIn = 0.2f;

    [Header("Text")]
    public TMP_Text dialogueText;
    public float charsPerSecond = 35f;

    [Header("Content")]
    public Line[] lines;

    [Header("Line switch fade (optional)")]
    [Tooltip("Fade the character and box out/in between lines (classic VN 'switch').")]
    public bool fadeBetweenLines = true;

    [Tooltip("Fade-out duration before switching to next line.")]
    public float switchFadeOut = 0.12f;

    [Tooltip("Fade-in duration after switching to next line.")]
    public float switchFadeIn = 0.12f;

    [Tooltip("Small pause between fade-out and fade-in (optional).")]
    public float switchHold = 0.02f;

    public bool IsFinished { get; private set; }
    public event Action Finished;

    CanvasGroup rootCg, bgCg, charCg, boxCg;

    int lineIndex = 0;
    bool isTyping = false;
    bool isPlaying = false;

    void Awake()
    {
        if (root == null) root = gameObject;

        // prevent flicker if same object assigned twice
        if (background == root) background = null;
        if (dialogueBox == root) dialogueBox = null;
        if (characterImage != null && characterImage.gameObject == root) characterImage = null;

        rootCg = EnsureCg(root);
        bgCg   = background ? EnsureCg(background) : null;
        charCg = characterImage ? EnsureCg(characterImage.gameObject) : null;
        boxCg  = dialogueBox ? EnsureCg(dialogueBox) : null;

        HideInstant();
    }

    void Update()
    {
        if (!isPlaying || IsFinished) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                FinishTypingInstant();
            }
            else
            {
                // click-to-advance only
                StopAllCoroutines();
                StartCoroutine(AdvanceLineCoroutine());
            }
        }
    }

    public void Begin()
    {
        if (isPlaying) return;

        // force active
        if (root != null && !root.activeInHierarchy)
            root.SetActive(true);

        if (background != null) background.SetActive(true);
        if (dialogueBox != null) dialogueBox.SetActive(true);
        if (characterImage != null) characterImage.gameObject.SetActive(true);
        if (dialogueText != null) dialogueText.gameObject.SetActive(true);

        // prevent 1-frame flash
        SetAlpha(rootCg, 0f);
        if (bgCg != null) SetAlpha(bgCg, 0f);
        if (charCg != null) SetAlpha(charCg, 0f);
        if (boxCg != null) SetAlpha(boxCg, 0f);

        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.maxVisibleCharacters = 0;
        }

        isPlaying = true;
        IsFinished = false;
        isTyping = false;
        lineIndex = 0;

        StopAllCoroutines();
        StartCoroutine(EnterAndTypeCurrent());
    }

    public void HideInstant()
    {
        StopAllCoroutines();

        isPlaying = false;
        IsFinished = false;
        isTyping = false;
        lineIndex = 0;

        SetAlpha(rootCg, 0f);
        if (bgCg != null) SetAlpha(bgCg, 0f);
        if (charCg != null) SetAlpha(charCg, 0f);
        if (boxCg != null) SetAlpha(boxCg, 0f);

        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.maxVisibleCharacters = 0;
        }

        if (root != null) root.SetActive(false);
    }

    IEnumerator EnterAndTypeCurrent()
    {
        yield return Fade(rootCg, 0f, 1f, rootFadeIn);

        if (bgCg != null)
            yield return Fade(bgCg, 0f, 1f, bgFadeIn);

        if (characterImage != null)
        {
            ApplyCharacterSpriteIfAny(lineIndex);
            if (charCg != null)
                yield return Fade(charCg, 0f, 1f, characterFadeIn);
        }

        if (boxCg != null)
            yield return Fade(boxCg, 0f, 1f, boxFadeIn);

        yield return TypeLine(GetLineText(lineIndex));
    }

    IEnumerator AdvanceLineCoroutine()
    {
        // move to next line
        int next = lineIndex + 1;

        if (lines == null || next >= lines.Length)
        {
            FinishAll();
            yield break;
        }

        // Optional "switch" fade (character + box)
        if (fadeBetweenLines)
        {
            // fade OUT the parts that look like "switching"
            if (charCg != null) yield return Fade(charCg, charCg.alpha, 0f, switchFadeOut);
            if (boxCg != null)  yield return Fade(boxCg,  boxCg.alpha,  0f, switchFadeOut);

            if (switchHold > 0f)
                yield return new WaitForSecondsRealtime(switchHold);
        }

        // apply new content
        lineIndex = next;
        ApplyCharacterSpriteIfAny(lineIndex);

        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.maxVisibleCharacters = 0;
        }

        if (fadeBetweenLines)
        {
            // fade IN
            if (charCg != null) yield return Fade(charCg, 0f, 1f, switchFadeIn);
            if (boxCg != null)  yield return Fade(boxCg,  0f, 1f, switchFadeIn);
        }

        // type next line (still click-driven; no auto-advance after typing)
        yield return TypeLine(GetLineText(lineIndex));
    }

    string GetLineText(int idx)
    {
        if (lines == null || idx < 0 || idx >= lines.Length) return "";
        return lines[idx].text ?? "";
    }

    IEnumerator TypeLine(string text)
    {
        if (dialogueText == null) yield break;

        dialogueText.text = text ?? "";
        dialogueText.ForceMeshUpdate();

        int total = dialogueText.textInfo.characterCount;
        dialogueText.maxVisibleCharacters = 0;

        isTyping = true;

        if (charsPerSecond <= 0f)
        {
            dialogueText.maxVisibleCharacters = total;
            isTyping = false;
            yield break;
        }

        float count = 0f;
        while (dialogueText.maxVisibleCharacters < total)
        {
            count += Time.unscaledDeltaTime * charsPerSecond;
            dialogueText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(count));
            yield return null;
        }

        isTyping = false;
    }

    void FinishTypingInstant()
    {
        if (dialogueText == null) { isTyping = false; return; }
        dialogueText.ForceMeshUpdate();
        dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
        isTyping = false;
    }

    void ApplyCharacterSpriteIfAny(int idx)
    {
        if (characterImage == null || lines == null || idx < 0 || idx >= lines.Length) return;
        if (lines[idx].characterSprite != null)
            characterImage.sprite = lines[idx].characterSprite;
        // If None: do nothing (demo)
    }

    void FinishAll()
    {
        IsFinished = true;
        isPlaying = false;
        Finished?.Invoke();
    }

    static CanvasGroup EnsureCg(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    static void SetAlpha(CanvasGroup cg, float a)
    {
        if (cg == null) return;
        cg.alpha = a;
        cg.blocksRaycasts = a > 0.001f;
        cg.interactable = a > 0.001f;
    }

    static IEnumerator Fade(CanvasGroup cg, float a, float b, float dur)
    {
        if (cg == null) yield break;

        cg.alpha = a;
        cg.blocksRaycasts = true;
        cg.interactable = true;

        if (dur <= 0f)
        {
            cg.alpha = b;
            yield break;
        }

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
