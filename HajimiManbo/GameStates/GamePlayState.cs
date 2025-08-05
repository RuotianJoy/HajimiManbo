using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using HajimiManbo.World;
using HajimiManbo.Gameplay;
using System;
using System.Threading.Tasks;

namespace HajimiManbo.GameStates
{
    public class GamePlayState : GameState
    {
        private World.World world;
        private WorldRenderer worldRenderer;
        private WorldGenerator worldGenerator;
        private bool isGenerating = false;
        private string generationStatus = "";
        private float generationProgress = 0f;
        
        // 玩家和摄像机系统
        private Player player;
        private Camera2D camera;
        private KeyboardState previousKeyboardState;
        private MouseState previousMouseState;
        
        // 网络玩家管理
        private System.Collections.Generic.Dictionary<int, Player> networkPlayers = new System.Collections.Generic.Dictionary<int, Player>();
        
        // 调试模式
        private bool showDebugInfo = false;
        
        // ESC菜单相关
        private bool showEscMenu = false;
        private int selectedMenuOption = 0;
        private readonly string[] menuOptions = { "返回房间", "取消" };
        
        public GamePlayState(Game1 game, GraphicsDeviceManager graphics, SpriteBatch spriteBatch, SpriteFont font)
            : base(game, graphics, spriteBatch, font)
        {
            worldRenderer = new WorldRenderer(graphics.GraphicsDevice, spriteBatch, game.Content);
            worldGenerator = new WorldGenerator();
            
            // 初始化摄像机
            camera = new Camera2D(graphics.GraphicsDevice.Viewport);
            
            // 订阅生成进度事件
            worldGenerator.OnProgressUpdate += OnWorldGenerationProgress;
            
            // 订阅世界生成完成事件
            worldGenerator.OnWorldGenerationComplete += OnWorldGenerationComplete;
            
            // 订阅网络事件
            var networkManager = HajimiManbo.Network.NetworkManager.Instance;
            networkManager.OnReturnToRoomFromGame += OnReturnToRoomFromGame;
        }
        
        /// <summary>
        /// 开始生成世界
        /// </summary>
        public void StartWorldGeneration(int seed, WorldSettings settings)
        {
            if (isGenerating) return;
            
            isGenerating = true;
            generationStatus = "开始生成世界...";
            generationProgress = 0f;
            
            // 在后台线程生成世界
            Task.Run(() =>
            {
                try
                {
                    world = worldGenerator.Generate(seed, settings);
                    
                    // 在世界最左边生成玩家 - 确保所有玩家在相同位置spawn
                    int spawnX = 10; // 距离左边界10个tile的位置
                    float spawnPixelX = spawnX * 16; // 转换为像素坐标
                    
                    // 获取玩家选择的角色
                    string selectedCharacter = HajimiManbo.Network.NetworkManager.Instance.GetLocalPlayerCharacter();
                    player = new Player(new Vector2(spawnPixelX, 0), world, graphics.GraphicsDevice, selectedCharacter, game.Content);
                    
                    // 找到地面位置并设置玩家位置
                    Vector2 finalSpawnPos = player.SpawnAt(spawnPixelX);
                    Console.WriteLine($"[GamePlay] Local player spawned at: ({finalSpawnPos.X}, {finalSpawnPos.Y})");
                    
                    // 通知网络管理器本地玩家的spawn位置
                    var networkManager = HajimiManbo.Network.NetworkManager.Instance;
                    if (networkManager.IsServer)
                    {
                        // 服务器更新本地玩家状态
                        networkManager.UpdatePlayerState(0, finalSpawnPos, Vector2.Zero, false, false, true, true);
                    }
                    
                    // 设置摄像机世界边界和初始位置
                    camera.WorldBounds = new Rectangle(0, 0, world.Width * 16, world.Height * 16);
                    camera.CenterOn(player.Position);
                    
                    // 设置世界到渲染器，初始化光照系统
                    worldRenderer?.SetWorld(world);
                    
                    isGenerating = false;
                    generationStatus = "世界生成完成！";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GamePlay] World generation failed: {ex.Message}");
                    isGenerating = false;
                    generationStatus = "世界生成失败！";
                }
            });
        }
        
        /// <summary>
        /// 世界生成进度回调
        /// </summary>
        private void OnWorldGenerationProgress(string status, float progress)
        {
            generationStatus = status;
            generationProgress = progress;
        }
        
        private void OnWorldGenerationComplete(World.World generatedWorld)
        {
            // 设置世界到渲染器，初始化光照系统
            worldRenderer?.SetWorld(generatedWorld);
            
            // 强制重建所有渲染缓存，确保新的地形算法生效
            worldRenderer?.ForceRebuildChunks();
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();
            
            // 处理ESC菜单
            if (showEscMenu)
            {
                HandleEscMenuInput(keyboardState);
                previousKeyboardState = keyboardState;
                previousMouseState = mouseState;
                return;
            }
            
            // 只有房主可以按ESC打开菜单
            var netManager = HajimiManbo.Network.NetworkManager.Instance;
            if (InputManager.IsKeyPressed("取消", keyboardState, previousKeyboardState))
            {
                if (netManager.IsServer)
                {
                    showEscMenu = true;
                    selectedMenuOption = 0;
                }
                // 非房主按ESC不做任何操作
                previousKeyboardState = keyboardState;
                previousMouseState = mouseState;
                return;
            }
            
            // 如果世界还在生成中，不处理其他输入
            if (isGenerating)
            {
                previousKeyboardState = keyboardState;
                previousMouseState = mouseState;
                return;
            }
            
            // 更新玩家
            if (player != null)
            {
                player.Update(gameTime);
                
                // 摄像机跟随玩家
                camera.SmoothFollow(player.Position, 0.1f);
                
                // 更新网络玩家状态
                var networkManager = HajimiManbo.Network.NetworkManager.Instance;
                if (networkManager.IsServer)
                {
                    // 更新本地玩家状态到网络管理器
                    networkManager.UpdatePlayerState(0, player.Position, player.Velocity, 
                        player.IsMoving, player.IsSprinting, player.IsOnGround, player.FacingRight);
                    
                    // 定时同步玩家状态
                    networkManager.SyncPlayerStates();
                    
                    // 服务器端同样需要渲染其他客户端玩家
                    UpdateNetworkPlayers(networkManager, gameTime);
                }
                else if (networkManager.IsClientConnected)
                {
                    // 先把自己的最新状态发给服务器
                    networkManager.UpdatePlayerState(0, player.Position, player.Velocity, 
                        player.IsMoving, player.IsSprinting, player.IsOnGround, player.FacingRight);
                    
                    // 再拉取/绘制其他玩家
                    UpdateNetworkPlayers(networkManager, gameTime);
                }
            }
            
            // 处理鼠标左键点击摧毁物块
            if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                HandleBlockDestruction(mouseState);
            }
            
            // 处理光照系统调试控制
            // 光照调试控制已移除
            
            // 处理鼠标右键点击放置物块
            if (mouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released)
            {
                HandleBlockPlacement(mouseState);
            }
            
            // 调试信息切换
            if (keyboardState.IsKeyDown(Keys.F3) && !previousKeyboardState.IsKeyDown(Keys.F3))
            {
                showDebugInfo = !showDebugInfo;
            }
            
            previousKeyboardState = keyboardState;
            previousMouseState = mouseState;
        }
        
        /// <summary>
        /// 更新网络玩家状态
        /// </summary>
        private void UpdateNetworkPlayers(HajimiManbo.Network.NetworkManager networkManager, GameTime gameTime)
        {
            var playerStateManager = networkManager.GetPlayerStateManager();
            
            Console.WriteLine($"[Client] UpdateNetworkPlayers called, local slot: {networkManager.GetLocalPlayerSlot()}");
            
            // 遍历所有活跃的玩家槽位
            for (int slot = 0; slot < 8; slot++)
            {
                var playerState = playerStateManager.GetPlayerState(slot);
                if (playerState != null && playerState.Active)
                {
                    Console.WriteLine($"[Client] Found active player in slot {slot}: {playerState.PlayerName}, character: {playerState.CharacterName}, pos: {playerState.Position}");
                    
                    // 跳过本地玩家
                    if (slot == networkManager.GetLocalPlayerSlot()) 
                    {
                        Console.WriteLine($"[Client] Skipping local player in slot {slot}");
                        continue;
                    }
                    
                    // 创建或更新网络玩家
                    if (!networkPlayers.ContainsKey(slot))
                    {
                        // 创建新的网络玩家
                        Console.WriteLine($"[Client] Creating new network player for slot {slot} at position ({playerState.Position.X}, {playerState.Position.Y})");
                        var networkPlayer = new Player(playerState.Position, world, graphics.GraphicsDevice, 
                            playerState.CharacterName, game.Content);
                        // 设置网络玩家的位置和状态
                        networkPlayer.Position = playerState.Position;
                        networkPlayer.UpdateNetworkState(playerState.Velocity, playerState.IsMoving, 
                            playerState.IsSprinting, playerState.IsOnGround, playerState.FacingRight);
                        networkPlayers[slot] = networkPlayer;
                    }
                    else
                    {
                        // 更新现有网络玩家的位置和状态
                        var networkPlayer = networkPlayers[slot];
                        networkPlayer.Position = playerState.Position;
                        networkPlayer.UpdateNetworkState(playerState.Velocity, playerState.IsMoving, 
                            playerState.IsSprinting, playerState.IsOnGround, playerState.FacingRight);
                    }
                    
                    // 更新网络玩家的动画（无论是新创建还是现有的）
                    if (networkPlayers.ContainsKey(slot))
                    {
                        networkPlayers[slot].UpdateNetworkAnimation(gameTime);
                    }
                }
                else if (networkPlayers.ContainsKey(slot))
                {
                    // 移除不活跃的玩家
                    Console.WriteLine($"[Client] Removing inactive player from slot {slot}");
                    networkPlayers.Remove(slot);
                }
            }
            
            // 调试输出当前网络玩家数量
            if (networkPlayers.Count > 0)
            {
                Console.WriteLine($"[Client] Currently tracking {networkPlayers.Count} network players");
            }
        }
        


        public override void Draw(GameTime gameTime)
        {
            Rectangle viewportBounds = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            
            if (isGenerating)
            {
                // 显示生成进度
                DrawGenerationProgress();
            }
            else if (world != null)
            {
                // 结束当前的SpriteBatch进行分块渲染
                spriteBatch.End();
                
                // 使用分块系统渲染世界（不需要SpriteBatch）
                worldRenderer.RenderWorld(world, viewportBounds, camera.GetViewMatrix(), camera.Position);
                
                // 重新开始SpriteBatch渲染玩家和UI
                spriteBatch.Begin(transformMatrix: camera.GetViewMatrix());
                
                // 渲染本地玩家
                if (player != null)
                {
                    player.Draw(spriteBatch);
                }
                
                // 渲染网络玩家
                foreach (var networkPlayer in networkPlayers.Values)
                {
                    networkPlayer.Draw(spriteBatch);
                }
                
                // 结束摄像机渲染，重新开始UI渲染
                spriteBatch.End();
                spriteBatch.Begin();
                
                // 显示控制提示
                DrawControlHints();
                
                // 显示调试信息
                if (showDebugInfo)
                {
                    DrawDebugInfo();
                }
                
                // 显示ESC菜单
                if (showEscMenu)
                {
                    DrawEscMenu();
                }
                
                // 注意：不要在这里调用End()，因为Game1会处理
            }
            else
            {
                // 显示错误信息
                string errorText = "世界生成失败，按ESC返回主菜单";
                Vector2 textSize = font.MeasureString(errorText);
                Vector2 position = new Vector2(
                    graphics.PreferredBackBufferWidth / 2 - textSize.X / 2,
                    graphics.PreferredBackBufferHeight / 2 - textSize.Y / 2
                );
                spriteBatch.DrawString(font, errorText, position, Color.Red);
            }
        }
        
        /// <summary>
        /// 绘制生成进度
        /// </summary>
        private void DrawGenerationProgress()
        {
            // 背景
            Rectangle screenBounds = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            
            // 状态文本
            Vector2 statusSize = font.MeasureString(generationStatus);
            Vector2 statusPos = new Vector2(
                graphics.PreferredBackBufferWidth / 2 - statusSize.X / 2,
                graphics.PreferredBackBufferHeight / 2 - statusSize.Y / 2 - 50
            );
            spriteBatch.DrawString(font, generationStatus, statusPos, Color.White);
            
            // 进度条
            int progressBarWidth = 400;
            int progressBarHeight = 20;
            Rectangle progressBarBg = new Rectangle(
                graphics.PreferredBackBufferWidth / 2 - progressBarWidth / 2,
                graphics.PreferredBackBufferHeight / 2 + 20,
                progressBarWidth,
                progressBarHeight
            );
            
            Rectangle progressBarFill = new Rectangle(
                progressBarBg.X,
                progressBarBg.Y,
                (int)(progressBarWidth * generationProgress),
                progressBarHeight
            );
            
            // 创建1x1像素纹理用于绘制进度条
            Texture2D pixelTexture = new Texture2D(graphics.GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
            
            spriteBatch.Draw(pixelTexture, progressBarBg, Color.Gray);
            spriteBatch.Draw(pixelTexture, progressBarFill, Color.Green);
            
            // 进度百分比
            string progressText = $"{(generationProgress * 100):F0}%";
            Vector2 progressTextSize = font.MeasureString(progressText);
            Vector2 progressTextPos = new Vector2(
                graphics.PreferredBackBufferWidth / 2 - progressTextSize.X / 2,
                progressBarBg.Y + progressBarHeight + 10
            );
            spriteBatch.DrawString(font, progressText, progressTextPos, Color.White);
            
            pixelTexture.Dispose();
        }
        
        /// <summary>
        /// 绘制控制提示
        /// </summary>
        private void DrawControlHints()
        {
            string controlText = "WASD: 移动 | 空格: 跳跃 | 鼠标左键: 摧毁 | 鼠标右键: 放置 | F3: 调试 | L: 光照 | T: 光源 | ESC: 菜单";
            Vector2 textSize = font.MeasureString(controlText);
            Vector2 position = new Vector2(
                graphics.PreferredBackBufferWidth / 2 - textSize.X / 2,
                graphics.PreferredBackBufferHeight - textSize.Y - 10
            );
            
            // 绘制背景
            Texture2D pixelTexture = new Texture2D(graphics.GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
            
            Rectangle background = new Rectangle(
                (int)position.X - 5,
                (int)position.Y - 5,
                (int)textSize.X + 10,
                (int)textSize.Y + 10
            );
            spriteBatch.Draw(pixelTexture, background, Color.Black * 0.7f);
            
            //spriteBatch.DrawString(font, controlText, position, Color.White);
            
            pixelTexture.Dispose();
        }
        
        /// <summary>
        /// 绘制调试信息
        /// </summary>
        private void DrawDebugInfo()
        {
            if (player == null || world == null) return;
            
            // 获取当前生物群系信息和背景混合状态
            var currentBiome = worldRenderer.GetCurrentBiome(world, player.Position);
            string biomeName = GetBiomeName(currentBiome);
            
            // 获取背景混合信息（多层背景系统）
            string blendInfo = "无混合";
            string layerInfo = "无";
            try
            {
                // 通过反射或者添加公共方法来获取背景管理器
                var backgroundManagerField = typeof(HajimiManbo.World.WorldRenderer).GetField("backgroundManager", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (backgroundManagerField != null)
                {
                    var backgroundManager = backgroundManagerField.GetValue(worldRenderer) as HajimiManbo.World.BiomeBackgroundManager;
                    if (backgroundManager != null)
                    {
                        // 获取多层背景信息
                        var (primaryLayers, secondaryLayers, blendFactor) = backgroundManager.GetBlendedBackgroundLayers(world, player.Position.X, player.Position.Y);
                        if (blendFactor > 0f)
                        {
                            int tileX = (int)(player.Position.X / 16);
                            int tileY = (int)(player.Position.Y / 16);
                            var (primaryType, secondaryType, _) = backgroundManager.GetBiomeBlend(world, tileX, tileY);
                            string primaryName = GetBiomeName(primaryType);
                            string secondaryName = GetBiomeName(secondaryType);
                            blendInfo = $"{primaryName} -> {secondaryName} ({blendFactor:F2})";
                        }
                        
                        // 显示当前背景层数信息
                        if (primaryLayers != null)
                        {
                            layerInfo = $"{primaryLayers.Length}层背景";
                        }
                    }
                }
            }
            catch
            {
                // 如果获取失败，保持默认值
            }
            
            string[] debugLines = {
                $"玩家位置: ({player.Position.X:F1}, {player.Position.Y:F1})",
                $"玩家速度: ({player.Velocity.X:F1}, {player.Velocity.Y:F1})",
                $"摄像机位置: ({camera.Position.X:F1}, {camera.Position.Y:F1})",
                $"摄像机缩放: {camera.Zoom:F2}",
                $"当前生物群系: {biomeName}",
                $"背景混合: {blendInfo}",
                $"背景层数: {layerInfo}",
                $"网络玩家: {networkPlayers.Count}",
                $"世界大小: {world.Width} x {world.Height}",
                $"FPS: {1.0f / (float)game.TargetElapsedTime.TotalSeconds:F0}"
            };
            
            Vector2 position = new Vector2(10, 10);
            
            // 绘制背景
            Texture2D pixelTexture = new Texture2D(graphics.GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
            
            float maxWidth = 0;
            float totalHeight = 0;
            foreach (string line in debugLines)
            {
                Vector2 lineSize = font.MeasureString(line);
                maxWidth = Math.Max(maxWidth, lineSize.X);
                totalHeight += lineSize.Y;
            }
            
            Rectangle background = new Rectangle(
                (int)position.X - 5,
                (int)position.Y - 5,
                (int)maxWidth + 10,
                (int)totalHeight + 10
            );
            spriteBatch.Draw(pixelTexture, background, Color.Black * 0.7f);
            
            // 绘制文本
            Vector2 currentPos = position;
            foreach (string line in debugLines)
            {
                spriteBatch.DrawString(font, line, currentPos, Color.White);
                currentPos.Y += font.MeasureString(line).Y;
            }
            
            pixelTexture.Dispose();
        }
        
        /// <summary>
        /// 绘制ESC菜单
        /// </summary>
        private void DrawEscMenu()
        {
            // 半透明背景
            Texture2D pixelTexture = new Texture2D(graphics.GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
            
            Rectangle screenBounds = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            spriteBatch.Draw(pixelTexture, screenBounds, Color.Black * 0.7f);
            
            // 菜单面板
            int panelWidth = 300;
            int panelHeight = 200;
            Rectangle panelRect = new Rectangle(
                graphics.PreferredBackBufferWidth / 2 - panelWidth / 2,
                graphics.PreferredBackBufferHeight / 2 - panelHeight / 2,
                panelWidth,
                panelHeight
            );
            
            spriteBatch.Draw(pixelTexture, panelRect, Color.DarkGray);
            
            // 菜单标题
            string title = "游戏菜单";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(
                panelRect.X + panelWidth / 2 - titleSize.X / 2,
                panelRect.Y + 20
            );
            spriteBatch.DrawString(font, title, titlePos, Color.White);
            
            // 菜单选项
            float startY = panelRect.Y + 80;
            for (int i = 0; i < menuOptions.Length; i++)
            {
                Color optionColor = (i == selectedMenuOption) ? Color.Yellow : Color.White;
                string optionText = (i == selectedMenuOption) ? "> " + menuOptions[i] + " <" : menuOptions[i];
                
                Vector2 optionSize = font.MeasureString(optionText);
                Vector2 optionPos = new Vector2(
                    panelRect.X + panelWidth / 2 - optionSize.X / 2,
                    startY + i * 40
                );
                
                spriteBatch.DrawString(font, optionText, optionPos, optionColor);
            }
            
            // // 操作提示
            // string hint = "鼠标点击选择  ESC取消";
            // Vector2 hintSize = font.MeasureString(hint);
            // Vector2 hintPos = new Vector2(
            //     panelRect.X + panelWidth / 2 - hintSize.X / 2,
            //     panelRect.Y + panelHeight - 30
            // );
            // spriteBatch.DrawString(font, hint, hintPos, Color.LightGray);
            
            pixelTexture.Dispose();
        }
        
        /// <summary>
        /// 处理ESC菜单输入
        /// </summary>
        private void HandleEscMenuInput(KeyboardState keyboardState)
        {
            MouseState mouseState = Mouse.GetState();
            
            // 获取鼠标位置
            Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
            
            // 计算菜单面板位置
            int panelWidth = 300;
            int panelHeight = 200;
            Rectangle panelRect = new Rectangle(
                graphics.PreferredBackBufferWidth / 2 - panelWidth / 2,
                graphics.PreferredBackBufferHeight / 2 - panelHeight / 2,
                panelWidth,
                panelHeight
            );
            
            // 检测鼠标悬停和点击
            float startY = panelRect.Y + 80;
            selectedMenuOption = -1; // 重置选择
            
            for (int i = 0; i < menuOptions.Length; i++)
            {
                Vector2 optionSize = font.MeasureString(menuOptions[i]);
                Rectangle optionRect = new Rectangle(
                    (int)(panelRect.X + panelWidth / 2 - optionSize.X / 2 - 10),
                    (int)(startY + i * 40 - 5),
                    (int)(optionSize.X + 20),
                    (int)(optionSize.Y + 10)
                );
                
                if (optionRect.Contains(mousePos))
                {
                    selectedMenuOption = i;
                    
                    // 检测鼠标点击
                    if (mouseState.LeftButton == ButtonState.Pressed && 
                        previousMouseState.LeftButton == ButtonState.Released)
                    {
                        ExecuteMenuOption();
                        return;
                    }
                }
            }
            
            // ESC键关闭菜单（使用自定义按键）
            if (InputManager.IsKeyPressed("取消", keyboardState, previousKeyboardState))
            {
                showEscMenu = false;
            }
        }
        
        /// <summary>
        /// 执行菜单选项
        /// </summary>
        private void ExecuteMenuOption()
        {
            switch (selectedMenuOption)
            {
                case 0: // 返回房间
                    var networkManager = HajimiManbo.Network.NetworkManager.Instance;
                    if (networkManager.IsServer)
                    {
                        // 房主广播返回房间消息
                        networkManager.BroadcastReturnToRoomFromGame();
                        // 房主自己也返回房间
                        game.ReturnToWaitingRoom();
                    }
                    break;
                case 1: // 取消
                    showEscMenu = false;
                    break;
            }
        }
        
        /// <summary>
        /// 处理从游戏返回房间的网络事件
        /// </summary>
        private void OnReturnToRoomFromGame()
        {
            // 客户端收到返回房间消息后，直接返回等待房间
            game.ReturnToWaitingRoom();
        }
        
        /// <summary>
        /// 获取生物群系的显示名称
        /// </summary>
        /// <param name="biomeType">生物群系类型</param>
        /// <returns>显示名称</returns>
        private string GetBiomeName(HajimiManbo.World.BiomeBackgroundManager.BiomeType biomeType)
        {
            return biomeType switch
            {
                HajimiManbo.World.BiomeBackgroundManager.BiomeType.Forest => "森林",
                HajimiManbo.World.BiomeBackgroundManager.BiomeType.Snow => "雪地",
                HajimiManbo.World.BiomeBackgroundManager.BiomeType.Jungle => "丛林",
                HajimiManbo.World.BiomeBackgroundManager.BiomeType.Desert => "沙漠",
                HajimiManbo.World.BiomeBackgroundManager.BiomeType.Underground => "地下",
                HajimiManbo.World.BiomeBackgroundManager.BiomeType.Default => "默认",
                _ => "未知"
            };
        }
        
        /// <summary>
        /// 处理物块摧毁
        /// </summary>
        private void HandleBlockDestruction(MouseState mouseState)
        {
            if (world == null || camera == null) return;
            
            // 将鼠标屏幕坐标转换为世界坐标
            Vector2 mouseScreenPos = new Vector2(mouseState.X, mouseState.Y);
            Vector2 mouseWorldPos = camera.ScreenToWorld(mouseScreenPos);
            
            // 将世界坐标转换为方块坐标（每个方块16像素）
            int tileX = (int)(mouseWorldPos.X / 16);
            int tileY = (int)(mouseWorldPos.Y / 16);
            
            // 检查坐标是否在世界范围内
            if (world.IsValidCoordinate(tileX, tileY))
            {
                // 获取当前方块
                var currentTile = world.GetTile(tileX, tileY);
                
                // 只摧毁非空气方块
                if (currentTile.Type != TileType.Air)
                {
                    // 创建新的方块，只清除方块类型
                    var newTile = new Tile(
                        TileType.Air,           // 方块类型设为空气
                        currentTile.Style,      // 保留样式
                        currentTile.Liquid,     // 保留液体
                        currentTile.FrameVariant, // 保留帧变体
                        currentTile.Slope       // 保留斜坡
                    );
                    
                    world.SetTile(tileX, tileY, newTile);
                    
                    // 通知世界渲染器更新分块
                    if (worldRenderer != null)
                    {
                        worldRenderer.OnTileChanged(tileX, tileY);
                    }
                }
            }
        }
        
        // 光照调试控制方法已移除
        
        /// <summary>
        /// 处理物块放置
        /// </summary>
        private void HandleBlockPlacement(MouseState mouseState)
        {
            if (world == null || camera == null) return;
            
            // 将鼠标屏幕坐标转换为世界坐标
            Vector2 mouseScreenPos = new Vector2(mouseState.X, mouseState.Y);
            Vector2 mouseWorldPos = camera.ScreenToWorld(mouseScreenPos);
            
            // 将世界坐标转换为方块坐标（每个方块16像素）
            int tileX = (int)(mouseWorldPos.X / 16);
            int tileY = (int)(mouseWorldPos.Y / 16);
            
            // 检查坐标是否在世界范围内
            if (world.IsValidCoordinate(tileX, tileY))
            {
                // 获取当前方块
                var currentTile = world.GetTile(tileX, tileY);
                
                // 只在空气方块位置放置新方块
                if (currentTile.Type == TileType.Air)
                {
                    // 创建新的方块
                    var newTile = new Tile(
                        TileType.Dirt,          // 放置泥土方块（可以后续扩展为可选择的方块类型）
                        currentTile.Style,      // 保留样式
                        currentTile.Liquid,     // 保留液体
                        currentTile.FrameVariant, // 保留帧变体
                        currentTile.Slope       // 保留斜坡
                    );
                    
                    world.SetTile(tileX, tileY, newTile);
                    
                    // 通知世界渲染器更新分块
                    if (worldRenderer != null)
                    {
                        worldRenderer.OnTileChanged(tileX, tileY);
                    }
                }
            }
        }
    }
}