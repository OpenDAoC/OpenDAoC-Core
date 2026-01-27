using System;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Database;
using System.Collections;

namespace DOL.GS.Scripts
{

    // Spawns a temporary teleport NPC at a boss's death location.
    // Teleport destination is based on the Boss Guildname via DbTeleport lookup. (/teleport add ...)
    // Automatically deletes itself after 5 minutes.
    // DungeonTeleporter.Create(this); needs to be added at boss's death
    public class DungeonTeleporter : GameNPC
    {
        protected ECSGameTimer m_cleanupTimer;
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;
            string targetLocation = this.GuildName;
            DbTeleport destination = WorldMgr.GetTeleportLocation(player.Realm, string.Format("{0}:{1}", string.Empty, targetLocation));
            if (player.InCombat == false && destination != null)
            {
                GameLocation currentLocation = new GameLocation("TeleportStart", player.CurrentRegionID, player.X, player.Y, player.Z);
                player.MoveTo((ushort) destination.RegionID, destination.X, destination.Y, destination.Z, (ushort) destination.Heading);
                GameServer.ServerRules.OnPlayerTeleport(player, currentLocation, destination);
            }
            return false;
        }

        public static void Create(GameObject boss)
        {
            if (boss == null) return;

            string npcGuild;
            ushort targetRegion, targetHeading;
            int targetX, targetY, targetZ;

            switch (boss.Name)
            {
                // Hibernia
                // Galladoria
                case "Olcasgean":
                    npcGuild = "Galladoria";
                    break;
                // Tur Suil
                case "Balor":
                    npcGuild = "Tur Suil";
                    break;
                // Fomor
                case "Anurigunda":
                    npcGuild = "Fomor";
                    break;


                // Albion
                // Krondon
                case "Orylle":
                    npcGuild = "Krondon";
                    break;
                // Avalon City
                case "Dura'ek the Empowered":
                    npcGuild = "Avalon City";
                    break;
                // Caer Sidi
                case "Apocalypse":
                    npcGuild = "Caer Sidi";
                    break;


                // Midgard
                // Trollheim
                case "Nosdoden":
                    npcGuild = "Trollheim";
                    break;
                // Iarnvidiur's Lair
                case "Iarnvidiur":
                    npcGuild = "Iarnvidiur's Lair";
                    break;
                // Tuscaran Glacier
                case "Queen Kula":
                    npcGuild = "Tuscaran Glacier";
                    break;



                // Aerus City (todo)
                case "The Phoenix":
                    npcGuild = "Aerus City";
                    break;
                default:
                    return;
            }

            DungeonTeleporter teleNPC = new DungeonTeleporter();

            teleNPC.Name = "Entrance";
            teleNPC.GuildName = npcGuild;
            teleNPC.Model = 1904;
            teleNPC.Size = 70;
            teleNPC.Level = 40;
            teleNPC.Realm = 0;
            teleNPC.Flags = eFlags.PEACE|eFlags.GHOST; // not attackable and ghost

            // Same position where boss died
            teleNPC.CurrentRegionID = boss.CurrentRegionID;
            teleNPC.X = boss.X;
            teleNPC.Y = boss.Y;
            teleNPC.Z = boss.Z;
            teleNPC.Heading = boss.Heading;

            teleNPC.AddToWorld();

            teleNPC.StartCleanupTimer(5);


            foreach (GamePlayer plr in teleNPC.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                plr.Out.SendMessage($"{teleNPC.Name} has been spawned, he brings you to the entrance!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        // Automatically remove NPC after 5min
        public void StartCleanupTimer(int minutes)
        {
            if (m_cleanupTimer != null) m_cleanupTimer.Stop();
            m_cleanupTimer = new ECSGameTimer(this, (timer) =>
            {
                this.Delete();
                return 0;
            });

            m_cleanupTimer.Start(minutes * 60 * 1000);
        }
        public override void Delete()
        {
            if (m_cleanupTimer != null)
            {
                m_cleanupTimer.Stop();
                m_cleanupTimer = null;
            }
            base.Delete();
        }
    }
}