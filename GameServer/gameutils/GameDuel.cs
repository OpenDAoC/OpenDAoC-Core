using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;

namespace DOL.GS
{
    public class GameDuel
    {
        private const string DUEL_PREVIOUS_LASTATTACKTICKPVP = "DUEL_PREVIOUS_LASTATTACKTICKPVP";
        private const string DUEL_PREVIOUS_LASTATTACKEDBYENEMYTICKPVP= "DUEL_PREVIOUS_LASTATTACKEDBYENEMYTICKPVP";
        public GamePlayer Starter { get; }
        public GamePlayer Target { get; }

        public GameDuel(GamePlayer starter, GamePlayer target)
        {
            Starter = starter;
            Target = target;
        }

        public GamePlayer GetPartnerOf(GameLiving living)
        {
            if (living is GameNPC npc && npc.Brain is ControlledMobBrain brain)
                living = brain.GetPlayerOwner();

            return living == Starter ? Target : Starter;
        }

        public void Start()
        {
            HandlePlayer(Starter, this);
            HandlePlayer(Target, this);

            static void HandlePlayer(GamePlayer player, GameDuel duel)
            {
                player.OnDuelStart(duel);
                player.TempProperties.SetProperty(DUEL_PREVIOUS_LASTATTACKTICKPVP, player.LastAttackTickPvP);
                player.TempProperties.SetProperty(DUEL_PREVIOUS_LASTATTACKEDBYENEMYTICKPVP, player.LastAttackedByEnemyTickPvP);
            }
        }

        public void Stop()
        {
            HandlePlayer(Starter, Target);
            HandlePlayer(Target, Starter);

            static void HandlePlayer(GamePlayer player, GamePlayer partner)
            {
                player.OnDuelStop();
                player.LastAttackTickPvP = player.TempProperties.GetProperty<long>(DUEL_PREVIOUS_LASTATTACKTICKPVP);
                player.LastAttackedByEnemyTickPvP = player.TempProperties.GetProperty<long>(DUEL_PREVIOUS_LASTATTACKEDBYENEMYTICKPVP);

                lock (player.XPGainers.SyncRoot)
                {
                    player.XPGainers.Clear();
                }

                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GamePlayer.DuelStop.DuelEnds"), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
                StopEffects(player, partner);
            }

            static void StopEffects(GamePlayer player, GamePlayer caster)
            {
                Loop(player.effectListComponent.GetAllEffects(), caster);

                IControlledBrain controlledBrain = player.ControlledBrain;

                if (controlledBrain != null)
                    Loop(controlledBrain.Body.effectListComponent.GetAllEffects(), caster);

                static void Loop(List<ECSGameEffect> effects, GamePlayer caster)
                {
                    GameNPC petCaster = caster.ControlledBrain?.Body;

                    foreach (ECSGameEffect effect in effects)
                    {
                        if (effect.HasPositiveEffect)
                            continue;

                        ISpellHandler spellHandler = effect.SpellHandler;

                        if (spellHandler == null)
                            continue;

                        if (spellHandler.Caster == caster || (spellHandler.Caster != null && spellHandler.Caster == petCaster))
                        {
                            effect.TriggersImmunity = false;
                            EffectService.RequestCancelEffect(effect);
                        }
                    }
                }
            }
        }
    }
}
