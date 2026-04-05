using System;
using Microsoft.Xna.Framework;

namespace Ifrit.Profiles
{
    public enum LineRadiation { None, Directional, PerpendicularUp, PerpendicularDown }

    public class LineProfile : IEmissionProfile
    {
        public Vector2 Axis { get; set; } = Vector2.UnitX;
        public float Length { get; set; }
        public Vector2 Direction { get; set; }
        public LineRadiation Radiate { get; set; }

        public void Emit(Random random, out Vector2 position, out Vector2 direction)
        {
            Vector2 normalizedAxis = Axis == Vector2.Zero ? Vector2.UnitX : Vector2.Normalize(Axis);
            float t = (float)(random.NextDouble() - 0.5) * Length;
            position = normalizedAxis * t;
            if (Radiate == LineRadiation.Directional && Direction != Vector2.Zero)
                direction = Vector2.Normalize(Direction);
            else
            {
                Vector2 perp = new Vector2(-normalizedAxis.Y, normalizedAxis.X);
                if (Radiate == LineRadiation.PerpendicularDown) direction = -perp;
                else if (Radiate == LineRadiation.PerpendicularUp) direction = perp;
                else
                {
                    float angle = (float)(random.NextDouble() * MathHelper.TwoPi);
                    direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                }
            }
        }
    }
}
