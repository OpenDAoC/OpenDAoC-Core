#!/usr/bin/env python3
"""
Script to add Game Rule Summary sections to SRD documents.
This helps explain technical mechanics in layman's terms.
"""

import os
import re
import glob
from pathlib import Path

def add_game_rule_summary(file_path, section_pattern, summary_text):
    """Add a game rule summary after a section header."""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Look for the pattern and add summary after it
    pattern = re.compile(section_pattern, re.MULTILINE)
    match = pattern.search(content)
    
    if match:
        insert_pos = match.end()
        # Insert the game rule summary
        new_content = (content[:insert_pos] + 
                      f"\n\n**Game Rule Summary**: {summary_text}\n" + 
                      content[insert_pos:])
        
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
        return True
    return False

def process_damage_calculation():
    """Add summaries to Damage_Calculation.md"""
    file_path = "SRD/01_Combat_Systems/Damage_Calculation.md"
    
    summaries = [
        {
            'pattern': r'### 1\. Base Damage Calculation\n\n',
            'summary': 'When you hit someone, the damage starts with your weapon\'s DPS (damage per second) multiplied by how fast it swings. Slower weapons hit harder per strike to balance out their speed. Your character\'s stats like Strength (for melee) or Dexterity (for archery) add extra damage on top of this.'
        },
        {
            'pattern': r'### 2\. Damage Modifiers\n\n',
            'summary': 'Different fighting styles and weapon types give bonuses to damage. Two-handed weapons hit harder because they\'re bigger and harder to use. Fighting with two weapons gives a penalty to the off-hand weapon but lets you attack more often. Better crafted weapons (higher quality) and well-maintained ones (good condition) deal more damage.'
        },
        {
            'pattern': r'### 3\. Damage Cap\n\n',
            'summary': 'There\'s a maximum amount of damage any single hit can do, even with the best gear and perfect conditions. This prevents one-shot kills and keeps combat balanced. The cap is three times what your weapon would normally do at perfect quality and condition.'
        }
    ]
    
    for summary_info in summaries:
        add_game_rule_summary(file_path, summary_info['pattern'], summary_info['summary'])

def process_character_progression():
    """Add summaries to Character_Progression.md"""
    file_path = "SRD/02_Character_Systems/Character_Progression.md"
    
    summaries = [
        {
            'pattern': r'### Experience System\n\n',
            'summary': 'Experience is the currency of character growth. You need increasingly large amounts to reach each new level - early levels might take minutes, but the final levels require many hours of gameplay. Different activities give different amounts of experience, and some zones provide bonuses to help players catch up.'
        },
        {
            'pattern': r'### Stat Progression\n\n',
            'summary': 'Starting at level 6, your character automatically gets stronger in their class\'s favored attributes. Warriors get stronger (Strength) every level, moderately tougher (Constitution) every other level, and slightly more agile (Dexterity) every third level. This ensures your character naturally grows in the right direction for their role.'
        }
    ]
    
    for summary_info in summaries:
        add_game_rule_summary(file_path, summary_info['pattern'], summary_info['summary'])

def main():
    """Main function to process all SRD documents."""
    print("Adding Game Rule Summaries to SRD documents...")
    
    # Process key documents
    try:
        process_damage_calculation()
        print("✓ Processed Damage_Calculation.md")
    except Exception as e:
        print(f"✗ Error processing Damage_Calculation.md: {e}")
    
    try:
        process_character_progression()
        print("✓ Processed Character_Progression.md")
    except Exception as e:
        print(f"✗ Error processing Character_Progression.md: {e}")
    
    print("Game Rule Summary addition complete!")

if __name__ == "__main__":
    main() 