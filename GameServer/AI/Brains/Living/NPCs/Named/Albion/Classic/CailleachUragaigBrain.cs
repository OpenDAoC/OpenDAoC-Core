using System;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.AI.Brain;

#region Cailleach Uragaig
public class CailleachUragaigBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public CailleachUragaigBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 500;
		ThinkInterval = 1500;
	}
	bool AggroMessage = false;
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(5000))
		{
			player.Out.SendMessage(message, EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}
	}
	private bool SpawnAdds = false;
	private bool RemoveAdds = false;
	private bool TorchOfLight_Enabled = false;
	public override void Think()
	{
		if(Body.IsAlive)
        {
			if (!Body.Spells.Contains(CailleachUragaigDD))
				Body.Spells.Add(CailleachUragaigDD);
        }
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			AggroMessage = false;
			TorchOfLight_Enabled = false;
			SpawnAdds = false;

			if (!RemoveAdds)
            {
				foreach(GameNpc npc in Body.GetNPCsInRadius(8000))
                {
					if (npc != null && npc.IsAlive && npc.Brain is TorchOfLightBrain)
						npc.RemoveFromWorld();
                }
				RemoveAdds = true;
            }
		}
		if (HasAggro && Body.TargetObject != null)
		{
			RemoveAdds = false;
			if(!SpawnAdds)
            {
				SpawnTorchOfLight();
				GameLiving target = Body.TargetObject as GameLiving;
				foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is TorchOfLightBrain brain)
                    {
						if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
							brain.AddToAggroList(target, 100);
                    }
				}
				SpawnAdds = true;
            }
			if(!TorchOfLight_Enabled)
            {
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetLights), 5000);
				TorchOfLight_Enabled = true;
            }
			if(!AggroMessage)
            {
				BroadcastMessage(String.Format("{0} says, \"Father! Lend me your Torch of Light so we may be delivered from these aggressors!\"",Body.Name));
				AggroMessage = true;
            }
			if (Body.HealthPercent <= 30)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null && npc.IsAlive && npc.PackageID == "CailleachUragaigBaf")
						AddAggroListTo(npc.Brain as StandardMobBrain);
				}
			}
			if(!Body.IsCasting && Util.Chance(20))
				Body.CastSpell(CailleachUragaigDD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.Think();
	}
	#region Spells
	private Spell m_CailleachUragaigDD;
	private Spell CailleachUragaigDD
	{
		get
		{
			if (m_CailleachUragaigDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 4;
				spell.RecastDelay = 0;
				spell.ClientEffect = 378;
				spell.Icon = 378;
				spell.Damage = 200;
				spell.DamageType = (int)EDamageType.Heat;
				spell.Name = "Flame Spear";
				spell.Range = 1800;
				spell.SpellID = 11983;
				spell.Target = "Enemy";
				spell.Type = ESpellType.Bolt.ToString();
				m_CailleachUragaigDD = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CailleachUragaigDD);
			}
			return m_CailleachUragaigDD;
		}
	}
	private Spell m_CailleachUragaigDD2;
	private Spell CailleachUragaigDD2
	{
		get
		{
			if (m_CailleachUragaigDD2 == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 4;
				spell.RecastDelay = Util.Random(10,20);
				spell.ClientEffect = 378;
				spell.Icon = 378;
				spell.Damage = 200;
				spell.DamageType = (int)EDamageType.Heat;
				spell.Name = "Flame Spear";
				spell.Range = 1800;
				spell.SpellID = 11983;
				spell.Target = "Enemy";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.Type = ESpellType.Bolt.ToString();
				m_CailleachUragaigDD2 = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CailleachUragaigDD2);
			}
			return m_CailleachUragaigDD2;
		}
	}
	#endregion
	private int ResetLights(EcsGameTimer timer)
    {
		foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
		{
			if (npc != null && npc.IsAlive && npc.Brain is TorchOfLightBrain)
				npc.RemoveFromWorld();
		}
		return 0;
    }
	#region Spawn Torch of Light
	private void SpawnTorchOfLight()
	{
		TorchOfLight npc = new TorchOfLight();
		npc.X = 316833;
		npc.Y = 664746;
		npc.Z = 3146;
		npc.Heading = 1238;
		npc.CurrentRegion = Body.CurrentRegion;
		npc.AddToWorld();

		TorchOfLight npc2 = new TorchOfLight();
		npc2.X = 316715;
		npc2.Y = 664155;
		npc2.Z = 3141;
		npc2.Heading = 1238;
		npc2.CurrentRegion = Body.CurrentRegion;
		npc2.AddToWorld();

		TorchOfLight npc3 = new TorchOfLight();
		npc3.X = 315842;
		npc3.Y = 663685;
		npc3.Z = 3356;
		npc3.Heading = 4038;
		npc3.CurrentRegion = Body.CurrentRegion;
		npc3.AddToWorld();

		TorchOfLight npc4 = new TorchOfLight();
		npc4.X = 315857;
		npc4.Y = 665332;
		npc4.Z = 3405;
		npc4.Heading = 2043;
		npc4.CurrentRegion = Body.CurrentRegion;
		npc4.AddToWorld();
	}
	#endregion
}
#endregion Cailleach Uragaig

#region Torch of Light
public class TorchOfLightBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public TorchOfLightBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 100;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		if(HasAggro && Body.TargetObject != null)
			Body.CastSpell(Torch_Of_Light_Bolt, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
		base.Think();
	}
	private Spell m_Torch_Of_Light_Bolt;
	private Spell Torch_Of_Light_Bolt
	{
		get
		{
			if (m_Torch_Of_Light_Bolt == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 20;
				spell.ClientEffect = 378;
				spell.Icon = 378;
				spell.Damage = 150;
				spell.DamageType = (int)EDamageType.Heat;
				spell.Name = "Flame Spear";
				spell.Range = 4000;
				spell.SpellID = 11895;
				spell.Target = "Enemy";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.Type = ESpellType.Bolt.ToString();
				m_Torch_Of_Light_Bolt = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Torch_Of_Light_Bolt);
			}
			return m_Torch_Of_Light_Bolt;
		}
	}
}
#endregion Torch of Light