using DOL.Database;
using DOL.GS.PacketHandler;
using System.Collections.Generic;

namespace DOL.GS.Commands
{
    [CmdAttribute("&targetstats",
        ePrivLevel.Player,
        "Display various combat related info about your target",
        "/targetstats")]
    public class TargetStatsCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "targetstats"))
                return;

            GameLiving target = client.Player.TargetObject as GameLiving;
            target ??= client.Player;

            if (target == null)
            {
                client.Out.SendMessage("No target or invalid target selected.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if ((ePrivLevel) client.Account.PrivLevel <= ePrivLevel.Player && client.Player != target && target is GamePlayer)
            {
                client.Out.SendMessage("This command cannot be used on another player.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            List<string> info = [];

            info.Add("+ Stats:");
            info.Add($"Strength:  {target.GetModified(eProperty.Strength)}  |  Constitution:  {target.GetModified(eProperty.Constitution)}");
            info.Add($"Dexterity:  {target.GetModified(eProperty.Dexterity)}  |  Quickness:  {target.GetModified(eProperty.Quickness)}");
            info.Add($"Intelligence:  {target.GetModified(eProperty.Intelligence)}  |  Empathy:  {target.GetModified(eProperty.Empathy)}");
            info.Add($"Piety:  {target.GetModified(eProperty.Piety)}  |  Charisma:  {target.GetModified(eProperty.Charisma)}");

            info.Add("");
            info.Add("+ Resists:");
            info.Add($"Thrust:  {target.GetResist(eDamageType.Thrust)}%  |  Crush:  {target.GetResist(eDamageType.Crush)}%  |  Slash:  {target.GetResist(eDamageType.Slash)}%");
            info.Add($"Heat:  {target.GetResist(eDamageType.Heat)}%  |  Cold:  {target.GetResist(eDamageType.Cold)}%  |  Matter:  {target.GetResist(eDamageType.Matter)}%");
            info.Add($"Energy:  {target.GetResist(eDamageType.Energy)}%  |  Spirit:  {target.GetResist(eDamageType.Spirit)}%  |  Body:  {target.GetResist(eDamageType.Body)}%");

            int naturalResist = target.GetResist(eDamageType.Natural);

            if (naturalResist != 0)
                info.Add($"Natural:  {naturalResist}%");

            DbInventoryItem mainWeapon = target.ActiveWeapon;
            DbInventoryItem leftWeapon = target.ActiveLeftWeapon;
            int leftHandSwingCount = target.attackComponent.CalculateLeftHandSwingCount(mainWeapon, leftWeapon);
            bool isDualWieldAttack = WeaponAction.IsDualWieldAttack(mainWeapon, leftWeapon, target, leftHandSwingCount);
            AttackData.eAttackType attackType = AttackData.GetAttackType(mainWeapon, isDualWieldAttack, target);

            if (target is GameNPC || mainWeapon != null)
            {
                info.Add("");
                info.Add("+ Attack (main hand):");
                DisplayWeaponInfo(mainWeapon, attackType);
            }

            if (target.attackComponent.CanUseLefthandedWeapon)
            {
                if (target is GameNPC npcTarget)
                {
                    double leftHandSwingChance = npcTarget.LeftHandSwingChance;

                    if (leftHandSwingChance > 0)
                        info.Add($"Left hand swing chance:  {leftHandSwingChance:0.##}%");
                }
                else if (target is GamePlayer)
                {
                    info.Add("");
                    info.Add("+ Attack (offhand):");

                    DisplayWeaponInfo(leftWeapon, attackType);
                    double leftHandSwingChance = target.attackComponent.CalculateDwCdLeftHandSwingChance();

                    if (leftHandSwingChance > 0)
                        info.Add($"Swing:  {leftHandSwingChance:0.##}%");
                    else
                    {
                        (double doubleSwingChance, double tripleSwingChance, double quadSwingChance) = target.attackComponent.CalculateHthSwingChances(leftWeapon);

                        if (doubleSwingChance > 0)
                            info.Add($"Double swing:  {doubleSwingChance:0.##}%  |  Triple swing:  {tripleSwingChance:0.##}%  |  Quad swing:  {quadSwingChance:0.##}%");
                    }
                }
            }

            eArmorSlot armorSlot = target is GamePlayer ? eArmorSlot.TORSO : eArmorSlot.NOTSET;
            string armorSlotString = armorSlot is not eArmorSlot.NOTSET ? $" ({armorSlot.ToString().ToLower()})" : string.Empty;

            info.Add("");
            info.Add($"+ Armor{armorSlotString}:");

            double targetArmor = AttackComponent.CalculateTargetArmor(target, armorSlot, out double armorFactor, out double absorb);
            info.Add($"Armor factor:  {armorFactor:0.##}");
            info.Add($"Absorption:  {absorb * 100:0.##}%");
            info.Add($"Armor (AF / ABS):  {targetArmor:0.##}");

            info.Add("");
            info.Add("+ Miscellaneous:");
            info.Add($"Level:  {target.Level}");
            info.Add($"Health:  {target.Health} / {target.MaxHealth}");
            info.Add($"Power:  {target.Mana} / {target.MaxMana}");
            info.Add($"Movement speed:  {target.movementComponent.CurrentSpeed} / {target.movementComponent.MaxSpeed}");
            info.Add($"Block: {target.GetModified(eProperty.BlockChance) * 0.1:0.##}%  |  Parry:  {target.GetModified(eProperty.ParryChance) * 0.1:0.##}%  |  Evade:  {target.GetModified(eProperty.EvadeChance) * 0.1:0.##}%");

            client.Out.SendCustomTextWindow($"[{target.Name}]", info);
            return;

            void DisplayWeaponInfo(DbInventoryItem weapon, AttackData.eAttackType attackType)
            {
                double weaponDamage = target.attackComponent.AttackDamage(weapon, out double weaponDamageCap);
                info.Add($"Weapon damage:  {weaponDamage:0}  |  {weaponDamageCap:0}");

                _ = target.attackComponent.CalculateWeaponSkill(weapon, client.Player, out _, out (double lowerLimit, double upperLimit) varianceRange, out _, out double baseWeaponSkill);
                info.Add($"Weapon skill:  {baseWeaponSkill:0.##}");
                info.Add($"Variance range:  {varianceRange.lowerLimit:0.00}~{varianceRange.upperLimit:0.00}");
                info.Add($"Attack speed:  {target.AttackSpeed(weapon) / 1000.0:0.###}");

                double defensePenetration = target.attackComponent.CalculateDefensePenetration(weapon, client.Player.Level);
                string defensePenetrationString = $"Defense penetration:  {defensePenetration * 100:0.##}%";

                if (attackType is AttackData.eAttackType.MeleeTwoHand)
                {
                    double twoHandedDefensePenetration = defensePenetration + (1 - defensePenetration) * target.TwoHandedDefensePenetrationFactor;

                    if (twoHandedDefensePenetration != defensePenetration)
                        defensePenetrationString += $"  (vs parry:  {twoHandedDefensePenetration * 100:0.##}%)";
                }
                else if (attackType is AttackData.eAttackType.MeleeDualWield)
                {
                    double dualWieldDefensePenetration = defensePenetration + (1 - defensePenetration) * target.DualWieldDefensePenetrationFactor;

                    if (dualWieldDefensePenetration != defensePenetration)
                        defensePenetrationString += $"  (vs evade / block:  {dualWieldDefensePenetration * 100:0.##}%)";
                }

                info.Add(defensePenetrationString);
            }
        }
    }
}
