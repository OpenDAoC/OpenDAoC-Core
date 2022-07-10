using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;
 
namespace DOL.GS.Keeps
{
    /// <summary>
    /// Class for the GateKeeper In the Keep (for leave)
    /// </summary>
    public class GateKeeperIn : GameKeepGuard
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private List<DBKeepDoorTeleport> m_destinationsIn = new List<DBKeepDoorTeleport>();
        private const string type = "GateKeeperIn";

        public override eFlags Flags
        {
            get { return eFlags.PEACE; }
        }

        public override bool AddToWorld()
        {
            if (!base.AddToWorld())
                return false;

            this.LoadDestinations();

            return true;
        }

        /// <summary>
        /// Use the NPC KeepID to find all the valid destinations for this teleporter
        /// </summary>
        protected void LoadDestinations()
        {
            if (this.m_destinationsIn.Count > 0)
                return;

            this.m_destinationsIn.AddRange(GameServer.Database.SelectObjects<DBKeepDoorTeleport>(" Text = 'sortir' AND TeleportType = '" + type + "'"));
        }

        /// <summary>
        /// Turn the teleporter to face the player.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            if (player.Client.Account.PrivLevel == 1)
            {
                if (IsCasting || player.Realm != this.Realm || !player.IsAlive || player.IsStunned || player.IsMezzed || !IsWithinRadius(player, 350))
                    return false;
            }
            this.LoadDestinations();
            SayTo(player, LanguageMgr.GetTranslation(player.Client.Account.Language, "GameGuard.GateKeeperIn.Interact"));

            GetTeleportLocation(player, "interact");

            return true;
        }

        /// <summary>
        /// Talk to the teleporter.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text))
                return false;

            GamePlayer player = source as GamePlayer;
            if (player == null)
                return false;

            if (player.Client.Account.PrivLevel == 1)
            {
                if (player.Realm != this.Realm || !player.IsAlive || player.IsStunned || player.IsMezzed || !IsWithinRadius(player, 350))
                    return false;
            }

            if (player.Client.Account.PrivLevel > 1 && text.ToLower() == "refresh")
            {
                this.m_destinationsIn.Clear();
                return false;
            }

            return GetTeleportLocation(player, text);
        }

        /// <summary>
        /// Teleport the player to the designated coordinates using the
        /// portal spell.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="destination"></param>
        protected virtual void OnTeleportSpell(GamePlayer player, DBKeepDoorTeleport destination, bool delayed)
        {
            if (destination == null)
                return;

            SpellLine spellLine = SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells);
            List<Spell> spellList = SkillBase.GetSpellList(GlobalSpellsLines.Mob_Spells);

            Spell spell = SkillBase.GetSpellByID(1889);	// UniPortal spell for Whisper

            Spell delayedSpell = SkillBase.GetSpellByID(1891);	// UniPortal spell delayed cast for Interact

            if (!delayed)
            {
                if (spell != null)
                {
                    TargetObject = player;
                    UniPortalKeep portalHandler = new UniPortalKeep(this, spell, spellLine, destination);
                    portalHandler.StartSpell(player);

                }
            }
            else
            {
                if (delayedSpell != null)
                {
                    TargetObject = player;
                    UniPortalKeep portalHandler = new UniPortalKeep(this, delayedSpell, spellLine, destination);
                    portalHandler.StartSpell(player);

                }
            }
        }

        /// <summary>
        /// Format "say" message and send it to target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="loc">chat location of the message</param>
        /// <param name="message"></param>
        public override void SayTo(GamePlayer target, eChatLoc loc, string message, bool announce = true)
        {
            if (target == null)
                return;

            string resultText = LanguageMgr.GetTranslation(target.Client.Account.Language, "GameNPC.SayTo.Says", GetName(0, true), message);
            switch (loc)
            {
                case eChatLoc.CL_PopupWindow:
                    target.Out.SendMessage(resultText, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    if (announce)
                    {
                        Message.ChatToArea(this, LanguageMgr.GetTranslation(target.Client.Account.Language, "GameNPC.SayTo.SpeaksTo", GetName(0, true), target.GetName(0, false)), eChatType.CT_System, WorldMgr.SAY_DISTANCE, target);
                    }
                    break;
                case eChatLoc.CL_ChatWindow:
                    target.Out.SendMessage(resultText, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
                    break;
                case eChatLoc.CL_SystemWindow:
                    target.Out.SendMessage(resultText, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;
            }
        }

        protected virtual bool GetTeleportLocation(GamePlayer player, string text)
        {
            DBKeepDoorTeleport port = null;
            if (text.ToLower().Contains("leave") || text.ToLower().Contains("sortir") || text == "interact")
            {

                foreach (DBKeepDoorTeleport t in m_destinationsIn)
                {
                    if (t == null) break;
                    if (t.KeepID == Component.Keep.KeepID)
                    {
                        port = t;
                        break;
                    }
                }
                if (port != null)
                {
                    if (port.Region == 0 && port.X == 0 && port.Y == 0 && port.Z == 0)
                    {
                        return false;
                    }
                    else
                    {
                        if (text == "interact")
                            OnTeleportSpell(player, port, true);
                        else
                            OnTeleportSpell(player, port, false);
                    }
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Class for the GateKeeper Out of the Keep (for enter)
    /// </summary>
    public class GateKeeperOut : GameKeepGuard
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private List<DBKeepDoorTeleport> m_destinationsOut = new List<DBKeepDoorTeleport>();
        private const string type = "GateKeeperOut";

        public override eFlags Flags
        {
            get { return eFlags.PEACE; }
        }

        public override bool AddToWorld()
        {
            if (!base.AddToWorld())
                return false;

            this.LoadDestinations();

            return true;
        }

        /// <summary>
        /// Use the NPC KeepID to find all the valid destinations for this teleporter
        /// </summary>
        protected void LoadDestinations()
        {
            if (this.m_destinationsOut.Count > 0)
                return;

            this.m_destinationsOut.AddRange(GameServer.Database.SelectObjects<DBKeepDoorTeleport>(" Text = 'entrer' AND TeleportType = '" + type + "'"));
        }

        /// <summary>
        /// Turn the teleporter to face the player.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            if (player.Client.Account.PrivLevel == 1)
            {
                if (IsCasting || player.Realm != this.Realm || !player.IsAlive || player.IsStunned || player.IsMezzed || !IsWithinRadius(player, 350))
                    return false;
            }

            this.LoadDestinations();
            SayTo(player, LanguageMgr.GetTranslation(player.Client.Account.Language, "GameGuard.GateKeeperOut.Interact"));

            GetTeleportLocation(player, "interact");

            return true;
        }

        /// <summary>
        /// Talk to the teleporter.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text))
                return false;

            GamePlayer player = source as GamePlayer;
            if (player == null)
                return false;

            if (player.Client.Account.PrivLevel == 1)
            {
                if (player.Realm != this.Realm || !player.IsAlive || player.IsStunned || player.IsMezzed || !IsWithinRadius(player, 350))
                    return false;
            }

            if (player.Client.Account.PrivLevel > 1 && text.ToLower() == "refresh")
            {
                this.m_destinationsOut.Clear();
                return false;
            }

            return GetTeleportLocation(player, text);
        }

        /// <summary>
        /// Teleport the player to the designated coordinates using the
        /// portal spell.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="destination"></param>
        protected virtual void OnTeleportSpell(GamePlayer player, DBKeepDoorTeleport destination, bool delayed)
        {
            if (destination == null)
                return;

            SpellLine spellLine = SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells);
            List<Spell> spellList = SkillBase.GetSpellList(GlobalSpellsLines.Mob_Spells);

            Spell spell = SkillBase.GetSpellByID(1889);	// UniPortal spell for Whisper

            Spell delayedSpell = SkillBase.GetSpellByID(1891);	// UniPortal spell delayed cast for Interact

            if (!delayed)
            {
                if (spell != null)
                {
                    TargetObject = player;
                    UniPortalKeep portalHandler = new UniPortalKeep(this, spell, spellLine, destination);
                    portalHandler.StartSpell(player);

                    return;
                }
            }
            else
            {
                if (delayedSpell != null)
                {
                    TargetObject = player;
                    UniPortalKeep portalHandler = new UniPortalKeep(this, delayedSpell, spellLine, destination);
                    portalHandler.StartSpell(player);

                    return;
                }
            }
        }

        /// <summary>
        /// Format "say" message and send it to target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="loc">chat location of the message</param>
        /// <param name="message"></param>
        public override void SayTo(GamePlayer target, eChatLoc loc, string message, bool announce = true)
        {
            if (target == null)
                return;

            string resultText = LanguageMgr.GetTranslation(target.Client.Account.Language, "GameNPC.SayTo.Says", GetName(0, true), message);
            switch (loc)
            {
                case eChatLoc.CL_PopupWindow:
                    target.Out.SendMessage(resultText, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    if (announce)
                    {
                        Message.ChatToArea(this, LanguageMgr.GetTranslation(target.Client.Account.Language, "GameNPC.SayTo.SpeaksTo", GetName(0, true), target.GetName(0, false)), eChatType.CT_System, WorldMgr.SAY_DISTANCE, target);
                    }
                    break;
                case eChatLoc.CL_ChatWindow:
                    target.Out.SendMessage(resultText, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
                    break;
                case eChatLoc.CL_SystemWindow:
                    target.Out.SendMessage(resultText, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;
            }
        }

        protected virtual bool GetTeleportLocation(GamePlayer player, string text)
        {
            DBKeepDoorTeleport port = null;
            if (text.ToLower().Contains("entrer") || text.ToLower().Contains("enter") || text == "interact")
            {
                foreach (DBKeepDoorTeleport t in this.m_destinationsOut)
                {
                    if (t == null) break;
                    if (t.KeepID == Component.Keep.KeepID)
                    {
                        port = t;
                        break;
                    }
                }
                if (port != null)
                {
                    if (port.Region == 0 && port.X == 0 && port.Y == 0 && port.Z == 0)
                    {
                        return false;
                    }
                    else
                    {
                        if (text == "interact")
                            OnTeleportSpell(player, port, true);
                        else
                            OnTeleportSpell(player, port, false);
                    }
                }
            }
            return true;
        }
    }
}