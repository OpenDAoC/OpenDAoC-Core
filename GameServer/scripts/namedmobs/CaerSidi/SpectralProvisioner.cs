using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS.Scripts
{
    public class SpectralProvisioner : GameNPC
    {
	    public override double GetArmorAF(eArmorSlot slot)
	    {
		    return 1000;
	    }

	    public override double GetArmorAbsorb(eArmorSlot slot)
	    {
		    // 85% ABS is cap.
		    return 0.85;
	    }

	    public override short MaxSpeedBase
	    {
		    get => (short)(191 + (Level * 2));
		    set => m_maxSpeedBase = value;
	    }
	    public override int MaxHealth => 20000;

	    public override int AttackRange
	    {
		    get => 180;
		    set { }
	    }
	    public override bool AddToWorld()
		{
			Model = 929;
			Name = "Spectral Provisioner";
			Size = 90;
			Level = 77;
			Gender = eGender.Neutral;
			BodyType = 11; // undead
			MaxDistance = 1500;
			TetherRange = 2000;
			RoamingRange = 0;
			SpectralProvisionerBrain sBrain = new SpectralProvisionerBrain();
			SetOwnBrain(sBrain);
			base.AddToWorld();
			return true;
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Spectral Provisioner NPC Initializing...");
		}

    }
    
}

namespace DOL.AI.Brain
{
	public class SpectralProvisionerBrain : StandardMobBrain
	{
		public override void OnAttackedByEnemy(AttackData ad)
		{
			Body.WalkTo(Body.X + Util.Random(1000), Body.Y + Util.Random(1000), Body.Z, 460);

			base.OnAttackedByEnemy(ad);
		}
		
		public override void AttackMostWanted()
		{
			if (Util.Chance(50))
			{
				ItemTemplate sackJunk = GameServer.Database.FindObjectByKey<ItemTemplate>("sack_of_decaying_junk");
				InventoryItem item = GameInventoryItem.Create(sackJunk);
			
				foreach (GamePlayer player in Body.GetPlayersInRadius(500))
				{
					if (player.IsAlive)
					{
						player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item);
					}
				}
			}
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