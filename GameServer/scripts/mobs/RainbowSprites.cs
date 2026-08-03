using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public abstract class RainbowSprite : GameNPC
	{
		protected abstract int TemplateId { get; }

		protected abstract RainbowSpriteBrain CreateBrain();

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(TemplateId);
			LoadTemplate(npcTemplate);

			RainbowSpriteBrain sbrain = CreateBrain();

			if (NPCTemplate != null)
			{
				sbrain.AggroLevel = NPCTemplate.AggroLevel;
				sbrain.AggroRange = NPCTemplate.AggroRange;
			}

			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}

	public class RainbowSpriteTan : RainbowSprite
	{
		protected override int TemplateId => 60165135;

		protected override RainbowSpriteBrain CreateBrain() => new RainbowSpriteTanBrain();
	}

	public class RainbowSpriteWhite : RainbowSprite
	{
		protected override int TemplateId => 50024;

		protected override RainbowSpriteBrain CreateBrain() => new RainbowSpriteWhiteBrain();
	}

	public class RainbowSpriteBlue : RainbowSprite
	{
		protected override int TemplateId => 60165136;

		protected override RainbowSpriteBrain CreateBrain() => new RainbowSpriteBlueBrain();
	}

	public class RainbowSpriteGreen : RainbowSprite
	{
		protected override int TemplateId => 50018;

		protected override RainbowSpriteBrain CreateBrain() => new RainbowSpriteGreenBrain();
	}
}

namespace DOL.AI.Brain
{
	public abstract class RainbowSpriteBrain : StandardMobBrain
	{
		public RainbowSpriteBrain() : base()
		{
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (Body.HealthPercent <= 20 && PullFriends(npc => npc.Brain is RainbowSpriteBrain, 1000) > 0)
				Message.MessageToArea(Body, $"The {Body.Name} chimes a tinkling call, and nearby sprites flit to its aid!", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);

			base.Think();
		}
	}

	public class RainbowSpriteTanBrain : RainbowSpriteBrain
	{
		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
				TryCastSpell(EarthenGrasp, 20, eEffect.MovementSpeedDebuff);

			base.Think();
		}

		#region Spells
		private static Spell EarthenGrasp => ScriptSpells.GetOrCreate("RainbowSpriteEarthenGrasp", 30, static db =>
		{
			db.CastTime = 3;
			db.RecastDelay = 25;
			db.ClientEffect = 5204;
			db.Icon = 5204;
			db.TooltipId = 5204;
			db.Duration = 8;
			db.Value = 99;
			db.Name = "Earthen Grasp";
			db.Description = "Target is rooted in place and unable to move for the duration of the spell.";
			db.Message1 = "Roots coil around your legs!";
			db.Message2 = "{0} is caught fast by grasping roots!";
			db.Message3 = "The roots crumble away.";
			db.Message4 = "{0} pulls free of the roots.";
			db.Range = 1500;
			db.SpellID = 11989;
			db.Target = eSpellTarget.ENEMY.ToString();
			db.Type = eSpellType.SpeedDecrease.ToString();
			db.Uninterruptible = false;
			db.DamageType = (int) eDamageType.Matter;
		});
		#endregion
	}

	public class RainbowSpriteWhiteBrain : RainbowSpriteBrain
	{
		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null && TryCastSpell(DazzlingFlash, 15, eEffect.Mez))
				Message.MessageToArea(Body, "The white sprite's wings flare with blinding light!", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, 1000);

			base.Think();
		}

		#region Spells
		private static Spell DazzlingFlash => ScriptSpells.GetOrCreate("RainbowSpriteDazzlingFlash", 30, static db =>
		{
			db.CastTime = 3;
			db.RecastDelay = 30;
			db.ClientEffect = 5318;
			db.Icon = 5318;
			db.TooltipId = 5318;
			db.Damage = 0;
			db.Duration = 6;
			db.Name = "Dazzling Flash";
			db.Description = "Targets around the caster are mesmerized and cannot move or take any other action for the duration of the spell.";
			db.Message1 = "You are mesmerized!";
			db.Message2 = "{0} is mesmerized!";
			db.Message3 = "You recover from the mesmerize.";
			db.Message4 = "{0} recovers from the mesmerize.";
			db.Radius = 350;
			db.Range = 0;
			db.SpellID = 11991;
			db.Target = eSpellTarget.ENEMY.ToString();
			db.Type = eSpellType.Mesmerize.ToString();
			db.Uninterruptible = false;
			db.DamageType = (int) eDamageType.Spirit;
		});
		#endregion
	}

	public class RainbowSpriteBlueBrain : RainbowSpriteBrain
	{
		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
				TryCastSpell(ChillingMist, 20, eEffect.MovementSpeedDebuff);

			base.Think();
		}

		#region Spells
		private static Spell ChillingMist => ScriptSpells.GetOrCreate("RainbowSpriteChillingMist", 30, static db =>
		{
			db.CastTime = 3;
			db.RecastDelay = 12;
			db.ClientEffect = 161;
			db.Icon = 161;
			db.TooltipId = 161;
			db.Damage = 70;
			db.Value = 40;
			db.Duration = 10;
			db.Name = "Chilling Mist";
			db.Description = "Inflicts cold damage to the target and reduces its movement speed for the duration of the spell.";
			db.Range = 1500;
			db.SpellID = 11990;
			db.Target = eSpellTarget.ENEMY.ToString();
			db.Type = eSpellType.DamageSpeedDecrease.ToString();
			db.Uninterruptible = false;
			db.DamageType = (int) eDamageType.Cold;
		});
		#endregion
	}

	public class RainbowSpriteGreenBrain : RainbowSpriteBrain
	{
		public override void Think()
		{
			if (Body.HealthPercent <= 50)
				TryCastSpell(GreenSpriteHeal, 100);

			base.Think();
		}

		#region Spells
		private static Spell GreenSpriteHeal => ScriptSpells.GetOrCreate("RainbowSpriteGreenHeal", 30, static db =>
		{
			db.CastTime = 3;
			db.RecastDelay = 8;
			db.ClientEffect = 1340;
			db.Icon = 1340;
			db.TooltipId = 1340;
			db.Value = 180;
			db.Name = "GreenSprite's Heal";
			db.Range = 1500;
			db.SpellID = 11988;
			db.Target = eSpellTarget.SELF.ToString();
			db.Type = eSpellType.Heal.ToString();
			db.Uninterruptible = true;
			db.MoveCast = true;
		});
		#endregion
	}
}
