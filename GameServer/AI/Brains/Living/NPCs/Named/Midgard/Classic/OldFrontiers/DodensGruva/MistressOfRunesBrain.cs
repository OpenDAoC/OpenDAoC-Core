using System;
using System.Collections;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Effects.Old;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.AI;

public class MistressOfRunesBrain : StandardMobBrain
{
	protected String[] m_SpearAnnounce;
	protected String m_NearsightAnnounce;

	//Re-Cast every 15 seconds.
	public const int SpearRecastInterval = 15;
	//Re-Cast every 60 seconds.
	public const int NearsightRecastInterval = 60;

	protected bool castsSpear = true;
	protected bool castsNearsight = true;

	public MistressOfRunesBrain() : base()
	{
		AggroLevel = 200;
		AggroRange = 500;

		m_SpearAnnounce = new String[] { "{0} casts a magical flaming spear on {1}!",
				"{0} drops a flaming spear from above!",
				"{0} uses all her might to create a flaming spear.",
				"{0} casts a dangerous spell!" };
		m_NearsightAnnounce = "{0} can no longer see properly and everyone in the vicinity!";
	}

	/// <summary>
	/// Set Mistress of Runes difficulty in percent of its max abilities
	/// 100 = full strength
	/// </summary>
	public virtual int MistressDifficulty
	{
		get { return ServerProperty.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS; }
	}

	public override void Think()
	{
		if (Body.InCombat && Body.IsAlive && HasAggro)
		{
			if (Body.TargetObject != null)
			{
				foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
				{
					if (player == null || !player.IsAlive || !player.IsVisibleTo(Body))
						return;

					//cast nearsight
					CheckNearsight(player);

					//cast AoE Spears
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(timer => CastSpear(timer, player)), 4000);
				}
			}
		}
		base.Think();
	}

	protected override void CheckNPCAggro()
	{
		if (Body.attackComponent.AttackState)
			return;

		foreach (GameNpc npc in Body.GetNPCsInRadius((ushort)AggroRange))
		{
			if (!npc.IsAlive || npc.ObjectState != GameObject.eObjectState.Active)
				continue;

			if (!GameServer.ServerRules.IsAllowedToAttack(Body, npc, true))
				continue;

			if (AggroTable.ContainsKey(npc))
				continue; // add only new NPCs

			if (npc.Brain != null && npc.Brain is IControlledBrain)
			{
				if (CanAggroTarget(npc))
				{
					AddToAggroList(npc, (npc.Level + 1) << 1);
				}
			}
		}
	}

	/// <summary>
	/// Broadcast relevant messages to the raid.
	/// </summary>
	/// <param name="message">The message to be broadcast.</param>
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
		}
	}

	/// <summary>
	/// Try to find a potential target for Nearsight.
	/// </summary>
	/// <returns>Whether or not a target was picked.</returns>
	public bool PickNearsightTarget()
	{
		MistressOfRunes mistress = Body as MistressOfRunes;
		if (mistress == null) return false;

		ArrayList inRangeLiving = new ArrayList();

		lock ((AggroTable as ICollection).SyncRoot)
		{
			Dictionary<GameLiving, long>.Enumerator enumerator = AggroTable.GetEnumerator();
			while (enumerator.MoveNext())
			{
				GameLiving living = enumerator.Current.Key;
				if (living != null &&
					living.IsAlive &&
					living.EffectList.GetOfType<NecromancerShadeEffect>() == null &&
					!mistress.IsWithinRadius(living, mistress.AttackRange))
				{
					inRangeLiving.Add(living);
				}
			}
		}

		if (inRangeLiving.Count > 0)
		{
			return CheckNearsight((GameLiving)(inRangeLiving[Util.Random(1, inRangeLiving.Count) - 1]));
		}

		return false;
	}

	/// <summary>
	/// Called whenever the Thane Dyggve's body sends something to its brain.
	/// </summary>
	/// <param name="e">The event that occured.</param>
	/// <param name="sender">The source of the event.</param>
	/// <param name="args">The event details.</param>
	public override void Notify(CoreEvent e, object sender, EventArgs args)
	{
		base.Notify(e, sender, args);
	}
	/// <summary>
	/// Cast Spears on the Target
	/// </summary>
	/// <param name="timer">The timer that started this cast.</param>
	/// <returns></returns>
	private int CastSpear(EcsGameTimer timer, GameLiving target)
	{
		if (target == null || !target.IsAlive)
			return 0;

		bool cast = Body.CastSpell(AoESpear, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

		if (Body.GetSkillDisabledDuration(AoESpear) > 0)
		{
			cast = false;
		}
		if (castsSpear && cast && Body.IsCasting)
		{
			castsSpear = false;
			int messageNo = Util.Random(1, m_SpearAnnounce.Length) - 1;
			BroadcastMessage(String.Format(m_SpearAnnounce[messageNo], Body.Name, target.Name));
		}
		else
		{
			castsSpear = true;
		}
		return 0;
	}

	#region Nearsight Method
	private const int m_NearsightChance = 100;

	/// <summary>
	/// Chance to cast Nearsight when a potential target has been detected.
	/// </summary>
	protected int NearsightChance
	{
		get { return m_NearsightChance; }
	}

	private GameLiving m_NearsightTarget;

	/// <summary>
	/// The target for the next Nearsight attack.
	/// </summary>
	private GameLiving NearsightTarget
	{
		get { return m_NearsightTarget; }
		set { m_NearsightTarget = value; PrepareToNearsight(); }
	}

	/// <summary>
	/// Check whether or not to Nearsight at this target.
	/// </summary>
	/// <param name="target">The potential target.</param>
	/// <returns>Whether or not the spell was cast.</returns>
	public bool CheckNearsight(GameLiving target)
	{
		if (target == null || NearsightTarget != null) return false;
		bool success = Util.Chance(NearsightChance);
		if (success)
			NearsightTarget = target;
		return success;
	}

	/// <summary>
	/// Announce the Nearsight and start the 2 second timer.
	/// </summary>
	private void PrepareToNearsight()
	{
		if (NearsightTarget == null) return;
		Body.TurnTo(NearsightTarget);

		new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastNearsight), 2000);
	}

	/// <summary>
	/// Cast Nearsight on the target.
	/// </summary>
	/// <param name="timer">The timer that started this cast.</param>
	/// <returns></returns>
	private int CastNearsight(EcsGameTimer timer)
	{
		// Turn around to the target and cast Nearsight, then go back to the original
		// target, if one exists.

		GameObject oldTarget = Body.TargetObject;
		Body.TargetObject = NearsightTarget;
		Body.Z = Body.SpawnPoint.Z; // this is a fix to correct Z errors that sometimes happen during Mistress fights
		Body.TurnTo(NearsightTarget);
		bool cast = Body.CastSpell(Nearsight, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		if (Body.GetSkillDisabledDuration(Nearsight) > 0)
		{
			cast = false;
		}
		if (castsNearsight && cast && Body.IsCasting)
		{
			castsNearsight = false;
			BroadcastMessage(String.Format(m_NearsightAnnounce, NearsightTarget.Name));
		}
		else
		{
			castsNearsight = true;
		}
		NearsightTarget = null;
		if (oldTarget != null) Body.TargetObject = oldTarget;
		return 0;
	}

	#endregion

	#region Runemaster AoE Spear
	private Spell m_AoESpell;
	/// <summary>
	/// The AoE spell.
	/// </summary>
	protected Spell AoESpear
	{
		get
		{
			if (m_AoESpell == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.Uninterruptible = true;
				spell.CastTime = 3;
				spell.ClientEffect = 2958;
				spell.Icon = 2958;
				spell.Damage = 550;
				spell.Name = "Odin's Hatred";
				spell.Range = 1500;
				spell.Radius = 450;
				spell.SpellID = 2958;
				spell.RecastDelay = SpearRecastInterval;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.MoveCast = false;
				spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
				m_AoESpell = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AoESpell);
			}
			return m_AoESpell;
		}
	}

	#endregion

	#region Runemaster Nearsight
	private Spell m_NearsightSpell;
	/// <summary>
	/// The Nearsight spell.
	/// </summary>
	protected Spell Nearsight
	{
		get
		{
			if (m_NearsightSpell == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.Uninterruptible = true;
				spell.CastTime = 1;
				spell.ClientEffect = 2735;
				spell.Icon = 2735;
				spell.Description = "Nearsight";
				spell.Name = "Diminish Vision";
				spell.Range = 1500;
				spell.Radius = 1500;
				spell.RecastDelay = NearsightRecastInterval;
				spell.Value = 65;
				spell.Duration = 45 * MistressDifficulty / 100;
				spell.Damage = 0;
				spell.DamageType = (int)EDamageType.Energy;
				spell.SpellID = 2735;
				spell.Target = "Enemy";
				spell.Type = ESpellType.Nearsight.ToString();
				spell.Message1 = "You are blinded!";
				spell.Message2 = "{0} is blinded!";
				m_NearsightSpell = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_NearsightSpell);
			}
			return m_NearsightSpell;
		}
	}
	#endregion
}