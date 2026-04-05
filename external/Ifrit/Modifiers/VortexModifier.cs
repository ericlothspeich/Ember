using Microsoft.Xna.Framework;

namespace Ifrit.Modifiers
{
    public class VortexModifier : IModifier
    {
        public string Name { get; set; } = "VortexModifier";
        public bool Enabled { get; set; } = true;
        public Vector2 Position { get; set; }
        public float Strength { get; set; }
        public float OuterRadius { get; set; }
        public float InnerRadius { get; set; }
        public float MaxVelocity { get; set; } = float.MaxValue;
        public float RotationAngle { get; set; }

        public void Update(ref Particle particle, float dt)
        {
            Vector2 toParticle = particle.Position - Position;
            float dist = toParticle.Length();
            if (dist < InnerRadius || dist > OuterRadius || dist < 0.0001f) return;

            Vector2 tangent = new Vector2(-toParticle.Y, toParticle.X) / dist;
            float falloff = 1f - (dist - InnerRadius) / (OuterRadius - InnerRadius);
            Vector2 force = tangent * Strength * falloff * dt;
            particle.Velocity += force;

            float speed = particle.Velocity.Length();
            if (speed > MaxVelocity)
                particle.Velocity = (particle.Velocity / speed) * MaxVelocity;
        }
    }
}
