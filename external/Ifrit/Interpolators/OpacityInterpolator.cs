namespace Ifrit.Interpolators
{
    public class OpacityInterpolator : IInterpolator
    {
        public string Name { get; set; } = "OpacityInterpolator";
        public bool Enabled { get; set; } = true;
        public float StartValue { get; set; }
        public float EndValue { get; set; }

        public void Interpolate(ref Particle particle, float normalizedAge)
        {
            particle.Opacity = StartValue + (EndValue - StartValue) * normalizedAge;
        }
    }
}
