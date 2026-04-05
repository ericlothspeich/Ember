using System.Collections.Generic;
using Ifrit.Interpolators;

namespace Ifrit.Modifiers
{
    public class VelocityModifier : IModifier
    {
        public string Name { get; set; } = "VelocityModifier";
        public bool Enabled { get; set; } = true;
        public List<IInterpolator> Interpolators { get; } = new List<IInterpolator>();
        public float VelocityThreshold { get; set; } = 100f;

        public void Update(ref Particle particle, float dt)
        {
            float speed = particle.Velocity.Length();
            float t = VelocityThreshold > 0f ? speed / VelocityThreshold : 0f;
            if (t > 1f) t = 1f;

            for (int i = 0; i < Interpolators.Count; i++)
            {
                Interpolators[i].Interpolate(ref particle, t);
            }
        }
    }
}
