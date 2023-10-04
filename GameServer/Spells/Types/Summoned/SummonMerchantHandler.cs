﻿using System;
using System.Reflection;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandler("SummonMerchant")]
    public class SummonMerchantSpellHandler : SpellHandler
    {
        protected GameMerchant Npc;
        protected ECSGameTimer timer;

        //private long SummonedTick;
        //private long EndTick;

        public SummonMerchantSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            var template = NpcTemplateMgr.GetTemplate((int) m_spell.Value);

            if (template.ClassType == "")
                Npc = new GameAtlasMerchant();
            else
            {
                try
                {
                    Npc = new GameAtlasMerchant();
                    Npc = (GameAtlasMerchant) Assembly.GetAssembly(typeof (GameServer)).CreateInstance(template.ClassType, false);
                }
                catch (Exception e)
                {
                }
                if (Npc == null)
                {
                    try
                    {
                        Npc = (GameAtlasMerchant) Assembly.GetExecutingAssembly().CreateInstance(template.ClassType, false);
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
           
            Point2D point = m_caster.GetPointFromHeading(m_caster.Heading, 64);
            Npc.X = point.X;
            Npc.Y = point.Y;
            Npc.Z = m_caster.Z;
            Npc.Flags += (byte) GameNPC.eFlags.GHOST + (byte) GameNPC.eFlags.PEACE;
            Npc.CurrentRegion = m_caster.CurrentRegion;
            Npc.Heading = (ushort) ((m_caster.Heading + 2048)%4096);
            Npc.Realm = m_caster.Realm;
            Npc.CurrentSpeed = 0;
            Npc.Level = m_caster.Level;
            Npc.Name = m_caster.Name + "'s Merchant";
            Npc.GuildName = "Temp Worker";
            switch (Npc.Realm)
            {
                case eRealm.Albion:
                    Npc.TradeItems = new MerchantTradeItems("portable_merchant");
                    Npc.Model = 10;
                    break;
                case eRealm.Hibernia: 
                    Npc.TradeItems = new MerchantTradeItems("portable_merchant3");
                    Npc.Model = 307;
                    break;
                case eRealm.Midgard:
                    Npc.TradeItems = new MerchantTradeItems("portable_merchant2");
                    Npc.Model = 158;
                    break;
                case eRealm.None: 
                    Npc.TradeItems = new MerchantTradeItems("portable_merchant");
                    Npc.Model = 10;
                    break;
            }
            Npc.SetOwnBrain(new BlankBrain());
            Npc.AddToWorld();
           // SummonedTick = GameLoop.GameLoopTime;
            //EndTick = GameLoop.GameLoopTime + Spell.Duration * 1000;
            //Console.WriteLine($"Summoned merchant summon {SummonedTick} end {EndTick} duration {Spell.Duration}");
            timer = new ECSGameTimer(Npc, new ECSGameTimer.ECSTimerCallback(OnEffectExpires), Spell.Duration);
            timer.Start();
        }

        public int OnEffectExpires(ECSGameTimer timer)
        {
            Npc?.Delete();
            //timer.Stop();
            return 0;
            //return base.OnEffectExpires(effect, noMessages);
        }

        public override bool IsOverwritable(ECSGameSpellEffect compare)
        {
            return false;
        }
    }
}
