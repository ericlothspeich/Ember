namespace Ifrit.Modifiers
{
    public class RectangleLoopContainerModifier : IModifier
    {
        public string Name { get; set; } = "RectangleLoopContainerModifier";
        public bool Enabled { get; set; } = true;
        public int Width { get; set; }
        public int Height { get; set; }

        public void Update(ref Particle particle, float dt)
        {
            float halfW = Width * 0.5f;
            float halfH = Height * 0.5f;

            if (particle.Position.X > halfW) particle.Position.X -= Width;
            else if (particle.Position.X < -halfW) particle.Position.X += Width;

            if (particle.Position.Y > halfH) particle.Position.Y -= Height;
            else if (particle.Position.Y < -halfH) particle.Position.Y += Height;
        }
    }
}
