using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using HajimiManbo.Network;
using System;
using System.Collections.Generic;

namespace HajimiManbo.GameStates
{
    /// <summary>
    /// 地图生成选项界面：房主可以设置地图大小、怪物难度、怪物数量等参数
    /// 其他玩家可以查看但不能修改
    /// </summary>
    public class MapGenerationState : GameState
    {
        private readonly Texture2D backgroundTexture;
        private readonly Texture2D secondBackgroundTexture;
        private readonly Texture2D roomBackgroundTexture;
        private readonly Rectangle screenBounds;
        private readonly ContentManager contentManager;
        
        private const float TitleScaleMultiplier = 3f;
        private const float OptionScaleMultiplier = 1.8f;
        private KeyboardState prevKeyboardState;
        private MouseState prevMouseState;
        
        // UI元素
        private Texture2D pixelTexture;
        
        // 地图设置选项
        private int mapSize = 1; // 0=小, 1=中, 2=大
        private int monsterDifficulty = 1; // 0=简单, 1=普通, 2=困难
        private int monsterCount = 1; // 0=少, 1=中等, 2=多
        
        // 按钮区域
        private Rectangle mapSizeLeftButton, mapSizeRightButton;
        private Rectangle difficultyLeftButton, difficultyRightButton;
        private Rectangle countLeftButton, countRightButton;
        private Rectangle startGameButton;
        private Rectangle backButton;
        
        // 选项文本
        private readonly string[] mapSizeOptions = { "小", "中", "大" };
        private readonly string[] difficultyOptions = { "简单", "普通", "困难" };
        private readonly string[] countOptions = { "少", "中等", "多" };
        
        public MapGenerationState(Game1 game, GraphicsDeviceManager graphics,
                                SpriteBatch spriteBatch, SpriteFont font,
                                Texture2D backgroundTexture, Texture2D secondBackgroundTexture,
                                Texture2D roomBackgroundTexture)
            : base(game, graphics, spriteBatch, font)
        {
            this.backgroundTexture = backgroundTexture;
            this.secondBackgroundTexture = secondBackgroundTexture;
            this.roomBackgroundTexture = roomBackgroundTexture;
            this.contentManager = game.Content;
            
            screenBounds = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            
            // 创建1x1像素纹理
            pixelTexture = new Texture2D(graphics.GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
            
            // 如果是客户端，从网络获取当前地图设置
            if (NetworkManager.Instance.IsClient && !NetworkManager.Instance.IsServer)
            {
                var settings = NetworkManager.Instance.GetMapSettings();
                mapSize = settings.mapSize;
                monsterDifficulty = settings.monsterDifficulty;
                monsterCount = settings.monsterCount;
            }
            
            CalculateButtonRects();
            
            // 初始化鼠标和键盘状态
            prevMouseState = Mouse.GetState();
            prevKeyboardState = Keyboard.GetState();
        }
        
        public override void Update(GameTime gameTime)
        {
            KeyboardState kb = Keyboard.GetState();
            MouseState mouse = Mouse.GetState();
            
            // ESC键返回等待房间（只有房主可以操作）
            if (kb.IsKeyDown(Keys.Escape) && !prevKeyboardState.IsKeyDown(Keys.Escape))
            {
                if (NetworkManager.Instance.IsServer)
                {
                    // 房主按ESC，广播返回消息并自己也返回
                    NetworkManager.Instance.BroadcastReturnToWaitingRoom();
                    game.ReturnToWaitingRoom();
                }
                return;
            }
            
            // 客户端实时同步地图设置
            if (NetworkManager.Instance.IsClient && !NetworkManager.Instance.IsServer)
            {
                var settings = NetworkManager.Instance.GetMapSettings();
                mapSize = settings.mapSize;
                monsterDifficulty = settings.monsterDifficulty;
                monsterCount = settings.monsterCount;
            }
            
            // 只有房主可以修改设置
            if (NetworkManager.Instance.IsServer)
            {
                HandleServerInput(mouse);
            }
            
            prevKeyboardState = kb;
            prevMouseState = mouse;
        }
        
        private void HandleServerInput(MouseState mouse)
        {
            if (mouse.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released)
            {
                // 地图大小按钮
                if (mapSizeLeftButton.Contains(mouse.Position))
                {
                    mapSize = Math.Max(0, mapSize - 1);
                    SyncSettingsToClients();
                }
                else if (mapSizeRightButton.Contains(mouse.Position))
                {
                    mapSize = Math.Min(2, mapSize + 1);
                    SyncSettingsToClients();
                }
                // 怪物难度按钮
                else if (difficultyLeftButton.Contains(mouse.Position))
                {
                    monsterDifficulty = Math.Max(0, monsterDifficulty - 1);
                    SyncSettingsToClients();
                }
                else if (difficultyRightButton.Contains(mouse.Position))
                {
                    monsterDifficulty = Math.Min(2, monsterDifficulty + 1);
                    SyncSettingsToClients();
                }
                // 怪物数量按钮
                else if (countLeftButton.Contains(mouse.Position))
                {
                    monsterCount = Math.Max(0, monsterCount - 1);
                    SyncSettingsToClients();
                }
                else if (countRightButton.Contains(mouse.Position))
                {
                    monsterCount = Math.Min(2, monsterCount + 1);
                    SyncSettingsToClients();
                }
                // 开始游戏按钮
                else if (startGameButton.Contains(mouse.Position))
                {
                    StartGame();
                }
                // 返回按钮
                else if (backButton.Contains(mouse.Position))
                {
                    // 房主点击返回，广播返回消息并自己也返回
                    NetworkManager.Instance.BroadcastReturnToWaitingRoom();
                    game.ReturnToWaitingRoom();
                }
            }
        }
        
        private void SyncSettingsToClients()
        {
            // 实现设置同步到客户端的网络逻辑
            NetworkManager.Instance.BroadcastMapSettings(mapSize, monsterDifficulty, monsterCount);
            Console.WriteLine($"[MapGeneration] Settings updated: Size={mapSizeOptions[mapSize]}, Difficulty={difficultyOptions[monsterDifficulty]}, Count={countOptions[monsterCount]}");
        }
        
        private void StartGame()
        {
            Console.WriteLine($"[MapGeneration] Starting game with settings: Size={mapSizeOptions[mapSize]}, Difficulty={difficultyOptions[monsterDifficulty]}, Count={countOptions[monsterCount]}");
            
            // 创建世界设置
            var worldSettings = new HajimiManbo.World.WorldSettings
            {
                MapSize = mapSize,
                MonsterDifficulty = monsterDifficulty,
                MonsterCount = monsterCount
            };
            
            // 生成随机种子
            int seed = new Random().Next();
            
            // 广播开始游戏消息给所有客户端
            if (NetworkManager.Instance.IsServer)
            {
                NetworkManager.Instance.BroadcastStartGame(seed, worldSettings);
            }
            
            // 切换到游戏状态
            game.SwitchToGamePlay(seed, worldSettings);
        }
        
        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Draw(roomBackgroundTexture, screenBounds, Color.White);
            
            DrawTitle();
            DrawMapSizeOption();
            DrawDifficultyOption();
            DrawCountOption();
            DrawButtons();
            DrawHint();
        }
        
        private void DrawTitle()
        {
            string title = NetworkManager.Instance.IsServer ? "地图生成设置" : "等待房主设置地图";
            Vector2 size = font.MeasureString(title);
            float scale = UIScaleManager.UniformScale * TitleScaleMultiplier;
            Vector2 pos = UIScaleManager.GetRelativePosition(0.5f, 0.15f);
            pos.X -= size.X * scale / 2f;
            pos.Y -= size.Y * scale / 2f;
            spriteBatch.DrawString(font, title, pos, Color.Gold, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        
        private void DrawMapSizeOption()
        {
            float scale = UIScaleManager.UniformScale * OptionScaleMultiplier;
            Vector2 pos = UIScaleManager.GetRelativePosition(0.5f, 0.35f);
            
            // 标题
            string label = "地图大小:";
            Vector2 labelSize = font.MeasureString(label);
            Vector2 labelPos = new Vector2(pos.X - labelSize.X * scale - 80 * UIScaleManager.UniformScale, pos.Y - labelSize.Y * scale / 2f);
            spriteBatch.DrawString(font, label, labelPos, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            
            // 左箭头按钮
            if (NetworkManager.Instance.IsServer)
            {
                DrawArrowButton(mapSizeLeftButton, "<", mapSize > 0);
            }
            
            // 当前选项
            string currentOption = mapSizeOptions[mapSize];
            Vector2 optionSize = font.MeasureString(currentOption);
            Vector2 optionPos = new Vector2(pos.X - optionSize.X * scale / 2f + 250 * UIScaleManager.UniformScale, pos.Y - optionSize.Y * scale / 2f);
            spriteBatch.DrawString(font, currentOption, optionPos, Color.LightGreen, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            
            // 右箭头按钮
            if (NetworkManager.Instance.IsServer)
            {
                DrawArrowButton(mapSizeRightButton, ">", mapSize < 2);
            }
        }
        
        private void DrawDifficultyOption()
        {
            float scale = UIScaleManager.UniformScale * OptionScaleMultiplier;
            Vector2 pos = UIScaleManager.GetRelativePosition(0.5f, 0.5f);
            
            // 标题
            string label = "怪物难度:";
            Vector2 labelSize = font.MeasureString(label);
            Vector2 labelPos = new Vector2(pos.X - labelSize.X * scale - 80 * UIScaleManager.UniformScale, pos.Y - labelSize.Y * scale / 2f);
            spriteBatch.DrawString(font, label, labelPos, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            
            // 左箭头按钮
            if (NetworkManager.Instance.IsServer)
            {
                DrawArrowButton(difficultyLeftButton, "<", monsterDifficulty > 0);
            }
            
            // 当前选项
            string currentOption = difficultyOptions[monsterDifficulty];
            Vector2 optionSize = font.MeasureString(currentOption);
            Vector2 optionPos = new Vector2(pos.X - optionSize.X * scale / 2f + 250 * UIScaleManager.UniformScale, pos.Y - optionSize.Y * scale / 2f);
            Color optionColor = monsterDifficulty == 0 ? Color.LightGreen : monsterDifficulty == 1 ? Color.Yellow : Color.Red;
            spriteBatch.DrawString(font, currentOption, optionPos, optionColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            
            // 右箭头按钮
            if (NetworkManager.Instance.IsServer)
            {
                DrawArrowButton(difficultyRightButton, ">", monsterDifficulty < 2);
            }
        }
        
        private void DrawCountOption()
        {
            float scale = UIScaleManager.UniformScale * OptionScaleMultiplier;
            Vector2 pos = UIScaleManager.GetRelativePosition(0.5f, 0.65f);
            
            // 标题
            string label = "怪物数量:";
            Vector2 labelSize = font.MeasureString(label);
            Vector2 labelPos = new Vector2(pos.X - labelSize.X * scale - 80 * UIScaleManager.UniformScale, pos.Y - labelSize.Y * scale / 2f);
            spriteBatch.DrawString(font, label, labelPos, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            
            // 左箭头按钮
            if (NetworkManager.Instance.IsServer)
            {
                DrawArrowButton(countLeftButton, "<", monsterCount > 0);
            }
            
            // 当前选项
            string currentOption = countOptions[monsterCount];
            Vector2 optionSize = font.MeasureString(currentOption);
            Vector2 optionPos = new Vector2(pos.X - optionSize.X * scale / 2f + 250 * UIScaleManager.UniformScale, pos.Y - optionSize.Y * scale / 2f);
            spriteBatch.DrawString(font, currentOption, optionPos, Color.LightBlue, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            
            // 右箭头按钮
            if (NetworkManager.Instance.IsServer)
            {
                DrawArrowButton(countRightButton, ">", monsterCount < 2);
            }
        }
        
        private void DrawArrowButton(Rectangle buttonRect, string text, bool enabled)
        {
            MouseState mouse = Mouse.GetState();
            bool isHovered = buttonRect.Contains(mouse.Position) && enabled;
            
            // 绘制按钮背景
            // Color bgColor = enabled ? (isHovered ? Color.LimeGreen * 0.8f : Color.Green * 0.7f) : Color.Gray * 0.5f;
            // spriteBatch.Draw(pixelTexture, buttonRect, bgColor);
            
            // // 绘制按钮边框
            // Color borderColor = enabled ? (isHovered ? Color.White : Color.LimeGreen) : Color.DarkGray;
            // DrawRectangleBorder(buttonRect, borderColor, 2);
            
            // 绘制按钮文本
            float scale = UIScaleManager.UniformScale * 1.5f;
            Vector2 textSize = font.MeasureString(text);
            Vector2 textPos = new Vector2(
                buttonRect.X + (buttonRect.Width - textSize.X * scale) / 2 ,
                buttonRect.Y + (buttonRect.Height - textSize.Y * scale) / 2
            );
            
            Color textColor = enabled ? (isHovered ? Color.Black : Color.White) : Color.DarkGray;
            spriteBatch.DrawString(font, text, textPos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        
        private void DrawButtons()
        {
            if (NetworkManager.Instance.IsServer)
            {
                // 开始游戏按钮
                DrawMainButton(startGameButton, "开始游戏", Color.Green);
                
                // 返回按钮（只有房主可见）
                DrawMainButton(backButton, "返回", Color.Orange);
            }
        }
        
        private void DrawMainButton(Rectangle buttonRect, string text, Color baseColor)
        {
            MouseState mouse = Mouse.GetState();
            bool isHovered = buttonRect.Contains(mouse.Position);
            
            // 绘制按钮背景
            Color bgColor = isHovered ? baseColor * 0.8f : baseColor * 0.7f;
            spriteBatch.Draw(pixelTexture, buttonRect, bgColor);
            
            // 绘制按钮边框
            Color borderColor = isHovered ? Color.White : Color.LightGray;
            DrawRectangleBorder(buttonRect, borderColor, 2);
            
            // 绘制按钮文本
            float scale = UIScaleManager.UniformScale * 1.5f;
            Vector2 textSize = font.MeasureString(text);
            Vector2 textPos = new Vector2(
                buttonRect.X + (buttonRect.Width - textSize.X * scale) / 2,
                buttonRect.Y + (buttonRect.Height - textSize.Y * scale) / 2
            );
            
            Color textColor = isHovered ? Color.Black : Color.White;
            spriteBatch.DrawString(font, text, textPos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        
        private void DrawRectangleBorder(Rectangle rect, Color color, int thickness)
        {
            // 上边
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // 下边
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            // 左边
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // 右边
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
        
        private void DrawHint()
        {
            string hint = NetworkManager.Instance.IsServer ? "设置完成后点击开始游戏" : "等待房主设置完成";
            float scale = UIScaleManager.UniformScale * 1.2f;
            Vector2 size = font.MeasureString(hint);
            Vector2 pos = UIScaleManager.GetRelativePosition(0.5f, 0.9f);
            pos.X -= size.X * scale / 2f;
            pos.Y -= size.Y * scale / 2f;
            spriteBatch.DrawString(font, hint, pos, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        
        private void CalculateButtonRects()
        {
            Vector2 buttonSize = UIScaleManager.GetRelativeSize(0.08f, 0.06f);
            Vector2 mainButtonSize = UIScaleManager.GetRelativeSize(0.2f, 0.08f);
            
            // 箭头按钮 - 调整位置以匹配选项值的新位置（向右移动250像素）
            Vector2 mapSizePos = UIScaleManager.GetRelativePosition(0.5f, 0.35f);
            mapSizeLeftButton = new Rectangle(
                (int)(mapSizePos.X + 250 * UIScaleManager.UniformScale - 130 * UIScaleManager.UniformScale - buttonSize.X / 2),
                (int)(mapSizePos.Y - buttonSize.Y / 2),
                (int)buttonSize.X, (int)buttonSize.Y
            );
            mapSizeRightButton = new Rectangle(
                (int)(mapSizePos.X + 250 * UIScaleManager.UniformScale + 130 * UIScaleManager.UniformScale - buttonSize.X / 2),
                (int)(mapSizePos.Y - buttonSize.Y / 2),
                (int)buttonSize.X, (int)buttonSize.Y
            );
            
            Vector2 difficultyPos = UIScaleManager.GetRelativePosition(0.5f, 0.5f);
            difficultyLeftButton = new Rectangle(
                (int)(difficultyPos.X + 250 * UIScaleManager.UniformScale - 130 * UIScaleManager.UniformScale - buttonSize.X / 2),
                (int)(difficultyPos.Y - buttonSize.Y / 2),
                (int)buttonSize.X, (int)buttonSize.Y
            );
            difficultyRightButton = new Rectangle(
                (int)(difficultyPos.X + 250 * UIScaleManager.UniformScale + 130 * UIScaleManager.UniformScale - buttonSize.X / 2),
                (int)(difficultyPos.Y - buttonSize.Y / 2),
                (int)buttonSize.X, (int)buttonSize.Y
            );
            
            Vector2 countPos = UIScaleManager.GetRelativePosition(0.5f, 0.65f);
            countLeftButton = new Rectangle(
                (int)(countPos.X + 250 * UIScaleManager.UniformScale - 130 * UIScaleManager.UniformScale - buttonSize.X / 2),
                (int)(countPos.Y - buttonSize.Y / 2),
                (int)buttonSize.X, (int)buttonSize.Y
            );
            countRightButton = new Rectangle(
                (int)(countPos.X + 250 * UIScaleManager.UniformScale + 130 * UIScaleManager.UniformScale - buttonSize.X / 2),
                (int)(countPos.Y - buttonSize.Y / 2),
                (int)buttonSize.X, (int)buttonSize.Y
            );
            
            // 主要按钮
            Vector2 startGamePos = UIScaleManager.GetRelativePosition(0.35f, 0.8f);
            startGameButton = new Rectangle(
                (int)(startGamePos.X - mainButtonSize.X / 2),
                (int)(startGamePos.Y - mainButtonSize.Y / 2),
                (int)mainButtonSize.X, (int)mainButtonSize.Y
            );
            
            Vector2 backPos = UIScaleManager.GetRelativePosition(0.65f, 0.8f);
            backButton = new Rectangle(
                (int)(backPos.X - mainButtonSize.X / 2),
                (int)(backPos.Y - mainButtonSize.Y / 2),
                (int)mainButtonSize.X, (int)mainButtonSize.Y
            );
        }
        
        public override void OnResolutionChanged()
        {
            CalculateButtonRects();
        }
    }
}