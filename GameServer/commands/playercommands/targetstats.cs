using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;
using static DOL.GS.NpcTemplateMgr;

namespace DOL.GS.Commands
{
    [Cmd("&targetstats",
        ePrivLevel.Player,
        "Display various combat related info about your target",
        "/targetstats")]
    public class TargetStatsCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "targetstats"))
                return;

            if (!TryValidateTarget(client, out GameLiving target))
                return;

            List<string> info = new();

            AddStats(info, target);
            AddResistances(info, target);
            AddWeaponsInfo(info, client, target);
            AddArmorInfo(info, target);
            AddDefenseInfo(info, target);
            AddMiscellaneousInfo(info, target);

            client.Out.SendCustomTextWindow($"[{target.Name}]", info);

            static bool TryValidateTarget(GameClient client, out GameLiving target)
            {
                target = client.Player.TargetObject as GameLiving;
                target ??= client.Player;

                if (target == null)
                {
                    client.Out.SendMessage("No target or invalid target selected.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }

                if ((ePrivLevel) client.Account.PrivLevel <= ePrivLevel.Player && client.Player != target && target is GamePlayer)
                {
                    client.Out.SendMessage("This command cannot be used on another player.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }

                return true;
            }

            static void AddStats(List<string> info, GameLiving target)
            {
                info.Add("+ Stats:");
                info.Add($"Strength:  {target.GetModified(eProperty.Strength)}  |  Constitution:  {target.GetModified(eProperty.Constitution)}");
                info.Add($"Dexterity:  {target.GetModified(eProperty.Dexterity)}  |  Quickness:  {target.GetModified(eProperty.Quickness)}");
                info.Add($"Intelligence:  {target.GetModified(eProperty.Intelligence)}  |  Empathy:  {target.GetModified(eProperty.Empathy)}");
                info.Add($"Piety:  {target.GetModified(eProperty.Piety)}  |  Charisma:  {target.GetModified(eProperty.Charisma)}");
            }

            static void AddResistances(List<string> info, GameLiving target)
            {
                info.Add("");
                info.Add("+ Resists:");
                info.Add($"Thrust:  {target.GetResist(eDamageType.Thrust)}%  |  Crush:  {target.GetResist(eDamageType.Crush)}%  |  Slash:  {target.GetResist(eDamageType.Slash)}%");
                info.Add($"Heat:  {target.GetResist(eDamageType.Heat)}%  |  Cold:  {target.GetResist(eDamageType.Cold)}%  |  Matter:  {target.GetResist(eDamageType.Matter)}%");
                info.Add($"Energy:  {target.GetResist(eDamageType.Energy)}%  |  Spirit:  {target.GetResist(eDamageType.Spirit)}%  |  Body:  {target.GetResist(eDamageType.Body)}%");

                int naturalResist = target.GetResist(eDamageType.Natural);

                if (naturalResist != 0)
                    info.Add($"Natural:  {naturalResist}%");
            }

            static void AddWeaponsInfo(List<string> info, GameClient client, GameLiving target)
            {
                DbInventoryItem mainWeapon = target.ActiveWeapon;
                DbInventoryItem leftWeapon = target.ActiveLeftWeapon;
                bool isDualWieldAttack = WeaponAction.IsDualWieldAttack(mainWeapon, leftWeapon, target);
                AttackData.eAttackType attackType = AttackData.GetAttackType(mainWeapon, isDualWieldAttack, target);

                if (target is GameNPC || mainWeapon != null)
                    AddMainHandInfo(info, client, target, mainWeapon, attackType);

                if (target.attackComponent.CanUseLefthandedWeapon)
                    AddOffHandInfo(info, client, target, leftWeapon, attackType);

                static void AddWeaponInfo(List<string> info, string header, GameClient client, GameLiving target, DbInventoryItem weapon, AttackData.eAttackType attackType)
                {
                    double weaponDamage = target.attackComponent.AttackDamage(weapon, null, out double weaponDamageCap);
                    double effectiveness = target.attackComponent.CalculateEffectiveness(weapon);
                    weaponDamage *= effectiveness;
                    weaponDamageCap *= effectiveness;

                    info.Add("");
                    info.Add(header);
                    info.Add($"Weapon damage:  {weaponDamage:0}  |  {weaponDamageCap:0} (cap)");

                    _ = target.attackComponent.CalculateWeaponSkill(weapon, client.Player, out _, out (double lowerLimit, double upperLimit) varianceRange, out _, out double baseWeaponSkill);
                    info.Add($"Weapon skill:  {baseWeaponSkill:0.00}");
                    info.Add($"Variance range:  {varianceRange.lowerLimit:0.00}~{varianceRange.upperLimit:0.00}");
                    info.Add($"Attack speed:  {target.AttackSpeed(weapon) / 1000.0:0.00#}");

                    double defensePenetration = target.attackComponent.CalculateDefensePenetration(weapon, client.Player.Level);
                    string defensePenetrationString = $"Defense penetration:  {defensePenetration * 100:0.00}%";

                    if (attackType is AttackData.eAttackType.MeleeTwoHand)
                    {
                        double twoHandedDefensePenetration = defensePenetration + (1 - defensePenetration) * (1 - target.TwoHandedDefensePenetrationFactor);

                        if (twoHandedDefensePenetration != defensePenetration)
                            defensePenetrationString += $"  (vs parry:  {twoHandedDefensePenetration * 100:0.00}%)";
                    }
                    else if (attackType is AttackData.eAttackType.MeleeDualWield)
                    {
                        double dualWieldDefensePenetration = defensePenetration + (1 - defensePenetration) * (1 - target.DualWieldDefensePenetrationFactor);

                        if (dualWieldDefensePenetration != defensePenetration)
                            defensePenetrationString += $"  (vs evade / block:  {dualWieldDefensePenetration * 100:0.00}%)";
                    }

                    info.Add(defensePenetrationString);
                }

                static void AddMainHandInfo(List<string> info, GameClient client, GameLiving target, DbInventoryItem weapon, AttackData.eAttackType attackType)
                {
                    AddWeaponInfo(info, "+ Attack (main hand):", client, target, weapon, attackType);
                }

                static void AddOffHandInfo(List<string> info, GameClient client, GameLiving target, DbInventoryItem weapon, AttackData.eAttackType attackType)
                {
                    if (target is GameNPC npcTarget)
                    {
                        double leftHandSwingChance = npcTarget.LeftHandSwingChance;

                        if (leftHandSwingChance > 0)
                            info.Add($"Left hand swing chance:  {leftHandSwingChance:0.00}%");
                    }
                    else if (target is GamePlayer)
                    {
                        AddWeaponInfo(info, "+ Attack (offhand):", client, target, weapon, attackType);
                        double leftHandSwingChance = target.attackComponent.CalculateDwCdLeftHandSwingChance();

                        if (leftHandSwingChance > 0)
                            info.Add($"Swing:  {leftHandSwingChance:0.00}%");
                        else
                        {
                            (double doubleSwingChance, double tripleSwingChance, double quadSwingChance) = target.attackComponent.CalculateHthSwingChances(weapon);

                            if (doubleSwingChance > 0)
                                info.Add($"Double swing:  {doubleSwingChance:0.00}%  |  Triple swing:  {tripleSwingChance:0.00}%  |  Quad swing:  {quadSwingChance:0.00}%");
                        }
                    }
                }
            }

            static void AddArmorInfo(List<string> info, GameLiving target)
            {
                eArmorSlot armorSlot = target is GamePlayer ? eArmorSlot.TORSO : eArmorSlot.NOTSET;
                string armorSlotString = armorSlot is not eArmorSlot.NOTSET ? $" ({armorSlot.ToString().ToLower()})" : string.Empty;

                info.Add("");
                info.Add($"+ Armor{armorSlotString}:");

                double targetArmor = AttackComponent.CalculateTargetArmor(target, armorSlot, out double armorFactor, out double absorb);
                info.Add($"Armor factor:  {armorFactor:0.00}");
                info.Add($"Absorption:  {absorb * 100:0.00}%");
                info.Add($"Armor (AF / ABS):  {targetArmor:0.00}");
            }

            static void AddDefenseInfo(List<string> info, GameLiving target)
            {
                AttackData lastAttackData = target.attackComponent.attackAction.LastAttackData;
                int meleeAttackerCount = target.attackComponent.AttackerTracker.MeleeCount;

                AttackData dummyAttackData = new()
                {
                    Attacker = target
                };

                Span<AttackData.eAttackType> attackTypes =
                [
                    AttackData.eAttackType.MeleeOneHand,
                    AttackData.eAttackType.MeleeTwoHand,
                    AttackData.eAttackType.MeleeDualWield,
                    AttackData.eAttackType.Ranged
                ];

                int spanLength = attackTypes.Length + 1;

                Span<double> evades = stackalloc double[spanLength];
                Span<double> parries = stackalloc double[spanLength];
                Span<double> blocks = stackalloc double[spanLength];

                evades[0] = target.GetModified(eProperty.EvadeChance) * 0.001;
                parries[0] = target.GetModified(eProperty.ParryChance) * 0.001;
                blocks[0] = target.GetModified(eProperty.BlockChance) * 0.001;

                for (int i = 1; i < spanLength; i++)
                {
                    dummyAttackData.AttackType = attackTypes[i - 1];
                    evades[i] = target.TryEvade(dummyAttackData, lastAttackData);
                    parries[i] = target.TryParry(dummyAttackData, lastAttackData, meleeAttackerCount);
                    blocks[i] = target.TryBlock(dummyAttackData, out _);
                }

                info.Add("");
                info.Add("+ Defenses (base  |  vs 1h  |  vs 2h  |  vs DW  |  vs ranged):"); // In `attackTypes` order.
                info.Add($"Evade:  {evades[0]:0.00%}  |  {evades[1]:0.00%}  |  {evades[2]:0.00%}  |  {evades[3]:0.00%}  |  {evades[4]:0.00%}");
                info.Add($"Parry:  {parries[0]:0.00%}  |  {parries[1]:0.00%}  |  {parries[2]:0.00%}  |  {parries[3]:0.00%}  |  {parries[4]:0.00%}");
                info.Add($"Block:  {blocks[0]:0.00%}  |  {blocks[1]:0.00%}  |  {blocks[2]:0.00%}  |  {blocks[3]:0.00%}  |  {blocks[4]:0.00%}");
            }

            static void AddMiscellaneousInfo(List<string> info, GameLiving target)
            {
                info.Add("");
                info.Add("+ Miscellaneous:");
                info.Add($"Level:  {target.Level}");
                info.Add($"Health:  {target.Health} / {target.MaxHealth}");

                if (target is GamePlayer)
                    info.Add($"Power:  {target.Mana} / {target.MaxMana}");

                info.Add($"Movement speed:  {target.movementComponent.CurrentSpeed} / {target.movementComponent.MaxSpeed}");

                if (target is GameNPC npc)
                {
                    eBodyType bodyType = (eBodyType) npc.BodyType;

                    if (bodyType is not eBodyType.None)
                        info.Add($"Type:  {bodyType}");
                }
            }
        }
    }
}
