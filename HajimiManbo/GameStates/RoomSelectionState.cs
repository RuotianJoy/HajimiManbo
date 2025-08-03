using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace HajimiManbo.GameStates
{
    /// <summary>
    /// 房间选择界面，用户可以选择创建房间或加入房间
    /// </summary>
    public class RoomSelectionState : GameState
    {
        #region ====== 房间选择配置 ======
        private readonly string[] menuItems = { "创建房间", "加入房间" };
        private int selectedMenuItem = 0;

        // Input
        private MouseState previousMouseState;
        private MouseState currentMouseState;
        private KeyboardState previousKeyboardState;

        // Layout
        private Rectangle[] menuButtonRects;
        public readonly Texture2D backgroundTexture;
        public readonly Texture2D secondBackgroundTexture;
        public readonly Texture2D roomBackgroundTexture;

        // Scaling factors
        private const float TitleScaleMultiplier = 3f; // 标题放大倍数
        private const float MenuScaleMultiplier = 2f; // 菜单字体/按钮放大倍数

        // Colors
        private static readonly Color HoverColor = Color.LightGreen;
        private static readonly Color ButtonColor = Color.White;
        private static readonly Color BorderColor = Color.Gray;

        private readonly Rectangle screenBounds;
        private float stateStartTime;
        private const float INPUT_DELAY = 0.2f; // 状态切换后的输入延迟（秒）
        #endregion

        public RoomSelectionState(Game1 game,
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
            this.roomBackgroundTexture = roomBackgroundTexture;
            screenBounds = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            
            // 初始化鼠标状态以避免状态切换时的意外点击
            currentMouseState = Mouse.GetState();
            previousMouseState = currentMouseState;
            
            // 初始化键盘状态以避免状态切换时的意外按键
            previousKeyboardState = Keyboard.GetState();
            
            stateStartTime = 0f;

            CalculateMenuButtonPositions();
        }

        #region ====== 布局辅助 ======
        private void CalculateMenuButtonPositions()
        {
            menuButtonRects = new Rectangle[menuItems.Length];

            // 基准尺寸（相对于 1920×1080）
            Vector2 baseButtonSize = UIScaleManager.GetRelativeSize(0.20f, 0.08f); // 更大的按钮
            Vector2 buttonSize = baseButtonSize * MenuScaleMultiplier;

            float baseSpacing = UIScaleManager.CurrentHeight * 0.001f; // 减少间距
            float buttonSpacing = baseSpacing * MenuScaleMultiplier;

            Vector2 startPos = UIScaleManager.GetRelativePosition(0.5f, 0.35f);

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
            stateStartTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            previousMouseState = currentMouseState;
            currentMouseState = Mouse.GetState();

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

            // 检查鼠标点击（添加输入延迟以防止状态切换时的意外点击）
            if (stateStartTime > INPUT_DELAY &&
                currentMouseState.LeftButton == ButtonState.Pressed &&
                previousMouseState.LeftButton == ButtonState.Released &&
                selectedMenuItem >= 0)
            {
                HandleMenuSelection(selectedMenuItem);
            }

            // 键盘导航
            KeyboardState keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Escape) && !previousKeyboardState.IsKeyDown(Keys.Escape))
            {
                // 返回主菜单
                game.ChangeState(new MainMenuState(game, graphics, spriteBatch, font, backgroundTexture, secondBackgroundTexture, roomBackgroundTexture));
            }
            previousKeyboardState = keyboardState;
        }

        private void HandleMenuSelection(int menuIndex)
        {
            switch (menuIndex)
            {
                case 0: // 创建房间
                    HajimiManbo.Network.NetworkManager.Instance.StartServer();
                    game.ChangeState(new WaitingRoomState(game, graphics, spriteBatch, font, backgroundTexture, secondBackgroundTexture, roomBackgroundTexture));
                    break;
                case 1: // 加入房间
                    game.ChangeState(new JoinRoomState(game, graphics, spriteBatch, font, backgroundTexture, secondBackgroundTexture, roomBackgroundTexture));
                    break;
            }
        }
        #endregion

        #region ====== Draw ======
        public override void Draw(GameTime gameTime)
        {
            // 背景
            spriteBatch.Draw(backgroundTexture, screenBounds, Color.White);

            DrawTitle();
            DrawMenuButtons();
            DrawInstructions();
        }

        private void DrawTitle()
        {
            const string title = "房间选择";

            Vector2 titleSize = font.MeasureString(title);
            float baseScale = UIScaleManager.UniformScale;
            float finalScale = baseScale * TitleScaleMultiplier;

            Vector2 titlePos = UIScaleManager.GetRelativePosition(0.5f, 0.2f);
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
            float scaledFontSize = UIScaleManager.UniformScale * MenuScaleMultiplier;

            for (int i = 0; i < menuItems.Length; i++)
            {
                bool isSelected = selectedMenuItem == i;
                Color buttonClr = isSelected ? HoverColor : ButtonColor;
                Color borderClr = isSelected ? HoverColor : BorderColor;
                Color textColor = isSelected ? Color.Black : Color.Black;

                // 背景（带透明度）
                spriteBatch.Draw(pixel, menuButtonRects[i], buttonClr * 0.8f);

                int borderThickness = Math.Max(1, (int)(3 * UIScaleManager.UniformScale));
                DrawRectangleBorder(pixel, menuButtonRects[i], borderClr, borderThickness);

                Vector2 textSize = font.MeasureString(menuItems[i]);
                Vector2 textPos = new(
                    menuButtonRects[i].X + menuButtonRects[i].Width / 2f - textSize.X * scaledFontSize / 2f,
                    menuButtonRects[i].Y + menuButtonRects[i].Height / 2f - textSize.Y * scaledFontSize / 2f);

                spriteBatch.DrawString(font, menuItems[i], textPos, textColor, 0f, Vector2.Zero, scaledFontSize, SpriteEffects.None, 0f);
            }
        }

        private void DrawInstructions()
        {
            const string instruction = "选择游戏模式 - 按ESC返回主菜单";
            
            Vector2 instructionSize = font.MeasureString(instruction);
            float instructionScale = UIScaleManager.UniformScale * 1.2f;
            
            Vector2 instructionPos = UIScaleManager.GetRelativePosition(0.5f, 0.85f);
            instructionPos.X -= instructionSize.X * instructionScale / 2f;
            instructionPos.Y -= instructionSize.Y * instructionScale / 2f;

            spriteBatch.DrawString(font, instruction, instructionPos, Color.White, 0f, Vector2.Zero, instructionScale, SpriteEffects.None, 0f);
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