using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Verb for Overpowering Corrosion ability - throws toxic bomb
    /// </summary>
    public class Verb_OverpoweringCorrosion : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            try
            {
                if (currentTarget.IsValid)
                {
                    // Spawn corrosion bomb projectile
                    ThingDef projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail("GFL_Projectile_CorrosionBomb");
                    if (projectileDef != null)
                    {
                        Thing spawnedThing = GenSpawn.Spawn(projectileDef, CasterPawn.Position, CasterPawn.Map);
                        Projectile_CorrosionBomb projectile = spawnedThing as Projectile_CorrosionBomb;
                        
                        if (projectile != null)
                        {
                            projectile.Launch(
                                CasterPawn,
                                CasterPawn.DrawPos,
                                currentTarget,
                                currentTarget,
                                ProjectileHitFlags.IntendedTarget
                            );

                            // Store caster reference
                            projectile.abilityCaster = CasterPawn;
                        }
                    }
                    else
                    {
                        Log.Error("[GFL Weapons] GFL_Projectile_CorrosionBomb not found!");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Verb_OverpoweringCorrosion.TryCastShot: {ex}");
            }

            return false;
        }
    }

    /// <summary>
    /// Corrosion bomb projectile with AoE explosion
    /// </summary>
    public class Projectile_CorrosionBomb : Projectile
    {
        public Pawn abilityCaster;

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            try
            {
                Map map = base.Map;
                IntVec3 position = base.Position;

                if (map == null || abilityCaster == null)
                {
                    base.Impact(hitThing, blockedByShield);
                    return;
                }

                // Get weapon damage
                float weaponDamage = 15f; // Default
                Thing weapon = abilityCaster.equipment?.Primary;
                if (weapon != null && weapon.def.Verbs != null && weapon.def.Verbs.Count > 0)
                {
                    var verb = weapon.def.Verbs[0];
                    if (verb.defaultProjectile?.projectile != null)
                    {
                        weaponDamage = verb.defaultProjectile.projectile.GetDamageAmount(weapon);
                    }
                }

                float baseDamage = weaponDamage * 0.9f; // 90% weapon damage

                // Set context flag for projectile explosion (enables sparks) BEFORE creating explosion
                SkyllaExplosionContext.IsProjectileTrigger = true;

                // Purple explosion visual with sprite system
                SkyllaUtility.DoCorrosionExplosion(position, map, 5f, abilityCaster);
                
                // Reset flag after explosion
                SkyllaExplosionContext.IsProjectileTrigger = false;

                // Get all pawns within 5-tile radius
                List<Thing> affectedThings = GenRadial.RadialDistinctThingsAround(position, map, 5f, true).ToList();
                List<Pawn> hitPawns = new List<Pawn>();

                foreach (Thing thing in affectedThings)
                {
                    if (thing is Pawn targetPawn && !targetPawn.Dead && targetPawn != abilityCaster)
                    {
                        // CRITICAL: Only affect enemies, never player faction or allies
                        if (targetPawn.Faction?.IsPlayer == true || !targetPawn.HostileTo(abilityCaster))
                        {
                            continue;
                        }

                        hitPawns.Add(targetPawn);

                        // Check if target already has Toxic Infiltration for bonus damage
                        float finalDamage = baseDamage;
                        HediffDef toxicInfiltrationDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_ToxicInfiltration");
                        if (toxicInfiltrationDef != null && targetPawn.health.hediffSet.HasHediff(toxicInfiltrationDef))
                        {
                            finalDamage *= 1.15f; // +15% bonus damage
                        }

                        // Apply damage
                        DamageDef corrosionDmg = DefDatabase<DamageDef>.GetNamedSilentFail("GFL_CorrosionDamage") ?? DamageDefOf.Burn;
                        DamageInfo dinfo = new DamageInfo(corrosionDmg, Mathf.RoundToInt(finalDamage), 0f, -1f, abilityCaster);
                        targetPawn.TakeDamage(dinfo);

                        // Apply Toxic Infiltration
                        ApplyToxicInfiltration(targetPawn, abilityCaster);

                        // Visual feedback
                        if (targetPawn.Map != null && targetPawn.Spawned)
                        {
                            MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, 
                                $"-{Mathf.RoundToInt(finalDamage)}", 
                                new Color(1f, 0.3f, 1f), 1.5f);
                        }
                    }
                }

                // 15% chance for secondary mini explosion at each hit pawn
                foreach (Pawn hitPawn in hitPawns)
                {
                    if (Rand.Chance(0.15f) && hitPawn.Map != null && hitPawn.Spawned)
                    {
                        TriggerSecondaryExplosion(hitPawn.Position, hitPawn.Map, abilityCaster, weaponDamage * 0.5f);
                    }
                }

                base.Impact(hitThing, blockedByShield);
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Projectile_CorrosionBomb.Impact: {ex}");
                base.Impact(hitThing, blockedByShield);
            }
        }

        private void TriggerSecondaryExplosion(IntVec3 position, Map map, Pawn caster, float damage)
        {
            try
            {
                // Smaller purple explosion (no sparks - not from projectile)
                SkyllaExplosionContext.IsProjectileTrigger = false;
                SkyllaUtility.DoCorrosionExplosion(position, map, 1.2f, caster);

                // Damage in 1.2 tile radius
                List<Thing> affectedThings = GenRadial.RadialDistinctThingsAround(position, map, 1.2f, true).ToList();

                foreach (Thing thing in affectedThings)
                {
                    if (thing is Pawn targetPawn && !targetPawn.Dead && targetPawn != caster && targetPawn.HostileTo(caster))
                    {
                        DamageDef corrosionDmg = DefDatabase<DamageDef>.GetNamedSilentFail("GFL_CorrosionDamage") ?? DamageDefOf.Burn;
                        DamageInfo dinfo = new DamageInfo(corrosionDmg, Mathf.RoundToInt(damage), 0f, -1f, caster);
                        targetPawn.TakeDamage(dinfo);

                        ApplyToxicInfiltration(targetPawn, caster);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in TriggerSecondaryExplosion: {ex}");
            }
        }

        private void ApplyToxicInfiltration(Pawn target, Pawn attacker)
        {
            try
            {
                // Null safety checks
                if (target == null || target.Dead || target.health == null)
                    return;

                HediffDef toxicInfiltrationDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_ToxicInfiltration");
                if (toxicInfiltrationDef == null) return;

                Hediff_ToxicInfiltration existing = target.health.hediffSet.GetFirstHediffOfDef(toxicInfiltrationDef) as Hediff_ToxicInfiltration;
                
                if (existing != null)
                {
                    // Refresh duration
                    existing.ageTicks = 0;
                }
                else
                {
                    // Apply new hediff
                    Hediff_ToxicInfiltration newHediff = HediffMaker.MakeHediff(toxicInfiltrationDef, target) as Hediff_ToxicInfiltration;
                    if (newHediff != null)
                    {
                        newHediff.originalAttacker = attacker;
                        target.health.AddHediff(newHediff);
                    }
                }

                // Also add Corrosive Infusion stack when hit by corrosion damage
                HediffDef corrosiveInfusionDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_CorrosiveInfusion");
                if (corrosiveInfusionDef != null)
                {
                    Hediff_CorrosiveInfusion existingInfusion = target.health.hediffSet.GetFirstHediffOfDef(corrosiveInfusionDef) as Hediff_CorrosiveInfusion;
                    
                    if (existingInfusion != null)
                    {
                        existingInfusion.Severity = Mathf.Min(existingInfusion.Severity + 1f, 10f);
                        existingInfusion.originalAttacker = attacker;
                    }
                    else
                    {
                        Hediff_CorrosiveInfusion newInfusion = HediffMaker.MakeHediff(corrosiveInfusionDef, target) as Hediff_CorrosiveInfusion;
                        if (newInfusion != null)
                        {
                            newInfusion.Severity = 1f;
                            newInfusion.originalAttacker = attacker;
                            target.health.AddHediff(newInfusion);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error applying Toxic Infiltration: {ex}");
            }
        }
    }
}
