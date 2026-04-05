namespace Ifrit.Interpolators
{
    public class RotationInterpolator : IInterpolator
    {
        public string Name { get; set; } = "RotationInterpolator";
        public bool Enabled { get; set; } = true;
        public float StartValue { get; set; }
        public float EndValue { get; set; }

        public void Interpolate(ref Particle particle, float normalizedAge)
        {
            particle.Rotation = StartValue + (EndValue - StartValue) * normalizedAge;
        }
    }
}
