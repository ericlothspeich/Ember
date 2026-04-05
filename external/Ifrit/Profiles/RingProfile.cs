using System;
using Microsoft.Xna.Framework;

namespace Ifrit.Profiles
{
    public class RingProfile : IEmissionProfile
    {
        public float Radius { get; set; }
        public CircleRadiation Radiate { get; set; }

        public void Emit(Random random, out Vector2 position, out Vector2 direction)
        {
            float angle = (float)(random.NextDouble() * MathHelper.TwoPi);
            position = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * Radius;
            if (Radiate == CircleRadiation.Out)
                direction = Vector2.Normalize(position);
            else if (Radiate == CircleRadiation.In)
                direction = -Vector2.Normalize(position);
            else
            {
                float dirAngle = (float)(random.NextDouble() * MathHelper.TwoPi);
                direction = new Vector2((float)Math.Cos(dirAngle), (float)Math.Sin(dirAngle));
            }
        }
    }
}
