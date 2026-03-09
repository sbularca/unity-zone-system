using System.Collections.Generic;
using UnityEngine;

namespace Jovian.ZoneSystem {
    [CreateAssetMenu(fileName = "NewZone", menuName = "ZoneSystem/Zone Data")]
    public class ZoneData : ScriptableObject {
        [Header("Identity")]
        public string zoneId;

        public string zoneName;
        public ZoneRole role = ZoneRole.Base;
        public int priority = 1;

        [Header("Visual (Editor Only)")]
        public Color debugColor = new(1f, 0.5f, 0f, 0.25f);

        // ── Base zone fields ────────────────────────────────────────────
        [Header("Base Zone Settings")]
        [Tooltip("Only used when Role = Base")]
        public string encounterTableId;

        [Tooltip("Only used when Role = Base")]
        public DifficultyTier baseDifficultyTier = DifficultyTier.Mild;

        [Tooltip("Base encounter chance per check (0..1). Only used when Role = Base")]
        [Range(0f, 1f)]
        public float baseEncounterChance = 0.2f;

        // ── Modifier zone fields ─────────────────────────────────────────
        [Header("Modifier Zone Settings")]
        [Tooltip("Multiplied onto the base encounter chance. Only used when Role = Modifier")]
        public float encounterChanceMultiplier = 1f;

        [Tooltip("Added to the base difficulty tier (clamped). Only used when Role = Modifier")]
        public int difficultyTierBonus;

        // ── Override zone fields ─────────────────────────────────────────
        [Header("Override Zone Settings")]
        [Tooltip("If true, no encounters occur in this zone. Only used when Role = Override")]
        public bool isSafeZone;

        [Tooltip("Only used when Role = Override and isSafeZone = false")]
        public string overrideEncounterTableId;

        [Tooltip("Only used when Role = Override and isSafeZone = false")]
        [Range(0f, 1f)]
        public float overrideEncounterChance = 1f;

        [Tooltip("Only used when Role = Override and isSafeZone = false")]
        public DifficultyTier overrideDifficultyTier = DifficultyTier.Deadly;

        // ── Shape ────────────────────────────────────────────────────────
        [HideInInspector]
        public ZoneShape shape = ZoneShape.Square;

        [HideInInspector]
        public float circleRadius = 2f;

        [HideInInspector]
        public List<Vector2> polygon = new();
    }
}
