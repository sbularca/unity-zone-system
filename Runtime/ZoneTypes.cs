namespace Jovian.ZoneSystem {
    public enum ZoneRole {
        Base, // Provides the encounter table and baseline difficulty
        Modifier, // Mutates difficulty/chance on top of a base zone
        Override // Completely replaces everything (safe towns, story events)
    }

    public enum ZoneShape {
        Square,
        Circle,
        Polygon
    }

    public enum DifficultyTier {
        Safe = 0,
        Mild = 1,
        Moderate = 2,
        Dangerous = 3,
        Deadly = 4
    }

    /// <summary>
    ///     The resolved result of overlapping zones at a world position.
    ///     This is what the encounter system consumes — it never needs to know about raw zones.
    /// </summary>
    public struct ZoneContext {
        public string encounterTableId;
        public float finalEncounterChance; // 0..1
        public DifficultyTier finalDifficultyTier;
        public bool isSafe;
        public string resolvedZoneId;
    }
}
