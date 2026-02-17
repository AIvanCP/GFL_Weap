using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Negative Charge Hediff - Applied to turrets and can be applied to enemies
    /// Causes splash damage to all Negative Charge units when taking Electric damage
    /// Takes 3% applier's attack as bonus damage when reapplied
    /// Takes 20-30% more Electric damage from Positive Charge attackers
    /// </summary>
    public class Hediff_NegativeCharge : HediffWithComps
    {
        public override string LabelInBrackets
        {
            get
            {
                try
                {
                    int ticksLeft = this.TryGetComp<HediffComp_Disappears>()?.ticksToDisappear ?? 0;
                    return $"{(ticksLeft / 60f):F1}s";
                }
                catch
                {
                    return "";
                }
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            
            try
            {
                // Visual effect when applied
                if (pawn?.Map != null && pawn.Spawned)
                {
                    FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 0.6f);
                }

                // If reapplying Negative Charge, deal 3% of applier's attack as instant damage
                if (dinfo.HasValue && dinfo.Value.Instigator is Pawn attacker)
                {
                    float attackPower = attacker.GetStatValue(StatDefOf.ShootingAccuracyPawn) * 100f; // Proxy for attack
                    int bonusDamage = Mathf.RoundToInt(attackPower * 0.03f);
                    
                    if (bonusDamage > 0 && pawn?.Map != null)
                    {
                        // Store position before damage
                        Vector3 pawnDrawPos = pawn.DrawPos;
                        Map pawnMap = pawn.Map;
                        
                        DamageInfo bonusDInfo = new DamageInfo(DamageDefOf.Burn, bonusDamage, 0f, -1f, attacker);
                        pawn.TakeDamage(bonusDInfo);
                        
                        // Use stored references for visual feedback
                        if (pawnMap != null)
                        {
                            MoteMaker.ThrowText(pawnDrawPos, pawnMap, $"-{bonusDamage} Reapply!", new Color(0.8f, 0.8f, 1f), 1.5f);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_NegativeCharge.PostAdd: {ex}");
            }
        }

        /// <summary>
        /// When taking Electric damage, splash to all nearby Negative Charge targets
        /// </summary>
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            
            try
            {
                // Skip if not electric damage or no damage dealt
                if (pawn == null || pawn.Dead || totalDamageDealt <= 0)
                {
                    return;
                }

                // Check if damage is "Electric" type (use Burn as proxy since Electric doesn't exist in vanilla)
                if (dinfo.Def == DamageDefOf.Burn || dinfo.Def == DamageDefOf.Flame)
                {
                    // Calculate splash damage (30% of received damage)
                    int splashDamage = Mathf.RoundToInt(totalDamageDealt * 0.3f);
                    
                    if (splashDamage > 0)
                    {
                        // Find all pawns with Negative Charge in radius 8
                        HediffDef negativeChargeDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_NegativeCharge");
                        if (negativeChargeDef != null && pawn.Map != null)
                        {
                            List<Pawn> targetsWithNegativeCharge = pawn.Map.mapPawns.AllPawnsSpawned
                                .Where(p => p != pawn &&
                                            !p.Dead &&
                                            p.health?.hediffSet?.HasHediff(negativeChargeDef) == true &&
                                            p.Position.DistanceTo(pawn.Position) <= 8f)
                                .ToList();

                            foreach (Pawn target in targetsWithNegativeCharge)
                            {
                                // Store target references before damage
                                Vector3 targetDrawPos = target.DrawPos;
                                Map targetMap = target.Map;
                                
                                DamageInfo splashDInfo = new DamageInfo(DamageDefOf.Burn, splashDamage, 0f, -1f, dinfo.Instigator);
                                target.TakeDamage(splashDInfo);
                                
                                // Use stored references for visual feedback
                                if (targetMap != null && target.Spawned)
                                {
                                    MoteMaker.ThrowText(targetDrawPos, targetMap, $"-{splashDamage} Splash!", new Color(0.5f, 0.7f, 1f), 1.2f);
                                    FleckMaker.ThrowLightningGlow(targetDrawPos, targetMap, 0.5f);
                                }
                            }

                            if (targetsWithNegativeCharge.Count > 0)
                            {
                                Log.Message($"[GFL Weapons] Negative Charge splash: {splashDamage} damage to {targetsWithNegativeCharge.Count} targets");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_NegativeCharge.Notify_PawnPostApplyDamage: {ex}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }
    }

    /// <summary>
    /// Positive Charge Hediff - Heals over time, bonus healing near other Positive Charge allies
    /// Increases damage vs Negative Charge targets
    /// </summary>
    public class Hediff_PositiveCharge : HediffWithComps
    {
        private int tickCounter = 0;
        private const int healInterval = 60; // Heal every 1 second

        public override string LabelInBrackets
        {
            get
            {
                try
                {
                    int ticksLeft = this.TryGetComp<HediffComp_Disappears>()?.ticksToDisappear ?? 0;
                    return $"{(ticksLeft / 60f):F1}s";
                }
                catch
                {
                    return "";
                }
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            
            try
            {
                // Visual effect when applied
                if (pawn?.Map != null && pawn.Spawned)
                {
                    FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 0.8f);
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Charged!", new Color(1f, 0.9f, 0.5f), 1.8f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_PositiveCharge.PostAdd: {ex}");
            }
        }

        public override void Tick()
        {
            base.Tick();
            
            try
            {
                tickCounter++;
                
                // Heal every second (60 ticks)
                if (tickCounter >= healInterval)
                {
                    tickCounter = 0;
                    
                    if (pawn != null && !pawn.Dead && pawn.health != null)
                    {
                        float maxHP = pawn.GetStatValue(StatDefOf.MaxHitPoints);
                        float baseHeal = maxHP * 0.20f; // 20% max HP
                        
                        // Check for nearby allies with Positive Charge (within 3 tiles)
                        HediffDef positiveChargeDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_PositiveCharge");
                        bool hasNearbyPositiveChargeAlly = false;
                        
                        if (positiveChargeDef != null && pawn.Map != null)
                        {
                            hasNearbyPositiveChargeAlly = pawn.Map.mapPawns.AllPawnsSpawned
                                .Any(p => p != pawn &&
                                          p.Faction == pawn.Faction &&
                                          !p.Dead &&
                                          p.health?.hediffSet?.HasHediff(positiveChargeDef) == true &&
                                          p.Position.DistanceTo(pawn.Position) <= 3f);
                        }
                        
                        // Bonus 15% heal if near another Positive Charge ally
                        float totalHeal = baseHeal;
                        if (hasNearbyPositiveChargeAlly)
                        {
                            totalHeal += maxHP * 0.15f;
                        }
                        
                        // Find an injury to heal
                        var injury = pawn.health.hediffSet.hediffs
                            .OfType<Hediff_Injury>()
                            .Where(h => h.CanHealNaturally())
                            .FirstOrDefault();

                        if (injury != null)
                        {
                            injury.Heal(totalHeal);
                            
                            if (pawn.Spawned)
                            {
                                FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 0.4f);
                                string healText = hasNearbyPositiveChargeAlly ? $"+{Mathf.RoundToInt(totalHeal)} Synergy!" : $"+{Mathf.RoundToInt(totalHeal)}";
                                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, healText, new Color(0.5f, 1f, 0.5f), 1.2f);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_PositiveCharge.Tick: {ex}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
        }
    }

    /// <summary>
    /// Shelter Hediff - Stackable damage reduction
    /// Decrements severity when hit (simplified implementation)
    /// </summary>
    public class Hediff_Shelter : HediffWithComps
    {
        public override string LabelInBrackets
        {
            get
            {
                return $"x{Mathf.RoundToInt(Severity)}";
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            
            try
            {
                if (pawn?.Map != null && pawn.Spawned)
                {
                    FleckMaker.ThrowSmoke(pawn.DrawPos, pawn.Map, 0.8f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_Shelter.PostAdd: {ex}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }
    }

    /// <summary>
    /// Fortified Stance Hediff - Damage reduction for caster
    /// -20% incoming damage, additional -15% from electrically debuffed attackers
    /// Cannot be cleansed
    /// </summary>
    public class Hediff_FortifiedStance : HediffWithComps
    {
        public override string LabelInBrackets
        {
            get
            {
                try
                {
                    int ticksLeft = this.TryGetComp<HediffComp_Disappears>()?.ticksToDisappear ?? 0;
                    return $"{(ticksLeft / 60f):F1}s";
                }
                catch
                {
                    return "";
                }
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            
            try
            {
                if (pawn?.Map != null && pawn.Spawned)
                {
                    FleckMaker.Static(pawn.DrawPos, pawn.Map, FleckDefOf.ExplosionFlash, 1.2f);
                    for (int i = 0; i < 6; i++)
                    {
                        FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 0.6f);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_FortifiedStance.PostAdd: {ex}");
            }
        }

        /// <summary>
        /// Reduce incoming damage
        /// Base -20%, additional -15% if attacker has electric debuff
        /// </summary>
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            
            try
            {
                // Visual spark when taking damage
                if (pawn?.Map != null && pawn.Spawned && totalDamageDealt > 0)
                {
                    FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 0.3f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_FortifiedStance.Notify_PawnPostApplyDamage: {ex}");
            }
        }

        public override void Tick()
        {
            base.Tick();
            
            try
            {
                // Occasional visual effect
                if (Find.TickManager.TicksGame % 30 == 0 && pawn?.Map != null && pawn.Spawned)
                {
                    FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 0.3f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_FortifiedStance.Tick: {ex}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }
    }
}
