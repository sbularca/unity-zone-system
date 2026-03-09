#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Jovian.ZoneSystem.Editor {
    [CustomEditor(typeof(ZoneEditorSettings))]
    public class ZoneEditorSettingsEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            bool changed = EditorGUI.EndChangeCheck();

            if(changed) {
                serializedObject.ApplyModifiedProperties();
                ApplyColorsToAllZoneData((ZoneEditorSettings)target);
            }

            EditorGUILayout.Space(8);
            if(GUILayout.Button("Apply Colors to All Zones")) {
                ApplyColorsToAllZoneData((ZoneEditorSettings)target);
            }
        }

        private static void ApplyColorsToAllZoneData(ZoneEditorSettings settings) {
            string[] guids = AssetDatabase.FindAssets("t:ZoneData");
            int updated = 0;

            foreach(string guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ZoneData data = AssetDatabase.LoadAssetAtPath<ZoneData>(path);
                if(data == null) {
                    continue;
                }

                Color newColor = settings.GetColorForRole(data.role);
                if(data.debugColor != newColor) {
                    Undo.RecordObject(data, "Update Zone Color");
                    data.debugColor = newColor;
                    EditorUtility.SetDirty(data);
                    updated++;
                }
            }

            if(updated > 0) {
                AssetDatabase.SaveAssets();
                SceneView.RepaintAll();
            }
        }
    }
}
#endif
