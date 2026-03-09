#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Jovian.ZoneSystem.Editor {
    [CreateAssetMenu(fileName = "ZoneEditorSettings", menuName = "Jovian/ZoneSystem/Zone Editor Settings")]
    public class ZoneEditorSettings : ScriptableObject {
        [Tooltip("Which two world axes your map lies on. Match this to your map's plane.")]
        public MapPlane mapPlane = MapPlane.XZ;

        [Tooltip("Folder path where new ZoneData assets are saved (relative to project root).")]
        public string zoneDataFolder = "Assets/ZoneData";

        [Tooltip("Debug color for each zone role. Add entries for any new roles.")]
        public List<ZoneRoleColor> roleColors = new() {
            new ZoneRoleColor { role = ZoneRole.Base, color = new Color(0.2f, 0.6f, 1f, 0.25f) },
            new ZoneRoleColor { role = ZoneRole.Modifier, color = new Color(1f, 0.8f, 0.2f, 0.25f) },
            new ZoneRoleColor { role = ZoneRole.Override, color = new Color(0.3f, 0.9f, 0.3f, 0.25f) }
        };

        public Color GetColorForRole(ZoneRole role) {
            foreach(ZoneRoleColor entry in roleColors) {
                if(entry.role == role) {
                    return entry.color;
                }
            }
            return new Color(1f, 0.5f, 0f, 0.25f);
        }

        /// <summary>
        ///     Ensures every ZoneRole enum value has a color entry.
        ///     Call this after adding new roles to the enum.
        /// </summary>
        public void SyncRoleEntries() {
            ZoneRole[] allRoles = (ZoneRole[])Enum.GetValues(typeof(ZoneRole));
            foreach(ZoneRole role in allRoles) {
                bool found = false;
                foreach(ZoneRoleColor entry in roleColors) {
                    if(entry.role == role) {
                        found = true;
                        break;
                    }
                }
                if(!found) {
                    roleColors.Add(new ZoneRoleColor {
                        role = role,
                        color = new Color(0.5f, 0.5f, 0.5f, 0.25f)
                    });
                }
            }
        }

        [MenuItem("Window/Zone System/Settings")]
        private static void SelectOrCreateSettings() {
            ZoneEditorSettings settings = FindOrCreateSettings();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        [MenuItem("Window/Zone System/Documentation")]
        private static void OpenDocumentation() {
            // Find the Documentation~ folder relative to this package
            string[] guids = AssetDatabase.FindAssets("t:Script ZoneEditorSettings");
            string packagePath = "Packages/com.jovian.zonesystem";
            if(guids.Length > 0) {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                // scriptPath is like "Packages/com.jovian.zonesystem/Editor/ZoneEditorSettings.cs"
                int editorIdx = scriptPath.IndexOf("/Editor/");
                if(editorIdx >= 0) {
                    packagePath = scriptPath.Substring(0, editorIdx);
                }
            }
            string fullPath = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath, "..", packagePath, "Documentation~", "index.html"));
            if(System.IO.File.Exists(fullPath)) {
                Application.OpenURL("file:///" + fullPath.Replace("\\", "/"));
            }
            else {
                Debug.LogWarning($"[ZoneSystem] Documentation not found at: {fullPath}");
            }
        }

        internal static ZoneEditorSettings FindOrCreateSettings() {
            string[] guids = AssetDatabase.FindAssets("t:ZoneEditorSettings");
            if(guids.Length > 0) {
                return AssetDatabase.LoadAssetAtPath<ZoneEditorSettings>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            // Create a new settings asset
            string folder = "Assets";
            ZoneEditorSettings newSettings = CreateInstance<ZoneEditorSettings>();
            newSettings.SyncRoleEntries();
            AssetDatabase.CreateAsset(newSettings, $"{folder}/ZoneEditorSettings.asset");
            AssetDatabase.SaveAssets();
            Debug.Log("[ZoneSystem] Created ZoneEditorSettings at Assets/ZoneEditorSettings.asset");
            return newSettings;
        }
    }

    [Serializable]
    public struct ZoneRoleColor {
        public ZoneRole role;
        public Color color;
    }
}
#endif
