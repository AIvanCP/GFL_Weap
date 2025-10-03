using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Utility class for Combat Extended compatibility and other helper methods
    /// </summary>
    public static class WeaponUtility
    {
        private static bool? isCombatExtendedLoaded;

        /// <summary>
        /// Check if Combat Extended is loaded
        /// </summary>
        public static bool IsCombatExtendedActive()
        {
            if (!isCombatExtendedLoaded.HasValue)
            {
                isCombatExtendedLoaded = ModsConfig.IsActive("CETeam.CombatExtended");
            }
            return isCombatExtendedLoaded.Value;
        }

        /// <summary>
        /// Get appropriate projectile def based on CE status
        /// </summary>
        public static ThingDef GetFreezeBoltProjectile()
        {
            try
            {
                // Try to get our custom projectile
                ThingDef customProjectile = DefDatabase<ThingDef>.GetNamedSilentFail("GFL_Projectile_FreezeBolt");
                
                if (customProjectile != null)
                {
                    return customProjectile;
                }
                else
                {
                    Log.Warning("[GFL Weapons] Custom freeze bolt projectile not found, using fallback");
                    // Try common bullet fallbacks via DefDatabase
                    ThingDef revolver = DefDatabase<ThingDef>.GetNamedSilentFail("Bullet_Revolver");
                    ThingDef pistol = DefDatabase<ThingDef>.GetNamedSilentFail("Bullet_Pistol");
                    return revolver ?? pistol;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error getting projectile: {ex}");
                // Fallback to any available bullet projectile
                ThingDef fallback = DefDatabase<ThingDef>.GetNamedSilentFail("Bullet_Revolver") 
                    ?? DefDatabase<ThingDef>.GetNamedSilentFail("Bullet_Pistol");
                return fallback;
            }
        }

        /// <summary>
        /// Check if pawn has a GFL weapon equipped
        /// </summary>
        public static bool HasGFLWeaponEquipped(Pawn pawn)
        {
            try
            {
                if (pawn?.equipment?.Primary == null)
                {
                    return false;
                }

                return pawn.equipment.Primary.def.defName.StartsWith("GFL_Weapon_");
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error checking weapon: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Get all pawns within radius of a position
        /// </summary>
        public static List<Pawn> GetPawnsInRadius(IntVec3 center, Map map, float radius, Func<Pawn, bool> validator = null)
        {
            var result = new List<Pawn>();
            
            try
            {
                if (map == null)
                {
                    return result;
                }

                foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
                {
                    if (!cell.InBounds(map))
                    {
                        continue;
                    }

                    var things = cell.GetThingList(map);
                    foreach (Thing thing in things)
                    {
                        if (thing is Pawn pawn && !pawn.Dead)
                        {
                            if (validator == null || validator(pawn))
                            {
                                if (!result.Contains(pawn))
                                {
                                    result.Add(pawn);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error getting pawns in radius: {ex}");
            }

            return result;
        }

        /// <summary>
        /// Safely spawn visual effects
        /// </summary>
        public static void SpawnFrostEffect(IntVec3 position, Map map, float scale = 1f)
        {
            try
            {
                if (map == null)
                {
                    return;
                }

                FleckDef psyFleck = DefDatabase<FleckDef>.GetNamedSilentFail("PsycastAreaEffect");
                if (psyFleck != null)
                {
                    FleckMaker.Static(position.ToVector3Shifted(), map, psyFleck, scale);
                }
                else
                {
                    FleckMaker.ThrowLightningGlow(position.ToVector3Shifted(), map, scale);
                }
                
                for (int i = 0; i < 3; i++)
                {
                    FleckMaker.ThrowAirPuffUp(position.ToVector3Shifted(), map);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error spawning frost effect: {ex}");
            }
        }

        /// <summary>
        /// Find and cleanse negative hediffs from a pawn
        /// </summary>
        public static bool TryCleanseBadHediff(Pawn pawn)
        {
            try
            {
                if (pawn?.health?.hediffSet == null)
                {
                    return false;
                }

                var badHediffs = pawn.health.hediffSet.hediffs
                    .Where(h => h.def.isBad && h.def.everCurableByItem && !h.def.chronic)
                    .ToList();

                if (badHediffs.Any())
                {
                    Hediff toRemove = badHediffs.RandomElement();
                    pawn.health.RemoveHediff(toRemove);
                    
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Cleansed!", Color.cyan, 2f);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error cleansing hediff: {ex}");
            }

            return false;
        }

        /// <summary>
        /// Apply visual shield effect to pawn
        /// </summary>
        public static void ApplyShieldVisual(Pawn pawn)
        {
            try
            {
                if (pawn?.Map == null)
                {
                    return;
                }

                // Spawn shield activation effect
                FleckDef psyFleck = DefDatabase<FleckDef>.GetNamedSilentFail("PsycastAreaEffect");
                if (psyFleck != null)
                {
                    FleckMaker.Static(pawn.DrawPos, pawn.Map, psyFleck, 2f);
                }
                else
                {
                    FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1f);
                }

                // Try to attach overlay mote if possible
                ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail("Mote_PsycastAreaEffect");
                if (moteDef != null)
                {
                    MoteMaker.MakeAttachedOverlay(pawn, moteDef, Vector3.zero, 1f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error applying shield visual: {ex}");
            }
        }

        /// <summary>
        /// Log mod message to player
        /// </summary>
        public static void ShowMessage(string message, MessageTypeDef messageType = null)
        {
            try
            {
                if (messageType == null)
                {
                    messageType = MessageTypeDefOf.NeutralEvent;
                }

                Messages.Message(message, messageType, false);
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error showing message: {ex}");
            }
        }
    }
}
