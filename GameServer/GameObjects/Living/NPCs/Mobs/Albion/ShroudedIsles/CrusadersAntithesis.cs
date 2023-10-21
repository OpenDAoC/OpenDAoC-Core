using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;

namespace Core.GS;

public class CrusadersAntithesis : GameEpicDungeonNPC
{
	public CrusadersAntithesis() : base()
	{
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(50041);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		CrusadersAntithesisBrain sbrain = new CrusadersAntithesisBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
	public override void OnAttackEnemy(AttackData ad) //on enemy actions
	{
		if (Util.Chance(35) && ad != null)
		{
			CastSpell(CrusaderDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.OnAttackEnemy(ad);
	}
	private Spell m_CrusaderDD;
	private Spell CrusaderDD
	{
		get
		{
			if (m_CrusaderDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.Power = 0;
				spell.RecastDelay = 3;
				spell.ClientEffect = 0;
				spell.Icon = 0;
				spell.Damage = Util.Random(350,450);
				spell.DamageType = (int)EDamageType.Slash;
				spell.Name = "Melee Swing";
				spell.Range = 400;
				spell.SpellID = 12016;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				m_CrusaderDD = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CrusaderDD);
			}
			return m_CrusaderDD;
		}
	}
}