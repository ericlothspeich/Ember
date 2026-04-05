using System.Collections.Generic;
using Ifrit.Interpolators;

namespace Ifrit.Modifiers
{
    public class AgeModifier : IModifier
    {
        public string Name { get; set; } = "AgeModifier";
        public bool Enabled { get; set; } = true;
        public List<IInterpolator> Interpolators { get; } = new List<IInterpolator>();

        public void Update(ref Particle particle, float dt)
        {
            float t = particle.Lifetime > 0f ? particle.Age / particle.Lifetime : 0f;
            if (t > 1f) t = 1f;
            for (int i = 0; i < Interpolators.Count; i++)
            {
                Interpolators[i].Interpolate(ref particle, t);
            }
        }
    }
}
