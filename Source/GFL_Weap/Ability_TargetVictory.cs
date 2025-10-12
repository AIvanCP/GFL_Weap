using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Verb for Target Victory ability - fires electrical bolt dealing 130% weapon damage and applies Conductivity
    /// </summary>
    public class Verb_TargetVictory : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            try
            {
                if (currentTarget.HasThing && currentTarget.Thing is Pawn targetPawn)
                {
                    // Get weapon base damage
                    float weaponBaseDamage = GetWeaponBaseDamage();
                    
                    // Calculate 130% damage
                    int abilityDamage = Mathf.RoundToInt(weaponBaseDamage * 1.3f);
                    
                    // Visual: Spawn electric projectile
                    ThingDef projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail("GFL_Projectile_TargetVictory");
                    if (projectileDef != null)
                    {
                        Thing spawnedThing = GenSpawn.Spawn(projectileDef, CasterPawn.Position, CasterPawn.Map);
                        Projectile_TargetVictory projectile = spawnedThing as Projectile_TargetVictory;
                        
                        if (projectile != null)
                        {
                            projectile.Launch(
                                CasterPawn,
                                CasterPawn.DrawPos,
                                currentTarget,
                                currentTarget,
                                ProjectileHitFlags.IntendedTarget
                            );

                            // Store damage and caster for projectile impact
                            projectile.abilityDamage = abilityDamage;
                            projectile.abilityCaster = CasterPawn;
                            
                            // Visual flash
                            FleckMaker.ThrowLightningGlow(CasterPawn.DrawPos, CasterPawn.Map, 1.2f);
                        }
                        else
                        {
                            Log.Error("[GFL Weapons] Failed to cast spawned projectile to Projectile_TargetVictory!");
                            ApplyDirectDamage(targetPawn, abilityDamage);
                        }
                    }
                    else
                    {
                        Log.Error("[GFL Weapons] GFL_Projectile_TargetVictory not found! Applying damage directly.");
                        ApplyDirectDamage(targetPawn, abilityDamage);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Verb_TargetVictory.TryCastShot: {ex}");
            }

            return false;
        }

        /// <summary>
        /// Get base damage from equipped Mosin weapon
        /// </summary>
        private float GetWeaponBaseDamage()
        {
            // Mosin Nagant base damage is 28 (from XML)
            // Hardcoded for reliability and compatibility with all RimWorld versions
            return 28f;
        }

        /// <summary>
        /// Apply damage directly if projectile spawn fails
        /// </summary>
        private void ApplyDirectDamage(Pawn targetPawn, int damage)
        {
            try
            {
                if (targetPawn == null || targetPawn.Dead || targetPawn.Map == null)
                {
                    return;
                }
                
                DamageInfo damageInfo = new DamageInfo(DamageDefOf.Bullet, damage, 0.35f, -1f, CasterPawn);
                targetPawn.TakeDamage(damageInfo);
                
                // Apply Conductivity hediff
                HediffDef conductivityDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_Conductivity");
                if (conductivityDef != null && targetPawn.health != null)
                {
                    targetPawn.health.AddHediff(conductivityDef);
                    MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, "Conductivity!", new Color(0.4f, 0.8f, 1f), 2f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error applying direct damage: {ex}");
            }
        }
    }

    /// <summary>
    /// Projectile for Target Victory - applies damage and Conductivity on impact
    /// </summary>
    public class Projectile_TargetVictory : Projectile
    {
        public int abilityDamage = 28;
        public Pawn abilityCaster;

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            try
            {
                Map map = base.Map;
                IntVec3 position = base.Position;

                if (hitThing is Pawn targetPawn && !targetPawn.Dead)
                {
                    // Store map and position BEFORE applying damage (in case pawn dies)
                    Map targetMap = targetPawn.Map;
                    Vector3 targetDrawPos = targetPawn.DrawPos;
                    IntVec3 targetPosition = targetPawn.Position;

                    // Check Combat Extended compatibility
                    bool hasCE = ModsConfig.IsActive("CETeam.CombatExtended");
                    
                    // Apply damage
                    DamageInfo damageInfo = new DamageInfo(DamageDefOf.Bullet, abilityDamage, 0.35f, -1f, abilityCaster ?? launcher);
                    targetPawn.TakeDamage(damageInfo);

                    // Apply Conductivity hediff (only if still alive)
                    HediffDef conductivityDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_Conductivity");
                    if (conductivityDef != null && targetPawn.health != null && !targetPawn.Dead)
                    {
                        // Remove existing Conductivity first (refresh duration)
                        Hediff existingConductivity = targetPawn.health.hediffSet.GetFirstHediffOfDef(conductivityDef);
                        if (existingConductivity != null)
                        {
                            targetPawn.health.RemoveHediff(existingConductivity);
                        }
                        
                        // Add fresh Conductivity
                        targetPawn.health.AddHediff(conductivityDef);
                        
                        // Visual feedback - use stored map reference
                        if (targetMap != null)
                        {
                            MoteMaker.ThrowText(targetDrawPos, targetMap, "Conductivity!", new Color(0.4f, 0.8f, 1f), 2f);
                        }
                    }

                    // Spawn electric sparks - use stored map reference
                    if (targetMap != null)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            FleckMaker.ThrowLightningGlow(targetDrawPos, targetMap, 0.6f);
                        }
                    }
                }

                base.Impact(hitThing, blockedByShield);
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Projectile_TargetVictory.Impact: {ex}");
                base.Impact(hitThing, blockedByShield);
            }
        }
    }
}
