using System;
using System.Collections.Generic;
using UnityEngine;

public class BubbleSystem : MonoBehaviour
{
    [Serializable]
    public class BubbleLine
    {
        [Range(0f, 1f)] public float x01 = 0.5f;
        [Range(0f, 1f)] public float y01 = 0.5f;

        [TextArea(2, 6)]
        public string text;
    }

    [Header("Prefab + Spawn Area")]
    [SerializeField] private MessageBubbleView bubblePrefab;
    [Tooltip("Usually a full-screen RectTransform under Canvas.")]
    [SerializeField] private RectTransform spawnArea;

    [Header("Options")]
    [Tooltip("Clamp bubble inside spawnArea bounds (recommended).")]
    [SerializeField] private bool clampToBounds = true;

    private readonly List<MessageBubbleView> _active = new();
    private MessageBubbleView _current;

    public MessageBubbleView Current => _current;

    public void ClearAll()
    {
        for (int i = 0; i < _active.Count; i++)
        {
            if (_active[i] != null) Destroy(_active[i].gameObject);
        }
        _active.Clear();
        _current = null;
    }

    public MessageBubbleView Spawn(float x01, float y01, string text)
    {
        if (bubblePrefab == null || spawnArea == null) return null;

        var bubble = Instantiate(bubblePrefab, spawnArea);
        var rt = bubble.GetComponent<RectTransform>();

        Vector2 anchored = NormalizedToAnchored(spawnArea, x01, y01);

        if (clampToBounds)
            anchored = ClampInside(spawnArea, rt, anchored);

        rt.anchoredPosition = anchored;

        bubble.PlayAppear();
        bubble.StartTyping(text);

        _active.Add(bubble);
        _current = bubble;
        return bubble;
    }

    /// <summary>
    /// Click behavior helper:
    /// - If current bubble is typing -> skip typing and return true
    /// - Otherwise -> return false (caller can spawn next bubble)
    /// </summary>
    public bool SkipIfTyping()
    {
        if (_current != null && _current.IsTyping)
        {
            _current.SkipTyping();
            return true;
        }
        return false;
    }

    public Vector2 NormalizedToAnchored(float x01, float y01)
        => NormalizedToAnchored(spawnArea, x01, y01);

    private static Vector2 NormalizedToAnchored(RectTransform area, float x01, float y01)
    {
        x01 = Mathf.Clamp01(x01);
        y01 = Mathf.Clamp01(y01);

        float x = (x01 - 0.5f) * area.rect.width;
        float y = (y01 - 0.5f) * area.rect.height;
        return new Vector2(x, y);
    }

    private static Vector2 ClampInside(RectTransform area, RectTransform bubbleRt, Vector2 desiredAnchoredPos)
    {
        // Works best when bubble pivot is (0.5,0.5)
        float halfW = bubbleRt.rect.width * bubbleRt.localScale.x * 0.5f;
        float halfH = bubbleRt.rect.height * bubbleRt.localScale.y * 0.5f;

        float minX = -area.rect.width * 0.5f + halfW;
        float maxX =  area.rect.width * 0.5f - halfW;
        float minY = -area.rect.height * 0.5f + halfH;
        float maxY =  area.rect.height * 0.5f - halfH;

        return new Vector2(
            Mathf.Clamp(desiredAnchoredPos.x, minX, maxX),
            Mathf.Clamp(desiredAnchoredPos.y, minY, maxY)
        );
    }

    // Optional: quick test driver (remove if you don't want it)
    [Header("Test Driver (optional)")]
    [SerializeField] private bool enableClickTest = false;
    [SerializeField] private List<BubbleLine> testLines = new();
    private int _testIndex = 0;

    private void Update()
    {
        if (!enableClickTest) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (SkipIfTyping()) return;
            if (_testIndex >= testLines.Count) return;

            var line = testLines[_testIndex++];
            Spawn(line.x01, line.y01, line.text);
        }
    }
}
