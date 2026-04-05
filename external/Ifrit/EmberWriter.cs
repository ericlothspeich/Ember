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
    public class EmberWriteContext
    {
        public string EffectName { get; set; }
        public bool AutoTrigger { get; set; }
        public float AutoTriggerFrequency { get; set; }
        public List<EmitterWriteData> Emitters { get; set; } = new List<EmitterWriteData>();
    }

    public class EmitterWriteData
    {
        public ParticleEmitter Emitter { get; set; }
        public string Name { get; set; }
        public string TextureName { get; set; }
        public int TextureBoundsX { get; set; }
        public int TextureBoundsY { get; set; }
        public int TextureBoundsWidth { get; set; }
        public int TextureBoundsHeight { get; set; }
    }

    public static class EmberWriter
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static void Save(string filePath, EmberWriteContext context)
        {
            File.WriteAllText(filePath, Serialize(context));
        }

        public static string Serialize(EmberWriteContext context)
        {
            var root = new XElement("ParticleEffect",
                new XAttribute("Name", context.EffectName ?? ""),
                new XAttribute("AutoTrigger", FormatBool(context.AutoTrigger)),
                new XAttribute("AutoTriggerFrequency", FormatFloat(context.AutoTriggerFrequency))
            );

            var emittersEl = new XElement("Emitters");
            foreach (var data in context.Emitters)
            {
                emittersEl.Add(WriteEmitter(data));
            }
            root.Add(emittersEl);

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
            using (var sw = new StringWriter())
            {
                doc.Save(sw);
                return sw.ToString();
            }
        }

        private static XElement WriteEmitter(EmitterWriteData data)
        {
            var emitter = data.Emitter;
            var el = new XElement("ParticleEmitter",
                new XAttribute("Name", data.Name ?? ""),
                new XAttribute("Capacity", emitter.Capacity.ToString(Inv)),
                new XAttribute("LifeSpan", FormatFloat(emitter.Lifetime))
            );

            // TextureRegion
            var texEl = new XElement("TextureRegion");
            if (!string.IsNullOrEmpty(data.TextureName))
                texEl.Add(new XAttribute("Name", data.TextureName));
            if (data.TextureBoundsWidth > 0 || data.TextureBoundsHeight > 0)
            {
                texEl.Add(new XAttribute("Bounds", string.Format(Inv, "{0} {1} {2} {3}",
                    data.TextureBoundsX, data.TextureBoundsY, data.TextureBoundsWidth, data.TextureBoundsHeight)));
            }
            el.Add(texEl);

            // Parameters
            el.Add(WriteParameters(emitter));

            // Profile
            if (emitter.Profile != null)
                el.Add(WriteProfile(emitter.Profile));

            // Modifiers
            if (emitter.Modifiers.Count > 0)
            {
                var modsEl = new XElement("Modifiers");
                foreach (var mod in emitter.Modifiers)
                {
                    var modEl = WriteModifier(mod);
                    if (modEl != null)
                        modsEl.Add(modEl);
                }
                el.Add(modsEl);
            }

            return el;
        }

        private static XElement WriteParameters(ParticleEmitter emitter)
        {
            var paramsEl = new XElement("Parameters");

            // Quantity — always written as Constant (EmberLoader reads RandomMax or Constant)
            paramsEl.Add(new XElement("Quantity",
                new XAttribute("Kind", "Constant"),
                new XAttribute("Constant", emitter.Quantity.ToString(Inv))));

            // Speed
            paramsEl.Add(WriteFloatParam("Speed", emitter.SpeedMin, emitter.SpeedMax));

            // Color
            paramsEl.Add(WriteVector3Param("Color", emitter.ColorStartMin, emitter.ColorStartMax));

            // Opacity
            paramsEl.Add(WriteFloatParam("Opacity", emitter.OpacityMin, emitter.OpacityMax));

            // Scale
            paramsEl.Add(WriteVector2Param("Scale", emitter.ScaleMin, emitter.ScaleMax));

            // Rotation
            paramsEl.Add(WriteFloatParam("Rotation", emitter.RotationMin, emitter.RotationMax));

            // Mass
            paramsEl.Add(WriteFloatParam("Mass", emitter.MassMin, emitter.MassMax));

            return paramsEl;
        }

        private static XElement WriteFloatParam(string name, float min, float max)
        {
            if (min == max)
            {
                return new XElement(name,
                    new XAttribute("Kind", "Constant"),
                    new XAttribute("Constant", FormatFloat(min)));
            }
            return new XElement(name,
                new XAttribute("Kind", "Random"),
                new XAttribute("RandomMin", FormatFloat(min)),
                new XAttribute("RandomMax", FormatFloat(max)));
        }

        private static XElement WriteVector2Param(string name, Vector2 min, Vector2 max)
        {
            if (min == max)
            {
                return new XElement(name,
                    new XAttribute("Kind", "Constant"),
                    new XAttribute("Constant", FormatVector2(min)));
            }
            return new XElement(name,
                new XAttribute("Kind", "Random"),
                new XAttribute("RandomMin", FormatVector2(min)),
                new XAttribute("RandomMax", FormatVector2(max)));
        }

        private static XElement WriteVector3Param(string name, Vector3 min, Vector3 max)
        {
            if (min == max)
            {
                return new XElement(name,
                    new XAttribute("Kind", "Constant"),
                    new XAttribute("Constant", FormatVector3(min)));
            }
            return new XElement(name,
                new XAttribute("Kind", "Random"),
                new XAttribute("RandomMin", FormatVector3(min)),
                new XAttribute("RandomMax", FormatVector3(max)));
        }

        private static XElement WriteProfile(IEmissionProfile profile)
        {
            var el = new XElement("Profile");

            if (profile is PointProfile)
            {
                el.Add(new XAttribute("Type", "PointProfile"));
            }
            else if (profile is BoxUniformProfile boxUniform)
            {
                el.Add(new XAttribute("Type", "BoxUniformProfile"));
                el.Add(new XAttribute("Width", FormatFloat(boxUniform.Width)));
                el.Add(new XAttribute("Height", FormatFloat(boxUniform.Height)));
            }
            else if (profile is BoxProfile box)
            {
                el.Add(new XAttribute("Type", "BoxProfile"));
                el.Add(new XAttribute("Width", FormatFloat(box.Width)));
                el.Add(new XAttribute("Height", FormatFloat(box.Height)));
            }
            else if (profile is RingProfile ring)
            {
                el.Add(new XAttribute("Type", "RingProfile"));
                el.Add(new XAttribute("Radius", FormatFloat(ring.Radius)));
                el.Add(new XAttribute("Radiate", ring.Radiate.ToString()));
            }
            else if (profile is CircleProfile circle)
            {
                el.Add(new XAttribute("Type", "CircleProfile"));
                el.Add(new XAttribute("Radius", FormatFloat(circle.Radius)));
                el.Add(new XAttribute("Radiate", circle.Radiate.ToString()));
            }
            else if (profile is LineProfile line)
            {
                el.Add(new XAttribute("Type", "LineProfile"));
                el.Add(new XAttribute("Length", FormatFloat(line.Length)));
                el.Add(new XAttribute("Direction", FormatVector2(line.Direction)));
                el.Add(new XAttribute("Axis", FormatVector2(line.Axis)));
                el.Add(new XAttribute("Radiate", line.Radiate.ToString()));
            }
            else if (profile is SprayProfile spray)
            {
                el.Add(new XAttribute("Type", "SprayProfile"));
                el.Add(new XAttribute("Direction", FormatVector2(spray.Direction)));
                el.Add(new XAttribute("Spread", FormatFloat(spray.Spread)));
            }

            return el;
        }

        private static XElement WriteModifier(IModifier modifier)
        {
            var el = new XElement("Modifier");

            if (modifier is LinearGravityModifier gravity)
            {
                el.Add(new XAttribute("Type", "LinearGravityModifier"));
                el.Add(new XAttribute("Direction", FormatVector2(gravity.Direction)));
                el.Add(new XAttribute("Strength", FormatFloat(gravity.Strength)));
            }
            else if (modifier is DragModifier drag)
            {
                el.Add(new XAttribute("Type", "DragModifier"));
                el.Add(new XAttribute("DragCoefficient", FormatFloat(drag.DragCoefficient)));
                el.Add(new XAttribute("Density", FormatFloat(drag.Density)));
            }
            else if (modifier is RotationModifier rotation)
            {
                el.Add(new XAttribute("Type", "RotationModifier"));
                el.Add(new XAttribute("RotationRate", FormatFloat(rotation.RotationRate)));
            }
            else if (modifier is AgeModifier age)
            {
                el.Add(new XAttribute("Type", "AgeModifier"));
                if (age.Interpolators.Count > 0)
                {
                    var interpolatorsEl = new XElement("Interpolators");
                    foreach (var interp in age.Interpolators)
                    {
                        var interpEl = WriteInterpolator(interp);
                        if (interpEl != null)
                            interpolatorsEl.Add(interpEl);
                    }
                    el.Add(interpolatorsEl);
                }
            }
            else if (modifier is OpacityFastFadeModifier)
            {
                el.Add(new XAttribute("Type", "OpacityFastFadeModifier"));
            }
            else if (modifier is VortexModifier vortex)
            {
                el.Add(new XAttribute("Type", "VortexModifier"));
                el.Add(new XAttribute("Position", FormatVector2(vortex.Position)));
                el.Add(new XAttribute("Strength", FormatFloat(vortex.Strength)));
                el.Add(new XAttribute("OuterRadius", FormatFloat(vortex.OuterRadius)));
                el.Add(new XAttribute("InnerRadius", FormatFloat(vortex.InnerRadius)));
                el.Add(new XAttribute("MaxVelocity", FormatFloat(vortex.MaxVelocity)));
                el.Add(new XAttribute("RotationAngle", FormatFloat(vortex.RotationAngle)));
            }
            else if (modifier is VelocityColorModifier velColor)
            {
                el.Add(new XAttribute("Type", "VelocityColorModifier"));
                el.Add(new XAttribute("StationaryColor", FormatVector3(velColor.StationaryColor)));
                el.Add(new XAttribute("VelocityColor", FormatVector3(velColor.VelocityColor)));
                el.Add(new XAttribute("VelocityThreshold", FormatFloat(velColor.VelocityThreshold)));
            }
            else if (modifier is CircleContainerModifier circleContainer)
            {
                el.Add(new XAttribute("Type", "CircleContainerModifier"));
                el.Add(new XAttribute("Radius", FormatFloat(circleContainer.Radius)));
                el.Add(new XAttribute("Inside", FormatBool(circleContainer.Inside)));
                el.Add(new XAttribute("RestitutionCoefficient", FormatFloat(circleContainer.RestitutionCoefficient)));
            }
            else if (modifier is RectangleLoopContainerModifier rectLoop)
            {
                el.Add(new XAttribute("Type", "RectangleLoopContainerModifier"));
                el.Add(new XAttribute("Width", rectLoop.Width.ToString(Inv)));
                el.Add(new XAttribute("Height", rectLoop.Height.ToString(Inv)));
            }
            else if (modifier is RectangleContainerModifier rectContainer)
            {
                el.Add(new XAttribute("Type", "RectangleContainerModifier"));
                el.Add(new XAttribute("Width", rectContainer.Width.ToString(Inv)));
                el.Add(new XAttribute("Height", rectContainer.Height.ToString(Inv)));
                el.Add(new XAttribute("RestitutionCoefficient", FormatFloat(rectContainer.RestitutionCoefficient)));
            }
            else if (modifier is VelocityModifier velMod)
            {
                el.Add(new XAttribute("Type", "VelocityModifier"));
                el.Add(new XAttribute("VelocityThreshold", FormatFloat(velMod.VelocityThreshold)));
                if (velMod.Interpolators.Count > 0)
                {
                    var interpolatorsEl = new XElement("Interpolators");
                    foreach (var interp in velMod.Interpolators)
                    {
                        var interpEl = WriteInterpolator(interp);
                        if (interpEl != null)
                            interpolatorsEl.Add(interpEl);
                    }
                    el.Add(interpolatorsEl);
                }
            }
            else
            {
                return null;
            }

            return el;
        }

        private static XElement WriteInterpolator(IInterpolator interpolator)
        {
            var el = new XElement("Interpolator");

            if (interpolator is OpacityInterpolator opacity)
            {
                el.Add(new XAttribute("Type", "OpacityInterpolator"));
                el.Add(new XAttribute("StartValue", FormatFloat(opacity.StartValue)));
                el.Add(new XAttribute("EndValue", FormatFloat(opacity.EndValue)));
            }
            else if (interpolator is ScaleInterpolator scale)
            {
                el.Add(new XAttribute("Type", "ScaleInterpolator"));
                el.Add(new XAttribute("StartValue", FormatVector2(scale.StartValue)));
                el.Add(new XAttribute("EndValue", FormatVector2(scale.EndValue)));
            }
            else if (interpolator is ColorInterpolator color)
            {
                el.Add(new XAttribute("Type", "ColorInterpolator"));
                el.Add(new XAttribute("StartValue", FormatVector3(color.StartValue)));
                el.Add(new XAttribute("EndValue", FormatVector3(color.EndValue)));
            }
            else if (interpolator is RotationInterpolator rot)
            {
                el.Add(new XAttribute("Type", "RotationInterpolator"));
                el.Add(new XAttribute("StartValue", FormatFloat(rot.StartValue)));
                el.Add(new XAttribute("EndValue", FormatFloat(rot.EndValue)));
            }
            else if (interpolator is HueInterpolator hue)
            {
                el.Add(new XAttribute("Type", "HueInterpolator"));
                el.Add(new XAttribute("StartValue", FormatFloat(hue.StartValue)));
                el.Add(new XAttribute("EndValue", FormatFloat(hue.EndValue)));
            }
            else if (interpolator is VelocityInterpolator vel)
            {
                el.Add(new XAttribute("Type", "VelocityInterpolator"));
                el.Add(new XAttribute("StartValue", FormatVector2(vel.StartValue)));
                el.Add(new XAttribute("EndValue", FormatVector2(vel.EndValue)));
            }
            else
            {
                return null;
            }

            return el;
        }

        // ── Formatting helpers ──────────────────────────────────────────────

        private static string FormatFloat(float value)
        {
            return value.ToString(Inv);
        }

        private static string FormatBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string FormatVector2(Vector2 v)
        {
            return string.Format(Inv, "{0} {1}", v.X, v.Y);
        }

        private static string FormatVector3(Vector3 v)
        {
            return string.Format(Inv, "{0} {1} {2}", v.X, v.Y, v.Z);
        }
    }
}
