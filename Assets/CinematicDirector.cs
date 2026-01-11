using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CinematicDirector : MonoBehaviour
{
    [Header("Refs")]
    public PanelController panelController;
    public BubbleSystem bubbleSystem;

    [Header("Steps")]
    [SerializeReference] public List<StepBase> steps = new();

    private int _index = 0;
    private bool _runningStep = false;

    [Serializable]
    public abstract class StepBase
    {
        public string note;
        public abstract IEnumerator Run(CinematicDirector d);
    }

    [Serializable]
    public class RevealPanelStep : StepBase
    {
        public string panelId;

        public override IEnumerator Run(CinematicDirector d)
        {
            if (d.panelController == null)
            {
                Debug.LogError("[CinematicDirector] panelController is null.");
                yield break;
            }

            if (string.IsNullOrWhiteSpace(panelId))
            {
                Debug.LogError("[CinematicDirector] RevealPanelStep panelId is empty.");
                yield break;
            }

            d.panelController.PlayIn(panelId);

            // Wait for tween to finish
            while (d.panelController.IsBusy) yield return null;
        }
    }

    [Serializable]
    public class ShowBubbleStep : StepBase
    {
        [Range(0f, 1f)] public float x01 = 0.5f;
        [Range(0f, 1f)] public float y01 = 0.5f;
        [TextArea(2, 6)] public string text;

        public override IEnumerator Run(CinematicDirector d)
        {
            if (d.bubbleSystem == null)
            {
                Debug.LogError("[CinematicDirector] bubbleSystem is null.");
                yield break;
            }

            d.bubbleSystem.Spawn(x01, y01, text);

            // Wait for typing to finish
            while (d.bubbleSystem.Current != null && d.bubbleSystem.Current.IsTyping)
                yield return null;
        }
    }

    [Serializable]
    public class WaitSecondsStep : StepBase
    {
        public float seconds = 0.5f;

        public override IEnumerator Run(CinematicDirector d)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, seconds));
        }
    }

    [Serializable]
    public class ClearBubblesStep : StepBase
    {
        public override IEnumerator Run(CinematicDirector d)
        {
            d.bubbleSystem?.ClearAll();
            yield break;
        }
    }

    [Serializable]
    public class ResetPanelsStep : StepBase
    {
        public override IEnumerator Run(CinematicDirector d)
        {
            d.panelController?.ResetAll(true);
            yield break;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            OnAdvanceClick();
    }

    private void OnAdvanceClick()
    {
        // 1) Skip typing first (common VN behavior)
        if (bubbleSystem != null && bubbleSystem.SkipIfTyping())
            return;

        // 2) Don’t start a new step while one is running
        if (_runningStep) return;

        // 3) Run next step
        if (_index >= steps.Count) return;
        if (steps[_index] == null) { _index++; return; }

        StartCoroutine(RunOneStep(steps[_index]));
        _index++;
    }

    private IEnumerator RunOneStep(StepBase step)
    {
        _runningStep = true;
        yield return step.Run(this);
        _runningStep = false;
    }
}
