using Microsoft.Xna.Framework;

namespace Ifrit.Interpolators
{
    public class ColorInterpolator : IInterpolator
    {
        public string Name { get; set; } = "ColorInterpolator";
        public bool Enabled { get; set; } = true;
        public Vector3 StartValue { get; set; }
        public Vector3 EndValue { get; set; }

        public void Interpolate(ref Particle particle, float normalizedAge)
        {
            Vector3 hsl = HslColor.Lerp(StartValue, EndValue, normalizedAge);
            particle.Color = HslColor.ToRgb(hsl);
        }
    }
}
