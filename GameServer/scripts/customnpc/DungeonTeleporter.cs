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
        protected DbTeleport m_destination;

        // What happens when player clicks on me
        public override bool Interact(GamePlayer player)
        {
            if (!this.IsWithinRadius(player, WorldMgr.INTERACT_DISTANCE))
            {
                player.Out.SendMessage("You are too far away!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return false;
            }
            string msg = $"Greetings, " + player.Name + " you want me to bring you to the [entrance] of the dungeon ?";
            SayTo(player, msg);
            return true;
        }

        // When played clicked [entrance] or did /whisper entrance
        public override bool WhisperReceive(GameLiving source, string text)
        {
            GamePlayer player = source as GamePlayer;
            if (player == null) return false;

            // Distance check
            if (!this.IsWithinRadius(player, WorldMgr.INTERACT_DISTANCE))
            {
                player.Out.SendMessage("You are too far away!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (text.ToLower().Contains("entrance"))
            {
                if (m_destination != null && !player.InCombat)
                {
                    GameLocation currentLocation = new GameLocation("TeleportStart", player.CurrentRegionID, player.X, player.Y, player.Z);
                    player.MoveTo((ushort)m_destination.RegionID, m_destination.X, m_destination.Y, m_destination.Z, (ushort)m_destination.Heading);
                    GameServer.ServerRules.OnPlayerTeleport(player, currentLocation, m_destination);
                }
                else if (player.InCombat)
                {
                    player.Out.SendMessage("You cannot leave while in combat!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
            }
            return true;
        }

        public static void Create(GameObject boss)
        {
            if (boss == null) return;

            string npcGuild;

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
            DbTeleport destination = DOLDB<DbTeleport>.SelectObject(DB.Column("TeleportID").IsEqualTo(npcGuild));

            // Dont create NPC when teleport location not found
            if (destination == null) return;

            DungeonTeleporter teleNPC = new DungeonTeleporter();

            teleNPC.Name = "Entrance";
            teleNPC.GuildName = npcGuild;
            teleNPC.Model = 826;
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
            teleNPC.m_destination = destination;

            teleNPC.AddToWorld();

            teleNPC.StartCleanupTimer(5);
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