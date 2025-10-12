using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Verb for Declaration of Victory ability - fires powerful lightning surge dealing 180% weapon damage
    /// If target has Conductivity, applies Paralysis
    /// </summary>
    public class Verb_DeclarationOfVictory : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            try
            {
                if (currentTarget.HasThing && currentTarget.Thing is Pawn targetPawn)
                {
                    // Get weapon base damage (Mosin = 28)
                    float weaponBaseDamage = 28f;
                    
                    // Calculate 180% damage
                    int abilityDamage = Mathf.RoundToInt(weaponBaseDamage * 1.8f);
                    
                    // Check if target has Conductivity
                    bool hasConductivity = false;
                    HediffDef conductivityDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_Conductivity");
                    if (conductivityDef != null && targetPawn.health != null)
                    {
                        hasConductivity = targetPawn.health.hediffSet.HasHediff(conductivityDef);
                    }
                    
                    // Visual: Spawn powerful electric projectile
                    ThingDef projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail("GFL_Projectile_DeclarationOfVictory");
                    if (projectileDef != null)
                    {
                        Thing spawnedThing = GenSpawn.Spawn(projectileDef, CasterPawn.Position, CasterPawn.Map);
                        Projectile_DeclarationOfVictory projectile = spawnedThing as Projectile_DeclarationOfVictory;
                        
                        if (projectile != null)
                        {
                            projectile.Launch(
                                CasterPawn,
                                CasterPawn.DrawPos,
                                currentTarget,
                                currentTarget,
                                ProjectileHitFlags.IntendedTarget
                            );

                            // Store damage, caster, and conductivity state for projectile impact
                            projectile.abilityDamage = abilityDamage;
                            projectile.abilityCaster = CasterPawn;
                            projectile.targetHadConductivity = hasConductivity;
                            
                            // Powerful visual flash
                            FleckMaker.ThrowLightningGlow(CasterPawn.DrawPos, CasterPawn.Map, 1.8f);
                            FleckMaker.Static(CasterPawn.DrawPos, CasterPawn.Map, FleckDefOf.ExplosionFlash, 1.2f);
                        }
                        else
                        {
                            Log.Error("[GFL Weapons] Failed to cast spawned projectile to Projectile_DeclarationOfVictory!");
                            ApplyDirectDamage(targetPawn, abilityDamage, hasConductivity);
                        }
                    }
                    else
                    {
                        Log.Error("[GFL Weapons] GFL_Projectile_DeclarationOfVictory not found! Applying damage directly.");
                        ApplyDirectDamage(targetPawn, abilityDamage, hasConductivity);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Verb_DeclarationOfVictory.TryCastShot: {ex}");
            }

            return false;
        }

        /// <summary>
        /// Apply damage directly if projectile spawn fails
        /// </summary>
        private void ApplyDirectDamage(Pawn targetPawn, int damage, bool hadConductivity)
        {
            try
            {
                if (targetPawn == null || targetPawn.Dead || targetPawn.Map == null)
                {
                    return;
                }
                
                DamageInfo damageInfo = new DamageInfo(DamageDefOf.Bullet, damage, 0.35f, -1f, CasterPawn);
                targetPawn.TakeDamage(damageInfo);
                
                // If target had Conductivity, apply Paralysis
                if (hadConductivity)
                {
                    HediffDef paralysisDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_Paralysis");
                    if (paralysisDef != null && targetPawn.health != null)
                    {
                        targetPawn.health.AddHediff(paralysisDef);
                        MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, "Paralyzed!", new Color(1f, 0.85f, 0.4f), 2.5f);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error applying direct damage: {ex}");
            }
        }
    }

    /// <summary>
    /// Projectile for Declaration of Victory - applies massive damage and Paralysis if Conductivity present
    /// </summary>
    public class Projectile_DeclarationOfVictory : Projectile
    {
        public int abilityDamage = 50;
        public Pawn abilityCaster;
        public bool targetHadConductivity = false;

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

                    // Apply damage
                    DamageInfo damageInfo = new DamageInfo(DamageDefOf.Bullet, abilityDamage, 0.35f, -1f, abilityCaster ?? launcher);
                    targetPawn.TakeDamage(damageInfo);

                    // If target had Conductivity, apply Paralysis (only if still alive)
                    if (targetHadConductivity && targetPawn.health != null && !targetPawn.Dead)
                    {
                        HediffDef paralysisDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_Paralysis");
                        if (paralysisDef != null)
                        {
                            targetPawn.health.AddHediff(paralysisDef);
                            
                            // Visual feedback - use stored map reference
                            if (targetMap != null)
                            {
                                MoteMaker.ThrowText(targetDrawPos, targetMap, "Paralyzed!", new Color(1f, 0.85f, 0.4f), 2.5f);
                            
                                // Powerful visual effect
                                for (int i = 0; i < 8; i++)
                                {
                                    FleckMaker.ThrowLightningGlow(targetDrawPos, targetMap, 1.2f);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Standard electric sparks - use stored map reference
                        if (targetMap != null)
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                FleckMaker.ThrowLightningGlow(targetDrawPos, targetMap, 0.8f);
                            }
                        }
                    }
                    
                    // Explosion-like visual - use stored map reference
                    if (targetMap != null)
                    {
                        FleckMaker.Static(targetDrawPos, targetMap, FleckDefOf.ExplosionFlash, 1.5f);
                    }
                }

                base.Impact(hitThing, blockedByShield);
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Projectile_DeclarationOfVictory.Impact: {ex}");
                base.Impact(hitThing, blockedByShield);
            }
        }
    }
}
