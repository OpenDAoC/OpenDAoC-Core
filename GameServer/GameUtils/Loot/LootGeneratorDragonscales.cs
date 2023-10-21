using System;
using System.Reflection;
using Core.AI.Brain;
using Core.Database;
using log4net;

namespace Core.GS
{
	/// <summary>
	/// At the moment this generator only adds dragonscales to the loot
	/// </summary>
	public class LootGeneratorDragonscales : LootGeneratorBase
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static DbItemTemplate m_dragonscales = GameServer.Database.FindObjectByKey<DbItemTemplate>("dragonscales");
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

				int killedcon = (int)player.GetConLevel(mob)+3;
				
				if(killedcon <= 0)
					return loot;

				int iScaleCount = 0;

				switch (mob.Name.ToLower())
				{
					// Epic dragons
					case "cuuldurach the glimmer king":
					case "gjalpinulva":
					case "golestandt":
					case "myrddraxis":
					case "nosdoden":
					case "xanxicar":
						iScaleCount = 10 * ServerProperties.Properties.LOOTGENERATOR_DRAGONSCALES_NAMED_COUNT;
						break;
					// Named Dragons
					case "asiintath":
					case "ghorvensath":
					case "iasentinth":
					case "neureksath":
					case "dyronith":
					case "elinkueth":
					case "jarkkonith":
					case "ljoridkorith":
					case "preniceth":
					case "runicaath":
					case "tollabarth":
					case "varrkorith":
						iScaleCount = 5 * ServerProperties.Properties.LOOTGENERATOR_DRAGONSCALES_NAMED_COUNT;
						break;
					// Dragon Spawn
					case "glimmer dragon spawn":
					case "stone dragon spawn":
					case "wolf dragon spawn":
						iScaleCount = 1;
						break;
					default:
						// Mobs range from 55 to 75, and we want an up to 10% bonus to drop chance based on that
						// I dislike losing accuracy rounding things, and it's a lot faster to do this in 1/10ths 
						//	of a percent than to convert to doubles and to multiply rather than divide.
						if (Util.Random(1000) < (10 * ServerProperties.Properties.LOOTGENERATOR_DRAGONSCALES_BASE_CHANCE 
							+ 5 * (mob.Level - 55)))
						{
							if (mob.Name.ToLower() != mob.Name)
								// Named critter
								iScaleCount = ServerProperties.Properties.LOOTGENERATOR_DRAGONSCALES_NAMED_COUNT;
							else
								iScaleCount = 1;
						}
						break;
				}

				if (iScaleCount > 0)
					loot.AddFixed(m_dragonscales, iScaleCount);
			}
			catch (Exception e)
			{
				log.Error("LootGeneratorDragonscales.GenerateLoot()", e);
				return loot;
			}
			
			return loot;
		}
	}
}
