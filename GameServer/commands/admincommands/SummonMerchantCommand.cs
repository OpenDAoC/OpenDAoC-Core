using System;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [Cmd(
        "&summonmerchant",
        // Message: '/summonmerchant' - Summons a merchant for a short period of time.
        "AdminCommands.SummonMerchant.CmdList.Description",
        // Message: <----- '/{0}' Command {1}----->
        "AllCommands.Header.General.Commands",
        // Required minimum privilege level to use the command
        ePrivLevel.Admin,
        // Message: Summons a merchant for a short period of time.
        "AdminCommands.SummonMerchant.Description",
        // Message: /summonmerchant
        "AdminCommands.SummonMerchant.Syntax.Summon",
        // Message: Summons a merchant at the client's present location.
        "AdminCommands.SummonMerchant.Usage.Summon"
    )]
    public class SummonMerchantCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        [ScriptLoadedEvent]
        public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            Spell load;
            load = MerchantSpell;
        }

        #region Command Timer

        public const string SummonMerch = "SummonMerch";

        public void OnCommand(GameClient client, string[] args)
        {
            var player = client.Player;
            var merchTick = player.TempProperties.getProperty(SummonMerch, 0L);
            var changeTime = GameLoop.GameLoopTime - merchTick;
            
            if (changeTime < 30000)
            {
                // Message: You must wait {0} more seconds before you can use this command again!
                ChatUtil.SendTypeMessage(eMsg.Error, client, "AdminCommands.SummonMerchant.Err.WaitSeconds", ((30000 - changeTime)/1000));
                return;
            }
            
            player.TempProperties.setProperty(SummonMerch, GameLoop.GameLoopTime);

            var line = new SpellLine("MerchantCast", "Merchant Cast", "unknown", false);
            var spellHandler = ScriptMgr.CreateSpellHandler(client.Player, MerchantSpell, line);
            
            if (spellHandler != null)
                spellHandler.StartSpell(client.Player);
            
            // Message: You have summoned a merchant!
            ChatUtil.SendTypeMessage(eMsg.Success, client, "AdminCommands.SummonMerchant.Msg.SummonedMerchant", null);
        }
        #endregion Command Timer

        #region Spell
        protected static Spell MMerchantSpell;

        public static Spell MerchantSpell
        {
            get
            {
                if (MMerchantSpell == null)
                {
                    var spell = new DBSpell {CastTime = 0, ClientEffect = 0, Duration = 15};
                    spell.Description = "Summons a merchant at your location for " + spell.Duration + " seconds.";
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
        #endregion Spell

        #region Merchant
        protected static NpcTemplate MMerchantTemplate;

        public static NpcTemplate MerchantTemplate
        {
            get
            {
                if (MMerchantTemplate == null)
                {
                    MMerchantTemplate = new NpcTemplate();
                    MMerchantTemplate.Flags += (byte) GameNPC.eFlags.GHOST + (byte) GameNPC.eFlags.PEACE;
                    MMerchantTemplate.Name = "Merchant";
                    MMerchantTemplate.ClassType = "DOL.GS.Scripts.SummonedMerchant";
                    MMerchantTemplate.Model = "50";
                    MMerchantTemplate.TemplateId = 93049;
                    NpcTemplateMgr.AddTemplate(MMerchantTemplate);
                }
                return MMerchantTemplate;
            }
        }
        #endregion Merchant
    }
}