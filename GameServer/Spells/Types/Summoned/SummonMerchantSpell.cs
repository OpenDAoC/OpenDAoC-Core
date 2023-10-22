using System;
using System.Reflection;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Players.Clients;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.Spells;

[SpellHandler("SummonMerchant")]
public class SummonMerchantSpell : SpellHandler
{
    protected GameMerchant Npc;
    protected EcsGameTimer timer;

    //private long SummonedTick;
    //private long EndTick;

    public SummonMerchantSpell(GameLiving caster, Spell spell, SpellLine line)
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
                    EChatType.CT_System);
                return;
            }
            Npc.LoadTemplate(template);
        }
       
        Point2D point = m_caster.GetPointFromHeading(m_caster.Heading, 64);
        Npc.X = point.X;
        Npc.Y = point.Y;
        Npc.Z = m_caster.Z;
        Npc.Flags += (byte) ENpcFlags.GHOST + (byte) ENpcFlags.PEACE;
        Npc.CurrentRegion = m_caster.CurrentRegion;
        Npc.Heading = (ushort) ((m_caster.Heading + 2048)%4096);
        Npc.Realm = m_caster.Realm;
        Npc.CurrentSpeed = 0;
        Npc.Level = m_caster.Level;
        Npc.Name = m_caster.Name + "'s Merchant";
        Npc.GuildName = "Temp Worker";
        switch (Npc.Realm)
        {
            case ERealm.Albion:
                Npc.TradeItems = new MerchantTradeItems("portable_merchant");
                Npc.Model = 10;
                break;
            case ERealm.Hibernia: 
                Npc.TradeItems = new MerchantTradeItems("portable_merchant3");
                Npc.Model = 307;
                break;
            case ERealm.Midgard:
                Npc.TradeItems = new MerchantTradeItems("portable_merchant2");
                Npc.Model = 158;
                break;
            case ERealm.None: 
                Npc.TradeItems = new MerchantTradeItems("portable_merchant");
                Npc.Model = 10;
                break;
        }
        Npc.SetOwnBrain(new BlankBrain());
        Npc.AddToWorld();
       // SummonedTick = GameLoop.GameLoopTime;
        //EndTick = GameLoop.GameLoopTime + Spell.Duration * 1000;
        //Console.WriteLine($"Summoned merchant summon {SummonedTick} end {EndTick} duration {Spell.Duration}");
        timer = new EcsGameTimer(Npc, new EcsGameTimer.EcsTimerCallback(OnEffectExpires), Spell.Duration);
        timer.Start();
    }

    public int OnEffectExpires(EcsGameTimer timer)
    {
        Npc?.Delete();
        //timer.Stop();
        return 0;
        //return base.OnEffectExpires(effect, noMessages);
    }

    public override bool IsOverwritable(EcsGameSpellEffect compare)
    {
        return false;
    }
}