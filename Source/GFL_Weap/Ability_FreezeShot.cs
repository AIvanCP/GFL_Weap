using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Verb for Freeze Shot ability - launches a freezing projectile
    /// </summary>
    public class Verb_FreezeShot : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            try
            {
                if (currentTarget.HasThing && currentTarget.Thing is Pawn targetPawn)
                {
                    // Spawn freeze bolt projectile
                    ThingDef projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail("GFL_Projectile_FreezeBolt");
                    if (projectileDef != null)
                    {
                        Thing spawnedThing = GenSpawn.Spawn(projectileDef, CasterPawn.Position, CasterPawn.Map);
                        Projectile_FreezeBolt projectile = spawnedThing as Projectile_FreezeBolt;
                        
                        if (projectile != null)
                        {
                            projectile.Launch(
                                CasterPawn,
                                CasterPawn.DrawPos,
                                currentTarget,
                                currentTarget,
                                ProjectileHitFlags.IntendedTarget
                            );

                            // Store caster for ally counting
                            projectile.abilityCaster = CasterPawn;
                        }
                        
                        // No spam message - visual projectile only
                    }
                    else
                    {
                        Log.Error("[GFL Weapons] GFL_Projectile_FreezeBolt not found!");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Verb_FreezeShot.TryCastShot: {ex}");
            }

            return false;
        }
    }

    /// <summary>
    /// Freeze bolt projectile with stun scaling based on nearby allies
    /// </summary>
    public class Projectile_FreezeBolt : Projectile
    {
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

                    // Deal base freeze damage (10-20)
                    int freezeDamage = Rand.Range(10, 20);
                    DamageDef frostDmg = DefDatabase<DamageDef>.GetNamedSilentFail("GFL_FrostDamage") ?? DamageDefOf.Frostbite;
                    DamageInfo damageInfo = new DamageInfo(frostDmg, freezeDamage, 0f, -1f, launcher);
                    targetPawn.TakeDamage(damageInfo);

                    // Count nearby allies (within 6 tiles of target) - only if pawn still has map
                    int allyCount = 0;
                    if (abilityCaster != null && abilityCaster.Faction != null && targetMap != null)
                    {
                        List<Pawn> nearbyPawns = targetMap.mapPawns.AllPawnsSpawned
                            .Where(p => p != null &&
                                        p.Faction == abilityCaster.Faction && 
                                        p != targetPawn && 
                                        p.Position.DistanceTo(targetPosition) <= 6f)
                            .ToList();
                        allyCount = nearbyPawns.Count;
                    }

                    // Apply stun: 0.5 seconds per ally (only if pawn is still alive)
                    if (allyCount > 0 && !targetPawn.Dead)
                    {
                        int stunTicks = Mathf.RoundToInt(allyCount * 0.5f * 60); // 0.5 sec = 30 ticks
                        targetPawn.stances?.stunner?.StunFor(stunTicks, launcher);
                        
                        // Visual feedback only - no spam message
                    }

                    // Spawn frost flecks - use stored map reference
                    if (targetMap != null)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            FleckMaker.ThrowAirPuffUp(targetDrawPos, targetMap);
                        }
                        FleckMaker.Static(targetDrawPos, targetMap, FleckDefOf.PsycastAreaEffect, 1f);
                    }
                }

                base.Impact(hitThing, blockedByShield);
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Projectile_FreezeBolt.Impact: {ex}");
                base.Impact(hitThing, blockedByShield);
            }
        }
    }
}
