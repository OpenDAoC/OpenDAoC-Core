using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.Language;


namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// 
	/// </summary>
	public class RAStatEnhancer : L5RealmAbility
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		eProperty m_property = eProperty.Undefined;

		public eProperty Property
		{
			get { return m_property; }
		}

		public RAStatEnhancer(DbAbility dba, int level, eProperty property)
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
					list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "RAStatEnhancer.DelveInfo.Info1", i, GetAmountForLevel(i)));
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
			// Stagger the update and delegate to the effect list component to avoid redundant packets and solve a case of deadlock.
			// We're typically holding an ability lock here, and sending a weapon/armor update requires an inventory lock.
			// But threads holding a lock on inventory seems to be able to request a lock on abilities too (encumbrance update maybe?).
			target.effectListComponent.RequestPlayerUpdate(EffectHelper.PlayerUpdate.Encumbrance | EffectHelper.PlayerUpdate.WeaponArmor | EffectHelper.PlayerUpdate.Stats | EffectHelper.PlayerUpdate.Status);
		}

		public override void Activate(GameLiving living, bool sendUpdates)
		{
			if (m_activeLiving == null)
			{
				living.AbilityBonus[m_property] += GetAmountForLevel(Level);
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
				living.AbilityBonus[m_property] -= GetAmountForLevel(Level);
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

			m_activeLiving.AbilityBonus[m_property] += GetAmountForLevel(newLevel) - GetAmountForLevel(oldLevel);
			SendUpdates(m_activeLiving);
		}
	}

	public class RAStrengthEnhancer : RAStatEnhancer
	{
		public RAStrengthEnhancer(DbAbility dba, int level) : base(dba, level, eProperty.Strength) { }
	}

	public class RAConstitutionEnhancer : RAStatEnhancer
	{
		public RAConstitutionEnhancer(DbAbility dba, int level) : base(dba, level, eProperty.Constitution) { }
	}

	public class RAQuicknessEnhancer : RAStatEnhancer
	{
		public RAQuicknessEnhancer(DbAbility dba, int level) : base(dba, level, eProperty.Quickness) { }
	}

	public class RADexterityEnhancer : RAStatEnhancer
	{
		public RADexterityEnhancer(DbAbility dba, int level) : base(dba, level, eProperty.Dexterity) { }
	}

	public class RAAcuityEnhancer : RAStatEnhancer
	{
		public RAAcuityEnhancer(DbAbility dba, int level) : base(dba, level, eProperty.Acuity) { }
	}

	public class RAMaxManaEnhancer : RAStatEnhancer
	{
		public RAMaxManaEnhancer(DbAbility dba, int level) : base(dba, level, eProperty.MaxMana) { }
	}

	public class RAMaxHealthEnhancer : RAStatEnhancer
	{
		public RAMaxHealthEnhancer(DbAbility dba, int level) : base(dba, level, eProperty.MaxHealth) { }

		public override bool CheckRequirement(GamePlayer player)
		{
			return player.Level >= 40;
		}
	}
	
	public class RAEndRegenEnhancer : RAStatEnhancer
	{
		public RAEndRegenEnhancer(DbAbility dba, int level) : base(dba, level, eProperty.EnduranceRegenerationAmount) { }
	}
}