using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Fortification Protocol - Ultimate ability
    /// Heals allies, applies buffs in radius 9
    /// </summary>
    public class Verb_FortificationProtocol : Verb_CastAbility
    {
        private const float aoeRadius = 9f;

        protected override bool TryCastShot()
        {
            try
            {
                if (CasterPawn == null || CasterPawn.Map == null)
                {
                    Log.Warning("[GFL Weapons] Fortification Protocol: Invalid caster");
                    return false;
                }

                Map map = CasterPawn.Map;
                IntVec3 casterPos = CasterPawn.Position;

                // Restore caster stability (rest)
                if (CasterPawn.needs?.rest != null)
                {
                    CasterPawn.needs.rest.CurLevel = Mathf.Min(1f, CasterPawn.needs.rest.CurLevel + 0.3f);
                }

                // Apply Fortified Stance to caster
                HediffDef fortifiedDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_FortifiedStance");
                if (fortifiedDef != null && CasterPawn.health != null)
                {
                    CasterPawn.health.AddHediff(fortifiedDef);
                    MoteMaker.ThrowText(CasterPawn.DrawPos, map, "Fortified!", new Color(1f, 0.8f, 0.4f), 2.5f);
                }

                // Find all allies within radius
                List<Pawn> alliesInRange = map.mapPawns.AllPawnsSpawned
                    .Where(p => p.Faction == CasterPawn.Faction &&
                                !p.Dead &&
                                p.Position.DistanceTo(casterPos) <= aoeRadius)
                    .ToList();

                Log.Message($"[GFL Weapons] Fortification Protocol: Found {alliesInRange.Count} allies in range");

                HediffDef shelterDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_Shelter");
                HediffDef positiveChargeDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_PositiveCharge");

                foreach (Pawn ally in alliesInRange)
                {
                    try
                    {
                        if (ally.health == null) continue;

                        // Apply 2 stacks of Shelter
                        if (shelterDef != null)
                        {
                            Hediff existingShelter = ally.health.hediffSet.GetFirstHediffOfDef(shelterDef);
                            if (existingShelter != null)
                            {
                                existingShelter.Severity = Mathf.Min(2f, existingShelter.Severity + 2f);
                            }
                            else
                            {
                                Hediff newShelter = ally.health.AddHediff(shelterDef);
                                if (newShelter != null)
                                {
                                    newShelter.Severity = 2f;
                                }
                            }
                        }

                        // Apply Positive Charge
                        if (positiveChargeDef != null)
                        {
                            // Remove existing first to refresh duration
                            Hediff existingCharge = ally.health.hediffSet.GetFirstHediffOfDef(positiveChargeDef);
                            if (existingCharge != null)
                            {
                                ally.health.RemoveHediff(existingCharge);
                            }
                            ally.health.AddHediff(positiveChargeDef);
                        }

                        // Visual feedback
                        FleckMaker.ThrowLightningGlow(ally.DrawPos, map, 1.0f);
                        if (ally != CasterPawn)
                        {
                            MoteMaker.ThrowText(ally.DrawPos, map, "Buffed!", Color.yellow, 2f);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[GFL Weapons] Error applying buffs to {ally.LabelShort}: {ex}");
                    }
                }

                // Massive visual effect
                FleckMaker.Static(CasterPawn.DrawPos, map, FleckDefOf.PsycastAreaEffect, 3f);
                for (int i = 0; i < 12; i++)
                {
                    Vector3 randomOffset = new Vector3(
                        Rand.Range(-aoeRadius, aoeRadius),
                        0f,
                        Rand.Range(-aoeRadius, aoeRadius)
                    );
                    FleckMaker.ThrowLightningGlow(CasterPawn.DrawPos + randomOffset, map, 0.8f);
                }

                Log.Message($"[GFL Weapons] {CasterPawn.LabelShort} used Fortification Protocol, buffed {alliesInRange.Count} allies");

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Verb_FortificationProtocol.TryCastShot: {ex}");
                return false;
            }
        }
    }
}
