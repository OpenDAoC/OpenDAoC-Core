using DOL.AI.Brain;
using DOL.GS;
using System.Collections.Generic;

namespace DOL.GS
{
	public class HrimthursaIcetouch : GameEpicNPC
	{
		public HrimthursaIcetouch() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162231);
			LoadTemplate(npcTemplate);

			HrimthursaIcetouchBrain sbrain = new HrimthursaIcetouchBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			return base.AddToWorld();
		}
	}
}
namespace DOL.AI.Brain
{
	public class HrimthursaIcetouchBrain : StandardMobBrain
	{
		public HrimthursaIcetouchBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		private GameNPC HealNpc;
		public override void Think()
		{
			if (Body.IsAlive)
			{
				#region Heal mobs
				if (HealNpc != null && (!HealNpc.IsAlive || HealNpc.HealthPercent >= 80 || !HealNpc.IsWithinRadius(Body, 1000)))
					HealNpc = null;

				if (HealNpc == null && Body.Faction != null)
				{
					List<GameNPC> npcToHeal = new List<GameNPC>();

					foreach (GameNPC npc in Body.GetNPCsInRadius(1000))
					{
						if (npc.IsAlive && npc.Faction == Body.Faction && npc.HealthPercent < 80)//add here mobs to heal
							npcToHeal.Add(npc);
					}

					if (npcToHeal.Count > 0)
						HealNpc = npcToHeal[Util.Random(0, npcToHeal.Count - 1)];//pick randomly mob that need to be healed
				}

				if (HealNpc != null && !Body.IsCasting)//start heal
				{
					GameObject oldTarget = Body.TargetObject;
					Body.TargetObject = HealNpc;
					Body.CastSpell(IcetouchHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
					Body.TargetObject = oldTarget;
				}
				#endregion

				if (HasAggro && Body.TargetObject != null)
				{
					TryCastSpell(IcetouchMezz, 30, eEffect.Mez);
					TryCastSpell(IcetouchRoot, 30);
				}
			}
			base.Think();
		}
		#region Spells
		private static Spell IcetouchHeal => ScriptSpells.GetOrCreate("HrimthursaHeal", 60, static spell =>
		{
			spell.CastTime = 4;
			spell.Power = 0;
			spell.RecastDelay = 0;
			spell.ClientEffect = 4659;
			spell.Icon = 4659;
			spell.Value = 500;
			spell.Name = "Glacier Healing";
			spell.Range = 1500;
			spell.SpellID = 11966;
			spell.Target = eSpellTarget.REALM.ToString();
			spell.Type = eSpellType.Heal.ToString();
			spell.Uninterruptible = true;
			spell.MoveCast = true;
		});
		private static Spell IcetouchMezz => ScriptSpells.GetOrCreate("HrimthursaMezz", 60, static spell =>
		{
			spell.CastTime = 3;
			spell.Power = 0;
			spell.RecastDelay = 0;
			spell.ClientEffect = 4678;
			spell.Icon = 4678;
			spell.TooltipId = 4678;
			spell.Duration = 80;
			spell.Name = "Unmake Mind";
			spell.Message1 = "You are mesmerized!";
			spell.Message2 = "{0} is mesmerized!";
			spell.Message3 = "You recover from the mesmerize.";
			spell.Message4 = "{0} recovers from the mesmerize.";
			spell.Range = 1500;
			spell.SpellID = 11967;
			spell.Target = eSpellTarget.ENEMY.ToString();
			spell.Type = eSpellType.Mesmerize.ToString();
			spell.DamageType = (int)eDamageType.Energy;
			spell.Uninterruptible = true;
			spell.MoveCast = true;
		});
		private static Spell IcetouchRoot => ScriptSpells.GetOrCreate("HrimthursaRoot", 60, static spell =>
		{
			spell.CastTime = 0;
			spell.Power = 0;
			spell.RecastDelay = 30;
			spell.ClientEffect = 177;
			spell.Icon = 177;
			spell.TooltipId = 177;
			spell.Duration = 80;
			spell.Value = 99;
			spell.Name = "Anchor Of Ice";
			spell.Message1 = "Your feet are frozen to the ground!";
			spell.Message2 = "{0}'s feet are frozen to the ground!";
			spell.Range = 1500;
			spell.SpellID = 11968;
			spell.Target = eSpellTarget.ENEMY.ToString();
			spell.Type = eSpellType.SpeedDecrease.ToString();
			spell.DamageType = (int)eDamageType.Cold;
			spell.Uninterruptible = true;
			spell.MoveCast = true;
		});
		#endregion
	}
}
