using System;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.AI.Brain;

public class HurionthexBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public HurionthexBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 750;
    }

    public static bool IsBaseForm = false;
    public static bool IsSaiyanForm = false;
    public static bool IsTreantForm = false;
    public static bool IsGranidonForm = false;
    public static bool BaseFormCheck = false;
    public static bool SaiyanFormCheck = false;
    public static bool TreantFormCheck = false;
    public static bool GranidonFormCheck = false;
    public static bool SwitchForm = false;

    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    
    //todo = Randomization of chosen form
    //todo = Timer for switching between forms
    //todo = Add spell casting upon expiration
    //todo = Add animations between switched forms

    #region Base Form

    // Base Form: Sylvan
    // Considered regular form, hits for ~450 against leather
    // DS, DA active (~35)

    /// <summary>
    /// Defines the attributes to revert to each time Hurionthex reverts to his base form.
    /// </summary>
    public void FormBase()
    {
        Body.Model = 889;
        Body.Size = 170;
        Body.MeleeDamageType = EDamageType.Crush;
        Body.BodyType = 5; // Giant

        Body.Strength = 250;
        Body.Dexterity = 200;
        Body.Quickness = 80;
        Body.Intelligence = 200;
        Body.Empathy = 400;
        Body.Piety = 200;
        Body.Charisma = 200;
    }

    #endregion Base Form

    #region Treant Form

    // Treant form
    // Behaviors: DA, DS, hits for 1000 (vs leather)

    /// <summary>
    /// Defines the attributes to change with Hurionthex when he changes to his tank (treant) form.
    /// </summary>
    public void FormTreant()
    {
        Body.Model = 946;
        Body.Size = 120;
        Body.AttackRange = 450;
        Body.MeleeDamageType = EDamageType.Spirit;
        Body.BodyType = 10; // Plant

        Body.Strength = 400;
        Body.Constitution = 100;
        Body.Dexterity = 200;
        Body.Quickness = 100;
        Body.Intelligence = 200;
        Body.Empathy = 400;
        Body.Piety = 200;
        Body.Charisma = 200;
    }

    #endregion Treant Form

    #region Saiyan Form

    // "Saiyan" form
    // Behaviors: DS, DA, hits for ~260 (vs leather), attacks very fast

    /// <summary>
    /// Defines the attributes to change with Hurionthex when he changes to his attack (Saiyan) form.
    /// </summary>
    public void FormSaiyan()
    {
        Body.Model = 844;
        Body.Size = 160;
        Body.AttackRange = 450;
        Body.MeleeDamageType = EDamageType.Spirit;
        Body.BodyType = 1; // Animal

        Body.Strength = 350;
        Body.Constitution = 100;
        Body.Dexterity = 200;
        Body.Quickness = 205;
        Body.Intelligence = 200;
        Body.Empathy = 400;
        Body.Piety = 200;
        Body.Charisma = 200;
    }

    #endregion Saiyan Form

    #region Granidan Form

    // Granidon form
    // Behaviors: DA, DS, 50-minute disease (Black Plague), hits for ~750 (vs leather)

    /// <summary>
    /// Defines the attributes to change with Hurionthex when he changes to his hybrid (Granidon) form.
    /// </summary>
    public void FormGranidon()
    {
        Body.Model = 925;
        Body.Size = 150;
        Body.AttackRange = 450;
        Body.MeleeDamageType = EDamageType.Spirit;

        Body.Strength = 300;
        Body.Constitution = 100;
        Body.Dexterity = 200;
        Body.Quickness = 80;
        Body.Intelligence = 200;
        Body.Empathy = 400;
        Body.Piety = 200;
        Body.Charisma = 200;
    }

    #endregion Granidan Form

    /// <summary>
    /// Handles how the form changes. If not in base form, change to base, otherwise randomly change to another of three forms: Treant, Saiyan, Granidon.
    /// </summary>
    public int ChangeForm(EcsGameTimer timer)
    {
        if (Body.InCombat && HasAggro)
        {
            BaseFormCheck = false;
            GranidonFormCheck = false;
            TreantFormCheck = false;
            SaiyanFormCheck = false;
            SwitchForm = false;
            reset_checks = false;

            int randomform = Util.Random(1, 4);
            switch (randomform)
            {
                case 1:
                {
                    if (IsGranidonForm == false)
                    {
                        BroadcastMessage(String.Format("Hurionthex casts a spell!"));
                        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(Change_Granidon), 2000);
                        IsGranidonForm = true;
                    }
                }
                    break;
                case 2:
                {
                    if (IsTreantForm == false)
                    {
                        BroadcastMessage(String.Format("Hurionthex casts a spell!"));
                        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(Change_Treant), 2000);
                        IsTreantForm = true;
                    }
                }
                    break;
                case 3:
                {
                    if (IsSaiyanForm == false)
                    {
                        BroadcastMessage(String.Format("Hurionthex casts a spell!"));
                        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(Change_Saiyan), 2000);
                        IsSaiyanForm = true;
                    }
                }
                    break;
                case 4:
                {
                    if (IsBaseForm == false)
                    {
                        BroadcastMessage(String.Format("Hurionthex casts a spell!"));
                        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(Change_Base), 2000);
                        IsBaseForm = true;
                    }
                }
                    break;
            }
        }

        return 0;
    }

    public int Change_Base(EcsGameTimer timer)
    {
        if (IsBaseForm == true && BaseFormCheck == false)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(Body, Body, 208, 0, false, 0x01);
            }

            BroadcastMessage(String.Format("Hurionthex returns to his natural form."));
            FormBase();
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(FormDuration), 2000);
            BaseFormCheck = true;
        }

        return 0;
    }

    public int Change_Granidon(EcsGameTimer timer)
    {
        if (IsGranidonForm == true && GranidonFormCheck == false)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(Body, Body, 208, 0, false, 0x01);
            }

            BroadcastMessage(String.Format("A ring of magical energy emanates from Hurionthex."));
            FormGranidon();
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(FormDuration), 2000);
            GranidonFormCheck = true;
        }

        return 0;
    }

    public int Change_Treant(EcsGameTimer timer)
    {
        if (IsTreantForm == true && TreantFormCheck == false)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(Body, Body, 208, 0, false, 0x01);
            }

            BroadcastMessage(String.Format("A ring of magical energy emanates from Hurionthex."));
            FormTreant();
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(FormDuration), 2000);
            TreantFormCheck = true;
        }

        return 0;
    }

    public int Change_Saiyan(EcsGameTimer timer)
    {
        if (IsSaiyanForm == true && SaiyanFormCheck == false)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(Body, Body, 208, 0, false, 0x01);
            }

            BroadcastMessage(String.Format("A ring of magical energy emanates from Hurionthex."));
            FormSaiyan();
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(FormDuration), 2000);
            SaiyanFormCheck = true;
        }

        return 0;
    }

    public int FormDuration(EcsGameTimer timer)
    {
        if (SwitchForm == false)
        {
            if (BaseFormCheck == true || GranidonFormCheck == true || TreantFormCheck == true ||
                SaiyanFormCheck == true)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetChecks), 2000);
                SwitchForm = true;
            }
        }

        return 0;
    }

    public static bool reset_checks = false;

    public int ResetChecks(EcsGameTimer timer)
    {
        if (SwitchForm == true && reset_checks == false)
        {
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ChangeForm), 18000);

            IsBaseForm = false;
            IsGranidonForm = false;
            IsTreantForm = false;
            IsSaiyanForm = false;
            reset_checks = true;
        }

        return 0;
    }

    public int CastBlackPlague(EcsGameTimer timer)
    {
        Body.CastSpell(BlackPlague, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        cast_disease = false;
        return 0;
    }

    public int CastDamageAdd(EcsGameTimer timer)
    {
        Body.CastSpell(DamageAdd, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        cast_DA = false;
        return 0;
    }

    public int CastDamageShield(EcsGameTimer timer)
    {
        Body.CastSpell(DamageShield, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        cast_DS = false;
        return 0;
    }

    //Todo = Add DA, DS spells

    public static bool StartForms = false;
    public static bool cast_DA = false;
    public static bool cast_disease = false;
    public static bool cast_DS = false;

    public override void Think()
    {
        // Reset boss encounter in the event of a party wipe or people running away
        if (!CheckProximityAggro())
        {
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            IsBaseForm = false;
            IsSaiyanForm = false;
            IsTreantForm = false;
            IsGranidonForm = false;

            BaseFormCheck = false;
            GranidonFormCheck = false;
            TreantFormCheck = false;
            SaiyanFormCheck = false;
            SwitchForm = false;
            reset_checks = false;

            StartForms = false;
            cast_DA = false;
            cast_disease = false;
            cast_DS = false;
        }

        if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            ClearAggroList();
            FormBase();
        }

        if (Body.IsOutOfTetherRange)
        {
            ClearAggroList();
            FormBase();
        }

        if (Body.InCombat && HasAggro)
        {
            if (Body.TargetObject != null)
            {
                //todo = Change switch case to form, make it dependent on timer trigger
                if (StartForms == false)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ChangeForm), 2000);
                    StartForms = true;
                }
            }
        }

        if (Body.InCombat && HasAggro)
        {
            if (Body.Model == 844) //cast disease as mokney form
            {
                if (Util.Chance(100) && Body.TargetObject != null)
                {
                    if (LivingHasEffect(Body.TargetObject as GameLiving, BlackPlague) == false &&
                        Body.TargetObject.IsVisibleTo(Body))
                    {
                        if (cast_disease == false)
                        {
                            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastBlackPlague), 2000);
                            cast_disease = true;
                        }
                    }
                }
            }

            if (Body.Model == 946) //cast damage add as tree
            {
                if (Util.Chance(100) && Body.TargetObject != null)
                {
                    if (!Body.effectListComponent.ContainsEffectForEffectType(EEffect.DamageAdd))
                    {
                        if (cast_DA == false)
                        {
                            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastDamageAdd), 2000);
                            cast_DA = true;
                        }
                    }
                }
            }

            if (Body.Model == 925) //cast damage shield as sanidon
            {
                if (Util.Chance(100) && Body.TargetObject != null)
                {
                    if (!Body.effectListComponent.ContainsEffectForEffectType(EEffect.DamageReturn))
                    {
                        if (cast_DS == false)
                        {
                            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastDamageShield), 2000);
                            cast_DS = true;
                        }
                    }
                }
            }
        }

        base.Think();
    }

    public Spell m_black_plague;

    public Spell BlackPlague
    {
        get
        {
            if (m_black_plague == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 40;
                spell.ClientEffect = 4375;
                spell.Icon = 4375;
                spell.Name = "Black Plague";
                spell.Message1 = "You are diseased!";
                spell.Message2 = "{0} is diseased!";
                spell.Message3 = "You look healthy.";
                spell.Message4 = "{0} looks healthy again.";
                spell.TooltipId = 4375;
                spell.Range = 1500;
                spell.Radius = 350;
                spell.Duration = 60;
                spell.SpellID = 11731;
                spell.Target = "Enemy";
                spell.Type = "Disease";
                spell.Uninterruptible = true;
                spell.DamageType = (int) EDamageType.Energy; //Energy DMG Type
                m_black_plague = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_black_plague);
            }

            return m_black_plague;
        }
    }

    public Spell m_damage_add;

    public Spell DamageAdd
    {
        get
        {
            if (m_damage_add == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 24;
                spell.ClientEffect = 18;
                spell.Icon = 18;
                spell.Name = "Damage Add";
                spell.TooltipId = 18;
                spell.Range = 1500;
                spell.Damage = 20;
                spell.Duration = 20;
                spell.SpellID = 11732;
                spell.Target = "Self";
                spell.Type = "DamageAdd";
                spell.Uninterruptible = true;
                spell.DamageType = (int) EDamageType.Energy; //Energy DMG Type
                m_damage_add = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_damage_add);
            }

            return m_damage_add;
        }
    }

    public Spell m_damage_shield;

    public Spell DamageShield
    {
        get
        {
            if (m_damage_shield == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 24;
                spell.ClientEffect = 57;
                spell.Icon = 57;
                spell.Name = "Damage Shield";
                spell.TooltipId = 57;
                spell.Range = 1500;
                spell.Damage = 120;
                spell.Duration = 20;
                spell.SpellID = 11733;
                spell.Target = "Self";
                spell.Type = "DamageShield";
                spell.Uninterruptible = true;
                spell.DamageType = (int) EDamageType.Heat; //heat DMG Type
                m_damage_shield = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_damage_shield);
            }

            return m_damage_shield;
        }
    }
}