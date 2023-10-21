using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.PlayerClass;

namespace DOL.GS
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