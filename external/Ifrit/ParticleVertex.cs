using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ifrit
{
    /// <summary>GPU instance vertex — one per live particle, uploaded each frame.
    /// UVOffset + UVScale select the particle's region in the shared texture atlas.</summary>
    public struct ParticleVertex : IVertexType
    {
        public Vector3 Position;
        public float Rotation;
        public Vector2 Scale;
        public Color Color;
        public Vector2 UVOffset;
        public Vector2 UVScale;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            new VertexElement(0,  VertexElementFormat.Vector3, VertexElementUsage.Position, 1),
            new VertexElement(12, VertexElementFormat.Single,  VertexElementUsage.TextureCoordinate, 1),
            new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 2),
            new VertexElement(24, VertexElementFormat.Color,   VertexElementUsage.Color, 1),
            new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 3),
            new VertexElement(36, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 4)
        );

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }
}
