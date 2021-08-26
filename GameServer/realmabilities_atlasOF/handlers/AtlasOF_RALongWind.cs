using System;
using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.RealmAbilities
{
    /// <summary>
    /// Long Wind : Decreases the amount of endurance taken per tick when sprinting, by the number listed.
    /// </summary>
    public class AtlasOF_RALongWind : RAPropertyEnhancer
    {
		
        public AtlasOF_RALongWind(DBAbility dba, int level) : base(dba, level, eProperty.Undefined) { }

        protected override string ValueUnit { get { return "%"; } }

        public override int GetAmountForLevel(int level)
        {
            
            switch (level)
            {
                case 1: return 20;
                case 2: return 40;
                case 3: return 60;
                case 4: return 80;
                case 5: return 100;
                default: return 0;
            }
        }
        

		/*
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		eProperty m_property = eProperty.EnduranceRegenerationRate;

		public eProperty Property
		{
			get { return m_property; }
		}

		public AtlasOF_RALongWind(DBAbility dba, int level, eProperty property)
			: base(dba, level, eProperty.EnduranceRegenerationRate)
		{
			m_property = property;
		}

		protected override string ValueUnit { get { return "%"; } }

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add(m_description);
				list.Add("");
				for (int i = 1; i <= MaxLevel; i++)
				{
					// TODO: Check if this translations are kept in language files (GameServer/language/{LANG}/RealmAbilities.txt
					list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "RAStatEnhancer.DelveInfo.Info1", i, GetAmountForLevel(i)));
				}
				return list;
			}
		}
		public override int CostForUpgrade(int level)
		{
			switch (level)
			{
				case 1: return 1;
				case 2: return 3;
				case 3: return 6;
				case 4: return 10;
				case 5: return 14;
				default: return 1000;
			}
		}
		*/
		/*
		public virtual int GetAmountForLevel(int level)
		{
			if (level < 1) return 0;

			switch (level)
			{
				case 1: return 20;
				case 2: return 40;
				case 3: return 60;
				case 4: return 80;
				case 5: return 100;
				default: return 0;
			}
		}

		/// <summary>
		/// send updates about the changes
		/// </summary>
		/// <param name="target"></param>
		public virtual void SendUpdates(GameLiving target)
		{
			GamePlayer player = target as GamePlayer;   // need new prop system to not worry about updates
			if (player != null)
			{
				player.Out.SendCharStatsUpdate();
				player.Out.SendUpdateWeaponAndArmorStats();
				player.UpdateEncumberance();
				player.UpdatePlayerStatus();
			}

			if (target.IsAlive)
			{
				if (target.Endurance < target.MaxEndurance) target.StartEnduranceRegeneration();
				else if (target.Endurance > target.MaxEndurance) target.Endurance = target.MaxEndurance;
			}
		}
		*/
		/*
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
		*/
	}
}
