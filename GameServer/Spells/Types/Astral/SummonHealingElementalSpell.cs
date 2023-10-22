using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.Spells
{
    /// <summary>
    /// Summons a Elemental that only follows the caster.
    /// </summary>
    [SpellHandler("SummonHealingElemental")]
    public class SummonHealingElementalSpell : MasterLevelSpellHandling
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        GameNpc summoned = null;
        GameSpellEffect beffect = null;
        public SummonHealingElementalSpell(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)  {}

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GamePlayer player = Caster as GamePlayer;
            if (player == null)
            {
                return;
            }

            INpcTemplate template = NpcTemplateMgr.GetTemplate(Spell.LifeDrainReturn);
            if (template == null)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("NPC template {0} not found! Spell: {1}", Spell.LifeDrainReturn, Spell.ToString());
                MessageToCaster("NPC template " + Spell.LifeDrainReturn + " not found!", EChatType.CT_System);
                return;
            }

            Point2D summonloc;
            beffect = CreateSpellEffect(target, Effectiveness);
            {
                summonloc = target.GetPointFromHeading(target.Heading, 64);

                BrittleGuardBrain controlledBrain = new BrittleGuardBrain(player);
                controlledBrain.IsMainPet = false;
                summoned = new GameNpc(template);
                summoned.SetOwnBrain(controlledBrain);
                summoned.X = summonloc.X;
                summoned.Y = summonloc.Y;
                summoned.Z = target.Z;
                summoned.CurrentRegion = target.CurrentRegion;
                summoned.Heading = (ushort)((target.Heading + 2048) % 4096);
                summoned.Realm = target.Realm;
                summoned.CurrentSpeed = 0;
                summoned.Level = Caster.Level;
                summoned.Size = 50;
                summoned.AddToWorld();
                controlledBrain.AggressionState = EAggressionState.Passive;
                beffect.Start(Caster);
            }
        }

        /// <summary>
        /// When an applied effect expires.
        /// Duration spells only.
        /// </summary>
        /// <param name="effect">The expired effect</param>
        /// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
        /// <returns>immunity duration in milliseconds</returns>
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (summoned != null)
            {
                summoned.Health = 0; // to send proper remove packet
                summoned.Delete();
            }
            return base.OnEffectExpires(effect, noMessages);
        }
    }
}