using Microsoft.Xna.Framework;

namespace Ifrit.Interpolators
{
    public class VelocityInterpolator : IInterpolator
    {
        public string Name { get; set; } = "VelocityInterpolator";
        public bool Enabled { get; set; } = true;
        public Vector2 StartValue { get; set; }
        public Vector2 EndValue { get; set; }

        public void Interpolate(ref Particle particle, float normalizedAge)
        {
            particle.Velocity = Vector2.Lerp(
                new Vector2(StartValue.X, StartValue.Y),
                new Vector2(EndValue.X, EndValue.Y),
                normalizedAge
            );
        }
    }
}
