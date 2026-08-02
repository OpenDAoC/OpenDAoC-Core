using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;
using System.Collections.Generic;

namespace DOL.GS
{
    public class QuillanMuire : GameNPC
	{
		public QuillanMuire() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165094);
			LoadTemplate(npcTemplate);
			Faction = FactionMgr.GetFactionByID(782);

			QuillanMuireBrain sbrain = new QuillanMuireBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;
			SaveIntoDatabase();
			return base.AddToWorld();
		}
	}
}
namespace DOL.AI.Brain
{
    public class QuillanMuireBrain : StandardMobBrain
	{
		public QuillanMuireBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
		}
		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
			{
				TryCastSpell(QuillanMuire_DD, 25);
				TryCastSpell(QuillanMuire_DD2, 25);

				int pulledFriends = PullFriends("QuillanBaf", 4000);
				pulledFriends += PullFriends(npc => npc.Brain is MuireHerbalistBrain, 4000);

				if (pulledFriends > 0)
					Message.MessageToArea(Body, "Quillan Muire calls out, 'Family! Rise and defend our tomb!'", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
			}
			base.Think();
		}
		#region Spells
		private static Spell QuillanMuire_DD => ScriptSpells.GetOrCreate("QuillanMuireDD", 20, spell =>
		{
			spell.CastTime = 3.5;
			spell.RecastDelay = Util.Random(10, 15);
			spell.ClientEffect = 14353;
			spell.Icon = 14353;
			spell.TooltipId = 14353;
			spell.Damage = 80;
			spell.Name = "Energy Blast";
			spell.Range = 1500;
			spell.SpellID = 11948;
			spell.Target = eSpellTarget.ENEMY.ToString();
			spell.Type = eSpellType.DirectDamageNoVariance.ToString();
			spell.Uninterruptible = true;
			spell.MoveCast = true;
			spell.DamageType = (int)eDamageType.Energy;
		});
		private static Spell QuillanMuire_DD2 => ScriptSpells.GetOrCreate("QuillanMuireDD2", 20, spell =>
		{
			spell.CastTime = 3.5;
			spell.RecastDelay = Util.Random(8, 12);
			spell.ClientEffect = 4356;
			spell.Icon = 4356;
			spell.TooltipId = 4356;
			spell.Damage = 70;
			spell.Name = "Energy Blast";
			spell.Range = 1500;
			spell.SpellID = 11949;
			spell.Target = eSpellTarget.ENEMY.ToString();
			spell.Type = eSpellType.DirectDamageNoVariance.ToString();
			spell.Uninterruptible = true;
			spell.MoveCast = true;
			spell.DamageType = (int)eDamageType.Energy;
		});
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
			MuireHerbalistBrain sbrain = new MuireHerbalistBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;
			SaveIntoDatabase();
			return base.AddToWorld();
		}
    }
}
namespace DOL.AI.Brain
{
    public class MuireHerbalistBrain : StandardMobBrain
	{
		public MuireHerbalistBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
		}
		private GameNPC HealNpc;
		private GameNPC BuffNpc;
		private bool _healAnnounced;
		private void HealAndBuff()
		{
			if (HealNpc != null && (!HealNpc.IsAlive || HealNpc.HealthPercent >= 50 || !HealNpc.IsWithinRadius(Body, 1500)))
				HealNpc = null;

			if (HealNpc == null && Body.Faction != null)
			{
				List<GameNPC> npcToHeal = new List<GameNPC>();

				foreach (GameNPC npc in Body.GetNPCsInRadius(1500))
				{
					if (npc.IsAlive && npc.Faction == Body.Faction && npc.HealthPercent < 50)
						npcToHeal.Add(npc);
				}

				if (npcToHeal.Count > 0)
					HealNpc = npcToHeal[Util.Random(0, npcToHeal.Count - 1)];
			}

			if (HealNpc != null)
			{
				if (!Body.IsCasting)
				{
					GameObject oldTarget = Body.TargetObject;
					Body.TargetObject = HealNpc;
					Body.CastSpell(MuireHerbalistHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
					Body.TargetObject = oldTarget;

					if (!_healAnnounced)
					{
						Message.MessageToArea(Body, "The Muire herbalist chants over the wounded, and torn flesh knits closed!", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
						_healAnnounced = true;
					}
				}

				return;
			}

			if (BuffNpc != null && (!BuffNpc.IsAlive || !BuffNpc.IsWithinRadius(Body, 500) || BuffNpc.effectListComponent.ContainsEffectForEffectType(eEffect.StrengthBuff)))
				BuffNpc = null;

			if (BuffNpc == null)
			{
				foreach (GameNPC npc in Body.GetNPCsInRadius(500))
				{
					if (npc.IsAlive && (npc.Name == "Muire Hero" || npc.Name == "Muire Champion" || npc.Name == "Quillan Muire")
						&& !npc.effectListComponent.ContainsEffectForEffectType(eEffect.StrengthBuff))
					{
						BuffNpc = npc;
						break;
					}
				}

				if (BuffNpc == null && !Body.effectListComponent.ContainsEffectForEffectType(eEffect.StrengthBuff))
					BuffNpc = Body;
			}

			if (BuffNpc != null && !Body.IsCasting)
			{
				GameObject oldTarget = Body.TargetObject;
				Body.TargetObject = BuffNpc;
				Body.CastSpell(MuireHerbalist_Buff_STR, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				Body.TargetObject = oldTarget;
			}
		}

        public override void Think()
		{
			if (!HasAggro)
				_healAnnounced = false;

			if (Body.IsAlive)
				HealAndBuff();

			base.Think();
        }
        #region Spells
        private static Spell MuireHerbalistHeal => ScriptSpells.GetOrCreate("MuireHerbalistHeal", 15, spell =>
		{
			spell.CastTime = 3;
			spell.RecastDelay = 3;
			spell.ClientEffect = 1340;
			spell.Icon = 1340;
			spell.TooltipId = 1340;
			spell.Value = 150;
			spell.Name = "Heal";
			spell.Range = 1500;
			spell.SpellID = 11949;
			spell.Target = eSpellTarget.REALM.ToString();
			spell.Type = eSpellType.Heal.ToString();
			spell.Uninterruptible = false;
			spell.MoveCast = false;
		});
		private static Spell MuireHerbalist_Buff_STR => ScriptSpells.GetOrCreate("MuireHerbalistBuffSTR", 15, spell =>
		{
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
			spell.Target = eSpellTarget.REALM.ToString();
			spell.Type = eSpellType.StrengthBuff.ToString();
			spell.Uninterruptible = false;
			spell.MoveCast = false;
		});
		#endregion
	}
}
#endregion
