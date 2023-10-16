using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.PropertyCalc;

namespace DOL.GS
{
    public class GameTrainingDummy : GameNpc
    {
        public GameTrainingDummy() : base()
        {
            MaxSpeedBase = 0;
            SetOwnBrain(new BlankBrain());
        }

        
        public override void StartAttack(GameObject target) { }

        /// <summary>
        /// Training Dummies never loose health
        /// </summary>
        public override int Health
        {
            get => base.MaxHealth;
            set { }
        }

        /// <summary>
        /// Training Dummies are always alive
        /// </summary>
        public override bool IsAlive => true;

        /// <summary>
        /// Training Dummies never attack
        /// </summary>
        /// <param name="ad"></param>
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (ad.IsHit && ad.CausesCombat)
            {
                if (ad.Attacker.Realm == 0 || Realm == 0)
                {
                    LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
                    ad.Attacker.LastAttackTickPvE = GameLoop.GameLoopTime;
                }
                else
                {
                    LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
                    ad.Attacker.LastAttackTickPvP = GameLoop.GameLoopTime;
                }
            }
        }

        /// <summary>
        /// Interacting with a training dummy does nothing
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool Interact(GamePlayer player)
        {
            Notify(GameObjectEvent.Interact, this, new InteractEventArgs(player));
            player.Notify(GameObjectEvent.InteractWith, player, new InteractWithEventArgs(this));
            return true;
        }

        protected static void ApplyBonus(GameLiving owner, EBuffBonusCategory BonusCat, EProperty Property, double Value, double Effectiveness, bool IsSubstracted)
        {
            int effectiveValue = (int)(Value * Effectiveness);

            IPropertyIndexer tblBonusCat;
            if (Property != EProperty.Undefined)
            {
                tblBonusCat = GetBonusCategory(owner, BonusCat);
                //Console.WriteLine($"Value before: {tblBonusCat[(int)Property]}");
                if (IsSubstracted)
                    tblBonusCat[(int)Property] -= effectiveValue;
                else
                    tblBonusCat[(int)Property] += effectiveValue;
                //Console.WriteLine($"Value after: {tblBonusCat[(int)Property]}");
            }
        }

        private static IPropertyIndexer GetBonusCategory(GameLiving target, EBuffBonusCategory categoryid)
        {
            IPropertyIndexer bonuscat = null;
            switch (categoryid)
            {
                case EBuffBonusCategory.BaseBuff:
                    bonuscat = target.BaseBuffBonusCategory;
                    break;
                case EBuffBonusCategory.SpecBuff:
                    bonuscat = target.SpecBuffBonusCategory;
                    break;
                case EBuffBonusCategory.Debuff:
                    bonuscat = target.DebuffCategory;
                    break;
                case EBuffBonusCategory.Other:
                    bonuscat = target.BuffBonusCategory4;
                    break;
                case EBuffBonusCategory.SpecDebuff:
                    bonuscat = target.SpecDebuffCategory;
                    break;
                case EBuffBonusCategory.AbilityBuff:
                    bonuscat = target.AbilityBonus;
                    break;
                default:
                    //if (log.IsErrorEnabled)
                    //    Console.WriteLine("BonusCategory not found " + categoryid + "!");
                    break;
            }
            return bonuscat;
        }
    }
}