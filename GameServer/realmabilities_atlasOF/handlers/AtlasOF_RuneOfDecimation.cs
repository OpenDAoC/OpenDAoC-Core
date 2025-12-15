using System.Collections.Generic;
using DOL.AI;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_RuneOfDecimation : TimedRealmAbility
    {
        public const int DURATION = 480000; // 8 minutes.

        private static Spell _spell = null;

        public override int MaxLevel => 1;
        public override int GetReUseDelay(int level) { return 900; } // 15 minutes.
        public override int CostForUpgrade(int level) { return 14; }

        public AtlasOF_RuneOfDecimation(DbAbility dba, int level) : base(dba, level) { }

        public override void AddEffectsInfo(IList<string> list)
        {
            EnsureSpellInitialized();
            SpellHandler.GetDelveInfo(null, _spell, null);
        }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED))
                return;

            if (living.IsCasting)
            {
                (living as GamePlayer)?.Out.SendMessage("You are already casting an ability.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            EnsureSpellInitialized();

            RuneOfDecimation rune = new(_spell)
            {
                Name = "Rune of Decimation",
                GuildName = living.Name,
                Realm = living.Realm,
                Size = 50,
                Level = living.Level,
                CurrentRegion = living.CurrentRegion,
                X = living.X,
                Y = living.Y,
                Z = living.Z,
                Owner = living
            };

            if (!rune.AddToWorld())
                return;

            foreach (GamePlayer player in living.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
            {
                if (player == living)
                    player.MessageToSelf($"You cast {Name}!", eChatType.CT_Spell);
                else
                    player.MessageFromArea(living, $"{living.Name} casts a spell!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
            }

            DisableSkill(living);
        }

        private static void EnsureSpellInitialized()
        {
            if (_spell != null)
                return;

            // Behaves like an AoE, not a PBAoE.
            // Damage is centered on the first enemy that enters the rune's radius.
            DbSpell _dbspell = new()
            {
                Name = "Rune Of Decimation",
                Icon = 4254,
                ClientEffect = 7153,
                Damage = 650,
                DamageType = (int) eDamageType.Energy,
                Target = eSpellTarget.ENEMY.ToString(),
                Range = 350, // Unknown.
                Radius = 350,
                Type = eSpellType.DirectDamage.ToString(),
                Value = 0,
                Duration = 0,
                Pulse = 0,
                PulsePower = 0,
                Power = 0,
                CastTime = 0,
                EffectGroup = 0
            };

            _spell = new(_dbspell, 0);
        }

        private class RuneOfDecimation : GameMine
        {
            public override bool IsVisibleToPlayers => true;
            public Spell Spell { get; init;}

            public RuneOfDecimation(Spell spell) : base(new RuneOfDecimationBrain(GameLoop.GameLoopTime + DURATION))
            {
                Spell = spell;
            }
        }

        private class RuneOfDecimationBrain : ABrain
        {
            private long _expireTime;
            public override int ThinkInterval => 500;

            public RuneOfDecimationBrain(long expireTime)
            {
                _expireTime = expireTime;
            }

            public override void Think()
            {
                if (GameServiceUtils.ShouldTick(_expireTime) ||
                    Body is not RuneOfDecimation rune ||
                    rune.Owner.ObjectState is GameObject.eObjectState.Deleted)
                {
                    Body.Die(Body);
                    return;
                }

                GameLiving triggeringLiving = null;
                Spell spell = rune.Spell;
                ushort spellRadius = (ushort) spell.Radius;
                GameLiving caster = rune.Owner;

                foreach (GamePlayer player in rune.GetPlayersInRadius(spellRadius))
                {
                    if (GameServer.ServerRules.IsAllowedToAttack(caster, player, true))
                    {
                        triggeringLiving ??= player;
                        break;
                    }
                }

                if (triggeringLiving == null)
                {
                    foreach (GameNPC living in rune.GetNPCsInRadius(spellRadius))
                    {
                        if (GameServer.ServerRules.IsAllowedToAttack(caster, living, true))
                        {
                            triggeringLiving ??= living;
                            break;
                        }
                    }
                }

                if (triggeringLiving == null)
                    return;

                // Bypass the casting component since there isn't an easy way to make the player cast an AoE or PBAoE spell from far away.
                ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(caster, _spell, GlobalSpellsLines.RealmSpellsSpellLine);
                spellHandler.StartSpell(triggeringLiving);
                rune.Die(rune);
            }

            public override void KillFSM() { }
        }
    }
}
