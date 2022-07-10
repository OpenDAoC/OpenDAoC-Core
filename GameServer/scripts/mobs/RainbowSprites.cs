using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;

#region Tan pixie
namespace DOL.GS
{
	public class RainbowSpriteTan : GameNPC
	{
		public RainbowSpriteTan() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165135);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RainbowSpriteTanBrain sbrain = new RainbowSpriteTanBrain();
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
}
namespace DOL.AI.Brain
{
	public class RainbowSpriteTanBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public RainbowSpriteTanBrain() : base()
		{
			ThinkInterval = 1500;
		}
		private bool CallforHelp = false;
		public override void Think()
		{
			if (!HasAggressionTable())
				CallforHelp = false;

			if (HasAggro && Body.TargetObject != null)
			{
				if (!CallforHelp)
				{
					if (Body.HealthPercent <= 20)
					{
						foreach (GameNPC npc in Body.GetNPCsInRadius(1000))
						{
							GameLiving target = Body.TargetObject as GameLiving;
							if (npc != null && npc.IsAlive && npc.Name.ToLower() == "rainbow sprite" && npc.Brain is not ControlledNpcBrain && npc.Brain is RainbowSpriteTanBrain brain && npc != Body)
							{
								if (target != null && target.IsAlive && brain != null && !brain.HasAggro)
									brain.AddToAggroList(target, 10);
							}
						}
						CallforHelp = true;
					}
				}
			}
			base.Think();
		}
	}
}
#endregion

#region White pixie
namespace DOL.GS
{
	public class RainbowSpriteWhite : GameNPC
	{
		public RainbowSpriteWhite() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(50024);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RainbowSpriteWhiteBrain sbrain = new RainbowSpriteWhiteBrain();
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
}
namespace DOL.AI.Brain
{
	public class RainbowSpriteWhiteBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public RainbowSpriteWhiteBrain() : base()
		{
			ThinkInterval = 1500;
		}
		private bool CallforHelp = false;
		public override void Think()
		{
			if (!HasAggressionTable())
				CallforHelp = false;

			if (HasAggro && Body.TargetObject != null)
			{
				if (!CallforHelp)
				{
					if (Body.HealthPercent <= 20)
					{
						foreach (GameNPC npc in Body.GetNPCsInRadius(1000))
						{
							GameLiving target = Body.TargetObject as GameLiving;
							if (npc != null && npc.IsAlive && npc.Name.ToLower() == "rainbow sprite" && npc.Brain is not ControlledNpcBrain && npc.Brain is RainbowSpriteWhiteBrain brain && npc != Body)
							{
								if (target != null && target.IsAlive && brain != null && !brain.HasAggro)
									brain.AddToAggroList(target, 10);
							}
						}
						CallforHelp = true;
					}
				}
			}
			base.Think();
		}
	}
}
#endregion

#region Blue pixie
namespace DOL.GS
{
	public class RainbowSpriteBlue : GameNPC
	{
		public RainbowSpriteBlue() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165136);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RainbowSpriteBlueBrain sbrain = new RainbowSpriteBlueBrain();
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
}
namespace DOL.AI.Brain
{
	public class RainbowSpriteBlueBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public RainbowSpriteBlueBrain() : base()
		{
			ThinkInterval = 1500;
		}
		private bool CallforHelp = false;
		public override void Think()
		{
			if (!HasAggressionTable())
				CallforHelp = false;

			if (HasAggro && Body.TargetObject != null)
			{
				if (!CallforHelp)
				{
					if (Body.HealthPercent <= 20)
					{
						foreach (GameNPC npc in Body.GetNPCsInRadius(1000))
						{
							GameLiving target = Body.TargetObject as GameLiving;
							if (npc != null && npc.IsAlive && npc.Name.ToLower() == "rainbow sprite" && npc.Brain is not ControlledNpcBrain && npc.Brain is RainbowSpriteBlueBrain brain && npc != Body)
							{
								if (target != null && target.IsAlive && brain != null && !brain.HasAggro)
									brain.AddToAggroList(target, 10);
							}
						}
						CallforHelp = true;
					}
				}
			}
			base.Think();
		}
	}
}
#endregion

#region Green pixie
namespace DOL.GS
{
	public class RainbowSpriteGreen : GameNPC
	{
		public RainbowSpriteGreen() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(50018);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RainbowSpriteGreenBrain sbrain = new RainbowSpriteGreenBrain();
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
}
namespace DOL.AI.Brain
{
	public class RainbowSpriteGreenBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public RainbowSpriteGreenBrain() : base()
		{
			ThinkInterval = 1500;
		}
		private bool CallforHelp = false;
		public override void Think()
		{
			if (!HasAggressionTable())
				CallforHelp = false;

			if(Body.HealthPercent <= 50 && !Body.IsCasting && Util.Chance(100))
				Body.CastSpell(GreenSpriteHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

			if (HasAggro && Body.TargetObject != null)
			{
				if (!CallforHelp)
				{
					if (Body.HealthPercent <= 20)
					{
						foreach (GameNPC npc in Body.GetNPCsInRadius(1000))
						{
							GameLiving target = Body.TargetObject as GameLiving;
							if (npc != null && npc.IsAlive && npc.Name.ToLower() == "rainbow sprite" && npc.Brain is not ControlledNpcBrain && npc.Brain is RainbowSpriteGreenBrain brain && npc != Body)
							{
								if (target != null && target.IsAlive && brain != null && !brain.HasAggro)
									brain.AddToAggroList(target, 10);
							}
						}
						CallforHelp = true;
					}
				}
			}
			base.Think();
		}
		private Spell m_GreenSpriteHeal;
		private Spell GreenSpriteHeal
		{
			get
			{
				if (m_GreenSpriteHeal == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 8;
					spell.ClientEffect = 1340;
					spell.Icon = 1340;
					spell.TooltipId = 1340;
					spell.Value = 180;
					spell.Name = "GreenSprite's Heal";
					spell.Range = 1500;
					spell.SpellID = 11988;
					spell.Target = "Self";
					spell.Type = eSpellType.Heal.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_GreenSpriteHeal = new Spell(spell, 30);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_GreenSpriteHeal);
				}
				return m_GreenSpriteHeal;
			}
		}
	}
}
#endregion