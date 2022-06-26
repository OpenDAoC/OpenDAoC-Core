/*
 *
 * ATLAS - Script to reboot the launch, yay
 *
 */

using System;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;
using log4net;

#region LoginEvent
namespace DOL.GS.GameEvents
{
    public class LaunchRestartScript
    {
        
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private static string BetaResetKey = "LaunchRestart";
        private static string CompensationString = "LaunchCompensation";
        
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
            var player = sender as GamePlayer;
            if (player == null) return;
            
            var launch = new DateTime(2022, 06, 26, 14, 00, 00);

            var creationDate = player.DBCharacter.CreationDate;

            if (creationDate >= launch) return;

            var needsReset = DOLDB<DOLCharactersXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId")
                .IsEqualTo(player.ObjectId).And(DB.Column("KeyName").IsEqualTo(BetaResetKey)));

            var playerCompensationString = CompensationString + player.Realm;
            
            var receivedCompensation = DOLDB<AccountXCustomParam>.SelectObject(DB.Column("Name")
                .IsEqualTo(player.AccountName).And(DB.Column("KeyName").IsEqualTo(playerCompensationString)));

            if (needsReset != null) return;

            player.RealmPoints = 0;
            player.RealmLevel = 0;
            player.Experience = 0;

            player.MoveToBind();

            player.RemoveAllSpecs();
            player.RemoveAllSpellLines();
            player.styleComponent.RemoveAllStyles();

            //reset before, and after changing the class.

            player.RespecAll();
            

            player.Out.SendUpdatePlayer();
            player.Out.SendUpdatePlayerSkills();
            player.Out.SendUpdatePoints();
            
            BattlegroundEventLoot.GenerateArmor(player);
            BattlegroundEventLoot.GenerateWeaponsForClass((eCharacterClass)player.CharacterClass.ID, player);

            var reset = new DOLCharactersXCustomParam
            {
                DOLCharactersObjectId = player.ObjectId,
                KeyName = BetaResetKey,
                Value = "1"
            };
            GameServer.Database.AddObject(reset);
            
            var message = $"Thanks for enduring our launch. \n\n" +
                          $"All player inventories, achievements, money and crafting skills have been reset. \n\n" +
                          $"A gold compensation has been added to your account." +
                          $"Visit Cruella de Vill in your Realm's Capital to claim an additional reward.";
            
            player.Out.SendMessage(message, eChatType.CT_Important, eChatLoc.CL_PopupWindow);
            
            if (receivedCompensation != null) return;
            
            player.AddMoney(250 * 10000); // 250 gold
            
            var compensation = new AccountXCustomParam
            {
                Name = player.AccountName,
                KeyName = playerCompensationString,
                Value = "1"
            };
            GameServer.Database.AddObject(compensation);

        }
        
    }
}
#endregion