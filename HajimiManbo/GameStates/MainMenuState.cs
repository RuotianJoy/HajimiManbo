using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace HajimiManbo.GameStates
{
    /// <summary>
    /// 主菜单界面（带十个随机移动的 GIF 动画精灵，其中四个会追踪鼠标）。
    /// </summary>
    public class MainMenuState : GameState
    {
        #region ====== 内嵌动画精灵类 ======
        private class AnimatedSprite
        {
            private readonly Texture2D[] frames;
            private readonly bool followMouse;
            private readonly float frameInterval; // 单帧时长（秒）


            private const float ACCEL_FOLLOW = 800f;  // ← 提升加速度
            private const float MAX_SPEED = 700f;  // ← 最高移动速度
            private const float FRICTION = 0.9f;  // ← 空气阻力

            private float timer;
            private int frameIndex;

            public Vector2 Position;
            private Vector2 velocity;

            public AnimatedSprite(Texture2D[] frames, Vector2 startPos, Vector2 startVelocity, bool followMouse, float frameInterval = 0.1f)
            {
                this.frames = frames;
                Position = startPos;
                velocity = startVelocity;
                this.followMouse = followMouse;
                this.frameInterval = frameInterval;
            }

            public void Update(GameTime gameTime, Vector2 mousePos, Rectangle bounds)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (followMouse)
                {
                    Vector2 dir = mousePos - Position;
                    float len = dir.Length();
                    if (len > 1f)
                    {
                        dir /= len;                          // 归一化
                        velocity += dir * ACCEL_FOLLOW * dt; // 加速
                    }
                }

                // === 阻尼（可选）===
                //velocity *= (float)Math.Pow(FRICTION, dt * 60f); // 60FPS 基准

                // === 限制最高速度（避免“飞走”） ===
                if (velocity.LengthSquared() > MAX_SPEED * MAX_SPEED)
                {
                    velocity.Normalize();
                    velocity *= MAX_SPEED;
                }

                // === 位置更新 ===
                Position += velocity * dt;

                // === 碰到屏幕边缘后弹回 ===
                if (Position.X < 0)
                {
                    Position.X = 0;
                    velocity.X *= -1f;
                }
                else if (Position.X > bounds.Width)
                {
                    Position.X = bounds.Width;
                    velocity.X *= -1f;
                }

                if (Position.Y < 0)
                {
                    Position.Y = 0;
                    velocity.Y *= -1f;
                }
                else if (Position.Y > bounds.Height)
                {
                    Position.Y = bounds.Height;
                    velocity.Y *= -1f;
                }

                // === 动画帧更新 ===
                timer += dt;
                if (timer >= frameInterval)
                {
                    timer -= frameInterval;
                    frameIndex = (frameIndex + 1) % frames.Length;
                }
            }

            public void Draw(SpriteBatch spriteBatch)
            {
                Texture2D tex = frames[frameIndex];
                Vector2 origin = new(tex.Width / 2f, tex.Height / 2f);
                spriteBatch.Draw(tex, Position, null, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
            }
        }
        #endregion

        #region ====== 主菜单配置 ======
        private readonly string[] menuItems = { "开始游戏", "设置", "退出游戏" };
        private int selectedMenuItem = 0;

        // Input
        private MouseState previousMouseState;
        private MouseState currentMouseState;

        // Layout
        private Rectangle[] menuButtonRects;
        public readonly Texture2D backgroundTexture;
        public readonly Texture2D secondBackgroundTexture;
        private readonly Texture2D roomBackgroundTexture;

        // Scaling factors
        private const float TitleScaleMultiplier = 4f; // 标题放大倍数
        private const float MenuScaleMultiplier = 2f; // 菜单字体/按钮放大倍数

        // Colors
        private static readonly Color HoverColor = Color.SkyBlue;

        // === Animated sprites ===
        private const int TotalSprites = 10;
        private const int FollowMouseCount = 4;
        private const int GifFrameCount = 112;      // 每个 GIF 拆分的帧数（自行调整）
        private readonly List<AnimatedSprite> animatedSprites = new();

        private readonly Rectangle screenBounds;
        private readonly Random rand = new();

        #endregion

        public MainMenuState(Game1 game,
                             GraphicsDeviceManager graphics,
                             SpriteBatch spriteBatch,
                             SpriteFont font,
                             Texture2D backgroundTexture,
                             Texture2D secondBackgroundTexture,
                             Texture2D roomBackgroundTexture)
            : base(game, graphics, spriteBatch, font)
        {
            this.backgroundTexture = backgroundTexture;
            this.secondBackgroundTexture = secondBackgroundTexture;
            screenBounds = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);

            CalculateMenuButtonPositions();
            LoadAnimatedSprites();
            this.roomBackgroundTexture = roomBackgroundTexture;
        }

        #region ====== Animated sprite helpers ======
        private void LoadAnimatedSprites()
        {
            // 预加载 GIF 拆帧纹理（命名示例： Content/Gif/Anim_0.png ... Anim_5.png）
            Texture2D[] frames = new Texture2D[GifFrameCount];
            for (int i = 0; i < GifFrameCount; i++)
            {
                frames[i] = game.Content.Load<Texture2D>($"img/BackGround/backanimation/backanimation_f{i:000}");
            }

            // 创建 10 个动画精灵，随机位置与速度，前 4 个会追随鼠标
            for (int i = 0; i < TotalSprites; i++)
            {
                Vector2 pos = new(rand.Next(screenBounds.Width), rand.Next(screenBounds.Height));
                Vector2 vel = new(rand.Next(-100, 101), rand.Next(-100, 101));
                bool follow = i < FollowMouseCount;

                animatedSprites.Add(new AnimatedSprite(frames, pos, vel, follow));
            }
        }
        #endregion

        #region ====== 布局辅助 ======
        private void CalculateMenuButtonPositions()
        {
            menuButtonRects = new Rectangle[menuItems.Length];

            // 基准尺寸（相对于 1920×1080）
            Vector2 baseButtonSize = UIScaleManager.GetRelativeSize(0.15625f, 0.0556f); // 300/1920, 60/1080
            Vector2 buttonSize = baseButtonSize * MenuScaleMultiplier;

            float baseSpacing = UIScaleManager.CurrentHeight * 0.0185f; // 20/1080
            float buttonSpacing = baseSpacing * MenuScaleMultiplier;

            Vector2 startPos = UIScaleManager.GetRelativePosition(0.5f, 0.45f);

            for (int i = 0; i < menuItems.Length; i++)
            {
                menuButtonRects[i] = new Rectangle(
                    (int)(startPos.X - buttonSize.X / 2),
                    (int)(startPos.Y + i * (buttonSize.Y + buttonSpacing)),
                    (int)buttonSize.X,
                    (int)buttonSize.Y);
            }
        }
        #endregion

        #region ====== Update / Input ======
        public override void Update(GameTime gameTime)
        {
            previousMouseState = currentMouseState;
            currentMouseState = Mouse.GetState();

            // 更新动画精灵
            Vector2 mousePos = currentMouseState.Position.ToVector2();
            foreach (var sprite in animatedSprites)
                sprite.Update(gameTime, mousePos, screenBounds);

            // 检查鼠标悬停
            selectedMenuItem = -1;
            for (int i = 0; i < menuButtonRects.Length; i++)
            {
                if (menuButtonRects[i].Contains(currentMouseState.Position))
                {
                    selectedMenuItem = i;
                    break;
                }
            }

            // 检查鼠标点击
            if (currentMouseState.LeftButton == ButtonState.Pressed &&
                previousMouseState.LeftButton == ButtonState.Released &&
                selectedMenuItem >= 0)
            {
                HandleMenuSelection(selectedMenuItem);
            }
        }

        private void HandleMenuSelection(int menuIndex)
        {
            switch (menuIndex)
            {
                case 0:
                    game.ChangeState(new RoomSelectionState(game, graphics, spriteBatch, font, backgroundTexture, secondBackgroundTexture, roomBackgroundTexture));
                    break;
                case 1:
                    game.ChangeState(new SettingsState(game, graphics, spriteBatch, font, backgroundTexture, secondBackgroundTexture));
                    break;
                case 2:
                    game.Exit();
                    break;
            }
        }
        #endregion

        #region ====== Draw ======
        public override void Draw(GameTime gameTime)
        {
            // 背景
            spriteBatch.Draw(backgroundTexture, screenBounds, Color.White);

            // 动画精灵（在 UI 之下）
            foreach (var sprite in animatedSprites)
                sprite.Draw(spriteBatch);

            DrawTitle();
            DrawMenuButtons();
        }

        private void DrawTitle()
        {
            const string title = "耄耋的家园";

            Vector2 titleSize = font.MeasureString(title);
            float baseScale = UIScaleManager.UniformScale;
            float finalScale = baseScale * TitleScaleMultiplier;

            Vector2 titlePos = UIScaleManager.GetRelativePosition(0.5f, 0.25f);
            titlePos.X -= titleSize.X * finalScale / 2f;
            titlePos.Y -= titleSize.Y * finalScale / 2f;

            float padding = UIScaleManager.CurrentWidth * 0.02f;
            Rectangle titleBg = new Rectangle(
                (int)(titlePos.X - padding),
                (int)(titlePos.Y - padding * 0.5f),
                (int)(titleSize.X * finalScale + padding * 2f),
                (int)(titleSize.Y * finalScale + padding));

            using var pixel = CreatePixelTexture();
            spriteBatch.Draw(pixel, titleBg, Color.Black * 0.7f);

            int borderThickness = Math.Max(1, (int)(3 * UIScaleManager.UniformScale));
            DrawRectangleBorder(pixel, titleBg, Color.Gold, borderThickness);

            spriteBatch.DrawString(font, title, titlePos, Color.Gold, 0f, Vector2.Zero, finalScale, SpriteEffects.None, 0f);
        }

        private void DrawMenuButtons()
        {
            using var pixel = CreatePixelTexture();
            float scaledFontSize = UIScaleManager.UniformScale * MenuScaleMultiplier; // 放大菜单字体

            for (int i = 0; i < menuItems.Length; i++)
            {
                bool isSelected = selectedMenuItem == i;
                Color buttonClr = isSelected ? HoverColor : Color.White;
                Color borderClr = isSelected ? HoverColor : Color.Gray;

                // 背景（带透明度）
                spriteBatch.Draw(pixel, menuButtonRects[i], buttonClr * 0.3f);

                int borderThickness = Math.Max(1, (int)(2 * UIScaleManager.UniformScale));
                DrawRectangleBorder(pixel, menuButtonRects[i], borderClr, borderThickness);

                Vector2 textSize = font.MeasureString(menuItems[i]);
                Vector2 textPos = new(
                    menuButtonRects[i].X + menuButtonRects[i].Width / 2f - textSize.X * scaledFontSize / 2f,
                    menuButtonRects[i].Y + menuButtonRects[i].Height / 2f - textSize.Y * scaledFontSize / 2f);

                // 菜单文字统一黑色
                spriteBatch.DrawString(font, menuItems[i], textPos, Color.Black, 0f, Vector2.Zero, scaledFontSize, SpriteEffects.None, 0f);
            }
        }
        #endregion

        #region Helpers
        private Texture2D CreatePixelTexture()
        {
            Texture2D pixel = new(graphics.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            return pixel;
        }

        private void DrawRectangleBorder(Texture2D pixel, Rectangle rect, Color color, int thickness)
        {
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);                                  // 上
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);        // 下
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);                                  // 左
            spriteBatch.Draw(pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);        // 右
        }
        #endregion
    }
}
