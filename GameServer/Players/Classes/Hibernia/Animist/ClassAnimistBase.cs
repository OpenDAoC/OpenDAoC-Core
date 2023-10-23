using Core.GS.AI.Brains;
using Core.GS.Events;

namespace Core.GS.Players;

public class ClassAnimistBase : ClassForester
{
	/// <summary>
	/// Releases controlled object
	/// </summary>
	public override void CommandNpcRelease()
	{
		TurretPet turretFnF = Player.TargetObject as TurretPet;
		if (turretFnF != null && turretFnF.Brain is TurretFnfBrain && Player.IsControlledNPC(turretFnF))
		{
			Player.Notify(GameLivingEvent.PetReleased, turretFnF);
			return;
		}

		base.CommandNpcRelease();
	}
}