using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Ifrit.Modifiers;
using Ifrit.Profiles;

namespace Ifrit
{
    public class ParticleEmitter
    {
        private readonly Particle[] _particles;
        private readonly int _capacity;
        private readonly Random _random = new Random();
        private int _liveCount;
        private float _triggerTimer;

        public string Name { get; set; } = "ParticleEmitter";
        public bool Visible { get; set; } = true;
        public IEmissionProfile Profile { get; set; }
        public List<IModifier> Modifiers { get; } = new List<IModifier>();

        public float Lifetime { get; set; } = 1f;
        public float SpeedMin { get; set; }
        public float SpeedMax { get; set; }
        public Vector3 ColorStartMin { get; set; }
        public Vector3 ColorStartMax { get; set; }
        public float OpacityMin { get; set; } = 1f;
        public float OpacityMax { get; set; } = 1f;
        public Vector2 ScaleMin { get; set; } = Vector2.One;
        public Vector2 ScaleMax { get; set; } = Vector2.One;
        public bool ScaleUniform { get; set; }
        public float RotationMin { get; set; }
        public float RotationMax { get; set; }
        public float MassMin { get; set; } = 1f;
        public float MassMax { get; set; } = 1f;
        public int Quantity { get; set; } = 10;
        public bool AutoTrigger { get; set; }
        public float AutoTriggerFrequency { get; set; } = 0.1f;
        public Vector2 Offset { get; set; }
        public float DepthOffset { get; set; }
        public Vector2 UVOffset { get; set; }
        public Vector2 UVScale { get; set; } = Vector2.One;

        /// <summary>Texture region size in pixels. Scale is normalized to this —
        /// Scale 1.0 = one full texture region in world units.</summary>
        public Vector2 TextureSize { get; set; } = Vector2.One;
        public int LiveCount => _liveCount;
        public int Capacity => _capacity;

        public ParticleEmitter(int capacity)
        {
            _capacity = capacity;
            _particles = new Particle[capacity];
        }

        public void Trigger(int count)
        {
            if (Profile == null) return;
            int spawned = 0;
            for (int i = 0; i < _capacity && spawned < count; i++)
            {
                if (!_particles[i].IsAlive)
                {
                    SpawnParticle(ref _particles[i]);
                    spawned++;
                    _liveCount++;
                }
            }
        }

        public void Update(float dt)
        {
            if (AutoTrigger)
            {
                _triggerTimer += dt;
                if (_triggerTimer >= AutoTriggerFrequency)
                {
                    _triggerTimer -= AutoTriggerFrequency;
                    Trigger(Quantity);
                }
            }

            _liveCount = 0;
            for (int i = 0; i < _capacity; i++)
            {
                if (!_particles[i].IsAlive) continue;
                _particles[i].Position += _particles[i].Velocity * dt;
                for (int m = 0; m < Modifiers.Count; m++)
                {
                    if (!Modifiers[m].Enabled)
                        continue;
                    Modifiers[m].Update(ref _particles[i], dt);
                }
                _particles[i].Age += dt;
                if (_particles[i].IsAlive)
                    _liveCount++;
            }
        }

        public int WriteInstanceData(ParticleVertex[] instances, int writeOffset)
        {
            return WriteInstanceData(instances, writeOffset, Vector3.Zero);
        }

        public int WriteInstanceData(ParticleVertex[] instances, int writeOffset, Vector3 worldPosition)
        {
            int written = 0;
            for (int i = 0; i < _capacity; i++)
            {
                if (!_particles[i].IsAlive) continue;
                instances[writeOffset + written] = new ParticleVertex
                {
                    Position = new Vector3(
                        _particles[i].Position.X + Offset.X + worldPosition.X,
                        _particles[i].Position.Y + Offset.Y + worldPosition.Y,
                        worldPosition.Z),
                    Rotation = _particles[i].Rotation,
                    Scale = _particles[i].Scale * TextureSize,
                    Color = _particles[i].Color * _particles[i].Opacity,
                    UVOffset = UVOffset,
                    UVScale = UVScale
                };
                written++;
            }
            return written;
        }

        public ParticleVertex[] GetInstanceData()
        {
            var instances = new ParticleVertex[_liveCount];
            WriteInstanceData(instances, 0);
            return instances;
        }

        private void SpawnParticle(ref Particle p)
        {
            Profile.Emit(_random, out Vector2 position, out Vector2 direction);
            float speed = Lerp(SpeedMin, SpeedMax, (float)_random.NextDouble());
            float opacity = Lerp(OpacityMin, OpacityMax, (float)_random.NextDouble());
            float rotation = Lerp(RotationMin, RotationMax, (float)_random.NextDouble());
            float mass = Lerp(MassMin, MassMax, (float)_random.NextDouble());
            float t = (float)_random.NextDouble();
            Vector2 scale;
            if (ScaleUniform)
            {
                float s = Lerp(ScaleMin.X, ScaleMax.X, t);
                scale = new Vector2(s, s);
            }
            else
            {
                scale = Vector2.Lerp(ScaleMin, ScaleMax, t);
            }
            Vector3 colorHsl = HslColor.Lerp(ColorStartMin, ColorStartMax, (float)_random.NextDouble());

            p.Position = position;
            p.Velocity = direction * speed;
            p.Rotation = rotation;
            p.RotationSpeed = 0f;
            p.Scale = scale;
            p.Opacity = opacity;
            p.Color = HslColor.ToRgb(colorHsl);
            p.Age = 0f;
            p.Lifetime = Lifetime;
            p.Mass = mass;
            p.ScaleStart = scale;
            p.ScaleEnd = scale;
            p.OpacityStart = opacity;
            p.OpacityEnd = 0f;
            p.ColorStart = p.Color;
            p.ColorEnd = p.Color;
            p.RotationStart = rotation;
            p.RotationEnd = rotation;
        }

        private static float Lerp(float a, float b, float t) { return a + (b - a) * t; }
    }
}
