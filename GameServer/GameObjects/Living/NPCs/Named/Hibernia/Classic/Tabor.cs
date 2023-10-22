using System;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.World;

namespace Core.GS;

#region Tabor
public class Tabor : GameNpc
{
	public Tabor() : base() { }

	public override bool AddToWorld()
	{
		foreach(GameNpc npc in GetNPCsInRadius(5000))
        {
			if (npc != null && npc.IsAlive && npc.Brain is TaborGhostBrain)
				npc.RemoveFromWorld();
        }
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166738);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
		template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 315, 0, 0);
		Inventory = template.CloseTemplate();
		SwitchWeapon(EActiveWeaponSlot.Standard);

		VisibleActiveWeaponSlots = 16;
		MeleeDamageType = EDamageType.Slash;

		TaborBrain sbrain = new TaborBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
		}
	}
	public override void Die(GameObject killer)
    {
		BroadcastMessage(String.Format("As {0} falls to the ground, you feel a breeze in the air.\nA swirl of dirt covers the area.", Name));
		SpawnSwirlDirt();
        base.Die(killer);
    }
	private void SpawnSwirlDirt()
    {
		SwirlDirt npc = new SwirlDirt();
		npc.X = 37256;
		npc.Y = 32460;
		npc.Z = 14437;
		npc.Heading = Heading;
		npc.CurrentRegion = CurrentRegion;
		npc.AddToWorld();
	}
}
#endregion

#region Ghost of Tabor
public class TaborGhost : GameNpc
{
	public TaborGhost() : base() { }
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}
	}
	public override bool AddToWorld()
	{
		BroadcastMessage("Ghost of Tabor says, \"You thought the fight was over did you ? \"");
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161293);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
		template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 445, 0, 0);
		template.AddNPCEquipment(EInventorySlot.DistanceWeapon, 471, 0, 0);
		Inventory = template.CloseTemplate();
		SwitchWeapon(EActiveWeaponSlot.Standard);

		VisibleActiveWeaponSlots = 16;
		MeleeDamageType = EDamageType.Slash;

		RespawnInterval = -1;
		TaborGhostBrain sbrain = new TaborGhostBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
    public override void Die(GameObject killer)
    {
		if(killer != null)
			BroadcastMessage(String.Format("{0} says, \"I will return some day.Be warned!\"",Name));
		base.Die(killer);
    }
}
#endregion

#region Swirl of Dirt
public class SwirlDirt : GameNpc
{
	public SwirlDirt() : base() { }

	public override bool AddToWorld()
	{
		Name = "Swirl of Dirt";
		Level = 50;
		Model = 665;
		Size = 70;
		Flags = (ENpcFlags)28;

		SwirlDirtBrain sbrain = new SwirlDirtBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		RespawnInterval = -1;
		bool success = base.AddToWorld();
		if (success)
		{
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 1000);
		}
		return success;
	}
	#region Show Effects
	protected int Show_Effect(EcsGameTimer timer)
	{
		if (IsAlive)
		{
			foreach (GamePlayer player in GetPlayersInRadius(3000))
			{
				if (player != null)
					player.Out.SendSpellEffectAnimation(this, this, 6072, 0, false, 0x01);
			}
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(SpawnGhostTabor), 1000);
		}
		return 0;
	}
	protected int RemoveMob(EcsGameTimer timer)
	{
		if (IsAlive)
			RemoveFromWorld();
		return 0;
	}
	private int SpawnGhostTabor(EcsGameTimer timer)
    {
		SpawnGhostOfTabor();
		return 0;
    }
	private void SpawnGhostOfTabor()
	{
		foreach (GameNpc mob in GetNPCsInRadius(5000))
		{
			if (mob.Brain is TaborGhostBrain)
				return;
		}
		TaborGhost npc = new TaborGhost();
		npc.X = 37256;
		npc.Y = 32460;
		npc.Z = 14437;
		npc.Heading = Heading;
		npc.CurrentRegion = CurrentRegion;
		npc.AddToWorld();
		new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(RemoveMob), 500);
	}
	#endregion
}
#endregion