using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.RealmAbilities;

public class NfRaCalmingNotesAbility : Rr5RealmAbility
{
    public NfRaCalmingNotesAbility(DbAbility dba, int level) : base(dba, level) { }

    public override void Execute(GameLiving living)
    {
        if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
        
		SpellLine spline = SkillBase.GetSpellLine(GlobalSpellsLines.Character_Abilities);
 		Spell abSpell = SkillBase.GetSpellByID(7045);
				 
 		if (spline != null && abSpell != null)
		{        
            foreach (GameNpc enemy in living.GetNPCsInRadius(750))
            {
	            if (enemy.IsAlive && enemy.Brain!=null)
	            	if(enemy.Brain is IControlledBrain)
						living.CastSpell(abSpell, spline);
            }
 		}
        DisableSkill(living);
    }
    public override int GetReUseDelay(int level)
    {
        return 300;
    }
    public override void AddEffectsInfo(IList<string> list)
    {
        list.Add("Insta-cast spell that mesmerizes all enemy pets within 750 radius for 30 seconds.");
        list.Add("");
        list.Add("Radius: 700");
        list.Add("Target: Pet");
        list.Add("Duration: 30 sec");
        list.Add("Casting time: instant");
    }


}