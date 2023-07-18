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

namespace DOL.GS
{
    public abstract class RegionECSAction : ECSGameTimer
    {
        public RegionECSAction(GameObject actionSource) : base(actionSource)
        {
            TimerOwner = actionSource;
            Callback = new ECSTimerCallback(OnTick);
        }

        protected abstract int OnTick(ECSGameTimer timer);
    }

    public abstract class AuxRegionECSAction : AuxECSGameTimer
    {
        public AuxRegionECSAction(GameObject actionSource) : base(actionSource)
        {
            TimerOwner = actionSource;
            Callback = new AuxECSTimerCallback(OnTick);
        }

        protected abstract int OnTick(AuxECSGameTimer timer);
    }
}
