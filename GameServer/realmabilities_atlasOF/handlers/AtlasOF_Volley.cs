using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using System.Collections.Generic;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_Volley : TimedRealmAbility
    {
        public override int MaxLevel { get { return 1; } }
        public override int CostForUpgrade(int level) { return 8; }
        public override int GetReUseDelay(int level) { return 15; } // 15 seconds
        public override bool CheckRequirement(GamePlayer player)
        {
            return AtlasRAHelpers.HasLongshotLevel(player, 1);
        }
        public override ushort Icon => 4281;
        
        private GamePlayer m_player;
        public const int DISABLE_DURATION = 15000; //15s to use again ability
        public override void Execute(GameLiving living)
		{
            m_player = living as GamePlayer;
            double attackrangeMin = m_player.AttackRange * 0.66;//minimum attack range
            double attackrangeMax = m_player.AttackRange / 0.66;//maximum attack range
            InventoryItem ammo = m_player.rangeAttackComponent.RangeAttackAmmo;
            Region rgn = WorldMgr.GetRegion(m_player.CurrentRegion.ID);

            if (CheckPreconditions(m_player, DEAD | SITTING | MEZZED | STUNNED))
                return;

            if (m_player.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
            {
                m_player.Out.SendMessage("You need to be equipped with a bow for use this ability.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (m_player.AttackWeapon == null)
            {
                m_player.Out.SendMessage("You need weapon to use this ability!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (ammo == null)
            {
                m_player.Out.SendMessage("You need to be equipped with a bow for use this ability.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (rgn == null || rgn.GetZone(m_player.GroundTarget.X, m_player.GroundTarget.Y) == null)
            {
                m_player.Out.SendMessage("You need a ground target for use this ability.", eChatType.CT_System, eChatLoc.CL_SystemWindow);     
                return;
            }
            if (m_player.IsWithinRadius(m_player.GroundTarget, (int)attackrangeMin))
            {
                m_player.Out.SendMessage("You ground target is too close to use this ability.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (!m_player.IsWithinRadius(m_player.GroundTarget, (int)attackrangeMax))
            {
                m_player.Out.SendMessage("You ground target is too far away to use this ability!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            AtlasOF_VolleyECSEffect Volley = (AtlasOF_VolleyECSEffect)m_player.EffectList.GetOfType(typeof(AtlasOF_VolleyECSEffect));
            if (Volley != null)
            {
               return;
            }
            TrueshotEffect trueShot = (TrueshotEffect)m_player.EffectList.GetOfType(typeof(TrueshotEffect));
            if (trueShot != null)
            {
                trueShot.Cancel(false);
                return;
            }
            RapidFireEffect rapidFire = (RapidFireEffect)m_player.EffectList.GetOfType(typeof(RapidFireEffect));
            if (rapidFire != null)
            {
                rapidFire.Cancel(false);
                return;
            }
            if (m_player.rangeAttackComponent.RangedAttackType == eRangedAttackType.Critical)
            {
                m_player.Out.SendMessage("You can't use Volley while Critical-Shot is active!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (m_player.rangeAttackComponent.RangedAttackType == eRangedAttackType.Long)
            {
                m_player.Out.SendMessage("You can't use Volley while Longshot is active!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            new AtlasOF_VolleyECSEffect(new ECSGameEffectInitParams(m_player, 0, 1));
            //DisableSkill(living);
		}
        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Ground-targetted archery attack that fires successive arrows at a various targets in a given area. To use this ability, choose a ground target. This target must be at least 66% of your bow's normal max range away from you.");
            list.Add("Once you are ready to fire, you can fire up to 5 arrows by hitting your bow button in succession.");
            list.Add("\nCasting time: 2.2 seconds");
            list.Add("Target: Ground");
            list.Add("Range: Minimum 66% of player range.");
        }
        public AtlasOF_Volley(DBAbility ability, int level) : base(ability, level)
        {
        }
    }
}