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
			Spells = [QuillanMuireBrain.DD, QuillanMuireBrain.DD2];

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
				int pulledFriends = PullFriends("QuillanBaf", 4000);
				pulledFriends += PullFriends(npc => npc.Brain is MuireHerbalistBrain, 4000);

				if (pulledFriends > 0)
					Message.MessageToArea(Body, "Quillan Muire calls out, 'Family! Rise and defend our tomb!'", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
			}
			base.Think();
		}
		#region Spells
		internal static Spell DD => ScriptSpells.GetOrCreate("QuillanMuireDD", 20, static spell =>
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
		internal static Spell DD2 => ScriptSpells.GetOrCreate("QuillanMuireDD2", 20, static spell =>
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
			Spells = [MuireHerbalistBrain.Heal, MuireHerbalistBrain.StrengthBuff];
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
		private bool _healAnnounced;

		public MuireHerbalistBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
		}

		public override void Think()
		{
			if (!HasAggro)
				_healAnnounced = false;
			else
			{
				// Defensive spells are only checked by out of combat states, so a fighting herbalist has to check them itself.
				// Stopping the attack mirrors what `AttackMostWanted` does for offensive spells, and lets the cast actually start.
				if (CheckSpells(eCheckSpellType.Defensive))
					Body.StopAttack();

				if (!_healAnnounced && Body.castingComponent.SpellHandler?.Spell.SpellType is eSpellType.Heal)
				{
					_healAnnounced = true;
					Message.MessageToArea(Body, "The Muire herbalist chants over the wounded, and torn flesh knits closed!", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
				}
			}

			base.Think();
        }

		protected override GameLiving FindTargetForDefensiveSpell(Spell spell)
		{
			switch (spell.SpellType)
			{
				case eSpellType.Heal:
					return FindWoundedFriend();
				case eSpellType.StrengthBuff:
					return FindBuffTarget();
				default:
					return base.FindTargetForDefensiveSpell(spell);
			}
		}

		private GameLiving FindWoundedFriend()
		{
			if (Body.Faction == null)
				return null;

			List<GameNPC> wounded = new();

			foreach (GameNPC npc in Body.GetNPCsInRadius(1500))
			{
				if (npc.IsAlive && npc.Faction == Body.Faction && npc.HealthPercent < 50)
					wounded.Add(npc);
			}

			return wounded.Count > 0 ? wounded[Util.Random(0, wounded.Count - 1)] : null;
		}

		private GameLiving FindBuffTarget()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(500))
			{
				if (npc.IsAlive && (npc.Name == "Muire Hero" || npc.Name == "Muire Champion" || npc.Name == "Quillan Muire")
					&& !npc.effectListComponent.ContainsEffectForEffectType(eEffect.StrengthBuff))
				{
					return npc;
				}
			}

			return !Body.effectListComponent.ContainsEffectForEffectType(eEffect.StrengthBuff) ? Body : null;
		}

		#region Spells
		internal static Spell Heal => ScriptSpells.GetOrCreate("MuireHerbalistHeal", 15, static spell =>
		{
			spell.CastTime = 3;
			spell.RecastDelay = 8;
			spell.ClientEffect = 1340;
			spell.Icon = 1340;
			spell.TooltipId = 1340;
			spell.Value = 150;
			spell.Name = "Heal";
			spell.Range = 1500;
			spell.SpellID = 11970;
			spell.Target = eSpellTarget.REALM.ToString();
			spell.Type = eSpellType.Heal.ToString();
			spell.Uninterruptible = false;
			spell.MoveCast = false;
		});
		internal static Spell StrengthBuff => ScriptSpells.GetOrCreate("MuireHerbalistBuffSTR", 15, static spell =>
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
