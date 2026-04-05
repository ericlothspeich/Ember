using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Ifrit.Interpolators;
using Ifrit.Modifiers;
using Ifrit.Profiles;

namespace Ifrit
{
    public class EmberData
    {
        public string Name { get; set; }
        public bool AutoTrigger { get; set; }
        public float AutoTriggerFrequency { get; set; }
        public List<EmberEmitterData> Emitters { get; } = new List<EmberEmitterData>();
    }

    public class EmberEmitterData
    {
        public string Name { get; set; }
        public int Capacity { get; set; }
        public float Lifetime { get; set; }
        public string TextureName { get; set; }
        public int TextureBoundsX { get; set; }
        public int TextureBoundsY { get; set; }
        public int TextureBoundsWidth { get; set; }
        public int TextureBoundsHeight { get; set; }
        public IEmissionProfile Profile { get; set; }
        public List<IModifier> Modifiers { get; } = new List<IModifier>();

        // Release parameters
        public int Quantity { get; set; }
        public float SpeedMin { get; set; }
        public float SpeedMax { get; set; }
        public Vector3 ColorMin { get; set; }
        public Vector3 ColorMax { get; set; }
        public float OpacityMin { get; set; }
        public float OpacityMax { get; set; }
        public Vector2 ScaleMin { get; set; }
        public Vector2 ScaleMax { get; set; }
        public float RotationMin { get; set; }
        public float RotationMax { get; set; }
        public float MassMin { get; set; }
        public float MassMax { get; set; }
    }

    public static class EmberLoader
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static EmberData Parse(string xml)
        {
            var doc = XDocument.Parse(xml);
            var root = doc.Root;

            var effect = new EmberData
            {
                Name = (string)root.Attribute("Name"),
                AutoTrigger = ParseBool(root.Attribute("AutoTrigger")),
                AutoTriggerFrequency = ParseFloat(root.Attribute("AutoTriggerFrequency"))
            };

            var emittersEl = root.Element("Emitters");
            if (emittersEl != null)
            {
                foreach (var emitterEl in emittersEl.Elements("ParticleEmitter"))
                {
                    effect.Emitters.Add(ParseEmitter(emitterEl));
                }
            }

            return effect;
        }

        public static EmberData Load(string filePath)
        {
            return Parse(File.ReadAllText(filePath));
        }

        public static ParticleEmitter CreateEmitter(EmberEmitterData data, EmberData effectData, int atlasWidth, int atlasHeight)
        {
            var emitter = new ParticleEmitter(data.Capacity)
            {
                Lifetime = data.Lifetime,
                SpeedMin = data.SpeedMin,
                SpeedMax = data.SpeedMax,
                ColorStartMin = data.ColorMin,
                ColorStartMax = data.ColorMax,
                OpacityMin = data.OpacityMin,
                OpacityMax = data.OpacityMax,
                ScaleMin = data.ScaleMin,
                ScaleMax = data.ScaleMax,
                RotationMin = data.RotationMin,
                RotationMax = data.RotationMax,
                MassMin = data.MassMin,
                MassMax = data.MassMax,
                Quantity = data.Quantity,
                AutoTrigger = effectData.AutoTrigger,
                AutoTriggerFrequency = effectData.AutoTriggerFrequency,
                Profile = data.Profile
            };

            foreach (var mod in data.Modifiers)
                emitter.Modifiers.Add(mod);

            Vector2 uvOffset = Vector2.Zero;
            Vector2 uvScale = Vector2.One;
            if (atlasWidth > 0 && atlasHeight > 0 && data.TextureBoundsWidth > 0 && data.TextureBoundsHeight > 0)
            {
                uvOffset = new Vector2((float)data.TextureBoundsX / atlasWidth, (float)data.TextureBoundsY / atlasHeight);
                uvScale = new Vector2((float)data.TextureBoundsWidth / atlasWidth, (float)data.TextureBoundsHeight / atlasHeight);
            }

            emitter.UVOffset = uvOffset;
            emitter.UVScale = uvScale;
            emitter.TextureSize = new Vector2(
                data.TextureBoundsWidth > 0 ? data.TextureBoundsWidth : atlasWidth,
                data.TextureBoundsHeight > 0 ? data.TextureBoundsHeight : atlasHeight);

            return emitter;
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private static EmberEmitterData ParseEmitter(XElement el)
        {
            var data = new EmberEmitterData
            {
                Name = (string)el.Attribute("Name"),
                Capacity = ParseInt(el.Attribute("Capacity")),
                Lifetime = ParseFloat(el.Attribute("LifeSpan"))
            };

            // TextureRegion
            var texEl = el.Element("TextureRegion");
            if (texEl != null)
            {
                data.TextureName = (string)texEl.Attribute("Name");
                var boundsStr = (string)texEl.Attribute("Bounds");
                if (!string.IsNullOrEmpty(boundsStr))
                {
                    var parts = boundsStr.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        data.TextureBoundsX = int.Parse(parts[0], Inv);
                        data.TextureBoundsY = int.Parse(parts[1], Inv);
                        data.TextureBoundsWidth = int.Parse(parts[2], Inv);
                        data.TextureBoundsHeight = int.Parse(parts[3], Inv);
                    }
                }
            }

            // Parameters
            var paramsEl = el.Element("Parameters");
            if (paramsEl != null)
                ParseParameters(paramsEl, data);

            // Profile
            var profileEl = el.Element("Profile");
            if (profileEl != null)
                data.Profile = ParseProfile(profileEl);

            // Modifiers
            var modsEl = el.Element("Modifiers");
            if (modsEl != null)
            {
                foreach (var modEl in modsEl.Elements("Modifier"))
                {
                    var mod = ParseModifier(modEl);
                    if (mod != null)
                        data.Modifiers.Add(mod);
                }
            }

            return data;
        }

        private static void ParseParameters(XElement paramsEl, EmberEmitterData data)
        {
            foreach (var child in paramsEl.Elements())
            {
                string kind = (string)child.Attribute("Kind") ?? "Constant";
                string name = child.Name.LocalName;

                switch (name)
                {
                    case "Quantity":
                    {
                        if (kind == "Random")
                            data.Quantity = ParseInt(child.Attribute("RandomMax"));
                        else
                            data.Quantity = ParseInt(child.Attribute("Constant"));
                        break;
                    }
                    case "Speed":
                    {
                        if (kind == "Random")
                        {
                            data.SpeedMin = ParseFloat(child.Attribute("RandomMin"));
                            data.SpeedMax = ParseFloat(child.Attribute("RandomMax"));
                        }
                        else
                        {
                            float val = ParseFloat(child.Attribute("Constant"));
                            data.SpeedMin = val;
                            data.SpeedMax = val;
                        }
                        break;
                    }
                    case "Color":
                    {
                        if (kind == "Random")
                        {
                            data.ColorMin = ParseVector3(child.Attribute("RandomMin"));
                            data.ColorMax = ParseVector3(child.Attribute("RandomMax"));
                        }
                        else
                        {
                            var val = ParseVector3(child.Attribute("Constant"));
                            data.ColorMin = val;
                            data.ColorMax = val;
                        }
                        break;
                    }
                    case "Opacity":
                    {
                        if (kind == "Random")
                        {
                            data.OpacityMin = ParseFloat(child.Attribute("RandomMin"));
                            data.OpacityMax = ParseFloat(child.Attribute("RandomMax"));
                        }
                        else
                        {
                            float val = ParseFloat(child.Attribute("Constant"));
                            data.OpacityMin = val;
                            data.OpacityMax = val;
                        }
                        break;
                    }
                    case "Scale":
                    {
                        if (kind == "Random")
                        {
                            data.ScaleMin = ParseVector2(child.Attribute("RandomMin"));
                            data.ScaleMax = ParseVector2(child.Attribute("RandomMax"));
                        }
                        else
                        {
                            var val = ParseVector2(child.Attribute("Constant"));
                            data.ScaleMin = val;
                            data.ScaleMax = val;
                        }
                        break;
                    }
                    case "Rotation":
                    {
                        if (kind == "Random")
                        {
                            data.RotationMin = ParseFloat(child.Attribute("RandomMin"));
                            data.RotationMax = ParseFloat(child.Attribute("RandomMax"));
                        }
                        else
                        {
                            float val = ParseFloat(child.Attribute("Constant"));
                            data.RotationMin = val;
                            data.RotationMax = val;
                        }
                        break;
                    }
                    case "Mass":
                    {
                        if (kind == "Random")
                        {
                            data.MassMin = ParseFloat(child.Attribute("RandomMin"));
                            data.MassMax = ParseFloat(child.Attribute("RandomMax"));
                        }
                        else
                        {
                            float val = ParseFloat(child.Attribute("Constant"));
                            data.MassMin = val;
                            data.MassMax = val;
                        }
                        break;
                    }
                }
            }
        }

        private static IEmissionProfile ParseProfile(XElement el)
        {
            string type = (string)el.Attribute("Type");
            switch (type)
            {
                case "PointProfile":
                    return new PointProfile();
                case "BoxProfile":
                    return new BoxProfile
                    {
                        Width = ParseFloat(el.Attribute("Width")),
                        Height = ParseFloat(el.Attribute("Height"))
                    };
                case "BoxFillProfile":
                    // Falls back to BoxProfile since BoxFillProfile doesn't exist
                    return new BoxProfile
                    {
                        Width = ParseFloat(el.Attribute("Width")),
                        Height = ParseFloat(el.Attribute("Height"))
                    };
                case "BoxUniformProfile":
                    return new BoxUniformProfile
                    {
                        Width = ParseFloat(el.Attribute("Width")),
                        Height = ParseFloat(el.Attribute("Height"))
                    };
                case "CircleProfile":
                    return new CircleProfile
                    {
                        Radius = ParseFloat(el.Attribute("Radius")),
                        Radiate = ParseEnum<CircleRadiation>(el.Attribute("Radiate"))
                    };
                case "RingProfile":
                    return new RingProfile
                    {
                        Radius = ParseFloat(el.Attribute("Radius")),
                        Radiate = ParseEnum<CircleRadiation>(el.Attribute("Radiate"))
                    };
                case "LineProfile":
                    return new LineProfile
                    {
                        Length = ParseFloat(el.Attribute("Length")),
                        Direction = ParseVector2(el.Attribute("Direction")),
                        Axis = ParseVector2OrDefault(el.Attribute("Axis"), Vector2.UnitX),
                        Radiate = ParseEnum<LineRadiation>(el.Attribute("Radiate"))
                    };
                case "SprayProfile":
                    return new SprayProfile
                    {
                        Direction = ParseVector2OrDefault(el.Attribute("Direction"), -Vector2.UnitY),
                        Spread = ParseFloat(el.Attribute("Spread"))
                    };
                default:
                    return null;
            }
        }

        private static IModifier ParseModifier(XElement el)
        {
            string type = (string)el.Attribute("Type");
            switch (type)
            {
                case "LinearGravityModifier":
                    return new LinearGravityModifier
                    {
                        Direction = ParseVector2(el.Attribute("Direction")),
                        Strength = ParseFloat(el.Attribute("Strength"))
                    };
                case "DragModifier":
                    return new DragModifier
                    {
                        DragCoefficient = ParseFloat(el.Attribute("DragCoefficient")),
                        Density = ParseFloatOrDefault(el.Attribute("Density"), 1f)
                    };
                case "RotationModifier":
                    return new RotationModifier
                    {
                        RotationRate = ParseFloat(el.Attribute("RotationRate"))
                    };
                case "AgeModifier":
                {
                    var ageMod = new AgeModifier();
                    var interpolatorsEl = el.Element("Interpolators");
                    if (interpolatorsEl != null)
                    {
                        foreach (var interpEl in interpolatorsEl.Elements("Interpolator"))
                        {
                            var interp = ParseInterpolator(interpEl);
                            if (interp != null)
                                ageMod.Interpolators.Add(interp);
                        }
                    }
                    return ageMod;
                }
                case "OpacityFastFadeModifier":
                    return new OpacityFastFadeModifier();
                case "VortexModifier":
                    return new VortexModifier
                    {
                        Position = ParseVector2(el.Attribute("Position")),
                        Strength = ParseFloat(el.Attribute("Strength")),
                        OuterRadius = ParseFloat(el.Attribute("OuterRadius")),
                        InnerRadius = ParseFloat(el.Attribute("InnerRadius")),
                        MaxVelocity = ParseFloatOrDefault(el.Attribute("MaxVelocity"), float.MaxValue),
                        RotationAngle = ParseFloat(el.Attribute("RotationAngle"))
                    };
                case "VelocityColorModifier":
                    return new VelocityColorModifier
                    {
                        StationaryColor = ParseVector3(el.Attribute("StationaryColor")),
                        VelocityColor = ParseVector3(el.Attribute("VelocityColor")),
                        VelocityThreshold = ParseFloat(el.Attribute("VelocityThreshold"))
                    };
                case "CircleContainerModifier":
                    return new CircleContainerModifier
                    {
                        Radius = ParseFloat(el.Attribute("Radius")),
                        Inside = ParseBoolOrDefault(el.Attribute("Inside"), true),
                        RestitutionCoefficient = ParseFloatOrDefault(el.Attribute("RestitutionCoefficient"), 1f)
                    };
                case "RectangleContainerModifier":
                    return new RectangleContainerModifier
                    {
                        Width = ParseInt(el.Attribute("Width")),
                        Height = ParseInt(el.Attribute("Height")),
                        RestitutionCoefficient = ParseFloatOrDefault(el.Attribute("RestitutionCoefficient"), 1f)
                    };
                case "RectangleLoopContainerModifier":
                    return new RectangleLoopContainerModifier
                    {
                        Width = ParseInt(el.Attribute("Width")),
                        Height = ParseInt(el.Attribute("Height"))
                    };
                case "VelocityModifier":
                {
                    var velMod = new VelocityModifier
                    {
                        VelocityThreshold = ParseFloatOrDefault(el.Attribute("VelocityThreshold"), 100f)
                    };
                    var interpolatorsEl = el.Element("Interpolators");
                    if (interpolatorsEl != null)
                    {
                        foreach (var interpEl in interpolatorsEl.Elements("Interpolator"))
                        {
                            var interp = ParseInterpolator(interpEl);
                            if (interp != null)
                                velMod.Interpolators.Add(interp);
                        }
                    }
                    return velMod;
                }
                default:
                    return null;
            }
        }

        private static IInterpolator ParseInterpolator(XElement el)
        {
            string type = (string)el.Attribute("Type");
            switch (type)
            {
                case "OpacityInterpolator":
                    return new OpacityInterpolator
                    {
                        StartValue = ParseFloat(el.Attribute("StartValue")),
                        EndValue = ParseFloat(el.Attribute("EndValue"))
                    };
                case "ScaleInterpolator":
                    return new ScaleInterpolator
                    {
                        StartValue = ParseVector2(el.Attribute("StartValue")),
                        EndValue = ParseVector2(el.Attribute("EndValue"))
                    };
                case "ColorInterpolator":
                    return new ColorInterpolator
                    {
                        StartValue = ParseVector3(el.Attribute("StartValue")),
                        EndValue = ParseVector3(el.Attribute("EndValue"))
                    };
                case "RotationInterpolator":
                    return new RotationInterpolator
                    {
                        StartValue = ParseFloat(el.Attribute("StartValue")),
                        EndValue = ParseFloat(el.Attribute("EndValue"))
                    };
                case "HueInterpolator":
                    return new HueInterpolator
                    {
                        StartValue = ParseFloat(el.Attribute("StartValue")),
                        EndValue = ParseFloat(el.Attribute("EndValue"))
                    };
                case "VelocityInterpolator":
                    return new VelocityInterpolator
                    {
                        StartValue = ParseVector2(el.Attribute("StartValue")),
                        EndValue = ParseVector2(el.Attribute("EndValue"))
                    };
                default:
                    return null;
            }
        }

        // ── Attribute parsers ────────────────────────────────────────────────

        private static float ParseFloat(XAttribute attr)
        {
            if (attr == null) return 0f;
            return float.Parse(attr.Value, Inv);
        }

        private static float ParseFloatOrDefault(XAttribute attr, float defaultValue)
        {
            if (attr == null) return defaultValue;
            return float.Parse(attr.Value, Inv);
        }

        private static int ParseInt(XAttribute attr)
        {
            if (attr == null) return 0;
            return int.Parse(attr.Value, Inv);
        }

        private static bool ParseBool(XAttribute attr)
        {
            if (attr == null) return false;
            return string.Equals(attr.Value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ParseBoolOrDefault(XAttribute attr, bool defaultValue)
        {
            if (attr == null) return defaultValue;
            return string.Equals(attr.Value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static Vector2 ParseVector2(XAttribute attr)
        {
            if (attr == null) return Vector2.Zero;
            var parts = attr.Value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return new Vector2(float.Parse(parts[0], Inv), float.Parse(parts[1], Inv));
            if (parts.Length == 1)
            {
                float v = float.Parse(parts[0], Inv);
                return new Vector2(v, v);
            }
            return Vector2.Zero;
        }

        private static Vector2 ParseVector2OrDefault(XAttribute attr, Vector2 defaultValue)
        {
            if (attr == null) return defaultValue;
            return ParseVector2(attr);
        }

        private static Vector3 ParseVector3(XAttribute attr)
        {
            if (attr == null) return Vector3.Zero;
            var parts = attr.Value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
                return new Vector3(float.Parse(parts[0], Inv), float.Parse(parts[1], Inv), float.Parse(parts[2], Inv));
            if (parts.Length >= 2)
                return new Vector3(float.Parse(parts[0], Inv), float.Parse(parts[1], Inv), 0f);
            if (parts.Length == 1)
            {
                float v = float.Parse(parts[0], Inv);
                return new Vector3(v, v, v);
            }
            return Vector3.Zero;
        }

        private static T ParseEnum<T>(XAttribute attr) where T : struct
        {
            if (attr == null) return default(T);
            T result;
            if (Enum.TryParse<T>(attr.Value, out result))
                return result;
            return default(T);
        }
    }
}
