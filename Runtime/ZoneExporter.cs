using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jovian.ZoneSystem {
    /// <summary>
    ///     Serializable representations for JSON export.
    ///     Kept in Runtime so server-side or headless builds can also consume them.
    /// </summary>
    [Serializable]
    public class ZoneExportEntry {
        public string id;
        public string name;
        public string role;
        public int priority;

        // Base
        public string encounterTableId;
        public int baseDifficultyTier;
        public float baseEncounterChance;

        // Modifier
        public float encounterChanceMultiplier;
        public int difficultyTierBonus;

        // Override
        public bool isSafeZone;
        public string overrideEncounterTableId;
        public float overrideEncounterChance;
        public int overrideDifficultyTier;

        // Shape
        public string shape;
        public float circleRadius;
        public float[] position;
        public List<float[]> polygon;
    }

    [Serializable]
    public class ZoneExportRoot {
        public List<ZoneExportEntry> zones = new();
    }

    public static class ZoneExporter {
        public static ZoneExportRoot BuildExport(ZoneInstance[] instances, MapPlane plane = MapPlane.XZ) {
            ZoneExportRoot root = new ZoneExportRoot();

            foreach(ZoneInstance inst in instances) {
                if(inst.data == null) {
                    continue;
                }
                ZoneData d = inst.data;
                Vector3 pos = inst.transform.position;
                Vector2 origin = MapPlaneUtility.ProjectToPlane(pos, plane);

                ZoneExportEntry entry = new ZoneExportEntry {
                    id = d.zoneId,
                    name = d.zoneName,
                    role = d.role.ToString(),
                    priority = d.priority,
                    shape = d.shape.ToString(),
                    circleRadius = d.circleRadius,
                    position = new[] { pos.x, pos.y, pos.z },
                    encounterTableId = d.encounterTableId,
                    baseDifficultyTier = (int)d.baseDifficultyTier,
                    baseEncounterChance = d.baseEncounterChance,
                    encounterChanceMultiplier = d.encounterChanceMultiplier,
                    difficultyTierBonus = d.difficultyTierBonus,
                    isSafeZone = d.isSafeZone,
                    overrideEncounterTableId = d.overrideEncounterTableId,
                    overrideEncounterChance = d.overrideEncounterChance,
                    overrideDifficultyTier = (int)d.overrideDifficultyTier,
                    polygon = new List<float[]>()
                };

                foreach(Vector2 pt in d.polygon) {
                    Vector2 worldPt = pt + origin;
                    entry.polygon.Add(new[] { worldPt.x, worldPt.y });
                }

                root.zones.Add(entry);
            }

            return root;
        }

        public static string ToJson(ZoneExportRoot root, bool pretty = true) {
            return JsonUtility.ToJson(root, pretty);
        }
    }
}
