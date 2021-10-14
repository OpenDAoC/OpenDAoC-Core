using System;
using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_PurgeAbility : PurgeAbility
    {
        public AtlasOF_PurgeAbility(DBAbility dba, int level) : base(dba, level) { }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING)) return;

            SendCastMessage(living);
            if (RemoveNegativeEffects(living, this))
            {
                DisableSkill(living);
            }
        }

        public override int MaxLevel { get { return 1; } }
        public override int GetReUseDelay(int level) { return 1800; } // 30 min
        public override int CostForUpgrade(int level) { return 10; }
    }

    /// <summary>
    /// This class exists because there is no way to get a player context when CostForUpgrade is called
    /// so we cannot dynamically change the cost based on class type (pure tank vs hybrid vs casters).
    /// </summary>
    public class AtlasOF_PurgeAbilityReduced : AtlasOF_PurgeAbility
    {
        public AtlasOF_PurgeAbilityReduced(DBAbility dba, int level) : base(dba, level) { }
        public override int CostForUpgrade(int level) { return 4; }
    }

    /// <summary>
    /// Group Purge Ability, removes negative effects from the caster and all group members 
    /// </summary>
    public class AtlasOF_GroupPurge : AtlasOF_PurgeAbility
    {
        public AtlasOF_GroupPurge(DBAbility dba, int level) : base(dba, level) { }

        public override int MaxLevel { get { return 1; } }
        public override int GetReUseDelay(int level) { return 1800; } // 30 min
        public override int CostForUpgrade(int level) { return 14; }

        int m_range = 1500;

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING)) return;

            GamePlayer player = living as GamePlayer;

            if (player == null)
                return;

            ArrayList targets = new ArrayList();
            if (player.Group == null)
                targets.Add(player);
            else
            {
                foreach (GamePlayer grpplayer in player.Group.GetPlayersInTheGroup())
                {
                    if (player.IsWithinRadius(grpplayer, m_range) && grpplayer.IsAlive)
                        targets.Add(grpplayer);
                }
            }

            SendCastMessage(player);

            bool AtLeastOneEffectRemoved = false;
            foreach (GamePlayer target in targets)
            {                   
                if (target != null)
                {
                    AtLeastOneEffectRemoved |= RemoveNegativeEffects(target, this, /* isFromGroupPurge = */ true);
                }
            }

            if (AtLeastOneEffectRemoved)
            {
                DisableSkill(living);
            }
        }
    }
}