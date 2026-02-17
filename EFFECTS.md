# GFL Weapons - Status Effects Quick Reference

## DEBUFFS (Enemy)

### Frost (Suomi)
- **Avalanche**: 20% slow, +20% damage taken, 5s
- **Frost Terrain**: Slow movement on frozen ground, 1 hour

### Electric (Mosin)
- **Conductivity**: +10% pain, vulnerable to paralysis, 4-6s
- **Paralysis**: Cannot move/act, 2s

### Fire (Trailblazer)
- **Scorch Mark**: Fire DOT, stacks up to 10, multiplicative damage
  - 1-2 stacks: Minor burns, +5% pain
  - 3-5 stacks: Moderate burns, +15% pain, -5% movement
  - 6-8 stacks: Severe burns, +30% pain, -10% movement, -10% manipulation
  - 9-10 stacks: Critical burns, +50% pain, -20% movement, -15% manipulation, -15% consciousness

### Physical (Hestia)
- **Rend**: +30% physical damage per stack (max 8), 15s
  - 1-2 stacks: Minor
  - 3-5 stacks: Moderate, -5% movement
  - 6-8 stacks: Severe, -10% movement
- **Gash**: Bleeding DOT (8% of applier attack × stacks), consumes 2 stacks/second

### Corrosion (Skylla)
- **Toxic Infiltration**: Adds 1 Corrosive Infusion/second, death explosion (6-tile), 20s
- **Corrosive Infusion**: AoE DOT (12% weapon damage × stacks, 1.5-tile radius), max 10 stacks, 10s
  - 5+ stacks: -10% movement
  - 10 stacks: -20% movement

### Hydro (Radiance)
- **False Intelligence**: +20% stagger, +10% Hydro damage, 40s
- **Congestion**: No healing, 20s
- **Damp**: -40% healing per stack, transforms to Congestion at 2 stacks, 15s
- **Taryz Tracker**: Counterattacks when tracked enemy attacks ally (10-20 damage + heal ally 6 HP), retargets on death, 50s

### Electric (Aglaea)
- **Negative Charge**: Vulnerable to splash damage from electric attacks, 10s

---

## BUFFS (Ally)

### Defense
- **Frost Barrier** (Suomi): 80 HP shield, 1 HP/s regen, cleanses 1 debuff, 30s
- **Shelter** (Aglaea): -10% damage per stack (max 2 stacks = -20%)
- **Fortified Stance** (Aglaea): -50% incoming damage, -10% movement, 2s

### Healing
- **Positive Charge** (Aglaea): +20% max HP heal/second, synergy bonus with nearby allies, 3s
- **Deep-Rooted Bonds** (Radiance): 2× max HP (all body parts), heals 30 HP on expire, 50s
- **Overflowing Care** (Radiance): 2 HP/s, overheal converts to +Hydro damage (1.5 overheal = +1% damage, max +35%), 50s

### Combat
- **Confectance Index** (Aglaea/Hestia): +5% accuracy/dodge per stack (max 10 stacks, Aglaea) or max 50 stacks (Hestia), 30s/60s

---

## ELEMENT INTERACTIONS

### Chain Reactions
- **Toxic Infiltration** (Skylla): Death explosions spread to nearby enemies (max 10 explosions/0.5s)
- **Taryz Tracker** (Radiance): Auto-retargets to highest HP enemy on death

### Transformations
- **Damp → Congestion** (Radiance): 2 stacks of Damp = full heal block
- **Conductivity → Paralysis** (Mosin): Conductivity enables 2s stun on ultimate hit

### Stacking
- **Multiplicative**: Scorch Mark damage, Rend vulnerability
- **Additive**: Corrosive Infusion stacks, Confectance Index stats
- **Consumption**: Gash (2 stacks/tick), Rend (6 stacks on execute)

---

## QUICK DAMAGE REFERENCE

**DOT Effects:**
- Scorch Mark: Varies by stacks, fire damage
- Gash: 8% applier attack × stacks per second
- Corrosive Infusion: 12% weapon damage × stacks per second (AoE)
- Overflowing Care: +2 HP/s (heal)
- Positive Charge: +20% max HP/s (heal)

**Vulnerability:**
- Rend: ×(1 + 0.30 × stacks) physical damage
- Avalanche: +20% all damage
- False Intelligence: +10% Hydro damage
- Negative Charge: 30% splash damage on electric hit

**Cleansable:**
- Yes: Damp, Congestion, Shelter
- No: Toxic Infiltration, False Intelligence, Taryz Tracker, Rend, Gash, Scorch Mark, Fortified Stance

---

## ULTIMATE SYNERGIES

**Best Combos:**
1. Hestia → Skylla: Apply Rend (8 stacks) → Overpowering Corrosion for massive damage
2. Radiance → Anyone: Deep-Rooted Bonds (2× HP) + Overflowing Care (healing) = tank boost
3. Mosin → Mosin: Conductivity → Declaration of Victory = 2s paralysis
4. Aglaea turrets → Aglaea: Each turret death = +1 Confectance Index (accuracy/dodge)
5. Skylla → Skylla: Toxic Infiltration death chains = area denial

**Status Duration Priority:**
- Long: Deep-Rooted Bonds (50s), Overflowing Care (50s), Taryz Tracker (50s)
- Medium: False Intelligence (40s), Frost Barrier (30s)
- Short: Avalanche (5s), Paralysis (2s), Fortified Stance (2s)
