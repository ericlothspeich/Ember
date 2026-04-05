using Microsoft.Xna.Framework;

namespace Ifrit.Modifiers
{
    public class VelocityColorModifier : IModifier
    {
        public string Name { get; set; } = "VelocityColorModifier";
        public bool Enabled { get; set; } = true;
        public Vector3 StationaryColor { get; set; }
        public Vector3 VelocityColor { get; set; }
        public float VelocityThreshold { get; set; }

        public void Update(ref Particle particle, float dt)
        {
            float speed = particle.Velocity.Length();
            float t = VelocityThreshold > 0f ? speed / VelocityThreshold : 0f;
            if (t > 1f) t = 1f;
            Vector3 hsl = HslColor.Lerp(StationaryColor, VelocityColor, t);
            particle.Color = HslColor.ToRgb(hsl);
        }
    }
}
