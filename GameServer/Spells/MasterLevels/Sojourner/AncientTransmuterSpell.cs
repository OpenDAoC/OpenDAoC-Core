using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Players.Clients;
using Core.GS.Skills;

namespace Core.GS.Spells
{
    //no shared timer

    [SpellHandler("AncientTransmuter")]
    public class AncientTransmuterSpell : SpellHandler
    {
        private GameMerchant merchant;

        /// <summary>
        /// Execute Acient Transmuter summon spell
        /// </summary>
        /// <param name="target"></param>
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            if (effect.Owner == null || !effect.Owner.IsAlive)
                return;

            merchant.AddToWorld();
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (merchant != null) merchant.Delete();
            return base.OnEffectExpires(effect, noMessages);
        }

        public AncientTransmuterSpell(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            if (caster is GamePlayer)
            {
                GamePlayer casterPlayer = caster as GamePlayer;
                merchant = new GameMerchant();
                //Fill the object variables
                merchant.X = casterPlayer.X + Util.Random(20, 40) - Util.Random(20, 40);
                merchant.Y = casterPlayer.Y + Util.Random(20, 40) - Util.Random(20, 40);
                merchant.Z = casterPlayer.Z;
                merchant.CurrentRegion = casterPlayer.CurrentRegion;
                merchant.Heading = (ushort)((casterPlayer.Heading + 2048) % 4096);
                merchant.Level = 1;
                merchant.Realm = casterPlayer.Realm;
                merchant.Name = "Ancient Transmuter";
                merchant.Model = 993;
                merchant.CurrentSpeed = 0;
                merchant.MaxSpeedBase = 0;
                merchant.GuildName = "";
                merchant.Size = 50;
                merchant.Flags |= ENpcFlags.PEACE;
                merchant.TradeItems = new MerchantTradeItems("ML_transmuteritems");
            }
        }
    }
}