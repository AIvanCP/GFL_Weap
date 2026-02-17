using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// False Intelligence hediff - increases stagger chance and Hydro damage taken
    /// </summary>
    public class Hediff_FalseIntelligence : HediffWithComps
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
                    FleckMaker.ThrowDustPuffThick(pawn.DrawPos, pawn.Map, 0.7f, new Color(0.6f, 0.6f, 0.9f));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_FalseIntelligence.PostAdd: {ex}");
            }
        }

        /// <summary>
        /// When taking Hydro damage, take +10% more
        /// </summary>
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            
            try
            {
                if (pawn == null || pawn.Dead || totalDamageDealt <= 0) return;

                // Check if damage is Hydro type
                DamageDef hydroDmg = DefDatabase<DamageDef>.GetNamedSilentFail("GFL_HydroDamage");
                if (hydroDmg != null && dinfo.Def == hydroDmg)
                {
                    // Apply bonus 10% damage
                    int bonusDamage = Mathf.RoundToInt(totalDamageDealt * 0.1f);
                    if (bonusDamage > 0)
                    {
                        DamageInfo bonusInfo = new DamageInfo(hydroDmg, bonusDamage, 0f, -1f, dinfo.Instigator);
                        pawn.TakeDamage(bonusInfo);

                        if (pawn.Map != null && pawn.Spawned)
                        {
                            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, $"+{bonusDamage}", new Color(0.5f, 0.8f, 1f), 1.2f);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_FalseIntelligence.Notify_PawnPostApplyDamage: {ex}");
            }
        }
    }

    /// <summary>
    /// Damp hediff - reduces healing, stacks up to 2, transforms to Congestion at 2 stacks
    /// </summary>
    public class Hediff_Damp : HediffWithComps
    {
        private int tickCounter = 0;

        public override string LabelInBrackets
        {
            get
            {
                try
                {
                    int stacks = Mathf.RoundToInt(Severity);
                    int ticksLeft = this.TryGetComp<HediffComp_Disappears>()?.ticksToDisappear ?? 0;
                    return $"{stacks} stacks, {(ticksLeft / 60f):F1}s";
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
                    FleckMaker.ThrowDustPuffThick(pawn.DrawPos, pawn.Map, 0.6f, new Color(0.5f, 0.7f, 0.9f));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_Damp.PostAdd: {ex}");
            }
        }

        public override void Tick()
        {
            base.Tick();

            try
            {
                tickCounter++;

                // Check if reached 2 stacks every 30 ticks
                if (tickCounter >= 30)
                {
                    tickCounter = 0;

                    if (Severity >= 2f)
                    {
                        // Transform to Congestion
                        HediffDef congestionDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_Congestion");
                        if (congestionDef != null && pawn != null && !pawn.Dead && pawn.health != null)
                        {
                            pawn.health.RemoveHediff(this);
                            Hediff congestion = HediffMaker.MakeHediff(congestionDef, pawn);
                            pawn.health.AddHediff(congestion);

                            if (pawn.Map != null && pawn.Spawned)
                            {
                                FleckMaker.Static(pawn.DrawPos, pawn.Map, FleckDefOf.ExplosionFlash, 0.8f);
                                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Congested!", new Color(0.4f, 0.4f, 0.7f), 2f);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_Damp.Tick: {ex}");
            }
        }
    }

    /// <summary>
    /// Deep-Rooted Bonds hediff - doubles max HP, heals on expire
    /// </summary>
    public class Hediff_DeepRootedBonds : HediffWithComps
    {
        public Pawn caster;
        public float healAmount = 30f;

        public override string LabelInBrackets
        {
            get
            {
                try
                {
                    int ticksLeft = this.TryGetComp<HediffComp_Disappears>()?.ticksToDisappear ?? 0;
                    return $"2× HP, {(ticksLeft / 60f):F1}s";
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
                    FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1.2f);
                    FleckMaker.ThrowDustPuffThick(pawn.DrawPos, pawn.Map, 1f, new Color(0.4f, 1f, 0.8f));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_DeepRootedBonds.PostAdd: {ex}");
            }
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            
            try
            {
                // Heal on expiration
                if (pawn != null && !pawn.Dead && pawn.health != null)
                {
                    var injury = pawn.health.hediffSet.hediffs
                        .OfType<Hediff_Injury>()
                        .Where(h => h.CanHealNaturally())
                        .FirstOrDefault();

                    if (injury != null)
                    {
                        injury.Heal(healAmount);

                        if (pawn.Map != null && pawn.Spawned)
                        {
                            FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1f);
                            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, 
                                $"+{Mathf.RoundToInt(healAmount)} HP", 
                                new Color(0.4f, 1f, 0.8f), 2f);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_DeepRootedBonds.PostRemoved: {ex}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref healAmount, "healAmount", 30f);
        }
    }

    /// <summary>
    /// Overflowing Care hediff - heals after actions, converts overheal to Hydro damage bonus
    /// </summary>
    public class Hediff_OverflowingCare : HediffWithComps
    {
        public Pawn caster;
        public float healPerAction = 2f;
        private float accumulatedOverheal = 0f;
        private int tickCounter = 0;

        public override string LabelInBrackets
        {
            get
            {
                try
                {
                    int hydroBonus = Mathf.RoundToInt((accumulatedOverheal / 1.5f) * 100f);
                    hydroBonus = Mathf.Min(hydroBonus, 35);
                    int ticksLeft = this.TryGetComp<HediffComp_Disappears>()?.ticksToDisappear ?? 0;
                    return $"+{hydroBonus}% Hydro, {(ticksLeft / 60f):F1}s";
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
                    FleckMaker.ThrowDustPuffThick(pawn.DrawPos, pawn.Map, 0.8f, new Color(0.3f, 0.9f, 1f));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_OverflowingCare.PostAdd: {ex}");
            }
        }

        public override void Tick()
        {
            base.Tick();

            try
            {
                tickCounter++;

                // Heal every 60 ticks (1 second)
                if (tickCounter >= 60)
                {
                    tickCounter = 0;

                    if (pawn != null && !pawn.Dead && pawn.health != null)
                    {
                        float currentHP = pawn.health.summaryHealth.SummaryHealthPercent * pawn.health.capacities.GetLevel(PawnCapacityDefOf.BloodPumping) * 100f;
                        float maxHP = 100f;

                        var injury = pawn.health.hediffSet.hediffs
                            .OfType<Hediff_Injury>()
                            .Where(h => h.CanHealNaturally())
                            .FirstOrDefault();

                        if (injury != null)
                        {
                            float healedAmount = Mathf.Min(healPerAction, injury.Severity);
                            injury.Heal(healPerAction);

                            // Check if at max HP - accumulate overheal
                            if (currentHP >= maxHP * 0.99f)
                            {
                                accumulatedOverheal += healPerAction;
                                accumulatedOverheal = Mathf.Min(accumulatedOverheal, 52.5f); // Cap at 35% bonus (52.5 / 1.5)

                                if (pawn.Map != null && pawn.Spawned)
                                {
                                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, 
                                        "Overheal!", 
                                        new Color(0.3f, 0.9f, 1f), 1.2f);
                                }
                            }
                        }
                    }
                }

                // Hydro damage bonus visual every 120 ticks
                if (ageTicks % 120 == 0 && pawn?.Map != null && pawn.Spawned)
                {
                    FleckMaker.ThrowDustPuffThick(pawn.DrawPos, pawn.Map, 0.5f, new Color(0.3f, 0.9f, 1f));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_OverflowingCare.Tick: {ex}");
            }
        }

        /// <summary>
        /// Get current Hydro damage bonus
        /// </summary>
        public float GetHydroDamageBonus()
        {
            return Mathf.Min((accumulatedOverheal / 1.5f) / 100f, 0.35f);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref healPerAction, "healPerAction", 2f);
            Scribe_Values.Look(ref accumulatedOverheal, "accumulatedOverheal", 0f);
        }
    }

    /// <summary>
    /// Taryz Tracker hediff - tracks enemy, counterattacks, retargets on death
    /// </summary>
    public class Hediff_TaryzTracker : HediffWithComps
    {
        public Pawn caster;
        public float attackPower = 20f;
        private int tickCounter = 0;

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
                    FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_TaryzTracker.PostAdd: {ex}");
            }
        }

        public override void Tick()
        {
            base.Tick();

            try
            {
                tickCounter++;

                // Visual tracking effect every 60 ticks
                if (tickCounter >= 60 && pawn?.Map != null && pawn.Spawned)
                {
                    tickCounter = 0;
                    FleckMaker.ThrowDustPuffThick(pawn.DrawPos, pawn.Map, 0.6f, new Color(0.5f, 0.8f, 1f));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_TaryzTracker.Tick: {ex}");
            }
        }

        /// <summary>
        /// When tracked pawn attacks an ally, Taryz counterattacks
        /// </summary>
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            
            try
            {
                if (pawn == null || pawn.Dead || caster == null || caster.Dead) return;

                // Check if damage was dealt to an ally
                if (dinfo.IntendedTarget is Pawn target && target != null && !target.Dead)
                {
                    if (target.Faction == caster.Faction)
                    {
                        // Taryz counterattack!
                        TaryzCounterattack(target);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_TaryzTracker.Notify_PawnPostApplyDamage: {ex}");
            }
        }

        private void TaryzCounterattack(Pawn allyVictim)
        {
            try
            {
                // Deal Hydro damage to tracked enemy
                int counterDamage = Rand.Range(10, 20);
                float scaledDamage = counterDamage * (attackPower / 20f);

                DamageDef hydroDmg = DefDatabase<DamageDef>.GetNamedSilentFail("GFL_HydroDamage") ?? DamageDefOf.Burn;
                DamageInfo dinfo = new DamageInfo(hydroDmg, Mathf.RoundToInt(scaledDamage), 0f, -1f, caster);
                pawn.TakeDamage(dinfo);

                // Heal the ally
                if (allyVictim != null && !allyVictim.Dead && allyVictim.health != null)
                {
                    var injury = allyVictim.health.hediffSet.hediffs
                        .OfType<Hediff_Injury>()
                        .Where(h => h.CanHealNaturally())
                        .FirstOrDefault();

                    if (injury != null)
                    {
                        float healAmount = attackPower * 0.3f;
                        injury.Heal(healAmount);

                        if (allyVictim.Map != null && allyVictim.Spawned)
                        {
                            MoteMaker.ThrowText(allyVictim.DrawPos, allyVictim.Map, 
                                $"+{Mathf.RoundToInt(healAmount)}", 
                                new Color(0.4f, 1f, 0.8f), 1.5f);
                        }
                    }
                }

                // Visual feedback
                if (pawn.Map != null && pawn.Spawned)
                {
                    FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1.2f);
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, 
                        "Taryz Strike!", 
                        new Color(0.5f, 0.8f, 1f), 2f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in TaryzCounterattack: {ex}");
            }
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            
            try
            {
                // If target died and hediff removed, try to retarget
                if (pawn != null && pawn.Dead && caster != null && !caster.Dead && caster.Map != null)
                {
                    RetargetToNewEnemy();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Hediff_TaryzTracker.PostRemoved: {ex}");
            }
        }

        private void RetargetToNewEnemy()
        {
            try
            {
                // Find enemy with highest current HP
                Pawn newTarget = caster.Map.mapPawns.AllPawnsSpawned
                    .Where(p => p != null && !p.Dead && p.HostileTo(caster) && p != pawn)
                    .OrderByDescending(p => p.health.summaryHealth.SummaryHealthPercent)
                    .FirstOrDefault();

                if (newTarget != null && newTarget.health != null)
                {
                    HediffDef taryzDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_TaryzTracker");
                    if (taryzDef != null)
                    {
                        Hediff_TaryzTracker newTracker = HediffMaker.MakeHediff(taryzDef, newTarget) as Hediff_TaryzTracker;
                        if (newTracker != null)
                        {
                            newTracker.caster = caster;
                            newTracker.attackPower = attackPower;
                            newTarget.health.AddHediff(newTracker);

                            if (newTarget.Map != null && newTarget.Spawned)
                            {
                                FleckMaker.ThrowLightningGlow(newTarget.DrawPos, newTarget.Map, 1.5f);
                                MoteMaker.ThrowText(newTarget.DrawPos, newTarget.Map, 
                                    "Taryz Retarget!", 
                                    new Color(0.5f, 0.8f, 1f), 2.5f);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in RetargetToNewEnemy: {ex}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref attackPower, "attackPower", 20f);
        }
    }
}
