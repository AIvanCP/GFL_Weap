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
                        Projectile_FreezeBolt projectile = (Projectile_FreezeBolt)GenSpawn.Spawn(projectileDef, CasterPawn.Position, CasterPawn.Map);
                        projectile.Launch(
                            CasterPawn,
                            CasterPawn.DrawPos,
                            currentTarget,
                            currentTarget,
                            ProjectileHitFlags.IntendedTarget
                        );

                        // Store caster for ally counting
                        projectile.abilityCaster = CasterPawn;
                        
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
                    // Deal base freeze damage (10-20)
                    int freezeDamage = Rand.Range(10, 20);
                    DamageDef frostDmg = DefDatabase<DamageDef>.GetNamedSilentFail("GFL_FrostDamage") ?? DamageDefOf.Frostbite;
                    DamageInfo damageInfo = new DamageInfo(frostDmg, freezeDamage, 0f, -1f, launcher);
                    targetPawn.TakeDamage(damageInfo);

                    // Count nearby allies (within 6 tiles of target)
                    int allyCount = 0;
                    if (abilityCaster != null && abilityCaster.Faction != null)
                    {
                        List<Pawn> nearbyPawns = targetPawn.Map.mapPawns.AllPawnsSpawned
                            .Where(p => p.Faction == abilityCaster.Faction && 
                                        p != targetPawn && 
                                        p.Position.DistanceTo(targetPawn.Position) <= 6f)
                            .ToList();
                        allyCount = nearbyPawns.Count;
                    }

                    // Apply stun: 0.5 seconds per ally
                    if (allyCount > 0)
                    {
                        int stunTicks = Mathf.RoundToInt(allyCount * 0.5f * 60); // 0.5 sec = 30 ticks
                        targetPawn.stances?.stunner?.StunFor(stunTicks, launcher);
                        
                        // Visual feedback only - no spam message
                    }

                    // Spawn frost flecks
                    for (int i = 0; i < 3; i++)
                    {
                        FleckMaker.ThrowAirPuffUp(targetPawn.DrawPos, map);
                    }
                    FleckMaker.Static(targetPawn.DrawPos, map, FleckDefOf.PsycastAreaEffect, 1f);
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
