using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Fissioned Firelight ability verb.
    /// Deals 120% weapon damage, applies 2 Rend stacks (max 8), grants +2 Confectance Index.
    /// </summary>
    public class Verb_FissionedFirelight : Verb_CastAbility
    {
        // Damage multiplier (120%)
        private const float DAMAGE_MULTIPLIER = 1.20f;

        // Rend stacks to apply
        private const int REND_STACKS_TO_APPLY = 2;

        // Confectance Index to grant
        private const int CONFECTANCE_TO_GRANT = 2;

        protected override bool TryCastShot()
        {
            try
            {
                // Validate caster
                if (CasterPawn == null)
                {
                    Log.Error("[GFL Weapons] Verb_FissionedFirelight: CasterPawn is null");
                    return false;
                }

                if (CasterPawn.Map == null)
                {
                    Log.Error("[GFL Weapons] Verb_FissionedFirelight: CasterPawn.Map is null");
                    return false;
                }

                // Validate target
                if (!currentTarget.HasThing)
                {
                    Log.Warning("[GFL Weapons] Verb_FissionedFirelight: Target has no thing");
                    return false;
                }

                Pawn targetPawn = currentTarget.Thing as Pawn;
                if (targetPawn == null)
                {
                    Log.Warning("[GFL Weapons] Verb_FissionedFirelight: Target is not a pawn");
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

                // Calculate damage
                float baseDamage = WeaponDamageResolver.GetWeaponBaseDamage(CasterPawn, weapon);
                float damage = baseDamage * DAMAGE_MULTIPLIER;

                // Apply skill scaling (1% per shooting level)
                if (CasterPawn.skills != null)
                {
                    var shootingSkill = CasterPawn.skills.GetSkill(SkillDefOf.Shooting);
                    if (shootingSkill != null)
                    {
                        float skillBonus = 1f + (shootingSkill.Level * 0.01f);
                        damage *= skillBonus;
                    }
                }

                // Apply damage
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

                // Store references before damage (pawn might die)
                Map targetMap = targetPawn.Map;
                Vector3 targetDrawPos = targetPawn.DrawPos;
                IntVec3 targetPos = targetPawn.Position;
                
                targetPawn.TakeDamage(dinfo);

                // Apply Rend stacks (only if alive)
                if (!targetPawn.Dead && targetPawn.health != null)
                {
                    ApplyRendStacks(targetPawn, REND_STACKS_TO_APPLY);
                }

                // Grant Confectance Index to caster
                GrantConfectanceIndex(CasterPawn, CONFECTANCE_TO_GRANT);

                // Visual effects (check if pawn still exists)
                if (targetMap != null && targetPawn.Spawned)
                {
                    FleckMaker.ThrowLightningGlow(targetDrawPos, targetMap, 1.5f);
                    FleckMaker.ThrowDustPuffThick(targetPos.ToVector3Shifted(), targetMap, 1.0f, Color.red);
                    MoteMaker.ThrowText(targetDrawPos, targetMap, $"{Mathf.RoundToInt(damage)} (firelight)", 2.5f);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Verb_FissionedFirelight.TryCastShot exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Apply Rend stacks to target.
        /// </summary>
        private void ApplyRendStacks(Pawn target, int stacksToAdd)
        {
            try
            {
                if (target == null || target.Dead || target.health == null)
                {
                    return;
                }

                // Check for existing Rend
                Hediff existingHediff = target.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("GFL_Hediff_Rend"));

                if (existingHediff is Hediff_Rend existingRend)
                {
                    // Add stacks to existing
                    existingRend.AddStacks(stacksToAdd);
                }
                else
                {
                    // Add new Rend hediff
                    Hediff_Rend newRend = (Hediff_Rend)HediffMaker.MakeHediff(
                        HediffDef.Named("GFL_Hediff_Rend"),
                        target
                    );

                    newRend.SetApplier(CasterPawn);
                    newRend.AddStacks(stacksToAdd - 1); // -1 because PostAdd initializes with 1
                    target.health.AddHediff(newRend);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Verb_FissionedFirelight.ApplyRendStacks exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Grant Confectance Index to caster.
        /// </summary>
        private void GrantConfectanceIndex(Pawn caster, int amount)
        {
            try
            {
                if (caster == null || caster.health == null)
                {
                    return;
                }

                // Check for existing Confectance Index
                Hediff existingHediff = caster.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("GFL_Hediff_ConfectanceIndex_Hestia"));

                if (existingHediff is Hediff_ConfectanceIndex existingConfectance)
                {
                    // Add stacks
                    existingConfectance.AddStacks(amount);
                }
                else
                {
                    // Add new Confectance Index hediff
                    Hediff_ConfectanceIndex newConfectance = (Hediff_ConfectanceIndex)HediffMaker.MakeHediff(
                        HediffDef.Named("GFL_Hediff_ConfectanceIndex_Hestia"),
                        caster
                    );

                    newConfectance.AddStacks(amount - 1); // -1 because PostAdd initializes with 1
                    caster.health.AddHediff(newConfectance);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Verb_FissionedFirelight.GrantConfectanceIndex exception: {ex.Message}");
            }
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = true;
            return 0f; // Single target
        }
    }
}
