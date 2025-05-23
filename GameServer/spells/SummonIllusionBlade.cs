using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.IllusionBladeSummon)]
    public class IllusionBladeSummon : SummonSpellHandler
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            //Template of the Illusionblade NPC
            INpcTemplate template = NpcTemplateMgr.GetTemplate(Spell.LifeDrainReturn);

            if (template == null)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("NPC template {0} not found! Spell: {1}", Spell.LifeDrainReturn, Spell.ToString());
                MessageToCaster("NPC template " + (ushort)Spell.LifeDrainReturn + " not found!", eChatType.CT_System);
                return;
            }

            GameSpellEffect effect = CreateSpellEffect(target, CasterEffectiveness);
            IControlledBrain brain = GetPetBrain(Caster);
            m_pet = GetGamePet(template);
            m_pet.SetOwnBrain(brain as AI.ABrain);
            int x, y, z;
            ushort heading;
            Region region;

            GetPetLocation(out x, out y, out z, out heading, out region);

            m_pet.X = x;
            m_pet.Y = y;
            m_pet.Z = z;
            m_pet.Heading = heading;
            m_pet.CurrentRegion = region;
           // m_pet.CurrentSpeed = 0;
            m_pet.Realm = Caster.Realm;
            m_pet.Race = 0;
            m_pet.Level = 44; // lowered in patch 1109b, also calls AutoSetStats()
            m_pet.AddToWorld();
            //Check for buffs
            if (brain is ControlledMobBrain)
                (brain as ControlledMobBrain).CheckSpells(StandardMobBrain.eCheckSpellType.Defensive);

            AddHandlers();
            SetBrainToOwner(brain);

            effect.Start(m_pet);
            //Set pet infos & Brain
        }

        protected override GameSummonedPet GetGamePet(INpcTemplate template) { return new IllusionBladePet(template); }
        protected override IControlledBrain GetPetBrain(GameLiving owner) { return new ProcPetBrain(owner); }
        protected override void SetBrainToOwner(IControlledBrain brain) { }
        protected override void AddHandlers() { GameEventMgr.AddHandler(m_pet, GameLivingEvent.AttackFinished, EventHandler); }

        protected void EventHandler(DOLEvent e, object sender, EventArgs arguments)
        {
            AttackFinishedEventArgs args = arguments as AttackFinishedEventArgs;
            if (args == null || args.AttackData == null)
                return;
        }
        public IllusionBladeSummon(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }
    }
}

namespace DOL.GS
{
    public class IllusionBladePet : GameSummonedPet
    {
        public override int MaxHealth
        {
            get { return Level * 10; }
        }
        public override void OnAttackedByEnemy(AttackData ad) { }
        public IllusionBladePet(INpcTemplate npcTemplate) : base(npcTemplate) { }
    }
}
