#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Jovian.ZoneSystem.Editor {
    [CustomEditor(typeof(ZoneInstance))]
    public class ZoneInstanceEditor : UnityEditor.Editor {

        // Set to true externally to auto-enable editing when the inspector opens
        internal static bool startEditingOnNextSelect;

        private bool _editingPolygon;
        private ZoneInstance _zone;

        // ── Helpers ──────────────────────────────────────────────────────

        private MapPlane ActivePlane {
            get {
                ZonesObjectHolder mgr = FindFirstObjectByType<ZonesObjectHolder>();
                return mgr != null ? mgr.mapPlane : MapPlane.XZ;
            }
        }

        private float DepthValue {
            get {
                MapPlane plane = ActivePlane;
                return plane == MapPlane.XZ ? _zone.transform.position.y
                    : plane == MapPlane.YZ ? _zone.transform.position.x
                    : _zone.transform.position.z;
            }
        }

        private void OnEnable() {
            _zone = (ZoneInstance)target;
            if(startEditingOnNextSelect) {
                startEditingOnNextSelect = false;
                _editingPolygon = true;
                SceneView.RepaintAll();
            }
        }
        private void OnDisable() {
            _editingPolygon = false;
            Tools.hidden = false;
        }

        // ── Scene GUI ────────────────────────────────────────────────────

        private void OnSceneGUI() {
            if(_zone.data == null) {
                return;
            }

            // Hide the default transform handle when not editing the shape
            if(!_editingPolygon) {
                Tools.hidden = true;
            } else {
                Tools.hidden = false;
            }

            DrawFilledPolygon();

            if(_editingPolygon) {
                // Esc stops editing
                if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) {
                    _editingPolygon = false;
                    Event.current.Use();
                    Repaint();
                    SceneView.RepaintAll();
                    return;
                }

                // Consume default scene input so clicks don't select other objects
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                if(_zone.data.shape == ZoneShape.Circle) {
                    DrawCircleRadiusHandle();
                } else {
                    DrawVertexHandles();
                    HandleEdgeInsert();
                }
            }
        }

        private Vector2 PlaneOrigin => MapPlaneUtility.ProjectToPlane(_zone.transform.position, ActivePlane);

        private Vector3 PolyPointToWorld(Vector2 pt) {
            return MapPlaneUtility.UnprojectFromPlane(pt + PlaneOrigin, ActivePlane, DepthValue);
        }

        private Vector2 WorldToPolyPoint(Vector3 world) {
            return MapPlaneUtility.ProjectToPlane(world, ActivePlane) - PlaneOrigin;
        }

        // ── Inspector ────────────────────────────────────────────────────

        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            EditorGUILayout.Space(8);

            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
            if(GUILayout.Button("✏️  Edit in Zone Editor", GUILayout.Height(30))) {
                ZoneEditorWindow.OpenAndEdit(_zone);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);

            if(_zone.data == null) {
                EditorGUILayout.HelpBox("Assign a ZoneData asset to begin editing.", MessageType.Warning);
                return;
            }

            // Active plane info
            EditorGUILayout.LabelField($"Active Plane: {ActivePlane}  |  Shape: {_zone.data.shape}",
                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 0.8f, 1f) } });

            // ── Vertex List ─────────────────────────────────────────────
            DrawVertexList();

            // ── Shape Editing ───────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);

            GUI.backgroundColor = _editingPolygon ? new Color(0.4f, 0.9f, 0.4f) : Color.white;
            if(GUILayout.Button(_editingPolygon ? "⬛ Stop Editing" : "✏️  Edit Shape", GUILayout.Height(27))) {
                _editingPolygon = !_editingPolygon;
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;

            if(_editingPolygon) {
                if(_zone.data.shape == ZoneShape.Circle) {
                    EditorGUILayout.HelpBox(
                        "• Drag the radius handle to resize the circle",
                        MessageType.Info);
                } else {
                    EditorGUILayout.HelpBox(
                        "• Drag handles to move vertices\n" +
                        "• Ctrl+Click on an edge to insert a vertex\n" +
                        "• Shift+Click a vertex to delete it",
                        MessageType.Info);
                }
            }

            if(_zone.data.shape == ZoneShape.Circle) {
                EditorGUI.BeginChangeCheck();
                float newRadius = EditorGUILayout.FloatField("Circle Radius", _zone.data.circleRadius);
                if(EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(_zone.data, "Change Circle Radius");
                    _zone.data.circleRadius = Mathf.Max(0.1f, newRadius);
                    ShapeFactory.RegenerateCircle(_zone.data);
                    EditorUtility.SetDirty(_zone.data);
                    _zone.RebuildBoundsCache();
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("⊕  Center Transform", GUILayout.Height(27))) {
                RecenterTransformOnZone();
            }
            GUI.backgroundColor = new Color(1f, 0.7f, 0.3f);
            if(GUILayout.Button("↺  Reset Shape", GUILayout.Height(27))) {
                Undo.RecordObject(_zone.data, "Reset Zone Shape");
                _zone.data.polygon.Clear();
                _zone.data.polygon.AddRange(ShapeFactory.CreateDefault(_zone.data.shape));
                if(_zone.data.shape == ZoneShape.Circle) {
                    _zone.data.circleRadius = ShapeFactory.DefaultRadius;
                }
                EditorUtility.SetDirty(_zone.data);
                _zone.RebuildBoundsCache();
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // ── Duplication ─────────────────────────────────────────────
            EditorGUILayout.Space(8);
            if(GUILayout.Button("📋  Duplicate Zone", GUILayout.Height(27))) {
                DuplicateZone();
            }

            // ── Summary ─────────────────────────────────────────────────
            EditorGUILayout.Space(8);
            DrawZoneSummary();
        }

        private void DrawZoneSummary() {
            if(_zone.data == null) {
                return;
            }
            ZoneData d = _zone.data;

            Color roleColor = ZoneEditorSettings.FindOrCreateSettings().GetColorForRole(d.role);

            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = roleColor * 2f;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = prevBg;

            EditorGUILayout.LabelField("Zone Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Role:     {d.role}");
            EditorGUILayout.LabelField($"Priority: {d.priority}");
            EditorGUILayout.LabelField($"Vertices: {d.polygon?.Count ?? 0}");

            switch(d.role) {
                case ZoneRole.Base:
                    EditorGUILayout.LabelField($"Tier:     {d.baseDifficultyTier}");
                    EditorGUILayout.LabelField($"Chance:   {d.baseEncounterChance:P0}");
                    EditorGUILayout.LabelField($"Table:    {d.encounterTableId}");
                    break;
                case ZoneRole.Modifier:
                    EditorGUILayout.LabelField($"Chance ×: {d.encounterChanceMultiplier:F2}");
                    EditorGUILayout.LabelField($"Tier +:   {d.difficultyTierBonus}");
                    break;
                case ZoneRole.Override:
                    EditorGUILayout.LabelField(d.isSafeZone
                        ? "⚑ SAFE ZONE — no encounters"
                        : $"⚑ Override → Tier {d.overrideDifficultyTier}, {d.overrideEncounterChance:P0}");
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        private bool _showVertexList;

        private void DrawVertexList() {
            List<Vector2> pts = _zone.data.polygon;
            if(pts == null || pts.Count == 0) {
                return;
            }

            EditorGUILayout.Space(4);
            _showVertexList = EditorGUILayout.BeginFoldoutHeaderGroup(_showVertexList,
                $"Vertices ({pts.Count})");

            if(_showVertexList) {
                GUIStyle miniStyle = new GUIStyle(EditorStyles.miniLabel) {
                    alignment = TextAnchor.MiddleLeft,
                    richText = true
                };

                for(int i = 0; i < pts.Count; i++) {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField(
                        $"<b>[{i}]</b>  ({pts[i].x:F2}, {pts[i].y:F2})",
                        miniStyle);

                    if(GUILayout.Button("Copy", EditorStyles.miniButton, GUILayout.Width(40))) {
                        EditorGUIUtility.systemCopyBuffer = $"{pts[i].x:F2}, {pts[i].y:F2}";
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawFilledPolygon() {
            List<Vector2> pts = _zone.data.polygon;
            if(pts == null || pts.Count < 3) {
                return;
            }

            Color fill = _zone.data.debugColor;
            Color border = new Color(fill.r, fill.g, fill.b, Mathf.Clamp01(fill.a * 3f));

            Vector3[] verts = new Vector3[pts.Count];
            for(int i = 0; i < pts.Count; i++) {
                verts[i] = PolyPointToWorld(pts[i]);
            }

            // Triangulate to handle concave polygons correctly
            List<int> tris = PolygonUtils.Triangulate(pts);
            Handles.color = fill;
            for(int i = 0; i + 2 < tris.Count; i += 3) {
                Handles.DrawAAConvexPolygon(verts[tris[i]], verts[tris[i + 1]], verts[tris[i + 2]]);
            }

            Handles.color = border;
            for(int i = 0; i < pts.Count; i++) {
                Handles.DrawLine(verts[i], verts[(i + 1) % pts.Count], 2f);
            }

            // Zone label at centroid
            Vector2 centroid2D = PolygonUtils.Centroid(pts);
            Vector3 labelPos = PolyPointToWorld(centroid2D);
            Handles.Label(labelPos, _zone.data.zoneName, new GUIStyle(EditorStyles.boldLabel) {
                normal = { textColor = Color.white },
                fontSize = 11
            });
        }

        private void DrawCircleRadiusHandle() {
            Vector2 center = PolygonUtils.Centroid(_zone.data.polygon);
            Vector2 radiusPoint = center + new Vector2(_zone.data.circleRadius, 0f);
            Vector3 worldRadiusPoint = PolyPointToWorld(radiusPoint);
            float size = HandleUtility.GetHandleSize(worldRadiusPoint) * 0.1f;

            Handles.color = Color.cyan;
            EditorGUI.BeginChangeCheck();
            Vector3 newWorld = Handles.FreeMoveHandle(worldRadiusPoint, size, Vector3.zero, Handles.DotHandleCap);
            if(EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(_zone.data, "Change Circle Radius");
                Vector2 newPlane = WorldToPolyPoint(newWorld);
                float newRadius = Mathf.Max(0.1f, Vector2.Distance(center, newPlane));
                _zone.data.circleRadius = newRadius;
                ShapeFactory.RegenerateCircle(_zone.data);
                EditorUtility.SetDirty(_zone.data);
                _zone.RebuildBoundsCache();
            }
        }

        private void DrawVertexHandles() {
            List<Vector2> pts = _zone.data.polygon;
            if(pts == null) {
                return;
            }

            Event e = Event.current;

            for(int i = 0; i < pts.Count; i++) {
                Vector3 worldPos = PolyPointToWorld(pts[i]);
                float size = HandleUtility.GetHandleSize(worldPos) * 0.08f;

                // Shift+Click → delete vertex (minimum 3)
                if(e.shift && e.type == EventType.MouseDown && e.button == 0) {
                    if(HandleUtility.DistanceToCircle(worldPos, size) < size && pts.Count > 3) {
                        Undo.RecordObject(_zone.data, "Delete Zone Vertex");
                        pts.RemoveAt(i);
                        EditorUtility.SetDirty(_zone.data);
                        _zone.RebuildBoundsCache();
                        e.Use();
                        return;
                    }
                }

                Handles.color = e.shift ? Color.red : Color.yellow;

                EditorGUI.BeginChangeCheck();
                Vector3 newWorld = Handles.FreeMoveHandle(worldPos, size, Vector3.zero, Handles.DotHandleCap);

                if(EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(_zone.data, "Move Zone Vertex");
                    pts[i] = WorldToPolyPoint(newWorld);
                    EditorUtility.SetDirty(_zone.data);
                    _zone.RebuildBoundsCache();
                }
            }
        }

        private void HandleEdgeInsert() {
            List<Vector2> pts = _zone.data.polygon;
            if(pts == null || pts.Count < 2) {
                return;
            }

            Event e = Event.current;

            // Highlight the closest edge while Ctrl is held for visual feedback
            if(e.control) {
                int previewEdge = FindClosestEdge(pts, out _, out float previewDist);
                if(previewEdge >= 0 && previewDist < 20f) {
                    Vector3 a = PolyPointToWorld(pts[previewEdge]);
                    Vector3 b = PolyPointToWorld(pts[(previewEdge + 1) % pts.Count]);
                    Handles.color = Color.cyan;
                    Handles.DrawLine(a, b, 4f);
                    HandleUtility.Repaint();
                }
            }

            if(!e.control || e.type != EventType.MouseDown || e.button != 0) {
                return;
            }

            // 20px screen-space tolerance — camera-distance independent
            int bestEdge = FindClosestEdge(pts, out Vector3 insertPoint, out float dist);
            if(bestEdge >= 0 && dist < 20f) {
                Undo.RecordObject(_zone.data, "Insert Zone Vertex");
                pts.Insert(bestEdge + 1, WorldToPolyPoint(insertPoint));
                EditorUtility.SetDirty(_zone.data);
                _zone.RebuildBoundsCache();
                e.Use();
            }
        }

        /// <summary>
        ///     Finds the polygon edge closest to the mouse in screen space (pixels).
        ///     Returns the edge index to insert after, the world-space insertion point, and pixel distance.
        ///     Screen-space comparison means tolerance is camera-distance independent.
        /// </summary>
        private int FindClosestEdge(List<Vector2> pts, out Vector3 closestPoint, out float closestPixelDist) {
            closestPoint = Vector3.zero;
            closestPixelDist = float.MaxValue;
            int bestEdge = -1;

            Vector2 mouseGUI = Event.current.mousePosition;

            for(int i = 0; i < pts.Count; i++) {
                Vector3 a = PolyPointToWorld(pts[i]);
                Vector3 b = PolyPointToWorld(pts[(i + 1) % pts.Count]);
                Vector2 aScreen = HandleUtility.WorldToGUIPoint(a);
                Vector2 bScreen = HandleUtility.WorldToGUIPoint(b);

                Vector2 ab = bScreen - aScreen;
                float len = ab.sqrMagnitude;
                float t = len > 0.0001f
                    ? Mathf.Clamp01(Vector2.Dot(mouseGUI - aScreen, ab) / len)
                    : 0f;

                float pixelDist = Vector2.Distance(mouseGUI, aScreen + (ab * t));

                if(pixelDist < closestPixelDist) {
                    closestPixelDist = pixelDist;
                    bestEdge = i;
                    closestPoint = Vector3.Lerp(a, b, t);
                }
            }

            return bestEdge;
        }

        // ── Re-center ────────────────────────────────────────────────────

        private void RecenterTransformOnZone() {
            List<Vector2> pts = _zone.data.polygon;
            if(pts == null || pts.Count == 0) {
                return;
            }

            Vector2 centroid = PolygonUtils.Centroid(pts);
            if(centroid.sqrMagnitude < 0.001f) {
                return;
            }

            Undo.RecordObject(_zone.data, "Center Transform on Zone");
            Undo.RecordObject(_zone.transform, "Center Transform on Zone");

            // Shift all polygon points so the centroid becomes (0,0)
            for(int i = 0; i < pts.Count; i++) {
                pts[i] -= centroid;
            }

            // Move the transform so the zone stays in the same world position
            Vector3 worldOffset = MapPlaneUtility.UnprojectFromPlane(centroid, ActivePlane, 0f);
            _zone.transform.position += worldOffset;

            EditorUtility.SetDirty(_zone.data);
            EditorUtility.SetDirty(_zone.transform);
            _zone.RebuildBoundsCache();
            SceneView.RepaintAll();
        }

        // ── Duplication ─────────────────────────────────────────────────

        private void DuplicateZone() {
            if(_zone.data == null) {
                return;
            }

            ZoneData original = _zone.data;
            ZoneEditorSettings settings = ZoneEditorSettings.FindOrCreateSettings();
            string folder = settings.zoneDataFolder;

            // Create independent ZoneData copy
            ZoneData copy = ScriptableObject.CreateInstance<ZoneData>();
            EditorUtility.CopySerialized(original, copy);
            copy.zoneId = original.zoneId + "_" + System.Guid.NewGuid().ToString("N").Substring(0, 6);
            copy.zoneName = original.zoneName + " (Copy)";

            string newName = original.zoneName.Replace(" ", "_") + "_Copy";
            string newPath = AssetDatabase.GenerateUniqueAssetPath(
                System.IO.Path.Combine(folder, newName + ".asset"));

            System.IO.Directory.CreateDirectory(
                System.IO.Path.Combine(Application.dataPath, "..", folder));
            AssetDatabase.CreateAsset(copy, newPath);
            AssetDatabase.SaveAssets();

            // Create scene GameObject
            GameObject duplicate = Instantiate(_zone.gameObject, _zone.transform.parent);
            duplicate.name = copy.zoneName;
            Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate Zone");

            ZoneInstance dupInstance = duplicate.GetComponent<ZoneInstance>();
            dupInstance.data = copy;
            dupInstance.RebuildBoundsCache();

            Vector3 offset = MapPlaneUtility.UnprojectFromPlane(new Vector2(1f, 1f), ActivePlane, 0f);
            duplicate.transform.position += offset;

            Selection.activeGameObject = duplicate;
            SceneView.RepaintAll();

            Debug.Log($"[ZoneSystem] Duplicated zone to {newPath}");
        }
    }
}
#endif
