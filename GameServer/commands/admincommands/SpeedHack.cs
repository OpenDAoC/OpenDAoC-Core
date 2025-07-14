using System;
using System.Reflection;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&speedhack",
        ePrivLevel.Admin,
        "Change speed hack detection parameters (non-persistent).",
        "/speedhack <parameter> <value>")]
    public class SpeedHack : AbstractCommandHandler, ICommandHandler
    {
        private const BindingFlags BINDING_FLAGS = BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase;

        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 3)
            {
                DisplaySyntax(client);
                DisplayCurrentValues(client);
                return;
            }

            string propertyString = args[1];
            PropertyInfo propertyInfo = typeof(PlayerMovementMonitor).GetProperty(propertyString, BINDING_FLAGS);

            if (propertyInfo == null)
            {
                DisplayMessage(client, $"\"{propertyString}\" is not a valid property.");
                DisplayCurrentValues(client);
                return;
            }

            string newValue = args[2];

            try
            {
                SetPropertyFromString(null, propertyInfo, newValue);
            }
            catch
            {
                DisplayMessage(client, $"\"{newValue}\" is not a valid value for \"{propertyString}\".");
                DisplayCurrentValues(client);
                return;
            }

            DisplayMessage(client, $"Set \"{propertyString}\" to \"{newValue}\".");

            static void SetPropertyFromString(object target, PropertyInfo property, string stringValue)
            {
                Type propertyType = property.PropertyType;
                object value;

                if (propertyType == typeof(string))
                    value = stringValue;
                else if (propertyType.IsEnum)
                    value = Enum.Parse(propertyType, stringValue, ignoreCase: true);
                else if (Nullable.GetUnderlyingType(propertyType) is Type nullableType)
                    value = string.IsNullOrEmpty(stringValue) ? null : Convert.ChangeType(stringValue, nullableType);
                else
                    value = Convert.ChangeType(stringValue, propertyType);

                property.SetValue(target, value);
            }
        }

        private void DisplayCurrentValues(GameClient client)
        {
            DisplayMessage(client, $"Available properties and current values:");

            foreach (PropertyInfo property in typeof(PlayerMovementMonitor).GetProperties(BINDING_FLAGS))
                DisplayMessage(client, $"* {property.Name} = {property.GetValue(null)}");
        }
    }
}
