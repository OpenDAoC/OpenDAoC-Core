using Core.Database.Tables;

namespace Core.GS.AI.Brains;

public class VasremBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public VasremBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 800;
		ThinkInterval = 1500;
	}

	public override void Think()
	{
		if(Body.IsAlive)
        {
			if (!Body.Spells.Contains(Vasrem_Lifetap))
				Body.Spells.Add(Vasrem_Lifetap);
			if (!Body.Spells.Contains(VasremDebuffDQ))
				Body.Spells.Add(VasremDebuffDQ);
			if (!Body.Spells.Contains(VasremSCDebuff))
				Body.Spells.Add(VasremSCDebuff);
		}
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
		}
		if(HasAggro && Body.TargetObject != null)
        {
			if (!Body.IsCasting && !Body.IsMoving)
			{
				foreach (Spell spells in Body.Spells)
				{
					if (spells != null)
					{
						if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject, spells.Range))
							Body.StopFollowing();
						else
							Body.Follow(Body.TargetObject, spells.Range - 50, 5000);

						Body.TurnTo(Body.TargetObject);
						if (Util.Chance(100))
						{
							GameLiving target = Body.TargetObject as GameLiving;
							if (target != null)
							{
								if (!Body.IsCasting && Body.GetSkillDisabledDuration(VasremSCDebuff) == 0 && !target.effectListComponent.ContainsEffectForEffectType(EEffect.StrConDebuff))
									Body.CastSpell(VasremSCDebuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
								else if (!Body.IsCasting && Body.GetSkillDisabledDuration(VasremDebuffDQ) == 0 && !target.effectListComponent.ContainsEffectForEffectType(EEffect.DexQuiDebuff))
									Body.CastSpell(VasremDebuffDQ, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
								else
									Body.CastSpell(Vasrem_Lifetap, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
							}
						}
					}
				}
			}
		}
		base.Think();
	}
    #region Spells
    private Spell m_Vasrem_Lifetap;
	private Spell Vasrem_Lifetap
	{
		get
		{
			if (m_Vasrem_Lifetap == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 4;
				spell.RecastDelay = 0;
				spell.ClientEffect = 9191;
				spell.Icon = 710;
				spell.Damage = 450;
				spell.Name = "Drain Life Essence";
				spell.Range = 1800;
				spell.SpellID = 11886;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.MoveCast = true;
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Body; //Body DMG Type
				m_Vasrem_Lifetap = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Vasrem_Lifetap);
			}
			return m_Vasrem_Lifetap;
		}
	}
	private Spell m_VasremSCDebuff;
	private Spell VasremSCDebuff
	{
		get
		{
			if (m_VasremSCDebuff == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 60;
				spell.ClientEffect = 5408;
				spell.Icon = 5408;
				spell.Name = "S/C Debuff";
				spell.TooltipId = 5408;
				spell.Range = 1200;
				spell.Value = 65;
				spell.Duration = 60;
				spell.SpellID = 11887;
				spell.Target = "Enemy";
				spell.Type = "StrengthConstitutionDebuff";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_VasremSCDebuff = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_VasremSCDebuff);
			}
			return m_VasremSCDebuff;
		}
	}
	private Spell m_VasremDebuffDQ;
	private Spell VasremDebuffDQ
	{
		get
		{
			if (m_VasremDebuffDQ == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 60;
				spell.Duration = 60;
				spell.Value = 65;
				spell.ClientEffect = 2627;
				spell.Icon = 2627;
				spell.TooltipId = 2627;
				spell.Name = "D/Q Debuff";
				spell.Range = 1500;
				spell.Radius = 350;
				spell.SpellID = 11888;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DexterityQuicknessDebuff.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_VasremDebuffDQ = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_VasremDebuffDQ);
			}
			return m_VasremDebuffDQ;
		}
	}
	#endregion
}