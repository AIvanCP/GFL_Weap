# GFL Weapons Mod for RimWorld

A RimWorld mod that adds weapons from Girls' Frontline with unique abilities. This mod is designed as an expandable framework for multiple GFL weapons.

## Current Weapons

### 1. Suomi SMG Weapon
- Industrial-tier submachine gun
- 3-round burst fire
- 24-tile range
- Similar stats to vanilla SMG

#### Freeze Shot Ability
- Cooldown: 5 seconds
- Range: 6 tiles
- Launches a freezing bolt that deals weapon damage + 10-20 frost damage
- Stuns target based on nearby allies (0.5s per ally within 6 tiles)
- Blue projectile with frost visual effects

#### Frost Barrier Ultimate Ability
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

### 2. Mosin-Nagant Sniper Rifle
- Precision sniper rifle
- Long-range combat (45 tiles)
- Electric damage abilities
- Higher cost than Suomi (80 Steel + 5 Components + 5 Gold)

#### Target Victory Ability
- Cooldown: 5 seconds
- Range: 35 tiles (sniper range)
- Deals 130% weapon damage
- Applies Conductivity debuff to target
- Makes target vulnerable to electrical attacks

#### Declaration of Victory Ultimate Ability
- Cooldown: 13 seconds
- Range: 35 tiles (sniper range)
- Deals 180% weapon damage
- If target has Conductivity, applies Paralysis debuff
- Powerful lightning surge visual effect

### 3. Aglaea Assault Rifle
- Advanced assault rifle with tactical AI systems
- 3-round burst fire
- 30-tile range
- Higher cost than Suomi but less than Mosin (70 Steel + 4 Components + 10 Plasteel)
- **Player-only weapon**

#### Gentle Offensive Ability
- Cooldown: Instant (no cooldown)
- Range: 5 tiles (AR placement range)
- Summons an Auto-Turret on an empty tile
- Turret inherits 50% HP, 50% attack, 100% defense from caster
- Maximum 2 turrets active at once (oldest destroyed automatically)
- When turret dies, caster gains +1 Confectance Index

#### Fortification Protocol Ultimate Ability
- Cooldown: 15 seconds
- Self-cast (no targeting required)
- Effects on caster:
  - Restores 30% rest
  - Applies Fortified Stance (2 seconds, -50% incoming damage)
- Effects on all allies within 9 tiles:
  - Applies 2 stacks of Shelter (damage reduction)
  - Applies Positive Charge (20% max HP heal per second for 3 seconds)
  - Healing synergy: +15% if nearby ally also has Positive Charge

## Auto-Turret Mechanics (Aglaea)

### Turret Stats
- Inherits 50% of caster's Max HP
- Inherits 50% of caster's attack power (ShootingAccuracyPawn)
- Inherits 100% of caster's defense
- Range: 24 tiles
- Burst: 2 shots
- Damage: 12 per bullet

### Turret Behavior
- Stationary defensive building
- Rotating head tracks and fires at enemies
- No power requirement (self-powered)
- Explodes when destroyed (1.9 tile radius, 50% chance to not explode from damage)
- Despawns automatically when summoning 3rd turret (max 2 per caster)
- Grants Confectance Index buff to summoner on death

### Hediff System (Aglaea)
- **Confectance Index**: Stackable (max 10), +5% accuracy/dodge per stack, 30s duration
- **Negative Charge**: Electric vulnerability, 30% splash damage, 10s duration
- **Positive Charge**: 20% max HP heal/second, synergy bonus with nearby allies, 3s duration
- **Shelter**: Stackable damage reduction (2 stacks max)
- **Fortified Stance**: 2s defensive stance, -50% incoming damage, cannot be cleansed

### 4. Trailblazer Shotgun
- Fire-based tactical shotgun
- 3-round burst fire
- 16-tile range
- Moderate cost (50 Steel + 3 Components)

#### Searing Sizzle Ability
- Cooldown: 8 seconds
- Range: 10 tiles
- Deals 140% weapon damage
- Applies Inferno debuff (fire DOT, 15s duration)
- Consumes 1 Confectance Index if available (bonus damage)

#### Boil and Reduce Ultimate Ability
- Cooldown: 30 seconds
- Range: 6 tiles (target location)
- Creates 6-tile radius fire zone
- Deals 160% weapon damage to all enemies in area
- Applies Reduce debuff (-30% armor, 20s duration)
- Visual: Explosion flash + smoke

### 5. Hestia Pistol (CZ75)
- Debuff-stacking pistol with momentum mechanics
- 12 base damage, 25-tile range
- Complex multi-hediff system
- Moderate cost (40 Steel + 3 Components)
- **Player-only weapon**

#### Fissioned Firelight Ability
- Cooldown: 18 seconds
- Range: 6 tiles
- Deals 120% weapon damage (scales with Shooting skill: +1% per level)
- Applies 2 stacks of **Rend** to target (max 8 stacks)
- Grants +2 **Confectance Index** to caster
- Visual: Lightning glow + red dust puff

#### No Survivors Ultimate Ability
- Cooldown: 2 minutes (120 seconds)
- Range: 6 tiles (single target) + 6-tile AoE spread
- Base: Deals 160% weapon damage to primary target
- **Execute Bonus**: If target has 6+ Rend stacks:
  - Damage increases to 280% (160% × 1.75 multiplier)
  - Consumes 6 Rend stacks from target
- **Gash Spread**: Applies Gash stacks to all enemies with Rend within 6 tiles
  - Gash stacks = primary target's current Rend count
- Visual: Explosion flash + smoke + "EXECUTE!" text on bonus trigger

### 6. Skylla Assault Rifle
- Advanced corrosive assault rifle with toxic infiltration systems
- 14 base damage, 3-round burst, 28-tile range
- 0.32 armor penetration, 1.65s cooldown
- High cost (75 Steel + 5 Components + 12 Plasteel + 15 Chemfuel)
- **Player-only weapon**

#### Overpowering Corrosion Ability
- Cooldown: 6 seconds
- Range: 8 tiles (target location)
- **Throws a corrosive grenade** (arcing trajectory, NOT straight shot)
- **5-tile radius explosion** (large AoE)
- Deals 90% weapon damage + applies **Toxic Infiltration**
- Targets already with Toxic Infiltration take +15% bonus damage
- 15% chance for secondary mini-explosion (1.2 tile radius)
- When affected enemies die → **vibrant purple explosion** spreads Toxic Infiltration
- **SAFE FOR PLAYER FACTION** (never affects allies or player pawns)
- Visual: Vibrant purple explosions, toxic dust, purple motes

#### Devastating Drift Ultimate Ability
- Cooldown: 15 seconds
- Range: 4-8 tiles (dash distance)
- Dash to target location, creating 5-tile-wide **vibrant purple explosion trail**
- Deals 100% weapon damage to all enemies in path
- Applies **Toxic Infiltration** to all hit enemies
- If 2+ enemies die → gain 1 extra cast (once per activation)
- Avoids walls and unreachable areas
- **SAFE FOR PLAYER FACTION** (never affects allies or player pawns)
- Visual: Vibrant purple explosion trail with dust puffs and motes

#### Status Effects

**Toxic Infiltration**
- Duration: 20 seconds (1200 ticks)
- Adds 1 stack of Corrosive Infusion every second
- On death: **6-tile radius purple explosion** spreads Toxic Infiltration to nearby enemies
- Chain reaction: Up to 10 explosions per 0.5 seconds
- **SAFE FOR PLAYER FACTION** (never spreads to allies)
- Unremovable debuff
- Visual: Vibrant purple toxic clouds and motes

**Corrosive Infusion**
- Stackable: 1-10 stacks
- Duration: 10 seconds (refreshes on new stacks)
- Deals AoE DOT every second = 12% × stacks × weapon damage
- Affects enemies within 1.5 tiles
- Gains stacks from Toxic Infiltration ticks and corrosion damage hits
- At 5+ stacks: -10% movement speed
- At 10 stacks: -20% movement speed (critical)
- Visual: Purple dust puffs on tick damage

### 7. Radiance Sniper Rifle
- Precision Hydro sniper rifle with tactical support and follower summon
- 30 base damage, single shot, 45-tile range
- 0.38 armor penetration, 2.5s cooldown
- High cost (80 Steel + 5 Components + 10 Gold + 15 Plasteel)
- **Player-only weapon**

#### Intel Manipulation Ability
- Cooldown: 7 seconds
- Range: 35 tiles
- Fires a Hydro shot dealing 130% weapon damage
- Applies **False Intelligence** (vulnerability, 40s duration)
- Applies **Congestion** (no healing, 20s duration)
- Grants +1 **Confectance Index** to caster
- Visual: Blue Hydro explosion effects

#### Path of Protection Ultimate Ability
- Cooldown: 30 seconds
- Range: 35 tiles (target location)
- Creates 3-tile radius Hydro explosion
- Deals 80% weapon damage to all enemies in area
- **Enemy Effects**:
  - Applies **Damp** debuff (1 stack, transforms to Congestion at 2 stacks)
  - Applies **False Intelligence** to primary target
  - Summons **Taryz Tracker** to primary target
- **Ally Effects** (all allies within 35 tiles):
  - Applies **Deep-Rooted Bonds** (2× max HP, heals 30 HP on expire, 50s duration)
  - Applies **Overflowing Care** (passive healing + overheal conversion, 50s duration)
  - Heals 20 HP immediately
  - Cleanses 1 negative hediff
- **Safe for allies** (no friendly fire)
- Visual: Blue Hydro explosion, lightning glow

#### Status Effects

**False Intelligence**
- Duration: 40 seconds (2400 ticks)
- +20% stagger chance
- +10% Hydro damage taken (bonus damage applied on hit)
- Unremovable debuff
- Visual: Blue dust puff on application, bonus damage text

**Congestion**
- Duration: 20 seconds (1200 ticks)
- No healing from any source (wounds, medicines, etc.)
- Can be applied directly or transformed from 2 stacks of Damp
- Cleanseable
- Visual: Blue explosion flash on transformation from Damp

**Damp**
- Duration: 15 seconds (900 ticks)
- Stackable: 1-2 stacks
- -40% healing per stack
- At 2 stacks: Transforms into Congestion (stronger debuff)
- Cleanseable
- Visual: Blue dust puff, explosion flash on transformation

**Deep-Rooted Bonds** (Ally buff)
- Duration: 50 seconds (3000 ticks)
- **2× max HP**: Multiplies ALL body part HP by 2 (head, torso, limbs, organs - everything doubles)
- On expire: Heals 30 HP
- Applied to **ALL allies on the current map** (full map range)
- Visual: Lightning glow + green dust puff on application, heal text on expire

**Overflowing Care** (Ally buff)
- Duration: 50 seconds (3000 ticks)
- Heals 2 HP every second
- When at full HP: Accumulates overheal
- **Overheal Conversion**: Every 1.5 overheal = +1% Hydro damage (max +35%)
- Applied to **ALL allies on the current map** (full map range)
- Visual: Blue dust puff, "Overheal!" text when at max HP

**Taryz Tracker** (Enemy debuff)
- Duration: 50 seconds (3000 ticks)
- Applied to primary target of Path of Protection
- **Counterattack**: When tracked enemy **ATTACKS** an ally (hit OR miss):
  - Deals 10-20 Hydro damage to tracked enemy
  - Heals attacked ally for 30% of damage (6 HP)
  - Triggers on attack attempt, not just successful damage
- **Retargeting**: On tracked enemy death:
  - Automatically retargets to enemy with highest current HP
  - New target must not already have Taryz Tracker
- Unremovable debuff
- Visual: Lightning glow on application, "Taryz Strike!" text on counterattack, "Taryz Retarget!" text on retarget

## Hediff System (Hestia)

### Rend (Defense Shredder)
- **Max Stacks**: 8
- **Duration**: 15 seconds (refreshes on stack addition)
- **Effect**: Multiplies incoming physical damage by `1 + (0.30 × stacks)`
  - Example: 4 Rend stacks = 220% damage taken (1 + 1.20)
  - Example: 8 Rend stacks = 340% damage taken (1 + 2.40)
- **Affected Damage Types**: Bullet, Cut, Stab, Blunt, Scratch, Bite, Crush
- **Implementation**: Harmony patch on `Pawn.PreApplyDamage`
- **Stacks Merge**: New Rend applications combine with existing stacks (capped at 8)

### Gash (Damage Over Time)
- **Tick Rate**: Every 60 ticks (1 second)
- **Damage Formula**: `applierAttackDamage × 0.08 × stacks` per second
  - Example: Applier with 15 attack, 5 Gash stacks = 6 damage/second
- **Stack Consumption**: Consumes 2 stacks per tick (auto-removes when stacks depleted)
- **Damage Type**: Cut (affected by armor)
- **Stacks Merge**: Multiple Gash applications combine stacks
- **Persistence**: Saves applier reference and cached attack damage

### Confectance Index (Momentum System)
- **Max Stacks**: 50 (shared across all Hestia users)
- **Duration**: 60 seconds per stack
- **Gain**: +2 per Fissioned Firelight hit
- **Consumption**: Used by No Survivors for bonus effects (future implementation)
- **Visual**: Shows stack count in brackets `[x10]`

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

## Changelog

### Version 1.3.0 (October 8, 2025)
**Bug Fixes:**
- Fixed InvalidCastException in Target Victory and Declaration of Victory projectile spawning
- Fixed InvalidCastException in Gentle Offensive turret spawning
- Fixed InvalidCastException in Freeze Shot projectile spawning
- Fixed NullReferenceException in ApplyDirectDamage methods (added Map null checks)
- Fixed NullReferenceException in Freeze Shot (added targetPawn.Map null check)
- Fixed projectile thingClass definitions (changed from `Bullet` to proper custom classes)

**Balance Changes:**
- Increased Mosin sniper abilities range: 9 → 35 tiles (sniper-appropriate range)
- Increased Aglaea Gentle Offensive turret placement range: 3 → 5 tiles (AR-appropriate range)

**Visual Improvements:**
- Improved Aglaea auto-turret visual placement:
  - Reduced body size: 1.5 → 1.2 (more compact)
  - Reduced head size: 0.8 → 0.7 (better proportions)
  - Adjusted head position: moved left (-0.15) and up (0.5) for better alignment
  - Head now properly positioned on top of turret body

**Technical:**
- Changed Fortification Protocol to self-cast (no target selection required)
- Improved null safety across all ability classes
- Fixed all GenSpawn.Spawn direct casts to use safe `as` operator with null checks

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
