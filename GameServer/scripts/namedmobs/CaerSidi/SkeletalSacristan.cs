using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS;

namespace DOL.GS.Scripts
{
    public class SkeletalSacristan : GameNPC
    {
	    public override bool AddToWorld()
		{
			Model = 916;
			Name = "Skeletal Sacristan";
			Size = 85;
			Level = 77;
			Gender = eGender.Neutral;
			BodyType = 11; // undead
			MaxDistance = 1500;
			TetherRange = 2000;
			RoamingRange = 0;
			SkeletalSacristanBrain sBrain = new SkeletalSacristanBrain();
			SetOwnBrain(sBrain);
			base.AddToWorld();
			return true;
		}
	    
	    public override void Die(GameObject killer)
	    {
		    // debug
		    log.Debug($"{Name} killed by {killer.Name}");
            
		    GamePlayer playerKiller = killer as GamePlayer;

		    if (playerKiller?.Group != null)
		    {
			    foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
			    {
				    AtlasROGManager.GenerateOrbAmount(groupPlayer,5000);
			    }
		    }
		    DropLoot(killer);
		    base.Die(killer);
	    }
		
		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Skeletal Sacristan NPC Initializing...");
		}
    }
    
}
namespace DOL.AI.Brain
{
	public class SkeletalSacristanBrain : StandardMobBrain
	{
		public override void OnAttackedByEnemy(AttackData ad)
		{
			Body.WalkTo(Body.X + Util.Random(1000), Body.Y + Util.Random(1000), Body.Z, 460);

			base.OnAttackedByEnemy(ad);
		}
		
		public override void AttackMostWanted()
		{

		}
		
		public override void Think()
		{
			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
				Body.WalkTo(Body.X + Util.Random(1000), Body.Y + Util.Random(1000), Body.Z, 460);
			}
			if (Body.InCombatInLast(15 * 1000) == false && this.Body.InCombatInLast(15 * 1000))
			{
				Body.WalkTo(Body.X + Util.Random(1000), Body.Y + Util.Random(1000), Body.Z, 460);
			}
			base.Think();
		}
		
	}
}