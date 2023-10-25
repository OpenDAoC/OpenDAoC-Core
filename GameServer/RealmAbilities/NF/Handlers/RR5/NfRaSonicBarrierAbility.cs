using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Scripts;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.RealmAbilities;

public class NfRaSonicBarrierAbility : Rr5RealmAbility
{
	public NfRaSonicBarrierAbility(DbAbility dba, int level) : base(dba, level) { }

	/// <summary>
	/// Action
	/// </summary>
	/// <param></param>
	public override void Execute(GameLiving living)
	{
		if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

		GamePlayer player = living as GamePlayer;
        if (player.Group != null)
        {
            foreach (GamePlayer member in player.Group.GetPlayersInTheGroup())
            {
                if (member.PlayerClass.ID == 1 || member.PlayerClass.ID == 2) // Plate
                {
                    Spell Spell1 = SkillBase.GetSpellByID(36005); // 34 % Absorb-Spell
                    ISpellHandler spellhandler1 = ScriptMgr.CreateSpellHandler(player, Spell1, SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
                    spellhandler1.StartSpell(member);
                }
                else if (member.PlayerClass.ID == 9 || member.PlayerClass.ID == 10 || member.PlayerClass.ID == 23 || member.PlayerClass.ID == 49 || member.PlayerClass.ID == 58)    // Leather
                {
                    Spell Spell2 = SkillBase.GetSpellByID(36002); // 10 % Absorb-Spell
                    ISpellHandler spellhandler2 = ScriptMgr.CreateSpellHandler(player, Spell2, SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
                    spellhandler2.StartSpell(member);
                }
                else if (member.PlayerClass.ID == 3 || member.PlayerClass.ID == 25 || member.PlayerClass.ID == 31 || member.PlayerClass.ID == 32 || member.PlayerClass.ID == 43 || member.PlayerClass.ID == 48 || member.PlayerClass.ID == 50 || member.PlayerClass.ID == 25) // Studderd
                {
                    Spell Spell3 = SkillBase.GetSpellByID(36003); // 19 % Absorb-Spell
                    ISpellHandler spellhandler3 = ScriptMgr.CreateSpellHandler(player, Spell3, SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
                    spellhandler3.StartSpell(member);
                }
                else if (member.PlayerClass.ID == 4 || member.PlayerClass.ID == 6 || member.PlayerClass.ID == 11 || member.PlayerClass.ID == 19 || member.PlayerClass.ID == 21 || member.PlayerClass.ID == 22 || member.PlayerClass.ID == 24 || member.PlayerClass.ID == 26 || member.PlayerClass.ID == 28 || member.PlayerClass.ID == 34 || member.PlayerClass.ID == 44 || member.PlayerClass.ID == 45 || member.PlayerClass.ID == 46 || member.PlayerClass.ID == 47) // Chain 
                {
                    Spell Spell4 = SkillBase.GetSpellByID(36004);// 24 % Absorb-Spell			
                    ISpellHandler spellhandler4 = ScriptMgr.CreateSpellHandler(player, Spell4, SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
                    spellhandler4.StartSpell(member);
                }
                else
                {
                    Spell Spell5 = SkillBase.GetSpellByID(36001); // 0 % Absorb-Spell
                    ISpellHandler spellhandler5 = ScriptMgr.CreateSpellHandler(player, Spell5, SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
                    spellhandler5.StartSpell(member);
                }
            }
        }
        else
        {
            player.Out.SendMessage("You need a group for this Ability!", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
            return;
        }
		DisableSkill(living);
	}

	public override int GetReUseDelay(int level)
	{
		return 420;
	}

	public override void AddEffectsInfo(IList<string> list)
	{
		list.Add("Sonic Barrier.");
		list.Add("");
		list.Add("Target: Group");
		list.Add("Duration: 45 sec");
		list.Add("Casting time: instant");
	}

}