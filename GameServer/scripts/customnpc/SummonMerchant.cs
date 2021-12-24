/* Summon Buffbot Command - Created By Deathwish, with a BIG THANKS to geshi for his help!
 * Version 1.0 (13/07/2010) For the use of all Dol Members.
 * 
 * Please share any updates or changes.
 * 
 * This script will summon a Buffbot for the cost of 5bps, that lasts 30 seconds.
 * This script contains everything you need to run the script.
 * 
 * How to use: Place script into your scripts folder.
 * InGame Use: /bb to summon (5 bps is needed to summon or you cant summon the Buffbot)
 * 
 * To change Buffbots name guild etc see line 204.
 * (I am not the creator of the buffbot script i have added to this script,
 * its only there for people that dont have a working bb and to make life easier for people that cant use C#!)
 * 
 * Update V1.1 (27/07/10)
 * Now summon buffbot will load in rvr zones, also remove the loading up error
 * 
 * Updated V1.2 (02/08/10)
 * Added a timer for 30 sec so player cant abuse the script.
 * 
 * Added check to to prevent recasting of buffs on buffed player
 * Removed the Bounty Cost
*/



#region

using System;
using System.Collections;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

#endregion

namespace DOL.GS.Commands
{
    [Cmd(
        "&summonmerchant",
        ePrivLevel.Admin, // Set to player.
        "/summonmerchant - summon a merchant at the cost of 10g")]
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
                player.Out.SendMessage(
                    "You must wait " + ((30000 - changeTime)/1000) + " more second to attempt to use this command!",
                    eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }
            player.TempProperties.setProperty(SummonMerch, GameLoop.GameLoopTime);

            #endregion Command timer
            
            #region Command spell Loader             

            var line = new SpellLine("MerchantCast", "Merchant Cast", "unknown", false);
            var spellHandler = ScriptMgr.CreateSpellHandler(client.Player, MerchantSpell, line);
            if (spellHandler != null)
                spellHandler.StartSpell(client.Player);
            client.Player.Out.SendMessage("You have summoned a merchant!", eChatType.CT_Important,
                eChatLoc.CL_SystemWindow);

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
                    var spell = new DBSpell {CastTime = 0, ClientEffect = 0, Duration = 15};
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

        #endregion
    }
}

namespace DOL.GS.Scripts
{
    public class SummonedMerchant : GameMerchant
    {
        

        public override bool AddToWorld()
        {
            switch (Realm)
            {
                case eRealm.Albion:Model = 10;break;
                case eRealm.Hibernia: Model = 307;break;
                case eRealm.Midgard:Model = 158;break;
                case eRealm.None: Model = 10;break;
            }
            GuildName = "Temp Worker";
            Realm = eRealm.None;
            return base.AddToWorld();
        }

        

        

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;
            if (player.InCombat)
            {
                player.Out.SendMessage("Merchant says \"stop your combat if you want me speak with me!\"", eChatType.CT_Say,
                    eChatLoc.CL_ChatWindow);
                return false;
            }

            if (GetDistanceTo(player) > WorldMgr.INTERACT_DISTANCE)
            {
                player.Out.SendMessage("You are too far away " + GetName(0, false) + ".", eChatType.CT_System,
                    eChatLoc.CL_SystemWindow);
                return false;
            }

            TurnTo(player, 3000);

            return true;
        }
        
    }
}

#region Summon 

namespace DOL.GS.Spells
{
    [SpellHandler("SummonMerchant")]
    public class SummonMerchantSpellHandler : SpellHandler
    {
        protected GameMerchant Npc;
        protected RegionTimer timer;

        public SummonMerchantSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            var template = NpcTemplateMgr.GetTemplate((int) m_spell.Value);
            
            //base.ApplyEffectOnTarget(target, effectiveness);

            if (template.ClassType == "")
                Npc = new GameMerchant();
            else
            {
                try
                {
                    Npc = new GameMerchant();
                    Npc = (GameMerchant) Assembly.GetAssembly(typeof (GameServer)).CreateInstance(template.ClassType, false);
                }
                catch (Exception e)
                {
                }
                if (Npc == null)
                {
                    try
                    {
                        Npc = (GameMerchant) Assembly.GetExecutingAssembly().CreateInstance(template.ClassType, false);
                    }
                    catch (Exception e)
                    {
                    }
                }
                if (Npc == null)
                {
                    MessageToCaster("There was an error creating an instance of " + template.ClassType + "!",
                        eChatType.CT_System);
                    return;
                }
                Npc.LoadTemplate(template);
            }
           
            int x, y;
            m_caster.GetSpotFromHeading(64, out x, out y);
            Npc.X = x;
            Npc.Y = y;
            Npc.Z = m_caster.Z;
            Npc.CurrentRegion = m_caster.CurrentRegion;
            Npc.Heading = (ushort) ((m_caster.Heading + 2048)%4096);
            Npc.Realm = m_caster.Realm;
            Npc.CurrentSpeed = 0;
            Npc.Level = m_caster.Level;
            Npc.Name = m_caster.Name + "'s Merchant";
            Npc.SetOwnBrain(new BlankBrain());
            Npc.AddToWorld();
            timer = new RegionTimer(Npc, new RegionTimerCallback(OnEffectExpires), Spell.Duration);
        }

        public int OnEffectExpires(RegionTimer timer)
        {
            Npc?.Delete();
            timer.Stop();
            return 0;
            //return base.OnEffectExpires(effect, noMessages);
        }

        public override bool IsOverwritable(GameSpellEffect compare)
        {
            return false;
        }
    }
}

#endregion