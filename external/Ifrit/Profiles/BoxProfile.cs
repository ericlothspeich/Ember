using System;
using Microsoft.Xna.Framework;

namespace Ifrit.Profiles
{
    public class BoxProfile : IEmissionProfile
    {
        public float Width { get; set; }
        public float Height { get; set; }

        public void Emit(Random random, out Vector2 position, out Vector2 direction)
        {
            float x = (float)(random.NextDouble() - 0.5) * Width;
            float y = (float)(random.NextDouble() - 0.5) * Height;
            position = new Vector2(x, y);
            float angle = (float)(random.NextDouble() * MathHelper.TwoPi);
            direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }
    }
}
