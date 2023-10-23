using System.Collections;
using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players;
using Core.GS.RealmAbilities;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.Artifacts;

[SpellHandler("RealmLore")]
public class RealmLore : SpellHandler
{
	public override bool CheckBeginCast(GameLiving selectedTarget)
    {
		if(!base.CheckBeginCast(selectedTarget)) 
			return false;

		if(selectedTarget==null) 
			return false;

		if (selectedTarget is GameNpc)
		{
			MessageToCaster("This spell works only on players.", EChatType.CT_SpellResisted); return false;
		}

		if(selectedTarget as GamePlayer==null) 
			return false;

		if(!m_caster.IsWithinRadius(selectedTarget, Spell.Range))
		{
			MessageToCaster("Your target is too far away.", EChatType.CT_SpellResisted); return false;
		}

        return true;
    }
	public override void OnDirectEffect(GameLiving target)
	{
		GamePlayer player = target as GamePlayer;
		if(player == null) 
			return;

		var text = new List<string>();
		text.Add("Class: "+player.PlayerClass.Name);
		text.Add("Realmpoints: "+player.RealmPoints+" = "+string.Format("{0:#L#} {1}",player.RealmLevel+10,player.RealmRankTitle(player.Client.Account.Language)));
		text.Add("----------------------------------------------------");
		text.Add("Str: "+player.Strength+" Dex: "+player.Dexterity+" Con: "+player.Constitution);
		text.Add("Qui: "+player.Quickness+" Emp: "+player.Empathy+" Cha: "+player.Charisma);
		text.Add("Pie: "+player.Piety+" Int: "+player.Intelligence+" HP: "+player.MaxHealth);
		text.Add("----------------------------------------------------");
		IList<Specialization> specs = player.GetSpecList();
		foreach (object obj in specs)
			if (obj is Specialization)
				text.Add(((Specialization)obj).Name + ": " + ((Specialization)obj).Level.ToString());
		text.Add("----------------------------------------------------");
		IList abilities = player.GetAllAbilities();
		foreach(Ability ab in abilities)
			if(ab is RealmAbility && ab is Rr5RealmAbility == false)
				text.Add(((RealmAbility)ab).Name);

		(m_caster as GamePlayer).Out.SendCustomTextWindow("Realm Lore [ "+player.Name+" ]",text);
		(m_caster as GamePlayer).Out.SendMessage("Realm Lore [ "+player.Name+" ]\n"+text,EChatType.CT_System,EChatLoc.CL_SystemWindow);
	}
	public RealmLore(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
}