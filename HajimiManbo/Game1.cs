using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using HajimiManbo.GameStates;
using HajimiManbo.Network;
using Microsoft.Xna.Framework.Media;

namespace HajimiManbo
{
    public class Game1 : Game
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
        
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _defaultFont;
        private Texture2D _bgTexture;
        private Texture2D _secondBgTexture;
        private Texture2D _RoomBgTexture;

        private GameState _currentState;
        private MainMenuState _mainMenuState;

        private Song _menuBgm;
        
        // 全局音量设置，用于记忆音量
        public static float GlobalVolume { get; set; } = 0.6f;
        
        // FPS显示相关
        private int _frameCount = 0;
        private TimeSpan _elapsedTime = TimeSpan.Zero;
        private float _fps = 0f;
        private bool _showFPS = true; // 是否显示FPS
        private Texture2D _pixelTexture; // 用于绘制FPS背景的像素纹理

        public Game1()
        {
#if DEBUG
            AllocConsole();            // 分配一个新的控制台窗口
#endif
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // —— 默认 1920×1080 全屏 ——
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            
            // —— 刷新率设置 ——
            // 设置目标帧率为60FPS（默认）
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / 60.0);
            // 启用固定时间步长，确保稳定的帧率
            IsFixedTimeStep = true;
            // 启用垂直同步，防止画面撕裂
            _graphics.SynchronizeWithVerticalRetrace = true;
        }

        public void ApplyGraphicsChanges()
        {
            _graphics.ApplyChanges();              // ① 真正套用到 GPU
            UIScaleManager.UpdateScale(_graphics); // ② 重新计算缩放因子

            // ③ 重新生成依赖静态像素坐标的主菜单
            _mainMenuState = new MainMenuState(this, _graphics, _spriteBatch,
                                               _defaultFont, _bgTexture, _secondBgTexture, _RoomBgTexture);
            if (_currentState is MainMenuState)
                _currentState = _mainMenuState;

            // ④ 通知当前状态刷新布局（SettingsState 等）
            _currentState?.OnResolutionChanged();
        }


        protected override void Initialize()
        {
            UIScaleManager.Initialize(_graphics);   // 先算一次基准比例
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _defaultFont = Content.Load<SpriteFont>("Font/MyFont");
            _bgTexture = Content.Load<Texture2D>("img/BackGround/BackGroundPicture");
            _secondBgTexture = Content.Load<Texture2D>("img/BackGround/test"); // 使用相同的房间背景图片
            _menuBgm = Content.Load<Song>("Music/hudmusic");  // 不带扩展名
            _RoomBgTexture = Content.Load<Texture2D>("img/BackGround/roombackground"); // 房间背景

            // 播放一次就循环
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = GlobalVolume;   // 使用全局音量设置
            MediaPlayer.Play(_menuBgm);

            // 初始化主菜单状态
            _mainMenuState = new MainMenuState(this, _graphics, _spriteBatch, _defaultFont, _bgTexture, _secondBgTexture, _RoomBgTexture);
            _currentState = _mainMenuState;
            
            // 订阅网络事件
            NetworkManager.Instance.OnSwitchToMapGeneration += OnNetworkSwitchToMapGeneration;
            NetworkManager.Instance.OnReturnToWaitingRoom += OnNetworkReturnToWaitingRoom;
            NetworkManager.Instance.OnStartGame += OnNetworkStartGame;
        }
        
        /// <summary>
        /// 网络事件：切换到地图生成页面
        /// </summary>
        private void OnNetworkSwitchToMapGeneration()
        {
            // 只有客户端需要响应这个事件，服务器自己处理切换
            if (NetworkManager.Instance.IsClient && !NetworkManager.Instance.IsServer)
            {
                SwitchToMapGeneration();
            }
        }
        
        /// <summary>
        /// 网络事件：返回等待房间
        /// </summary>
        private void OnNetworkReturnToWaitingRoom()
        {
            ReturnToWaitingRoom();
        }
        
        /// <summary>
        /// 网络事件：开始游戏
        /// </summary>
        private void OnNetworkStartGame(int seed, HajimiManbo.World.WorldSettings worldSettings)
        {
            // 只有客户端需要响应这个事件，服务器自己处理切换
            if (NetworkManager.Instance.IsClient && !NetworkManager.Instance.IsServer)
            {
                SwitchToGamePlay(seed, worldSettings);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            // 计算FPS
            if (_showFPS)
            {
                _elapsedTime += gameTime.ElapsedGameTime;
                if (_elapsedTime >= TimeSpan.FromSeconds(1))
                {
                    _fps = _frameCount / (float)_elapsedTime.TotalSeconds;
                    _frameCount = 0;
                    _elapsedTime = TimeSpan.Zero;
                }
            }
            
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.RightAlt))
                Exit();

            // F11切换全屏
            if (Keyboard.GetState().IsKeyDown(Keys.F11))
            {
                ToggleFullScreen();
            }

            NetworkManager.Instance.Update();
            _currentState?.Update(gameTime);
            base.Update(gameTime);
        }

        private void ToggleFullScreen()
        {
            _graphics.IsFullScreen = !_graphics.IsFullScreen;
            // 不再随意改分辨率，保留当前 back-buffer 大小
            ApplyGraphicsChanges();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            _currentState?.Draw(gameTime);
            
            // 绘制FPS显示
            if (_showFPS && _defaultFont != null)
            {
                DrawFPS();
            }
            
            _spriteBatch.End();

            // 增加帧计数
            if (_showFPS)
            {
                _frameCount++;
            }
            
            base.Draw(gameTime);
        }

        public void ChangeState(GameState newState)
        {
            _currentState = newState;
        }

        public void ReturnToMainMenu()
        {
            _currentState = _mainMenuState;
        }

        public void ReturnToSelectRoom()
        {
            _currentState = new RoomSelectionState(this, _graphics, _spriteBatch, _defaultFont, _bgTexture, _secondBgTexture, _RoomBgTexture);
        }
        
        /// <summary>
        /// 设置游戏刷新率
        /// </summary>
        /// <param name="fps">目标帧率（如60, 120, 144等）</param>
        public void SetFrameRate(int fps)
        {
            if (fps <= 0) fps = 60; // 默认60FPS
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / fps);
            Console.WriteLine($"[Game] Frame rate set to {fps} FPS");
        }
        
        /// <summary>
        /// 切换垂直同步
        /// </summary>
        /// <param name="enabled">是否启用垂直同步</param>
        public void SetVSync(bool enabled)
        {
            _graphics.SynchronizeWithVerticalRetrace = enabled;
            _graphics.ApplyChanges();
            Console.WriteLine($"[Game] VSync {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// 切换固定时间步长
        /// </summary>
        /// <param name="enabled">是否启用固定时间步长</param>
        public void SetFixedTimeStep(bool enabled)
        {
            IsFixedTimeStep = enabled;
            Console.WriteLine($"[Game] Fixed time step {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// 获取当前目标刷新率
        /// </summary>
        /// <returns>当前刷新率，如果是无上限模式返回0</returns>
        public int GetCurrentFrameRate()
        {
            if (!IsFixedTimeStep)
                return 0; // 无上限模式
            
            // 计算当前目标FPS
            double targetFPS = 1.0 / TargetElapsedTime.TotalSeconds;
            return (int)Math.Round(targetFPS);
        }
        
        /// <summary>
        /// 绘制FPS显示
        /// </summary>
        private void DrawFPS()
        {
            // 优化FPS显示格式
            string fpsText;
            if (_fps >= 10000)
                fpsText = $"{(_fps / 1000):F1}K";
            else if (_fps >= 1000)
                fpsText = $"{(_fps / 1000):F2}K";
            else
                fpsText = $"{_fps:F1}";
                
            Vector2 position = new Vector2(10, 10); // 左上角位置
            Color color = _fps >= 50 ? Color.Green : _fps >= 30 ? Color.Yellow : Color.Red;
            float scale = 1.0f;
            
            // 绘制背景
            Vector2 textSize = _defaultFont.MeasureString(fpsText) * scale;
            Rectangle background = new Rectangle(
                (int)position.X - 5,
                (int)position.Y - 5,
                (int)textSize.X + 10,
                (int)textSize.Y + 10
            );
            
            // 创建1x1像素纹理用于绘制背景
            if (_pixelTexture == null)
            {
                _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
                _pixelTexture.SetData(new[] { Color.White });
            }
            
            // 绘制半透明黑色背景
            _spriteBatch.Draw(_pixelTexture, background, Color.Black * 0.5f);
            
            // 绘制FPS文本
            _spriteBatch.DrawString(_defaultFont, fpsText, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        
        /// <summary>
        /// 切换FPS显示开关
        /// </summary>
        public void ToggleFPSDisplay()
        {
            _showFPS = !_showFPS;
            Console.WriteLine($"[Game] FPS display {(_showFPS ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// 切换到角色选择界面
        /// </summary>
        public void SwitchToCharacterSelection()
        {
            _currentState = new CharacterSelectionState(this, _graphics, _spriteBatch, _defaultFont, _RoomBgTexture, _secondBgTexture);
        }
        
        /// <summary>
        /// 返回等待房间界面
        /// </summary>
        public void ReturnToWaitingRoom()
        {
            _currentState = new WaitingRoomState(this, _graphics, _spriteBatch, _defaultFont, _RoomBgTexture, _secondBgTexture, _RoomBgTexture);
        }
        
        /// <summary>
        /// 切换到地图生成设置界面
        /// </summary>
        public void SwitchToMapGeneration()
        {
            _currentState = new MapGenerationState(this, _graphics, _spriteBatch, _defaultFont, _bgTexture, _secondBgTexture, _RoomBgTexture);
        }
        
        /// <summary>
        /// 切换到游戏界面并开始世界生成
        /// </summary>
        public void SwitchToGamePlay(int seed, HajimiManbo.World.WorldSettings worldSettings)
        {
            var gamePlayState = new GamePlayState(this, _graphics, _spriteBatch, _defaultFont);
            gamePlayState.StartWorldGeneration(seed, worldSettings);
            _currentState = gamePlayState;
        }
    }
}