using System;
using Verse;
using RimWorld;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Helper class to resolve base weapon damage for ability calculations.
    /// Priority: Projectile damage → StatBases → Skill-based fallback
    /// </summary>
    public static class WeaponDamageResolver
    {
        /// <summary>
        /// Get the base damage value for a weapon held by a pawn.
        /// </summary>
        /// <param name="caster">The pawn wielding the weapon</param>
        /// <param name="weapon">The weapon Thing</param>
        /// <returns>Base damage value as float</returns>
        public static float GetWeaponBaseDamage(Pawn caster, Thing weapon)
        {
            try
            {
                if (weapon == null)
                {
                    Log.Warning("[GFL Weapons] WeaponDamageResolver: weapon is null, using fallback");
                    return GetFallbackDamage(caster);
                }

                // Priority 1: Check projectile damage
                if (weapon.def.Verbs != null && weapon.def.Verbs.Count > 0)
                {
                    var verb = weapon.def.Verbs[0];
                    if (verb.defaultProjectile != null && verb.defaultProjectile.projectile != null)
                    {
                        int projectileDamage = verb.defaultProjectile.projectile.GetDamageAmount(weapon);
                        if (projectileDamage > 0)
                        {
                            return projectileDamage;
                        }
                    }
                }

                // Priority 2: Check statBases for RangedWeapon_DamageAmount
                if (weapon.def.statBases != null)
                {
                    foreach (var statModifier in weapon.def.statBases)
                    {
                        if (statModifier.stat != null && 
                            statModifier.stat.defName == "RangedWeapon_DamageAmount")
                        {
                            if (statModifier.value > 0f)
                            {
                                return statModifier.value;
                            }
                        }
                    }
                }

                // Priority 3: Fallback to skill-based calculation
                Log.Warning($"[GFL Weapons] WeaponDamageResolver: Could not find damage for {weapon.def.defName}, using fallback calculation");
                return GetFallbackDamage(caster);
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] WeaponDamageResolver exception: {ex.Message}\n{ex.StackTrace}");
                return 10f; // Safe default
            }
        }

        /// <summary>
        /// Calculate fallback damage based on shooter's skill level.
        /// </summary>
        private static float GetFallbackDamage(Pawn caster)
        {
            if (caster == null)
            {
                return 10f;
            }

            int shootingLevel = 1;
            if (caster.skills != null)
            {
                var shootingSkill = caster.skills.GetSkill(SkillDefOf.Shooting);
                if (shootingSkill != null)
                {
                    shootingLevel = shootingSkill.Level;
                }
            }

            return 5f + (shootingLevel * 0.5f);
        }
    }
}
