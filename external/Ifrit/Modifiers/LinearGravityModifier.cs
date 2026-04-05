using Microsoft.Xna.Framework;

namespace Ifrit.Modifiers
{
    public class LinearGravityModifier : IModifier
    {
        public string Name { get; set; } = "LinearGravityModifier";
        public bool Enabled { get; set; } = true;
        public Vector2 Direction { get; set; }
        public float Strength { get; set; }

        public void Update(ref Particle particle, float dt)
        {
            particle.Velocity += Direction * Strength * dt;
        }
    }
}
