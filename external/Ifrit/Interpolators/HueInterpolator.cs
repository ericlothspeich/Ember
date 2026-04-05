using Microsoft.Xna.Framework;

namespace Ifrit.Interpolators
{
    public class HueInterpolator : IInterpolator
    {
        public string Name { get; set; } = "HueInterpolator";
        public bool Enabled { get; set; } = true;
        public float StartValue { get; set; }
        public float EndValue { get; set; }

        public void Interpolate(ref Particle particle, float normalizedAge)
        {
            float hue = StartValue + (EndValue - StartValue) * normalizedAge;
            particle.Color = HslColor.ToRgb(new Vector3(hue, 1f, 0.5f));
        }
    }
}
