using Core.AI.Brain;
using Core.Events;
using Core.GS.PlayerClass;

namespace Core.GS
{
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
}