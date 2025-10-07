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
        
        // Shield bubble overlay reference
        private Effecter shieldBubbleEffecter;

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
                
                // Create persistent shield bubble effecter
                if (pawn?.Map != null && pawn.Spawned)
                {
                    EffecterDef shieldEffect = DefDatabase<EffecterDef>.GetNamedSilentFail("Skip_EntryNoDelay");
                    if (shieldEffect != null)
                    {
                        shieldBubbleEffecter = shieldEffect.Spawn();
                        shieldBubbleEffecter.offset = new Vector3(0f, 0f, 0f);
                    }
                    
                    // Initial visual flash
                    FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1f);
                    
                    // Floating text instead of message log
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Frost Barrier +80", Color.cyan, 2f);
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

                // Maintain shield bubble visual
                if (shieldHitPoints > 0 && pawn?.Spawned == true && shieldBubbleEffecter != null)
                {
                    shieldBubbleEffecter.EffectTick(pawn, pawn);
                }
                
                // Show persistent blue glow every 30 ticks (half second) while shield active
                if (tickCounter % 30 == 0 && pawn?.Map != null && pawn.Spawned && shieldHitPoints > 0)
                {
                    // Blue shield glow effect
                    FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 0.8f);
                }

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
                    // Clean up effecter
                    if (shieldBubbleEffecter != null)
                    {
                        shieldBubbleEffecter.Cleanup();
                        shieldBubbleEffecter = null;
                    }
                    
                    // Shield expired or broken - remove silently
                    pawn?.health?.RemoveHediff(this);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_FrostBarrier.Tick: {ex}");
            }
        }
        
        public override void PostRemoved()
        {
            base.PostRemoved();
            
            try
            {
                // Clean up effecter when hediff removed
                if (shieldBubbleEffecter != null)
                {
                    shieldBubbleEffecter.Cleanup();
                    shieldBubbleEffecter = null;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_FrostBarrier.PostRemoved: {ex}");
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
