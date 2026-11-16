using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using Terraria;
using Terraria.ModLoader;
using static CursorHighlighter.CursorHighlighterConfig;

namespace CursorHighlighter
{
    public class CursorHighlighterSystem : ModSystem
    {
        private const int PixelSize = 2;

        private static float _leftFade;
        private static float _rightFade;
        private static long _lastTicks;

        public static ModKeybind ToggleHotkey;
        public static ModKeybind CycleModeHotkey;

        private static Texture2D _tex;
        private static int _texRadius = -1;
        private static bool _texPixelated;
        private static bool _texSmooth;

        private static Color _leftColor;
        private static Color _rightColor;
        private static float _lastOpacity = -1f;
        private static Color _lastLeftColor;
        private static Color _lastRightColor;

        private static bool _toggleWasDown;
        private static bool _cycleWasDown;

        private static readonly BlendState InvertBlend = new BlendState
        {
            ColorSourceBlend = Blend.InverseDestinationColor,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.Add
        };

        public override void Load()
        {
            if (Main.dedServ)
                return;

            ToggleHotkey = KeybindLoader.RegisterKeybind(Mod, "ToggleHighlight", 0);
            CycleModeHotkey = KeybindLoader.RegisterKeybind(Mod, "CycleRenderMode", 0);

            Main.OnPostDraw += OnPostDraw;
        }

        public override void Unload()
        {
            if (Main.dedServ)
                return;

            Main.OnPostDraw -= OnPostDraw;

            var tex = _tex;
            _tex = null;

            Main.QueueMainThreadAction(() => tex?.Dispose());
        }

        public override void PostUpdateInput()
        {
            if (Main.dedServ)
                return;

            var cfg = ModContent.GetInstance<CursorHighlighterConfig>();
            if (cfg == null)
                return;

            bool togglePressed = ToggleHotkey.JustPressed;
            bool cyclePressed = CycleModeHotkey.JustPressed;

            if (Main.gameMenu)
            {
                var kb = Keyboard.GetState();

                bool toggleDown = false;
                foreach (string key in ToggleHotkey.GetAssignedKeys())
                {
                    if (Enum.TryParse(key, true, out Keys k) && kb.IsKeyDown(k))
                    {
                        toggleDown = true;
                        break;
                    }
                }

                bool cycleDown = false;
                foreach (string key in CycleModeHotkey.GetAssignedKeys())
                {
                    if (Enum.TryParse(key, true, out Keys k) && kb.IsKeyDown(k))
                    {
                        cycleDown = true;
                        break;
                    }
                }

                if (toggleDown && !_toggleWasDown)
                    togglePressed = true;
                if (cycleDown && !_cycleWasDown)
                    cyclePressed = true;

                _toggleWasDown = toggleDown;
                _cycleWasDown = cycleDown;
            }
            else
            {
                _toggleWasDown = false;
                _cycleWasDown = false;
            }

            if (togglePressed)
            {
                cfg.Toggle = !cfg.Toggle;
                cfg.SaveChanges();
            }

            if (cyclePressed)
            {
                int next = ((int)cfg.RenderMode + 1) % 3;
                cfg.RenderMode = (HighlightRenderMode)next;
                cfg.SaveChanges();
            }
        }

        private static void ResetFade()
        {
            _leftFade = 0f;
            _rightFade = 0f;
            _lastTicks = 0;
        }

        private static float StepFade(float value, bool pressed, float dt, bool fadeEnabled)
        {
            var cfg = ModContent.GetInstance<CursorHighlighterConfig>();

            if (pressed)
                return 1f;
            if (!fadeEnabled || value <= 0f)
                return 0f;
            return MathF.Max(0f, value - dt / cfg.FadeDuration);
        }

        private static void OnPostDraw(GameTime gameTime)
        {
            if (Main.instance == null || !Main.instance.IsActive)
            {
                ResetFade();
                return;
            }

            var cfg = ModContent.GetInstance<CursorHighlighterConfig>();
            if (cfg == null || !cfg.Toggle)
            {
                ResetFade();
                return;
            }

            var mouse = Mouse.GetState();
            bool left = mouse.LeftButton == ButtonState.Pressed;
            bool right = mouse.RightButton == ButtonState.Pressed;

            long now = Stopwatch.GetTimestamp();
            float dt = _lastTicks == 0 ? 0f : MathF.Min((now - _lastTicks) / (float)Stopwatch.Frequency, 0.1f);
            _lastTicks = now;

            _leftFade = StepFade(_leftFade, left, dt, cfg.FadeOut);
            _rightFade = StepFade(_rightFade, right, dt, cfg.FadeOut);

            if (_leftFade <= 0f && _rightFade <= 0f)
            {
                _lastTicks = 0;
                return;
            }

            var device = Main.instance.GraphicsDevice;
            if (device == null)
                return;

            bool pixelated = cfg.Pixelation;
            int block = pixelated ? PixelSize : 1;

            EnsureTexture(device, cfg.Radius, pixelated, cfg.Smoothing, block);
            UpdateColors(cfg);

            Vector2 topLeft = new Vector2(
                mouse.X - _tex.Width * block * 0.5f,
                mouse.Y - _tex.Height * block * 0.5f);

            float leftI = _leftFade * _leftFade;
            float rightI = _rightFade * _rightFade;
            float maxI = MathF.Max(leftI, rightI);

            switch (cfg.RenderMode)
            {
                case HighlightRenderMode.Invert:
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, InvertBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
                    Main.spriteBatch.Draw(_tex, topLeft, null, Color.White * maxI, 0f, Vector2.Zero, block, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                    return;

                case HighlightRenderMode.ColoredInvert:
                    {
                        float wl = leftI / (leftI + rightI + 0.0001f);
                        Color baseColor = Color.Lerp(cfg.RightClickColor, cfg.LeftClickColor, wl);
                        Color drawColor = Premultiply(baseColor, cfg.Transparency * maxI);

                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, InvertBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
                        Main.spriteBatch.Draw(_tex, topLeft, null, drawColor, 0f, Vector2.Zero, block, SpriteEffects.None, 0f);
                        Main.spriteBatch.End();
                        break;
                    }

                default: // Normal
                    {
                        float wl = leftI / (leftI + rightI + 0.0001f);
                        Color baseColor = Color.Lerp(cfg.RightClickColor, cfg.LeftClickColor, wl);
                        Color drawColor = Premultiply(baseColor, cfg.Transparency) * maxI;

                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
                        Main.spriteBatch.Draw(_tex, topLeft, null, drawColor, 0f, Vector2.Zero, block, SpriteEffects.None, 0f);
                        Main.spriteBatch.End();
                        break;
                    }
            }
        }

        private static void UpdateColors(CursorHighlighterConfig cfg)
        {
            if (_lastOpacity == cfg.Transparency &&
                _lastLeftColor == cfg.LeftClickColor &&
                _lastRightColor == cfg.RightClickColor)
                return;

            _lastOpacity = cfg.Transparency;
            _lastLeftColor = cfg.LeftClickColor;
            _lastRightColor = cfg.RightClickColor;

            _leftColor = Premultiply(cfg.LeftClickColor, cfg.Transparency);
            _rightColor = Premultiply(cfg.RightClickColor, cfg.Transparency);
        }

        private static Color Premultiply(Color c, float intensity)
        {
            float a = MathHelper.Clamp(c.A / 255f * intensity, 0f, 1f);
            return new Color(c.R / 255f * a, c.G / 255f * a, c.B / 255f * a, a);
        }

        private static void EnsureTexture(GraphicsDevice device, int radius, bool pixelated, bool smooth, int block)
        {
            if (_tex != null && !_tex.IsDisposed && _texRadius == radius && _texPixelated == pixelated && _texSmooth == smooth)
                return;

            _tex?.Dispose();

            int size = (int)Math.Ceiling(radius * 2f / block);
            if ((size & 1) != 0)
                size++;

            _tex = BuildCircle(device, size, smooth);
            _texRadius = radius;
            _texPixelated = pixelated;
            _texSmooth = smooth;
        }

        private static Texture2D BuildCircle(GraphicsDevice device, int size, bool smooth)
        {
            var tex = new Texture2D(device, size, size);
            var data = new Color[size * size];

            float half = size * 0.5f;
            float halfSq = half * half;

            for (int y = 0; y < size; y++)
            {
                float dy = y + 0.5f - half;
                float dySq = dy * dy;
                int row = y * size;

                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - half;
                    float distSq = dx * dx + dySq;

                    float alpha = smooth ? MathHelper.Clamp(half - MathF.Sqrt(distSq), 0f, 1f) : (distSq <= halfSq ? 1f : 0f);

                    data[row + x] = new Color(alpha, alpha, alpha, alpha);
                }
            }

            tex.SetData(data);
            return tex;
        }
    }
}