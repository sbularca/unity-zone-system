#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Jovian.ZoneSystem.Editor {
    /// <summary>
    ///     Custom inspector for ZoneData ScriptableObject.
    ///     Shows only fields relevant to the selected ZoneRole.
    /// </summary>
    [CustomEditor(typeof(ZoneData))]
    public class ZoneDataEditor : UnityEditor.Editor {

        // Modifier
        private SerializedProperty _chanceMultiplier, _tierBonus;

        // Base
        private SerializedProperty _encounterTableId, _baseDifficultyTier, _baseEncounterChance;

        // Override
        private SerializedProperty _isSafeZone, _overrideTableId, _overrideChance, _overrideTier;
        private SerializedProperty _zoneId, _zoneName, _role, _priority, _debugColor;
        private SerializedProperty _shape, _circleRadius;

        private void OnEnable() {
            _zoneId = serializedObject.FindProperty("zoneId");
            _zoneName = serializedObject.FindProperty("zoneName");
            _role = serializedObject.FindProperty("role");
            _priority = serializedObject.FindProperty("priority");
            _debugColor = serializedObject.FindProperty("debugColor");
            _shape = serializedObject.FindProperty("shape");
            _circleRadius = serializedObject.FindProperty("circleRadius");

            _encounterTableId = serializedObject.FindProperty("encounterTableId");
            _baseDifficultyTier = serializedObject.FindProperty("baseDifficultyTier");
            _baseEncounterChance = serializedObject.FindProperty("baseEncounterChance");

            _chanceMultiplier = serializedObject.FindProperty("encounterChanceMultiplier");
            _tierBonus = serializedObject.FindProperty("difficultyTierBonus");

            _isSafeZone = serializedObject.FindProperty("isSafeZone");
            _overrideTableId = serializedObject.FindProperty("overrideEncounterTableId");
            _overrideChance = serializedObject.FindProperty("overrideEncounterChance");
            _overrideTier = serializedObject.FindProperty("overrideDifficultyTier");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            // Identity
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_zoneId, new GUIContent("Zone ID"));
            EditorGUILayout.PropertyField(_zoneName, new GUIContent("Zone Name"));
            // Track role changes to auto-apply color
            ZoneRole roleBefore = (ZoneRole)_role.enumValueIndex;
            EditorGUILayout.PropertyField(_role, new GUIContent("Role"));
            ZoneRole roleAfter = (ZoneRole)_role.enumValueIndex;
            if(roleBefore != roleAfter) {
                ZoneEditorSettings settings = ZoneEditorSettings.FindOrCreateSettings();
                _debugColor.colorValue = settings.GetColorForRole(roleAfter);
            }

            EditorGUILayout.PropertyField(_priority, new GUIContent("Priority"));
            EditorGUILayout.PropertyField(_shape, new GUIContent("Shape"));

            if((ZoneShape)_shape.enumValueIndex == ZoneShape.Circle) {
                EditorGUILayout.PropertyField(_circleRadius, new GUIContent("Circle Radius"));
            }

            EditorGUILayout.Space(8);

            // Role-specific fields
            ZoneRole role = (ZoneRole)_role.enumValueIndex;
            switch(role) {
                case ZoneRole.Base:
                    DrawBaseFields();
                    break;
                case ZoneRole.Modifier:
                    DrawModifierFields();
                    break;
                case ZoneRole.Override:
                    DrawOverrideFields();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBaseFields() {
            EditorGUILayout.LabelField("Base Zone Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Base zones define the encounter table and baseline difficulty. " +
                "Only the highest-priority Base zone at a position is used.",
                MessageType.None);

            EditorGUILayout.PropertyField(_encounterTableId, new GUIContent("Encounter Table ID"));
            EditorGUILayout.PropertyField(_baseDifficultyTier, new GUIContent("Difficulty Tier"));
            EditorGUILayout.PropertyField(_baseEncounterChance, new GUIContent("Encounter Chance"));
        }

        private void DrawModifierFields() {
            EditorGUILayout.LabelField("Modifier Zone Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Modifier zones adjust an overlapping Base zone's values multiplicatively. " +
                "All Modifier zones at a position are stacked.",
                MessageType.None);

            EditorGUILayout.PropertyField(_chanceMultiplier, new GUIContent("Chance Multiplier"));
            EditorGUILayout.PropertyField(_tierBonus, new GUIContent("Difficulty Tier Bonus"));
        }

        private void DrawOverrideFields() {
            EditorGUILayout.LabelField("Override Zone Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Override zones completely replace all other zones at this position. " +
                "Useful for story events, towns, and safe areas. " +
                "Highest-priority Override wins if multiple are present.",
                MessageType.None);

            EditorGUILayout.PropertyField(_isSafeZone, new GUIContent("Is Safe Zone"));

            if(!_isSafeZone.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_overrideTableId, new GUIContent("Encounter Table ID"));
                EditorGUILayout.PropertyField(_overrideChance, new GUIContent("Encounter Chance"));
                EditorGUILayout.PropertyField(_overrideTier, new GUIContent("Difficulty Tier"));
                EditorGUI.indentLevel--;
            }
        }
    }
}
#endif
