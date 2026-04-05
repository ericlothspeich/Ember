using System;
using Microsoft.Xna.Framework;

namespace Ifrit.Profiles
{
    public class SprayProfile : IEmissionProfile
    {
        public Vector2 Direction { get; set; } = -Vector2.UnitY;
        public float Spread { get; set; } = MathHelper.PiOver4;

        public void Emit(Random random, out Vector2 position, out Vector2 direction)
        {
            position = Vector2.Zero;
            float baseAngle = (float)Math.Atan2(Direction.Y, Direction.X);
            float offset = (float)(random.NextDouble() - 0.5) * Spread;
            float angle = baseAngle + offset;
            direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }
    }
}
