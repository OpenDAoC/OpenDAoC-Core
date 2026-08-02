using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class Tabor : GameNPC
	{
		public Tabor() : base() { }

		public override bool AddToWorld()
		{
			foreach (GameNPC npc in GetNPCsInRadius(5000))
			{
				if (npc is TaborGhost && npc.IsAlive && !npc.InCombat)
					npc.RemoveFromWorld();
			}

			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166738);
			LoadTemplate(npcTemplate);

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 315, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.Standard);

			VisibleActiveWeaponSlots = 16;
			MeleeDamageType = eDamageType.Slash;

			SetOwnBrain(new TaborBrain(1000));
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}

		public override void Die(GameObject killer)
		{
			Message.MessageToArea(this, $"As {Name} falls to the ground, you feel a breeze in the air.\nA swirl of dirt covers the area.", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
			SpawnSwirlDirt();
			base.Die(killer);
		}

		private void SpawnSwirlDirt()
		{
			SwirlDirt swirl = new();
			swirl.X = 37256;
			swirl.Y = 32460;
			swirl.Z = 14437;
			swirl.Heading = Heading;
			swirl.CurrentRegion = CurrentRegion;
			swirl.AddToWorld();
		}
	}

	public class TaborGhost : GameNPC
	{
		public TaborGhost() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161293);
			LoadTemplate(npcTemplate);

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 445, 0, 0);
			template.AddNPCEquipment(eInventorySlot.DistanceWeapon, 471, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.Standard);

			VisibleActiveWeaponSlots = 16;
			MeleeDamageType = eDamageType.Slash;

			SetOwnBrain(new TaborBrain(1500));
			LoadedFromScript = true;
			RespawnInterval = -1;

			if (!base.AddToWorld())
				return false;

			Message.MessageToArea(this, $"{Name} says, \"You thought the fight was over, did you?\"", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
			return true;
		}

		public override void Die(GameObject killer)
		{
			if (killer != null)
				Message.MessageToArea(this, $"{Name} says, \"I will return some day.Be warned!\"", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);

			base.Die(killer);
		}
	}

	public class SwirlDirt : GameNPC
	{
		public SwirlDirt() : base() { }

		public override bool AddToWorld()
		{
			Name = "Swirl of Dirt";
			Level = 50;
			Model = 665;
			Size = 70;
			Flags = eFlags.DONTSHOWNAME | eFlags.CANTTARGET | eFlags.PEACE;

			SetOwnBrain(new SwirlDirtBrain());
			LoadedFromScript = true;
			RespawnInterval = -1;
			bool success = base.AddToWorld();

			if (success)
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(ShowEffect), 1000);

			return success;
		}

		private int ShowEffect(ECSGameTimer timer)
		{
			if (IsAlive)
			{
				foreach (GamePlayer player in GetPlayersInRadius(3000))
					player.Out.SendSpellEffectAnimation(this, this, 6072, 0, false, 0x01);

				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(SpawnGhost), 1000);
			}

			return 0;
		}

		private int SpawnGhost(ECSGameTimer timer)
		{
			SpawnGhostOfTabor();
			new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(RemoveMob), 500);
			return 0;
		}

		private int RemoveMob(ECSGameTimer timer)
		{
			if (IsAlive)
				RemoveFromWorld();

			return 0;
		}

		private void SpawnGhostOfTabor()
		{
			foreach (GameNPC npc in GetNPCsInRadius(5000))
			{
				if (npc is TaborGhost)
					return;
			}

			TaborGhost ghost = new();
			ghost.X = X;
			ghost.Y = Y;
			ghost.Z = Z;
			ghost.Heading = Heading;
			ghost.CurrentRegion = CurrentRegion;
			ghost.AddToWorld();
		}
	}
}

namespace DOL.AI.Brain
{
	public class TaborBrain : StandardMobBrain
	{
		public TaborBrain(int thinkInterval) : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = thinkInterval;
		}

		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
			{
				TryCastSpell(Tabor_Dot, 15, eEffect.DamageOverTime);
				TryCastSpell(Tabor_Dot2, 15, eEffect.DamageOverTime);
				TryCastSpell(Tabor_DD, 15);
				TryCastSpell(Tabor_DD2, 15);
			}

			base.Think();
		}

		#region Spells
		private static Spell Tabor_DD => ScriptSpells.GetOrCreate("TaborEarthBlast", 20, db =>
		{
			db.CastTime = 3.5;
			db.RecastDelay = Util.Random(10, 15);
			db.ClientEffect = 5087;
			db.Icon = 5087;
			db.TooltipId = 5087;
			db.Damage = 100;
			db.Name = "Earth Blast";
			db.Range = 1500;
			db.SpellID = 11931;
			db.Target = eSpellTarget.ENEMY.ToString();
			db.Type = eSpellType.DirectDamageNoVariance.ToString();
			db.Uninterruptible = true;
			db.DamageType = (int)eDamageType.Matter;
		});

		private static Spell Tabor_DD2 => ScriptSpells.GetOrCreate("TaborEarthBlastAoe", 20, db =>
		{
			db.CastTime = 3.5;
			db.RecastDelay = Util.Random(15, 20);
			db.ClientEffect = 6159;
			db.Icon = 6159;
			db.TooltipId = 6169;
			db.Damage = 80;
			db.Name = "Earth Shatter";
			db.Range = 1500;
			db.Radius = 350;
			db.SpellID = 11932;
			db.Target = eSpellTarget.ENEMY.ToString();
			db.Type = eSpellType.DirectDamageNoVariance.ToString();
			db.Uninterruptible = true;
			db.DamageType = (int)eDamageType.Matter;
		});

		private static Spell Tabor_Dot => ScriptSpells.GetOrCreate("TaborPoison", 20, db =>
		{
			db.CastTime = 3;
			db.RecastDelay = 20;
			db.ClientEffect = 3411;
			db.Icon = 3411;
			db.Name = "Poison";
			db.Description = "Inflicts 25 damage to the target every 4 sec for 20 seconds";
			db.Message1 = "A cloud of stinging poison surrounds you!";
			db.Message2 = "{0} is engulfed in stinging poison!";
			db.Message3 = "The poison wears off.";
			db.Message4 = "{0} recovers from the poison.";
			db.TooltipId = 3411;
			db.Range = 1500;
			db.Damage = 25;
			db.Duration = 20;
			db.Frequency = 40;
			db.SpellID = 11933;
			db.Target = eSpellTarget.ENEMY.ToString();
			db.SpellGroup = 1802;
			db.EffectGroup = 1502;
			db.Type = eSpellType.DamageOverTime.ToString();
			db.Uninterruptible = true;
			db.DamageType = (int)eDamageType.Matter;
		});

		private static Spell Tabor_Dot2 => ScriptSpells.GetOrCreate("TaborAcid", 20, db =>
		{
			db.CastTime = 3;
			db.RecastDelay = 20;
			db.ClientEffect = 3475;
			db.Icon = 4431;
			db.Name = "Acid";
			db.Description = "Inflicts 25 damage to the target every 4 sec for 20 seconds";
			db.Message1 = "An acidic cloud surrounds you!";
			db.Message2 = "{0} is surrounded by an acidic cloud!";
			db.Message3 = "The acidic mist around you dissipates.";
			db.Message4 = "The acidic mist around {0} dissipates.";
			db.TooltipId = 4431;
			db.Range = 1500;
			db.Damage = 25;
			db.Duration = 20;
			db.Frequency = 40;
			db.SpellID = 11934;
			db.Target = eSpellTarget.ENEMY.ToString();
			db.SpellGroup = 1803;
			db.EffectGroup = 1503;
			db.Type = eSpellType.DamageOverTime.ToString();
			db.Uninterruptible = true;
			db.DamageType = (int)eDamageType.Body;
		});
		#endregion
	}

	public class SwirlDirtBrain : StandardMobBrain
	{
		public SwirlDirtBrain() : base()
		{
			AggroLevel = 0;
			AggroRange = 0;
		}
	}
}
