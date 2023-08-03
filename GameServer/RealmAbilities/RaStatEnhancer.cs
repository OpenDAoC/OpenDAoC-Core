using System.Collections.Generic;
using DOL.Database;
using DOL.GS;
using DOL.GS.RealmAbilities;
using DOL.Language;

namespace Core.GS.RealmAbilities
{
	/// <summary>
	/// 
	/// </summary>
	public class RaStatEnhancer : L5RealmAbility
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		EProperty m_property = EProperty.Undefined;

		public EProperty Property
		{
			get { return m_property; }
		}

		public RaStatEnhancer(DbAbilities dba, int level, EProperty property)
			: base(dba, level)
		{
			m_property = property;
		}

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add(m_description);
				list.Add("");
				for (int i = 1; i <= MaxLevel; i++)
				{
					list.Add(LanguageMgr.GetTranslation(DOL.GS.ServerProperties.ServerProperties.SERV_LANGUAGE, "RAStatEnhancer.DelveInfo.Info1", i, GetAmountForLevel(i)));
				}
				return list;
			}
		}
		public override int CostForUpgrade(int level)
		{
				switch (level)
				{
					case 0: return 1;
					case 1: return 3;
					case 2: return 6;
					case 3: return 10;
					case 4: return 14;
					default: return 1000;
				}
		}
		public virtual int GetAmountForLevel(int level)
		{
			if (level < 1) return 0;

				switch (level)
				{
						case 1: return 6;
						case 2: return 12;
						case 3: return 18;
						case 4: return 24;
						case 5: return 30;
						default: return 30;
				}
		}

		/// <summary>
		/// send updates about the changes
		/// </summary>
		/// <param name="target"></param>
		public virtual void SendUpdates(GameLiving target)
		{
			GamePlayer player = target as GamePlayer;	// need new prop system to not worry about updates
			if (player != null)
			{
				player.Out.SendCharStatsUpdate();
				player.Out.SendUpdateWeaponAndArmorStats();
				player.UpdateEncumberance();
				player.UpdatePlayerStatus();
			}

			if (target.IsAlive)
			{
				if (target.Health < target.MaxHealth) target.StartHealthRegeneration();
				else if (target.Health > target.MaxHealth) target.Health = target.MaxHealth;

				if (target.Mana < target.MaxMana) target.StartPowerRegeneration();
				else if (target.Mana > target.MaxMana) target.Mana = target.MaxMana;

				if (target.Endurance < target.MaxEndurance) target.StartEnduranceRegeneration();
				else if (target.Endurance > target.MaxEndurance) target.Endurance = target.MaxEndurance;
			}
		}

		public override void Activate(GameLiving living, bool sendUpdates)
		{
			if (m_activeLiving == null)
			{
				living.AbilityBonus[(int)m_property] += GetAmountForLevel(Level);
				m_activeLiving = living;
				if (sendUpdates) SendUpdates(living);
			}
			else
			{
				log.Warn("ability " + Name + " already activated on " + living.Name);
			}
		}

		public override void Deactivate(GameLiving living, bool sendUpdates)
		{
			if (m_activeLiving != null)
			{
				living.AbilityBonus[(int)m_property] -= GetAmountForLevel(Level);
				if (sendUpdates) SendUpdates(living);
				m_activeLiving = null;
			}
			else
			{
				log.Warn("ability " + Name + " already deactivated on " + living.Name);
			}
		}

		public override void OnLevelChange(int oldLevel, int newLevel = 0)
		{
			if (newLevel == 0)
				newLevel = Level;

			m_activeLiving.AbilityBonus[(int)m_property] += GetAmountForLevel(newLevel) - GetAmountForLevel(oldLevel);
			SendUpdates(m_activeLiving);
		}
	}

	public class RaStrengthEnhancer : RaStatEnhancer
	{
		public RaStrengthEnhancer(DbAbilities dba, int level) : base(dba, level, EProperty.Strength) { }
	}

	public class RaConstitutionEnhancer : RaStatEnhancer
	{
		public RaConstitutionEnhancer(DbAbilities dba, int level) : base(dba, level, EProperty.Constitution) { }
	}

	public class RaQuicknessEnhancer : RaStatEnhancer
	{
		public RaQuicknessEnhancer(DbAbilities dba, int level) : base(dba, level, EProperty.Quickness) { }
	}

	public class RaDexterityEnhancer : RaStatEnhancer
	{
		public RaDexterityEnhancer(DbAbilities dba, int level) : base(dba, level, EProperty.Dexterity) { }
	}

	public class RaAcuityEnhancer : RaStatEnhancer
	{
		public RaAcuityEnhancer(DbAbilities dba, int level) : base(dba, level, EProperty.Acuity) { }
	}

	public class RaMaxPowerEnhancer : RaStatEnhancer
	{
		public RaMaxPowerEnhancer(DbAbilities dba, int level) : base(dba, level, EProperty.MaxMana) { }
	}

	public class RaMaxHealthEnhancer : RaStatEnhancer
	{
		public RaMaxHealthEnhancer(DbAbilities dba, int level) : base(dba, level, EProperty.MaxHealth) { }

		public override bool CheckRequirement(GamePlayer player)
		{
			return player.Level >= 40;
		}
	}
	
	public class RaEndRegenEnhancer : RaStatEnhancer
	{
		public RaEndRegenEnhancer(DbAbilities dba, int level) : base(dba, level, EProperty.EnduranceRegenerationRate) { }
	}
}