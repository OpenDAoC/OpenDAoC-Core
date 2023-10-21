using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

public class RuckusBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public RuckusBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 400;
		ThinkInterval = 1500;
	}
	private bool PrepareStun = false;

	public override void Think()
	{
		if(HasAggro && Body.TargetObject != null)
        {
			GameLiving target = Body.TargetObject as GameLiving;
			if (Util.Chance(25) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity) 
				&& !target.effectListComponent.ContainsEffectForEffectType(EEffect.Stun) && target.IsAlive && target != null && !PrepareStun)
            {
				foreach(GamePlayer player in Body.GetPlayersInRadius(1500))
                {
					if (player != null)
						player.Out.SendMessage("Ruckus begins saving energy for a stunning blow.\nRuckus attacks begin to stun his opponent with next blow.", EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
                }
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastStun), 2000);
				PrepareStun = true;
            }
			if (!Body.effectListComponent.ContainsEffectForEffectType(EEffect.DamageAdd) && !Body.IsCasting)
				Body.CastSpell(RuckusDA, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.Think();
	}
	private int CastStun(EcsGameTimer timer)
    {
		if (HasAggro && Body.TargetObject != null)		
			Body.CastSpell(Ruckus_stun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetStun), 20000);
		return 0;
    }
	private int ResetStun(EcsGameTimer timer)
	{
		PrepareStun = false;
		return 0;
	}
	#region Spells
	private Spell m_RuckusDA;
	private Spell RuckusDA
	{
		get
		{
			if (m_RuckusDA == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.Power = 0;
				spell.RecastDelay = 10;
				spell.ClientEffect = 18;
				spell.Icon = 18;
				spell.Damage = 10;
				spell.Duration = 10;
				spell.DamageType = (int)EDamageType.Matter;
				spell.Name = "Earthen Fury";
				spell.Range = 1000;
				spell.SpellID = 11942;
				spell.Target = "Self";
				spell.Type = ESpellType.DamageAdd.ToString();
				spell.Uninterruptible = true;
				m_RuckusDA = new Spell(spell, 20);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_RuckusDA);
			}
			return m_RuckusDA;
		}
	}
	private Spell m_Ruckus_stun;
	private Spell Ruckus_stun
	{
		get
		{
			if (m_Ruckus_stun == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 2;
				spell.ClientEffect = 2165;
				spell.Icon = 2132;
				spell.TooltipId = 2132;
				spell.Duration = 4;
				spell.Description = "Target is stunned and cannot move or take any other action for the duration of the spell.";
				spell.Name = "Stun";
				spell.Range = 400;
				spell.SpellID = 11943;
				spell.Target = "Enemy";
				spell.Type = ESpellType.Stun.ToString();
				m_Ruckus_stun = new Spell(spell, 20);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Ruckus_stun);
			}
			return m_Ruckus_stun;
		}
	}
    #endregion
}