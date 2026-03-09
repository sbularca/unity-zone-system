#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Jovian.ZoneSystem.Editor {
    /// <summary>
    ///     Main Zone Editor window.
    ///     Open via: Window → Zone System → Zone Editor
    /// </summary>
    public class ZoneEditorWindow : EditorWindow {

        private string _exportPath = "Assets/StreamingAssets/zones.json";

        // ── Create form state ───────────────────────────────────────────
        private bool _showCreateForm;
        private string _newZoneName = "New Zone";
        private ZoneShape _newZoneShape = ZoneShape.Square;

        // ── Edit state ──────────────────────────────────────────────────
        private ZoneInstance _editingZone;
        private SerializedObject _editingSO;
        private bool _isUnsavedNewData;
        private bool _hasUnsavedChanges;
        private string _saveError;
        private ZoneShape _shapeOnEditStart;

        // ── Scroll / foldouts ───────────────────────────────────────────
        private Vector2 _scrollPos;
        private bool _showExportFoldout = true;

        // ── GUI ──────────────────────────────────────────────────────────
        private void OnGUI() {
            DrawHeader();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if(_editingZone != null) {
                DrawEditSection();
            } else {
                DrawCreateButton();
                if(_showCreateForm) {
                    DrawCreateForm();
                }
                EditorGUILayout.Space(6);
                DrawSceneZonesList();
            }

            EditorGUILayout.Space(6);
            DrawExportSection();

            EditorGUILayout.EndScrollView();
        }

        private void OnFocus() {
            ValidateEditingState();
            Repaint();
        }
        private void OnHierarchyChange() {
            ValidateEditingState();
            Repaint();
        }
        private void OnSelectionChange() {
            ValidateEditingState();
            Repaint();
        }

        private void ValidateEditingState() {
            if(_editingZone == null && _editingSO != null) {
                _editingSO = null;
            }
        }

        [MenuItem("Window/Zone System/Zone Editor")]
        public static void Open() {
            GetWindow<ZoneEditorWindow>("Zone Editor");
        }

        public static void OpenAndEdit(ZoneInstance zone) {
            ZoneEditorWindow window = GetWindow<ZoneEditorWindow>("Zone Editor");
            window.EnterEditMode(zone);
        }

        // ── Header ──────────────────────────────────────────────────────

        private void DrawHeader() {
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.3f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = prevBg;

            EditorGUILayout.LabelField("🗺  Zone System Editor", new GUIStyle(EditorStyles.boldLabel) {
                fontSize = 14,
                normal = { textColor = new Color(0.8f, 0.9f, 1f) }
            });
            EditorGUILayout.LabelField("Define map zones for encounter difficulty and chance.",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        // ── Create Button + Dropdown ────────────────────────────────────

        private void DrawCreateButton() {
            GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
            if(GUILayout.Button("➕  Create New Zone", GUILayout.Height(30))) {
                _showCreateForm = !_showCreateForm;
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawCreateForm() {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.indentLevel++;
            _newZoneName = EditorGUILayout.TextField("Zone Name", _newZoneName);
            _newZoneShape = (ZoneShape)EditorGUILayout.EnumPopup("Shape", _newZoneShape);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("All zone data can be edited after creation.",
                EditorStyles.miniLabel);

            if(GUILayout.Button("Create & Edit", GUILayout.Height(26))) {
                CreateZoneInScene();
            }

            EditorGUILayout.EndVertical();
        }

        // ── Edit Section ────────────────────────────────────────────────

        private void EnterEditMode(ZoneInstance zone) {
            _editingZone = zone;
            _editingSO = zone.data != null ? new SerializedObject(zone.data) : null;
            _showCreateForm = false;
            _shapeOnEditStart = zone.data != null ? zone.data.shape : ZoneShape.Polygon;

            Selection.activeGameObject = zone.gameObject;
            SceneView.FrameLastActiveSceneView();
            ZoneInstanceEditor.startEditingOnNextSelect = true;
        }

        private void ExitEditMode() {
            // If exiting with unsaved new data, clean up the in-memory asset
            if(_isUnsavedNewData && _editingZone != null && _editingZone.data != null) {
                DestroyImmediate(_editingZone.data);
                _editingZone.data = null;
            }
            _editingZone = null;
            _editingSO = null;
            _isUnsavedNewData = false;
            _hasUnsavedChanges = false;
            _saveError = null;
        }

        private void DrawEditSection() {
            // Validate that the zone still exists
            if(_editingZone == null || _editingZone.data == null) {
                ExitEditMode();
                return;
            }

            // Back button
            EditorGUILayout.BeginHorizontal();
            Color prevBackBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if(GUILayout.Button("← Back", GUILayout.Height(36), GUILayout.Width(70))) {
                ExitEditMode();
                GUI.backgroundColor = prevBackBg;
                EditorGUILayout.EndHorizontal();
                return;
            }
            GUI.backgroundColor = prevBackBg;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            ZoneData d = _editingZone.data;

            // Zone header with color swatch
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.3f, 0.4f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = prevBg;

            string headerLabel = _isUnsavedNewData ? $"New Zone: {d.zoneName}" : $"Editing: {d.zoneName}";
            EditorGUILayout.LabelField(headerLabel, new GUIStyle(EditorStyles.boldLabel) {
                fontSize = 13,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) }
            });

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // Draw the ZoneData fields using SerializedObject
            if(_editingSO == null) {
                return;
            }

            _editingSO.Update();

            // Identity
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_editingSO.FindProperty("zoneId"), new GUIContent("Zone ID"));
            EditorGUILayout.PropertyField(_editingSO.FindProperty("zoneName"), new GUIContent("Zone Name"));

            // Track role changes to auto-apply color
            SerializedProperty rolePropForColor = _editingSO.FindProperty("role");
            ZoneRole roleBefore = (ZoneRole)rolePropForColor.enumValueIndex;
            EditorGUILayout.PropertyField(rolePropForColor, new GUIContent("Role"));
            ZoneRole roleAfter = (ZoneRole)rolePropForColor.enumValueIndex;
            if(roleBefore != roleAfter) {
                ZoneEditorSettings settings = FindSettings();
                _editingSO.FindProperty("debugColor").colorValue = settings.GetColorForRole(roleAfter);
            }

            EditorGUILayout.PropertyField(_editingSO.FindProperty("priority"), new GUIContent("Priority"));
            EditorGUILayout.PropertyField(_editingSO.FindProperty("shape"), new GUIContent("Shape"));

            SerializedProperty shapeProp = _editingSO.FindProperty("shape");
            if((ZoneShape)shapeProp.enumValueIndex == ZoneShape.Circle) {
                EditorGUILayout.PropertyField(_editingSO.FindProperty("circleRadius"), new GUIContent("Circle Radius"));
            }

            EditorGUILayout.Space(8);

            // Role-specific fields
            SerializedProperty roleProp = _editingSO.FindProperty("role");
            ZoneRole role = (ZoneRole)roleProp.enumValueIndex;
            switch(role) {
                case ZoneRole.Base:
                    EditorGUILayout.LabelField("Base Zone Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_editingSO.FindProperty("encounterTableId"), new GUIContent("Encounter Table ID"));
                    EditorGUILayout.PropertyField(_editingSO.FindProperty("baseDifficultyTier"), new GUIContent("Difficulty Tier"));
                    EditorGUILayout.PropertyField(_editingSO.FindProperty("baseEncounterChance"), new GUIContent("Encounter Chance"));
                    break;
                case ZoneRole.Modifier:
                    EditorGUILayout.LabelField("Modifier Zone Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_editingSO.FindProperty("encounterChanceMultiplier"), new GUIContent("Chance Multiplier"));
                    EditorGUILayout.PropertyField(_editingSO.FindProperty("difficultyTierBonus"), new GUIContent("Difficulty Tier Bonus"));
                    break;
                case ZoneRole.Override:
                    EditorGUILayout.LabelField("Override Zone Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_editingSO.FindProperty("isSafeZone"), new GUIContent("Is Safe Zone"));
                    if(!_editingSO.FindProperty("isSafeZone").boolValue) {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(_editingSO.FindProperty("overrideEncounterTableId"), new GUIContent("Encounter Table ID"));
                        EditorGUILayout.PropertyField(_editingSO.FindProperty("overrideEncounterChance"), new GUIContent("Encounter Chance"));
                        EditorGUILayout.PropertyField(_editingSO.FindProperty("overrideDifficultyTier"), new GUIContent("Difficulty Tier"));
                        EditorGUI.indentLevel--;
                    }
                    break;
            }

            if(_editingSO.ApplyModifiedProperties()) {
                _hasUnsavedChanges = true;
            }

            EditorGUILayout.Space(8);

            // Save button — shown for new unsaved zones or any modified existing zone
            if(_isUnsavedNewData || _hasUnsavedChanges) {
                // Show error if any
                if(!string.IsNullOrEmpty(_saveError)) {
                    EditorGUILayout.HelpBox(_saveError, MessageType.Error);
                }

                GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
                if(GUILayout.Button("💾  Save Zone", GUILayout.Height(30))) {
                    SaveZoneData();
                }
                GUI.backgroundColor = Color.white;
            }

            // Delete button at the bottom
            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if(GUILayout.Button("🗑  Delete Zone", GUILayout.Height(27))) {
                ZoneInstance zone = _editingZone;
                ExitEditMode();
                DeleteZone(zone);
            }
            GUI.backgroundColor = Color.white;
        }

        // ── Create ──────────────────────────────────────────────────────

        private static ZoneEditorSettings FindSettings() {
            return ZoneEditorSettings.FindOrCreateSettings();
        }

        private void CreateZoneInScene() {
            // Create in-memory ZoneData (saved when user clicks Save)
            ZoneEditorSettings settings = FindSettings();
            ZoneData data = CreateInstance<ZoneData>();
            data.zoneId = _newZoneName.ToLower().Replace(" ", "_");
            data.zoneName = _newZoneName;
            data.shape = _newZoneShape;
            data.debugColor = settings.GetColorForRole(data.role);
            data.polygon.AddRange(ShapeFactory.CreateDefault(_newZoneShape));

            // Create the scene GameObject
            GameObject go = new GameObject(_newZoneName);
            Undo.RegisterCreatedObjectUndo(go, "Create Zone");

            ZoneInstance inst = go.AddComponent<ZoneInstance>();
            inst.data = data;

            // Try to parent under ZoneManager if it exists
            ZonesObjectHolder mgr = FindFirstObjectByType<ZonesObjectHolder>();
            if(mgr != null) {
                go.transform.SetParent(mgr.transform, true);
            }

            _showCreateForm = false;
            _isUnsavedNewData = true;
            _saveError = null;
            EnterEditMode(inst);
        }

        private void CreateDataForZone(ZoneInstance zone) {
            string zoneName = zone.gameObject.name;
            ZoneEditorSettings settings = FindSettings();

            // Create in-memory ZoneData (not saved as asset yet)
            ZoneData data = CreateInstance<ZoneData>();
            data.zoneId = zoneName.ToLower().Replace(" ", "_");
            data.zoneName = zoneName;
            data.shape = ZoneShape.Polygon;
            data.debugColor = settings.GetColorForRole(data.role);
            data.polygon.AddRange(ShapeFactory.CreateDefault(ZoneShape.Polygon));

            zone.data = data;
            _isUnsavedNewData = true;
            _saveError = null;
            EnterEditMode(zone);
        }

        private void SaveZoneData() {
            ZoneData data = _editingZone.data;
            string zoneId = data.zoneId;
            string assetName = data.zoneName.Replace(" ", "_");

            // Check for duplicate zoneId or asset name among existing ZoneData assets
            string[] existingGuids = AssetDatabase.FindAssets("t:ZoneData");
            foreach(string guid in existingGuids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ZoneData existing = AssetDatabase.LoadAssetAtPath<ZoneData>(path);
                if(existing == null || existing == data) {
                    continue;
                }
                if(existing.zoneId == zoneId) {
                    _saveError = $"A ZoneData asset with ID '{zoneId}' already exists at:\n{path}";
                    return;
                }
                string existingAssetName = Path.GetFileNameWithoutExtension(path);
                if(existingAssetName == assetName) {
                    _saveError = $"A ZoneData asset named '{assetName}' already exists at:\n{path}";
                    return;
                }
            }

            // Reset polygon if shape type changed since edit started
            if(data.shape != _shapeOnEditStart) {
                data.polygon.Clear();
                data.polygon.AddRange(ShapeFactory.CreateDefault(data.shape));
                if(data.shape == ZoneShape.Circle) {
                    data.circleRadius = ShapeFactory.DefaultRadius;
                }
                _editingZone.RebuildBoundsCache();
                SceneView.RepaintAll();
            }
            _shapeOnEditStart = data.shape;

            if(_isUnsavedNewData) {
                // New zone — create the asset for the first time
                ZoneEditorSettings settings = FindSettings();
                string folder = settings != null ? settings.zoneDataFolder : "Assets/ZoneData";
                string soPath = $"{folder}/{assetName}.asset";
                string fullFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", folder));
                Directory.CreateDirectory(fullFolder);

                AssetDatabase.CreateAsset(data, soPath);
                AssetDatabase.SaveAssets();

                // Rebuild the SerializedObject now that the asset is persisted
                _editingSO = new SerializedObject(data);
                _isUnsavedNewData = false;

                Debug.Log($"[ZoneSystem] Created ZoneData '{data.zoneName}' at {soPath}");
            }
            else {
                // Existing zone — rename asset if needed, then save
                string currentPath = AssetDatabase.GetAssetPath(data);
                string currentAssetName = Path.GetFileNameWithoutExtension(currentPath);
                if(currentAssetName != assetName) {
                    string renameError = AssetDatabase.RenameAsset(currentPath, assetName);
                    if(!string.IsNullOrEmpty(renameError)) {
                        _saveError = $"Failed to rename asset: {renameError}";
                        return;
                    }
                }

                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();

                Debug.Log($"[ZoneSystem] Saved ZoneData '{data.zoneName}'");
            }

            // Rename the GameObject to match the zone name
            Undo.RecordObject(_editingZone.gameObject, "Rename Zone GameObject");
            _editingZone.gameObject.name = data.zoneName;

            EditorUtility.SetDirty(_editingZone);
            _hasUnsavedChanges = false;
            _saveError = null;
        }

        // ── Scene Zones List ────────────────────────────────────────────

        private void DrawSceneZonesList() {
            EditorGUILayout.LabelField("Scene Zones", EditorStyles.boldLabel);

            // Show active map plane from ZoneManager
            ZonesObjectHolder mgr = FindFirstObjectByType<ZonesObjectHolder>();
            if(mgr != null) {
                EditorGUILayout.LabelField($"Map Plane: {mgr.mapPlane}  (set on ZoneManager)",
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 0.8f, 1f) } });
            }
            else {
                EditorGUILayout.HelpBox("No ZoneManager found in scene.", MessageType.Warning);
            }

            ZoneInstance[] zones = FindObjectsByType<ZoneInstance>(FindObjectsSortMode.None)
                .OrderByDescending(z => z.data?.priority ?? 0)
                .ThenBy(z => z.data?.zoneName ?? "")
                .ToArray();

            if(zones.Length == 0) {
                EditorGUILayout.HelpBox("No ZoneInstance objects found in the scene.", MessageType.Info);
                return;
            }

            foreach(ZoneInstance zone in zones) {
                DrawZoneRow(zone);
            }
        }

        private void DrawZoneRow(ZoneInstance zone) {
            if(zone.data == null) {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                // Warning icon
                GUIContent warnIcon = EditorGUIUtility.IconContent("console.warnicon.sml");
                EditorGUILayout.LabelField(warnIcon, GUILayout.Width(18), GUILayout.Height(20));

                EditorGUILayout.LabelField($"{zone.gameObject.name}: Missing ZoneData", EditorStyles.miniLabel);

                // Add & Edit button — creates a ZoneData asset and enters edit mode
                if(GUILayout.Button("+ Add & Edit", GUILayout.Width(90), GUILayout.Height(20))) {
                    CreateDataForZone(zone);
                }

                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                if(GUILayout.Button("✕", GUILayout.Width(28), GUILayout.Height(20))) {
                    if(EditorUtility.DisplayDialog("Delete Zone",
                        $"Delete '{zone.gameObject.name}'? (no ZoneData asset to remove)", "Delete", "Cancel")) {
                        Undo.DestroyObjectImmediate(zone.gameObject);
                    }
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                return;
            }

            ZoneData d = zone.data;
            Color roleColor = FindSettings().GetColorForRole(d.role);

            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = (roleColor * 2f * 0.6f) + (Color.gray * 0.4f);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUI.backgroundColor = prevBg;

            // Color swatch
            Rect swatchRect = GUILayoutUtility.GetRect(12, 20, GUILayout.Width(12));
            EditorGUI.DrawRect(swatchRect, roleColor * 3f);

            // Info
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"{d.zoneName}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(BuildZoneSummaryString(d), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            // Select / Edit button
            if(GUILayout.Button("Select", GUILayout.Width(55), GUILayout.Height(36))) {
                EnterEditMode(zone);
            }

            // Duplicate button
            if(GUILayout.Button("📋", GUILayout.Width(28), GUILayout.Height(36))) {
                DuplicateZone(zone);
            }

            // Delete button
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if(GUILayout.Button("✕", GUILayout.Width(28), GUILayout.Height(36))) {
                DeleteZone(zone);
            }
            GUI.backgroundColor = prevBg;

            EditorGUILayout.EndHorizontal();
        }

        private void DeleteZone(ZoneInstance zone) {
            string zoneName = zone.data != null ? zone.data.zoneName : zone.gameObject.name;
            string assetPath = zone.data != null ? AssetDatabase.GetAssetPath(zone.data) : null;

            string message = $"Delete zone '{zoneName}'?";
            if(!string.IsNullOrEmpty(assetPath)) {
                message += $"\n\nThis will also delete the asset:\n{assetPath}";
            }

            if(!EditorUtility.DisplayDialog("Delete Zone", message, "Delete", "Cancel")) {
                return;
            }

            if(!string.IsNullOrEmpty(assetPath)) {
                AssetDatabase.DeleteAsset(assetPath);
            }

            Undo.DestroyObjectImmediate(zone.gameObject);
        }

        private void DuplicateZone(ZoneInstance zone) {
            if(zone.data == null) {
                return;
            }

            ZoneData original = zone.data;
            ZoneEditorSettings settings = FindSettings();
            string folder = settings.zoneDataFolder;

            // Create independent ZoneData copy
            ZoneData copy = CreateInstance<ZoneData>();
            EditorUtility.CopySerialized(original, copy);
            copy.zoneId = original.zoneId + "_" + System.Guid.NewGuid().ToString("N").Substring(0, 6);
            copy.zoneName = original.zoneName + " (Copy)";

            string newName = original.zoneName.Replace(" ", "_") + "_Copy";
            string newPath = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(folder, newName + ".asset"));

            string fullFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", folder));
            Directory.CreateDirectory(fullFolder);
            AssetDatabase.CreateAsset(copy, newPath);
            AssetDatabase.SaveAssets();

            // Create scene GameObject
            GameObject duplicate = Instantiate(zone.gameObject, zone.transform.parent);
            duplicate.name = copy.zoneName;
            Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate Zone");

            ZoneInstance dupInstance = duplicate.GetComponent<ZoneInstance>();
            dupInstance.data = copy;
            dupInstance.RebuildBoundsCache();

            // Offset slightly so it's not on top of the original
            ZonesObjectHolder mgr = FindFirstObjectByType<ZonesObjectHolder>();
            MapPlane plane = mgr != null ? mgr.mapPlane : MapPlane.XZ;
            Vector3 offset = MapPlaneUtility.UnprojectFromPlane(new Vector2(1f, 1f), plane, 0f);
            duplicate.transform.position += offset;

            Debug.Log($"[ZoneSystem] Duplicated zone to {newPath}");
        }

        private string BuildZoneSummaryString(ZoneData d) {
            switch(d.role) {
                case ZoneRole.Base:
                    return $"Base | Priority {d.priority} | {d.baseDifficultyTier} | {d.baseEncounterChance:P0} | Table: {d.encounterTableId}";
                case ZoneRole.Modifier:
                    return $"Modifier | Priority {d.priority} | Chance ×{d.encounterChanceMultiplier:F2} | Tier +{d.difficultyTierBonus}";
                case ZoneRole.Override:
                    return d.isSafeZone
                        ? $"Override | Priority {d.priority} | ✓ SAFE"
                        : $"Override | Priority {d.priority} | {d.overrideDifficultyTier} | {d.overrideEncounterChance:P0}";
                default: return "";
            }
        }

        // ── Export Section ───────────────────────────────────────────────
        private void DrawExportSection() {
            _showExportFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_showExportFoldout, "Export Zones to JSON");
            if(_showExportFoldout) {
                EditorGUI.indentLevel++;
                _exportPath = EditorGUILayout.TextField("Output Path", _exportPath);

                EditorGUILayout.BeginHorizontal();
                if(GUILayout.Button("Browse…", GUILayout.Width(70))) {
                    string picked = EditorUtility.SaveFilePanel(
                        "Save zones.json", Path.GetDirectoryName(_exportPath),
                        Path.GetFileName(_exportPath), "json");
                    if(!string.IsNullOrEmpty(picked)) {
                        _exportPath = "Assets" + picked.Substring(Application.dataPath.Length);
                    }
                }

                GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                if(GUILayout.Button("📦  Export Now", GUILayout.Height(24))) {
                    ExportZones();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void ExportZones() {
            ZoneInstance[] instances = FindObjectsByType<ZoneInstance>(FindObjectsSortMode.None);
            ZonesObjectHolder mgr = FindFirstObjectByType<ZonesObjectHolder>();
            MapPlane plane = mgr != null ? mgr.mapPlane : MapPlane.XZ;
            ZoneExportRoot root = ZoneExporter.BuildExport(instances, plane);
            string json = ZoneExporter.ToJson(root);

            string fullPath = Path.Combine(Application.dataPath, "../", _exportPath);
            fullPath = Path.GetFullPath(fullPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, json);

            AssetDatabase.Refresh();
            Debug.Log($"[ZoneSystem] Exported {root.zones.Count} zones → {fullPath}");
            EditorUtility.DisplayDialog("Zone Export",
                $"Successfully exported {root.zones.Count} zone(s) to:\n{_exportPath}", "OK");
        }
    }
}
#endif
