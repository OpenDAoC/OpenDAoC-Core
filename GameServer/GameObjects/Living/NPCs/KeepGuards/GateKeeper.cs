using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;
using Core.GS.Spells;
using Core.Language;

namespace Core.GS.Keeps
{
    /// <summary>
    /// Class for the GateKeeper In the Keep (for leave)
    /// </summary>
    public class GateKeeperIn : GameKeepGuard
    {
        private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private List<DbKeepDoorTeleport> m_destinationsIn = new List<DbKeepDoorTeleport>();
        private const string type = "GateKeeperIn";

        public override ENpcFlags Flags
        {
            get { return ENpcFlags.PEACE; }
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

            this.m_destinationsIn.AddRange(GameServer.Database.SelectObjects<DbKeepDoorTeleport>(DB.Column("Text").IsEqualTo("sortir").And(DB.Column("TeleportType").IsEqualTo(type))));
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
        protected virtual void OnTeleportSpell(GamePlayer player, DbKeepDoorTeleport destination, bool delayed)
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
                    UniPortalKeepSpell portalHandler = new UniPortalKeepSpell(this, spell, spellLine, destination);
                    portalHandler.StartSpell(player);

                }
            }
            else
            {
                if (delayedSpell != null)
                {
                    TargetObject = player;
                    UniPortalKeepSpell portalHandler = new UniPortalKeepSpell(this, delayedSpell, spellLine, destination);
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
        public override void SayTo(GamePlayer target, EChatLoc loc, string message, bool announce = true)
        {
            if (target == null)
                return;

            string resultText = LanguageMgr.GetTranslation(target.Client.Account.Language, "GameNPC.SayTo.Says", GetName(0, true), message);
            switch (loc)
            {
                case EChatLoc.CL_PopupWindow:
                    target.Out.SendMessage(resultText, EChatType.CT_System, EChatLoc.CL_PopupWindow);
                    if (announce)
                    {
                        MessageUtil.ChatToArea(this, LanguageMgr.GetTranslation(target.Client.Account.Language, "GameNPC.SayTo.SpeaksTo", GetName(0, true), target.GetName(0, false)), EChatType.CT_System, WorldMgr.SAY_DISTANCE, target);
                    }
                    break;
                case EChatLoc.CL_ChatWindow:
                    target.Out.SendMessage(resultText, EChatType.CT_Say, EChatLoc.CL_ChatWindow);
                    break;
                case EChatLoc.CL_SystemWindow:
                    target.Out.SendMessage(resultText, EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    break;
            }
        }

        protected virtual bool GetTeleportLocation(GamePlayer player, string text)
        {
            DbKeepDoorTeleport port = null;
            if (text.ToLower().Contains("leave") || text.ToLower().Contains("sortir") || text == "interact")
            {

                foreach (DbKeepDoorTeleport t in m_destinationsIn)
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
        private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private List<DbKeepDoorTeleport> m_destinationsOut = new List<DbKeepDoorTeleport>();
        private const string type = "GateKeeperOut";

        public override ENpcFlags Flags
        {
            get { return ENpcFlags.PEACE; }
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

            this.m_destinationsOut.AddRange(GameServer.Database.SelectObjects<DbKeepDoorTeleport>(DB.Column("Text").IsEqualTo("entrer").And(DB.Column("TeleportType").IsEqualTo(type))));
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
        protected virtual void OnTeleportSpell(GamePlayer player, DbKeepDoorTeleport destination, bool delayed)
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
                    UniPortalKeepSpell portalHandler = new UniPortalKeepSpell(this, spell, spellLine, destination);
                    portalHandler.StartSpell(player);

                    return;
                }
            }
            else
            {
                if (delayedSpell != null)
                {
                    TargetObject = player;
                    UniPortalKeepSpell portalHandler = new UniPortalKeepSpell(this, delayedSpell, spellLine, destination);
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
        public override void SayTo(GamePlayer target, EChatLoc loc, string message, bool announce = true)
        {
            if (target == null)
                return;

            string resultText = LanguageMgr.GetTranslation(target.Client.Account.Language, "GameNPC.SayTo.Says", GetName(0, true), message);
            switch (loc)
            {
                case EChatLoc.CL_PopupWindow:
                    target.Out.SendMessage(resultText, EChatType.CT_System, EChatLoc.CL_PopupWindow);
                    if (announce)
                    {
                        MessageUtil.ChatToArea(this, LanguageMgr.GetTranslation(target.Client.Account.Language, "GameNPC.SayTo.SpeaksTo", GetName(0, true), target.GetName(0, false)), EChatType.CT_System, WorldMgr.SAY_DISTANCE, target);
                    }
                    break;
                case EChatLoc.CL_ChatWindow:
                    target.Out.SendMessage(resultText, EChatType.CT_Say, EChatLoc.CL_ChatWindow);
                    break;
                case EChatLoc.CL_SystemWindow:
                    target.Out.SendMessage(resultText, EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    break;
            }
        }

        protected virtual bool GetTeleportLocation(GamePlayer player, string text)
        {
            DbKeepDoorTeleport port = null;
            if (text.ToLower().Contains("entrer") || text.ToLower().Contains("enter") || text == "interact")
            {
                foreach (DbKeepDoorTeleport t in this.m_destinationsOut)
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