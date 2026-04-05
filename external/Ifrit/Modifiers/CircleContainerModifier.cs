using Microsoft.Xna.Framework;

namespace Ifrit.Modifiers
{
    public class CircleContainerModifier : IModifier
    {
        public string Name { get; set; } = "CircleContainerModifier";
        public bool Enabled { get; set; } = true;
        public float Radius { get; set; }
        public bool Inside { get; set; } = true;
        public float RestitutionCoefficient { get; set; } = 1f;

        public void Update(ref Particle particle, float dt)
        {
            float dist = particle.Position.Length();
            if (Inside && dist > Radius && dist > 0.0001f)
            {
                Vector2 normal = particle.Position / dist;
                particle.Position = normal * Radius;
                float dot = Vector2.Dot(particle.Velocity, normal);
                if (dot > 0f) particle.Velocity -= normal * dot * (1f + RestitutionCoefficient);
            }
            else if (!Inside && dist < Radius && dist > 0.0001f)
            {
                Vector2 normal = -particle.Position / dist;
                particle.Position = -normal * Radius;
                float dot = Vector2.Dot(particle.Velocity, normal);
                if (dot > 0f) particle.Velocity -= normal * dot * (1f + RestitutionCoefficient);
            }
        }
    }
}
