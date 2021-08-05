using DOL.GS;
using DOL.GS.Scheduler;

namespace DOL.AI.Brain
{
    /// <summary>
    /// This class contains the brain for water elementals in Albion SI.
    /// These mobs grow with the intensity of the storm and ocassionally cast an effect.
    /// It may be worth finding the effect live does with a packet logger in the future.
    /// </summary>
    public class WaterElementalBrain : StandardMobBrain
    {
        /// <summary>
        /// Store the size & strength buff modifiers
        /// </summary>
        private readonly (ushort rainThreshold, decimal sizeModifier, decimal strengthModifier)[] _modifiers = new[]
        {
            ((ushort)1, .50m, .05m),
            ((ushort)55, .75m, .10m),
            ((ushort)100, 1.25m, .15m),
        };

        /// <summary>
        /// For safety checks
        /// </summary>
        private readonly byte _maxSize = 255;

        /// <summary>
        /// Keep track of original size
        /// </summary>
        private byte _originalSize;

        /// <summary>
        /// Keep track of the original strength
        /// </summary>
        private short _originalStrength;

        /// <summary>
        /// There's no initialize methods for brain? This is toggle for initialization
        /// </summary>
        private bool _initialized = false;

        /// <summary>
        /// Perform neccesary checks to determine wich modifier to apply
        /// </summary>
        public override void Think()
        {
            if (!_initialized)
            {
                _originalSize = Body.Size;
                _originalStrength = Body.Strength;
                _initialized = true;
            }
            if (Body.IsAlive)
            {
                var regionWeather = GameServer.Instance.WorldManager.WeatherManager[Body.CurrentRegionID];
                if(_isInStorm(regionWeather))
                {
                    if (Util.Random(5) == 0)
                    {
                        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        {
                            player.Out.SendSpellEffectAnimation(Body, Body, (ushort)2976, 0, false, 1);
                        }
                    }
                    var playersInRange = Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE);
                    foreach (GamePlayer player in playersInRange)
                    {
                        player.Out.SendSpellEffectAnimation(Body, Body, (ushort)2976, 0, false, 1);
                    }
                    int index = _determinIndex(regionWeather.Intensity);                    
                    byte targetBodySize = (byte)(_originalSize + (byte)(_originalSize * _modifiers[index].sizeModifier));
                    if (Body.Size != targetBodySize)
                    {
                        Body.Size = targetBodySize;
                    }
                    short targetStrength = (short)(_originalStrength + (short)(_originalStrength * _modifiers[index].strengthModifier));
                    if (Body.Strength != targetStrength)
                    {
                        Body.Strength = targetStrength;
                    }
                }
                else
                {
                    if(Body.Size != _originalSize)
                    {
                        Body.Size = _originalSize;
                    }
                    if(Body.Strength != _originalStrength)
                    {
                        Body.Strength = _originalStrength;
                    }
                }
            }
            //This should never happen, but, in a run-away instance, let's not let these become giant
            if(Body.Size >= _maxSize)
            {
                Body.Size = _originalSize;
            }
            base.Think();
        }

        /// <summary>
        /// Check if mob is currently in the storm.
        /// </summary>
        /// <param name="regionWeather"></param>
        /// <returns></returns>
        private bool _isInStorm(RegionWeather regionWeather)
        {
            if(regionWeather is null)
            {
                return false;
            }
            var weatherCurrentPosition = regionWeather.CurrentPosition(SimpleScheduler.Ticks);
            if (Body.X > (weatherCurrentPosition - regionWeather.Width) && Body.X < weatherCurrentPosition)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// A rather not-so data driven way to determine index for modifiers
        /// </summary>
        /// <param name="intensity"></param>
        private int _determinIndex(ushort intensity)
        {
            if (intensity >= _modifiers[0].rainThreshold && intensity < _modifiers[1].rainThreshold)
            {
                return 0;
            }
            else if (intensity >= _modifiers[1].rainThreshold && intensity < _modifiers[2].rainThreshold)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }
    }
}
