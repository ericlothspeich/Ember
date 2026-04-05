namespace Ifrit.Modifiers
{
    public class OpacityFastFadeModifier : IModifier
    {
        public string Name { get; set; } = "OpacityFastFadeModifier";
        public bool Enabled { get; set; } = true;

        public void Update(ref Particle particle, float dt)
        {
            float t = particle.Lifetime > 0f ? particle.Age / particle.Lifetime : 1f;
            float inv = 1f - t;
            particle.Opacity = inv * inv * inv;
        }
    }
}
