using UnityEditor;
using UnityEngine;

namespace BlanketGhost.Tools.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ContentSizeAdjuster))]
    public class ContentSizeAdjusterEditor : UnityEditor.Editor
    {
        SerializedProperty horizontalFit;
        SerializedProperty verticalFit;
        SerializedProperty childAdjusters;
        SerializedProperty autoAdjustOnEnable;
        
        private void OnEnable()
        {
            horizontalFit = serializedObject.FindProperty("horizontalFit");
            verticalFit = serializedObject.FindProperty("verticalFit");
            childAdjusters = serializedObject.FindProperty("childAdjusters");
            autoAdjustOnEnable = serializedObject.FindProperty("autoAdjustOnEnable");
        }

        public override void OnInspectorGUI()
        {
            var contentSizeAdjuster = (ContentSizeAdjuster)target;

            serializedObject.Update();

            float totalWidth = EditorGUIUtility.currentViewWidth;
            float buttonWidth = (totalWidth / 2) - 12.5f;

            EditorGUILayout.PropertyField(horizontalFit);
            EditorGUILayout.PropertyField(verticalFit);
            EditorGUILayout.PropertyField(autoAdjustOnEnable);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(childAdjusters, true);
            EditorGUILayout.Space();
            if (GUILayout.Button("Get Child Adjusters", GUILayout.Height(20), GUILayout.Width(buttonWidth)))
            {
                contentSizeAdjuster.GetChildAdjusters();
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}