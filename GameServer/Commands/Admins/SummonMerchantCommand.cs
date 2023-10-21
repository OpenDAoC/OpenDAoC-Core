﻿using System;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.PacketHandler;

namespace Core.GS.Commands
{
    [Command(
        "&summonmerchant",
        EPrivLevel.Admin, // Set to player.
        "/summonmerchant - summon a merchant at the cost of 10g")]
    public class SummonMerchantCommand : ACommandHandler, ICommandHandler
    {
        [ScriptLoadedEvent]
        public static void OnScriptLoaded(CoreEvent e, object sender, EventArgs args)
        {
            Spell load;
            load = MerchantSpell;
        }

        #region Command Timer

        public const string SummonMerch = "SummonMerch";

        public void OnCommand(GameClient client, string[] args)
        {
            var player = client.Player;
            var merchTick = player.TempProperties.GetProperty(SummonMerch, 0L);
            var changeTime = GameLoop.GameLoopTime - merchTick;
            if (changeTime < 30000)
            {
                player.Out.SendMessage(
                    "You must wait " + ((30000 - changeTime)/1000) + " more second to attempt to use this command!",
                    EChatType.CT_System, EChatLoc.CL_ChatWindow);
                return;
            }
            player.TempProperties.SetProperty(SummonMerch, GameLoop.GameLoopTime);

            #endregion Command timer
            
            #region Command spell Loader             

            var line = new SpellLine("MerchantCast", "Merchant Cast", "unknown", false);
            var spellHandler = ScriptMgr.CreateSpellHandler(client.Player, MerchantSpell, line);
            if (spellHandler != null)
                spellHandler.StartSpell(client.Player);
            client.Player.Out.SendMessage("You have summoned a merchant!", EChatType.CT_Important,
                EChatLoc.CL_SystemWindow);

            #endregion command spell loader
        }

        #region Spell

        protected static Spell MMerchantSpell;

        public static Spell MerchantSpell
        {
            get
            {
                if (MMerchantSpell == null)
                {
                    var spell = new DbSpell {CastTime = 0, ClientEffect = 0, Duration = 15};
                    spell.Description = "Summons a merchant to your location for " + spell.Duration + " seconds.";
                    spell.Name = "Merchant Spell";
                    spell.Type = "SummonMerchant";
                    spell.Range = 0;
                    spell.SpellID = 121232;
                    spell.Target = "Self";
                    spell.Value = MerchantTemplate.TemplateId;
                    MMerchantSpell = new Spell(spell, 1);
                    SkillBase.GetSpellList(GlobalSpellsLines.Item_Effects).Add(MMerchantSpell);
                }
                return MMerchantSpell;
            }
        }

        #endregion

        #region Npc

        protected static NpcTemplate MMerchantTemplate;

        public static NpcTemplate MerchantTemplate
        {
            get
            {
                if (MMerchantTemplate == null)
                {
                    MMerchantTemplate = new NpcTemplate();
                    MMerchantTemplate.Flags += (byte) ENpcFlags.GHOST + (byte) ENpcFlags.PEACE;
                    MMerchantTemplate.Name = "Merchant";
                    MMerchantTemplate.ClassType = "DOL.GS.Scripts.SummonedMerchant";
                    MMerchantTemplate.Model = "50";
                    MMerchantTemplate.TemplateId = 93049;
                    NpcTemplateMgr.AddTemplate(MMerchantTemplate);
                }
                return MMerchantTemplate;
            }
        }

        #endregion
    }
}
