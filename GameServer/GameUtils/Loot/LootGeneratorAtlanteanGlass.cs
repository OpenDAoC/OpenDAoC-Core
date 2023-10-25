using System;
using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Server;

namespace Core.GS.GameUtils;

/// <summary>
/// At the moment this generator only adds AtlanteanGlass to the loot
/// </summary>
public class LootGeneratorAtlanteanGlass : LootGeneratorBase
{
	
	private static DbItemTemplate m_atlanteanglass = GameServer.Database.FindObjectByKey<DbItemTemplate>("atlanteanglass");
	
	/// <summary>
    /// Generate loot for given mob
	/// </summary>
	/// <param name="mob"></param>
	/// <param name="killer"></param>
	/// <returns></returns>
	public override LootList GenerateLoot(GameNpc mob, GameObject killer)
	{
		LootList loot = base.GenerateLoot(mob, killer);
		
		try
		{
			GamePlayer player = killer as GamePlayer;
			if (killer is GameNpc && ((GameNpc)killer).Brain is IControlledBrain)
				player = ((ControlledNpcBrain)((GameNpc)killer).Brain).GetPlayerOwner();
			if (player == null)
				return loot;			
		
			DbItemTemplate atlanteanGlass = GameServer.Database.FindObjectByKey<DbItemTemplate>(m_atlanteanglass.Id_nb);
			// ItemTemplate atlanteanGlass = new ItemTemplate(m_atlanteanglass);  Creating a new ItemTemplate throws an exception later

			int killedcon = (int)player.GetConLevel(mob)+3;
			
			if(killedcon <= 0)
				return loot;
							
			int lvl = mob.Level + 1;
			if (lvl < 1) lvl = 1;
			int maxcount = 1;
			
			//Switch pack size
			if (lvl > 0 && lvl < 10) 
			{
				//Single AtlanteanGlass
				maxcount = (int)Math.Floor((double)(lvl/2))+1;
			}
			else if (lvl >= 10 && lvl < 20)
			{
				//Double
				atlanteanGlass.PackSize = 2;
				maxcount = (int)Math.Floor((double)((lvl-10)/2))+1;
			}
			else if (lvl >= 20 && lvl < 30)
			{
				//Triple
				atlanteanGlass.PackSize = 3;
				maxcount = (int)Math.Floor((double)((lvl-20)/2))+1;
				
			}
			else if (lvl >=30 && lvl < 40) 
			{
				//Quad
				atlanteanGlass.PackSize = 4;
				maxcount = (int)Math.Floor((double)((lvl-30)/2))+1;
			}
			else if (lvl >= 40 && lvl < 50)
			{
				//Quint
				atlanteanGlass.PackSize = 5;
				maxcount = (int)Math.Floor((double)((lvl-40)/2))+1;
			}
			else 
			{
				//Cache (x10)
				atlanteanGlass.PackSize = 10;
				maxcount = (int)Math.Round((double)(lvl/10));
			}
			
			if (!mob.Name.ToLower().Equals(mob.Name))
			{
				//Named mob, more cash !
				maxcount = (int)Math.Round(maxcount*ServerProperty.LOOTGENERATOR_ATLANTEANGLASS_NAMED_COUNT);
			}
			
			if(maxcount > 0 && Util.Chance(ServerProperty.LOOTGENERATOR_ATLANTEANGLASS_BASE_CHANCE+Math.Max(10, killedcon)))
				loot.AddFixed(atlanteanGlass, maxcount);
		}
		catch
		{
			return loot;
		}
		
		return loot;
	}
}