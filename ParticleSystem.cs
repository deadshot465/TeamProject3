using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nez;
using Nez.ParticleDesigner;
using Nez.Particles;

namespace TeamProject3
{
    public static class ParticleSystem
    {
        private static Dictionary<ParticleType, ParticleEmitterConfig> _particleTypes =
            new Dictionary<ParticleType, ParticleEmitterConfig>();
        
        public enum ParticleType
        {
            AtomicBubble, BlueFlame, BlueGalaxy, Comet, CrazyBlue, Electrons,
            Fire, Foam, GirosGratis, Huo1, IntoTheBlue, Flash, RisingUp, Swirl1,
            Leaves, BloodSpill, PlasmaGlow, RealPopCorn, ShootingFireball, Snow,
            Sun, Thingy, TouchUp, Trippy, WinnerStars, Wu1
        }

        public static void Initialize()
        {
            var particleConfigs = Directory.GetFiles("Content/particle", "*.pex").ToList();

            var fileKeywords = File.ReadAllLines("Content/particle_types.txt");

            for (var i = 0; i < fileKeywords.Length; i++)
            {
                var index = particleConfigs.FindIndex(str => str.ToLower()
                .Contains(fileKeywords[i]));

                var type = ((ParticleType[])Enum.GetValues(typeof(ParticleType)))[i];

                _particleTypes.Add(type, ParticleEmitterConfigLoader.Load(particleConfigs[index]));
            }
        }

        public static ParticleEmitter CreateEmitter(ParticleType type, bool worldSimulation = true)
        {
            var _emitter = new ParticleEmitter(_particleTypes[type]);
            _emitter.SimulateInWorldSpace = worldSimulation;
            _emitter.CollisionConfig.Enabled = true;
            return _emitter;
        }
    }
}
