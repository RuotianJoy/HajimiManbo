using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using HajimiManbo.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HajimiManbo.GameStates
{
    /// <summary>
    /// 等待房间界面：显示玩家列表，主机等待客户端连接，客户端等待主机开始。
    /// </summary>
    public class WaitingRoomState : GameState
    {
        private readonly Texture2D backgroundTexture;
        private readonly Texture2D secondBackgroundTexture;
        private readonly Texture2D roomBackgroundTexture;
        private readonly Rectangle screenBounds;
        private readonly ContentManager contentManager;

        private const float TitleScaleMultiplier = 3f;
        private const float ListScaleMultiplier = 1.8f;
        private KeyboardState prevKeyboardState;
        
        // 名字输入相关
        private string playerName;
        private bool isEditingName = false;
        private Rectangle nameInputRect;
        private Texture2D pixelTexture;
        
        // 选择角色按钮
        private Rectangle characterSelectButtonRect;
        private MouseState prevMouseState;
        
        // 开始游戏按钮（仅房主可见）
        private Rectangle startGameButtonRect;
        
        // 角色动画相关
        private Dictionary<string, Texture2D[]> characterAnimationFrames = new Dictionary<string, Texture2D[]>();
        private Dictionary<string, CharacterData> characterDataCache = new Dictionary<string, CharacterData>();
        private float animationTimer = 0f;
        private const float ANIMATION_FRAME_TIME = 0.1f;
        
        // 玩家列表滚动相关
        private Rectangle playerListRect;
        private float scrollOffset = 0f;
        private const float SCROLL_SPEED = 30f;
        private const int MAX_VISIBLE_PLAYERS = 2;

        public WaitingRoomState(Game1 game,
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
            this.contentManager = game.Content;
            screenBounds = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            
            // 初始化键盘状态以避免状态切换时的意外按键
            prevKeyboardState = Keyboard.GetState();
            prevMouseState = Mouse.GetState();
            
            // 初始化玩家名字
            playerName = NetworkManager.Instance.GetLocalPlayerName();
            
            // 创建像素纹理用于绘制输入框
            pixelTexture = new Texture2D(graphics.GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
            
            // 加载角色数据
            LoadCharacterData();
            
            // 计算UI布局
            CalculateNameInputRect();
            CalculatePlayerListRect();
            CalculateCharacterSelectButtonRect();
            CalculateStartGameButtonRect();
        }

        public override void Update(GameTime gameTime)
        {
            // 更新动画计时器
            animationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // 检查连接状态 - 如果是客户端且连接断开，返回房间选择界面
            if (!NetworkManager.Instance.IsServer && !NetworkManager.Instance.IsClientConnected)
            {
                Console.WriteLine("[WaitingRoom] Connection lost, returning to room selection");
                game.ReturnToSelectRoom();
                return;
            }
            
            KeyboardState kb = Keyboard.GetState();
            MouseState mouse = Mouse.GetState();
            
            // 处理玩家列表滚动
            HandlePlayerListScroll(mouse);
            
            // 检查是否点击名字输入框
            if (mouse.LeftButton == ButtonState.Pressed && nameInputRect.Contains(mouse.Position))
            {
                isEditingName = true;
            }
            else if (mouse.LeftButton == ButtonState.Pressed && !nameInputRect.Contains(mouse.Position))
            {
                if (isEditingName)
                {
                    // 同步名字到NetworkManager
                    NetworkManager.Instance.SetLocalPlayerName(playerName);
                }
                isEditingName = false;
            }
            
            // 处理名字输入
            if (isEditingName)
            {
                HandleNameInput(kb);
            }
            
            // ESC键退出房间
            if (kb.IsKeyDown(Keys.Escape) && !prevKeyboardState.IsKeyDown(Keys.Escape))
            {
                if (isEditingName)
                {
                    // 同步名字到NetworkManager
                    NetworkManager.Instance.SetLocalPlayerName(playerName);
                    isEditingName = false; // 如果正在编辑名字，先退出编辑模式
                }
                else
                {
                    // 退出房间返回主菜单
                    NetworkManager.Instance.Stop();
                    game.ReturnToSelectRoom();
                }
            }
            
            // 检查按钮点击
            if (mouse.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released)
            {
                if (characterSelectButtonRect.Contains(mouse.Position))
                {
                    // 切换到角色选择界面
                    game.SwitchToCharacterSelection();
                }
                else if (startGameButtonRect.Contains(mouse.Position) && NetworkManager.Instance.IsServer)
                {
                    // 检查是否所有玩家都选择了角色
                    if (AllPlayersHaveSelectedCharacters())
                    {
                        // 切换到地图生成设置界面
                        Console.WriteLine("[WaitingRoom] Switching to map generation settings...");
                        // 通知所有客户端切换到地图生成页面
                        NetworkManager.Instance.BroadcastSwitchToMapGeneration();
                        // 服务器自己也切换
                        game.SwitchToMapGeneration();
                    }
                }
            }
            
            prevKeyboardState = kb;
            prevMouseState = mouse;
        }

        public override void Draw(GameTime gameTime)
        {
            // 启用裁剪测试以支持滚动容器
            var originalRasterizerState = spriteBatch.GraphicsDevice.RasterizerState;
            var scissorRasterizerState = new RasterizerState
            {
                CullMode = originalRasterizerState.CullMode,
                FillMode = originalRasterizerState.FillMode,
                MultiSampleAntiAlias = originalRasterizerState.MultiSampleAntiAlias,
                ScissorTestEnable = true
            };
            spriteBatch.GraphicsDevice.RasterizerState = scissorRasterizerState;
            
            spriteBatch.Draw(roomBackgroundTexture, screenBounds, Color.White);
            DrawTitle();
            DrawNameInput();
            DrawPlayerList();
            DrawCharacterSelectButton();
            
            // 只有房主才显示开始游戏按钮
            if (NetworkManager.Instance.IsServer)
            {
                DrawStartGameButton();
            }
            
            DrawPing();
            DrawHint();
            
            // 恢复原始光栅化状态
            spriteBatch.GraphicsDevice.RasterizerState = originalRasterizerState;
        }

        private void DrawTitle()
        {
            string title = NetworkManager.Instance.IsServer ? "等待玩家加入" : "已连接，等待开始";
            Vector2 size = font.MeasureString(title);
            float scale = UIScaleManager.UniformScale * TitleScaleMultiplier;
            Vector2 pos = UIScaleManager.GetRelativePosition(0.5f, 0.1f);
            pos.X -= size.X * scale / 2f;
            pos.Y -= size.Y * scale / 2f;
            spriteBatch.DrawString(font, title, pos, Color.Gold, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private void DrawPlayerList()
        {
            var playerInfo = NetworkManager.Instance.GetPlayerInfo();
            float scale = UIScaleManager.UniformScale * ListScaleMultiplier;
            float lineSpacing = font.LineSpacing * scale * 1.2f;
            
            // 不绘制背景板，保持透明
            
            // 设置裁剪区域
            var originalScissorRect = spriteBatch.GraphicsDevice.ScissorRectangle;
            spriteBatch.GraphicsDevice.ScissorRectangle = playerListRect;
            
            Vector2 start = new Vector2(playerListRect.X + 20, playerListRect.Y + 20 - scrollOffset);
            
            for (int i = 0; i < playerInfo.Length; i++)
            {
                var (playerName, characterName) = playerInfo[i];
                float yPos = start.Y + i * lineSpacing;
                
                // 只绘制在可见区域内的玩家
                if (yPos >= playerListRect.Y - lineSpacing && yPos <= playerListRect.Bottom + lineSpacing)
                {
                    // 绘制角色动画（如果有选择角色）
                    if (!string.IsNullOrEmpty(characterName) && characterDataCache.ContainsKey(characterName))
                    {
                        DrawCharacterAnimation(characterName, start.X - 80 * UIScaleManager.UniformScale, yPos);
                    }
                    
                    // 构建显示文本
                    string displayText;
                    if (!string.IsNullOrEmpty(characterName))
                    {
                        displayText = $"[{characterName}] {playerName}";
                    }
                    else
                    {
                        displayText = playerName;
                    }
                    
                    Vector2 size = font.MeasureString(displayText);
                    Vector2 pos = new(start.X, yPos - size.Y * scale / 2f);
                    
                    // 根据是否选择角色使用不同颜色
                    Color textColor = !string.IsNullOrEmpty(characterName) ? Color.LightGreen : Color.White;
                    spriteBatch.DrawString(font, displayText, pos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }
            
            // 恢复裁剪区域
            spriteBatch.GraphicsDevice.ScissorRectangle = originalScissorRect;
            
            // 绘制滚动条（如果需要）
            if (playerInfo.Length > MAX_VISIBLE_PLAYERS)
            {
                DrawScrollbar(playerInfo.Length);
            }
        }

        private void DrawPing()
        {
            if (NetworkManager.Instance.IsClient)
            {
                string pingText = $"延迟: {NetworkManager.Instance.Ping:F0}ms";
                float scale = UIScaleManager.UniformScale * 1.2f;
                Vector2 size = font.MeasureString(pingText);
                Vector2 pos = UIScaleManager.GetRelativePosition(0.85f, 0.1f);
                pos.X -= size.X * scale / 2f;
                pos.Y -= size.Y * scale / 2f;
                
                Color pingColor = NetworkManager.Instance.Ping < 50 ? Color.Green :
                                 NetworkManager.Instance.Ping < 100 ? Color.Yellow : Color.Red;
                
                spriteBatch.DrawString(font, pingText, pos, pingColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }
        
        private void DrawHint()
        {
            string hint = isEditingName ? "输入名字后按ESC确认，或点击其他地方" : "按 ESC 退出房间";
            float scale = UIScaleManager.UniformScale * 1.2f;
            Vector2 size = font.MeasureString(hint);
            Vector2 pos = UIScaleManager.GetRelativePosition(0.5f, 0.9f);
            pos.X -= size.X * scale / 2f;
            pos.Y -= size.Y * scale / 2f;
            spriteBatch.DrawString(font, hint, pos, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        
        private void CalculateNameInputRect()
        {
            Vector2 pos = UIScaleManager.GetRelativePosition(0.5f, 0.3f);
            Vector2 size = UIScaleManager.GetRelativeSize(0.25f, 0.05f);
            nameInputRect = new Rectangle(
                (int)(pos.X - size.X / 2),
                (int)(pos.Y - size.Y / 2),
                (int)size.X,
                (int)size.Y
            );
        }
        
        private void DrawNameInput()
        {
            // 绘制输入框背景
            Color bgColor = isEditingName ? Color.LightBlue * 0.8f : Color.Gray * 0.6f;
            spriteBatch.Draw(pixelTexture, nameInputRect, bgColor);
            
            // 绘制边框
            Color borderColor = isEditingName ? Color.Blue : Color.White;
            DrawRectangleBorder(nameInputRect, borderColor, 2);
            
            // 绘制文本
            string displayText = isEditingName ? playerName + "_" : playerName;
            float scale = UIScaleManager.UniformScale * 1.5f;
            Vector2 textSize = font.MeasureString(displayText);
            Vector2 textPos = new Vector2(
                nameInputRect.X + (nameInputRect.Width - textSize.X * scale) / 2,
                nameInputRect.Y + (nameInputRect.Height - textSize.Y * scale) / 2
            );
            spriteBatch.DrawString(font, displayText, textPos, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            
            // 绘制标签
            string label = "玩家名字 (点击修改):";
            Vector2 labelSize = font.MeasureString(label);
            Vector2 labelPos = new Vector2(
                nameInputRect.X + (nameInputRect.Width - labelSize.X * scale) / 2,
                nameInputRect.Y - labelSize.Y * scale - 10
            );
            spriteBatch.DrawString(font, label, labelPos, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
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
        
        private void DrawScrollbar(int totalPlayers)
        {
            int scrollbarWidth = 10;
            int scrollbarX = screenBounds.Right - scrollbarWidth - 10; // 移到屏幕右边界
            int scrollbarY = playerListRect.Y + 5;
            int scrollbarHeight = playerListRect.Height - 10;
            
            // 绘制滚动条背景
            Rectangle scrollbarBg = new Rectangle(scrollbarX, scrollbarY, scrollbarWidth, scrollbarHeight);
            spriteBatch.Draw(pixelTexture, scrollbarBg, Color.Gray * 0.5f);
            
            // 计算滚动条滑块位置和大小
            float maxScroll = (totalPlayers - MAX_VISIBLE_PLAYERS) * (font.LineSpacing * UIScaleManager.UniformScale * ListScaleMultiplier * 1.2f);
            float scrollRatio = maxScroll > 0 ? scrollOffset / maxScroll : 0;
            float thumbHeight = Math.Max(20, scrollbarHeight * MAX_VISIBLE_PLAYERS / totalPlayers);
            float thumbY = scrollbarY + (scrollbarHeight - thumbHeight) * scrollRatio;
            
            // 绘制滚动条滑块
            Rectangle scrollbarThumb = new Rectangle(scrollbarX, (int)thumbY, scrollbarWidth, (int)thumbHeight);
            spriteBatch.Draw(pixelTexture, scrollbarThumb, Color.White * 0.8f);
        }
        
        private void HandleNameInput(KeyboardState currentKeyboard)
        {
            Keys[] pressedKeys = currentKeyboard.GetPressedKeys();
            
            foreach (Keys key in pressedKeys)
            {
                if (!prevKeyboardState.IsKeyDown(key)) // 只处理新按下的键
                {
                    if (key == Keys.Back && playerName.Length > 0)
                    {
                        playerName = playerName.Substring(0, playerName.Length - 1);
                    }
                    else if (key == Keys.Enter)
                    {
                        isEditingName = false;
                        // 同步名字到NetworkManager
                        NetworkManager.Instance.SetLocalPlayerName(playerName);
                    }
                    else if (playerName.Length < 12) // 限制名字长度
                    {
                        char? character = GetCharacterFromKey(key, currentKeyboard.IsKeyDown(Keys.LeftShift) || currentKeyboard.IsKeyDown(Keys.RightShift));
                        if (character.HasValue)
                        {
                            playerName += character.Value;
                        }
                    }
                }
            }
        }
        
        private char? GetCharacterFromKey(Keys key, bool shift)
        {
            // 处理字母
            if (key >= Keys.A && key <= Keys.Z)
            {
                char baseChar = (char)('a' + (key - Keys.A));
                return shift ? char.ToUpper(baseChar) : baseChar;
            }
            
            // 处理数字
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                if (shift)
                {
                    string symbols = ")!@#$%^&*(";
                    return symbols[key - Keys.D0];
                }
                return (char)('0' + (key - Keys.D0));
            }
            
            // 处理空格
            if (key == Keys.Space)
                return ' ';
                
            // 处理其他常用符号
            switch (key)
            {
                case Keys.OemMinus: return shift ? '_' : '-';
                case Keys.OemPeriod: return shift ? '>' : '.';
                case Keys.OemComma: return shift ? '<' : ',';
                default: return null;
            }
        }
        
        private void CalculateCharacterSelectButtonRect()
        {
            Vector2 pos = UIScaleManager.GetRelativePosition(0.65f, 0.8f); // 向右移动并居中
            Vector2 size = UIScaleManager.GetRelativeSize(0.2f, 0.06f);
            characterSelectButtonRect = new Rectangle(
                (int)(pos.X - size.X / 2),
                (int)(pos.Y - size.Y / 2),
                (int)size.X,
                (int)size.Y
            );
        }
        
        private void CalculateStartGameButtonRect()
        {
            Vector2 pos = UIScaleManager.GetRelativePosition(0.35f, 0.8f); // 向右移动并与选择角色按钮居中对齐
            Vector2 size = UIScaleManager.GetRelativeSize(0.2f, 0.06f);
            startGameButtonRect = new Rectangle(
                (int)(pos.X - size.X / 2),
                (int)(pos.Y - size.Y / 2),
                (int)size.X,
                (int)size.Y
            );
        }
        
        private void CalculatePlayerListRect()
        {
            int width = (int)(500 * UIScaleManager.UniformScale);
            int height = (int)(250 * UIScaleManager.UniformScale);
            Vector2 pos = UIScaleManager.GetRelativePosition(0.5f, 0.6f);
            playerListRect = new Rectangle((int)(pos.X - width / 2), (int)(pos.Y - height / 2), width, height);
        }
        
        private void HandlePlayerListScroll(MouseState mouse)
        {
            if (playerListRect.Contains(mouse.Position))
            {
                var playerInfo = NetworkManager.Instance.GetPlayerInfo();
                if (playerInfo.Length > MAX_VISIBLE_PLAYERS)
                {
                    int scrollDelta = mouse.ScrollWheelValue - prevMouseState.ScrollWheelValue;
                    if (scrollDelta != 0)
                    {
                        float lineSpacing = font.LineSpacing * UIScaleManager.UniformScale * ListScaleMultiplier * 1.2f;
                        float maxScroll = (playerInfo.Length - MAX_VISIBLE_PLAYERS) * lineSpacing;
                        
                        if (scrollDelta > 0)
                        {
                            // 向上滚动
                            scrollOffset = Math.Max(0, scrollOffset - SCROLL_SPEED);
                        }
                        else if (scrollDelta < 0)
                        {
                            // 向下滚动
                            scrollOffset = Math.Min(maxScroll, scrollOffset + SCROLL_SPEED);
                        }
                    }
                }
                else
                {
                    scrollOffset = 0; // 如果玩家数量不足，重置滚动偏移
                }
            }
        }
        
        private void DrawCharacterSelectButton()
        {
            MouseState mouse = Mouse.GetState();
            bool isHovered = characterSelectButtonRect.Contains(mouse.Position);
            
            // 绘制按钮背景
            Color bgColor = isHovered ? Color.Gold * 0.8f : Color.DarkBlue * 0.7f;
            spriteBatch.Draw(pixelTexture, characterSelectButtonRect, bgColor);
            
            // 绘制按钮边框
            Color borderColor = isHovered ? Color.White : Color.Gold;
            DrawRectangleBorder(characterSelectButtonRect, borderColor, 2);
            
            // 绘制按钮文本
            string buttonText = "选择角色";
            float scale = UIScaleManager.UniformScale * 1.5f;
            Vector2 textSize = font.MeasureString(buttonText);
            Vector2 textPos = new Vector2(
                characterSelectButtonRect.X + (characterSelectButtonRect.Width - textSize.X * scale) / 2,
                characterSelectButtonRect.Y + (characterSelectButtonRect.Height - textSize.Y * scale) / 2
            );
            
            Color textColor = isHovered ? Color.Black : Color.White;
            spriteBatch.DrawString(font, buttonText, textPos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        
        private void DrawStartGameButton()
        {
            MouseState mouse = Mouse.GetState();
            bool isHovered = startGameButtonRect.Contains(mouse.Position);
            bool canStart = AllPlayersHaveSelectedCharacters();
            
            // 绘制按钮背景
            Color bgColor;
            if (!canStart)
            {
                bgColor = Color.Gray * 0.5f; // 灰色表示不可点击
            }
            else if (isHovered)
            {
                bgColor = Color.LimeGreen * 0.8f;
            }
            else
            {
                bgColor = Color.Green * 0.7f;
            }
            
            spriteBatch.Draw(pixelTexture, startGameButtonRect, bgColor);
            
            // 绘制按钮边框
            Color borderColor = canStart ? (isHovered ? Color.White : Color.LimeGreen) : Color.DarkGray;
            DrawRectangleBorder(startGameButtonRect, borderColor, 2);
            
            // 绘制按钮文本
            string buttonText = "开始游戏";
            float scale = UIScaleManager.UniformScale * 1.5f;
            Vector2 textSize = font.MeasureString(buttonText);
            Vector2 textPos = new Vector2(
                startGameButtonRect.X + (startGameButtonRect.Width - textSize.X * scale) / 2,
                startGameButtonRect.Y + (startGameButtonRect.Height - textSize.Y * scale) / 2
            );
            
            Color textColor;
            if (!canStart)
            {
                textColor = Color.DarkGray;
            }
            else if (isHovered)
            {
                textColor = Color.Black;
            }
            else
            {
                textColor = Color.White;
            }
            
            spriteBatch.DrawString(font, buttonText, textPos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        
        /// <summary>
        /// 检查是否所有玩家都选择了角色
        /// </summary>
        private bool AllPlayersHaveSelectedCharacters()
        {
            var playerInfo = NetworkManager.Instance.GetPlayerInfo();
            
            // 如果没有玩家，返回false
            if (playerInfo.Length == 0)
                return false;
                
            // 检查每个玩家是否都选择了角色
            foreach (var (playerName, characterName) in playerInfo)
            {
                if (string.IsNullOrEmpty(characterName))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 加载角色数据
        /// </summary>
        private void LoadCharacterData()
        {
            try
            {
                string characterIndexPath = "Content/Character/characters.json";
                if (File.Exists(characterIndexPath))
                {
                    string indexJson = File.ReadAllText(characterIndexPath);
                    var characterIndex = JsonSerializer.Deserialize<CharacterIndex>(indexJson);
                    
                    if (characterIndex?.Characters != null)
                    {
                        foreach (var entry in characterIndex.Characters)
                        {
                            if (entry.Enabled)
                            {
                                LoadSingleCharacter(entry.File);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WaitingRoom] Failed to load character data: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 加载单个角色数据
        /// </summary>
        private void LoadSingleCharacter(string fileName)
        {
            try
            {
                string characterPath = $"Content/Character/{fileName}";
                if (File.Exists(characterPath))
                {
                    string characterJson = File.ReadAllText(characterPath);
                    var character = JsonSerializer.Deserialize<CharacterData>(characterJson);
                    
                    if (character != null && !string.IsNullOrEmpty(character.Name))
                    {
                        characterDataCache[character.Name] = character;
                        LoadCharacterAnimation(character);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WaitingRoom] Failed to load character {fileName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 加载角色动画帧
        /// </summary>
        private void LoadCharacterAnimation(CharacterData character)
        {
            try
            {
                if (string.IsNullOrEmpty(character.Animation.FolderName))
                    return;
                    
                string animationPath = $"img/Character/{character.Animation.FolderName}/{character.Animation.IdleAnimation}";
                List<Texture2D> frames = new List<Texture2D>();
                
                // 尝试加载动画帧（假设帧文件命名为 frame_0.png, frame_1.png 等）
                for (int i = 0; i < 10; i++) // 最多尝试10帧
                {
                    try
                    {
                        string framePath = $"{animationPath}/frame_{i}";
                        var frame = contentManager.Load<Texture2D>(framePath);
                        frames.Add(frame);
                    }
                    catch
                    {
                        // 如果加载失败，尝试其他命名方式
                        try
                        {
                            string framePath = $"{animationPath}/{i}";
                            var frame = contentManager.Load<Texture2D>(framePath);
                            frames.Add(frame);
                        }
                        catch
                        {
                            // 如果这帧不存在，停止加载
                            break;
                        }
                    }
                }
                
                // 如果没有找到帧，尝试加载单个图片
                if (frames.Count == 0)
                {
                    try
                    {
                        var singleFrame = contentManager.Load<Texture2D>(animationPath);
                        frames.Add(singleFrame);
                    }
                    catch
                    {
                        Console.WriteLine($"[WaitingRoom] Could not load animation for character {character.Name}");
                        return;
                    }
                }
                
                if (frames.Count > 0)
                {
                    characterAnimationFrames[character.Name] = frames.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WaitingRoom] Failed to load animation for {character.Name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 绘制角色动画
        /// </summary>
        private void DrawCharacterAnimation(string characterName, float x, float y)
        {
            if (!characterAnimationFrames.ContainsKey(characterName))
                return;
                
            var frames = characterAnimationFrames[characterName];
            if (frames.Length == 0)
                return;
                
            // 计算当前帧
            float frameTime = characterDataCache.ContainsKey(characterName) ? 
                characterDataCache[characterName].Animation.FrameTime : ANIMATION_FRAME_TIME;
            int currentFrame = (int)(animationTimer / frameTime) % frames.Length;
            
            var texture = frames[currentFrame];
            
            // 计算绘制位置和大小
            float maxSize = 60 * UIScaleManager.UniformScale;
            float scale = Math.Min(maxSize / texture.Width, maxSize / texture.Height);
            
            Vector2 position = new Vector2(x, y - maxSize / 2);
            Vector2 origin = Vector2.Zero;
            
            spriteBatch.Draw(texture, position, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
        }
    }
}