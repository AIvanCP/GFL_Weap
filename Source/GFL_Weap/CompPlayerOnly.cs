using System;
using RimWorld;
using Verse;

namespace GFL_Weap
{
    /// <summary>
    /// Comp that restricts weapon to player faction only.
    /// Prevents enemies and neutral factions from using the weapon.
    /// Reusable for all GFL weapons.
    /// </summary>
    public class CompPlayerOnly : ThingComp
    {
        public CompProperties_PlayerOnly Props => (CompProperties_PlayerOnly)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            CheckAndHandleNonPlayerOwnership();
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            
            try
            {
                if (pawn == null || pawn.Dead)
                {
                    return;
                }

                // Check if equipped by non-player faction
                if (pawn.Faction != Faction.OfPlayer)
                {
                    // Force unequip and drop
                    Log.Warning($"[GFL Weapons] {parent.Label} equipped by non-player pawn {pawn.LabelShort}. Forcing drop.");
                    
                    if (pawn.equipment != null && pawn.equipment.Primary == parent)
                    {
                        pawn.equipment.TryDropEquipment(parent, out ThingWithComps resultingThing, pawn.Position, false);
                    }
                    
                    // Optional: destroy instead of drop (set destroyOnNonPlayerUse in CompProperties)
                    if (Props.destroyOnNonPlayerUse && !parent.Destroyed)
                    {
                        Messages.Message($"{parent.Label} cannot be used by {pawn.Faction?.Name ?? "non-player"} factions.", MessageTypeDefOf.NeutralEvent, false);
                        parent.Destroy(DestroyMode.Vanish);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in CompPlayerOnly.Notify_Equipped: {ex}");
            }
        }

        private void CheckAndHandleNonPlayerOwnership()
        {
            try
            {
                // Check if spawned in non-player pawn inventory
                if (parent.ParentHolder is Pawn_EquipmentTracker eqTracker)
                {
                    Pawn pawn = eqTracker.pawn;
                    if (pawn != null && pawn.Faction != Faction.OfPlayer)
                    {
                        Log.Warning($"[GFL Weapons] {parent.Label} found on non-player pawn {pawn.LabelShort} at spawn. Removing.");
                        
                        if (Props.destroyOnNonPlayerUse)
                        {
                            parent.Destroy(DestroyMode.Vanish);
                        }
                        else
                        {
                            pawn.equipment?.TryDropEquipment(parent, out ThingWithComps _, pawn.Position, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error in CompPlayerOnly.CheckAndHandleNonPlayerOwnership: {ex}");
            }
        }
    }

    public class CompProperties_PlayerOnly : CompProperties
    {
        public bool destroyOnNonPlayerUse = false; // If true, destroys item instead of dropping it

        public CompProperties_PlayerOnly()
        {
            compClass = typeof(CompPlayerOnly);
        }
    }
}
