using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using System;
using DOL.GS;
using DOL.Database;

namespace DOL.GS
{
	public class UnnaturalStorm : GameNPC
	{
		public UnnaturalStorm() : base() { }
        #region Stats
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		#endregion
		public override bool AddToWorld()
		{
			Name = "Unnatural Storm";
			Model = 665;
			Level = (byte)Util.Random(65, 70);
			Size = 100;
			MeleeDamageType = eDamageType.Crush;
			Race = 2003;
			Flags = (GameNPC.eFlags)44;//notarget noname flying
			MaxSpeedBase = 0;
			RespawnInterval = -1;
			SpawnAdditionalStorms();

			UnnaturalStormBrain sbrain = new UnnaturalStormBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			bool success = base.AddToWorld();
			if (success)
			{
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
			}
			return success;
		}
		#region Effects
		protected int Show_Effect(ECSGameTimer timer)
		{
			if (IsAlive)
			{
				foreach (GamePlayer player in GetPlayersInRadius(10000))
				{
					if (player != null)
					{
						player.Out.SendSpellCastAnimation(this, 14323, 1);
						player.Out.SendSpellEffectAnimation(this, this, 3508, 0, false, 0x01);
					}
				}
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoCast), 1500);
			}
			return 0;
		}
		protected int DoCast(ECSGameTimer timer)
		{
			if (IsAlive)
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 1500);
			return 0;
		}
		#endregion
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
			}
		}
        public override void StartAttack(GameObject target)
        {
        }
		private void SpawnAdditionalStorms()
        {
			foreach (GameClient client in WorldMgr.GetClientsOfZone(CurrentZone.ID))
			{
				if (client == null) break;
				if (client.Player == null) continue;
				if (client.IsPlaying)
				{
					client.Out.SendMessage("An intense supernatural storm explodes in the sky over the northeastern expanse of Lyonesse!", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
				}
			}
			for (int i = 0; i < Util.Random(4, 5); i++)
			{
				UnnaturalStormAdds Add = new UnnaturalStormAdds();
				Add.X = X + Util.Random(-1000, 1000);
				Add.Y = Y + Util.Random(-1000, 800);
				Add.Z = Z + Util.Random(-400, 400);
				Add.CurrentRegion = CurrentRegion;
				Add.Heading = Heading;
				Add.AddToWorld();
			}
		}
    }
}
namespace DOL.AI.Brain
{
	public class UnnaturalStormBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public UnnaturalStormBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 2500;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if(HasAggro && Body.TargetObject != null)
            {
				if (!Body.IsCasting)
					Body.CastSpell(StormDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			}
			base.Think();
		}
		#region Spell
		private Spell m_StormDD;
		private Spell StormDD
		{
			get
			{
				if (m_StormDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 3;
					spell.Power = 0;
					spell.ClientEffect = 3508;
					spell.Icon = 3508;
					spell.Damage = 200;
					spell.DamageType = (int)eDamageType.Energy;
					spell.Name = "Storm Lightning";
					spell.Range = 2500;
					spell.SpellID = 11947;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_StormDD = new Spell(spell, 50);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_StormDD);
				}
				return m_StormDD;
			}
		}
		#endregion
	}
}

#region Additional Storm effect mobs
namespace DOL.GS
{
	public class UnnaturalStormAdds : GameNPC
	{
		public UnnaturalStormAdds() : base() { }
		public override bool AddToWorld()
		{
			Name = "Unnatural Storm";
			Model = 665;
			Level = (byte)Util.Random(40, 42);
			Size = 100;
			MeleeDamageType = eDamageType.Crush;
			Race = 2003;
			Flags = (GameNPC.eFlags)60;//notarget noname flying
			MaxSpeedBase = 0;
			RespawnInterval = -1;

			UnnaturalStormAddsBrain sbrain = new UnnaturalStormAddsBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			bool success = base.AddToWorld();
			if (success)
			{
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
			}
			return success;
		}
		#region Effects
		private protected int Show_Effect(ECSGameTimer timer)
		{
			if (IsAlive)
			{
				foreach (GamePlayer player in GetPlayersInRadius(6000))
				{
					if (player != null)
					{
						player.Out.SendSpellCastAnimation(this, 14323, 1);
						player.Out.SendSpellEffectAnimation(this, this, 3508, 0, false, 0x01);
					}
				}
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoCast), 1500);
			}
			return 0;
		}
		private protected int DoCast(ECSGameTimer timer)
		{
			if (IsAlive)
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 1500);
			return 0;
		}
		#endregion
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
			}
		}
		public override void StartAttack(GameObject target)
		{
		}
	}
}
namespace DOL.AI.Brain
{
	public class UnnaturalStormAddsBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public UnnaturalStormAddsBrain() : base()
		{
			AggroLevel = 0;
			AggroRange = 0;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion

#region Unnatural Storm Controller - controll when storm will appear
namespace DOL.GS
{
	public class UnnaturalStormController : GameNPC
	{
		public UnnaturalStormController() : base()
		{
		}
		public override bool IsVisibleToPlayers => true;
		public override bool AddToWorld()
		{
			Name = "Unnatural Storm Controller";
			GuildName = "DO NOT REMOVE";
			Level = 50;
			Model = 665;
			RespawnInterval = 5000;
			Flags = (GameNPC.eFlags)60;

			UnnaturalStormControllerBrain sbrain = new UnnaturalStormControllerBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}

namespace DOL.AI.Brain
{
	public class UnnaturalStormControllerBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public UnnaturalStormControllerBrain()
			: base()
		{
			AggroLevel = 0; //neutral
			AggroRange = 0;
			ThinkInterval = 1000;
		}
		public override void Think()
		{
			uint hour = WorldMgr.GetCurrentGameTime() / 1000 / 60 / 60;
			uint minute = WorldMgr.GetCurrentGameTime() / 1000 / 60 % 60;
			//log.Warn("Current time: " + hour + ":" + minute);
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is UnnaturalStormBrain brain)
				{
					if (!brain.HasAggro && hour >= 7 && hour < 18)
					{
						npc.RemoveFromWorld();

						foreach (GameNPC adds in Body.GetNPCsInRadius(8000))
						{
							if (adds != null && adds.IsAlive && adds.Brain is UnnaturalStormAddsBrain)
								adds.RemoveFromWorld();
						}
					}
				}
			}
			if (hour == 18 && minute == 30)
				SpawnUnnaturalStorm();

			base.Think();
		}
		public void SpawnUnnaturalStorm()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is UnnaturalStormBrain)
					return;
			}
			UnnaturalStorm boss = new UnnaturalStorm();
			boss.X = Body.X;
			boss.Y = Body.Y;
			boss.Z = Body.Z;
			boss.Heading = Body.Heading;
			boss.CurrentRegion = Body.CurrentRegion;
			boss.AddToWorld();
		}
	}
}
#endregion