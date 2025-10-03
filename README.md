# GFL Weapons Mod for RimWorld

A RimWorld mod that adds weapons from Girls' Frontline with unique abilities. This mod is designed as an expandable framework for multiple GFL weapons.

## Current Weapons

### Suomi SMG Weapon
- Industrial-tier submachine gun
- 3-round burst fire
- 24-tile range
- Similar stats to vanilla SMG

### Freeze Shot Ability
- Cooldown: 5 seconds
- Range: 6 tiles
- Launches a freezing bolt that deals weapon damage + 10-20 frost damage
- Stuns target based on nearby allies (0.5s per ally within 6 tiles)
- Blue projectile with frost visual effects

### Frost Barrier Ultimate Ability
- Cooldown: 60 seconds
- Range: 6 tiles (target location)
- Creates a 3-tile radius frost zone with multiple effects:
  - Deals 10-20 damage to all enemies in area
  - Applies Avalanche debuff (20% slow, +20% damage taken, 5s duration)
  - Creates frost terrain tiles that persist for 1 in-game hour
  - Grants all allied pawns a Frost Barrier shield:
    - 80 HP damage absorption
    - 1 HP/second regeneration
    - Cleanses 1 negative effect on application
    - Lasts 30 seconds or until depleted

## Obtaining Weapons

GFL weapons are **player-only** and can be obtained through:
- **Trading**: Available from combat suppliers (rare)
- **Quest Rewards**: Can appear as quest rewards
- **Crafting**: Can be crafted at machining table (requires Gas Operation research)

## Installation

1. Copy the entire `GFL_Weap` folder to your RimWorld `Mods` directory
2. Enable the mod in the game's mod manager
3. Start a new game or load an existing save

## Building from Source

### Prerequisites
- .NET Framework 4.8 SDK
- Visual Studio 2019+ or MSBuild
- RimWorld installation at `D:\Game\Rimworld` (or set RIMWORLD_DIR environment variable)

### Build Steps

1. The project uses automatic path detection:
   - Set `RIMWORLD_DIR` environment variable to your RimWorld installation
   - Or it will default to `D:\Game\Rimworld`

2. Build the project:
   ```powershell
   cd Source/GFL_Weap
   dotnet build -c Release
   ```
   
   Or with MSBuild:
   ```powershell
   msbuild GFL_Weap.csproj /p:Configuration=Release
   ```

3. The compiled DLL will be placed in the `Assemblies` folder automatically

## Compatibility

- **RimWorld Version**: 1.5 and 1.6
- **Combat Extended**: Compatible (will fall back to vanilla mechanics if CE is detected)
- **Other Mods**: Should be compatible with most mods

## File Structure

```
GFL_Weap/
├── About/
│   ├── About.xml           # Mod metadata
│   ├── Preview.png         # Mod preview image
│   └── LoadFolders.xml     # Version-specific loading
├── Assemblies/
│   └── GFL_Weap.dll        # Compiled C# code (after build)
├── Defs/
│   ├── AbilityDefs/        # Ability definitions
│   ├── DamageDefs/         # Custom damage types
│   ├── HediffDefs/         # Status effects (buffs/debuffs)
│   ├── TerrainDefs/        # Frost terrain
│   ├── ThingDefs/          # Weapon and projectile definitions
│   ├── TraderKindDefs/     # Trader availability
│   └── ThingSetMakerDefs/  # Quest rewards
├── Source/
│   └── GFL_Weap/           # C# source code
│       ├── Ability_FreezeShot.cs
│       ├── Ability_FrostBarrier.cs
│       ├── CompWeaponAbilities.cs
│       ├── CompPlayerOnly.cs
│       ├── Hediffs.cs
│       ├── ModInitializer.cs
│       ├── WeaponUtility.cs
│       └── GFL_Weap.csproj
└── Textures/
    ├── Things/
    │   └── Weapons/
    │       └── Suomi/
    │           └── Suomi_smg.png    # Weapon texture (64x64)
    └── UI/
        └── Icons/
            └── Abilities/
                ├── FreezeShot.png    # Ability icon (64x64)
                └── FrostBarrier.png  # Ability icon (64x64)
```

## Technical Details

### C# Classes

- **CompWeaponAbilities**: Generalized component that grants abilities when weapon is equipped (reusable for all GFL weapons)
- **CompPlayerOnly**: Prevents non-player factions from using GFL weapons
- **Verb_FreezeShot**: Handles Freeze Shot ability casting
- **Projectile_FreezeBolt**: Custom projectile with stun mechanics
- **Verb_FrostBarrier**: Handles Frost Barrier ultimate ability
- **Hediff_FrostBarrier**: Shield hediff with damage absorption and healing
- **Hediff_Avalanche**: Slow debuff applied to enemies
- **FrostTerrainManager**: Manages frost terrain persistence and effects
- **WeaponUtility**: Helper methods and compatibility checks (weapon-agnostic)

### XML Definitions

- **ThingDef**: Weapon definition with CompAbilities
- **AbilityDef**: Two ability definitions (Freeze Shot, Frost Barrier)
- **HediffDef**: Three hediff definitions (FrostBarrier, Avalanche, FrostTerrain)
- **ProjectileDef**: Custom freeze bolt projectile
- **DamageDef**: Frost damage type
- **TerrainDef**: Frost zone terrain

## Known Issues

- None currently reported

## Credits

**Original Characters and Concepts:**
- Girls' Frontline © Sunborn Games / MICA Team
- This is a fan-made mod and is not officially affiliated with Girls' Frontline

**Mod Development:**
- Created by AIvanCP for RimWorld

## License

This mod is provided as-is for personal use. 
- Girls' Frontline characters and concepts belong to Sunborn Games / MICA Team
- RimWorld modding framework belongs to Ludeon Studios
- Mod code is available for community use and learning
