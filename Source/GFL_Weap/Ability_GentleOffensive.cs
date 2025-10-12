using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Gentle Offensive ability - Summons auto-turrets
    /// Max 2 turrets per caster, oldest destroyed if exceeds limit
    /// </summary>
    public class Verb_GentleOffensive : Verb_CastAbility
    {
        private static Dictionary<Pawn, List<Building_AutoTurret>> activeTurrets = new Dictionary<Pawn, List<Building_AutoTurret>>();
        private const int maxTurretsPerCaster = 2;

        protected override bool TryCastShot()
        {
            try
            {
                if (CasterPawn == null || !currentTarget.IsValid)
                {
                    Log.Warning("[GFL Weapons] Gentle Offensive: Invalid caster or target");
                    return false;
                }

                IntVec3 targetCell = currentTarget.Cell;
                Map map = CasterPawn.Map;

                // Validate target cell
                if (!targetCell.IsValid || !targetCell.InBounds(map))
                {
                    Messages.Message("Cannot summon turret: Target out of bounds", MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                if (targetCell.Filled(map) || targetCell.GetFirstBuilding(map) != null || targetCell.GetFirstPawn(map) != null)
                {
                    Messages.Message("Cannot summon turret: Location blocked", MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                // Check turret limit
                if (!activeTurrets.ContainsKey(CasterPawn))
                {
                    activeTurrets[CasterPawn] = new List<Building_AutoTurret>();
                }

                // Remove destroyed/null turrets from tracking
                activeTurrets[CasterPawn].RemoveAll(t => t == null || t.Destroyed);

                // If at limit, destroy oldest turret
                if (activeTurrets[CasterPawn].Count >= maxTurretsPerCaster)
                {
                    Building_AutoTurret oldestTurret = activeTurrets[CasterPawn][0];
                    if (oldestTurret != null && !oldestTurret.Destroyed)
                    {
                        MoteMaker.ThrowText(oldestTurret.DrawPos, oldestTurret.Map, "Deactivated", Color.gray, 1.5f);
                        oldestTurret.Destroy(DestroyMode.Vanish);
                    }
                    activeTurrets[CasterPawn].RemoveAt(0);
                    Log.Message($"[GFL Weapons] Destroyed oldest turret for {CasterPawn.LabelShort} (limit: {maxTurretsPerCaster})");
                }

                // Spawn turret
                ThingDef turretDef = DefDatabase<ThingDef>.GetNamedSilentFail("GFL_AutoTurret_Aglaea");
                if (turretDef == null)
                {
                    Log.Error("[GFL Weapons] GFL_AutoTurret_Aglaea ThingDef not found!");
                    return false;
                }

                Thing spawnedThing = GenSpawn.Spawn(turretDef, targetCell, map, WipeMode.Vanish);
                Building_AutoTurret turret = spawnedThing as Building_AutoTurret;
                
                if (turret != null)
                {
                    // Set summoner and inherit stats
                    turret.summonerPawn = CasterPawn;
                    turret.SetFactionDirect(CasterPawn.Faction);
                    
                    // Track turret
                    activeTurrets[CasterPawn].Add(turret);
                    
                    // Visual effects
                    FleckMaker.ThrowLightningGlow(targetCell.ToVector3(), map, 1.5f);
                    for (int i = 0; i < 5; i++)
                    {
                        FleckMaker.ThrowSmoke(targetCell.ToVector3(), map, 0.8f);
                    }
                    
                    MoteMaker.ThrowText(CasterPawn.DrawPos, map, $"Turret {activeTurrets[CasterPawn].Count}/{maxTurretsPerCaster}", Color.cyan, 2f);
                    
                    Log.Message($"[GFL Weapons] {CasterPawn.LabelShort} summoned turret at {targetCell} (Total: {activeTurrets[CasterPawn].Count})");
                    
                    return true;
                }
                else
                {
                    Log.Error("[GFL Weapons] Failed to spawn turret!");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Verb_GentleOffensive.TryCastShot: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Get active turret count for a pawn
        /// </summary>
        public static int GetActiveTurretCount(Pawn pawn)
        {
            try
            {
                if (!activeTurrets.ContainsKey(pawn))
                {
                    return 0;
                }
                
                activeTurrets[pawn].RemoveAll(t => t == null || t.Destroyed);
                return activeTurrets[pawn].Count;
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in GetActiveTurretCount: {ex}");
                return 0;
            }
        }

        /// <summary>
        /// Clean up turret tracking when pawn dies or leaves map
        /// </summary>
        public static void CleanupTurrets(Pawn pawn)
        {
            try
            {
                if (activeTurrets.ContainsKey(pawn))
                {
                    activeTurrets.Remove(pawn);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in CleanupTurrets: {ex}");
            }
        }
    }
}
