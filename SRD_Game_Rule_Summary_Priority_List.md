# SRD Game Rule Summary Priority List

## Objective
Add "Game Rule Summary" sections to all SRD documents to explain technical mechanics in layman's terms. This makes the documentation accessible to both developers and game designers who need to understand DAoC mechanics.

## Current Status
- **Total SRD Documents**: 145
- **Documents with Game Rule Summaries**: 3
  - SRD/01_Combat_Systems/Attack_Resolution.md ✅
  - SRD/01_Combat_Systems/Damage_Calculation.md ✅  
  - SRD/02_Character_Systems/Character_Progression.md ✅

## Priority 1: Core Combat Systems (Critical for all players)
These systems affect every combat interaction and need immediate attention:

1. **SRD/01_Combat_Systems/Defense_Mechanics.md** - How evade/parry/block work
2. **SRD/01_Combat_Systems/Style_Mechanics.md** - Combat styles and positioning
3. **SRD/01_Combat_Systems/Attack_Speed_Timing.md** - Attack timing and speed
4. **SRD/01_Combat_Systems/Critical_Strike_System.md** - Critical hits
5. **SRD/01_Combat_Systems/Resistance_System.md** - Damage resistance

## Priority 2: Character Development (Essential for progression)
Systems that players interact with for character growth:

1. **SRD/02_Character_Systems/Character_Class_System.md** - Class mechanics
2. **SRD/02_Character_Systems/Specialization_Skills.md** - Skill training
3. **SRD/02_Character_Systems/Realm_Points_Ranks.md** - RvR progression
4. **SRD/02_Character_Systems/Master_Levels_System.md** - ML progression
5. **SRD/02_Character_Systems/Champion_Level_System.md** - CL progression

## Priority 3: Magic Systems (Critical for casters)
Systems that affect all spellcasting:

1. **SRD/03_Magic_Systems/Spell_Mechanics.md** - How spells work
2. **SRD/03_Magic_Systems/Casting_Mechanics_System.md** - Casting process
3. **SRD/03_Magic_Systems/Spell_Effects_System.md** - Spell effects
4. **SRD/03_Magic_Systems/Buff_Debuff_System.md** - Buffs and debuffs
5. **SRD/03_Magic_Systems/Spell_Resistance_Immunity.md** - Magic resistance

## Priority 4: Item Systems (Important for all players)
Systems that govern equipment and items:

1. **SRD/04_Item_Systems/Item_Mechanics.md** - Basic item mechanics
2. **SRD/04_Item_Systems/Equipment_Slot_System.md** - Equipment slots
3. **SRD/04_Item_Systems/Inventory_System.md** - Inventory management
4. **SRD/04_Item_Systems/Artifact_System.md** - Artifact mechanics
5. **SRD/04_Item_Systems/Durability_Repair_System.md** - Item maintenance

## Priority 5: Social Systems (Important for community)
Systems that facilitate player interaction:

1. **SRD/05_Social_Systems/Guild_System.md** - Guild mechanics
2. **SRD/05_Social_Systems/Group_System.md** - Group mechanics
3. **SRD/05_Social_Systems/Chat_System.md** - Communication
4. **SRD/05_Social_Systems/Housing_System.md** - Player housing
5. **SRD/05_Social_Systems/Trade_System.md** - Trading between players

## Game Rule Summary Template

For each major section, add a summary like this:

```markdown
### Section Name

**Game Rule Summary**: [Explain the mechanic in simple terms that a player would understand. Focus on what it means for gameplay, why it matters, and how players experience it. Avoid technical jargon and programming concepts.]

[Existing technical content...]
```

## Example Game Rule Summaries

### For Combat Mechanics:
"When you attack someone, the game checks if they can dodge, parry, or block your attack in that specific order. Only one defense can work per attack, so you can't stack all your defenses together. Fighting multiple enemies makes your defenses less effective because you can't focus on everyone at once."

### For Character Progression:
"Your character gets stronger by gaining experience from killing monsters and completing quests. Early levels are quick but later levels take much longer. Starting at level 6, your character automatically gains points in their most important stats, so warriors get stronger while wizards get smarter."

### For Magic Systems:
"Spells have a chance to miss just like weapon attacks, and enemies can resist magical damage just like they resist physical damage. The more skilled you are with magic, the more reliable your spells become. Different types of magic resistance protect against different spell types."

## Implementation Strategy

1. **Start with Priority 1** - These affect every player every day
2. **Focus on major sections** - Add summaries to main mechanics, not every minor detail
3. **Use simple language** - Write for someone learning the game, not a programmer
4. **Keep it concise** - 1-3 sentences that capture the essence
5. **Focus on player impact** - What does this mean for someone playing the game?

## Success Criteria

- Players can understand game mechanics without technical background
- Game designers can quickly grasp system purposes
- New developers can understand game rules before diving into code
- Documentation serves both technical and design audiences

## Notes

- Don't change existing technical content
- Game rule summaries are additions, not replacements
- Focus on the "why" and "what" rather than the "how"
- Prioritize player-facing mechanics over internal systems 