using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using HajimiManbo.World;
using System;
using System.Text.Json;

namespace HajimiManbo.Gameplay
{
    /// <summary>
    /// 玩家类，处理玩家移动、碰撞检测等逻辑
    /// </summary>
    public class Player
    {
        private Vector2 _position;
        private Vector2 _velocity;
        private Rectangle _bounds;
        private World.World _world;
        private Texture2D _texture;
        private Color _color;
        private CharacterAnimator _animator;
        private ContentManager _contentManager;
        private string _characterName;
        private bool _facingRight = true; // 角色面向方向，true为右，false为左
        
        // 移动参数（从JSON文件加载）
        private float _moveSpeed = 200f; // 像素/秒
        private float _sprintSpeed = 350f; // 冲刺速度
        private float _jumpSpeed = 400f;
        private const float Gravity = 800f; // 重力保持固定
        private const float MaxFallSpeed = 600f; // 最大下落速度保持固定
        
        // 角色数据结构
        private class CharacterData
        {
            public string id { get; set; }
            public string name { get; set; }
            public Stats stats { get; set; }
            public Animation animation { get; set; }
        }
        
        private class Stats
        {
            public float speed_multiplier { get; set; } = 1.0f;
            public float jump_multiplier { get; set; } = 1.0f;
        }
        
        private class Animation
        {
            public string folder_name { get; set; }
        }
        
        // 动画状态
        private bool _isMoving;
        private bool _isSprinting;
        
        // 玩家尺寸
        private const int PlayerWidth = 64;
        private const int PlayerHeight = 64;
        
        public Vector2 Position
        {
            get => _position;
            set
            {
                _position = value;
                UpdateBounds();
            }
        }
        
        public Vector2 Velocity => _velocity;
        
        public bool IsOnGround => _isOnGround;
        public bool IsMoving => _isMoving;
        public bool IsSprinting => _isSprinting;
        public bool FacingRight => _facingRight;
        public Rectangle Bounds => _bounds;
        
        /// <summary>
        /// 更新网络玩家的状态（仅用于网络同步）
        /// </summary>
        public void UpdateNetworkState(Vector2 velocity, bool isMoving, bool isSprinting, bool isOnGround, bool facingRight)
        {
            _velocity = velocity;
            _isMoving = isMoving;
            _isSprinting = isSprinting;
            _isOnGround = isOnGround;
            _facingRight = facingRight;
        }
        
        /// <summary>
        /// 更新网络玩家的动画（仅用于网络玩家，不处理输入和物理）
        /// </summary>
        public void UpdateNetworkAnimation(GameTime gameTime)
        {
            UpdateAnimation(gameTime);
        }
        
        private bool _isOnGround;
        private KeyboardState _previousKeyboardState;
        
        public Player(Vector2 startPosition, World.World world, GraphicsDevice graphicsDevice, string characterName, ContentManager contentManager)
        {
            _world = world;
            _position = startPosition;
            _velocity = Vector2.Zero;
            _contentManager = contentManager;
            _characterName = characterName;
            
            // 从JSON文件加载角色数据
            LoadCharacterData(characterName);
            
            // 根据角色名称设置颜色
            _color = GetCharacterColor(characterName);
            
            // 初始化动画系统
            try
            {
                _animator = new CharacterAnimator(contentManager, GetAnimationCharacterName(characterName));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Player] Failed to load character animations: {ex.Message}");
                // 创建后备纹理
                _texture = new Texture2D(graphicsDevice, 1, 1);
                _texture.SetData(new[] { Color.White });
            }
            
            UpdateBounds();
            _previousKeyboardState = Keyboard.GetState();
        }
        
        /// <summary>
        /// 从JSON文件加载角色数据
        /// </summary>
        private void LoadCharacterData(string characterName)
        {
            try
            {
                string jsonPath = $"Content/Character/{characterName?.ToLower()}.json";
                if (System.IO.File.Exists(jsonPath))
                {
                    string jsonContent = System.IO.File.ReadAllText(jsonPath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var characterData = JsonSerializer.Deserialize<CharacterData>(jsonContent, options);
                    
                    if (characterData?.stats != null)
                    {
                        // 基础移动速度
                        float baseMoveSpeed = 200f;
                        float baseSprintSpeed = 350f;
                        float baseJumpSpeed = 400f;
                        
                        // 根据角色数据调整移动参数
                        _moveSpeed = baseMoveSpeed * characterData.stats.speed_multiplier;
                        _sprintSpeed = baseSprintSpeed * characterData.stats.speed_multiplier;
                        _jumpSpeed = baseJumpSpeed * characterData.stats.jump_multiplier;
                        
                        Console.WriteLine($"[Player] Loaded character data for {characterName}: MoveSpeed={_moveSpeed}, SprintSpeed={_sprintSpeed}, JumpSpeed={_jumpSpeed}");
                    }
                }
                else
                {
                    Console.WriteLine($"[Player] Character JSON file not found: {jsonPath}, using default values");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Player] Failed to load character data for {characterName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 根据角色名称获取对应的颜色
        /// </summary>
        private Color GetCharacterColor(string characterName)
        {
            return characterName?.ToLower() switch
            {
                "hajiwei" => Color.Red,
                "hajiyang" => Color.Green,
                "doro" => Color.Yellow,
                _ => Color.Blue // 默认颜色
            };
        }
        
        /// <summary>
        /// 获取动画文件夹对应的角色名称
        /// </summary>
        private string GetAnimationCharacterName(string characterName)
        {
            return characterName?.ToLower() switch
            {
                "hajiwei" => "HaJiWei",
                "hajiyang" => "HaJiYang",
                "doro" => "Doro",
                _ => "HaJiWei" // 默认角色
            };
        }
        
        private void UpdateBounds()
        {
            _bounds = new Rectangle(
                (int)(_position.X - PlayerWidth / 2),
                (int)(_position.Y - PlayerHeight / 2),
                PlayerWidth,
                PlayerHeight
            );
        }
        
        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState keyboardState = Keyboard.GetState();
            
            HandleInput(keyboardState, deltaTime);
            ApplyPhysics(deltaTime);
            HandleCollisions();
            UpdateAnimation(gameTime);
            
            _previousKeyboardState = keyboardState;
        }
        
        /// <summary>
        /// 更新动画状态
        /// </summary>
        private void UpdateAnimation(GameTime gameTime)
        {
            if (_animator == null)
                return;
                
            // 根据玩家状态设置动画
            AnimationState newState;
            if (_isSprinting && _isMoving)
            {
                newState = AnimationState.Sprinting;
            }
            else if (_isMoving)
            {
                newState = AnimationState.Moving;
            }
            else
            {
                newState = AnimationState.Idle;
            }
            
            _animator.SetState(newState);
            _animator.Update(gameTime);
        }
        
        /// <summary>
        /// 根据角色类型和当前状态判断是否需要翻转动画
        /// </summary>
        private bool ShouldFlipAnimation()
        {
            // doro的冲刺移动和哈基为的普通移动是朝向左的，向右时需要翻转
            // 哈基为的冲刺和哈基阳的普通移动和冲刺都是向右的，向左时需要翻转
            
            if (_characterName == "doro")
            {
                // doro的冲刺移动朝向左，向右时翻转
                if (_isSprinting && _isMoving)
                {
                    return _facingRight;
                }
                // doro的其他动画保持原样
                return false;
            }
            else if (_characterName == "hajiwei")
            {
                // 哈基为的普通移动朝向左，向右时翻转
                if (_isMoving && !_isSprinting)
                {
                    return _facingRight;
                }
                // 哈基为的冲刺朝向右，向左时翻转
                else if (_isSprinting && _isMoving)
                {
                    return !_facingRight;
                }
                // 空闲状态保持原样
                return false;
            }
            else if (_characterName == "hajiyang")
            {
                // 哈基阳的普通移动和冲刺都朝向右，向左时翻转
                if (_isMoving)
                {
                    return !_facingRight;
                }
                // 空闲状态保持原样
                return false;
            }
            
            // 其他角色默认不翻转
            return false;
        }
        
        /// <summary>
        /// 根据角色类型和当前状态获取动画缩放比例
        /// </summary>
        private float GetAnimationScale()
        {
            // doro的冲刺动画放大一倍
            if (_characterName == "doro" && _isSprinting && _isMoving)
            {
                return 2.0f;
            }
            
            // 其他情况保持原始大小
            return 1.0f;
        }
        
        private void HandleInput(KeyboardState keyboardState, float deltaTime)
        {
            // 检测冲刺（使用自定义按键）
            _isSprinting = InputManager.IsKeyDown("冲刺", keyboardState);
            
            // 根据是否冲刺选择移动速度
            float currentMoveSpeed = _isSprinting ? _sprintSpeed : _moveSpeed;
            
            // 水平移动（使用自定义按键）
            bool movingLeft = InputManager.IsKeyDown("左移", keyboardState);
            bool movingRight = InputManager.IsKeyDown("右移", keyboardState);
            
            if (movingLeft)
            {
                _velocity.X = -currentMoveSpeed;
                _isMoving = true;
                _facingRight = false; // 向左移动
            }
            else if (movingRight)
            {
                _velocity.X = currentMoveSpeed;
                _isMoving = true;
                _facingRight = true; // 向右移动
            }
            else
            {
                _velocity.X = 0;
                _isMoving = false;
            }
            
            // 跳跃（使用自定义按键）- 检查脚下5像素内是否有地面
            if (InputManager.IsKeyPressed("跳跃", keyboardState, _previousKeyboardState) && CanJump())
            {
                _velocity.Y = -_jumpSpeed;
                _isOnGround = false;
            }
        }
        
        private void ApplyPhysics(float deltaTime)
        {
            // 应用重力
            if (!_isOnGround)
            {
                _velocity.Y += Gravity * deltaTime;
                if (_velocity.Y > MaxFallSpeed)
                    _velocity.Y = MaxFallSpeed;
            }
            
            // 分别处理X轴和Y轴的移动，避免穿墙
            // 先处理水平移动
            _position.X += _velocity.X * deltaTime;
            UpdateBounds();
            HandleHorizontalCollisions();
            
            // 再处理垂直移动
            _position.Y += _velocity.Y * deltaTime;
            UpdateBounds();
            HandleVerticalCollisions();
        }
        
        private void HandleCollisions()
        {
            // 限制玩家在世界边界内
            if (_position.X < PlayerWidth / 2)
            {
                _position.X = PlayerWidth / 2;
                _velocity.X = 0;
            }
            else if (_position.X > _world.Width * 16 - PlayerWidth / 2)
            {
                _position.X = _world.Width * 16 - PlayerWidth / 2;
                _velocity.X = 0;
            }
            
            if (_position.Y > _world.Height * 16)
            {
                // 玩家掉出世界，重置到地面
                _position.Y = FindGroundLevel(_position.X);
                _velocity.Y = 0;
            }
            
            UpdateBounds();
        }
        
        /// <summary>
        /// 处理水平方向的碰撞检测
        /// </summary>
        private void HandleHorizontalCollisions()
        {
            Rectangle playerRect = _bounds;
            
            // 获取玩家周围的瓦片
            int leftTile = Math.Max(0, playerRect.Left / 16);
            int rightTile = Math.Min(_world.Width - 1, playerRect.Right / 16);
            int topTile = Math.Max(0, playerRect.Top / 16);
            int bottomTile = Math.Min(_world.Height - 1, playerRect.Bottom / 16);
            
            for (int x = leftTile; x <= rightTile; x++)
            {
                for (int y = topTile; y <= bottomTile; y++)
                {
                    if (_world.GetTile(x, y).Type != TileType.Air)
                    {
                        Rectangle tileRect = new Rectangle(x * 16, y * 16, 16, 16);
                        
                        if (playerRect.Intersects(tileRect))
                        {
                            // 水平碰撞处理
                            if (_velocity.X > 0) // 向右移动
                            {
                                _position.X = tileRect.Left - PlayerWidth / 2;
                            }
                            else if (_velocity.X < 0) // 向左移动
                            {
                                _position.X = tileRect.Right + PlayerWidth / 2;
                            }
                            
                            _velocity.X = 0;
                            UpdateBounds();
                            return; // 处理完一个碰撞就退出，避免多重修正
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 处理垂直方向的碰撞检测
        /// </summary>
        private void HandleVerticalCollisions()
        {
            _isOnGround = false; // 重置地面状态
            Rectangle playerRect = _bounds;
            
            // 获取玩家周围的瓦片
            int leftTile = Math.Max(0, playerRect.Left / 16);
            int rightTile = Math.Min(_world.Width - 1, playerRect.Right / 16);
            int topTile = Math.Max(0, playerRect.Top / 16);
            int bottomTile = Math.Min(_world.Height - 1, playerRect.Bottom / 16);
            
            for (int x = leftTile; x <= rightTile; x++)
            {
                for (int y = topTile; y <= bottomTile; y++)
                {
                    if (_world.GetTile(x, y).Type != TileType.Air)
                    {
                        Rectangle tileRect = new Rectangle(x * 16, y * 16, 16, 16);
                        
                        if (playerRect.Intersects(tileRect))
                        {
                            // 垂直碰撞处理
                            if (_velocity.Y > 0) // 向下移动
                            {
                                _position.Y = tileRect.Top - PlayerHeight / 2;
                                _velocity.Y = 0;
                                _isOnGround = true;
                            }
                            else if (_velocity.Y < 0) // 向上移动
                            {
                                _position.Y = tileRect.Bottom + PlayerHeight / 2;
                                _velocity.Y = 0;
                            }
                            
                            UpdateBounds();
                            return; // 处理完一个碰撞就退出，避免多重修正
                        }
                    }
                }
            }
        }
        
        private float FindGroundLevel(float x)
        {
            int tileX = (int)(x / 16);
            if (tileX < 0 || tileX >= _world.Width) return 0;
            
            // 从上往下找第一个非空气方块
            for (int y = 0; y < _world.Height; y++)
            {
                if (_world.GetTile(tileX, y).Type != TileType.Air)
                {
                    return y * 16 - PlayerHeight / 2;
                }
            }
            
            return _world.Height * 16 - PlayerHeight / 2;
        }
        
        public void Draw(SpriteBatch spriteBatch)
        {
            // 获取动画缩放比例
            float scale = GetAnimationScale();
            
            // 根据缩放比例调整绘制大小
            int scaledWidth = (int)(PlayerWidth * scale);
            int scaledHeight = (int)(PlayerHeight * scale);
            
            Rectangle drawRect = new Rectangle(
                (int)(_position.X - scaledWidth / 2),
                (int)(_position.Y - scaledHeight / 2),
                scaledWidth,
                scaledHeight
            );
            
            // 确定是否需要翻转动画
            SpriteEffects spriteEffects = ShouldFlipAnimation() ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            
            // 使用动画系统渲染
            if (_animator != null)
            {
                var currentFrame = _animator.GetCurrentFrame();
                if (currentFrame != null)
                {
                    spriteBatch.Draw(currentFrame, drawRect, null, Color.White, 0f, Vector2.Zero, spriteEffects, 0f);
                    return;
                }
            }
            
            // 后备渲染（如果动画系统失败）
            if (_texture != null)
            {
                spriteBatch.Draw(_texture, drawRect, null, _color, 0f, Vector2.Zero, spriteEffects, 0f);
            }
        }
        
        /// <summary>
        /// 在指定位置生成玩家（寻找合适的地面位置）
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <returns>生成的玩家位置</returns>
        public Vector2 SpawnAt(float x)
        {
            float groundY = FindGroundLevel(x);
            _position = new Vector2(x, groundY);
            _velocity = Vector2.Zero;
            UpdateBounds();
            return _position;
        }
        
        /// <summary>
        /// 检查玩家是否可以跳跃（脚下5像素内有地面）
        /// </summary>
        /// <returns>是否可以跳跃</returns>
        private bool CanJump()
        {
            // 检查玩家脚下5像素范围内是否有地面
            Rectangle playerRect = _bounds;
            int checkDistance = 5; // 检查距离（像素）
            
            // 扩展检查区域到玩家脚下5像素
            Rectangle checkRect = new Rectangle(
                playerRect.Left,
                playerRect.Bottom,
                playerRect.Width,
                checkDistance
            );
            
            // 获取检查区域内的瓦片
            int leftTile = Math.Max(0, checkRect.Left / 16);
            int rightTile = Math.Min(_world.Width - 1, checkRect.Right / 16);
            int topTile = Math.Max(0, checkRect.Top / 16);
            int bottomTile = Math.Min(_world.Height - 1, checkRect.Bottom / 16);
            
            // 检查是否有固体瓦片
            for (int x = leftTile; x <= rightTile; x++)
            {
                for (int y = topTile; y <= bottomTile; y++)
                {
                    if (_world.GetTile(x, y).Type != TileType.Air)
                    {
                        Rectangle tileRect = new Rectangle(x * 16, y * 16, 16, 16);
                        if (checkRect.Intersects(tileRect))
                        {
                            return true; // 找到地面，可以跳跃
                        }
                    }
                }
            }
            
            return false; // 没有找到地面，不能跳跃
        }
    }
}