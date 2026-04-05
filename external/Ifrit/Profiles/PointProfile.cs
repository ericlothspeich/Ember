using System;
using Microsoft.Xna.Framework;

namespace Ifrit.Profiles
{
    public class PointProfile : IEmissionProfile
    {
        public void Emit(Random random, out Vector2 position, out Vector2 direction)
        {
            position = Vector2.Zero;
            float angle = (float)(random.NextDouble() * MathHelper.TwoPi);
            direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }
    }
}
