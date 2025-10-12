using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Boil and Reduce ability - AoE fire attack that consumes Confectance Index.
    /// Deals 90% weapon damage increased by 5% per Confectance point consumed.
    /// Applies ScorchMark hediff to all hit enemies.
    /// Dead enemies explode in secondary firestorms.
    /// </summary>
    public class Verb_BoilAndReduce : Verb_CastAbility
    {
        // Track exploded positions to prevent infinite recursion
        private HashSet<IntVec3> explodedPositions = new HashSet<IntVec3>();

        protected override bool TryCastShot()
        {
            try
            {
                // Validate caster
                if (CasterPawn == null)
                {
                    Log.Error("[GFL Weapons] Verb_BoilAndReduce: CasterPawn is null");
                    return false;
                }

                if (CasterPawn.Map == null)
                {
                    Log.Error("[GFL Weapons] Verb_BoilAndReduce: CasterPawn.Map is null");
                    return false;
                }

                // Validate target
                if (!currentTarget.IsValid)
                {
                    Log.Warning("[GFL Weapons] Verb_BoilAndReduce: Invalid target");
                    return false;
                }

                // Get target cell
                IntVec3 targetCell = currentTarget.Cell;

                // Get weapon
                Thing weapon = EquipmentSource;
                if (weapon == null && CasterPawn.equipment != null)
                {
                    weapon = CasterPawn.equipment.Primary;
                }

                // Get and consume Confectance
                int confectanceConsumed = CompConfectance.ConsumeAll(CasterPawn);

                // Calculate base damage
                float baseDamage = WeaponDamageResolver.GetWeaponBaseDamage(CasterPawn, weapon);
                baseDamage *= 0.90f; // 90% of weapon damage

                // Apply Confectance multiplier: +5% per point
                float damageMultiplier = 1f + (0.05f * confectanceConsumed);
                float finalDamage = baseDamage * damageMultiplier;

                // Clear explosion tracking for this cast
                explodedPositions.Clear();

                // Apply damage to all pawns in radius
                ApplyAoEDamage(targetCell, CasterPawn.Map, finalDamage, weapon, CasterPawn);

                // Visual effects - main explosion
                CreateExplosionEffects(targetCell, CasterPawn.Map, 3f);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Verb_BoilAndReduce.TryCastShot exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Apply damage to all enemy pawns in radius.
        /// </summary>
        private void ApplyAoEDamage(IntVec3 center, Map map, float damage, Thing weapon, Pawn caster)
        {
            try
            {
                // Mark this position as exploded to prevent recursion
                explodedPositions.Add(center);

                // Find all pawns in 3-tile radius
                List<Thing> affectedThings = GenRadial.RadialDistinctThingsAround(center, map, 3f, true).ToList();
                List<Pawn> affectedPawns = new List<Pawn>();

                foreach (Thing thing in affectedThings)
                {
                    Pawn pawn = thing as Pawn;
                    if (pawn != null && !pawn.Dead && pawn != caster && pawn.HostileTo(caster))
                    {
                        affectedPawns.Add(pawn);
                    }
                }

                Log.Message($"[GFL Weapons] Boil and Reduce: Found {affectedPawns.Count} enemy pawns in radius, damage={Mathf.RoundToInt(damage)}");

                // Apply damage to each affected pawn
                foreach (Pawn targetPawn in affectedPawns)
                {
                    if (targetPawn == null || targetPawn.Dead)
                    {
                        continue;
                    }

                    // Store map and position BEFORE applying damage (in case pawn dies)
                    Map targetMap = targetPawn.Map;
                    Vector3 targetDrawPos = targetPawn.DrawPos;
                    IntVec3 targetPosition = targetPawn.Position;
                    bool wasAlive = !targetPawn.Dead;

                    // Apply damage
                    DamageInfo dinfo = new DamageInfo(
                        DamageDefOf.Flame,
                        Mathf.RoundToInt(damage),
                        0f,
                        -1f,
                        caster,
                        null,
                        weapon?.def,
                        DamageInfo.SourceCategory.ThingOrUnknown
                    );

                    targetPawn.TakeDamage(dinfo);

                    Log.Message($"[GFL Weapons] Applied {Mathf.RoundToInt(damage)} Flame damage to {targetPawn.LabelShort}, Dead={targetPawn.Dead}");

                    // Apply ScorchMark hediff if still alive
                    if (!targetPawn.Dead)
                    {
                        ApplyScorchMark(targetPawn, caster);

                        // Show damage text - use stored map reference
                        if (targetMap != null)
                        {
                            MoteMaker.ThrowText(targetDrawPos, targetMap, $"{Mathf.RoundToInt(damage)} (boil)", 2.0f);
                        }
                    }
                    else if (wasAlive)
                    {
                        // Corpse explosion - trigger secondary AoE
                        TriggerCorpseExplosion(targetPosition, map, damage * 0.8f, weapon, caster);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Verb_BoilAndReduce.ApplyAoEDamage exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply or stack ScorchMark hediff on target pawn.
        /// </summary>
        private void ApplyScorchMark(Pawn targetPawn, Pawn applier)
        {
            try
            {
                if (targetPawn == null || targetPawn.Dead || targetPawn.health == null)
                {
                    return;
                }

                // Check if pawn already has ScorchMark
                Hediff existingHediff = targetPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("GFL_ScorchMark"));

                if (existingHediff is Hediff_ScorchMark existingScorch)
                {
                    // Add stack to existing hediff
                    existingScorch.AddStack();
                }
                else
                {
                    // Add new ScorchMark hediff
                    Hediff_ScorchMark newScorch = (Hediff_ScorchMark)HediffMaker.MakeHediff(
                        HediffDef.Named("GFL_ScorchMark"),
                        targetPawn
                    );

                    newScorch.SetApplier(applier);
                    targetPawn.health.AddHediff(newScorch);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Verb_BoilAndReduce.ApplyScorchMark exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Trigger corpse explosion at given position.
        /// </summary>
        private void TriggerCorpseExplosion(IntVec3 position, Map map, float damage, Thing weapon, Pawn caster)
        {
            try
            {
                // Guard against infinite recursion
                if (explodedPositions.Contains(position))
                {
                    return;
                }

                // Mark position as exploded
                explodedPositions.Add(position);

                // Visual effects for secondary explosion
                CreateExplosionEffects(position, map, 2f);

                // Apply damage in smaller radius (2 tiles instead of 3)
                List<Thing> affectedThings = GenRadial.RadialDistinctThingsAround(position, map, 2f, true).ToList();

                foreach (Thing thing in affectedThings)
                {
                    Pawn targetPawn = thing as Pawn;
                    if (targetPawn != null && !targetPawn.Dead && targetPawn != caster && targetPawn.HostileTo(caster))
                    {
                        // Store map and position BEFORE applying damage (in case pawn dies)
                        Map targetMap = targetPawn.Map;
                        Vector3 targetDrawPos = targetPawn.DrawPos;
                        IntVec3 targetPosition = targetPawn.Position;
                        bool wasAlive = !targetPawn.Dead;

                        // Apply reduced damage
                        DamageInfo dinfo = new DamageInfo(
                            DamageDefOf.Flame,
                            Mathf.RoundToInt(damage),
                            0f,
                            -1f,
                            caster,
                            null,
                            weapon?.def,
                            DamageInfo.SourceCategory.ThingOrUnknown
                        );

                        targetPawn.TakeDamage(dinfo);

                        // Apply ScorchMark if alive
                        if (!targetPawn.Dead)
                        {
                            ApplyScorchMark(targetPawn, caster);

                            if (targetMap != null)
                            {
                                MoteMaker.ThrowText(targetDrawPos, targetMap, $"{Mathf.RoundToInt(damage)} (chain)", 1.5f);
                            }
                        }
                        else if (wasAlive)
                        {
                            // Chain explosion (reduced damage further)
                            TriggerCorpseExplosion(targetPosition, map, damage * 0.6f, weapon, caster);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Verb_BoilAndReduce.TriggerCorpseExplosion exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Create visual explosion effects.
        /// </summary>
        private void CreateExplosionEffects(IntVec3 position, Map map, float radius)
        {
            try
            {
                // Fire glow
                FleckMaker.ThrowFireGlow(position.ToVector3Shifted(), map, radius);

                // Smoke
                FleckMaker.ThrowSmoke(position.ToVector3Shifted(), map, radius);

                // Heat wave
                FleckMaker.ThrowHeatGlow(position, map, radius);
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Verb_BoilAndReduce.CreateExplosionEffects exception: {ex.Message}");
            }
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = true;
            return 3f; // Show 3-tile AoE radius
        }
    }
}
