using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.AI.Brains;

namespace Core.GS;

public class Dooben : GameNpc
{
	public Dooben() : base() { }

	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12676);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		//RespawnInterval = Util.Random(3600000, 7200000);

		DoobenBrain sbrain = new DoobenBrain();
		if (NPCTemplate != null)
		{
			sbrain.AggroLevel = NPCTemplate.AggroLevel;
			sbrain.AggroRange = NPCTemplate.AggroRange;
		}
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
	
	public override void OnAttackEnemy(AttackData ad) //on enemy actions
	{
		if (Util.Chance(45))
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
				CastSpell(DoobenDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.OnAttackEnemy(ad);
	}
	private Spell m_DoobenDD;
	public Spell DoobenDD
	{
		get
		{
			if (m_DoobenDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.Power = 0;
				spell.RecastDelay = 2;
				spell.ClientEffect = 127;
				spell.Icon = 127;
				spell.Damage = 25;
				spell.DamageType = (int)EDamageType.Spirit;
				spell.Name = "Sand Strike";
				spell.Range = 350;
				spell.SpellID = 11988;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				m_DoobenDD = new Spell(spell, 10);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_DoobenDD);
			}
			return m_DoobenDD;
		}
	}
}