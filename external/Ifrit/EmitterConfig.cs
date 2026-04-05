using Microsoft.Xna.Framework;

namespace Ifrit
{
    public class EmitterConfig
    {
        public string Name { get; set; }
        public int Capacity { get; set; } = 256;
        public float Lifetime { get; set; } = 1f;
        public Vector2 Offset { get; set; }
        public string TextureName { get; set; }
        public int TextureBoundsX { get; set; }
        public int TextureBoundsY { get; set; }
        public int TextureBoundsWidth { get; set; }
        public int TextureBoundsHeight { get; set; }
        public bool AutoTrigger { get; set; }
        public float AutoTriggerFrequency { get; set; } = 0.1f;
        public int Quantity { get; set; } = 10;
        public float SpeedMin { get; set; }
        public float SpeedMax { get; set; }
        public Vector3 ColorStartMin { get; set; }
        public Vector3 ColorStartMax { get; set; }
        public float OpacityMin { get; set; } = 1f;
        public float OpacityMax { get; set; } = 1f;
        public Vector2 ScaleMin { get; set; } = Vector2.One;
        public Vector2 ScaleMax { get; set; } = Vector2.One;
        public float RotationMin { get; set; }
        public float RotationMax { get; set; }
        public float MassMin { get; set; } = 1f;
        public float MassMax { get; set; } = 1f;
    }
}
