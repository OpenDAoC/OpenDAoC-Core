﻿using System;
using System.Reflection;
using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Keeps;
using Core.GS.Server;
using log4net;

namespace Core.GS.GameUtils;

/// <summary>
/// At the moment this generator only adds dreaded seals to the loot
/// </summary>
public class LootGeneratorDreadedSeals : LootGeneratorBase
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    private static readonly DbItemTemplate m_GlowingDreadedSeal = GameServer.Database.FindObjectByKey<DbItemTemplate>("glowing_dreaded_seal");
    private static readonly DbItemTemplate m_SanguineDreadedSeal = GameServer.Database.FindObjectByKey<DbItemTemplate>("sanguine_dreaded_seal");

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

            switch (mob)
            {
                // Certain mobs have a 100% drop chance of multiple seals at once
                case GuardLord lord:
                    if (lord.IsTowerGuard || lord.Component.Keep.BaseLevel < 50)
                        loot.AddFixed(m_SanguineDreadedSeal, 1);  // Guaranteed drop, but towers and BGs only merit 1 seal.
                    else
                        loot.AddFixed(m_SanguineDreadedSeal, 5 * lord.Component.Keep.Level);
                    break;
                default:
                    if (mob.Name.ToUpper() == "LORD AGRAMON")
                        loot.AddFixed(m_SanguineDreadedSeal, 10);
                    else if (mob.Level >= ServerProperty.LOOTGENERATOR_DREADEDSEALS_STARTING_LEVEL)
                    {
                    int iPercentDrop = (mob.Level - ServerProperty.LOOTGENERATOR_DREADEDSEALS_STARTING_LEVEL)
	                    * ServerProperty.LOOTGENERATOR_DREADEDSEALS_DROP_CHANCE_PER_LEVEL
	                    + ServerProperty.LOOTGENERATOR_DREADEDSEALS_BASE_CHANCE;

                    if (!mob.Name.ToLower().Equals(mob.Name)) // Named mobs are more likely to drop a seal
	                    iPercentDrop = (int)Math.Round(iPercentDrop * ServerProperty.LOOTGENERATOR_DREADEDSEALS_NAMED_CHANCE);

                    if (Util.Random(9999) < iPercentDrop)
	                    loot.AddFixed(m_GlowingDreadedSeal, 1);
                    }
                    break;
	        }// switch
        }//try
        catch (Exception e)
        {
            log.Error(e.Message);
        }

        return loot;
    }
}