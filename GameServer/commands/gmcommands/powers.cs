using System;
using System.Globalization;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&powers",
        ePrivLevel.GM,
        "Toggle temporary GM powers.",
        "/powers",
        "/powers <god | attackable | damageboost> [on | off]",
        "/powers damageboost [multiplier]",
        "/powers <status | reset | help>")]
    public class PowersCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private const double DefaultDamageBoost = 10.0;
        private const double MinimumDamageBoost = 1.0;
        private const double MaximumDamageBoost = 100.0;

        public void OnCommand(GameClient client, string[] args)
        {
            if (client?.Player == null)
                return;

            if (IsSpammingCommand(client.Player, "Powers"))
                return;

            if (args.Length == 1 || args[1].Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                DisplayStatus(client);
                return;
            }

            switch (args[1].ToLowerInvariant())
            {
                case "god":
                    ToggleGodMode(client, args);
                    break;
                case "attackable":
                    ToggleAttackable(client, args);
                    break;
                case "damageboost":
                case "damage":
                    ToggleDamageBoost(client, args);
                    break;
                case "reset":
                case "alloff":
                    GMPowers.DisableAll(client.Player);
                    DisplayMessage(client, "[Powers] All powers are OFF.");
                    break;
                case "help":
                    DisplayHelp(client);
                    break;
                default:
                    DisplayMessage(client, $"[Powers] Unknown power '{args[1]}'.");
                    DisplayHelp(client);
                    break;
            }
        }

        private void ToggleGodMode(GameClient client, string[] args)
        {
            GMPowers powers = client.Player.GMPowers;

            if (!TryGetToggleState(client, args, powers.GodModeEnabled, out bool enabled))
                return;

            powers = GMPowers.GetOrCreate(client.Player);
            powers.GodModeEnabled = enabled;
            GMPowers.RemoveIfInactive(client.Player);
            string detail = enabled
                ? "Incoming damage is still reported, but your health will not change."
                : "Incoming damage will reduce your health normally.";
            DisplayPowerState(client, "God mode", enabled, detail);
        }

        private void ToggleAttackable(GameClient client, string[] args)
        {
            GMPowers powers = client.Player.GMPowers;

            if (!TryGetToggleState(client, args, powers.AttackableEnabled, out bool enabled))
                return;

            powers = GMPowers.GetOrCreate(client.Player);
            powers.AttackableEnabled = enabled;
            GMPowers.RemoveIfInactive(client.Player);
            string detail = enabled
                ? "Mobs and enemy players may attack you."
                : "Normal staff attack protection is restored.";
            DisplayPowerState(client, "Attackable", enabled, detail);
        }

        private void ToggleDamageBoost(GameClient client, string[] args)
        {
            GamePlayer player = client.Player;
            GMPowers powers = player.GMPowers;

            if (args.Length == 2)
            {
                if (powers.DamageBoostEnabled)
                {
                    powers.DisableDamageBoost();
                    GMPowers.RemoveIfInactive(player);
                    DisplayPowerState(client, "Damage boost", false, "Outgoing damage multiplier restored to 1.0.");
                }
                else
                    EnableDamageBoost(client, DefaultDamageBoost);

                return;
            }

            if (args.Length > 3)
            {
                DisplayMessage(client, "[Powers] Usage: /powers damageboost [on | off | multiplier]");
                return;
            }

            string value = args[2];

            if (value.Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                EnableDamageBoost(client, powers.DamageBoostEnabled ? powers.DamageMultiplier : DefaultDamageBoost);
                return;
            }

            if (value.Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                powers.DisableDamageBoost();
                GMPowers.RemoveIfInactive(player);
                DisplayPowerState(client, "Damage boost", false, "Outgoing damage multiplier restored to 1.0.");
                return;
            }

            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double multiplier) ||
                !double.IsFinite(multiplier) ||
                multiplier < MinimumDamageBoost ||
                multiplier > MaximumDamageBoost)
            {
                DisplayMessage(client, $"[Powers] Damage multiplier must be a number from {MinimumDamageBoost:0.0} to {MaximumDamageBoost:0.0}.");
                return;
            }

            EnableDamageBoost(client, multiplier);
        }

        private void EnableDamageBoost(GameClient client, double multiplier)
        {
            GMPowers.GetOrCreate(client.Player).EnableDamageBoost(multiplier);
            DisplayPowerState(client, "Damage boost", true, $"Outgoing damage multiplier is {multiplier:0.0}.");
        }

        private bool TryGetToggleState(GameClient client, string[] args, bool currentState, out bool enabled)
        {
            enabled = !currentState;

            if (args.Length == 2)
                return true;

            if (args.Length > 3)
            {
                DisplayMessage(client, $"[Powers] Usage: /powers {args[1].ToLowerInvariant()} [on | off]");
                return false;
            }

            if (args[2].Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                enabled = true;
                return true;
            }

            if (args[2].Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                enabled = false;
                return true;
            }

            DisplayMessage(client, $"[Powers] Expected 'on' or 'off', not '{args[2]}'.");
            return false;
        }

        private void DisplayStatus(GameClient client)
        {
            GMPowers powers = client.Player.GMPowers;
            DisplayMessage(client, "[Powers] Currently enabled:");

            bool anyEnabled = false;

            if (powers.GodModeEnabled)
            {
                DisplayMessage(client, "  - God mode (damage is reported but ignored)");
                anyEnabled = true;
            }

            if (powers.DamageBoostEnabled)
            {
                DisplayMessage(client, $"  - Damage boost ({powers.DamageMultiplier:0.0}x outgoing damage)");
                anyEnabled = true;
            }

            if (powers.AttackableEnabled)
            {
                DisplayMessage(client, "  - Attackable (mobs and enemy players can attack you)");
                anyEnabled = true;
            }

            if (!anyEnabled)
                DisplayMessage(client, "  None");

            DisplayMessage(client, "Use /powers help for commands.");
        }

        private void DisplayHelp(GameClient client)
        {
            DisplayMessage(client, "[Powers] Available commands:");
            DisplayMessage(client, "  /powers god [on|off] - take no damage while retaining damage messages");
            DisplayMessage(client, "  /powers damageboost [on|off|multiplier] - multiply outgoing damage (default 10.0)");
            DisplayMessage(client, "  /powers attackable [on|off] - allow mobs and enemy players to attack you");
            DisplayMessage(client, "  /powers - show enabled powers");
            DisplayMessage(client, "  /powers reset - turn every power off");
        }

        private void DisplayPowerState(GameClient client, string name, bool enabled, string detail)
        {
            DisplayMessage(client, $"[Powers] {name} {(enabled ? "ON" : "OFF")}. {detail}");
        }
    }
}
