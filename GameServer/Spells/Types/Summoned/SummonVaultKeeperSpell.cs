using System;
using System.Reflection;
using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.PacketHandler;

namespace Core.GS.Spells
{
    [SpellHandler("SummonVaultkeeper")]
    public class SummonVaultKeeperSpell : SpellHandler
    {
        protected GameVaultKeeper Npc;
        protected EcsGameTimer timer;

        public SummonVaultKeeperSpell(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            var template = NpcTemplateMgr.GetTemplate((int) m_spell.Value);

            if (template.ClassType == "")
                Npc = new GameVaultKeeper();
            else
            {
                try
                {
                    Npc = new GameVaultKeeper();
                    Npc = (GameVaultKeeper) Assembly.GetAssembly(typeof (GameServer)).CreateInstance(template.ClassType, false);
                }
                catch (Exception e)
                {
                }
                if (Npc == null)
                {
                    try
                    {
                        Npc = (GameVaultKeeper) Assembly.GetExecutingAssembly().CreateInstance(template.ClassType, false);
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
            Npc.Name = m_caster.Name + "'s Vaultkeeper";
            Npc.GuildName = "Temp Worker";
            switch (Npc.Realm)
            {
                case ERealm.Albion:
                    Npc.Model = 10;
                    break;
                case ERealm.Hibernia: 
                    Npc.Model = 307;
                    break;
                case ERealm.Midgard:
                    Npc.Model = 158;
                    break;
                case ERealm.None: 
                    Npc.Model = 10;
                    break;
            }
            Npc.SetOwnBrain(new BlankBrain());
            Npc.AddToWorld();
            timer = new EcsGameTimer(Npc, new EcsGameTimer.EcsTimerCallback(OnEffectExpires), Spell.Duration);
        }

        public int OnEffectExpires(EcsGameTimer timer)
        {
            Npc?.Delete();
            timer.Stop();
            return 0;
        }

        public override bool IsOverwritable(EcsGameSpellEffect compare)
        {
            return false;
        }
    }
}
