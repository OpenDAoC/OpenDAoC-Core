/*
 *
 * ATLAS - Scripts to reset to level 1 the characters naturally levelled during beta to save the name
 *
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;
using log4net;

#region LoginEvent
namespace DOL.GS.GameEvents
{
    public class LaunchResetScript
    {
        
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private static string BetaResetKey = "BetaToLaunchReset";
        
        [GameServerStartedEvent]
        public static void OnServerStart(DOLEvent e, object sender, EventArgs arguments)
        {
            GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, new DOLEventHandler(BetaLevelledPlayerEntered));
        }

        /// <summary>
        /// Event handler fired when server is stopped
        /// </summary>
        [GameServerStoppedEvent]
        public static void OnServerStop(DOLEvent e, object sender, EventArgs arguments)
        {
            GameEventMgr.RemoveHandler(GamePlayerEvent.GameEntered, new DOLEventHandler(BetaLevelledPlayerEntered));
        }
        
        /// <summary>
        /// Event handler fired when players enters the game
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        private static void BetaLevelledPlayerEntered(DOLEvent e, object sender, EventArgs arguments)
        {
            var player = sender as GamePlayer;
            if (player == null) return;
            
            var launch = new DateTime(2022, 06, 24, 00, 00, 00);

            var creationDate = player.DBCharacter.CreationDate;

            if (creationDate >= launch) return;

            var needsReset = DOLDB<DOLCharactersXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId")
                .IsEqualTo(player.ObjectId).And(DB.Column("KeyName").IsEqualTo(BetaResetKey)));

            if (needsReset != null) return;

            player.DBCharacter.Level = 50;
            player.RealmPoints = 0;
            player.RealmLevel = 0;
            player.Experience = 0;

            player.MoveToBind();

            player.RemoveAllSpecs();
            player.RemoveAllSpellLines();
            player.styleComponent.RemoveAllStyles();

            //reset before, and after changing the class.
            player.Reset();
            player.SetCharacterClass(player.CharacterClass.ID);
            player.Reset();
            player.RespecAll();

            //this is just for additional updates
            //that add all the new class changes.
            player.OnLevelUp(0);

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
            
            player.Out.SendMessage("Thanks for playing Beta! Your level has been reset to 1, we wish you good luck with your adventure.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }
        
    }
}
#endregion