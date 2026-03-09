using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Jovian.ZoneSystem {
    public static class ZoneResolver {
        /// <summary>
        ///     Resolves a list of overlapping ZoneData into a single ZoneContext.
        ///     Resolution rules:
        ///     1. If any Override zone is present → use highest-priority Override exclusively.
        ///     2. Otherwise → find highest-priority Base zone, then stack all Modifier zones
        ///     multiplicatively on top.
        /// </summary>
        public static ZoneContext Resolve(List<ZoneData> overlapping) {
            if(overlapping == null || overlapping.Count == 0) {
                return SafeFallback(string.Empty);
            }

            // ── 1. Check for Override zones ──────────────────────────────
            var overrides = overlapping
                .Where(z => z.role == ZoneRole.Override)
                .OrderByDescending(z => z.priority)
                .ToList();

            if(overrides.Count > 0) {
                var ov = overrides[0];
                return new ZoneContext {
                    resolvedZoneId = ov.zoneId,
                    isSafe = ov.isSafeZone,
                    encounterTableId = ov.overrideEncounterTableId,
                    finalEncounterChance = ov.overrideEncounterChance,
                    finalDifficultyTier = ov.overrideDifficultyTier
                };
            }

            // ── 2. Find highest-priority Base zone ───────────────────────
            var baseZone = overlapping
                .Where(z => z.role == ZoneRole.Base)
                .OrderByDescending(z => z.priority)
                .FirstOrDefault();

            if(!baseZone) {
                return SafeFallback(string.Empty);
            }

            // ── 3. Collect all Modifier zones ────────────────────────────
            var modifiers = overlapping
                .Where(z => z.role == ZoneRole.Modifier)
                .ToList();

            var chance = baseZone.baseEncounterChance;
            var tierOffset = 0;

            foreach(var mod in modifiers) {
                // Multiplicative stacking — each modifier is independent
                chance *= mod.encounterChanceMultiplier;
                tierOffset += mod.difficultyTierBonus;
            }

            chance = Mathf.Clamp01(chance);
            var rawTier = (int)baseZone.baseDifficultyTier + tierOffset;
            var clampedTier = Mathf.Clamp(rawTier, (int)DifficultyTier.Safe, (int)DifficultyTier.Deadly);

            return new ZoneContext {
                resolvedZoneId = baseZone.zoneId,
                isSafe = false,
                encounterTableId = baseZone.encounterTableId,
                finalEncounterChance = chance,
                finalDifficultyTier = (DifficultyTier)clampedTier
            };
        }

        private static ZoneContext SafeFallback(string name) {
            return new ZoneContext {
                resolvedZoneId = name,
                isSafe = true,
                encounterTableId = string.Empty,
                finalEncounterChance = 0f,
                finalDifficultyTier = DifficultyTier.Safe
            };
        }
    }
}
