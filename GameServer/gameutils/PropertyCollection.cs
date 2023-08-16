/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;

namespace DOL.GS
{
    public class PropertyCollection
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ConcurrentDictionary<string, object> _properties = new();

        public T GetProperty<T>(string key)
        {
            return GetProperty(key, default(T));
        }

        public T GetProperty<T>(string key, T @default)
        {
            return GetProperty(key, @default, false);
        }

        public T GetProperty<T>(string key, T @default, bool logged)
        {
            bool exists = _properties.TryGetValue(key, out object value);

            if (!exists)
            {
                if (Log.IsWarnEnabled && logged)
                    Log.Warn($"Property {key} is required but not found, default value {@default} will be used.");

                return @default;
            }

            if (value is not T result)
            {
                if (Log.IsWarnEnabled && logged)
                    Log.Warn($"Property {key} found, but {value} isn't of the type provided {typeof(T)}, default value {@default} will be used.");

                return @default;
            }

            return result;
        }

        public void SetProperty(string key, object value)
        {
            if (value == null)
                _properties.TryRemove(key, out _);
            else
                _properties[key] = value;
        }

        public void RemoveProperty(string key)
        {
            _properties.TryRemove(key, out _);
        }

        public bool RemoveAndGetProperty(string key, out object value)
        {
            return _properties.TryRemove(key, out value);
        }

        public List<string> GetAllProperties()
        {
            return _properties.Keys.ToList();
        }

        public void RemoveAllProperties()
        {
            _properties.Clear();
        }
    }
}
