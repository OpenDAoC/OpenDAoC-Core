using Core.AI.Brain;

namespace Core.GS;

#region Tan pixie
public class RainbowSpriteTan : GameNpc
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
	#endregion Tan pixie


#region White pixie
public class RainbowSpriteWhite : GameNpc
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
#endregion White pixie

#region Blue pixie
public class RainbowSpriteBlue : GameNpc
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
#endregion Blue pixie

#region Green pixie
public class RainbowSpriteGreen : GameNpc
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
#endregion Green pixie