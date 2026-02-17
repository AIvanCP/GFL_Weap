using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Toxic Infiltration hediff - adds Corrosive Infusion stacks and explodes on death
    /// </summary>
    public class Hediff_ToxicInfiltration : HediffWithComps
    {
        private int tickCounter = 0;
        private const int ticksPerTurn = 60; // 1 second = 1 turn
        public Pawn originalAttacker; // Store who applied this effect

        public override string LabelInBrackets
        {
            get
            {
                int remainingTicks = 1200 - ageTicks;
                return $"{(remainingTicks / 60f):F1}s";
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            
            try
            {
                if (pawn?.Map != null && pawn.Spawned)
                {
                    // Purple toxic visual
                    FleckMaker.ThrowDustPuffThick(pawn.DrawPos, pawn.Map, 0.8f, new Color(0.8f, 0.3f, 0.8f));
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Toxic Infiltration", new Color(1f, 0.3f, 1f), 2f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_ToxicInfiltration.PostAdd: {ex}");
            }
        }

        public override void Tick()
        {
            base.Tick();

            try
            {
                tickCounter++;

                // Add 1 stack of Corrosive Infusion every turn (60 ticks)
                if (tickCounter >= ticksPerTurn)
                {
                    tickCounter = 0;
                    
                    if (pawn != null && !pawn.Dead)
                    {
                        AddCorrosiveInfusionStack();
                    }
                }

                // Purple glow every 30 ticks
                if (ageTicks % 30 == 0 && pawn?.Map != null && pawn.Spawned)
                {
                    FleckMaker.ThrowDustPuffThick(pawn.DrawPos, pawn.Map, 0.5f, new Color(0.8f, 0.3f, 0.8f));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_ToxicInfiltration.Tick: {ex}");
            }
        }

        private void AddCorrosiveInfusionStack()
        {
            try
            {
                if (pawn == null || pawn.Dead || pawn.health == null)
                {
                    return;
                }

                HediffDef corrosiveInfusionDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_CorrosiveInfusion");
                if (corrosiveInfusionDef == null) return;

                Hediff_CorrosiveInfusion existingInfusion = pawn.health.hediffSet.GetFirstHediffOfDef(corrosiveInfusionDef) as Hediff_CorrosiveInfusion;
                
                if (existingInfusion != null)
                {
                    // Add 1 stack (max 10)
                    existingInfusion.Severity = Mathf.Min(existingInfusion.Severity + 1f, 10f);
                    existingInfusion.originalAttacker = originalAttacker; // Pass attacker reference
                }
                else
                {
                    // Create new hediff with 1 stack
                    Hediff_CorrosiveInfusion newInfusion = HediffMaker.MakeHediff(corrosiveInfusionDef, pawn) as Hediff_CorrosiveInfusion;
                    if (newInfusion != null)
                    {
                        newInfusion.Severity = 1f;
                        newInfusion.originalAttacker = originalAttacker;
                        pawn.health.AddHediff(newInfusion);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error adding Corrosive Infusion stack: {ex}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref originalAttacker, "originalAttacker");
        }
    }

    /// <summary>
    /// Corrosive Infusion hediff - stackable DOT that damages nearby enemies
    /// </summary>
    public class Hediff_CorrosiveInfusion : HediffWithComps
    {
        private int tickCounter = 0;
        private const int ticksPerDamage = 60; // Deal damage every 1 second
        public Pawn originalAttacker; // Store who applied this effect

        public override string LabelInBrackets
        {
            get
            {
                int stacks = Mathf.RoundToInt(Severity);
                int remainingTicks = 600 - ageTicks;
                return $"{stacks} stacks, {(remainingTicks / 60f):F1}s";
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            
            try
            {
                if (pawn?.Map != null && pawn.Spawned)
                {
                    FleckMaker.ThrowDustPuffThick(pawn.DrawPos, pawn.Map, 0.6f, new Color(0.6f, 0.2f, 0.6f));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_CorrosiveInfusion.PostAdd: {ex}");
            }
        }

        public override void Tick()
        {
            base.Tick();

            try
            {
                tickCounter++;

                // Deal AoE DOT every 60 ticks
                if (tickCounter >= ticksPerDamage)
                {
                    tickCounter = 0;
                    ApplyCorrosiveDamage();
                }

                // Purple dust visual every 40 ticks
                if (ageTicks % 40 == 0 && pawn?.Map != null && pawn.Spawned)
                {
                    FleckMaker.ThrowDustPuffThick(pawn.DrawPos, pawn.Map, 0.4f, new Color(0.6f, 0.2f, 0.6f));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_CorrosiveInfusion.Tick: {ex}");
            }
        }

        private void ApplyCorrosiveDamage()
        {
            try
            {
                if (pawn == null || pawn.Dead || pawn.Map == null) return;

                int stacks = Mathf.RoundToInt(Severity);
                
                // Calculate damage = 12% * stacks * attacker's weapon damage
                float baseDamage = 10f; // Default if attacker not available
                
                if (originalAttacker != null && !originalAttacker.Dead)
                {
                    Thing weapon = originalAttacker.equipment?.Primary;
                    if (weapon != null && weapon.def.Verbs != null && weapon.def.Verbs.Count > 0)
                    {
                        var verb = weapon.def.Verbs[0];
                        if (verb.defaultProjectile?.projectile != null)
                        {
                            baseDamage = verb.defaultProjectile.projectile.GetDamageAmount(weapon);
                        }
                    }
                }

                float totalDamage = baseDamage * 0.12f * stacks;
                
                // Apply AoE damage to nearby enemies (1.5 tile radius)
                List<Thing> nearbyThings = GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 1.5f, true).ToList();
                
                foreach (Thing thing in nearbyThings)
                {
                    if (thing is Pawn targetPawn && !targetPawn.Dead && targetPawn != pawn && targetPawn.HostileTo(pawn))
                    {
                        DamageDef corrosionDmg = DefDatabase<DamageDef>.GetNamedSilentFail("GFL_CorrosionDamage") ?? DamageDefOf.Burn;
                        DamageInfo dinfo = new DamageInfo(corrosionDmg, Mathf.RoundToInt(totalDamage), 0f, -1f, originalAttacker);
                        targetPawn.TakeDamage(dinfo);

                        // Visual feedback
                        if (targetPawn.Map != null && targetPawn.Spawned)
                        {
                            MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, 
                                $"-{Mathf.RoundToInt(totalDamage)}", 
                                new Color(0.8f, 0.2f, 0.8f), 1.5f);
                        }
                    }
                }

                // Purple explosion visual at pawn
                if (pawn.Map != null && pawn.Spawned)
                {
                    FleckMaker.ThrowDustPuffThick(pawn.DrawPos, pawn.Map, 1f, new Color(0.6f, 0.2f, 0.6f));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_CorrosiveInfusion.ApplyCorrosiveDamage: {ex}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref originalAttacker, "originalAttacker");
        }
    }
}
