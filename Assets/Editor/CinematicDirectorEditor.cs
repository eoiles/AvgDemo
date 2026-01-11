#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CinematicDirector))]
public class CinematicDirectorEditor : Editor
{
    SerializedProperty panelControllerProp;
    SerializedProperty bubbleSystemProp;
    SerializedProperty modeProp;
    SerializedProperty stepsProp;

    private void OnEnable()
    {
        panelControllerProp = serializedObject.FindProperty("panelController");
        bubbleSystemProp    = serializedObject.FindProperty("bubbleSystem");
        modeProp            = serializedObject.FindProperty("mode");
        stepsProp           = serializedObject.FindProperty("steps");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Refs", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(panelControllerProp);
        EditorGUILayout.PropertyField(bubbleSystemProp);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(modeProp);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Steps", EditorStyles.boldLabel);

        DrawStepsList();

        EditorGUILayout.Space(8);
        DrawAddButtons();

        serializedObject.ApplyModifiedProperties();
    }

    void DrawStepsList()
    {
        if (stepsProp == null) return;

        for (int i = 0; i < stepsProp.arraySize; i++)
        {
            var element = stepsProp.GetArrayElementAtIndex(i);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Step {i}", EditorStyles.boldLabel);

                    if (GUILayout.Button("Up", GUILayout.Width(40)) && i > 0)
                        stepsProp.MoveArrayElement(i, i - 1);

                    if (GUILayout.Button("Down", GUILayout.Width(55)) && i < stepsProp.arraySize - 1)
                        stepsProp.MoveArrayElement(i, i + 1);

                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        stepsProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                // Draw the actual step fields (managed reference)
                EditorGUILayout.PropertyField(element, includeChildren: true);
            }
        }
    }

    void DrawAddButtons()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Reveal Panel"))
                AddStep(new CinematicDirector.RevealPanelStep());

            if (GUILayout.Button("Add Show Bubble"))
                AddStep(new CinematicDirector.ShowBubbleStep());
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Wait Seconds"))
                AddStep(new CinematicDirector.WaitSecondsStep());

            if (GUILayout.Button("Add Clear Bubbles"))
                AddStep(new CinematicDirector.ClearBubblesStep());

            if (GUILayout.Button("Add Reset Panels"))
                AddStep(new CinematicDirector.ResetPanelsStep());
        }
    }

    void AddStep(CinematicDirector.StepBase step)
    {
        int idx = stepsProp.arraySize;
        stepsProp.InsertArrayElementAtIndex(idx);
        var element = stepsProp.GetArrayElementAtIndex(idx);

        element.managedReferenceValue = step;
    }
}
#endif
