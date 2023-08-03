using System;
using DOL.GS.Effects;
using DOL.Database;
using DOL.Events;
using DOL.AI.Brain;

namespace DOL.GS.Spells
{
	[SpellHandlerAttribute("TraitorsDaggerProc")]
	public class TraitorsDaggerProcHandler : OffensiveProcSpellHandler
	{
		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect);
			if (effect.Owner is GamePlayer)
			{
				GamePlayer player = effect.Owner as GamePlayer;
				foreach (GameSpellEffect Effect in player.EffectList.GetAllOfType<GameSpellEffect>())
                {
                    if (Effect.SpellHandler.Spell.SpellType.Equals("ShadesOfMist") || 
                        Effect.SpellHandler.Spell.SpellType.Equals("DreamMorph") ||
                        Effect.SpellHandler.Spell.SpellType.Equals("DreamGroupMorph") ||
                        Effect.SpellHandler.Spell.SpellType.Equals("MaddeningScalars") ||
                        Effect.SpellHandler.Spell.SpellType.Equals("AtlantisTabletMorph") ||
                        Effect.SpellHandler.Spell.SpellType.Equals("AlvarusMorph"))
                    {
                        player.Out.SendMessage("You already have an active morph!", DOL.GS.PacketHandler.EChatType.CT_SpellResisted, DOL.GS.PacketHandler.EChatLoc.CL_ChatWindow);
                        return;
                    }
                }
				player.Shade(true);
                player.Out.SendUpdatePlayer();
			}
		}
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			if (effect.Owner is GamePlayer)
			{
				GamePlayer player = effect.Owner as GamePlayer;
				player.Shade(false);
                player.Out.SendUpdatePlayer();
			}
			return base.OnEffectExpires(effect, noMessages);
		}
   
		public TraitorsDaggerProcHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}

    [SpellHandler("DdtProcDd")]
    public class DdtProcDdHandler:DirectDmgHandler
    {
        public DdtProcDdHandler(GameLiving caster,Spell spell,SpellLine line) : base(caster,spell,line) { }

        public override void OnDirectEffect(GameLiving target,double effectiveness)
        {
            base.OnDirectEffect(target,effectiveness);
            Caster.ChangeHealth(Caster,EHealthChangeType.Spell,-Spell.ResurrectHealth);
        }
    }

    [SpellHandler("TraitorsDaggerSummon")]
    public class TraitorsDaggerSummonHandler : SummonHandler
    {
        private ISpellHandler _trap;

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            //Set pet infos & Brain
            base.ApplyEffectOnTarget(target, effectiveness);
            ProcPetBrain petBrain = (ProcPetBrain) m_pet.Brain;
            petBrain.AddToAggroList(target, 1);
            petBrain.Think();
        }

        protected override GameSummonedPet GetGamePet(INpcTemplate template) { return new TraitorsDaggerPet(template); }
        protected override IControlledBrain GetPetBrain(GameLiving owner) { return new ProcPetBrain(owner); }
        protected override void SetBrainToOwner(IControlledBrain brain) { }
        protected override void AddHandlers() { GameEventMgr.AddHandler(m_pet, GameLivingEvent.AttackFinished, EventHandler); }

        protected void EventHandler(CoreEvent e, object sender, EventArgs arguments)
        {
            AttackFinishedEventArgs args = arguments as AttackFinishedEventArgs;
            if(args == null || args.AttackData == null)
                return;
            // Spirit procs lifetap when hitting ennemy
            if(_trap == null)
            {
                _trap = MakeTrap();
            }
            if(UtilCollection.Chance(50))
            {
                _trap.StartSpell(args.AttackData.Target);
            }
        }

        private ISpellHandler MakeTrap()
        {
            DBSpell dbs = new DBSpell();
            dbs.Name = "Increased Essence Consumption";
            dbs.Icon = 11020;
            dbs.ClientEffect = 11020;
            dbs.DamageType = 10;
            dbs.Target = "Enemy";
            dbs.Radius = 0;
            dbs.Type = ESpellType.PetLifedrain.ToString();
            dbs.Damage = 70;
            dbs.LifeDrainReturn = 100;
            dbs.Value = -100;
            dbs.Duration = 0;
            dbs.Frequency = 0;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = 350;
            Spell s = new Spell(dbs, 50);
            SpellLine sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            return ScriptMgr.CreateSpellHandler(m_pet, s, sl);
        }

        public TraitorsDaggerSummonHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }
    }
}

namespace DOL.GS
{
	public class TraitorsDaggerPet : GameSummonedPet
	{
		public override int MaxHealth
		{
			get { return Level * 15; }
		}
		public override void OnAttackedByEnemy(AttackData ad) { }
		public TraitorsDaggerPet(INpcTemplate npcTemplate) : base(npcTemplate) { }
	}
}