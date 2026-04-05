using Microsoft.Xna.Framework;

namespace Ifrit.Interpolators
{
    public class ScaleInterpolator : IInterpolator
    {
        public string Name { get; set; } = "ScaleInterpolator";
        public bool Enabled { get; set; } = true;
        public Vector2 StartValue { get; set; }
        public Vector2 EndValue { get; set; }

        public void Interpolate(ref Particle particle, float normalizedAge)
        {
            particle.Scale = Vector2.Lerp(StartValue, EndValue, normalizedAge);
        }
    }
}
