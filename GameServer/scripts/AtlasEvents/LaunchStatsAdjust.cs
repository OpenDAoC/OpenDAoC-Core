/*
 *
 * ATLAS - Script to adjust stats of reset characters above level 5
 *
 */

using System;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Realm;
using log4net;

#region LoginEvent
namespace DOL.GS.GameEvents
{
    public class LaunchRestartStatsScript
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private static string StatsKey = "StatsAdjust";
        
        [GameServerStartedEvent]
        public static void OnServerStart(DOLEvent e, object sender, EventArgs arguments)
        {
            GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, new DOLEventHandler(PlayerEntered));
        }

        /// <summary>
        /// Event handler fired when server is stopped
        /// </summary>
        [GameServerStoppedEvent]
        public static void OnServerStop(DOLEvent e, object sender, EventArgs arguments)
        {
            GameEventMgr.RemoveHandler(GamePlayerEvent.GameEntered, new DOLEventHandler(PlayerEntered));
        }
        
        /// <summary>
        /// Event handler fired when players enters the game
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        private static void PlayerEntered(DOLEvent e, object sender, EventArgs arguments)
        {
            if (sender is not GamePlayer player) return;
            
            var secondLaunchDate = new DateTime(2022, 7,4,15,0,0);
            
            //Male chars created after 2nd launch day dont need to be reset.
            if (player.CreationDate > secondLaunchDate && player.Gender == eGender.Male) return;

            var femaleFixDate = new DateTime(2022, 8,9,7,0,0);

            //Characters created after Female Stat Fix Date dont need stats reset.
            if (player.CreationDate > femaleFixDate) return;
            
            var needsAdjustment = DOLDB<DOLCharactersXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId")
                .IsEqualTo(player.ObjectId).And(DB.Column("KeyName").IsEqualTo("StatsAdjust")));
            
            if (needsAdjustment != null) return;
            
            Log.Warn($"STATSADJUST - {player.Name} ({player.AccountName})");

            var stsMessage = $"STATSADJUST - {player.Name} PREV STATS ";
            stsMessage +=
                $"STR: {player.GetBaseStat(eStat.STR)} CON: {player.GetBaseStat(eStat.CON)} DEX: {player.GetBaseStat(eStat.DEX)} QUI: {player.GetBaseStat(eStat.QUI)} INT: {player.GetBaseStat(eStat.INT)} PIE: {player.GetBaseStat(eStat.PIE)} EMP: {player.GetBaseStat(eStat.EMP)} CHR: {player.GetBaseStat(eStat.CHR)}";
            Log.Warn(stsMessage);

            
            var rBaseStats = RaceStats.GetRaceStats(player.Race);

            var baseStr = (short)rBaseStats.Strength;
            var baseCon = (short)rBaseStats.Constitution;
            var baseDex = (short)rBaseStats.Dexterity;
            var baseQui = (short)rBaseStats.Quickness;
            var baseInt = (short)rBaseStats.Intelligence;
            var basePie = (short)rBaseStats.Piety;
            var baseEmp = (short)rBaseStats.Empathy;
            var baseCha = (short)rBaseStats.Charisma;

            player.ChangeBaseStat(eStat.STR, (short)-player.GetBaseStat(eStat.STR));
            player.ChangeBaseStat(eStat.EMP,(short)-player.GetBaseStat(eStat.EMP));
            player.ChangeBaseStat(eStat.CHR,(short)-player.GetBaseStat(eStat.CHR));
            player.ChangeBaseStat(eStat.PIE,(short)-player.GetBaseStat(eStat.PIE));
            player.ChangeBaseStat(eStat.INT,(short)-player.GetBaseStat(eStat.INT));
            player.ChangeBaseStat(eStat.QUI,(short)-player.GetBaseStat(eStat.QUI));
            player.ChangeBaseStat(eStat.DEX,(short)-player.GetBaseStat(eStat.DEX));
            player.ChangeBaseStat(eStat.CON,(short)-player.GetBaseStat(eStat.CON));
            
            player.ChangeBaseStat(eStat.STR,baseStr);
            player.ChangeBaseStat(eStat.CON,baseCon);
            player.ChangeBaseStat(eStat.DEX,baseDex);
            player.ChangeBaseStat(eStat.QUI,baseQui);
            player.ChangeBaseStat(eStat.INT,baseInt);
            player.ChangeBaseStat(eStat.PIE,basePie);
            player.ChangeBaseStat(eStat.EMP,baseEmp);
            player.ChangeBaseStat(eStat.CHR,baseCha);
            
            for (var i = 6; i <= player.Level ; i++)
            {
                if (player.CharacterClass.PrimaryStat != eStat.UNDEFINED)
                {
                    player.ChangeBaseStat(player.CharacterClass.PrimaryStat, +1);
                }
                if (player.CharacterClass.SecondaryStat != eStat.UNDEFINED && ((i - 6) % 2 == 0))
                {
                    player.ChangeBaseStat(player.CharacterClass.SecondaryStat, +1);
                }
                if (player.CharacterClass.TertiaryStat != eStat.UNDEFINED && ((i - 6) % 3 == 0))
                {
                    player.ChangeBaseStat(player.CharacterClass.TertiaryStat, +1);
                }
            }
            
            stsMessage = $"STATSADJUST - {player.Name} NEW STATS ";
            stsMessage +=
                $"STR: {player.GetBaseStat(eStat.STR)} CON: {player.GetBaseStat(eStat.CON)} DEX: {player.GetBaseStat(eStat.DEX)} QUI: {player.GetBaseStat(eStat.QUI)} INT: {player.GetBaseStat(eStat.INT)} PIE: {player.GetBaseStat(eStat.PIE)} EMP: {player.GetBaseStat(eStat.EMP)} CHR: {player.GetBaseStat(eStat.CHR)}";
            Log.Warn(stsMessage);
            
            Log.Warn($"STATSADJUST - {player.Name} Granted a stats respec");
            player.CustomisationStep = 3;
            player.SaveIntoDatabase();
            
            var adjusted = new DOLCharactersXCustomParam
            {
                DOLCharactersObjectId = player.ObjectId,
                KeyName = "StatsAdjust",
                Value = "1"
            };
            GameServer.Database.AddObject(adjusted);
            
            var message = "Your base stats have been adjusted and you have been granted a free stats respec.\n";
            player.Out.SendDialogBox(eDialogCode.SimpleWarning, 0, 0, 0, 0, eDialogType.Ok, true, message);
            message += "Logout and click on CUSTOMIZE to redistribute your 30 starting points.\n\n";
            
            player.Out.SendCharStatsUpdate();
            
            player.Out.SendMessage(message, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            player.Out.SendMessage(message, eChatType.CT_Staff, eChatLoc.CL_ChatWindow);
            
            message += "Your new base stats are:\n\n";
            message += $"STR: {player.GetBaseStat(eStat.STR)}\n" +
                       $"CON: {player.GetBaseStat(eStat.CON)}\n" +
                       $"DEX: {player.GetBaseStat(eStat.DEX)}\n" +
                       $"QUI: {player.GetBaseStat(eStat.QUI)}\n" +
                       $"INT: {player.GetBaseStat(eStat.INT)}\n" +
                       $"PIE: {player.GetBaseStat(eStat.PIE)}\n" +
                       $"EMP: {player.GetBaseStat(eStat.EMP)}\n" +
                       $"CHR: {player.GetBaseStat(eStat.CHR)}\n\n";
            
            message += "REMEMBER TO LOGOUT AND CLICK ON CUSTOMIZE TO DISTRIBUTE YOUR 30 STARTING POINTS.";

            player.Out.SendMessage(message, eChatType.CT_Important, eChatLoc.CL_PopupWindow);

        }
    }
}
#endregion