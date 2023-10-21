﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.PacketHandler;

namespace Core.GS.Commands;

[Command("&spammob", //command to handle
    EPrivLevel.GM, //minimum privelege level
    "Mob creation and modification commands.", //command description
    "/spammob create [amount] - creates [amount] mobs at the player's location",
    "/spammob create [amount] [radius] - creates [amount] mobs in a [radius] around the player",
    "/spammob clear [radius] - deletes all spam mobs in a [radius] around the player"
)]
public class SpamMobCommand : ACommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (args.Length <= 2)
        {
            DisplaySyntax(client);
            return;
        }

        // if (args.Length == 2)
        // {
        //     //create one mob
        //     string theType = "DOL.GS.SpamMob.SpamMobNPC";
        //     byte realm = 0;
        //
        //     //Create a new mob
        //     GameNPC mob = null;
        //
        //     foreach (Assembly script in ScriptMgr.GameServerScripts)
        //     {
        //         try
        //         {
        //             client.Out.SendDebugMessage(script.FullName);
        //             mob = (GameNPC)script.CreateInstance(theType, false);
        //
        //             if (mob != null)
        //                 break;
        //         }
        //         catch (Exception e)
        //         {
        //             client.Out.SendMessage(e.ToString(), eChatType.CT_System, eChatLoc.CL_PopupWindow);
        //         }
        //     }
        //
        //     if (mob == null)
        //     {
        //         client.Out.SendMessage("There was an error creating an instance of " + theType + "!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        //         return;
        //     }
        //
        //     //Fill the object variables
        //     mob.X = client.Player.X;
        //     mob.Y = client.Player.Y;
        //     mob.Z = client.Player.Z;
        //     mob.CurrentRegion = client.Player.CurrentRegion;
        //     mob.Heading = client.Player.Heading;
        //     mob.Level = 50;
        //     mob.Realm = (eRealm)realm;
        //     mob.Name = "Spam Mob";
        //     mob.Model = 34;
        //
        //     //Fill the living variables
        //     mob.CurrentSpeed = 0;
        //     mob.MaxSpeedBase = 200;
        //     mob.GuildName = "Burn Baby Burn";
        //     mob.Size = 50;
        //     mob.Flags |= GameNPC.eFlags.PEACE;
        //     mob.Flags ^= GameNPC.eFlags.PEACE;
        //     mob.AddToWorld();
        //     //mob.LoadedFromScript = false; // allow saving
        //     //mob.SaveIntoDatabase();
        //     //client.Out.SendMessage("Mob created: OID=" + mob.ObjectID, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        //     //client.Out.SendMessage("The mob has been created with the peace flag, so it can't be attacked, to remove type /mob peace", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        // }

        if (args.Length == 3)
        {
            if (args[1].Equals("clear"))
            {
                ushort radius;
                if (ushort.TryParse(args[2], out radius))
                {
                    if (radius < 0) radius = 0;
                    if (radius > 5000) radius = 5000;

                    foreach (GameNpc npc in client.Player.GetNPCsInRadius(radius))
                        if (npc.Realm == ERealm.None && (npc is SpamMobNPC))
                            remove(npc);
                }
                else
                    DisplayMessage(client.Player, "Radius not valid");
            }
                
            if (args[1].Equals("create"))
            {
                int temp = 0;
                int.TryParse(args[2], out temp);
                if (temp == 0)
                {
                    DisplayMessage(client.Player, "Amount not valid, using default value of 1");
                    temp = 1;
                }
                SpawnSpamMob(client, temp);
            }
            //create multiple mobs
        }
            
        if (args.Length == 4)
        {
            if (args[1].Equals("create"))
            {
                int temp = 0;
                int.TryParse(args[2], out temp);
                if (temp == 0)
                {
                    temp = 1;
                }
                    
                int radius;
                if (int.TryParse(args[3], out radius))
                {
                    SpawnSpamMob(client, temp, radius);
                }
                else
                {
                    DisplayMessage(client.Player, "Radius not valid");
                }
                    
            }
            //create multiple mobs
        }
    }

    public void SpawnSpamMob(GameClient client, int number, int radius = 0)
    {
        for (int i = 0; i < number; i++)
        {
            string theType = "DOL.GS.SpamMob.SpamMobNPC";
            byte realm = 0;

            //Create a new mob
            GameNpc mob = null;

            foreach (Assembly script in ScriptMgr.GameServerScripts)
            {
                try
                {
                    client.Out.SendDebugMessage(script.FullName);
                    mob = (GameNpc) script.CreateInstance(theType, false);

                    if (mob != null)
                        break;
                }
                catch (Exception e)
                {
                    client.Out.SendMessage(e.ToString(), EChatType.CT_System, EChatLoc.CL_PopupWindow);
                }
            }

            if (mob == null)
            {
                client.Out.SendMessage("There was an error creating an instance of " + theType + "!",
                    EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }

            //Fill the object variables
            mob.X = client.Player.X + Util.Random(radius);
            mob.Y = client.Player.Y + Util.Random(radius);
            mob.Z = client.Player.Z;
            mob.CurrentRegion = client.Player.CurrentRegion;
            mob.Heading = client.Player.Heading;
            mob.Level = 50;
            mob.Realm = (ERealm) realm;
            mob.Name = "Spam Mob";
            mob.Model = 34;

            //Fill the living variables
            mob.CurrentSpeed = 0;
            mob.MaxSpeedBase = 200;
            mob.GuildName = "Burn Baby Burn";
            mob.Size = 50;
            mob.Flags |= ENpcFlags.PEACE;
            mob.Flags ^= ENpcFlags.PEACE;
            mob.AddToWorld();
            //mob.LoadedFromScript = false; // allow saving
            //mob.SaveIntoDatabase();
            //client.Out.SendMessage("Mob created: OID=" + mob.ObjectID, eChatType.CT_System, eChatLoc.CL_SystemWindow);
            //client.Out.SendMessage("The mob has been created with the peace flag, so it can't be attacked, to remove type /mob peace", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }
    }
        
    private void remove(GameNpc targetMob)
    {
        targetMob.StopAttack();
        targetMob.StopCurrentSpellcast();
        targetMob.DeleteFromDatabase();
        targetMob.Delete();
    }
}

public class SpamMobBrain : StandardMobBrain
{
    public SpamMobBrain()
    {
    }

    public override void Think()
    {
        base.Think();
    }

    public override bool CheckSpells(ECheckSpellType type)
    {
        if (Body.IsCasting)
            return true;

        bool casted = false;

        if (Body != null && Body.Spells != null && Body.Spells.Count > 0)
        {
            ArrayList spell_rec = new ArrayList();
            Spell spellToCast = null;

            if (type == ECheckSpellType.Defensive)
            {
                foreach (Spell spell in Body.Spells)
                {
                    if (Body.GetSkillDisabledDuration(spell) > 0)
                        continue;

                    if (spell.Target is ESpellTarget.ENEMY or ESpellTarget.AREA or ESpellTarget.CONE)
                        continue;

                    if (spell.Uninterruptible && CheckDefensiveSpells(spell))
                        casted = true;
                    else if (!Body.IsBeingInterrupted && CheckDefensiveSpells(spell))
                        casted = true;
                }
            }
            else if (type == ECheckSpellType.Offensive)
            {
                foreach (Spell spell in Body.Spells)
                {
                    if (Body.GetSkillDisabledDuration(spell) == 0)
                    {
                        if (spell.CastTime > 0)
                        {
                            if (spell.Target is ESpellTarget.ENEMY or ESpellTarget.AREA or ESpellTarget.CONE)

                                spell_rec.Add(spell);
                        }
                    }
                }

                if (spell_rec.Count > 0)
                {
                    spellToCast = (Spell) spell_rec[Util.Random((spell_rec.Count - 1))];


                    if (spellToCast.Uninterruptible && CheckOffensiveSpells(spellToCast))
                        casted = true;
                    else if (!Body.IsBeingInterrupted && CheckOffensiveSpells(spellToCast))
                        casted = true;
                }
            }

            return casted;
        }

        return casted;
    }

    protected override bool CheckDefensiveSpells(Spell spell)
    {
        if (spell == null) return false;
        if (Body.GetSkillDisabledDuration(spell) > 0) return false;

        bool casted = false;

        // clear current target, set target based on spell type, cast spell, return target to original target
        GameObject lastTarget = Body.TargetObject;

        Body.TargetObject = null;
        switch (spell.SpellType)
        {
            #region Buffs
            case ESpellType.AcuityBuff:
            case ESpellType.AFHitsBuff:
            case ESpellType.AllMagicResistBuff:
            case ESpellType.ArmorAbsorptionBuff:
            case ESpellType.ArmorFactorBuff:
            case ESpellType.BodyResistBuff:
            case ESpellType.BodySpiritEnergyBuff:
            case ESpellType.Buff:
            case ESpellType.CelerityBuff:
            case ESpellType.ColdResistBuff:
            case ESpellType.CombatSpeedBuff:
            case ESpellType.ConstitutionBuff:
            case ESpellType.CourageBuff:
            case ESpellType.CrushSlashTrustBuff:
            case ESpellType.DexterityBuff:
            case ESpellType.DexterityQuicknessBuff:
            case ESpellType.EffectivenessBuff:
            case ESpellType.EnduranceRegenBuff:
            case ESpellType.EnergyResistBuff:
            case ESpellType.FatigueConsumptionBuff:
            case ESpellType.FlexibleSkillBuff:
            case ESpellType.HasteBuff:
            case ESpellType.HealthRegenBuff:
            case ESpellType.HeatColdMatterBuff:
            case ESpellType.HeatResistBuff:
            case ESpellType.HeroismBuff:
            case ESpellType.KeepDamageBuff:
            case ESpellType.MagicResistBuff:
            case ESpellType.MatterResistBuff:
            case ESpellType.MeleeDamageBuff:
            case ESpellType.MesmerizeDurationBuff:
            case ESpellType.MLABSBuff:
            case ESpellType.PaladinArmorFactorBuff:
            case ESpellType.ParryBuff:
            case ESpellType.PowerHealthEnduranceRegenBuff:
            case ESpellType.PowerRegenBuff:
            case ESpellType.SavageCombatSpeedBuff:
            case ESpellType.SavageCrushResistanceBuff:
            case ESpellType.SavageDPSBuff:
            case ESpellType.SavageParryBuff:
            case ESpellType.SavageSlashResistanceBuff:
            case ESpellType.SavageThrustResistanceBuff:
            case ESpellType.SpiritResistBuff:
            case ESpellType.StrengthBuff:
            case ESpellType.StrengthConstitutionBuff:
            case ESpellType.SuperiorCourageBuff:
            case ESpellType.ToHitBuff:
            case ESpellType.WeaponSkillBuff:
            case ESpellType.DamageAdd:
            case ESpellType.OffensiveProc:
            case ESpellType.DefensiveProc:
            case ESpellType.DamageShield:
            case ESpellType.DamageOverTime:
            {
                // Buff self, if not in melee, but not each and every mob
                // at the same time, because it looks silly.
                if (!LivingHasEffect(Body, spell) && !Body.attackComponent.AttackState && Util.Chance(40) && spell.Target != ESpellTarget.PET)
                {
                    Body.TargetObject = Body;
                    break;
                }
                if (Body.ControlledBrain != null && Body.ControlledBrain.Body != null && Util.Chance(40) && Body.GetDistanceTo(Body.ControlledBrain.Body) <= spell.Range && !LivingHasEffect(Body.ControlledBrain.Body, spell) && spell.Target != ESpellTarget.SELF)
                {
                    Body.TargetObject = Body.ControlledBrain.Body;
                    break;
                }
                break;
            }
            #endregion Buffs

            #region Disease Cure/Poison Cure/Summon
            case ESpellType.CureDisease:
                if (Body.IsDiseased)
                {
                    Body.TargetObject = Body;
                    break;
                }
                if (Body.ControlledBrain != null && Body.ControlledBrain.Body != null && Body.ControlledBrain.Body.IsDiseased
                    && Body.GetDistanceTo(Body.ControlledBrain.Body) <= spell.Range && spell.Target != ESpellTarget.SELF)
                {
                    Body.TargetObject = Body.ControlledBrain.Body;
                    break;
                }
                break;
            case ESpellType.CurePoison:
                if (LivingIsPoisoned(Body))
                {
                    Body.TargetObject = Body;
                    break;
                }
                if (Body.ControlledBrain != null && Body.ControlledBrain.Body != null && LivingIsPoisoned(Body.ControlledBrain.Body)
                    && Body.GetDistanceTo(Body.ControlledBrain.Body) <= spell.Range && spell.Target != ESpellTarget.SELF)
                {
                    Body.TargetObject = Body.ControlledBrain.Body;
                    break;
                }
                break;
            case ESpellType.Summon:
                Body.TargetObject = Body;
                break;
            case ESpellType.SummonMinion:
                //If the list is null, lets make sure it gets initialized!
                if (Body.ControlledNpcList == null)
                    Body.InitControlledBrainArray(2);
                else
                {
                    //Let's check to see if the list is full - if it is, we can't cast another minion.
                    //If it isn't, let them cast.
                    IControlledBrain[] icb = Body.ControlledNpcList;
                    int numberofpets = 0;
                    for (int i = 0; i < icb.Length; i++)
                    {
                        if (icb[i] != null)
                            numberofpets++;
                    }
                    if (numberofpets >= icb.Length)
                        break;
                }
                Body.TargetObject = Body;
                break;
            #endregion Disease Cure/Poison Cure/Summon

            #region Heals
            case ESpellType.CombatHeal:
            case ESpellType.Heal:
            case ESpellType.HealOverTime:
            case ESpellType.MercHeal:
            case ESpellType.OmniHeal:
            case ESpellType.PBAoEHeal:
            case ESpellType.SpreadHeal:
                if (spell.Target == ESpellTarget.SELF)
                {
                    // if we have a self heal and health is less than 75% then heal, otherwise return false to try another spell or do nothing
                    if (Body.HealthPercent < ServerProperties.Properties.NPC_HEAL_THRESHOLD)
                    {
                        Body.TargetObject = Body;
                    }
                    break;
                }

                // Chance to heal self when dropping below 30%, do NOT spam it.
                if (Body.HealthPercent < (ServerProperties.Properties.NPC_HEAL_THRESHOLD / 2.0)
                    && Util.Chance(10) && spell.Target != ESpellTarget.PET)
                {
                    Body.TargetObject = Body;
                    break;
                }

                if (Body.ControlledBrain != null && Body.ControlledBrain.Body != null
                                                 && Body.GetDistanceTo(Body.ControlledBrain.Body) <= spell.Range
                                                 && Body.ControlledBrain.Body.HealthPercent < ServerProperties.Properties.NPC_HEAL_THRESHOLD
                                                 && spell.Target != ESpellTarget.SELF)
                {
                    Body.TargetObject = Body.ControlledBrain.Body;
                    break;
                }
                break;
            #endregion

            //case "SummonAnimistFnF":
            //case "SummonAnimistPet":
            case ESpellType.SummonCommander:
            case ESpellType.SummonDruidPet:
            case ESpellType.SummonHunterPet:
            case ESpellType.SummonNecroPet:
            case ESpellType.SummonUnderhill:
            case ESpellType.SummonSimulacrum:
            case ESpellType.SummonSpiritFighter:
                //case "SummonTheurgistPet":
                if (Body.ControlledBrain != null)
                    break;
                Body.TargetObject = Body;
                break;

            default:
                //log.Warn($"CheckDefensiveSpells() encountered an unknown spell type [{spell.SpellType}]");
                break;
        }

        if (Body.TargetObject != null && (spell.Duration == 0 || (Body.TargetObject is GameLiving living && LivingHasEffect(living, spell) == false)))
        {
            casted = Body.CastSpell(spell, m_mobSpellLine);

            if (casted && spell.CastTime > 0)
            {
                if (Body.IsMoving)
                    Body.StopFollowing();

                if (Body.TargetObject != Body)
                    Body.TurnTo(Body.TargetObject);
            }
        }

        Body.TargetObject = lastTarget;

        return casted;
    }
}

public class SpamMobNPC : GameNpc
{
    private Spell dot;
    private Spell af;
    private Spell str;
    private Spell con;
    private Spell dex;


    public SpamMobNPC() : base(new SpamMobBrain())
    {
        if (dot == null)
        {
            DbSpell spell = new DbSpell();
            spell.AllowAdd = false;
            spell.CastTime = 3;
            spell.Concentration = 0;
            spell.ClientEffect = 10111;
            spell.Icon = 1467;
            spell.Duration = 50;
            spell.Damage = 4;
            spell.DamageType = 14;
            spell.Frequency = 50;
            spell.Name = "DOT";
            spell.Description =
                "Damage Over Time";
            spell.Range = WorldMgr.VISIBILITY_DISTANCE;
            spell.SpellID = 88001;
            spell.Target = "Self";
            spell.Message1 = "Damage Over TIme";
            spell.Type = ESpellType.DamageOverTime.ToString();
            spell.EffectGroup = 1070;

            af = new Spell(spell, 0);
        }

        if (af == null)
        {
            DbSpell spell = new DbSpell();
            spell.AllowAdd = false;
            spell.CastTime = 3;
            spell.Concentration = 0;
            spell.ClientEffect = 1467;
            spell.Icon = 1467;
            spell.Duration = 5;
            spell.Value = 20; //Effective buff 58
            spell.Name = "Armor of the Realm";
            spell.Description =
                "Adds to the recipient's Armor Factor (AF) resulting in better protection againts some forms of attack. It acts in addition to any armor the target is wearing.";
            spell.Range = WorldMgr.VISIBILITY_DISTANCE;
            spell.SpellID = 88001;
            spell.Target = "Self";
            spell.Message1 = "Increases target's Base Armor Factor by 20.";
            spell.Type = ESpellType.ArmorFactorBuff.ToString();
            spell.EffectGroup = 1;

            af = new Spell(spell, 0);
        }

        if (str == null)
        {
            DbSpell spell = new DbSpell();
            spell.AllowAdd = false;
            spell.CastTime = 3;
            spell.Concentration = 0;
            spell.ClientEffect = 5004;
            spell.Icon = 5004;
            spell.Duration = 5;
            spell.Value = 20; //effective buff 55
            spell.Name = "Strength of the Realm";
            spell.Description = "Increases target's Strength.";
            spell.Range = WorldMgr.VISIBILITY_DISTANCE;
            spell.SpellID = 88002;
            spell.Target = "Self";
            spell.Message1 = "Increases target's Strength by 20.";
            spell.Type = ESpellType.StrengthBuff.ToString();
            spell.EffectGroup = 4;

            str = new Spell(spell, 0);
        }

        if (con == null)
        {
            DbSpell spell = new DbSpell();
            spell.AllowAdd = false;
            spell.CastTime = 3;
            spell.Concentration = 0;
            spell.ClientEffect = 5034;
            spell.Icon = 5034;
            spell.Duration = 5;
            spell.Value = 20; //effective buff 55
            spell.Name = "Fortitude of the Realm";
            spell.Description = "Increases target's Constitution.";
            spell.Range = WorldMgr.VISIBILITY_DISTANCE;
            spell.SpellID = 88003;
            spell.Target = "Self";
            spell.Message1 = "Increases target's Constitution by 20.";
            spell.Type = ESpellType.ConstitutionBuff.ToString();
            spell.EffectGroup = 201;

            con = new Spell(spell, 0);
        }

        if (dex == null)
        {
            DbSpell spell = new DbSpell();
            spell.AllowAdd = false;
            spell.CastTime = 3;
            spell.Concentration = 0;
            spell.ClientEffect = 5024;
            spell.Icon = 5024;
            spell.Duration = 5;
            spell.Value = 20; //effective buff 55
            spell.Name = "Dexterity of the Realm";
            spell.Description = "Increases Dexterity for a character.";
            spell.Range = WorldMgr.VISIBILITY_DISTANCE;
            spell.SpellID = 88004;
            spell.Target = "Self";
            spell.Message1 = "Increases target's Dexterity by 20.";
            spell.Type = ESpellType.DexterityBuff.ToString();
            spell.EffectGroup = 202;

            dex = new Spell(spell, 39);
        }

        Spells = new List<Spell>
        {
            SkillBase.GetSpellByID(1311),
            af,
            str,
            con,
            dex
        };
    }

    public override bool IsBeingInterrupted => false;
    public override bool CastSpell(Spell spell, SpellLine line)
    {
        return castingComponent.RequestStartCastSpell(spell, line);
    }

    public override bool CastSpell(Spell spell, SpellLine line, bool checkLOS)
    {
        return castingComponent.RequestStartCastSpell(spell, line);
    }
}