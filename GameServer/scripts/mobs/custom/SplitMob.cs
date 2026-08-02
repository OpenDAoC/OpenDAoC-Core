using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
	public class SplitMob : GameNPC
	{
		private const byte SPLIT_HEALTH_PERCENT = 50;
		private const byte LEVELS_LOST_PER_SPLIT = 2;
		private const byte MIN_SPLIT_LEVEL = 45;
		private const byte SIZE_LOST_PER_SPLIT = 5;
		private const byte MIN_SIZE = 20;
		private const long BOUNTY_POINT_REWARD = 5000;
		private const ushort REWARD_RADIUS = 3000;
		private const string MINION_NAME = "Split's Minion";

		private readonly List<SplitMobMinion> _minions = new();

		private byte _spawnLevel;
		private byte _spawnSize;
		private bool _spawnStatsCaptured;
		private bool _splitExhaustedAnnounced;

		public override bool AddToWorld()
		{
			if (!base.AddToWorld())
				return false;

			if (!_spawnStatsCaptured)
			{
				_spawnLevel = Level;
				_spawnSize = Size;
				_spawnStatsCaptured = true;
			}

			return true;
		}

		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			base.TakeDamage(source, damageType, damageAmount, criticalAmount);

			if (!IsAlive)
				return;

			if (HealthPercent > SPLIT_HEALTH_PERCENT)
			{
				_splitExhaustedAnnounced = false;
				return;
			}

			if (Level >= MIN_SPLIT_LEVEL + LEVELS_LOST_PER_SPLIT)
				Split(source);
			else if (!_splitExhaustedAnnounced)
			{
				_splitExhaustedAnnounced = true;
				Message.MessageToArea(this, $"{Name} shudders, but holds together. It is too small to split any further.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
			}
		}

		public override void Die(GameObject killer)
		{
			RewardParticipants(killer);
			RemoveMinions();
			Level = _spawnLevel;
			Size = _spawnSize;
			base.Die(killer);
		}

		public void OnMinionDied(SplitMobMinion minion)
		{
			_minions.Remove(minion);
		}

		private void Split(GameObject source)
		{
			Level -= LEVELS_LOST_PER_SPLIT;
			Health = MaxHealth;
			Size = (byte) Math.Max(Size - SIZE_LOST_PER_SPLIT, MIN_SIZE);

			SplitMobMinion minion = new() { Owner = this };
			CopyTo(minion);

			if (!minion.AddToWorld())
				return;

			_minions.Add(minion);

			Message.MessageToArea(this, $"{Name} convulses and tears itself in two!", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);

			GameLiving target = source as GameLiving;

			if (target == null || !target.IsAlive)
				target = TargetObject as GameLiving;

			if (target == null)
				return;

			if (minion.Brain is StandardMobBrain minionBrain)
				minionBrain.AddToAggroList(target, 1);

			minion.StartAttack(target);
		}

		private void CopyTo(SplitMobMinion minion)
		{
			minion.X = X + 10;
			minion.Y = Y + 10;
			minion.Z = Z;
			minion.CurrentRegion = CurrentRegion;
			minion.Heading = Heading;
			minion.Name = MINION_NAME;
			minion.Level = Level;
			minion.Realm = Realm;
			minion.Model = Model;
			minion.Size = Size;
			minion.Flags = Flags;
			minion.MeleeDamageType = MeleeDamageType;
			minion.RoamingRange = RoamingRange;
			minion.RespawnInterval = -1;

			minion.Strength = Strength;
			minion.Constitution = Constitution;
			minion.Dexterity = Dexterity;
			minion.Quickness = Quickness;
			minion.Intelligence = Intelligence;
			minion.Empathy = Empathy;
			minion.Piety = Piety;
			minion.Charisma = Charisma;

			minion.MaxSpeedBase = MaxSpeedBase;
			minion.NPCTemplate = NPCTemplate;
			minion.Inventory = Inventory;
			minion.EquipmentTemplateID = EquipmentTemplateID;

			if (minion.Inventory != null)
				minion.SwitchWeapon(ActiveWeaponSlot);

			StandardMobBrain minionBrain = new();

			if (Brain is StandardMobBrain brain)
			{
				minionBrain.AggroLevel = brain.AggroLevel;
				minionBrain.AggroRange = brain.AggroRange;
			}

			minion.SetOwnBrain(minionBrain);
		}

		private void RemoveMinions()
		{
			foreach (SplitMobMinion minion in _minions)
				minion.RemoveFromWorld();

			_minions.Clear();
		}

		private void RewardParticipants(GameObject killer)
		{
			HashSet<GamePlayer> contributors = new();

			lock (XpGainersLock)
			{
				foreach (KeyValuePair<GameLiving, double> pair in XPGainers)
				{
					GamePlayer contributor = ResolvePlayer(pair.Key);

					if (contributor != null)
						contributors.Add(contributor);
				}
			}

			GamePlayer killerPlayer = ResolvePlayer(killer);

			if (killerPlayer != null)
				contributors.Add(killerPlayer);

			HashSet<GamePlayer> eligible = new(contributors);

			foreach (GamePlayer contributor in contributors)
			{
				if (contributor.Group == null)
					continue;

				foreach (GamePlayer member in contributor.Group.GetPlayersInTheGroup())
					eligible.Add(member);
			}

			foreach (GamePlayer player in GetPlayersInRadius(REWARD_RADIUS))
			{
				if (!eligible.Contains(player))
					continue;

				player.GainBountyPoints(BOUNTY_POINT_REWARD, true);
				player.Out.SendMessage($"You have defeated {Name} and gain {BOUNTY_POINT_REWARD} bounty points!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
			}
		}

		private static GamePlayer ResolvePlayer(GameObject source)
		{
			if (source is GamePlayer player)
				return player;

			if (source is GameNPC npc && npc.Brain is IControlledBrain controlledBrain)
				return controlledBrain.GetPlayerOwner();

			return null;
		}
	}

	public class SplitMobMinion : GameNPC
	{
		public SplitMob Owner { get; init; }

		public override void Die(GameObject killer)
		{
			Owner?.OnMinionDied(this);
			base.Die(killer);
		}
	}
}
