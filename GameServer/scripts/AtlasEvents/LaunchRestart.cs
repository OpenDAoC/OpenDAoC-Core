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
        
        private static string RestartKey = "LaunchRestart";
        
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
            
            var launch = new DateTime(2022, 07, 04, 15, 00, 00);
            // var launch = new DateTime(2022, 07, 02, 15, 30, 00);
            
            var creationDate = player.DBCharacter.CreationDate;

            if (DateTime.Now < launch) return;
            if (creationDate >= launch) return;

            var needsReset = DOLDB<DOLCharactersXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId")
                .IsEqualTo(player.ObjectId).And(DB.Column("KeyName").IsEqualTo(RestartKey)));

            if (needsReset != null) return;

            player.MoveToBind();
            
            player.RealmPoints = 0;
            player.RealmLevel = 0;
            player.Experience = 0;
            
            player.RespecRealm();
            player.Reset();
            player.SetCharacterClass(player.CharacterClass.ID);
            player.Reset();
            player.OnLevelUp(0);
            
            BattlegroundEventLoot.GenerateArmor(player);
            BattlegroundEventLoot.GenerateWeaponsForClass((eCharacterClass)player.CharacterClass.ID, player);
            player.ReceiveItem(player, "Personal_Bind_Recall_Stone");

            player.Out.SendUpdatePlayer();
            player.Out.SendUpdatePlayerSkills();
            player.Out.SendUpdatePoints();
            
            var reset = new DOLCharactersXCustomParam
            {
                DOLCharactersObjectId = player.ObjectId,
                KeyName = RestartKey,
                Value = "1"
            };
            GameServer.Database.AddObject(reset);
            
            player.Achieve($"{RestartKey}-Credit");
            
            player.Out.SendMessage("Thanks for playing Atlas! Your level has been reset to 1, we wish you good luck with your adventure.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }
        
    }
}
#endregion