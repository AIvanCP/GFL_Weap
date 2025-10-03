# Changelog - GFL Suomi SMG Mod

## Version 1.0.0 (Initial Release)

### Features
- Added Suomi SMG weapon (Industrial-tier submachine gun)
  - 3-round burst fire
  - 24-tile effective range
  - Balanced stats similar to vanilla SMG
  - Craftable at machining table with Gas Operation research

- Freeze Shot ability (5s cooldown)
  - Launches frost bolt projectile at enemy within 6 tiles
  - Deals weapon damage + 10-20 frost damage
  - Stuns target based on nearby allies (0.5s per ally within 6 tiles)
  - Blue-tinted projectile with frost visual effects

- Frost Barrier ultimate ability (60s cooldown)
  - Target empty tile within 6 tiles
  - Creates 3-tile radius frost zone with multiple effects:
    - AoE damage to enemies (10-20 damage)
    - Applies Avalanche debuff (20% slow, 20% increased damage taken, 5s duration)
    - Creates persistent frost terrain (lasts 1 in-game hour)
    - Grants Frost Barrier shield to all allies:
      - 80 HP damage absorption
      - 1 HP/second regeneration
      - Cleanses 1 random negative effect on application
      - Lasts 30 seconds or until depleted

### Technical Implementation
- Custom CompAbilities system for granting abilities on equip
- Frost terrain management with automatic revert
- Shield hediff with damage absorption mechanics
- Debuff system with stat modifications
- Comprehensive error handling and logging
- Combat Extended compatibility layer

### Compatibility
- RimWorld 1.5 and 1.6
- Combat Extended: Compatible (fallback to vanilla mechanics)
- Should work with most other mods

### Known Issues
- None at release

---

## Future Plans (TBD)
- Additional GFL weapon variants
- More ability visual effects
- Balance adjustments based on feedback
- Localization support

---

## Installation Notes
1. Extract to RimWorld/Mods folder
2. Enable in mod manager
3. Load order: After Core, after Royalty (if used), after/before CE (works either way)

## Uninstallation Notes
- Can be safely removed from existing saves
- Pawns with Suomi equipped will lose their abilities
- Active shields/debuffs will persist until expired
- Frost terrain will remain until natural revert
