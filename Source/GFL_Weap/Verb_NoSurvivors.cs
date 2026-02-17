using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// No Survivors ability verb.
    /// Deals 160% weapon damage to primary target.
    /// Spreads Gash stacks (equal to target's Rend) to all Rend holders within 6 tiles.
    /// If target has 6+ Rend stacks, consume 6 and gain +120% bonus damage (total 280%).
    /// </summary>
    public class Verb_NoSurvivors : Verb_CastAbility
    {
        // Base damage multiplier (160%)
        private const float BASE_DAMAGE_MULTIPLIER = 1.60f;

        // Bonus damage multiplier when consuming 6+ Rend (additional +120%)
        private const float BONUS_DAMAGE_MULTIPLIER = 1.20f;

        // Minimum Rend stacks for bonus
        private const int MIN_REND_FOR_BONUS = 6;

        // Rend stacks to consume for bonus
        private const int REND_STACKS_TO_CONSUME = 6;

        // AoE radius for Gash spread
        private const float AOE_RADIUS = 6f;

        protected override bool TryCastShot()
        {
            try
            {
                // Validate caster
                if (CasterPawn == null)
                {
                    Log.Error("[GFL Weapons] Verb_NoSurvivors: CasterPawn is null");
                    return false;
                }

                if (CasterPawn.Map == null)
                {
                    Log.Error("[GFL Weapons] Verb_NoSurvivors: CasterPawn.Map is null");
                    return false;
                }

                // Validate target
                if (!currentTarget.HasThing)
                {
                    Log.Warning("[GFL Weapons] Verb_NoSurvivors: Target has no thing");
                    return false;
                }

                Pawn targetPawn = currentTarget.Thing as Pawn;
                if (targetPawn == null)
                {
                    Log.Warning("[GFL Weapons] Verb_NoSurvivors: Target is not a pawn");
                    return false;
                }

                if (targetPawn.Dead)
                {
                    return false;
                }

                // Get weapon
                Thing weapon = EquipmentSource;
                if (weapon == null && CasterPawn.equipment != null)
                {
                    weapon = CasterPawn.equipment.Primary;
                }

                // Calculate base damage
                float baseDamage = WeaponDamageResolver.GetWeaponBaseDamage(CasterPawn, weapon);
                float damage = baseDamage * BASE_DAMAGE_MULTIPLIER;

                // Apply skill scaling
                if (CasterPawn.skills != null)
                {
                    var shootingSkill = CasterPawn.skills.GetSkill(SkillDefOf.Shooting);
                    if (shootingSkill != null)
                    {
                        float skillBonus = 1f + (shootingSkill.Level * 0.01f);
                        damage *= skillBonus;
                    }
                }

                // Check target's Rend stacks
                Hediff_Rend targetRend = targetPawn.health?.hediffSet.GetFirstHediffOfDef(HediffDef.Named("GFL_Hediff_Rend")) as Hediff_Rend;
                int targetRendStacks = targetRend?.Stacks ?? 0;
                bool applyBonus = targetRendStacks >= MIN_REND_FOR_BONUS;

                // Apply bonus damage if 6+ Rend stacks
                if (applyBonus)
                {
                    damage *= (1f + BONUS_DAMAGE_MULTIPLIER); // Total 1.6 * 2.2 = 3.52x or 280%
                    
                    // Consume 6 Rend stacks
                    targetRend?.ConsumeStacks(REND_STACKS_TO_CONSUME);
                }

                // Apply damage to primary target
                DamageInfo dinfo = new DamageInfo(
                    DamageDefOf.Bullet,
                    Mathf.RoundToInt(damage),
                    0f,
                    -1f,
                    CasterPawn,
                    null,
                    weapon?.def,
                    DamageInfo.SourceCategory.ThingOrUnknown
                );

                // Store position before damage for AoE spreading (pawn might die)
                IntVec3 targetPos = targetPawn.Position;
                Map targetMap = targetPawn.Map;
                Vector3 targetDrawPos = targetPawn.DrawPos;
                
                targetPawn.TakeDamage(dinfo);

                // Spread Gash to all Rend holders in AoE (use stored position)
                if (targetMap != null)
                {
                    SpreadGashToRendHolders(targetPos, targetMap, targetRendStacks, baseDamage);
                }

                // Visual effects (check if pawn still exists)
                if (targetMap != null && targetPawn.Spawned)
                {
                    // Main target effects
                    FleckMaker.ThrowExplosionCell(targetPos, targetMap, FleckDefOf.ExplosionFlash, Color.red);
                    FleckMaker.ThrowSmoke(targetPos.ToVector3Shifted(), targetMap, 2f);
                    
                    string damageText = applyBonus ? $"{Mathf.RoundToInt(damage)} (EXECUTE!)" : $"{Mathf.RoundToInt(damage)} (no survivors)";
                    MoteMaker.ThrowText(targetDrawPos, targetMap, damageText, 3f);

                    // AoE indicator
                    FleckMaker.ThrowLightningGlow(targetPos.ToVector3Shifted(), targetMap, AOE_RADIUS);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Verb_NoSurvivors.TryCastShot exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Spread Gash stacks to all pawns with Rend within AoE radius.
        /// </summary>
        private void SpreadGashToRendHolders(IntVec3 center, Map map, int gashStacksToApply, float applierAttackDamage)
        {
            try
            {
                if (gashStacksToApply <= 0)
                {
                    return;
                }

                // Find all pawns in radius
                List<Thing> affectedThings = GenRadial.RadialDistinctThingsAround(center, map, AOE_RADIUS, true).ToList();

                foreach (Thing thing in affectedThings)
                {
                    Pawn pawn = thing as Pawn;
                    if (pawn == null || pawn.Dead || pawn == CasterPawn)
                    {
                        continue;
                    }

                    // Check if pawn has Rend
                    Hediff_Rend rend = pawn.health?.hediffSet.GetFirstHediffOfDef(HediffDef.Named("GFL_Hediff_Rend")) as Hediff_Rend;
                    if (rend == null)
                    {
                        continue;
                    }

                    // Apply Gash stacks
                    ApplyGashStacks(pawn, gashStacksToApply, applierAttackDamage);

                    // Visual feedback
                    if (pawn.Map != null)
                    {
                        FleckMaker.ThrowDustPuffThick(pawn.Position.ToVector3Shifted(), pawn.Map, 0.8f, new Color(0.8f, 0.1f, 0.1f));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Verb_NoSurvivors.SpreadGashToRendHolders exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply Gash stacks to a pawn.
        /// </summary>
        private void ApplyGashStacks(Pawn target, int stacksToAdd, float applierAttackDamage)
        {
            try
            {
                if (target == null || target.Dead || target.health == null)
                {
                    return;
                }

                // Check for existing Gash
                Hediff existingHediff = target.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("GFL_Hediff_Gash"));

                if (existingHediff is Hediff_Gash existingGash)
                {
                    // Add stacks
                    existingGash.AddStacks(stacksToAdd);
                }
                else
                {
                    // Add new Gash hediff
                    Hediff_Gash newGash = (Hediff_Gash)HediffMaker.MakeHediff(
                        HediffDef.Named("GFL_Hediff_Gash"),
                        target
                    );

                    newGash.SetApplier(CasterPawn, applierAttackDamage);
                    newGash.AddStacks(stacksToAdd - 1); // -1 because PostAdd initializes with 1
                    target.health.AddHediff(newGash);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Verb_NoSurvivors.ApplyGashStacks exception: {ex.Message}");
            }
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = true;
            return AOE_RADIUS; // Show 6-tile AoE
        }
    }
}
