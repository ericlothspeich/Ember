using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ifrit
{
    /// <summary>
    /// GPU-instanced particle renderer. Manages emitters, shared instance buffer,
    /// and a single DrawInstancedPrimitives call. Positioned in the scene via WorldPosition.
    /// Use ParticleSystem (in Core) to integrate with the 3D scene graph.
    /// </summary>
    public class ParticleEffect : IDisposable
    {
        private static readonly VertexPositionTexture[] QuadVertices = new VertexPositionTexture[]
        {
            new VertexPositionTexture(new Vector3(-0.5f, -0.5f, 0f), new Vector2(0f, 1f)),
            new VertexPositionTexture(new Vector3(-0.5f,  0.5f, 0f), new Vector2(0f, 0f)),
            new VertexPositionTexture(new Vector3( 0.5f,  0.5f, 0f), new Vector2(1f, 0f)),
            new VertexPositionTexture(new Vector3( 0.5f, -0.5f, 0f), new Vector2(1f, 1f)),
        };

        private static readonly short[] QuadIndices = new short[] { 0, 1, 2, 0, 2, 3 };

        private readonly GraphicsDevice _graphicsDevice;
        private readonly VertexBuffer _quadVertexBuffer;
        private readonly IndexBuffer _quadIndexBuffer;
        private readonly Effect _effect;
        private readonly EffectParameter _viewParam;
        private readonly EffectParameter _projectionParam;
        private readonly EffectParameter _textureParam;

        private ParticleVertex[] _instances;
        private DynamicVertexBuffer _instanceBuffer;
        private int _totalLiveCount;
        private int _allocatedCapacity;

        public string Name { get; set; }
        public List<ParticleEmitter> Emitters { get; } = new List<ParticleEmitter>();

        public bool AutoTrigger { get; set; }
        public float AutoTriggerFrequency { get; set; } = 1.0f;

        public bool DepthSort { get; set; }

        public Matrix View { set => _viewParam?.SetValue(value); }
        public Matrix Projection { set => _projectionParam?.SetValue(value); }
        public Texture2D Texture { set => _textureParam?.SetValue(value); }

        public int TotalLiveCount => _totalLiveCount;

        /// <summary>World-space position. All particles are offset by this.</summary>
        public Vector3 WorldPosition { get; set; }

        public ParticleEffect(GraphicsDevice graphicsDevice, Effect effect)
        {
            _graphicsDevice = graphicsDevice;
            _effect = effect;

            _viewParam = effect.Parameters["View"];
            _projectionParam = effect.Parameters["Projection"];
            _textureParam = effect.Parameters["Texture"];

            _quadVertexBuffer = new VertexBuffer(
                graphicsDevice, typeof(VertexPositionTexture),
                QuadVertices.Length, BufferUsage.WriteOnly);
            _quadVertexBuffer.SetData(QuadVertices);

            _quadIndexBuffer = new IndexBuffer(
                graphicsDevice, IndexElementSize.SixteenBits,
                QuadIndices.Length, BufferUsage.WriteOnly);
            _quadIndexBuffer.SetData(QuadIndices);

            _allocatedCapacity = 256;
            _instances = new ParticleVertex[_allocatedCapacity];
            _instanceBuffer = new DynamicVertexBuffer(
                graphicsDevice, ParticleVertex.VertexDeclaration,
                _allocatedCapacity, BufferUsage.WriteOnly);
        }

        public void AddEmitter(ParticleEmitter emitter)
        {
            Emitters.Add(emitter);
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = 0; i < Emitters.Count; i++)
                Emitters[i].Update(dt);

            int totalNeeded = 0;
            for (int i = 0; i < Emitters.Count; i++)
                totalNeeded += Emitters[i].LiveCount;

            if (totalNeeded > _allocatedCapacity)
            {
                _allocatedCapacity = totalNeeded * 2;
                _instances = new ParticleVertex[_allocatedCapacity];
                _instanceBuffer.Dispose();
                _instanceBuffer = new DynamicVertexBuffer(
                    _graphicsDevice, ParticleVertex.VertexDeclaration,
                    _allocatedCapacity, BufferUsage.WriteOnly);
            }

            _totalLiveCount = 0;
            for (int i = 0; i < Emitters.Count; i++)
            {
                int written = Emitters[i].WriteInstanceData(_instances, _totalLiveCount, WorldPosition);
                _totalLiveCount += written;
            }

            if (DepthSort && _totalLiveCount > 1)
                Array.Sort(_instances, 0, _totalLiveCount, DepthComparer.Instance);
        }

        public void Draw()
        {
            if (_totalLiveCount == 0) return;

            _instanceBuffer.SetData(_instances, 0, _totalLiveCount, SetDataOptions.Discard);

            _graphicsDevice.SetVertexBuffers(
                new VertexBufferBinding(_quadVertexBuffer, 0, 0),
                new VertexBufferBinding(_instanceBuffer, 0, 1));
            _graphicsDevice.Indices = _quadIndexBuffer;

            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawInstancedPrimitives(
                    PrimitiveType.TriangleList, 0, 0, 2, _totalLiveCount);
            }
        }

        public void Trigger()
        {
            for (int i = 0; i < Emitters.Count; i++)
                Emitters[i].Trigger(Emitters[i].Quantity);
        }

        public void Dispose()
        {
            _quadVertexBuffer?.Dispose();
            _quadIndexBuffer?.Dispose();
            _instanceBuffer?.Dispose();
            GC.SuppressFinalize(this);
        }

        public static ParticleEffect Create(GraphicsDevice graphicsDevice, Effect particleEffect)
        {
            return new ParticleEffect(graphicsDevice, particleEffect.Clone());
        }

        private class DepthComparer : IComparer<ParticleVertex>
        {
            public static readonly DepthComparer Instance = new DepthComparer();
            public int Compare(ParticleVertex a, ParticleVertex b)
            {
                return a.Position.Z.CompareTo(b.Position.Z);
            }
        }
    }
}
