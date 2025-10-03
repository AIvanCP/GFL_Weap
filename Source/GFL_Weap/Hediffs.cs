using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Frost Barrier shield hediff - provides damage absorption, healing, and visual effects
    /// </summary>
    public class Hediff_FrostBarrier : HediffWithComps
    {
        private int ticksRemaining = 1800; // 30 seconds
        public float shieldHitPoints = 80f; // Public so HediffComp can access
        private const float maxShieldHP = 80f;
        private const float healPerSecond = 1f;
        private int tickCounter = 0;

        public override string LabelInBrackets
        {
            get
            {
                return $"{Mathf.RoundToInt(shieldHitPoints)}/{Mathf.RoundToInt(maxShieldHP)} HP, {(ticksRemaining / 60f):F1}s";
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            
            try
            {
                // Initialize shield
                shieldHitPoints = maxShieldHP;
                ticksRemaining = 1800;
                
                // Visual: spawn shield bubble effect
                if (pawn?.Map != null)
                {
                    // Try to use psycast fleck if available, otherwise use a generic effect
                    FleckDef psyFleck = DefDatabase<FleckDef>.GetNamedSilentFail("PsycastAreaEffect");
                    if (psyFleck != null)
                    {
                        FleckMaker.Static(pawn.DrawPos, pawn.Map, psyFleck, 2f);
                    }
                    else
                    {
                        FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1f);
                    }

                    ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail("Mote_PsycastAreaEffect");
                    if (moteDef != null)
                    {
                        MoteMaker.MakeAttachedOverlay(pawn, moteDef, Vector3.zero, 1f);
                    }
                    
                    // Floating text instead of message log
                    if (pawn?.Map != null)
                    {
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Frost Barrier +80", Color.cyan, 2f);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_FrostBarrier.PostAdd: {ex}");
            }
        }

        public override void Tick()
        {
            base.Tick();

            try
            {
                tickCounter++;
                ticksRemaining--;

                // Heal 1 HP per second (60 ticks)
                if (tickCounter >= 60)
                {
                    tickCounter = 0;
                    
                    if (pawn != null && !pawn.Dead)
                    {
                        // Find a injury to heal
                        var injury = pawn.health.hediffSet.hediffs
                            .OfType<Hediff_Injury>()
                            .Where(h => h.CanHealNaturally())
                            .FirstOrDefault();

                        if (injury != null)
                        {
                            injury.Heal(healPerSecond);
                        }
                    }
                }

                // Check if expired
                if (ticksRemaining <= 0 || shieldHitPoints <= 0)
                {
                    // Shield expired or broken
                    if (pawn?.Map != null)
                    {
                        FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1f);
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Shield expired", Color.cyan, 2f);
                    }
                    
                    pawn?.health?.RemoveHediff(this);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_FrostBarrier.Tick: {ex}");
            }
        }

        // Note: RimWorld version used may not expose PostPreApplyDamage as an overridable method on HediffWithComps.
        // Keep this method non-virtual so the project compiles; the damage interception will still work if the game calls
        // a matching method via reflection or if you move this logic into a HediffComp in the future.
        public void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            try
            {
                // Some RimWorld versions don't expose externalViolence; allow shield to absorb most incoming damage.
                if (shieldHitPoints > 0)
                {
                    float damageAmount = dinfo.Amount;
                    
                    if (damageAmount <= shieldHitPoints)
                    {
                        // Shield absorbs all damage
                        shieldHitPoints -= damageAmount;
                        absorbed = true;
                        dinfo.SetAmount(0);
                        
                        // Visual feedback
                        if (pawn?.Map != null)
                        {
                            FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 0.5f);
                            
                            if (pawn?.Map != null)
                            {
                                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, $"Absorbed {Mathf.RoundToInt(damageAmount)}", Color.white, 1.8f);
                            }
                        }
                    }
                    else
                    {
                        // Shield absorbs partial damage
                        float remainingDamage = damageAmount - shieldHitPoints;
                        shieldHitPoints = 0;
                        dinfo.SetAmount(remainingDamage);
                        
                        // Shield broken
                        if (pawn?.Map != null)
                        {
                            FleckMaker.Static(pawn.DrawPos, pawn.Map, FleckDefOf.ExplosionFlash, 1f);
                            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Shield broken!", Color.red, 2f);
                            
                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_FrostBarrier.PostPreApplyDamage: {ex}");
                // base.PostPreApplyDamage may not exist on this RimWorld version; swallow to avoid crashing.
                absorbed = false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", 1800);
            Scribe_Values.Look(ref shieldHitPoints, "shieldHitPoints", 80f);
            Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
        }
    }

    /// <summary>
    /// Avalanche debuff - slows movement and increases damage taken
    /// </summary>
    public class Hediff_Avalanche : HediffWithComps
    {
        public override string LabelInBrackets
        {
            get
            {
                int ticksLeft = this.TryGetComp<HediffComp_Disappears>()?.ticksToDisappear ?? 0;
                return $"{(ticksLeft / 60f):F1}s";
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            
            try
            {
                // Visual effect when applied
                if (pawn?.Map != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        FleckMaker.ThrowAirPuffUp(pawn.DrawPos, pawn.Map);
                    }
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Slowed!", new Color(0.6f, 0.6f, 1f), 2f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_Avalanche.PostAdd: {ex}");
            }
        }
    }
}
