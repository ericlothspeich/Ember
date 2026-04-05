using Microsoft.Xna.Framework;

namespace Ifrit
{
    /// <summary>CPU-side particle simulation data. Contiguous in arrays for cache locality.</summary>
    public struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;
        public float RotationSpeed;
        public Vector2 Scale;
        public float Opacity;
        public Color Color;
        public float Age;
        public float Lifetime;
        public float Mass;

        // Interpolation endpoints (set on spawn from release params)
        public Vector2 ScaleStart;
        public Vector2 ScaleEnd;
        public float OpacityStart;
        public float OpacityEnd;
        public Color ColorStart;
        public Color ColorEnd;
        public float RotationStart;
        public float RotationEnd;

        public bool IsAlive => Lifetime > 0f && Age <= Lifetime;
    }
}
