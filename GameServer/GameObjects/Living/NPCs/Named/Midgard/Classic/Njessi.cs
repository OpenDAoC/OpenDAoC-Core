using Core.AI.Brain;
using Core.Database;

namespace Core.GS;

public class Njessi : GameNpc
{
	public Njessi() : base() { }

    public override bool IsVisibleToPlayers => true; //mob brain will work if there are 0 players around

    public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164504);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

        NjessiBrain sbrain = new NjessiBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    public override void ReturnToSpawnPoint(short speed)
    {
        return;
    }
    public override void OnAttackEnemy(AttackData ad) //on enemy actions
    {
        if (Util.Chance(10) && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
        {
            if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
                CastSpell(NjessiPoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        if (Util.Chance(30))
        {
            if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
                CastSpell(NjessiDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        base.OnAttackEnemy(ad);
    }
    #region Spells
    private Spell m_NjessiDD;
    public Spell NjessiDD
    {
        get
        {
            if (m_NjessiDD == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.Power = 0;
                spell.RecastDelay = 10;
                spell.ClientEffect = 5700;
                spell.Icon = 5700;
                spell.Damage = 100;
                spell.DamageType = (int)EDamageType.Heat;
                spell.Name = "Flame Breath";
                spell.Range = 500;
                spell.Radius = 300;
                spell.SpellID = 11933;
                spell.Target = "Enemy";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                m_NjessiDD = new Spell(spell, 20);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_NjessiDD);
            }
            return m_NjessiDD;
        }
    }
    private Spell m_NjessiPoison;
    public Spell NjessiPoison
    {
        get
        {
            if (m_NjessiPoison == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 10;
                spell.ClientEffect = 4099;
                spell.Icon = 4099;
                spell.TooltipId = 4099;
                spell.Name = "Njessi Venom";
                spell.Description = "Inflicts 25 damage to the target every 3 sec for 20 seconds";
                spell.Message1 = "You are afflicted with a vicious poison!";
                spell.Message2 = "{0} has been poisoned!";
                spell.Message3 = "The poison has run its course.";
                spell.Message4 = "{0} looks healthy again.";
                spell.Damage = 25;
                spell.Duration = 20;
                spell.Frequency = 30;
                spell.Range = 500;
                spell.SpellID = 11934;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.DamageOverTime.ToString();
                spell.DamageType = (int)EDamageType.Body;
                spell.Uninterruptible = true;
                m_NjessiPoison = new Spell(spell, 20);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_NjessiPoison);
            }
            return m_NjessiPoison;
        }
    }
    #endregion
}