using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// Verb for Frost Barrier ultimate ability - tile-based AoE
    /// </summary>
    public class Verb_FrostBarrier : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            try
            {
                if (currentTarget.IsValid)
                {
                    IntVec3 targetCell = currentTarget.Cell;
                    Map map = CasterPawn.Map;

                    if (map == null)
                    {
                        return false;
                    }

                    // Visual effects at target location
                    FleckMaker.Static(targetCell.ToVector3Shifted(), map, FleckDefOf.ExplosionFlash, 12f);
                    
                    for (int i = 0; i < 5; i++)
                    {
                        FleckMaker.ThrowAirPuffUp(targetCell.ToVector3Shifted(), map);
                    }

                    // Get all cells within 3-tile radius
                    List<IntVec3> affectedCells = new List<IntVec3>();
                    foreach (IntVec3 cell in GenRadial.RadialCellsAround(targetCell, 3f, true))
                    {
                        if (cell.InBounds(map))
                        {
                            affectedCells.Add(cell);
                        }
                    }

                    // Damage enemies in AoE and apply Avalanche debuff
                    List<Pawn> enemiesHit = new List<Pawn>();
                    foreach (IntVec3 cell in affectedCells)
                    {
                        // Copy list to avoid collection modification during enumeration
                        List<Thing> things = cell.GetThingList(map).ToList();
                        foreach (Thing thing in things)
                        {
                            if (thing is Pawn pawn && !pawn.Dead && pawn.Faction != Faction.OfPlayer)
                            {
                                if (!enemiesHit.Contains(pawn))
                                {
                                    // Deal 10-20 damage
                                    int damage = Rand.Range(10, 20);
                                    DamageDef frostDmg = DefDatabase<DamageDef>.GetNamedSilentFail("GFL_FrostDamage") ?? DamageDefOf.Frostbite;
                                    DamageInfo damageInfo = new DamageInfo(frostDmg, damage, 0f, -1f, CasterPawn);
                                    pawn.TakeDamage(damageInfo);

                                    // Apply Avalanche debuff (stack 2)
                                    HediffDef avalancheDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_Avalanche");
                                    if (avalancheDef != null)
                                    {
                                        // Apply twice for stack 2
                                        Hediff existingAvalanche = pawn.health.hediffSet.GetFirstHediffOfDef(avalancheDef);
                                        if (existingAvalanche != null)
                                        {
                                            existingAvalanche.Severity = Mathf.Min(existingAvalanche.Severity + 1f, 2f);
                                        }
                                        else
                                        {
                                            Hediff avalanche = HediffMaker.MakeHediff(avalancheDef, pawn);
                                            avalanche.Severity = 2f;
                                            pawn.health.AddHediff(avalanche);
                                        }
                                    }

                                    enemiesHit.Add(pawn);
                                }
                            }
                        }
                    }

                    // Create frost terrain
                    TerrainDef frostTerrain = DefDatabase<TerrainDef>.GetNamedSilentFail("GFL_FrostTerrain");
                    if (frostTerrain != null)
                    {
                        foreach (IntVec3 cell in affectedCells)
                        {
                            if (cell.InBounds(map) && cell.Walkable(map))
                            {
                                // Store original terrain before changing
                                TerrainDef originalTerrain = map.terrainGrid.TerrainAt(cell);
                                map.terrainGrid.SetTerrain(cell, frostTerrain);
                                
                                // Schedule terrain revert after 1 in-game hour (2500 ticks)
                                if (FrostTerrainManager.Instance != null)
                                {
                                    FrostTerrainRevertData revertData = new FrostTerrainRevertData
                                    {
                                        cell = cell,
                                        originalTerrain = originalTerrain,
                                        expireTick = Find.TickManager.TicksGame + 2500,
                                        map = map
                                    };
                                    
                                    FrostTerrainManager.Instance.RegisterFrostTerrain(revertData);
                                }
                            }
                        }
                    }

                    // Apply Frost Barrier buff to all allied pawns
                    List<Pawn> alliedPawns = map.mapPawns.AllPawnsSpawned
                        .Where(p => p.Faction == Faction.OfPlayer && !p.Dead)
                        .ToList();

                    foreach (Pawn ally in alliedPawns)
                    {
                        HediffDef frostBarrierDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_FrostBarrier");
                        if (frostBarrierDef != null)
                        {
                            // Remove existing frost barrier if any
                            Hediff existingBarrier = ally.health.hediffSet.GetFirstHediffOfDef(frostBarrierDef);
                            if (existingBarrier != null)
                            {
                                ally.health.RemoveHediff(existingBarrier);
                            }

                            // Add new frost barrier
                            Hediff_FrostBarrier barrier = (Hediff_FrostBarrier)HediffMaker.MakeHediff(frostBarrierDef, ally);
                            ally.health.AddHediff(barrier);

                            // Cleanse 1 random negative hediff
                            CleanseRandomDebuff(ally);
                        }
                    }

                    // No spam message - visual effects only
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in Verb_FrostBarrier.TryCastShot: {ex}");
            }

            return false;
        }

        private void CleanseRandomDebuff(Pawn pawn)
        {
            try
            {
                List<Hediff> negativeHediffs = pawn.health.hediffSet.hediffs
                    .Where(h => h.def.isBad && h.def.everCurableByItem)
                    .ToList();

                if (negativeHediffs.Any())
                {
                    Hediff toRemove = negativeHediffs.RandomElement();
                    pawn.health.RemoveHediff(toRemove);
                    
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Cleansed!", Color.cyan, 2f);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error cleansing debuff: {ex}");
            }
        }
    }

    /// <summary>
    /// Manages frost terrain revert timing
    /// </summary>
    public class FrostTerrainManager : GameComponent
    {
        private static FrostTerrainManager instance;
        public static FrostTerrainManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Current.Game?.GetComponent<FrostTerrainManager>();
                }
                return instance;
            }
        }

        private List<FrostTerrainRevertData> frostTerrains = new List<FrostTerrainRevertData>();

        public FrostTerrainManager(Game game)
        {
            instance = this;
        }

        public void RegisterFrostTerrain(FrostTerrainRevertData data)
        {
            frostTerrains.Add(data);
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            try
            {
                int currentTick = Find.TickManager.TicksGame;
                List<FrostTerrainRevertData> toRemove = new List<FrostTerrainRevertData>();

                foreach (var data in frostTerrains)
                {
                    if (currentTick >= data.expireTick)
                    {
                        // Revert terrain to original
                        if (data.map != null && data.cell.InBounds(data.map))
                        {
                            if (data.originalTerrain != null)
                            {
                                data.map.terrainGrid.SetTerrain(data.cell, data.originalTerrain);
                            }
                        }
                        toRemove.Add(data);
                    }
                    else
                    {
                        // Check for pawns on frost terrain and apply debuff
                        if (data.map != null && data.cell.InBounds(data.map))
                        {
                            // Copy list to avoid collection modification during enumeration
                            List<Thing> things = data.cell.GetThingList(data.map).ToList();
                            foreach (Thing thing in things)
                            {
                                if (thing is Pawn pawn && !pawn.Dead && pawn.Faction != Faction.OfPlayer)
                                {
                                    HediffDef frostTerrainDef = DefDatabase<HediffDef>.GetNamedSilentFail("GFL_Hediff_FrostTerrain");
                                    if (frostTerrainDef != null)
                                    {
                                        Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(frostTerrainDef);
                                        if (existing == null)
                                        {
                                            Hediff frostDebuff = HediffMaker.MakeHediff(frostTerrainDef, pawn);
                                            pawn.health.AddHediff(frostDebuff);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var data in toRemove)
                {
                    frostTerrains.Remove(data);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in FrostTerrainManager tick: {ex}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref frostTerrains, "frostTerrains", LookMode.Deep);
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit && frostTerrains == null)
            {
                frostTerrains = new List<FrostTerrainRevertData>();
            }
        }
    }

    public class FrostTerrainRevertData : IExposable
    {
        public IntVec3 cell;
        public TerrainDef originalTerrain;
        public int expireTick;
        public Map map;

        public void ExposeData()
        {
            Scribe_Values.Look(ref cell, "cell");
            Scribe_Defs.Look(ref originalTerrain, "originalTerrain");
            Scribe_Values.Look(ref expireTick, "expireTick");
            Scribe_References.Look(ref map, "map");
        }
    }
}
