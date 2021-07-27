using DOL.Events;
using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.AI.Brain
{
    public class WaterElemental : StandardMobBrain
    {
        //max size of anything is 255
        /// <summary>
        /// Store the size & strength buff modifiers
        /// </summary>
        private readonly (ushort rainThreshold, decimal sizeModifier, decimal strengthModifier)[] _modifiers = new[]
        {
            ((ushort)5, .50m, .25m),
            ((ushort)55, .75m, .50m),
            ((ushort)100, 1.25m, .75m),
        };

        /// <summary>
        /// Minimum intensity before performing buffs
        /// </summary>
        private readonly ushort _minIntensity = 5;

        /// <summary>
        /// For safety checks
        /// </summary>
        private readonly byte _maxSize = 255;

        /// <summary>
        /// Keep track of current position so we can incrementally grow if a large storm pops up quickly.
        /// </summary>
        private int _modifierIndex = -1;


        /// <summary>
        /// Determine if we need to promote/demote when storm starts and begins
        /// </summary>
        private int _modfierOnLastTick  = -1;

        /// <summary>
        /// Keep track of original size
        /// </summary>
        private byte _originalSize;

        private short _originalStrength;

        /// <summary>
        /// There's no initialize methods for brain?
        /// </summary>
        private bool _initialized = false;

        /// <summary>
        /// Perform neccesary checks to determine wich modifier to apply
        /// </summary>
        public override void Think()
        {
            if (!_initialized)
            {
                Console.WriteLine("Init called");
                _originalSize = Body.Size;
                _originalStrength = Body.Strength;
                _initialized = true;
            }
            Console.WriteLine($"CurrentIntensity: {GameServer.Instance.WorldManager.WeatherManager[Body.CurrentRegionID].Intensity} OriginalSize:{_originalSize} CurrentSize: {Body.Size} OriginalStrength: {_originalStrength} CurrentStrength: {Body.Strength}");
            _modfierOnLastTick = _modifierIndex;
            if (Body.IsAlive)
            {
                var regionWeather = GameServer.Instance.WorldManager.WeatherManager[Body.CurrentRegionID];
                if (regionWeather.Intensity >= _modifiers[0].rainThreshold)
                {
                    var playersInRange = Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE);
                    foreach(GamePlayer player in playersInRange)
                    {
                        //14317
                        //202
                        player.Out.SendSpellEffectAnimation(Body, Body, (ushort)2976, 0, false, 1);
                    }
                    _determineIndex(regionWeather.Intensity);                    
                    //Determine if we should promote/demote
                    if (_modfierOnLastTick != _modifierIndex)
                    {
                        _modfierOnLastTick = _modifierIndex;
                        Body.Size += (byte)(Body.Size * _modifiers[_modifierIndex].sizeModifier);
                        Body.Strength += (short)(Body.Strength * _modifiers[_modifierIndex].strengthModifier);                        
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

            base.Think();
        }

        public override bool Start()
        {
            Console.WriteLine("Start Called");
            return base.Start();
        }

        /// <summary>
        /// A rather not-so data driven way to determine index for modifiers
        /// </summary>
        /// <param name="intensity"></param>
        private void _determineIndex(ushort intensity)
        {
            if(intensity >= _modifiers[0].rainThreshold && intensity < _modifiers[1].rainThreshold)
            {
                _modifierIndex = 0;
            }
            else if(intensity >= _modifiers[1].rainThreshold && intensity < _modifiers[2].rainThreshold)
            {
                _modifierIndex = 1;
            }
            else
            {
                _modifierIndex = 2;
            }
        }

    }
}
