using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GFL_Weap
{
    /// <summary>
    /// ThingComp that grants abilities when a GFL weapon is equipped
    /// </summary>
    public class CompWeaponAbilities : ThingComp
    {
        public CompProperties_WeaponAbilities Props => (CompProperties_WeaponAbilities)props;

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            try
            {
                // Skip during pawn generation to avoid conflicts with other mods
                if (pawn == null || !pawn.Spawned || pawn.Map == null)
                {
                    return;
                }

                // Grant abilities when equipped
                if (pawn.abilities == null)
                {
                    pawn.abilities = new Pawn_AbilityTracker(pawn);
                }

                // Add abilities defined in Props
                if (Props.abilities != null)
                {
                    foreach (string abilityDefName in Props.abilities)
                    {
                        if (!pawn.abilities.AllAbilitiesForReading.Any(a => a.def.defName == abilityDefName))
                        {
                            var abilityDef = DefDatabase<AbilityDef>.GetNamedSilentFail(abilityDefName);
                            if (abilityDef != null)
                            {
                                pawn.abilities.GainAbility(abilityDef);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error granting abilities: {ex}");
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            try
            {
                // Skip if pawn is null or not properly initialized
                if (pawn == null)
                {
                    return;
                }

                // Remove abilities when unequipped
                if (pawn.abilities != null && Props.abilities != null)
                {
                    foreach (string abilityDefName in Props.abilities)
                    {
                        var ability = pawn.abilities.AllAbilitiesForReading.FirstOrDefault(a => a.def.defName == abilityDefName);
                        if (ability != null)
                        {
                            pawn.abilities.RemoveAbility(ability.def);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error removing abilities: {ex}");
            }
        }
    }

    public class CompProperties_WeaponAbilities : CompProperties
    {
        public List<string> abilities = new List<string>();

        public CompProperties_WeaponAbilities()
        {
            compClass = typeof(CompWeaponAbilities);
        }
    }
}
