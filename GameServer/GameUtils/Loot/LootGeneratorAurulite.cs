using System;
using Core.AI.Brain;
using Core.Database;

namespace Core.GS
{
	/// <summary>
	/// At the moment this generator only adds aurulite to the loot
	/// </summary>
	public class LootGeneratorAurulite : LootGeneratorBase
	{
		public static DbItemTemplate m_aurulite = GameServer.Database.FindObjectByKey<DbItemTemplate>("aurulite");
		
		/// <summary>
        /// Generate loot for given mob
		/// </summary>
		/// <param name="mob"></param>
		/// <param name="killer"></param>
		/// <returns>Lootlist with Aurulite drops</returns>
		public override LootList GenerateLoot(GameNpc mob, GameObject killer)
		{
			LootList loot = base.GenerateLoot(mob, killer);
			
			// ItemTemplate aurulite = new ItemTemplate(m_aurulite);  Creating a new ItemTemplate throws an exception later
			DbItemTemplate aurulite = GameServer.Database.FindObjectByKey<DbItemTemplate>(m_aurulite.Id_nb);
			
			
			try
			{
				GamePlayer player = killer as GamePlayer;
				if (killer is GameNpc && ((GameNpc)killer).Brain is IControlledBrain)
					player = ((ControlledNpcBrain)((GameNpc)killer).Brain).GetPlayerOwner();
				if (player == null)
					return loot;			
			
				int killedcon = (int)player.GetConLevel(mob)+3;
				
				if(killedcon <= 0)
					return loot;
				
				int lvl = mob.Level + 1;
				if (lvl < 1) lvl = 1;
				int maxcount = 1;
				
				//Switch pack size
				if (lvl > 0 && lvl < 10) 
				{
					//Aurulite only
					maxcount = (int)Math.Floor((double)(lvl/2))+1;
				}
				else if (lvl >= 10 && lvl < 20)
				{
					//Aurulire Chip (x5)
					aurulite.PackSize = 5;
					maxcount = (int)Math.Floor((double)((lvl-10)/2))+1;
				}
				else if (lvl >= 20 && lvl < 30)
				{
					//Aurulite Fragment (x10)
					aurulite.PackSize = 10;
					maxcount = (int)Math.Floor((double)((lvl-20)/2))+1;
					
				}
				else if (lvl >=30 && lvl < 40) 
				{
					//Aurulite Shard (x20)
					aurulite.PackSize = 20;
					maxcount = (int)Math.Floor((double)((lvl-30)/2))+1;
				}
				else if (lvl >= 40 && lvl < 50)
				{
					//Aurulite Cluster (x30)
					aurulite.PackSize = 30;
					maxcount = (int)Math.Floor((double)((lvl-40)/2))+1;
				}
				else 
				{
					//Aurulite Cache (x40)
					aurulite.PackSize = 40;
					maxcount = (int)Math.Round((double)(lvl/10));
				}
				
				if (!mob.Name.ToLower().Equals(mob.Name))
				{
					//Named mob, more cash !
					maxcount = (int)Math.Round(maxcount*ServerProperties.Properties.LOOTGENERATOR_AURULITE_NAMED_COUNT);
				}
				
				// add to loot
				if(maxcount > 0 && Util.Chance(ServerProperties.Properties.LOOTGENERATOR_AURULITE_BASE_CHANCE+Math.Max(10, killedcon))) {
					// Add to fixed to prevent overrides with loottemplate
					loot.AddFixed(aurulite, (int)Math.Ceiling(maxcount*ServerProperties.Properties.LOOTGENERATOR_AURULITE_AMOUNT_RATIO));
				}
				
			}
			catch
			{
				// Prevent displaying errors
				return loot;
			}
			
			return loot;
		}
	}
}
