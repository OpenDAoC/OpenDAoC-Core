using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.AI.Brains;

public class LurfosHeraldBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public LurfosHeraldBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	public static bool IsColdWeapon = false;
	public static bool IsHeatWeapon = false;
	public static bool IsNormalWeapon = false;
	public static bool StartSwitchWeapons = false;
	public int SwitchToCold(EcsGameTimer timer)
    {
		if (HasAggro)
		{
			IsColdWeapon = true;
			IsHeatWeapon = false;
			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				if (player != null)
					player.Out.SendSpellEffectAnimation(Body, Body, 4075, 0, false, 1);
			}
			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 7, 0, 27);
			Body.Inventory = template.CloseTemplate();
			Body.BroadcastLivingEquipmentUpdate();
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SwitchToHeat), Util.Random(30000, 50000));
		}
		return 0;
    }
	public int SwitchToHeat(EcsGameTimer timer)
	{
		if (HasAggro)
		{
			IsHeatWeapon = true;
			IsColdWeapon = false;
			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				if (player != null)
					player.Out.SendSpellEffectAnimation(Body, Body, 4051, 0, false, 1);
			}
			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 7, 0, 21);
			Body.Inventory = template.CloseTemplate();
			Body.BroadcastLivingEquipmentUpdate();
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SwitchToCold), Util.Random(30000, 50000));
		}
		return 0;
	}
	public int SwitchToNormal(EcsGameTimer timer)
	{
		GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
		template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 7, 0, 0);
		Body.Inventory = template.CloseTemplate();
		Body.BroadcastLivingEquipmentUpdate();
		return 0;
	}
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			IsColdWeapon = false;
			IsHeatWeapon = false;
			StartSwitchWeapons = false;
			if (IsNormalWeapon==false)
            {
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SwitchToNormal), 1000);
				IsNormalWeapon = true;
            }
		}
		if (HasAggro && Body.TargetObject != null)
		{
			IsNormalWeapon = false;
			if(StartSwitchWeapons==false)
            {
				switch(Util.Random(1,2))
                {
					case 1: new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SwitchToCold), 1000); break;
					case 2: new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SwitchToHeat), 1000); break;
				}					
				StartSwitchWeapons =true;
            }
		}
		base.Think();
	}
}