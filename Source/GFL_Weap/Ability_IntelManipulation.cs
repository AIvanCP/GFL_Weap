using System;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Verb for Intel Manipulation ability - single-target Hydro shot
    /// </summary>
    public class Verb_IntelManipulation : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            try
            {
                if (currentTarget.HasThing && currentTarget.Thing is Pawn targetPawn)
                {
                    // Spawn Hydro projectile
                    ThingDef projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail("GFL_Projectile_RadianceHydroShot");
                    if (projectileDef != null)
                    {
                        Thing spawnedThing = GenSpawn.Spawn(projectileDef, CasterPawn.Position, CasterPawn.Map);
                        Projectile_RadianceHydroShot projectile = spawnedThing as Projectile_RadianceHydroShot;
                        
                        if (projectile != null)
                        {
                            projectile.Launch(
                                CasterPawn,
                                CasterPawn.DrawPos,
                                currentTarget,
                                currentTarget,
                                ProjectileHitFlags.IntendedTarget
                            );

                            projectile.abilityCaster = CasterPawn;
                        }
                    }
                    else
                    {
                        Log.Error("[GFL Weapons] GFL_Projectile_RadianceHydroShot not found!");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Verb_IntelManipulation.TryCastShot: {ex}");
            }

            return false;
        }
    }

    /// <summary>
    /// Hydro shot projectile for Intel Manipulation
    /// </summary>
    public class Projectile_RadianceHydroShot : Projectile
    {
        public Pawn abilityCaster;

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            try
            {
                Map map = base.Map;
                IntVec3 position = base.Position;

                if (hitThing is Pawn targetPawn && !targetPawn.Dead && abilityCaster != null)
                {
                    // Only affect enemies
                    if (!targetPawn.HostileTo(abilityCaster))
                    {
                        base.Impact(hitThing, blockedByShield);
                        return;
                    }

                    // Get weapon damage
                    float weaponDamage = 20f; // Default
                    Thing weapon = abilityCaster.equipment?.Primary;
                    if (weapon != null && weapon.def.Verbs != null && weapon.def.Verbs.Count > 0)
                    {
                        var verb = weapon.def.Verbs[0];
                        if (verb.defaultProjectile?.projectile != null)
                        {
                            weaponDamage = verb.defaultProjectile.projectile.GetDamageAmount(weapon);
                        }
                    }

                    float finalDamage = weaponDamage * 1.3f; // 130% weapon damage

                    // Apply Hydro damage
                    DamageDef hydroDmg = DefDatabase<DamageDef>.GetNamedSilentFail("GFL_HydroDamage") ?? DamageDefOf.Burn;
                    DamageInfo dinfo = new DamageInfo(hydroDmg, Mathf.RoundToInt(finalDamage), 0f, -1f, abilityCaster);
                    targetPawn.TakeDamage(dinfo);

                    // Apply False Intelligence and Congestion (only if alive)
                    if (!targetPawn.Dead && targetPawn.health != null)
                    {
                        HediffDef falseIntelDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_FalseIntelligence");
                        if (falseIntelDef != null)
                        {
                            Hediff falseIntel = HediffMaker.MakeHediff(falseIntelDef, targetPawn);
                            targetPawn.health.AddHediff(falseIntel);
                        }

                        HediffDef congestionDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_Congestion");
                        if (congestionDef != null)
                        {
                            Hediff congestion = HediffMaker.MakeHediff(congestionDef, targetPawn);
                            targetPawn.health.AddHediff(congestion);
                        }
                    }

                    // Grant +1 Confectance Index to caster
                    HediffDef confectanceDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_ConfectanceIndex");
                    if (confectanceDef != null && abilityCaster.health != null)
                    {
                        Hediff existing = abilityCaster.health.hediffSet.GetFirstHediffOfDef(confectanceDef);
                        if (existing != null)
                        {
                            existing.Severity = Mathf.Min(existing.Severity + 1f, 50f);
                        }
                        else
                        {
                            Hediff newConfectance = HediffMaker.MakeHediff(confectanceDef, abilityCaster);
                            newConfectance.Severity = 1f;
                            abilityCaster.health.AddHediff(newConfectance);
                        }
                    }

                    // Visual feedback
                    if (targetPawn.Map != null && targetPawn.Spawned)
                    {
                        // Hydro blue explosion
                        FleckMaker.Static(targetPawn.DrawPos, targetPawn.Map, FleckDefOf.ExplosionFlash, 1.5f);
                        FleckMaker.ThrowDustPuffThick(targetPawn.DrawPos, targetPawn.Map, 1f, new Color(0.5f, 0.8f, 1f));
                        
                        MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, 
                            $"-{Mathf.RoundToInt(finalDamage)} Hydro", 
                            new Color(0.5f, 0.8f, 1f), 2f);
                    }
                }

                base.Impact(hitThing, blockedByShield);
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Projectile_RadianceHydroShot.Impact: {ex}");
                base.Impact(hitThing, blockedByShield);
            }
        }
    }
}
