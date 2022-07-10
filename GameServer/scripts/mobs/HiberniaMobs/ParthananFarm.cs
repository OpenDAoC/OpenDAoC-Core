using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;
using System;
using System.Collections.Generic;

#region Amalgamate Parthanan
namespace DOL.GS
{
	public class AmalgamateParthanan : GameNPC
	{
		public AmalgamateParthanan() : base() { }
        #region Immune to specific dammage/range attack
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GamePet)
			{
				GameLiving target = source as GameLiving;
				if (target == null || target.AttackWeapon == null) return;
				if (damageType == eDamageType.Body || damageType == eDamageType.Cold ||
					damageType == eDamageType.Energy || damageType == eDamageType.Heat
					|| damageType == eDamageType.Matter || damageType == eDamageType.Spirit || target.AttackWeapon.Object_Type == (int)eObjectType.RecurvedBow || target.AttackWeapon.Object_Type == (int)eObjectType.Fired)
				{
					GamePlayer truc;
					if (source is GamePlayer)
						truc = (source as GamePlayer);
					else
						truc = ((source as GamePet).Owner as GamePlayer);
					if (truc != null)
						truc.Out.SendMessage(Name + " is immune to this form of attack!", eChatType.CT_SpellResisted, eChatLoc.CL_ChatWindow);
					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
				else //take dmg
				{
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
			}
			if (source is GameNPC)//for charmed pets or other faction mobs
			{
				GameNPC npc = source as GameNPC;
				if (npc.AttackWeapon != null && npc.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
				{
					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
				else if (damageType == eDamageType.Body || damageType == eDamageType.Cold ||
					damageType == eDamageType.Energy || damageType == eDamageType.Heat
					|| damageType == eDamageType.Matter || damageType == eDamageType.Spirit)
				{
					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
				else //take dmg
				{
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
			}
		}
		#endregion

		public override void StartAttack(GameObject target)//dont attack in initial phase after spawn
		{
			#region Lough Derg
			if (PackageID == "ParthananBossLoughDerg")
			{
				if (ParthananFarmController1Brain.SacrificeParthanan1)
					return;
				else
					base.StartAttack(target);
			}
			#endregion
			#region Connacht
			if (PackageID == "ParthananBossConnacht")
			{
				if (ParthananFarmController2Brain.SacrificeParthanan2)
					return;
				else
					base.StartAttack(target);
			}
			//2nd farm
			if (PackageID == "ParthananBossConnacht2")
			{
				if (ParthananFarmController2bBrain.SacrificeParthanan2b)
					return;
				else
					base.StartAttack(target);
			}
			#endregion
			#region Lough Gur
			if (PackageID == "ParthananBossLoughGur")
			{
				if (ParthananFarmController3Brain.SacrificeParthanan3)
					return;
				else
					base.StartAttack(target);
			}
			//2nd farm
			if (PackageID == "ParthananBossLoughGur2")
			{
				if (ParthananFarmController3bBrain.SacrificeParthanan3b)
					return;
				else
					base.StartAttack(target);
			}
            #endregion
        }
        public override bool HasAbility(string keyName)//immune to cc and dmg(in certain situation only)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;
            #region Lough Derg
            if (ParthananFarmController1Brain.SacrificeParthanan1 && PackageID == "ParthananBossLoughDerg" && IsAlive && keyName == GS.Abilities.DamageImmunity)
				return true;
            #endregion
            #region Connacht
            if (ParthananFarmController2Brain.SacrificeParthanan2 && PackageID == "ParthananBossConnacht" && IsAlive && keyName == GS.Abilities.DamageImmunity)
				return true;
			if (ParthananFarmController2bBrain.SacrificeParthanan2b && PackageID == "ParthananBossConnacht2" && IsAlive && keyName == GS.Abilities.DamageImmunity)
				return true;
            #endregion
            #region Lough Gur
            if (ParthananFarmController3Brain.SacrificeParthanan3 && PackageID == "ParthananBossLoughGur" && IsAlive && keyName == GS.Abilities.DamageImmunity)
				return true;
			if (ParthananFarmController3bBrain.SacrificeParthanan3b && PackageID == "ParthananBossLoughGur2" && IsAlive && keyName == GS.Abilities.DamageImmunity)
				return true;
            #endregion
            return base.HasAbility(keyName);
		}

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60157792);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			AmalgamateParthananBrain sbrain = new AmalgamateParthananBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			bool success = base.AddToWorld();
			if (success)
			{
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
			}
			return success;
		}
		public override int MaxHealth
		{
			get { return 3000; }
		}
		public override void Die(GameObject killer)
        {
			#region Lough Derg
			if (PackageID == "ParthananBossLoughDerg")
			{
				ParthananFarmController1Brain.LoughDergBoss = 0;
				ParthananFarmController1Brain.BossIsUP = false;
				ParthananFarmController1Brain.ParthansCanDie = false;
				if (ParthananFarmController1Brain.MinParthAround.Count > 0)
					ParthananFarmController1Brain.MinParthAround.Clear();
				ParthananFarmController1Brain.MobsToKillLoughDerg = Util.Random(60, 120);
			}
            #endregion
            #region Connacht
            if (PackageID == "ParthananBossConnacht")
			{
				ParthananFarmController2Brain.ConnachtBoss = 0;
				ParthananFarmController2Brain.BossIsUP2 = false;
				ParthananFarmController2Brain.ParthansCanDie2 = false;
				if (ParthananFarmController2Brain.MinParthAround2.Count > 0)
					ParthananFarmController2Brain.MinParthAround2.Clear();
				ParthananFarmController2Brain.MobsToKillConnacht = Util.Random(60, 120);
			}
			//2nd farm
			if (PackageID == "ParthananBossConnacht2")
			{
				ParthananFarmController2bBrain.Connacht2Boss = 0;
				ParthananFarmController2bBrain.BossIsUP2b = false;
				ParthananFarmController2bBrain.ParthansCanDie2b = false;
				if (ParthananFarmController2bBrain.MinParthAround2b.Count > 0)
					ParthananFarmController2bBrain.MinParthAround2b.Clear();
				ParthananFarmController2bBrain.MobsToKillConnacht2 = Util.Random(60, 80);
			}
            #endregion
            #region Lough Gur
            if (PackageID == "ParthananBossLoughGur")
			{
				ParthananFarmController3Brain.LoughGurBoss = 0;
				ParthananFarmController3Brain.BossIsUP3 = false;
				ParthananFarmController3Brain.ParthansCanDie3 = false;
				if (ParthananFarmController3Brain.MinParthAround3.Count > 0)
					ParthananFarmController3Brain.MinParthAround3.Clear();
				ParthananFarmController3Brain.MobsToKillLoughGur = Util.Random(60, 120);
			}
			//2nd farm
			if (PackageID == "ParthananBossLoughGur2")
			{
				ParthananFarmController3bBrain.LoughGur2Boss = 0;
				ParthananFarmController3bBrain.BossIsUP3b = false;
				ParthananFarmController3bBrain.ParthansCanDie3b = false;
				if (ParthananFarmController3bBrain.MinParthAround3b.Count > 0)
					ParthananFarmController3bBrain.MinParthAround3b.Clear();
				ParthananFarmController3bBrain.MobsToKillLoughGur2 = Util.Random(60, 120);
			}
            #endregion
            base.Die(killer);
        }
        #region Effects
        protected int Show_Effect(ECSGameTimer timer)
		{
            #region Lough Derg
            if (IsAlive && ParthananFarmController1Brain.SacrificeParthanan1 && PackageID == "ParthananBossLoughDerg")
			{
				foreach (GamePlayer player in GetPlayersInRadius(10000))
				{
					if (player != null)
						player.Out.SendSpellCastAnimation(this, 2909, 1);
				}
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoCast), 1500);
			}
			if (IsAlive && !ParthananFarmController1Brain.SacrificeParthanan1 && PackageID == "ParthananBossLoughDerg")
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoEndCast), 100);
            #endregion
            #region Connacht
            if (IsAlive && ParthananFarmController2Brain.SacrificeParthanan2 && PackageID == "ParthananBossConnacht")
			{
				foreach (GamePlayer player in GetPlayersInRadius(10000))
				{
					if (player != null)
						player.Out.SendSpellCastAnimation(this, 2909, 1);
				}
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoCast), 1500);
			}
			if (IsAlive && !ParthananFarmController2Brain.SacrificeParthanan2 && PackageID == "ParthananBossConnacht")
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoEndCast), 100);

			//2nd farm
			if (IsAlive && ParthananFarmController2bBrain.SacrificeParthanan2b && PackageID == "ParthananBossConnacht2")
			{
				foreach (GamePlayer player in GetPlayersInRadius(10000))
				{
					if (player != null)
						player.Out.SendSpellCastAnimation(this, 2909, 1);
				}
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoCast), 1500);
			}
			if (IsAlive && !ParthananFarmController2bBrain.SacrificeParthanan2b && PackageID == "ParthananBossConnacht2")
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoEndCast), 100);
			#endregion
			#region Lough Gur
			if (IsAlive && ParthananFarmController3Brain.SacrificeParthanan3 && PackageID == "ParthananBossLoughGur")
			{
				foreach (GamePlayer player in GetPlayersInRadius(10000))
				{
					if (player != null)
						player.Out.SendSpellCastAnimation(this, 2909, 1);
				}
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoCast), 1500);
			}
			if (IsAlive && !ParthananFarmController2Brain.SacrificeParthanan2 && PackageID == "ParthananBossLoughGur")
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoEndCast), 100);

			//2nd farm
			if (IsAlive && ParthananFarmController3bBrain.SacrificeParthanan3b && PackageID == "ParthananBossLoughGur2")
			{
				foreach (GamePlayer player in GetPlayersInRadius(10000))
				{
					if (player != null)
						player.Out.SendSpellCastAnimation(this, 2909, 1);
				}
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoCast), 1500);
			}
			if (IsAlive && !ParthananFarmController3bBrain.SacrificeParthanan3b && PackageID == "ParthananBossLoughGur2")
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoEndCast), 100);
			#endregion
			return 0;
		}
		protected int DoCast(ECSGameTimer timer)
		{
            #region Lough Derg
            if (IsAlive && ParthananFarmController1Brain.SacrificeParthanan1 && PackageID == "ParthananBossLoughDerg")
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 1500);
			if(IsAlive && !ParthananFarmController1Brain.SacrificeParthanan1 && PackageID == "ParthananBossLoughDerg")
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoEndCast), 100);
            #endregion
            #region Connacht
            if (IsAlive && ParthananFarmController2Brain.SacrificeParthanan2 && PackageID == "ParthananBossConnacht")
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 1500);
			if (IsAlive && !ParthananFarmController2Brain.SacrificeParthanan2 && PackageID == "ParthananBossConnacht")
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoEndCast), 100);

			//2nd farm
			if (IsAlive && ParthananFarmController2bBrain.SacrificeParthanan2b && PackageID == "ParthananBossConnacht2")
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 1500);
			if (IsAlive && !ParthananFarmController2bBrain.SacrificeParthanan2b && PackageID == "ParthananBossConnacht2")
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoEndCast), 100);
			#endregion
			#region Lough Gur
			if (IsAlive && ParthananFarmController3Brain.SacrificeParthanan3 && PackageID == "ParthananBossLoughGur")
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 1500);
			if (IsAlive && !ParthananFarmController3Brain.SacrificeParthanan3 && PackageID == "ParthananBossLoughGur")
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoEndCast), 100);

			//2nd farm
			if (IsAlive && ParthananFarmController3bBrain.SacrificeParthanan3b && PackageID == "ParthananBossLoughGur2")
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 1500);
			if (IsAlive && !ParthananFarmController3bBrain.SacrificeParthanan3b && PackageID == "ParthananBossLoughGur2")
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoEndCast), 100);
			#endregion
			return 0;
		}
		protected int DoEndCast(ECSGameTimer timer)
		{
            #region Lough Derg
            if (IsAlive && !ParthananFarmController1Brain.SacrificeParthanan1 && PackageID == "ParthananBossLoughDerg")
			{
				foreach (GamePlayer player in GetPlayersInRadius(10000))
				{
					if (player != null)
						player.Out.SendSpellEffectAnimation(this, this, 6159, 0, false, 0x01);
				}
			}
            #endregion
            #region Connacht
            if (IsAlive && !ParthananFarmController2Brain.SacrificeParthanan2 && PackageID == "ParthananBossConnacht")
			{
				foreach (GamePlayer player in GetPlayersInRadius(10000))
				{
					if (player != null)
						player.Out.SendSpellEffectAnimation(this, this, 6159, 0, false, 0x01);
				}
			}
			//2nd farm
			if (IsAlive && !ParthananFarmController2bBrain.SacrificeParthanan2b && PackageID == "ParthananBossConnacht2")
			{
				foreach (GamePlayer player in GetPlayersInRadius(10000))
				{
					if (player != null)
						player.Out.SendSpellEffectAnimation(this, this, 6159, 0, false, 0x01);
				}
			}
			#endregion
			#region Lough Gur
			if (IsAlive && !ParthananFarmController3Brain.SacrificeParthanan3 && PackageID == "ParthananBossLoughGur")
			{
				foreach (GamePlayer player in GetPlayersInRadius(10000))
				{
					if (player != null)
						player.Out.SendSpellEffectAnimation(this, this, 6159, 0, false, 0x01);
				}
			}
			//2nd farm
			if (IsAlive && !ParthananFarmController3bBrain.SacrificeParthanan3b && PackageID == "ParthananBossLoughGur2")
			{
				foreach (GamePlayer player in GetPlayersInRadius(10000))
				{
					if (player != null)
						player.Out.SendSpellEffectAnimation(this, this, 6159, 0, false, 0x01);
				}
			}
			#endregion
			return 0;
		}
		#endregion
	}
}
namespace DOL.AI.Brain
{
	public class AmalgamateParthananBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public AmalgamateParthananBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 500;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion

#region Parthanans
namespace DOL.GS
{
	public class Parthanan : GameNPC
	{
		public Parthanan() : base() { }

		public override bool AddToWorld()
		{

			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164845);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			ParthananBrain sbrain = new ParthananBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void Die(GameObject killer)
		{
            #region Lough Derg
            if (!ParthananFarmController1Brain.SacrificeParthanan1)
			{
				if (PackageID == "ParthananLoughDerg")
					++ParthananFarmController1Brain.ParthanansKilledFarm1;
			}
			else
            {
				if (PackageID == "ParthananLoughDerg")
				{
					if (ParthananFarmController1Brain.MinParthAround.Contains(this))
						ParthananFarmController1Brain.MinParthAround.Remove(this);
				}
			}
            #endregion
            #region Connacht
            if (!ParthananFarmController2Brain.SacrificeParthanan2)
			{
				if (PackageID == "ParthananConnacht")
					++ParthananFarmController2Brain.ParthanansKilledFarm2;
			}
			else
			{
				if (PackageID == "ParthananConnacht")
				{
					if (ParthananFarmController2Brain.MinParthAround2.Contains(this))
						ParthananFarmController2Brain.MinParthAround2.Remove(this);
				}
			}
			//2nd farm
			if (!ParthananFarmController2bBrain.SacrificeParthanan2b)
			{
				if (PackageID == "ParthananConnacht2")
					++ParthananFarmController2bBrain.ParthanansKilledFarm2b;
			}
			else
			{
				if (PackageID == "ParthananConnacht2")
				{
					if (ParthananFarmController2bBrain.MinParthAround2b.Contains(this))
						ParthananFarmController2bBrain.MinParthAround2b.Remove(this);
				}
			}
            #endregion
            #region Lough Gur
            if (!ParthananFarmController3Brain.SacrificeParthanan3)
			{
				if (PackageID == "ParthananLoughGur")
					++ParthananFarmController3Brain.ParthanansKilledFarm3;
			}
			else
			{
				if (PackageID == "ParthananLoughGur")
				{
					if (ParthananFarmController3Brain.MinParthAround3.Contains(this))
						ParthananFarmController3Brain.MinParthAround3.Remove(this);
				}
			}
			//2nd farm
			if (!ParthananFarmController3bBrain.SacrificeParthanan3b)
			{
				if (PackageID == "ParthananLoughGur2")
					++ParthananFarmController3bBrain.ParthanansKilledFarm3b;
			}
			else
			{
				if (PackageID == "ParthananLoughGur2")
				{
					if (ParthananFarmController3bBrain.MinParthAround3b.Contains(this))
						ParthananFarmController3bBrain.MinParthAround3b.Remove(this);
				}
			}
            #endregion
            base.Die(killer);
		}
    }
}
namespace DOL.AI.Brain
{
	public class ParthananBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public ParthananBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 500;
			ThinkInterval = 1500;
		}
        public override void AttackMostWanted()
        {
			if (!Body.IsControlledNPC(Body))
			{
                #region Lough Derg
                if (Body.PackageID == "ParthananLoughDerg")
				{
					if (ParthananFarmController1Brain.SacrificeParthanan1 && ParthananFarmController1Brain.ParthansCanDie)
						return;
					else
						base.AttackMostWanted();
				}
                #endregion
                #region Connacht
                if (Body.PackageID == "ParthananConnacht")
				{
					if (ParthananFarmController2Brain.SacrificeParthanan2 && ParthananFarmController2Brain.ParthansCanDie2)
						return;
					else
						base.AttackMostWanted();
				}
				//2nd farm
				if (Body.PackageID == "ParthananConnacht2")
				{
					if (ParthananFarmController2bBrain.SacrificeParthanan2b && ParthananFarmController2bBrain.ParthansCanDie2b)
						return;
					else
						base.AttackMostWanted();
				}
                #endregion
                #region Lough Gur
                if (Body.PackageID == "ParthananLoughGur")
				{
					if (ParthananFarmController3Brain.SacrificeParthanan3 && ParthananFarmController3Brain.ParthansCanDie3)
						return;
					else
						base.AttackMostWanted();
				}
				//2nd farm
				if (Body.PackageID == "ParthananLoughGur2")
				{
					if (ParthananFarmController3bBrain.SacrificeParthanan3b && ParthananFarmController3bBrain.ParthansCanDie3b)
						return;
					else
						base.AttackMostWanted();
				}
                #endregion
            }
        }
		ushort oldModel;
		GameNPC.eFlags oldFlags;
		bool changed;
		private protected bool setbrain = false;
		public override void Think()
		{
			if (Body.IsAlive && !Body.IsControlledNPC(Body))
			{
				#region Lough Derg Parthnanans
				if (Body.PackageID == "ParthananLoughDerg")
				{
					INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164845);
					if (ParthananFarmController1Brain.LoughDergBoss == 1)
					{
						if (changed == false)
						{
							oldFlags = Body.Flags;
							Body.Flags ^= GameNPC.eFlags.CANTTARGET;
							Body.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
							Body.Flags ^= GameNPC.eFlags.PEACE;

							if (oldModel == 0)
								oldModel = Body.Model;

							Body.Model = 1;
							changed = true;
						}
					}
					else
					{
						if (changed)
						{
							Body.Flags = (GameNPC.eFlags)npcTemplate.Flags;
							Body.Model = Convert.ToUInt16(npcTemplate.Model);
							changed = false;
						}
					}

					if (ParthananFarmController1Brain.SacrificeParthanan1 && ParthananFarmController1Brain.ParthansCanDie)
					{
						ClearAggroList();
						Body.StopAttack();
						foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
						{
							if (npc.Brain is ParthananFarmController1Brain)
							{
								if (!Body.IsWithinRadius(npc, 50))
									Body.WalkTo(npc.X, npc.Y, npc.Z, Body.MaxSpeedBase);
								else
									Body.Die(npc);
							}
						}
					}
					else
					{
						if (Body.Model == 1)
						{
							foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
							{
								if (npc.Brain is ParthananFarmController1Brain)
								{
									if (Body.IsWithinRadius(npc, 50))
										Body.Die(npc);
								}
							}
						}
					}
				}
				#endregion
				#region Connacht Parthnanans
				if (Body.PackageID == "ParthananConnacht")
				{
					INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164845);
					if (ParthananFarmController2Brain.ConnachtBoss == 1)
					{
						if (changed == false)
						{
							oldFlags = Body.Flags;
							Body.Flags ^= GameNPC.eFlags.CANTTARGET;
							Body.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
							Body.Flags ^= GameNPC.eFlags.PEACE;

							if (oldModel == 0)
								oldModel = Body.Model;

							Body.Model = 1;
							changed = true;
						}
					}
					else
					{
						if (changed)
						{
							Body.Flags = (GameNPC.eFlags)npcTemplate.Flags;
							Body.Model = Convert.ToUInt16(npcTemplate.Model);
							changed = false;
						}
					}

					if (ParthananFarmController2Brain.SacrificeParthanan2 && ParthananFarmController2Brain.ParthansCanDie2)
					{
						ClearAggroList();
						Body.StopAttack();
						foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
						{
							if (npc.Brain is ParthananFarmController2Brain)
							{
								if (!Body.IsWithinRadius(npc, 50))
									Body.WalkTo(npc.X, npc.Y, npc.Z, Body.MaxSpeedBase);
								else
									Body.Die(npc);
							}
						}
					}
					else
					{
						if (Body.Model == 1)
						{
							foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
							{
								if (npc.Brain is ParthananFarmController2Brain)
								{
									if (Body.IsWithinRadius(npc, 50))
										Body.Die(npc);
								}
							}
						}
					}
				}
				///
				///////////////////////////////// 2nd farm
				///
				if (Body.PackageID == "ParthananConnacht2")
				{
					INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164845);
					if (ParthananFarmController2bBrain.Connacht2Boss == 1)
					{
						if (changed == false)
						{
							oldFlags = Body.Flags;
							Body.Flags ^= GameNPC.eFlags.CANTTARGET;
							Body.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
							Body.Flags ^= GameNPC.eFlags.PEACE;

							if (oldModel == 0)
								oldModel = Body.Model;

							Body.Model = 1;
							changed = true;
						}
					}
					else
					{
						if (changed)
						{
							Body.Flags = (GameNPC.eFlags)npcTemplate.Flags;
							Body.Model = Convert.ToUInt16(npcTemplate.Model);
							changed = false;
						}
					}

					if (ParthananFarmController2bBrain.SacrificeParthanan2b && ParthananFarmController2bBrain.ParthansCanDie2b)
					{
						ClearAggroList();
						Body.StopAttack();
						foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
						{
							if (npc.Brain is ParthananFarmController2bBrain)
							{
								if (!Body.IsWithinRadius(npc, 50))
									Body.WalkTo(npc.X, npc.Y, npc.Z, Body.MaxSpeedBase);
								else
									Body.Die(npc);
							}
						}
					}
					else
					{
						if (Body.Model == 1)
						{
							foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
							{
								if (npc.Brain is ParthananFarmController2bBrain)
								{
									if (Body.IsWithinRadius(npc, 50))
										Body.Die(npc);
								}
							}
						}
					}
				}
				#endregion
				#region Lough Gur Parthanans
				if (Body.PackageID == "ParthananLoughGur")
				{
					INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164845);
					if (ParthananFarmController3Brain.LoughGurBoss == 1)
					{
						if (changed == false)
						{
							oldFlags = Body.Flags;
							Body.Flags ^= GameNPC.eFlags.CANTTARGET;
							Body.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
							Body.Flags ^= GameNPC.eFlags.PEACE;

							if (oldModel == 0)
								oldModel = Body.Model;

							Body.Model = 1;
							changed = true;
						}
					}
					else
					{
						if (changed)
						{
							Body.Flags = (GameNPC.eFlags)npcTemplate.Flags;
							Body.Model = Convert.ToUInt16(npcTemplate.Model);
							changed = false;
						}
					}
					if (ParthananFarmController3Brain.SacrificeParthanan3 && ParthananFarmController3Brain.ParthansCanDie3)
					{
						ClearAggroList();
						Body.StopAttack();
						foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
						{
							if (npc.Brain is ParthananFarmController3Brain)
							{
								Point3D point = new Point3D(npc.X, npc.Y, npc.Z);
								if (!Body.IsWithinRadius(npc, 50))
									Body.WalkTo(point, 100);
								else
									Body.Die(npc);
							}
						}
					}
					else
                    {
						if (Body.Model == 1)
						{
							foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
							{
								if (npc.Brain is ParthananFarmController3Brain)
								{
									if (Body.IsWithinRadius(npc, 50))
										Body.Die(npc);
								}
							}
						}
                    }
				}
				///
				/////////////////////////////////////////////// 2nd farm
				///
				if (Body.PackageID == "ParthananLoughGur2")
				{
					INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164845);
					if (ParthananFarmController3bBrain.LoughGur2Boss == 1)
					{
						if (changed == false)
						{
							oldFlags = Body.Flags;
							Body.Flags ^= GameNPC.eFlags.CANTTARGET;
							Body.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
							Body.Flags ^= GameNPC.eFlags.PEACE;

							if (oldModel == 0)
								oldModel = Body.Model;

							Body.Model = 1;
							changed = true;
						}
					}
					else
					{
						if (changed)
						{
							Body.Flags = (GameNPC.eFlags)npcTemplate.Flags;
							Body.Model = Convert.ToUInt16(npcTemplate.Model);
							changed = false;
						}
					}
					if (ParthananFarmController3bBrain.SacrificeParthanan3b && ParthananFarmController3bBrain.ParthansCanDie3b)
					{
						ClearAggroList();
						Body.StopAttack();
						foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
						{
							if (npc.Brain is ParthananFarmController3bBrain)
							{
								if (!Body.IsWithinRadius(npc, 50))
									Body.WalkTo(npc.X, npc.Y, npc.Z, Body.MaxSpeedBase);
								else
									Body.Die(npc);
							}
						}
					}
					else
					{
						if (Body.Model == 1)
						{
							foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
							{
								if (npc.Brain is ParthananFarmController3bBrain)
								{
									if (Body.IsWithinRadius(npc, 50))
										Body.Die(npc);
								}
							}
						}
					}
				}
				#endregion
			}
			base.Think();
		}
	}
}
#endregion

#region Parthanan Farm Controllers
#region Lough Derg
namespace DOL.GS
{
	public class ParthananFarmController1 : GameNPC
	{
		public ParthananFarmController1() : base()
		{
		}
		public override bool IsVisibleToPlayers => true;
		public override bool AddToWorld()
		{
			Name = "Parthanan Farm Controller";
			GuildName = "DO NOT REMOVE";
			Level = 50;
			Model = 665;
			RespawnInterval = 5000;
			Flags = (GameNPC.eFlags)28;

			ParthananFarmController1Brain sbrain = new ParthananFarmController1Brain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}

namespace DOL.AI.Brain
{
	public class ParthananFarmController1Brain : StandardMobBrain
	{
		private static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ParthananFarmController1Brain()
			: base()
		{
			AggroLevel = 0; //neutral
			AggroRange = 0;
			ThinkInterval = 1000;
		}
		public static bool BossIsUP = false;
		public static int ParthanansKilledFarm1 = 0; // Lough Derg
		public static bool SacrificeParthanan1 = false;
		public static int LoughDergBoss = 0;
		public static List<GameNPC> MinParthAround = new List<GameNPC>();
		public static bool ParthansCanDie = false;

		public static int MobsToKillLoughDerg = 60;
		public static int m_mobstokillloughderg
		{
			get { return m_mobstokillloughderg; }
			set { m_mobstokillloughderg = value; }
		}
		public override void Think()
		{
			if (ParthanansKilledFarm1 >= MobsToKillLoughDerg)
			{
				SacrificeParthanan1 = true;
				if (SacrificeParthanan1)
				{
					foreach(GameNPC npc in Body.GetNPCsInRadius(3000))
                    {
						if (npc != null && npc.IsAlive && npc.Brain is ParthananBrain && npc.PackageID == "ParthananLoughDerg" && !MinParthAround.Contains(npc))
							MinParthAround.Add(npc);
                    }
					if(MinParthAround.Count >= 5)
						SpawnBigOne();
					if(MinParthAround.Count >= 5)
						ParthansCanDie = true;
				}
			}
			if (SacrificeParthanan1 && MinParthAround.Count == 0 && BossIsUP)
			{
				SacrificeParthanan1 = false;
				LoughDergBoss = 1;
			}

			base.Think();
		}
		public void SpawnBigOne()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is AmalgamateParthananBrain && npc.PackageID == "ParthananBossLoughDerg")
					return;
			}
			AmalgamateParthanan boss = new AmalgamateParthanan();
			boss.X = Body.X;
			boss.Y = Body.Y;
			boss.Z = Body.Z;
			boss.Heading = Body.Heading;
			boss.CurrentRegion = Body.CurrentRegion;
			boss.PackageID = "ParthananBossLoughDerg";
			boss.AddToWorld();
			ParthanansKilledFarm1 = 0;
			BossIsUP = true;
		}
	}
}
#endregion
#region Connacht
namespace DOL.GS
{
	public class ParthananFarmController2 : GameNPC
	{
		public ParthananFarmController2() : base()
		{
		}
		public override bool IsVisibleToPlayers => true;
		public override bool AddToWorld()
		{
			Name = "Parthanan Farm Controller";
			GuildName = "DO NOT REMOVE";
			Level = 50;
			Model = 665;
			RespawnInterval = 5000;
			Flags = (GameNPC.eFlags)28;

			ParthananFarmController2Brain sbrain = new ParthananFarmController2Brain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}

namespace DOL.AI.Brain
{
	public class ParthananFarmController2Brain : StandardMobBrain
	{
		private static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ParthananFarmController2Brain()
			: base()
		{
			AggroLevel = 0; //neutral
			AggroRange = 0;
			ThinkInterval = 1000;
		}
		public static bool BossIsUP2 = false;
		public static int ParthanansKilledFarm2 = 0; // Connacht
		public static bool SacrificeParthanan2 = false;
		public static int ConnachtBoss = 0;
		public static List<GameNPC> MinParthAround2 = new List<GameNPC>();
		public static bool ParthansCanDie2 = false;

		public static int MobsToKillConnacht = 60;
		public static int m_mobstokillconnacht
		{
			get { return m_mobstokillconnacht; }
			set { m_mobstokillconnacht = value; }
		}
		public override void Think()
		{
			if (ParthanansKilledFarm2 >= MobsToKillConnacht)
			{
				SacrificeParthanan2 = true;
				if (SacrificeParthanan2)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(3000))
					{
						if (npc != null && npc.IsAlive && npc.Brain is ParthananBrain && npc.PackageID == "ParthananConnacht" && !MinParthAround2.Contains(npc))
							MinParthAround2.Add(npc);
					}
					if (MinParthAround2.Count >= 5)
						SpawnBigOne();
					if (MinParthAround2.Count >= 5)
						ParthansCanDie2 = true;
				}
			}
			if (SacrificeParthanan2 && MinParthAround2.Count == 0 && BossIsUP2)
			{
				SacrificeParthanan2 = false;
				ConnachtBoss = 1;
			}
			base.Think();
		}
		public void SpawnBigOne()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is AmalgamateParthananBrain && npc.PackageID == "ParthananBossConnacht")
					return;
			}
			AmalgamateParthanan boss = new AmalgamateParthanan();
			boss.X = Body.X;
			boss.Y = Body.Y;
			boss.Z = Body.Z;
			boss.Heading = Body.Heading;
			boss.CurrentRegion = Body.CurrentRegion;
			boss.PackageID = "ParthananBossConnacht";
			boss.AddToWorld();
			ParthanansKilledFarm2 = 0;
			BossIsUP2 = true;
		}
	}
}
/// <summary>
/// /////////////////////////////////////////////////////2nd farm
/// </summary>
namespace DOL.GS
{
	public class ParthananFarmController2b : GameNPC
	{
		public ParthananFarmController2b() : base()
		{
		}
		public override bool IsVisibleToPlayers => true;
		public override bool AddToWorld()
		{
			Name = "Parthanan Farm Controller";
			GuildName = "DO NOT REMOVE";
			Level = 50;
			Model = 665;
			RespawnInterval = 5000;
			Flags = (GameNPC.eFlags)28;

			ParthananFarmController2bBrain sbrain = new ParthananFarmController2bBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}

namespace DOL.AI.Brain
{
	public class ParthananFarmController2bBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ParthananFarmController2bBrain()
			: base()
		{
			AggroLevel = 0; //neutral
			AggroRange = 0;
			ThinkInterval = 1000;
		}
		public static bool BossIsUP2b = false;
		public static int ParthanansKilledFarm2b = 0; // Connacht
		public static bool SacrificeParthanan2b = false;
		public static int Connacht2Boss = 0;
		public static List<GameNPC> MinParthAround2b = new List<GameNPC>();
		public static bool ParthansCanDie2b = false;

		public static int MobsToKillConnacht2 = 60;
		public static int m_mobstokillconnacht2
		{
			get { return m_mobstokillconnacht2; }
			set { m_mobstokillconnacht2 = value; }
		}
		public override void Think()
		{
			if (ParthanansKilledFarm2b >= MobsToKillConnacht2)
			{
				SacrificeParthanan2b = true;
				if(SacrificeParthanan2b)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(3000))
					{
						if (npc != null && npc.IsAlive && npc.Brain is ParthananBrain && npc.PackageID == "ParthananConnacht2" && !MinParthAround2b.Contains(npc))
							MinParthAround2b.Add(npc);
					}
					if (MinParthAround2b.Count >= 5)
						SpawnBigOne();
					if (MinParthAround2b.Count >= 5)
						ParthansCanDie2b = true;
				}
			}
			if (SacrificeParthanan2b && MinParthAround2b.Count == 0 && BossIsUP2b)
			{
				SacrificeParthanan2b = false;
				Connacht2Boss = 1;
			}
			base.Think();
		}
		public void SpawnBigOne()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is AmalgamateParthananBrain && npc.PackageID == "ParthananBossConnacht2")
					return;
			}
			AmalgamateParthanan boss = new AmalgamateParthanan();
			boss.X = Body.X;
			boss.Y = Body.Y;
			boss.Z = Body.Z;
			boss.Heading = Body.Heading;
			boss.CurrentRegion = Body.CurrentRegion;
			boss.PackageID = "ParthananBossConnacht2";
			boss.AddToWorld();
			ParthanansKilledFarm2b = 0;
			BossIsUP2b = true;
		}
	}
}
#endregion
#region Lough Gur
namespace DOL.GS
{
	public class ParthananFarmController3 : GameNPC
	{
		public ParthananFarmController3() : base()
		{
		}
		public override bool IsVisibleToPlayers => true;
		public override bool AddToWorld()
		{
			Name = "Parthanan Farm Controller";
			GuildName = "DO NOT REMOVE";
			Level = 50;
			Model = 665;
			RespawnInterval = 5000;
			Flags = (GameNPC.eFlags)28;

			ParthananFarmController3Brain sbrain = new ParthananFarmController3Brain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}

namespace DOL.AI.Brain
{
	public class ParthananFarmController3Brain : StandardMobBrain
	{
		private static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ParthananFarmController3Brain()
			: base()
		{
			AggroLevel = 0; //neutral
			AggroRange = 0;
			ThinkInterval = 1000;
		}
		public static bool BossIsUP3 = false;
		public static int ParthanansKilledFarm3 = 0; // Lough Gur
		public static bool SacrificeParthanan3 = false;
		public static int LoughGurBoss = 0;
		public static List<GameNPC> MinParthAround3 = new List<GameNPC>();
		public static bool ParthansCanDie3 = false;

		public static int MobsToKillLoughGur = 60;
		public static int m_mobstokillloughgur
		{
			get { return m_mobstokillloughgur; }
			set { m_mobstokillloughgur = value; }
		}
		public override void Think()
		{
			//log.Warn("MinParthAround3 = " + MinParthAround3.Count + ", SacrificeParthanan3 = " + SacrificeParthanan3 + ", BossIsUP3 = " + BossIsUP3);
			if(ParthanansKilledFarm3 >= MobsToKillLoughGur)
            {
				SacrificeParthanan3 = true;
				if(SacrificeParthanan3)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(3000))
					{
						if (npc != null && npc.IsAlive && npc.Brain is ParthananBrain && npc.PackageID == "ParthananLoughGur" && !MinParthAround3.Contains(npc))
							MinParthAround3.Add(npc);
					}
					if (MinParthAround3.Count >= 5)
						SpawnBigOne();
					if (MinParthAround3.Count >= 5)
						ParthansCanDie3 = true;
				}
			}
			if (SacrificeParthanan3 && MinParthAround3.Count == 0 && BossIsUP3)
			{
				SacrificeParthanan3 = false;
				LoughGurBoss = 1;
			}
			base.Think();
		}
		public void SpawnBigOne()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is AmalgamateParthananBrain && npc.PackageID == "ParthananBossLoughGur")
					return;
			}
			AmalgamateParthanan boss = new AmalgamateParthanan();
			boss.X = Body.X;
			boss.Y = Body.Y;
			boss.Z = Body.Z;
			boss.Heading = Body.Heading;
			boss.CurrentRegion = Body.CurrentRegion;
			boss.PackageID = "ParthananBossLoughGur";
			boss.AddToWorld();
			ParthanansKilledFarm3 = 0;
			BossIsUP3 = true;
		}
	}
}
///////////////////////////////////////////////////////// 2nd farm

namespace DOL.GS
{
	public class ParthananFarmController3b : GameNPC
	{
		public ParthananFarmController3b() : base()
		{
		}
		public override bool IsVisibleToPlayers => true;
		public override bool AddToWorld()
		{
			Name = "Parthanan Farm Controller";
			GuildName = "DO NOT REMOVE";
			Level = 50;
			Model = 665;
			RespawnInterval = 5000;
			Flags = (GameNPC.eFlags)28;

			ParthananFarmController3bBrain sbrain = new ParthananFarmController3bBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}

namespace DOL.AI.Brain
{
	public class ParthananFarmController3bBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ParthananFarmController3bBrain()
			: base()
		{
			AggroLevel = 0; //neutral
			AggroRange = 0;
			ThinkInterval = 1000;
		}

		public static bool BossIsUP3b = false;
		public static int ParthanansKilledFarm3b = 0; // Lough Gur
		public static bool SacrificeParthanan3b = false;
		public static int LoughGur2Boss = 0;
		public static List<GameNPC> MinParthAround3b = new List<GameNPC>();
		public static bool ParthansCanDie3b = false;

		public static int MobsToKillLoughGur2 = 60;
		public static int m_mobstokillloughgur2
		{
			get { return m_mobstokillloughgur2; }
			set { m_mobstokillloughgur2 = value; }
		}
		public override void Think()
		{
			if (ParthanansKilledFarm3b >= MobsToKillLoughGur2)
			{
				SacrificeParthanan3b = true;
				if(SacrificeParthanan3b)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(3000))
					{
						if (npc != null && npc.IsAlive && npc.Brain is ParthananBrain && npc.PackageID == "ParthananLoughGur2" && !MinParthAround3b.Contains(npc))
							MinParthAround3b.Add(npc);
					}
					if (MinParthAround3b.Count >= 5)
						SpawnBigOne();
					if (MinParthAround3b.Count >= 5)
						ParthansCanDie3b = true;
				}
			}
			if (SacrificeParthanan3b && MinParthAround3b.Count == 0 && BossIsUP3b)
			{
				SacrificeParthanan3b = false;
				LoughGur2Boss = 1;
			}
			base.Think();
		}
		public void SpawnBigOne()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is AmalgamateParthananBrain && npc.PackageID == "ParthananBossLoughGur2")
					return;
			}
			AmalgamateParthanan boss = new AmalgamateParthanan();
			boss.X = Body.X;
			boss.Y = Body.Y;
			boss.Z = Body.Z;
			boss.Heading = Body.Heading;
			boss.CurrentRegion = Body.CurrentRegion;
			boss.PackageID = "ParthananBossLoughGur2";
			boss.AddToWorld();
			ParthanansKilledFarm3b = 0;
			BossIsUP3b = true;
		}
	}
}
#endregion
#endregion