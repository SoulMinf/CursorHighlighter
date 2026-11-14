using Microsoft.Xna.Framework;
using System;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace CursorHighlighter
{
    public class CursorHighlighterConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        public static CursorHighlighterConfig Instance { get; private set; }

        public override void OnLoaded() => Instance = this;

        public override void OnChanged() => Instance = this;

        public enum HighlightRenderMode
        {
            [LabelKey("$Mods.CursorHighlighter.Switch.Negative.Normal")]
            Normal,

            [LabelKey("$Mods.CursorHighlighter.Switch.Negative.Invert")]
            Invert,

            [LabelKey("$Mods.CursorHighlighter.Switch.Negative.ColoredInvert")]
            ColoredInvert
        }



        [Header("$Mods.CursorHighlighter.Headers.Color")]

        [LabelKey("$Mods.CursorHighlighter.Color.LMBColor")]
        [DefaultValue(typeof(Color), "255, 220, 0, 255")]
        [ColorNoAlpha]
        public Color LeftClickColor;

        [LabelKey("$Mods.CursorHighlighter.Color.RMBColor")]
        [DefaultValue(typeof(Color), "0, 140, 255, 255")]
        [ColorNoAlpha]
        public Color RightClickColor;



        [Header("$Mods.CursorHighlighter.Headers.General")]

        // Toggle
        [LabelKey("$Mods.CursorHighlighter.General.Toggle")]
        [DefaultValue(true)]
        public bool Toggle;

        // Transparency
        private float _Transparency = 0.6f;
        [LabelKey("$Mods.CursorHighlighter.General.Transparency")]
        [DefaultValue(0.6f)]
        [Range(0.1f, 1f)]
        [Increment(0.05f)]
        [Slider]
        public float Transparency
        {
            get => (float)Math.Round(_Transparency, 1);
            set => _Transparency = value;
        }

        // Pixelation
        [LabelKey("$Mods.CursorHighlighter.General.Pixelation.Label")]
        [TooltipKey("$Mods.CursorHighlighter.General.Pixelation.Tooltip")]
        [DefaultValue(true)]
        public bool Pixelation;

        // Smoothing
        [LabelKey("$Mods.CursorHighlighter.General.Smoothing.Label")]
        [TooltipKey("$Mods.CursorHighlighter.General.Smoothing.Tooltip")]
        [DefaultValue(true)]
        public bool Smoothing;

        // Fade out
        [LabelKey("$Mods.CursorHighlighter.General.FadeOut.Label")]
        [TooltipKey("$Mods.CursorHighlighter.General.FadeOut.Tooltip")]
        [DefaultValue(false)]
        public bool FadeOut;

        // Negative
        [LabelKey("$Mods.CursorHighlighter.General.RenderMode")]
        [DefaultValue(HighlightRenderMode.Normal)]
        public HighlightRenderMode RenderMode;

        // Fade out duration
        private float _FadeDuration = 0.2f;
        [LabelKey("$Mods.CursorHighlighter.General.FadeOut.FadeDuration")]
        [Range(0.1f, 1f)]
        [DefaultValue(0.2f)]
        public float FadeDuration
        {
            get => (float)Math.Round(_FadeDuration, 1);
            set => _FadeDuration = value;
        }

        // Circle radius
        [LabelKey("$Mods.CursorHighlighter.General.Radius")]
        [DefaultValue(30)]
        [Range(10, 60)]
        [Increment(2)]
        [Slider]
        public int Radius;
    }
}