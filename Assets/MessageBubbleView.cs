using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class MessageBubbleView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform bubbleRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI textTMP;

    [Header("Appear")]
    [SerializeField] private float appearDuration = 0.25f;
    [SerializeField] private Ease appearEase = Ease.OutBack;
    [SerializeField] private float startScale = 0.85f;

    [Header("Typewriter")]
    [SerializeField] private float charsPerSecond = 40f;

    private Coroutine _typingCo;
    private string _fullText = "";
    private bool _isTyping;

    public bool IsTyping => _isTyping;
    public string FullText => _fullText;

    private void Awake()
    {
        if (bubbleRoot == null) bubbleRoot = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    public void PlayAppear()
    {
        canvasGroup.DOKill();
        bubbleRoot.DOKill();

        canvasGroup.alpha = 0f;
        bubbleRoot.localScale = Vector3.one * startScale;

        DOTween.Sequence()
            .Append(canvasGroup.DOFade(1f, appearDuration).SetEase(Ease.OutCubic))
            .Join(bubbleRoot.DOScale(1f, appearDuration).SetEase(appearEase));
    }

    public void SetTextInstant(string text)
    {
        StopTypingInternal();
        _fullText = text ?? "";
        textTMP.text = _fullText;
        _isTyping = false;
    }

    public void StartTyping(string text)
    {
        StopTypingInternal();
        _fullText = text ?? "";
        _typingCo = StartCoroutine(TypeRoutine(_fullText));
    }

    public void SkipTyping()
    {
        if (!_isTyping) return;
        StopTypingInternal();
        textTMP.text = _fullText;
        _isTyping = false;
    }

    public void HideInstant()
    {
        StopTypingInternal();
        canvasGroup.alpha = 0f;
    }

    private IEnumerator TypeRoutine(string text)
    {
        _isTyping = true;
        textTMP.text = "";

        float delay = 1f / Mathf.Max(1f, charsPerSecond);

        for (int i = 0; i < text.Length; i++)
        {
            textTMP.text += text[i];
            yield return new WaitForSecondsRealtime(delay);
        }

        _isTyping = false;
        _typingCo = null;
    }

    private void StopTypingInternal()
    {
        if (_typingCo != null)
        {
            StopCoroutine(_typingCo);
            _typingCo = null;
        }
        _isTyping = false;
    }
}
