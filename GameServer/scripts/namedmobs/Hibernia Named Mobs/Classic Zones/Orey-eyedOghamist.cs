using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Movement;

namespace DOL.GS
{
	public class OreyEyedOghamist : GameEpicBoss
	{
		public OreyEyedOghamist() : base() { }

		private static readonly (int X, int Y, int Z)[] _patrolPoints =
		[
			(504754, 505351, 4939),
			(506042, 507443, 4945),
			(505194, 509109, 5052),
			(505401, 510887, 5382),
			(503682, 512545, 5423),
			(502505, 513021, 5204),
			(501470, 515019, 5120),
			(500667, 516402, 4848),
			(498310, 516387, 4923),
			(495586, 513451, 5510),
			(495006, 509166, 4991),
			(498351, 507021, 5059),
			(500833, 506164, 5074)
		];

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Orey-eyed Oghamist Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 40;// dmg reduction for melee dmg
				case eDamageType.Crush: return 40;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
				default: return 70;// dmg reduction for rest resists
			}
		}

		public override int MeleeAttackRange => 350;
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 30000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164703);
			LoadTemplate(npcTemplate);

            RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			CurrentPathPoint = MovementMgr.CreatePath(EPathType.Loop, MaxSpeedBase, _patrolPoints);
			Spells = [OreyEyedOghamistBrain.Bomb];
			OreyEyedOghamistBrain sbrain = new OreyEyedOghamistBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class OreyEyedOghamistBrain : StandardMobBrain
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public OreyEyedOghamistBrain() : base()
		{
			AggroLevel = 0;//he is neutral
			AggroRange = 800;
			ThinkInterval = 1500;
		}
        public override void Think()
		{
			if(Body.TargetObject != null && HasAggro)
            {
				GameLiving target = Body.TargetObject as GameLiving;
				if (!Body.IsWithinRadius(Body.TargetObject, 300))
				{
					if (!Body.IsCasting)
						Body.CastSpell(OreyDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				}
				else
				{
					if (target != null && target.IsAlive)
					{
						if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.StrConDebuff) && !Body.IsCasting && Util.Chance(25))
							Body.CastSpell(Orey_SC_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					}
				}
            }
            base.Think();
		}
		#region Spells
		internal static Spell Bomb => ScriptSpells.GetOrCreate("OreyBomb", 60, static spell =>
		{
			spell.CastTime = 5;
			spell.Power = 0;
			spell.RecastDelay = Util.Random(20, 30);
			spell.ClientEffect = 4369;
			spell.Icon = 4369;
			spell.Damage = 800;
			spell.DamageType = (int)eDamageType.Energy;
			spell.Name = "Energy Blast";
			spell.Range = 0;
			spell.Radius = 1000;
			spell.SpellID = 12012;
			spell.Target = eSpellTarget.ENEMY.ToString();
			spell.Uninterruptible = true;
			spell.Type = eSpellType.DirectDamageNoVariance.ToString();
		});
		private Spell m_Orey_SC_Debuff;
		private Spell Orey_SC_Debuff
		{
			get
			{
				if (m_Orey_SC_Debuff == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 30;
					spell.Duration = 60;
					spell.ClientEffect = 5408;
					spell.Icon = 5408;
					spell.Name = "Greater Infirmity";
					spell.TooltipId = 5408;
					spell.Range = 1000;
					spell.Value = 73;
					spell.Radius = 400;
					spell.SpellID = 12013;
					spell.Target = eSpellTarget.ENEMY.ToString();
					spell.Type = eSpellType.StrengthConstitutionDebuff.ToString();
					spell.DamageType = (int)eDamageType.Body;
					m_Orey_SC_Debuff = new Spell(spell, 60);
				}
				return m_Orey_SC_Debuff;
			}
		}
		private Spell m_OreyDD;
		private Spell OreyDD
		{
			get
			{
				if (m_OreyDD == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Power = 0;
					spell.RecastDelay = 3;
					spell.ClientEffect = 0;
					spell.Icon = 0;
					spell.Damage = 500;
					spell.DamageType = (int)eDamageType.Slash;
					spell.Name = "Ranged Melee Swing";
					spell.Range = 2200;
					spell.SpellID = 12014;
					spell.Target = eSpellTarget.ENEMY.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_OreyDD = new Spell(spell, 60);
				}
				return m_OreyDD;
			}
		}
		#endregion
	}
}

