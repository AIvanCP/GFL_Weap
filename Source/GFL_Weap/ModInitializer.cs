using System;
using Verse;
using RimWorld;

namespace GFL_Weap
{
    /// <summary>
    /// Mod entry point - handles initialization
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ModInitializer
    {
        static ModInitializer()
        {
            try
            {
                Log.Message("[GFL Weapons] GFL Weapons mod loaded successfully!");
                
                // Check for Combat Extended
                if (WeaponUtility.IsCombatExtendedActive())
                {
                    Log.Message("[GFL Weapons] Combat Extended detected. Compatibility mode enabled.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[GFL Weapons] Error during initialization: {ex}");
            }
        }
    }
}
