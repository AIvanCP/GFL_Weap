using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Custom Building class for Aglaea Auto-Turret with rotating head
    /// Handles stat inheritance, auto-targeting, and visual rendering
    /// </summary>
    public class Building_AutoTurret : Building_TurretGun
    {
        public Pawn summonerPawn; // The pawn who summoned this turret
        public float inheritedMaxHP = 100f;
        public float inheritedAttackPower = 12f;
        public float inheritedDefense = 0f;

        private int tickCounter = 0;
        private const int ticksPerCheck = 30; // Check every 0.5 seconds

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            
            try
            {
                if (!respawningAfterLoad)
                {
                    // Apply inherited stats
                    ApplyInheritedStats();
                    
                    // Note: Turrets are buildings, not pawns, so we can't apply hediffs directly
                    // Negative Charge applied visually only
                    
                    // Visual spawn effect
                    FleckMaker.ThrowLightningGlow(DrawPos, map, 1.2f);
                    MoteMaker.ThrowText(DrawPos, map, "Turret Deployed!", Color.cyan, 2f);
                    
                    Log.Message($"[GFL Weapons] Auto-Turret spawned by {summonerPawn?.LabelShort ?? "unknown"} at {Position}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Building_AutoTurret.SpawnSetup: {ex}");
            }
        }

        /// <summary>
        /// Apply inherited stats from summoner pawn
        /// 50% HP, 50% attack, 100% defense
        /// </summary>
        private void ApplyInheritedStats()
        {
            try
            {
                if (summonerPawn != null && !summonerPawn.Dead)
                {
                    // Inherit 50% of max HP
                    float summonerMaxHP = summonerPawn.GetStatValue(StatDefOf.MaxHitPoints);
                    inheritedMaxHP = summonerMaxHP * 0.5f;
                    
                    // Set turret current HP to inherited value
                    HitPoints = Mathf.RoundToInt(inheritedMaxHP);
                    
                    // Inherit 50% of attack power (use shooting accuracy as proxy)
                    inheritedAttackPower = summonerPawn.GetStatValue(StatDefOf.ShootingAccuracyPawn) * 0.5f;
                    
                    // Inherit 100% of defense (armor)
                    inheritedDefense = summonerPawn.GetStatValue(StatDefOf.ArmorRating_Sharp);
                    
                    Log.Message($"[GFL Weapons] Turret inherited stats: HP={inheritedMaxHP}, Attack={inheritedAttackPower}, Defense={inheritedDefense}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error applying inherited stats: {ex}");
            }
        }

        protected override void Tick()
        {
            base.Tick();
            
            try
            {
                tickCounter++;
                
                // Periodic visual spark for Negative Charge
                if (tickCounter % 60 == 0)
                {
                    FleckMaker.ThrowLightningGlow(DrawPos, Map, 0.3f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Building_AutoTurret.Tick: {ex}");
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            try
            {
                // Grant Confectance Index to summoner when turret dies
                if (summonerPawn != null && !summonerPawn.Dead && summonerPawn.health != null)
                {
                    var confectanceDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_ConfectanceIndex");
                    if (confectanceDef != null)
                    {
                        Hediff existingHediff = summonerPawn.health.hediffSet.GetFirstHediffOfDef(confectanceDef);
                        if (existingHediff != null)
                        {
                            // Increase severity (stack)
                            existingHediff.Severity += 1f;
                        }
                        else
                        {
                            // Add new hediff
                            summonerPawn.health.AddHediff(confectanceDef);
                        }
                        
                        MoteMaker.ThrowText(summonerPawn.DrawPos, summonerPawn.Map, "+1 Confectance", new Color(0.5f, 1f, 0.5f), 2f);
                        Log.Message($"[GFL Weapons] {summonerPawn.LabelShort} gained +1 Confectance Index from turret death");
                    }
                }
                
                // Explosion effect
                FleckMaker.Static(DrawPos, Map, FleckDefOf.ExplosionFlash, 1.2f);
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Building_AutoTurret.Destroy: {ex}");
            }
            
            base.Destroy(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref summonerPawn, "summonerPawn");
            Scribe_Values.Look(ref inheritedMaxHP, "inheritedMaxHP", 100f);
            Scribe_Values.Look(ref inheritedAttackPower, "inheritedAttackPower", 12f);
            Scribe_Values.Look(ref inheritedDefense, "inheritedDefense", 0f);
        }
    }

    /// <summary>
    /// Comp properties for auto-turret behavior
    /// </summary>
    public class CompProperties_AutoTurret : CompProperties
    {
        public CompProperties_AutoTurret()
        {
            compClass = typeof(Comp_AutoTurret);
        }
    }

    /// <summary>
    /// Comp for handling turret-specific behavior
    /// </summary>
    public class Comp_AutoTurret : ThingComp
    {
        public CompProperties_AutoTurret Props => (CompProperties_AutoTurret)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            
            try
            {
                // Additional setup if needed
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Comp_AutoTurret.PostSpawnSetup: {ex}");
            }
        }
    }
}
