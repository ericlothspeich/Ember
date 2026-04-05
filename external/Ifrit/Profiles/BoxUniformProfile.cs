using System;
using Microsoft.Xna.Framework;

namespace Ifrit.Profiles
{
    /// <summary>
    /// Spawns particles on the perimeter of a rectangle with uniform density
    /// proportional to side length. Longer sides get more particles.
    /// </summary>
    public class BoxUniformProfile : IEmissionProfile
    {
        public float Width { get; set; }
        public float Height { get; set; }

        public void Emit(Random random, out Vector2 position, out Vector2 direction)
        {
            float perimeter = 2f * Width + 2f * Height;
            float value = (float)(random.NextDouble() * perimeter);

            if (value < Width)
            {
                // Top edge
                position = new Vector2((float)(random.NextDouble() - 0.5) * Width, Height * -0.5f);
            }
            else if (value < 2f * Width)
            {
                // Bottom edge
                position = new Vector2((float)(random.NextDouble() - 0.5) * Width, Height * 0.5f);
            }
            else if (value < 2f * Width + Height)
            {
                // Left edge
                position = new Vector2(Width * -0.5f, (float)(random.NextDouble() - 0.5) * Height);
            }
            else
            {
                // Right edge
                position = new Vector2(Width * 0.5f, (float)(random.NextDouble() - 0.5) * Height);
            }

            float angle = (float)(random.NextDouble() * MathHelper.TwoPi);
            direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }
    }
}
