using System;
using System.Linq;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.Database.Attributes;
using DOL.GS;
using DOL.Database;          

namespace DOL.GS.Commands
{
    [CmdAttribute("&tele",
        ePrivLevel.GM, 
        "Teleports to a specified city location, dungeon, or keep.",
        "Usage: /tele <LocationName>",
        "/tele <location name>"
        )]
    public class TeleCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        // Konstante für den ClassType ist nicht mehr nötig, da wir nach allen Bereichen suchen
        
        /// <summary>
        /// Ruft die geografischen Daten (X, Y, Z, RegionID) für eine benannte Location (Stadt, Dungeon, Keep etc.) ab.
        /// </summary>
        private GameLocation GetTeleportLocation(string locationName)
        {
            // GENERALISIERT: Suche nach JEDEM DbArea-Eintrag, dessen Beschreibung den gesuchten Namen enthält.
            // Der Filter nach ClassType wird entfernt.
            DbArea targetArea = GameServer.Database.SelectAllObjects<DbArea>() 
                               .FirstOrDefault(area => area.Description.IndexOf(locationName, StringComparison.OrdinalIgnoreCase) >= 0);

            if (targetArea != null)
            {
                // Die Eigenschaften sind nun von DbArea (der Datenbank-Klasse) zugänglich.
                return new GameLocation(targetArea.Description, targetArea.Region, targetArea.X, targetArea.Y, targetArea.Z, 0); 
            }
            
            return null;
        }

        public void OnCommand(GameClient client, string[] args)
        {
            try
            {
                if (args.Length != 2)
                {
                    DisplaySyntax(client);
                    return;
                }

                string locationName = args[1]; // Name des Ortes (Stadt, Keep, Dungeon)
                GameLocation targetLocation = GetTeleportLocation(locationName);

                if (targetLocation == null)
                {
                    client.Out.SendMessage($"Der Ort '{locationName}' konnte nicht gefunden werden. Bitte überprüfen Sie den Namen.", 
                        eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }
                
                if (!CheckExpansion(client, client, targetLocation.RegionID))
                {
                    return;
                }
                
                // Tatsächliche Teleportation
                client.Player.MoveTo(targetLocation.RegionID, targetLocation.X, targetLocation.Y, targetLocation.Z, client.Player.Heading);

                Region targetRegion = WorldMgr.GetRegion(targetLocation.RegionID);
                string regionDescription = targetRegion?.Description ?? targetLocation.RegionID.ToString();
                
                client.Out.SendMessage($"Teleportiert zu '{targetLocation.Name}' in Region: {regionDescription}", 
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
            catch (Exception ex)
            {
                client.Out.SendMessage($"Fehler bei /tele: {ex.Message}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        public bool CheckExpansion(GameClient clientJumper, GameClient clientJumpee, ushort RegionID)
        {
            Region reg = WorldMgr.GetRegion(RegionID);
            if (reg != null && reg.Expansion > (int)clientJumpee.ClientType)
            {
                string messageJumper = LanguageMgr.GetTranslation(clientJumper, "GMCommands.Jump.CheckExpansion.CannotJump", clientJumpee.Player.Name, reg.Description);
                clientJumper.Out.SendMessage(messageJumper, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                
                if (clientJumper != clientJumpee)
                {
                    string messageJumpee = LanguageMgr.GetTranslation(clientJumpee, "GMCommands.Jump.CheckExpansion.ClientNoSup", clientJumpee.Player.Name, reg.Description);
                    clientJumpee.Out.SendMessage(messageJumpee, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                return false;
            }
            return true;
        }

        public override void DisplaySyntax(GameClient client)
        {
            string message = "Syntax: /tele <LocationName> - Teleportiert dich zum angegebenen Ort (Stadt, Dungeon, Keep etc.).";
            client.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
}