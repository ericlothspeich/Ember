namespace Ifrit.Modifiers
{
    public class RectangleContainerModifier : IModifier
    {
        public string Name { get; set; } = "RectangleContainerModifier";
        public bool Enabled { get; set; } = true;
        public int Width { get; set; }
        public int Height { get; set; }
        public float RestitutionCoefficient { get; set; } = 1f;

        public void Update(ref Particle particle, float dt)
        {
            float halfW = Width * 0.5f;
            float halfH = Height * 0.5f;

            if (particle.Position.X > halfW)
            {
                particle.Position.X = halfW;
                particle.Velocity.X = -particle.Velocity.X * RestitutionCoefficient;
            }
            else if (particle.Position.X < -halfW)
            {
                particle.Position.X = -halfW;
                particle.Velocity.X = -particle.Velocity.X * RestitutionCoefficient;
            }

            if (particle.Position.Y > halfH)
            {
                particle.Position.Y = halfH;
                particle.Velocity.Y = -particle.Velocity.Y * RestitutionCoefficient;
            }
            else if (particle.Position.Y < -halfH)
            {
                particle.Position.Y = -halfH;
                particle.Velocity.Y = -particle.Velocity.Y * RestitutionCoefficient;
            }
        }
    }
}
