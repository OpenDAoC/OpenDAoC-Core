using System;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using System.Collections.Generic;
using DOL.Language;
using DOL.GS.Spells;
using DOL.AI.Brain;

namespace DOL.GS.RealmAbilities
{
    [SkillHandler(Abilities.Volley)]
    public class AtlasOF_Volley : TimedRealmAbility
    {
        public override int MaxLevel { get { return 1; } }
        public override int CostForUpgrade(int level) { return 8; }
        public override int GetReUseDelay(int level) { return 15; } // 15 seconds
        public override bool CheckRequirement(GamePlayer player)
        {
            return AtlasRAHelpers.HasLongshotLevel(player, 1);
        }

        private GamePlayer m_player;
        public const int DISABLE_DURATION = 15000;              //15s to use again ability
        public override void Execute(GameLiving living)
		{
            m_player = living as GamePlayer;
            double attackrangeMin = m_player.AttackRange * 0.66;//minimum attack range
            double attackrangeMax = m_player.AttackRange / 0.66;//maximum attack range
            InventoryItem ammo = m_player.rangeAttackComponent.RangeAttackAmmo;
            Region rgn = WorldMgr.GetRegion(m_player.CurrentRegion.ID);

            if (CheckPreconditions(m_player, DEAD | SITTING | MEZZED | STUNNED))
                return;

            if (m_player.AttackWeapon == null)
            {
                m_player.Out.SendMessage("You need weapon to use this ability!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (ammo == null)
            {
                m_player.Out.SendMessage("You need to be equiped with a bow for use this ability.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            // Check if selected ammo is compatible for ranged attack
            if (!m_player.rangeAttackComponent.CheckRangedAmmoCompatibilityWithActiveWeapon())
            {
                m_player.Out.SendMessage("You need to be equiped with a bow for use this ability.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (!GlobalConstants.IsBowWeapon((eObjectType)m_player.AttackWeapon.Object_Type))
            {
                m_player.Out.SendMessage("You need to be equiped with a bow for use this ability.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);   
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
        public override IList<string> DelveInfo
        {
            get
            {
                var delveInfoList = new List<string>();
                delveInfoList.Add("Ground-targetted archery attack that fires successive arrows at a various targets in a given area. To use this ability, choose a ground target. This target must be at least 66% of your bow's normal max range away from you.\n Once you are ready to fire, you can fire up to 5 arrows by hitting your bow button in succession.");
                delveInfoList.Add("Casting time: instant");

                return delveInfoList;
            }
        }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Ground-targetted archery attack that fires successive arrows at a various targets in a given area. To use this ability, choose a ground target. This target must be at least 66% of your bow's normal max range away from you.\n Once you are ready to fire, you can fire up to 5 arrows by hitting your bow button in succession.");
            list.Add("Casting time: instant");
        }
        public AtlasOF_Volley(DBAbility ability, int level) : base(ability, level)
        {
        }
    }
}