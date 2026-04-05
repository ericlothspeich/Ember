namespace Ifrit.Modifiers
{
    public class RotationModifier : IModifier
    {
        public string Name { get; set; } = "RotationModifier";
        public bool Enabled { get; set; } = true;
        public float RotationRate { get; set; }

        public void Update(ref Particle particle, float dt)
        {
            particle.Rotation += RotationRate * dt;
        }
    }
}
