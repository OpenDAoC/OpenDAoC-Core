using System.Collections.Generic;
using DOL.AI;
using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public abstract class UnnaturalStormCloud : GameNPC
	{
		protected abstract byte SpawnLevel { get; }
		protected abstract eFlags SpawnFlags { get; }

		protected UnnaturalStormCloud(ABrain defaultBrain) : base(defaultBrain) { }

		public override bool AddToWorld()
		{
			Name = "Unnatural Storm";
			Model = 665;
			Size = 100;
			Level = SpawnLevel;
			MeleeDamageType = eDamageType.Crush;
			Race = 2003;
			Flags = SpawnFlags;
			MaxSpeedBase = 0;
			RespawnInterval = -1;
			LoadedFromScript = true;

			if (!base.AddToWorld())
				return false;

			_ = new ECSGameTimer(this, ShowEffect, 500);
			return true;
		}

		public override void StartAttack(GameObject target) { }

		private int ShowEffect(ECSGameTimer timer)
		{
			if (!IsAlive)
				return 0;

			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.Out.SendSpellCastAnimation(this, 14323, 1);
				player.Out.SendSpellEffectAnimation(this, this, 3508, 0, false, 0x01);
			}

			return 3000;
		}
	}

	public class UnnaturalStorm : UnnaturalStormCloud
	{
		private readonly List<UnnaturalStormAdds> _adds = new();

		protected override byte SpawnLevel => (byte) Util.Random(65, 70);
		protected override eFlags SpawnFlags => eFlags.DONTSHOWNAME | eFlags.CANTTARGET | eFlags.FLYING;

		public UnnaturalStorm() : base(new UnnaturalStormBrain()) { }

		public override bool AddToWorld()
		{
			if (!base.AddToWorld())
				return false;

			Intelligence = 200;
			Dexterity = 200;

			Message.MessageToZone(CurrentZone, "An intense supernatural storm explodes in the sky over the northeastern expanse of Lyonesse!", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);

			SpawnAdds();
			return true;
		}

		public override void Die(GameObject killer)
		{
			RemoveAdds();

			Message.MessageToZone(CurrentZone, "The unnatural storm over the northeastern expanse of Lyonesse breaks apart, its fury spent!", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);

			base.Die(killer);
		}

		public void Dismiss()
		{
			RemoveAdds();
			RemoveFromWorld();
		}

		private void RemoveAdds()
		{
			foreach (UnnaturalStormAdds add in _adds)
				add.RemoveFromWorld();

			_adds.Clear();
		}

		private void SpawnAdds()
		{
			int count = Util.Random(4, 5);

			for (int i = 0; i < count; i++)
			{
				UnnaturalStormAdds add = new()
				{
					X = X + Util.Random(-1000, 1000),
					Y = Y + Util.Random(-1000, 800),
					Z = Z + Util.Random(-400, 400),
					Heading = Heading,
					CurrentRegion = CurrentRegion
				};

				if (add.AddToWorld())
					_adds.Add(add);
			}
		}
	}

	public class UnnaturalStormAdds : UnnaturalStormCloud
	{
		protected override byte SpawnLevel => (byte) Util.Random(40, 42);
		protected override eFlags SpawnFlags => eFlags.DONTSHOWNAME | eFlags.CANTTARGET | eFlags.PEACE | eFlags.FLYING;

		public UnnaturalStormAdds() : base(new StandardMobBrain { AggroLevel = 0, AggroRange = 0 }) { }
	}

	public class UnnaturalStormController : GameNPC
	{
		public override bool IsVisibleToPlayers => true;

		public override bool AddToWorld()
		{
			Name = "Unnatural Storm Controller";
			GuildName = "DO NOT REMOVE";
			Level = 50;
			Model = 665;
			RespawnInterval = 5000;
			Flags = eFlags.DONTSHOWNAME | eFlags.CANTTARGET | eFlags.PEACE | eFlags.FLYING;
			SetOwnBrain(new UnnaturalStormControllerBrain());
			return base.AddToWorld();
		}
	}
}

namespace DOL.AI.Brain
{
	public class UnnaturalStormBrain : StandardMobBrain
	{
		private bool _engaged;

		public UnnaturalStormBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 2500;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (HasAggro)
			{
				if (!_engaged)
				{
					_engaged = true;

					Message.MessageToArea(Body, "The unnatural storm rumbles violently as bolts of lightning lash the ground below!", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
				}

				if (Body.TargetObject != null && !Body.IsCasting)
					Body.CastSpell(StormLightning, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			}
			else
				_engaged = false;

			base.Think();
		}

		private static Spell StormLightning => ScriptSpells.GetOrCreate("UnnaturalStormLightning", 50, db =>
		{
			db.CastTime = 0;
			db.RecastDelay = 3;
			db.Power = 0;
			db.ClientEffect = 3508;
			db.Icon = 3508;
			db.Damage = 200;
			db.DamageType = (int) eDamageType.Energy;
			db.Name = "Storm Lightning";
			db.Range = 2500;
			db.SpellID = 11947;
			db.Target = eSpellTarget.ENEMY.ToString();
			db.Type = eSpellType.DirectDamageNoVariance.ToString();
			db.Uninterruptible = true;
			db.MoveCast = true;
		});
	}

	public class UnnaturalStormControllerBrain : APlayerVicinityBrain
	{
		private UnnaturalStorm _storm;
		private bool _spawnedThisNight;

		public UnnaturalStormControllerBrain() : base()
		{
			ThinkInterval = 1000;
		}

		public override void Think()
		{
			if (_storm != null && (!_storm.IsAlive || _storm.ObjectState is not GameObject.eObjectState.Active))
				_storm = null;

			uint gameTime = WorldMgr.GetCurrentGameTime();
			uint hour = gameTime / 1000 / 60 / 60;
			uint minute = gameTime / 1000 / 60 % 60;
			bool isDay = hour is >= 7 and < 18;
			bool isNight = hour < 7 || hour > 18 || (hour is 18 && minute >= 30);

			if (isDay)
			{
				_spawnedThisNight = false;
				DismissStorm();
			}
			else if (isNight && !_spawnedThisNight)
				SpawnStorm();
		}

		private void DismissStorm()
		{
			if (_storm == null || _storm.Brain is not StandardMobBrain brain || brain.HasAggro)
				return;

			Message.MessageToZone(_storm.CurrentZone, "The unnatural storm over the northeastern expanse of Lyonesse dissipates with the morning light.", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);

			_storm.Dismiss();
			_storm = null;
		}

		private void SpawnStorm()
		{
			UnnaturalStorm storm = new()
			{
				X = Body.X,
				Y = Body.Y,
				Z = Body.Z,
				Heading = Body.Heading,
				CurrentRegion = Body.CurrentRegion
			};

			if (!storm.AddToWorld())
				return;

			_storm = storm;
			_spawnedThisNight = true;
		}
	}
}
