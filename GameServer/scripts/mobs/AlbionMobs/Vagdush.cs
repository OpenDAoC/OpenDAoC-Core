using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class Vagdush : GameNPC
	{
		public Vagdush() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12742);
			LoadTemplate(npcTemplate);

			VagdushBrain sbrain = new VagdushBrain();
			if (NPCTemplate != null)
			{
				sbrain.AggroLevel = NPCTemplate.AggroLevel;
				sbrain.AggroRange = NPCTemplate.AggroRange;
			}
			SetOwnBrain(sbrain);
			return base.AddToWorld();
		}
		public override void Die(GameObject killer)
		{
			switch (Util.Random(1, 2))
			{
				case 1:
					SpawnPoint.X = 421759;
					SpawnPoint.Y = 650509;
					SpawnPoint.Z = 3933;
					Heading = 3842;
					break;
				case 2:
					SpawnPoint.X = 421716;
					SpawnPoint.Y = 658478;
					SpawnPoint.Z = 4196;
					Heading = 2164;
					break;
			}
			base.Die(killer);
		}
	}
}
namespace DOL.AI.Brain
{
	public class VagdushBrain : StandardMobBrain
	{
		public VagdushBrain() : base()
		{
			ThinkInterval = 1500;
		}
		private bool CallforHelp = false;
		private bool Rooted = false;
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				CallforHelp = false;
				Body.MaxSpeedBase = Body.NPCTemplate.MaxSpeed;
				Rooted = false;
			}

			if (HasAggro && Body.TargetObject != null)
			{
				if (!CallforHelp)
				{
					if (Body.HealthPercent <= 10)
					{
						if (PullFriends("VagdushBaf", 1500) > 0)
							Message.MessageToArea(Body, "Vagdush snarls, 'Kill them all! Attack!'", eChatType.CT_Say, eChatLoc.CL_ChatWindow, 3000);
						CallforHelp = true;
					}
				}
				if (Body.TargetObject is GameLiving target && target.IsAlive && !target.IsWithinRadius(Body, Body.attackComponent.AttackRange))
				{
					if (!Rooted)
					{
						Message.MessageToArea(Body, "Vagdush plants his feet and snarls, channeling a powerful curse!", eChatType.CT_Say, eChatLoc.CL_ChatWindow, 3000);
						Rooted = true;
					}
					Body.MaxSpeedBase = 0;
					if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.Disease))
						TryCastSpell(VagdushDisease, 100);
					else
						TryCastSpell(VagdushDD, 100);
				}
				else
				{
					Body.MaxSpeedBase = Body.NPCTemplate.MaxSpeed;
					Rooted = false;
				}
			}
			base.Think();
		}
		#region Spells
		private static Spell VagdushDisease => ScriptSpells.GetOrCreate("VagdushDisease", 10, db =>
		{
			db.CastTime = 2;
			db.RecastDelay = 0;
			db.ClientEffect = 731;
			db.Icon = 731;
			db.TooltipId = 731;
			db.Name = "Persistent Disease";
			db.Description = "Inflicts a wasting disease on the target that slows it, weakens it, and inhibits heal spells.";
			db.Message1 = "You are diseased!";
			db.Message2 = "{0} is diseased!";
			db.Message3 = "You look healthy.";
			db.Message4 = "{0} looks healthy again.";
			db.Range = 1500;
			db.Duration = 60;
			db.SpellID = 11986;
			db.Target = eSpellTarget.ENEMY.ToString();
			db.Type = "Disease";
			db.DamageType = (int)eDamageType.Body; //Energy DMG Type
		});
		private static Spell VagdushDD => ScriptSpells.GetOrCreate("VagdushDD", 10, db =>
		{
			db.CastTime = 3;
			db.RecastDelay = 0;
			db.ClientEffect = 754;
			db.Icon = 754;
			db.Name = "Vagdush Blast";
			db.Damage = 50;
			db.Range = 1500;
			db.SpellID = 11987;
			db.Target = eSpellTarget.ENEMY.ToString();
			db.Type = eSpellType.DirectDamageNoVariance.ToString();
			db.DamageType = (int)eDamageType.Matter;
		});
		#endregion
	}
}
