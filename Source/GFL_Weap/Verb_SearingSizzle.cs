using System;
using Verse;
using RimWorld;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Searing Sizzle ability - Single target burn attack with distance bonus.
    /// Deals 120% weapon damage as Burn damage.
    /// Targets within 4 tiles receive +5% bonus and +2 flat damage.
    /// </summary>
    public class Verb_SearingSizzle : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            try
            {
                // Validate caster
                if (CasterPawn == null)
                {
                    Log.Error("[GFL Weapons] Verb_SearingSizzle: CasterPawn is null");
                    return false;
                }

                if (CasterPawn.Map == null)
                {
                    Log.Error("[GFL Weapons] Verb_SearingSizzle: CasterPawn.Map is null");
                    return false;
                }

                // Validate target
                if (!currentTarget.HasThing)
                {
                    Log.Warning("[GFL Weapons] Verb_SearingSizzle: Target has no thing");
                    return false;
                }

                Pawn targetPawn = currentTarget.Thing as Pawn;
                if (targetPawn == null)
                {
                    Log.Warning("[GFL Weapons] Verb_SearingSizzle: Target is not a pawn");
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

                // Apply 120% multiplier
                float damage = baseDamage * 1.20f;

                // Check distance for close-range bonus
                float distance = CasterPawn.Position.DistanceTo(targetPawn.Position);
                if (distance <= 4f)
                {
                    // Apply +5% multiplier and +2 flat damage
                    damage = (damage * 1.05f) + 2f;
                }

                // Apply damage as Flame type
                DamageInfo dinfo = new DamageInfo(
                    DamageDefOf.Flame,
                    Mathf.RoundToInt(damage),
                    0f,
                    -1f,
                    CasterPawn,
                    null,
                    weapon?.def,
                    DamageInfo.SourceCategory.ThingOrUnknown
                );

                targetPawn.TakeDamage(dinfo);

                // Visual effects - fire mote with blue/orange tint
                if (targetPawn.Map != null)
                {
                    // Throw fire glow effect
                    FleckMaker.ThrowFireGlow(targetPawn.Position.ToVector3Shifted(), targetPawn.Map, 1.5f);
                    
                    // Throw smoke
                    FleckMaker.ThrowSmoke(targetPawn.Position.ToVector3Shifted(), targetPawn.Map, 1.0f);

                    // Show damage number
                    MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, $"{Mathf.RoundToInt(damage)} (sear)", 2.5f);
                }

                // Add Confectance on successful hit
                if (!targetPawn.Dead)
                {
                    CompConfectance.Add(CasterPawn, 1);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Verb_SearingSizzle.TryCastShot exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = true;
            return 0f; // Single target, no AoE highlight
        }
    }
}
