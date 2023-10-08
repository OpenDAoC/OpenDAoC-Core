using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.Events;
using DOL.GS.Spells;

namespace DOL.GS.Effects
{
    public class NfRaSearingPetEffect : TimedEffect
    {
        // Parameters
        private const ushort spellRadius = 350; 		// Radius of the RA
        private const int spellDamage = 25;			// pbaoe damage
        private const int spellFrequency = 3;		// pbaoe pulse frequency

        // Objects
        private Spell petSpell;				// The spell to cast
        private SpellLine petSpellLine;	 		// The spell line
        private ISpellHandler pbaoe;					// The Spell handler
        private GamePlayer EffectOwner;			// Owner of the effect
        private GameNPC pet;					// The pet
        private EcsGameTimer pulseTimer;				// Pulse timer
        private int currentTick = 0;		// Count ticks

        public NfRaSearingPetEffect(GamePlayer owner)
            : base(RealmAbilities.NfRaSearingPetAbility.DURATION)
        {
            EffectOwner = owner;

            // Build spell
            DbSpell tSpell = new DbSpell();
            tSpell.AllowAdd = false;
            tSpell.Description = "Damage the target.";
            tSpell.Name = "PBAoE damage";
            tSpell.Target = "Enemy";
            tSpell.Radius = 0;
            tSpell.Range = WorldMgr.VISIBILITY_DISTANCE;
            tSpell.CastTime = 0;
            tSpell.Duration = 0;
            tSpell.Frequency = 0;
            tSpell.Pulse = 0;
            tSpell.Uninterruptible = true;
            tSpell.Type = ESpellType.DirectDamage.ToString();
            tSpell.Damage = spellDamage;
            tSpell.DamageType = (int)EDamageType.Heat;
            tSpell.Value = 0;
            tSpell.Icon = 476;			// not official effect
            tSpell.ClientEffect = 476;	// not official effect
            petSpell = new Spell(tSpell, 1);
            petSpellLine = new SpellLine("RAs", "RealmAbilitys", "RealmAbilitys", true);
        }

        public override void Start(GameLiving target)
        {
            base.Start(target);
            if (target is GameNPC)
            {
                pet = target as GameNPC;
                pbaoe = ScriptMgr.CreateSpellHandler(EffectOwner, petSpell, petSpellLine);
                pulseTimer = new EcsGameTimer(EffectOwner, new EcsGameTimer.EcsTimerCallback(PulseTimer), 1000);
                GameEventMgr.AddHandler(EffectOwner, GamePlayerEvent.Quit, new CoreEventHandler(PlayerLeftWorld));
            }
        }
        public override void Stop()
        {
            if (pulseTimer != null) { pulseTimer.Stop(); pulseTimer = null; }
            if (EffectOwner != null)
                GameEventMgr.RemoveHandler(EffectOwner, GamePlayerEvent.Quit, new CoreEventHandler(PlayerLeftWorld));

            base.Stop();
        }
        protected virtual int PulseTimer(EcsGameTimer timer)
        {
            if (EffectOwner == null || pet == null || pbaoe == null)
            {
                timer.Stop();
                timer = null;
                return 0;
            }
            if (currentTick % spellFrequency == 0)
            {
                foreach (GamePlayer target in pet.GetPlayersInRadius(spellRadius))
                {
                    pbaoe.StartSpell(target);
                }
                foreach (GameNPC npc in pet.GetNPCsInRadius(spellRadius))
                {
                    pbaoe.StartSpell(npc);
                }
            }
            currentTick++;
            return 1000;
        }

        /// <summary>
        /// Called when a player leaves the game
        /// </summary>
        /// <param name="e">The event which was raised</param>
        /// <param name="sender">Sender of the event</param>
        /// <param name="args">EventArgs associated with the event</param>
        private static void PlayerLeftWorld(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;
            if (player != null && player.ControlledBrain != null && player.ControlledBrain.Body != null)
            {
                GameNPC pet = player.ControlledBrain.Body as GameNPC;
				NfRaSearingPetEffect SearingPet = pet.EffectList.GetOfType<NfRaSearingPetEffect>();
                if (SearingPet != null)
                    SearingPet.Cancel(false);
            }
        }

        public override string Name { get { return "Searing pet"; } }
        public override ushort Icon { get { return 7064; } }

        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add("PBAoE Pet pulsing effect.");
                return list;
            }
        }
    }
}
