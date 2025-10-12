using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace GFL_Weap
{
    /// <summary>
    /// Component properties for attaching Confectance tracking to weapons
    /// </summary>
    public class CompProperties_Confectance : CompProperties
    {
        public CompProperties_Confectance()
        {
            compClass = typeof(CompConfectance);
        }
    }

    /// <summary>
    /// ThingComp that stores Confectance Index per pawn wielder.
    /// Used by Trailblazer weapon to track combat momentum.
    /// </summary>
    public class CompConfectance : ThingComp
    {
        // Static storage for all pawns' Confectance Index
        private static Dictionary<int, int> confectanceByPawnID = new Dictionary<int, int>();

        public CompProperties_Confectance Props => (CompProperties_Confectance)props;

        /// <summary>
        /// Get the current Confectance Index for a pawn.
        /// </summary>
        public static int Get(Pawn pawn)
        {
            if (pawn == null)
            {
                return 0;
            }

            if (confectanceByPawnID.TryGetValue(pawn.thingIDNumber, out int value))
            {
                return value;
            }

            return 0;
        }

        /// <summary>
        /// Add Confectance Index to a pawn.
        /// </summary>
        public static void Add(Pawn pawn, int amount)
        {
            if (pawn == null || amount <= 0)
            {
                return;
            }

            int current = Get(pawn);
            confectanceByPawnID[pawn.thingIDNumber] = current + amount;

            // Optional: Log for debugging
            // Log.Message($"[GFL Weapons] Added {amount} Confectance to {pawn.LabelShort}, now at {current + amount}");
        }

        /// <summary>
        /// Consume all Confectance Index from a pawn and return the amount consumed.
        /// </summary>
        public static int ConsumeAll(Pawn pawn)
        {
            if (pawn == null)
            {
                return 0;
            }

            int current = Get(pawn);
            if (current > 0)
            {
                confectanceByPawnID[pawn.thingIDNumber] = 0;
                // Log.Message($"[GFL Weapons] Consumed {current} Confectance from {pawn.LabelShort}");
            }

            return current;
        }

        /// <summary>
        /// Save/load Confectance data across game saves.
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();

            // Save/load the entire dictionary
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // Convert dictionary to lists for serialization
                List<int> keys = new List<int>(confectanceByPawnID.Keys);
                List<int> values = new List<int>(confectanceByPawnID.Values);
                Scribe_Collections.Look(ref keys, "confectanceKeys", LookMode.Value);
                Scribe_Collections.Look(ref values, "confectanceValues", LookMode.Value);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                List<int> keys = null;
                List<int> values = null;
                Scribe_Collections.Look(ref keys, "confectanceKeys", LookMode.Value);
                Scribe_Collections.Look(ref values, "confectanceValues", LookMode.Value);

                if (keys != null && values != null && keys.Count == values.Count)
                {
                    confectanceByPawnID.Clear();
                    for (int i = 0; i < keys.Count; i++)
                    {
                        confectanceByPawnID[keys[i]] = values[i];
                    }
                }
            }
        }

        /// <summary>
        /// Clean up dead pawns from the dictionary periodically.
        /// </summary>
        public override void CompTick()
        {
            base.CompTick();

            // Cleanup every 250 ticks (~4 seconds)
            if (parent.IsHashIntervalTick(250))
            {
                CleanupDeadPawns();
            }
        }

        private static void CleanupDeadPawns()
        {
            try
            {
                List<int> toRemove = new List<int>();

                foreach (var kvp in confectanceByPawnID)
                {
                    // Check if pawn still exists and is not destroyed
                    bool found = false;
                    foreach (Map map in Find.Maps)
                    {
                        foreach (Pawn pawn in map.mapPawns.AllPawns)
                        {
                            if (pawn.thingIDNumber == kvp.Key)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }

                    if (!found)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (int id in toRemove)
                {
                    confectanceByPawnID.Remove(id);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] CompConfectance cleanup error: {ex.Message}");
            }
        }
    }
}
