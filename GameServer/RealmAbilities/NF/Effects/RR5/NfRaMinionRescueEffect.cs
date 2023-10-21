using System.Collections.Generic;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Spells;

namespace Core.GS.Effects
{
    public class NfRaMinionRescueEffect : TimedEffect
    {
        // Parameters
        private const int spiritCount = 8; 			// Max number of spirits to summon
        private const byte spiritLevel = 50;			// Level of the spirit
        private const int spiritModel = 908; 		// Model to use for spirit
        private const int spiritSpeed = 350; 		// Max speed of the spirit
        private const string spiritName = "Spirit";	// Name of spirit
        private const int spellDuration = 4;		// Duration of stun in seconds

        // Objects
        private GameNpc[] spirits;				// Array containing spirits
        private EcsGameTimer[] spiritTimer;			// Array containing spirit timers
        private Spell spiritSpell;			// The spell to cast
        private SpellLine spiritSpellLine;	 	// The spell line
        private ISpellHandler stun;					// The spell handler
        private GamePlayer EffectOwner;			// Owner of the effect

        public NfRaMinionRescueEffect()
            : base(RealmAbilities.NfRaMinionRescueAbility.DURATION)
        {
            // Init NPC & Timer array
            spirits = new GameNpc[spiritCount];
            spiritTimer = new EcsGameTimer[spiritCount];

            // Build spell
            DbSpell tSpell = new DbSpell();
            tSpell.AllowAdd = false;
            tSpell.Description = "Target is stunned and can't move or do any action during spell duration.";
            tSpell.Name = "Rescue stun";
            tSpell.Target = "Enemy";
            tSpell.Radius = 0;
            tSpell.Range = WorldMgr.VISIBILITY_DISTANCE;
            tSpell.CastTime = 0;
            tSpell.Duration = spellDuration;
            tSpell.Uninterruptible = true;
            tSpell.Type = ESpellType.Stun.ToString();
			tSpell.ResurrectMana=1;
			tSpell.ResurrectHealth=1;
            tSpell.Damage = 0;
            tSpell.DamageType = 0;
            tSpell.Value = 0;
            tSpell.Icon = 7049;
            tSpell.ClientEffect = 7049;
            spiritSpell = new Spell(tSpell, 1);
            spiritSpellLine = new SpellLine("RAs", "RealmAbilitys", "RealmAbilitys", true);
        }

        public override void Start(GameLiving target)
        {
            base.Start(target);
            if (target is GamePlayer)
            {
                EffectOwner = target as GamePlayer;
                stun = ScriptMgr.CreateSpellHandler(EffectOwner, spiritSpell, spiritSpellLine);

                int targetCount = 0;
                foreach (GamePlayer targetPlayer in EffectOwner.GetPlayersInRadius((ushort)RealmAbilities.NfRaMinionRescueAbility.SpellRadius))
                {
                    if (targetCount == spiritCount) return;
                    if (targetPlayer.IsAlive && GameServer.ServerRules.IsAllowedToAttack(EffectOwner, targetPlayer, true))
                    {
                        SummonSpirit(targetCount, targetPlayer);
                        targetCount++;
                    }
                }
            }
        }

        public override void Stop()
        {
            for (int index = 0; index < spiritCount; index++)
            {
                if (spiritTimer[index] != null) { spiritTimer[index].Stop(); spiritTimer[index] = null; }
                if (spirits[index] != null) { spirits[index].Delete(); spirits[index] = null; }
            }

            base.Stop();
        }

        // Summon a spirit that will follow target
        private void SummonSpirit(int spiritId, GamePlayer targetPlayer)
        {
            spirits[spiritId] = new GameNpc();
            spirits[spiritId].CurrentRegion = EffectOwner.CurrentRegion;
            spirits[spiritId].Heading = (ushort)((EffectOwner.Heading + 2048) % 4096);
            spirits[spiritId].Level = spiritLevel;
            spirits[spiritId].Realm = EffectOwner.Realm;
            spirits[spiritId].Name = spiritName;
            spirits[spiritId].Model = spiritModel;
            spirits[spiritId].CurrentSpeed = 0;
            spirits[spiritId].MaxSpeedBase = spiritSpeed;
            spirits[spiritId].GuildName = "";
            spirits[spiritId].Size = 50;
            spirits[spiritId].X = EffectOwner.X + Util.Random(20, 40) - Util.Random(20, 40);
            spirits[spiritId].Y = EffectOwner.Y + Util.Random(20, 40) - Util.Random(20, 40);
            spirits[spiritId].Z = EffectOwner.Z;
            spirits[spiritId].Flags |= ENpcFlags.DONTSHOWNAME;
            spirits[spiritId].SetOwnBrain(new StandardMobBrain());
            spirits[spiritId].AddToWorld();
            spirits[spiritId].TargetObject = targetPlayer;
            spirits[spiritId].Follow(targetPlayer, 0, RealmAbilities.NfRaMinionRescueAbility.SpellRadius + 100);
            spiritTimer[spiritId] = new EcsGameTimer(spirits[spiritId], new EcsGameTimer.EcsTimerCallback(spiritCallBack), 200);
        }

        // Check distance between spirit and target
        private int spiritCallBack(EcsGameTimer timer)
        {
            if (timer.Owner == null || !(timer.Owner is GameNpc))
            {
                timer.Stop();
                timer = null;
                return 0;
            }

            GameNpc spirit = timer.Owner as GameNpc;
            GamePlayer targetPlayer = spirit.TargetObject as GamePlayer;

            if (targetPlayer == null || !targetPlayer.IsAlive)
            {
                spirit.StopFollowing();
                timer.Stop();
                timer = null;
                return 0;
            }

            if ( targetPlayer.IsWithinRadius( spirit, 100 ) )
            {
                ApplySpiritEffect(spirit, targetPlayer);
                timer.Stop();
                timer = null;
                return 0;
            }

            return 200;
        }

        // Stun target when spirit come in contact
        private void ApplySpiritEffect(GameLiving source, GameLiving target)
        {
            if (stun != null) stun.StartSpell(target);
            source.Die(null);
            source.Delete();
        }

        public override string Name { get { return "Minion Rescue"; } }
        public override ushort Icon { get { return 3048; } }

        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add("Summon pets that will follow and stun enemies.");
                return list;
            }
        }
    }
}
