using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_Volley : TimedRealmAbility
    {
        public const int DISABLE_DURATION = 15000;

        private GamePlayer _player;

        public override int MaxLevel => 1;
        public override ushort Icon => 4281;

        public AtlasOF_Volley(DbAbility ability, int level) : base(ability, level) { }

        public static int GetMinAttackRange(eRealm realm)
        {
            int minAttackRange = realm switch
            {
                eRealm.Albion => 2200,
                eRealm.Hibernia => 2100,
                _ => 2000,
            };

            return (int) (minAttackRange * 0.66);
        }

        public static int GetMaxAttackRange(eRealm realm)
        {
            int maxAttackRange = realm switch
            {
                eRealm.Albion => 4400,
                eRealm.Hibernia => 4300,
                _ => 4200,
            };

            return maxAttackRange;
        }

        public override int CostForUpgrade(int level)
        {
            return 8;
        }

        public override int GetReUseDelay(int level)
        {
            return 15;
        }

        public override bool CheckRequirement(GamePlayer player)
        {
            return AtlasRAHelpers.GetLongshotLevel(player) >= 1;
        }

        public override void Execute(GameLiving living)
        {
            _player = living as GamePlayer;

            if (_player == null)
                return;

            if (CheckPreconditions(_player, DEAD | SITTING | MEZZED | STUNNED))
                return;

            if (_player.CurrentRegion.IsDungeon)
            {
                _player.Out.SendMessage("You can't use Volley in dungeons!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (_player.ActiveWeaponSlot is not eActiveWeaponSlot.Distance || _player.ActiveWeapon == null)
            {
                _player.Out.SendMessage("You need to be equipped with a bow to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (_player.rangeAttackComponent.UpdateAmmo(_player.ActiveWeapon) == null)
            {
                _player.Out.SendMessage("You need arrows to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (_player.CurrentRegion.GetZone(_player.GroundTarget.X, _player.GroundTarget.Y) == null)
            {
                _player.Out.SendMessage("You must have a ground target to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (_player.IsWithinRadius(_player.GroundTarget, GetMinAttackRange(_player.Realm)))
            {
                _player.Out.SendMessage("You ground target is too close to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (!_player.IsWithinRadius(_player.GroundTarget, GetMaxAttackRange(_player.Realm)))
            {
                _player.Out.SendMessage("You ground target is too far away to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            ECSGameEffect volley = EffectListService.GetEffectOnTarget(_player, eEffect.Volley);

            if (volley != null)
            {
                _player.Out.SendMessage("You are already using Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (_player.rangeAttackComponent.RangedAttackType is eRangedAttackType.Critical)
            {
                _player.Out.SendMessage("You can't use Volley while Critical Shot is active!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (_player.rangeAttackComponent.RangedAttackType is eRangedAttackType.Long)
            {
                _player.Out.SendMessage("You can't use Volley while Longshot is active!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // Can't use Volley inside portal and border keeps on a RvR server.
            if (GameServer.Instance.Configuration.ServerType is EGameServerType.GST_Normal)
            {
                foreach (IArea area in _player.CurrentRegion.GetAreasOfSpot(_player.GroundTarget))
                {
                    if (area is KeepArea keepArea)
                    {
                        AbstractGameKeep keep = keepArea.Keep;

                        if (keep != null && keep.IsPortalKeep && keep.Realm != _player.Realm)
                        {
                            _player.Out.SendMessage("You can't use Volley inside enemy Portal Keep!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                    }
                    else if (area is Area.Circle circleArea)
                    {
                        bool canUseVolley = true;

                        if (_player.Realm is eRealm.Albion)
                        {
                            if (circleArea.Description is "Druim Ligen" or "Druim Cain" or "Svasud Faste" or "Vindsaul Faste")
                                canUseVolley = false;
                        }
                        else if (_player.Realm is eRealm.Hibernia)
                        {
                            if (circleArea.Description is "Svasud Faste" or "Vindsaul Faste" or "Castle Sauvage" or "Snowdonia Fortress")
                                canUseVolley = false;
                        }
                        else if (_player.Realm is eRealm.Midgard)
                        {
                            if (circleArea.Description is "Druim Ligen" or "Druim Cain" or "Castle Sauvage" or "Snowdonia Fortress")
                                canUseVolley = false;
                        }

                        if (!canUseVolley)
                        {
                            _player.Out.SendMessage("You can't use Volley inside enemy Border Keep!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                    }
                }
            }

            if (_player.IsInterrupted)
            {
                GameObject attacker = _player.LastInterrupter;

                if (attacker is GameNPC npcAttacker)
                    _player.Out.SendMessage(LanguageMgr.GetTranslation(_player.Client.Account.Language, "GamePlayer.Attack.Interrupted", attacker.GetName(0, true, _player.Client.Account.Language, npcAttacker), "volley"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                else
                    _player.Out.SendMessage(LanguageMgr.GetTranslation(_player.Client.Account.Language, "GamePlayer.Attack.Interrupted", attacker.GetName(0, true), "volley"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);

                return;
            }

            ECSGameEffectFactory.Create(new(_player, 0, 1), static (in i) => new AtlasOF_VolleyECSEffect(i));
        }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Ground-targetted archery attack that fires successive arrows at a random target in a given area. To use this ability, choose a ground target. This target must be at least 66% of your bow's normal max range away from you.");
            list.Add("Once you are ready to fire, you can fire up to 5 arrows by hitting your bow button in succession.");
            list.Add("\nTarget: Ground");
            list.Add("Range: Minimum 66% of player range.");
        }
    }
}
