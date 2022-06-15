using DOL.AI;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;

namespace DOL.GS
{
    public class QuillanMuire : GameNPC
	{
		public QuillanMuire() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165094);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			Faction = FactionMgr.GetFactionByID(782);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(782));

			QuillanMuireBrain sbrain = new QuillanMuireBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
    public class QuillanMuireBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public QuillanMuireBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if(HasAggro && Body.TargetObject != null)
            {
				if(!Body.IsCasting && Util.Chance(25))
					Body.CastSpell(QuillanMuire_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				if (!Body.IsCasting && Util.Chance(25))
					Body.CastSpell(QuillanMuire_DD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

				GameLiving target = Body.TargetObject as GameLiving;
				foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is MuireHerbalistBrain brian)
					{
						if (!brian.HasAggro && brian != null && target != null && target.IsAlive)
							brian.AddToAggroList(target, 10);
					}
				}
				foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
				{
					if (npc != null && npc.IsAlive && npc.PackageID == "QuillanBaf")
						AddAggroListTo(npc.Brain as StandardMobBrain); 
				}
			}
			base.Think();
		}
		#region Spells
		private Spell m_QuillanMuire_DD;
		private Spell QuillanMuire_DD
		{
			get
			{
				if (m_QuillanMuire_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3.5;
					spell.RecastDelay = Util.Random(10, 15);
					spell.ClientEffect = 14353;
					spell.Icon = 14353;
					spell.TooltipId = 14353;
					spell.Damage = 80;
					spell.Name = "Energy Blast";
					spell.Range = 1500;
					spell.SpellID = 11948;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Energy;
					m_QuillanMuire_DD = new Spell(spell, 20);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_QuillanMuire_DD);
				}
				return m_QuillanMuire_DD;
			}
		}
		private Spell m_QuillanMuire_DD2;
		private Spell QuillanMuire_DD2
		{
			get
			{
				if (m_QuillanMuire_DD2 == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3.5;
					spell.RecastDelay = Util.Random(8, 12);
					spell.ClientEffect = 4356;
					spell.Icon = 4356;
					spell.TooltipId = 4356;
					spell.Damage = 70;
					spell.Name = "Energy Blast";
					spell.Range = 1500;
					spell.SpellID = 11949;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Energy;
					m_QuillanMuire_DD2 = new Spell(spell, 20);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_QuillanMuire_DD2);
				}
				return m_QuillanMuire_DD2;
			}
		}
		#endregion
	}
}
#region Muire herbalist
namespace DOL.GS
{
    public class MuireHerbalist : GameNPC
	{
		public MuireHerbalist() : base() { }

		#region Stats
		public override short Constitution { get => base.Constitution; set => base.Constitution = 100; }
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 180; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 150; }
		#endregion
		public override bool AddToWorld()
		{
			Name = "Muire herbalist";
			Level = (byte)Util.Random(18, 19);
			Model = 446;
			Size = 52;
			Faction = FactionMgr.GetFactionByID(782);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(782));
			MuireHerbalistBrain sbrain = new MuireHerbalistBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
    }
}
namespace DOL.AI.Brain
{
    public class MuireHerbalistBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MuireHerbalistBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
        public override void AttackMostWanted()
        {
			if (Ishealing || IsBuffing || IsBuffingSelf)
				return;
			else
				base.AttackMostWanted();
        }
        private protected bool Ishealing = false;
		private protected bool IsBuffing = false;
		private protected bool IsBuffingSelf = false;
		private protected void HealAndBuff()
        {
			foreach (GameNPC npc in Body.GetNPCsInRadius(1500))
			{
				if (npc.IsAlive && npc != null && npc.Faction == Body.Faction)
				{
					foreach (Spell spell in Body.Spells)
					{
						if (spell != null)
						{
							if (npc.HealthPercent < 50)
							{
								Ishealing = true;
								if (!Body.IsCasting)
								{
									if (Body.TargetObject != npc)
										Body.TargetObject = npc;

									Body.CastSpell(MuireHerbalistHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
								}
							}
							if(Body.GetSkillDisabledDuration(MuireHerbalistHeal) > 0)
							{
								Ishealing = false;
								Body.TargetObject = null;
							}
						}
					}
				}
			}
			if (!Ishealing)
			{
				foreach (GameNPC npc in Body.GetNPCsInRadius(500))
				{
					if (npc != null && npc.IsAlive && (npc.Name == "Muire Hero" || npc.Name == "Muire Champion" || npc.Name == "Quillan Muire"))
					{
						if (!Body.IsCasting && !npc.effectListComponent.ContainsEffectForEffectType(eEffect.StrengthBuff))
						{
							IsBuffing = true;
							Body.TargetObject = npc;
							Body.CastSpell(MuireHerbalist_Buff_STR, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
						}
						else
						{
							Body.TargetObject = null;
							IsBuffing = false;
							if (!Body.IsCasting && !Body.effectListComponent.ContainsEffectForEffectType(eEffect.StrengthBuff))
							{
								IsBuffingSelf = true;
								Body.TargetObject = Body;
								Body.CastSpell(MuireHerbalist_Buff_STR, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
							}
							else
							{
								IsBuffingSelf = false;
								Body.TargetObject = null;
							}
						}
					}
				}
			}
		}

        public override void Think()
		{
			if(Body.IsAlive)
            {
				if (!Body.Spells.Contains(MuireHerbalistHeal))
					Body.Spells.Add(MuireHerbalistHeal);

			}
			HealAndBuff();
			base.Think();
        }
        #region Spells
        private Spell m_MuireHerbalistHeal;
		private Spell MuireHerbalistHeal
		{
			get
			{
				if (m_MuireHerbalistHeal == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 3;
					spell.ClientEffect = 1340;
					spell.Icon = 1340;
					spell.TooltipId = 1340;
					spell.Value = 150;
					spell.Name = "Heal";
					spell.Range = 1500;
					spell.SpellID = 11949;
					spell.Target = "Realm";
					spell.Type = eSpellType.Heal.ToString();
					spell.Uninterruptible = true;
					m_MuireHerbalistHeal = new Spell(spell, 15);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_MuireHerbalistHeal);
				}
				return m_MuireHerbalistHeal;
			}
		}
		private Spell m_MuireHerbalist_Buff_STR;
		private Spell MuireHerbalist_Buff_STR
		{
			get
			{
				if (m_MuireHerbalist_Buff_STR == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 1451;
					spell.Duration = 1200;
					spell.Icon = 1451;
					spell.TooltipId = 5003;
					spell.Value = 20;
					spell.Name = "Herbalist Strength";
					spell.Range = 1500;
					spell.SpellID = 11950;
					spell.Target = "Realm";
					spell.Type = eSpellType.StrengthBuff.ToString();
					spell.Uninterruptible = true;
					m_MuireHerbalist_Buff_STR = new Spell(spell, 15);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_MuireHerbalist_Buff_STR);
				}
				return m_MuireHerbalist_Buff_STR;
			}
		}
		#endregion
	}
}
#endregion