using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;

namespace Core.GS.ServerRules
{
	/// <summary>
	/// Handles Adventure Wings (Catacombs) Instance Jump Point.
	/// Find available Instance, Create If Needed
	/// Clean up remaining instance before timer countdown
	/// </summary>
	public class AdventureWingJumpPoint : IJumpPointHandler
	{

		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Decides whether player can jump to the target point.
		/// All messages with reasons must be sent here.
		/// Can change destination too.
		/// </summary>
		/// <param name="targetPoint">The jump destination</param>
		/// <param name="player">The jumping player</param>
		/// <returns>True if allowed</returns>
        public bool IsAllowedToJump(DbZonePoint targetPoint, GamePlayer player)
        {            
            
            //Handles zoning INTO an instance.
            GameLocation loc = null;
            AdventureWingInstance previousInstance = null;
                      
            // Do we have a group ?
            if(player.Group != null) 
            {
            	//Check if there is an instance dedicated to this group
            	foreach(Region region in WorldMgr.GetAllRegions()) 
            	{
            		if(region is AdventureWingInstance && ((AdventureWingInstance)region).Group != null && ((AdventureWingInstance)region).Group == player.Group) 
            		{
            			// Our group has an instance !
            			previousInstance = (AdventureWingInstance)region;
            			break;
            		}
            		else if(region is AdventureWingInstance && ((AdventureWingInstance)region).Player != null && ((AdventureWingInstance)region).Player == player.Group.Leader) 
            		{
            			// Our leader has an instance !
            			previousInstance = (AdventureWingInstance)region;
            			previousInstance.Group = player.Group;
            			break;
            		}
            	}
            	
            }
            else {
            	// I am solo !
            	//Check if there is an instance dedicated to me
            	foreach(Region region in WorldMgr.GetAllRegions()) 
            	{
            		if(region is AdventureWingInstance && ((AdventureWingInstance)region).Player != null && ((AdventureWingInstance)region).Player == player) 
            		{
            			// I have an Instance !
            			previousInstance = (AdventureWingInstance)region;
            			previousInstance.Group = player.Group;
            			break;
            		}
            	}
            }
            
            
            if(previousInstance != null) 
            {
            	// We should check if we can go in !
            	if(previousInstance.Skin != targetPoint.TargetRegion) 
            	{
            		//we're trying to enter in an other instance and we still have one !
            		//check if previous one is empty
            		if(previousInstance.NumPlayers > 0) 
            		{
            			//We can't jump !
            			player.Out.SendMessage("You have another instance (" + previousInstance.Description + ") running with people in it !", EChatType.CT_System, EChatLoc.CL_SystemWindow);
            			return false;
            		}
            		else 
            		{
            			log.Warn("Player : "+ player.Name +" requested new Instance, destroying instance " + previousInstance.Description + ", ID: " + previousInstance.ID + ", type=" + previousInstance.GetType().ToString() + ".");
                		WorldMgr.RemoveInstance(previousInstance);
                		previousInstance = null;
            		}
            		
            	}
            	
            }
            
           if(previousInstance == null) 
           {
            	// I have no instance to go to, create one !
            	previousInstance = (AdventureWingInstance)WorldMgr.CreateInstance(targetPoint.TargetRegion, typeof(AdventureWingInstance));
            	if(targetPoint.SourceRegion != 0 && targetPoint.SourceRegion == player.CurrentRegionID) {
            		//source loc seems legit...
            		previousInstance.SourceEntrance = new GameLocation("source", targetPoint.SourceRegion, targetPoint.SourceX, targetPoint.SourceY, targetPoint.SourceZ);
            	}
            	
        		if(player.Group != null) 
        		{
	        		previousInstance.Group = player.Group;
	        		previousInstance.Player = player.Group.Leader;
        		}
        		else 
        		{
	       			previousInstance.Group = null;
	        		previousInstance.Player = player;
        		}
        		
        		//get region data
				long mobs = 0;
				long merchants = 0;
				long items = 0;
				long bindpoints = 0;
	
				previousInstance.LoadFromDatabase(previousInstance.RegionData.Mobs, ref mobs, ref merchants, ref items, ref bindpoints);

				if (log.IsInfoEnabled)
				{
					log.Info("Total Mobs: " + mobs);
					log.Info("Total Merchants: " + merchants);
					log.Info("Total Items: " + items);
				}
				
				//Attach Loot Generator
				LootMgr.RegisterLootGenerator(new LootGeneratorAurulite(), null, null, null, previousInstance.ID);
				
				// Player created new instance
            	// Destroy all other instance that should be...
            	List<Region> to_delete = new List<Region>();
            	foreach(Region region in WorldMgr.GetAllRegions()) 
            	{
            		if (region is AdventureWingInstance && (AdventureWingInstance)region != previousInstance) 
            		{
            			AdventureWingInstance to_clean = (AdventureWingInstance)region;
           			
           				// Won't clean up populated Instance
            			if(to_clean.NumPlayers == 0)
            			{
            				
            				if(to_clean.Group != null && player.Group != null && to_clean.Group == player.Group)
            				{
            					// Got another instance for the same group... Destroy it !
                				to_delete.Add(to_clean);
            				}
            				else if(to_clean.Player != null && (to_clean.Player == player || (player.Group != null && player.Group.Leader == to_clean.Player)))
            				{
            					// Got another instance for the same player... Destroy it !
                				to_delete.Add(to_clean);           					
            				}
            				else if(to_clean.Group == null && to_clean.Player == null) 
            				{
            					//nobody owns this instance anymore
								to_delete.Add(to_clean);     
            				}
            			}
            		}
            	}
            	
            	//enumerate to_delete
            	foreach(Region region in to_delete) 
            	{
            		log.Warn("Player : "+ player.Name +" has provoked an instance cleanup - " + region.Description + ", ID: " + region.ID + ", type=" + region.GetType().ToString() + ".");
            		WorldMgr.RemoveInstance((BaseInstance)region);
            	}
           	}
            

        	//get loc of instance
        	if(previousInstance != null) 
        	{
        		loc = new GameLocation(previousInstance.Description + " (instance)", previousInstance.ID, targetPoint.TargetX,  targetPoint.TargetY,  targetPoint.TargetZ,  targetPoint.TargetHeading);
        	}


            if (loc != null)
            {
            	
            	// Move Player, changing target destination is failing !!
            	player.MoveTo(loc);
                return false;
            }

            player.Out.SendMessage("Something went Wrong when creating Instance !", EChatType.CT_System, EChatLoc.CL_SystemWindow);
            return false;
        }
	}
}
