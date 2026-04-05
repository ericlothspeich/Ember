using Microsoft.Xna.Framework;

namespace Ifrit.Modifiers
{
    public class DragModifier : IModifier
    {
        public string Name { get; set; } = "DragModifier";
        public bool Enabled { get; set; } = true;
        public float DragCoefficient { get; set; }
        public float Density { get; set; } = 1f;

        public void Update(ref Particle particle, float dt)
        {
            float speed = particle.Velocity.Length();
            if (speed < 0.0001f) return;
            float mass = particle.Mass > 0f ? particle.Mass : 1f;
            float dragForce = 0.5f * Density * DragCoefficient * speed * speed;
            float decel = (dragForce / mass) * dt;
            if (decel >= speed)
            {
                particle.Velocity = Vector2.Zero;
            }
            else
            {
                Vector2 dir = particle.Velocity / speed;
                particle.Velocity -= dir * decel;
            }
        }
    }
}
