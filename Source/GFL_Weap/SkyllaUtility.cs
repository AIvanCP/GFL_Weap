using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Context for tracking explosion trigger source
    /// </summary>
    public static class SkyllaExplosionContext
    {
        public static bool IsProjectileTrigger = false;
    }

    /// <summary>
    /// Utility class for Skylla weapon effects with sprite-based explosions
    /// </summary>
    public static class SkyllaUtility
    {
        private static FleckDef purpleExplosionDef = null;
        private static FleckDef purpleSparkDef = null;

        // Lazy load fleck defs
        private static FleckDef PurpleExplosionDef
        {
            get
            {
                if (purpleExplosionDef == null)
                {
                    purpleExplosionDef = DefDatabase<FleckDef>.GetNamedSilentFail("GFL_Fleck_PurpleExplosion");
                }
                return purpleExplosionDef;
            }
        }

        private static FleckDef PurpleSparkDef
        {
            get
            {
                if (purpleSparkDef == null)
                {
                    purpleSparkDef = DefDatabase<FleckDef>.GetNamedSilentFail("GFL_Fleck_PurpleSpark");
                }
                return purpleSparkDef;
            }
        }

        /// <summary>
        /// Main corrosion explosion with sprite-based visuals (temporary effect like vanilla)
        /// </summary>
        public static void DoCorrosionExplosion(IntVec3 position, Map map, float radius, Pawn instigator)
        {
            try
            {
                if (map == null) return;

                // Use vanilla explosion flash for immediate visibility
                FleckMaker.Static(position.ToVector3Shifted(), map, FleckDefOf.ExplosionFlash, 12f);
                
                // Spawn temporary purple explosion visual overlays across the radius
                SpawnPurpleExplosionArea(position, map, radius);

                // Add purple dust puffs for effect (reduced amount, no rotation)
                for (int i = 0; i < 8; i++)
                {
                    Vector3 randomPos = position.ToVector3Shifted() + Gen.RandomHorizontalVector(radius * 0.7f);
                    // Use static dust that doesn't rotate
                    FleckMaker.Static(randomPos, map, FleckDefOf.DustPuffThick, 1.2f);
                }

                // Spawn purple sparks only for projectile explosions
                if (SkyllaExplosionContext.IsProjectileTrigger)
                {
                    SpawnPurpleSparks(position, map, radius);
                    SkyllaExplosionContext.IsProjectileTrigger = false; // Reset flag
                }
                
                // Add smoke for extra effect
                FleckMaker.ThrowSmoke(position.ToVector3Shifted(), map, radius * 1.5f);
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in DoCorrosionExplosion: {ex}");
            }
        }

        /// <summary>
        /// Reduced explosion for Devastating Drift ultimate ability
        /// </summary>
        public static void DoCorrosionExplosion_Ultimate(IntVec3 position, Map map, float radius)
        {
            try
            {
                if (map == null) return;

                // Use vanilla explosion flash for trail visibility
                FleckMaker.Static(position.ToVector3Shifted(), map, FleckDefOf.ExplosionFlash, radius * 4f);
                
                // Spawn temporary purple explosion visual (reduced for trail effect)
                SpawnPurpleExplosionArea(position, map, radius * 0.7f);

                // Add purple dust for trail effect (reduced amount, no rotation)
                for (int i = 0; i < 5; i++)
                {
                    Vector3 randomPos = position.ToVector3Shifted() + Gen.RandomHorizontalVector(radius * 0.5f);
                    // Use static dust that doesn't rotate
                    FleckMaker.Static(randomPos, map, FleckDefOf.DustPuffThick, 0.9f);
                }
                
                // NO sparks for ultimate ability
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in DoCorrosionExplosion_Ultimate: {ex}");
            }
        }

        /// <summary>
        /// Spawn purple explosion area with multiple overlapping visual effects (like vanilla explosion)
        /// </summary>
        private static void SpawnPurpleExplosionArea(IntVec3 position, Map map, float radius)
        {
            try
            {
                if (PurpleExplosionDef == null)
                {
                    Log.Warning("[GFL Weapons] GFL_Fleck_PurpleExplosion not found, using fallback effects");
                    // Fallback: Use vanilla explosion effects with purple tint
                    FleckMaker.Static(position.ToVector3Shifted(), map, FleckDefOf.ExplosionFlash, radius * 3f);
                    return;
                }

                // Get all cells within explosion radius
                List<IntVec3> affectedCells = GenRadial.RadialCellsAround(position, radius, true).ToList();
                
                // Limit number of cells to reasonable amount (1 fleck per tile)
                int maxCells = Mathf.Min(affectedCells.Count, 30);

                // Spawn ONE purple explosion fleck per cell (no duplicates)
                for (int i = 0; i < maxCells; i++)
                {
                    IntVec3 cell = affectedCells[i];
                    if (!cell.InBounds(map)) continue;

                    Vector3 drawPos = cell.ToVector3Shifted();
                    
                    // Fixed 1.0 tile scale - exactly one tile per fleck, no overlap
                    float scale = 1.0f;
                    
                    // Spawn purple explosion fleck at this cell - use Static system
                    FleckCreationData fleckData = FleckMaker.GetDataStatic(drawPos, map, PurpleExplosionDef, scale);
                    fleckData.rotationRate = 0f; // No rotation
                    fleckData.rotation = Rand.RangeInclusive(0, 3) * 90f; // Static rotation: 0, 90, 180, 270 degrees
                    map.flecks.CreateFleck(fleckData);
                }
                
                // Central explosion burst using PurpleSpark instead of large PurpleExplosion
                if (PurpleSparkDef != null)
                {
                    Vector3 centerPos = position.ToVector3Shifted();
                    // Spawn multiple purple sparks for center burst effect
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 sparkPos = centerPos + Gen.RandomHorizontalVector(1.5f);
                        FleckCreationData sparkFleck = FleckMaker.GetDataStatic(sparkPos, map, PurpleSparkDef, 0.8f);
                        sparkFleck.rotationRate = Rand.Range(-3f, 3f);
                        sparkFleck.velocityAngle = Rand.Range(0, 360);
                        sparkFleck.velocitySpeed = Rand.Range(0.3f, 1.2f);
                        map.flecks.CreateFleck(sparkFleck);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in SpawnPurpleExplosionArea: {ex}");
            }
        }

        /// <summary>
        /// Spawn purple ground overlay sprite at center (kept for backwards compatibility)
        /// </summary>
        private static void SpawnPurpleGroundOverlay(IntVec3 position, Map map, float radius)
        {
            if (PurpleExplosionDef == null)
            {
                Log.Warning("[GFL Weapons] GFL_Fleck_PurpleExplosion not found");
                return;
            }

            Vector3 drawPos = position.ToVector3Shifted();
            float scale = radius * 2.0f; // Scale with explosion radius for better visibility

            FleckCreationData fleckData = FleckMaker.GetDataStatic(drawPos, map, PurpleExplosionDef, scale);
            fleckData.rotationRate = 0f;
            fleckData.solidTimeOverride = Rand.Range(0.4f, 0.6f);
            map.flecks.CreateFleck(fleckData);
        }

        /// <summary>
        /// Spawn purple spark particles (only for projectile explosions)
        /// </summary>
        private static void SpawnPurpleSparks(IntVec3 position, Map map, float radius)
        {
            try
            {
                // Always use vanilla micro sparks as they're reliable and visible
                Vector3 center = position.ToVector3Shifted();
                for (int i = 0; i < 25; i++)
                {
                    Vector3 spawnPos = center + Gen.RandomHorizontalVector(radius);
                    FleckMaker.ThrowMicroSparks(spawnPos, map);
                }
                
                // Add additional purple dust puffs for color
                for (int i = 0; i < 8; i++)
                {
                    Vector3 spawnPos = center + Gen.RandomHorizontalVector(radius * 0.7f);
                    FleckMaker.ThrowDustPuffThick(spawnPos, map, 0.8f, new Color(0.8f, 0.2f, 1f));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in SpawnPurpleSparks: {ex}");
            }
        }

        /// <summary>
        /// Legacy method for backwards compatibility - redirects to new system
        /// </summary>
        [Obsolete("Use DoCorrosionExplosion or DoCorrosionExplosion_Ultimate instead")]
        public static void CreatePurpleExplosionEffects(IntVec3 position, Map map, float radius)
        {
            DoCorrosionExplosion(position, map, radius, null);
        }
    }
}
