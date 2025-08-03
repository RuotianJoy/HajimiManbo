using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HajimiManbo.GameStates
{
    // 角色数据结构
    public class CharacterData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
        
        [JsonPropertyName("story")]
        public string Story { get; set; } = "";
        
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
        
        [JsonPropertyName("color")]
        public int[] Color { get; set; } = new int[3];
        
        [JsonPropertyName("stats")]
        public CharacterStats Stats { get; set; } = new CharacterStats();
        
        [JsonPropertyName("gameplay")]
        public CharacterGameplay Gameplay { get; set; } = new CharacterGameplay();
        
        [JsonPropertyName("animation")]
        public CharacterAnimation Animation { get; set; } = new CharacterAnimation();
    }
    
    public class CharacterStats
    {
        [JsonPropertyName("attack_power")]
        public int AttackPower { get; set; }
        
        [JsonPropertyName("speed_multiplier")]
        public float SpeedMultiplier { get; set; }
        
        [JsonPropertyName("defense")]
        public int Defense { get; set; }
        
        [JsonPropertyName("jump_multiplier")]
        public float JumpMultiplier { get; set; }
        
        [JsonPropertyName("max_health")]
        public int MaxHealth { get; set; }
        
        [JsonPropertyName("star_ratings")]
        public StarRatings StarRatings { get; set; } = new StarRatings();
    }
    
    public class StarRatings
    {
        [JsonPropertyName("attack")]
        public int Attack { get; set; }
        
        [JsonPropertyName("speed")]
        public int Speed { get; set; }
        
        [JsonPropertyName("defense")]
        public int Defense { get; set; }
        
        [JsonPropertyName("jump")]
        public int Jump { get; set; }
    }
    
    public class CharacterGameplay
    {
        [JsonPropertyName("character_class")]
        public string CharacterClass { get; set; } = "";
        
        [JsonPropertyName("special_abilities")]
        public string[] SpecialAbilities { get; set; } = new string[0];
        
        [JsonPropertyName("weapon")]
        public string Weapon { get; set; } = "";
    }
    
    public class CharacterAnimation
    {
        [JsonPropertyName("folder_name")]
        public string FolderName { get; set; } = "";
        
        [JsonPropertyName("idle_animation")]
        public string IdleAnimation { get; set; } = "noMoveAnimation";
        
        [JsonPropertyName("frame_time")]
        public float FrameTime { get; set; } = 0.1f;
    }
    
    public class CharacterIndex
    {
        [JsonPropertyName("characters")]
        public CharacterIndexEntry[] Characters { get; set; } = new CharacterIndexEntry[0];
    }
    
    public class CharacterIndexEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        
        [JsonPropertyName("file")]
        public string File { get; set; } = "";
        
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
        
        [JsonPropertyName("default")]
        public bool Default { get; set; }
    }
    
    // 武器数据结构
    public class WeaponData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
        
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
        
        [JsonPropertyName("rarity")]
        public string Rarity { get; set; } = "";
        
        [JsonPropertyName("stats")]
        public WeaponStats Stats { get; set; } = new WeaponStats();
        
        [JsonPropertyName("special_abilities")]
        public string[] SpecialAbilities { get; set; } = new string[0];
        
        [JsonPropertyName("visual")]
        public WeaponVisual Visual { get; set; } = new WeaponVisual();
    }
    
    public class WeaponStats
    {
        [JsonPropertyName("damage")]
        public int Damage { get; set; }
        
        [JsonPropertyName("attack_speed")]
        public float AttackSpeed { get; set; }
        
        [JsonPropertyName("range")]
        public int Range { get; set; }
        
        [JsonPropertyName("knockback")]
        public float Knockback { get; set; }
        
        [JsonPropertyName("critical_chance")]
        public int CriticalChance { get; set; }
    }
    
    public class WeaponVisual
    {
        [JsonPropertyName("sprite_path")]
        public string SpritePath { get; set; } = "";
        
        [JsonPropertyName("projectile_sprite")]
        public string ProjectileSprite { get; set; } = "";
        
        [JsonPropertyName("swing_animation")]
        public string SwingAnimation { get; set; } = "";
        
        [JsonPropertyName("color_scheme")]
        public int[] ColorScheme { get; set; } = new int[0];
    }
    
    public class WeaponIndex
    {
        [JsonPropertyName("weapons")]
        public WeaponIndexEntry[] Weapons { get; set; } = new WeaponIndexEntry[0];
    }
    
    public class WeaponIndexEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        
        [JsonPropertyName("file")]
        public string File { get; set; } = "";
        
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
        
        [JsonPropertyName("category")]
        public string Category { get; set; } = "";
    }

    /// <summary>
    /// 角色选择界面：显示角色信息、属性、武器装备等
    /// </summary>
    public class CharacterSelectionState : GameState
    {
        private readonly Texture2D backgroundTexture;
        private readonly Texture2D secondBackgroundTexture;
        private readonly Rectangle screenBounds;
        private readonly Texture2D pixelTexture;
        
        // 角色数据
        private List<CharacterData> characters = new List<CharacterData>();
        private readonly string[] statNames = { "攻击力", "速度", "防御力", "跳跃力" };
        private readonly string characterDataPath = "Content/Character/";
        
        // 角色动画数据
        private Dictionary<string, Texture2D[]> characterAnimationFrames = new Dictionary<string, Texture2D[]>();
        private float animationTimer = 0f;
        private const float ANIMATION_FRAME_TIME = 0.1f; // 每帧0.1秒
        
        // 武器数据
        private Dictionary<string, WeaponData> weapons = new Dictionary<string, WeaponData>();
        private Dictionary<string, Texture2D> weaponTextures = new Dictionary<string, Texture2D>();
        private Dictionary<string, Texture2D> projectileTextures = new Dictionary<string, Texture2D>();
        private bool showProjectileTexture = false; // 是否显示弹幕贴图
        private readonly string weaponDataPath = "Content/Weapon/";
        private readonly ContentManager contentManager;
        
        // 武器名称映射（从JSON加载后动态填充）
        private readonly Dictionary<string, string> weaponNames = new Dictionary<string, string>
        {
            { "", "默认武器" }
        };
        
        // 武器描述映射（从JSON加载后动态填充）
        private readonly Dictionary<string, string> weaponDescriptions = new Dictionary<string, string>
        {
            { "", "标准攻击武器" }
        };
        
        // UI状态
        private int selectedCharacterIndex = 0;
        private KeyboardState prevKeyboardState;
        private MouseState prevMouseState;
        
        // UI布局
        private Rectangle characterPortraitRect;
        private Rectangle characterInfoRect;
        private Rectangle weaponInfoRect;
        private Rectangle[] navigationButtonRects;
        private Rectangle confirmButtonRect;
        private Rectangle backButtonRect;
        
        public CharacterSelectionState(Game1 game,
                                     GraphicsDeviceManager graphics,
                                     SpriteBatch spriteBatch,
                                     SpriteFont font,
                                     Texture2D backgroundTexture,
                                     Texture2D secondBackgroundTexture)
            : base(game, graphics, spriteBatch, font)
        {
            this.backgroundTexture = backgroundTexture;
            this.secondBackgroundTexture = secondBackgroundTexture;
            this.contentManager = game.Content;
            screenBounds = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            
            // 创建像素纹理
            pixelTexture = new Texture2D(graphics.GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
            
            // 初始化状态
            prevKeyboardState = Keyboard.GetState();
            prevMouseState = Mouse.GetState();
            
            // 加载武器数据
            LoadWeaponData();
            
            // 加载角色数据
            LoadCharacterData();
            
            // 计算UI布局
            CalculateLayout();
        }
        
        public override void Update(GameTime gameTime)
        {
            // 更新动画计时器
            animationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            KeyboardState kb = Keyboard.GetState();
            MouseState mouse = Mouse.GetState();
            
            // 键盘导航
            if (characters.Count > 0)
            {
                if (kb.IsKeyDown(Keys.Left) && !prevKeyboardState.IsKeyDown(Keys.Left))
                {
                    selectedCharacterIndex = (selectedCharacterIndex - 1 + characters.Count) % characters.Count;
                }
                else if (kb.IsKeyDown(Keys.Right) && !prevKeyboardState.IsKeyDown(Keys.Right))
                {
                    selectedCharacterIndex = (selectedCharacterIndex + 1) % characters.Count;
                }
            }
            
            // 鼠标点击处理
            if (mouse.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released)
            {
                // 检查导航按钮
                if (navigationButtonRects != null && characters.Count > 0)
                {
                    if (navigationButtonRects[0].Contains(mouse.Position)) // 左箭头
                    {
                        selectedCharacterIndex = (selectedCharacterIndex - 1 + characters.Count) % characters.Count;
                    }
                    else if (navigationButtonRects[1].Contains(mouse.Position)) // 右箭头
                    {
                        selectedCharacterIndex = (selectedCharacterIndex + 1) % characters.Count;
                    }
                }
                
                // 检查确认按钮
                if (confirmButtonRect.Contains(mouse.Position))
                {
                    // 确认选择角色，保存到NetworkManager并返回等待房间
                    if (characters.Count > 0 && selectedCharacterIndex < characters.Count)
                    {
                        var selectedCharacter = characters[selectedCharacterIndex];
                        HajimiManbo.Network.NetworkManager.Instance.SetLocalPlayerCharacter(selectedCharacter.Id);
                    }
                    game.ReturnToWaitingRoom();
                }
                
                // 检查返回按钮
                if (backButtonRect.Contains(mouse.Position))
                {
                    game.ReturnToWaitingRoom();
                }
                
                // 检查武器图片点击（切换显示模式）
                if (characters.Count > 0 && selectedCharacterIndex < characters.Count)
                {
                    var currentCharacter = characters[selectedCharacterIndex];
                    string weaponKey = currentCharacter.Gameplay.Weapon ?? "";
                    
                    // 计算武器图片区域（与DrawWeaponInfo中的逻辑保持一致）
                    float scale = UIScaleManager.UniformScale * 1.2f;
                    Vector2 titleSize = font.MeasureString("武器装备");
                    float weaponImageY = weaponInfoRect.Y + 20 + titleSize.Y * scale + 20;
                    
                    Rectangle weaponClickArea;
                    if ((showProjectileTexture && projectileTextures.ContainsKey(weaponKey)) || 
                        (!showProjectileTexture && weaponTextures.ContainsKey(weaponKey)))
                    {
                        // 有贴图时的点击区域
                        float maxImageSize;
                        if (showProjectileTexture)
                        {
                            maxImageSize = 120 * UIScaleManager.UniformScale; // 弹幕贴图限制为120*120
                        }
                        else
                        {
                            maxImageSize = 280 * UIScaleManager.UniformScale; // 武器贴图改为280
                        }
                        var texture = showProjectileTexture ? projectileTextures[weaponKey] : weaponTextures[weaponKey];
                        float imageScale = Math.Min(maxImageSize / texture.Width, maxImageSize / texture.Height);
                        int imageWidth = (int)(texture.Width * imageScale);
                        int imageHeight = (int)(texture.Height * imageScale);
                        
                        weaponClickArea = new Rectangle(
                            weaponInfoRect.X + (weaponInfoRect.Width - imageWidth) / 2,
                            (int)(weaponImageY + (280 * UIScaleManager.UniformScale - imageHeight) / 2), // 在武器区域中心
                            imageWidth,
                            imageHeight
                        );
                    }
                    else
                    {
                        // 占位符的点击区域
                        weaponClickArea = new Rectangle(
                            weaponInfoRect.X + (weaponInfoRect.Width - 60) / 2,
                            (int)(weaponImageY + (280 * UIScaleManager.UniformScale - 60) / 2),
                            60,
                            60
                        );
                    }
                    
                    if (weaponClickArea.Contains(mouse.Position))
                    {
                        // 切换显示模式
                        showProjectileTexture = !showProjectileTexture;
                    }
                }
            }
            
            // ESC键返回
            if (kb.IsKeyDown(Keys.Escape) && !prevKeyboardState.IsKeyDown(Keys.Escape))
            {
                game.ReturnToWaitingRoom();
            }
            
            // Enter键确认
            if (kb.IsKeyDown(Keys.Enter) && !prevKeyboardState.IsKeyDown(Keys.Enter))
            {
                // 确认选择角色，保存到NetworkManager并返回等待房间
                if (characters.Count > 0 && selectedCharacterIndex < characters.Count)
                {
                    var selectedCharacter = characters[selectedCharacterIndex];
                    HajimiManbo.Network.NetworkManager.Instance.SetLocalPlayerCharacter(selectedCharacter.Id);
                }
                game.ReturnToWaitingRoom();
            }
            
            prevKeyboardState = kb;
            prevMouseState = mouse;
        }
        
        public override void Draw(GameTime gameTime)
        {
            // 绘制房间背景
            spriteBatch.Draw(backgroundTexture, screenBounds, Color.White);
            
            DrawTitle();
            DrawCharacterPortrait();
            DrawCharacterInfo();
            DrawWeaponInfo();
            DrawNavigationButtons();
            DrawActionButtons();
        }
        
        private void CalculateLayout()
        {
            // 角色头像区域 (左侧)
            Vector2 portraitPos = UIScaleManager.GetRelativePosition(0.2f, 0.5f);
            Vector2 portraitSize = UIScaleManager.GetRelativeSize(0.25f, 0.6f);
            characterPortraitRect = new Rectangle(
                (int)(portraitPos.X - portraitSize.X / 2),
                (int)(portraitPos.Y - portraitSize.Y / 2),
                (int)portraitSize.X,
                (int)portraitSize.Y
            );
            
            // 角色信息区域 (中间)
            Vector2 infoPos = UIScaleManager.GetRelativePosition(0.5f, 0.5f);
            Vector2 infoSize = UIScaleManager.GetRelativeSize(0.3f, 0.6f);
            characterInfoRect = new Rectangle(
                (int)(infoPos.X - infoSize.X / 2),
                (int)(infoPos.Y - infoSize.Y / 2),
                (int)infoSize.X,
                (int)infoSize.Y
            );
            
            // 武器信息区域 (右侧)
            Vector2 weaponPos = UIScaleManager.GetRelativePosition(0.8f, 0.5f);
            Vector2 weaponSize = UIScaleManager.GetRelativeSize(0.25f, 0.6f);
            weaponInfoRect = new Rectangle(
                (int)(weaponPos.X - weaponSize.X / 2),
                (int)(weaponPos.Y - weaponSize.Y / 2),
                (int)weaponSize.X,
                (int)weaponSize.Y
            );
            
            // 导航按钮 (左侧在角色区域左边，右侧在武器区域右边)
            navigationButtonRects = new Rectangle[2];
            Vector2 leftArrowPos = UIScaleManager.GetRelativePosition(0.05f, 0.5f);
            Vector2 rightArrowPos = UIScaleManager.GetRelativePosition(0.95f, 0.5f);
            Vector2 arrowSize = UIScaleManager.GetRelativeSize(0.05f, 0.05f);
            
            navigationButtonRects[0] = new Rectangle(
                (int)(leftArrowPos.X - arrowSize.X / 2),
                (int)(leftArrowPos.Y - arrowSize.Y / 2),
                (int)arrowSize.X,
                (int)arrowSize.Y
            );
            
            navigationButtonRects[1] = new Rectangle(
                (int)(rightArrowPos.X - arrowSize.X / 2),
                (int)(rightArrowPos.Y - arrowSize.Y / 2),
                (int)arrowSize.X,
                (int)arrowSize.Y
            );
            
            // 确认和返回按钮
            Vector2 confirmPos = UIScaleManager.GetRelativePosition(0.4f, 0.9f);
            Vector2 backPos = UIScaleManager.GetRelativePosition(0.6f, 0.9f);
            Vector2 buttonSize = UIScaleManager.GetRelativeSize(0.15f, 0.06f);
            
            confirmButtonRect = new Rectangle(
                (int)(confirmPos.X - buttonSize.X / 2),
                (int)(confirmPos.Y - buttonSize.Y / 2),
                (int)buttonSize.X,
                (int)buttonSize.Y
            );
            
            backButtonRect = new Rectangle(
                (int)(backPos.X - buttonSize.X / 2),
                (int)(backPos.Y - buttonSize.Y / 2),
                (int)buttonSize.X,
                (int)buttonSize.Y
            );
        }
        
        private void DrawTitle()
        {
            string title = "选择角色";
            float scale = UIScaleManager.UniformScale * 3f;
            Vector2 size = font.MeasureString(title);
            Vector2 pos = UIScaleManager.GetRelativePosition(0.5f, 0.1f);
            pos.X -= size.X * scale / 2f;
            pos.Y -= size.Y * scale / 2f;
            
            // 绘制标题背景
            Rectangle titleBg = new Rectangle(
                (int)(pos.X - 20),
                (int)(pos.Y - 10),
                (int)(size.X * scale + 40),
                (int)(size.Y * scale + 20)
            );
            spriteBatch.Draw(pixelTexture, titleBg, Color.Gold * 0.3f);
            DrawRectangleBorder(titleBg, Color.Gold, 2);
            
            spriteBatch.DrawString(font, title, pos, Color.Gold, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        
        private void DrawCharacterPortrait()
        {
            // 绘制角色头像背景
            spriteBatch.Draw(pixelTexture, characterPortraitRect, new Color(40, 40, 80, 128));
            DrawRectangleBorder(characterPortraitRect, Color.Gold, 3);
            
            // 绘制角色动画
            if (characters.Count > 0 && selectedCharacterIndex < characters.Count)
            {
                var currentCharacter = characters[selectedCharacterIndex];
                string characterId = currentCharacter.Id;
                
                if (characterAnimationFrames.ContainsKey(characterId) && characterAnimationFrames[characterId].Length > 0)
                {
                    // 计算当前帧索引
                    var frames = characterAnimationFrames[characterId];
                    float frameTime = currentCharacter.Animation.FrameTime > 0 ? currentCharacter.Animation.FrameTime : ANIMATION_FRAME_TIME;
                    int frameIndex = (int)(animationTimer / frameTime) % frames.Length;
                    var currentFrame = frames[frameIndex];
                    
                    // 计算动画显示区域（在角色框内居中，留出底部名称空间）
                    int animationAreaHeight = characterPortraitRect.Height - 80; // 留出底部名称空间
                    int maxSize = Math.Min(characterPortraitRect.Width - 40, animationAreaHeight); // 左右边距20
                    
                    // 计算缩放比例以适应显示区域
                    float scaleX = (float)maxSize / currentFrame.Width;
                    float scaleY = (float)maxSize / currentFrame.Height;
                    float scale = Math.Min(scaleX, scaleY);
                    
                    // 计算绘制尺寸和位置
                    int drawWidth = (int)(currentFrame.Width * scale);
                    int drawHeight = (int)(currentFrame.Height * scale);
                    
                    Vector2 animationPos = new Vector2(
                        characterPortraitRect.X + (characterPortraitRect.Width - drawWidth) / 2,
                        characterPortraitRect.Y + (animationAreaHeight - drawHeight) / 2 + 20
                    );
                    
                    Rectangle animationRect = new Rectangle(
                        (int)animationPos.X,
                        (int)animationPos.Y,
                        drawWidth,
                        drawHeight
                    );
                    
                    // 绘制当前动画帧
                    spriteBatch.Draw(currentFrame, animationRect, Color.White);
                }
                else
                {
                    // 如果没有动画帧，绘制占位符
                    Vector2 center = new Vector2(
                        characterPortraitRect.X + characterPortraitRect.Width / 2,
                        characterPortraitRect.Y + (characterPortraitRect.Height - 80) / 2 + 20
                    );
                    int radius = Math.Min(characterPortraitRect.Width, characterPortraitRect.Height - 80) / 4;
                    
                    // 绘制圆形占位符
                    for (int x = -radius; x <= radius; x++)
                    {
                        for (int y = -radius; y <= radius; y++)
                        {
                            if (x * x + y * y <= radius * radius)
                            {
                                Vector2 pixelPos = center + new Vector2(x, y);
                                Rectangle pixelRect = new Rectangle((int)pixelPos.X, (int)pixelPos.Y, 1, 1);
                                spriteBatch.Draw(pixelTexture, pixelRect, Color.SandyBrown);
                            }
                        }
                    }
                }
            }
            
            // 绘制角色名称（在框内底部）
            if (characters.Count > 0 && selectedCharacterIndex < characters.Count)
            {
                string characterName = characters[selectedCharacterIndex].Name;
                float nameScale = UIScaleManager.UniformScale * 1.8f;
                Vector2 nameSize = font.MeasureString(characterName);
                
                // 计算名称框的位置（在角色框内底部）
                int nameBoxHeight = (int)(nameSize.Y * nameScale + 10);
                Rectangle nameBg = new Rectangle(
                    characterPortraitRect.X + 10,
                    characterPortraitRect.Y + characterPortraitRect.Height - nameBoxHeight - 10,
                    characterPortraitRect.Width - 20,
                    nameBoxHeight
                );
                
                // 绘制名称背景
                spriteBatch.Draw(pixelTexture, nameBg, Color.Gold * 0.9f);
                DrawRectangleBorder(nameBg, Color.Gold, 2);
                
                // 计算文字居中位置
                Vector2 namePos = new Vector2(
                    nameBg.X + (nameBg.Width - nameSize.X * nameScale) / 2,
                    nameBg.Y + (nameBg.Height - nameSize.Y * nameScale) / 2
                );
                
                spriteBatch.DrawString(font, characterName, namePos, Color.Black, 0f, Vector2.Zero, nameScale, SpriteEffects.None, 0f);
            }
        }
        
        private void DrawCharacterInfo()
        {
            // 绘制角色信息背景
            spriteBatch.Draw(pixelTexture, characterInfoRect, new Color(40, 40, 80, 128));
            DrawRectangleBorder(characterInfoRect, Color.Gold, 2);
            
            float scale = UIScaleManager.UniformScale * 1.3f;
            Vector2 startPos = new Vector2(characterInfoRect.X + 20, characterInfoRect.Y + 20);
            
            // 绘制"角色详情"标题
            string infoTitle = "角色详情";
            Vector2 titleSize = font.MeasureString(infoTitle);
            Vector2 titlePos = new Vector2(
                characterInfoRect.X + (characterInfoRect.Width - titleSize.X * scale) / 2,
                startPos.Y
            );
            
            // 绘制标题背景
            Rectangle titleBg = new Rectangle(
                (int)(titlePos.X - 10),
                (int)(titlePos.Y - 5),
                (int)(titleSize.X * scale + 20),
                (int)(titleSize.Y * scale + 10)
            );
            spriteBatch.Draw(pixelTexture, titleBg, Color.Gold * 0.3f);
            
            spriteBatch.DrawString(font, infoTitle, titlePos, Color.Gold, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            
            if (characters.Count > 0 && selectedCharacterIndex < characters.Count)
            {
                var currentCharacter = characters[selectedCharacterIndex];
                
                // 绘制角色描述
                string description = currentCharacter.Description;
                Vector2 descPos = new Vector2(startPos.X, titlePos.Y + titleSize.Y * scale + 20);
                spriteBatch.DrawString(font, description, descPos, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                
                // 绘制属性
                Vector2 statStartPos = new Vector2(startPos.X, descPos.Y + font.LineSpacing * scale + 20);
                
                // 获取星级评分
                int[] statValues = {
                    currentCharacter.Stats.StarRatings.Attack,
                    currentCharacter.Stats.StarRatings.Speed,
                    currentCharacter.Stats.StarRatings.Defense,
                    currentCharacter.Stats.StarRatings.Jump
                };
                
                for (int i = 0; i < statNames.Length && i < statValues.Length; i++)
                {
                    Vector2 statPos = new Vector2(statStartPos.X, statStartPos.Y + i * (font.LineSpacing * scale + 10));
                    
                    // 绘制属性名称
                    string statName = statNames[i];
                    spriteBatch.DrawString(font, statName, statPos, Color.LightBlue, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    
                    // 绘制星级
                    int statValue = statValues[i];
                    Vector2 starPos = new Vector2(statPos.X + 280 * UIScaleManager.UniformScale, statPos.Y);
                    
                    for (int j = 0; j < 5; j++)
                    {
                        Color starColor = j < statValue ? Color.Gold : Color.Gray;
                        string star = "★";
                        spriteBatch.DrawString(font, star, starPos, starColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                        starPos.X += font.MeasureString(star).X * scale;
                    }
                }
                
                // 绘制角色故事（在四个数值下方）
                if (!string.IsNullOrEmpty(currentCharacter.Story))
                {
                    float storyScale = UIScaleManager.UniformScale * 1.3f;
                    string storyText = $"\"{currentCharacter.Story}\"";
                    
                    // 计算故事文字的位置（在四个数值下方）
                    Vector2 storyPos = new Vector2(
                        characterInfoRect.X + (20 * UIScaleManager.UniformScale),
                        statStartPos.Y + 5 * (font.LineSpacing * scale + (10 * UIScaleManager.UniformScale)) + (20 * UIScaleManager.UniformScale)
                    );
                    
                    // 绘制故事文字
                    spriteBatch.DrawString(font, storyText, storyPos, Color.Red, 0f, Vector2.Zero, storyScale, SpriteEffects.None, 0f);
                }
            }
            
            // 绘制角色索引（在详情框下方）
            if (characters.Count > 0)
            {
                string indexText = $"{selectedCharacterIndex + 1} / {characters.Count}";
                float indexScale = UIScaleManager.UniformScale * 1.2f;
                Vector2 indexSize = font.MeasureString(indexText);
                
                // 计算索引框的位置（在详情框下方）
                int indexBoxHeight = (int)(indexSize.Y * indexScale + 10);
                Rectangle indexBg = new Rectangle(
                    characterInfoRect.X + (characterInfoRect.Width - 100) / 2,
                    characterInfoRect.Y + characterInfoRect.Height + 10,
                    100,
                    indexBoxHeight
                );
                
                //// 绘制索引边框（无背景）
                //DrawRectangleBorder(indexBg, Color.Gold, 2);
                
                // 计算索引文字居中位置
                Vector2 indexPos = new Vector2(
                    indexBg.X + (indexBg.Width - indexSize.X * indexScale) / 2,
                    indexBg.Y + (indexBg.Height - indexSize.Y * indexScale) / 2
                );
                
                spriteBatch.DrawString(font, indexText, indexPos, Color.Gold, 0f, Vector2.Zero, indexScale, SpriteEffects.None, 0f);
            }
        }
        
        private void DrawWeaponInfo()
        {
            // 绘制武器信息背景
            spriteBatch.Draw(pixelTexture, weaponInfoRect, new Color(40, 40, 80, 128));
            DrawRectangleBorder(weaponInfoRect, Color.Gold, 2);
            
            float scale = UIScaleManager.UniformScale * 1.2f;
            Vector2 startPos = new Vector2(weaponInfoRect.X + 20, weaponInfoRect.Y + 20);
            
            // 绘制"武器装备"标题
            string weaponTitle = "武器装备";
            Vector2 titleSize = font.MeasureString(weaponTitle);
            Vector2 titlePos = new Vector2(
                weaponInfoRect.X + (weaponInfoRect.Width - titleSize.X * scale) / 2,
                startPos.Y
            );
            
            spriteBatch.DrawString(font, weaponTitle, titlePos, Color.Gold, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            
            if (characters.Count > 0 && selectedCharacterIndex < characters.Count)
            {
                var currentCharacter = characters[selectedCharacterIndex];
                string weaponKey = currentCharacter.Gameplay.Weapon ?? "";
                
                // 绘制武器贴图（在标题下面）
                float weaponImageY = titlePos.Y + titleSize.Y * scale + 20;
                
                // 根据显示状态选择贴图
                Texture2D currentTexture = null;
                if (showProjectileTexture && projectileTextures.ContainsKey(weaponKey))
                {
                    currentTexture = projectileTextures[weaponKey];
                }
                else if (!showProjectileTexture && weaponTextures.ContainsKey(weaponKey))
                {
                    currentTexture = weaponTextures[weaponKey];
                }
                
                // 定义固定的图片区域大小（以武器贴图为基准）
                float weaponAreaHeight = 280 * UIScaleManager.UniformScale + 15; // 武器区域高度
                
                if (currentTexture != null)
                {
                    // 计算贴图大小和位置（居中显示，限制最大尺寸）
                    float maxImageSize;
                    if (showProjectileTexture)
                    {
                        maxImageSize = 120 * UIScaleManager.UniformScale; // 弹幕贴图限制为120*120
                    }
                    else
                    {
                        maxImageSize = 280 * UIScaleManager.UniformScale; // 武器贴图改为280
                    }
                    float imageScale = Math.Min(maxImageSize / currentTexture.Width, maxImageSize / currentTexture.Height);
                    
                    int imageWidth = (int)(currentTexture.Width * imageScale);
                    int imageHeight = (int)(currentTexture.Height * imageScale);
                    
                    // 弹幕贴图在武器区域中心显示
                    Rectangle weaponImageRect = new Rectangle(
                        weaponInfoRect.X + (weaponInfoRect.Width - imageWidth) / 2,
                        (int)(weaponImageY + (280 * UIScaleManager.UniformScale - imageHeight) / 2), // 在武器区域中心
                        imageWidth,
                        imageHeight
                    );
                    
                    spriteBatch.Draw(currentTexture, weaponImageRect, Color.White);
                }
                else
                {
                    // 如果没有贴图，显示占位符（在武器区域中心）
                    Rectangle placeholderRect = new Rectangle(
                        weaponInfoRect.X + (weaponInfoRect.Width - 60) / 2,
                        (int)(weaponImageY + (280 * UIScaleManager.UniformScale - 60) / 2),
                        60,
                        60
                    );
                    spriteBatch.Draw(pixelTexture, placeholderRect, Color.Gray * 0.3f);
                    DrawRectangleBorder(placeholderRect, Color.Gray, 1);
                }
                
                // 固定文字位置（武器区域下方）
                weaponImageY += weaponAreaHeight;
                
                // 绘制武器名称（根据显示模式）
                string weaponName;
                string weaponDesc;
                if (showProjectileTexture && projectileTextures.ContainsKey(weaponKey))
                {
                    // 显示弹幕模式
                    weaponName = (weaponNames.ContainsKey(weaponKey) ? weaponNames[weaponKey] : weaponNames[""]) + " - 弹幕";
                    weaponDesc = "武器发射的弹幕效果";
                }
                else
                {
                    // 显示武器模式
                    weaponName = weaponNames.ContainsKey(weaponKey) ? weaponNames[weaponKey] : weaponNames[""];
                    weaponDesc = weaponDescriptions.ContainsKey(weaponKey) ? weaponDescriptions[weaponKey] : weaponDescriptions[""];
                }
                
                Vector2 weaponNamePos = new Vector2(startPos.X, weaponImageY);
                spriteBatch.DrawString(font, weaponName, weaponNamePos, Color.LightPink, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                
                // 绘制武器描述（支持换行）
                Vector2 weaponDescPos = new Vector2(startPos.X, weaponNamePos.Y + font.LineSpacing * scale + 5);
                float descScale = scale * 0.8f;
                
                // 计算可用宽度（减去左右边距）
                float availableWidth = weaponInfoRect.Width - 40; // 左右边距
                
                // 改进的文字换行处理（适合中文）
                string currentLine = "";
                float currentY = weaponDescPos.Y;
                
                for (int i = 0; i < weaponDesc.Length; i++)
                {
                    char currentChar = weaponDesc[i];
                    string testLine = currentLine + currentChar;
                    Vector2 testSize = font.MeasureString(testLine) * descScale;
                    
                    if (testSize.X <= availableWidth)
                    {
                        currentLine = testLine;
                    }
                    else
                    {
                        // 绘制当前行
                        if (!string.IsNullOrEmpty(currentLine))
                        {
                            spriteBatch.DrawString(font, currentLine, new Vector2(weaponDescPos.X, currentY), Color.White, 0f, Vector2.Zero, descScale, SpriteEffects.None, 0f);
                            currentY += font.LineSpacing * descScale;
                        }
                        currentLine = currentChar.ToString();
                    }
                }
                
                // 绘制最后一行
                if (!string.IsNullOrEmpty(currentLine))
                {
                    spriteBatch.DrawString(font, currentLine, new Vector2(weaponDescPos.X, currentY), Color.White, 0f, Vector2.Zero, descScale, SpriteEffects.None, 0f);
                    currentY += font.LineSpacing * descScale;
                }
                
                // 绘制武器攻击力（固定位置）
                float fixedAttackY = weaponImageY + 200 * UIScaleManager.UniformScale; // 固定在武器名称和描述区域下方
                if (weapons.ContainsKey(weaponKey))
                {
                    var weapon = weapons[weaponKey];
                    string attackText = $"攻击力: {weapon.Stats.Damage}";
                    Vector2 attackPos = new Vector2(startPos.X, fixedAttackY);
                    spriteBatch.DrawString(font, attackText, attackPos, Color.Orange, 0f, Vector2.Zero, descScale, SpriteEffects.None, 0f);
                }
                else
                {
                    // 默认武器攻击力
                    string attackText = "攻击力: 50";
                    Vector2 attackPos = new Vector2(startPos.X, fixedAttackY);
                    spriteBatch.DrawString(font, attackText, attackPos, Color.Orange, 0f, Vector2.Zero, descScale, SpriteEffects.None, 0f);
                }
            }
            

        }
        
        private void DrawNavigationButtons()
        {
            MouseState mouse = Mouse.GetState();
            
            // 左箭头
            bool leftHovered = navigationButtonRects[0].Contains(mouse.Position);
            Color leftColor = leftHovered ? Color.Gold : Color.White;
            //spriteBatch.Draw(pixelTexture, navigationButtonRects[0], leftColor * 0.7f);
            //DrawRectangleBorder(navigationButtonRects[0], leftColor, 2);
            
            string leftArrow = "<";
            float arrowScale = UIScaleManager.UniformScale * 2f;
            Vector2 leftArrowSize = font.MeasureString(leftArrow);
            Vector2 leftArrowPos = new Vector2(
                navigationButtonRects[0].X + (navigationButtonRects[0].Width - leftArrowSize.X * arrowScale) / 2,
                navigationButtonRects[0].Y + (navigationButtonRects[0].Height - leftArrowSize.Y * arrowScale) / 2
            );
            spriteBatch.DrawString(font, leftArrow, leftArrowPos, leftColor, 0f, Vector2.Zero, arrowScale, SpriteEffects.None, 0f);
            
            // 右箭头
            bool rightHovered = navigationButtonRects[1].Contains(mouse.Position);
            Color rightColor = rightHovered ? Color.Gold : Color.White;
            //spriteBatch.Draw(pixelTexture, navigationButtonRects[1], rightColor * 0.7f);
            //DrawRectangleBorder(navigationButtonRects[1], rightColor, 2);
            
            string rightArrow = ">";
            Vector2 rightArrowSize = font.MeasureString(rightArrow);
            Vector2 rightArrowPos = new Vector2(
                navigationButtonRects[1].X + (navigationButtonRects[1].Width - rightArrowSize.X * arrowScale) / 2,
                navigationButtonRects[1].Y + (navigationButtonRects[1].Height - rightArrowSize.Y * arrowScale) / 2
            );
            spriteBatch.DrawString(font, rightArrow, rightArrowPos, rightColor, 0f, Vector2.Zero, arrowScale, SpriteEffects.None, 0f);
        }
        
        private void DrawActionButtons()
        {
            MouseState mouse = Mouse.GetState();
            
            // 确认按钮
            bool confirmHovered = confirmButtonRect.Contains(mouse.Position);
            Color confirmBgColor = confirmHovered ? Color.Green * 0.8f : Color.DarkGreen * 0.7f;
            Color confirmTextColor = confirmHovered ? Color.White : Color.LightGreen;
            
            spriteBatch.Draw(pixelTexture, confirmButtonRect, confirmBgColor);
            DrawRectangleBorder(confirmButtonRect, confirmTextColor, 2);
            
            string confirmText = "确认选择";
            float buttonScale = UIScaleManager.UniformScale * 1.3f;
            Vector2 confirmTextSize = font.MeasureString(confirmText);
            Vector2 confirmTextPos = new Vector2(
                confirmButtonRect.X + (confirmButtonRect.Width - confirmTextSize.X * buttonScale) / 2,
                confirmButtonRect.Y + (confirmButtonRect.Height - confirmTextSize.Y * buttonScale) / 2
            );
            spriteBatch.DrawString(font, confirmText, confirmTextPos, confirmTextColor, 0f, Vector2.Zero, buttonScale, SpriteEffects.None, 0f);
            
            // 返回按钮
            bool backHovered = backButtonRect.Contains(mouse.Position);
            Color backBgColor = backHovered ? Color.Red * 0.8f : Color.DarkRed * 0.7f;
            Color backTextColor = backHovered ? Color.White : Color.LightPink;
            
            spriteBatch.Draw(pixelTexture, backButtonRect, backBgColor);
            DrawRectangleBorder(backButtonRect, backTextColor, 2);
            
            string backText = "返回";
            Vector2 backTextSize = font.MeasureString(backText);
            Vector2 backTextPos = new Vector2(
                backButtonRect.X + (backButtonRect.Width - backTextSize.X * buttonScale) / 2,
                backButtonRect.Y + (backButtonRect.Height - backTextSize.Y * buttonScale) / 2
            );
            spriteBatch.DrawString(font, backText, backTextPos, backTextColor, 0f, Vector2.Zero, buttonScale, SpriteEffects.None, 0f);
        }
        
        private void DrawRectangleBorder(Rectangle rect, Color color, int thickness)
        {
            // 上边
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // 下边
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
            // 左边
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // 右边
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
        }
        
        private void LoadCharacterData()
        {
            try
            {
                // 获取应用程序目录
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string fullCharacterDataPath = Path.Combine(appDirectory, characterDataPath);
                
                // 读取角色索引文件
                string indexPath = Path.Combine(fullCharacterDataPath, "characters.json");
                Console.WriteLine($"[CharacterSelection] Looking for character index at: {indexPath}");
                
                if (!File.Exists(indexPath))
                {
                    Console.WriteLine($"[CharacterSelection] Character index file not found: {indexPath}");
                    Console.WriteLine($"[CharacterSelection] App directory: {appDirectory}");
                    Console.WriteLine($"[CharacterSelection] Character data path: {characterDataPath}");
                    LoadDefaultCharacters();
                    return;
                }
                
                string indexJson = File.ReadAllText(indexPath);
                var characterIndex = JsonSerializer.Deserialize<CharacterIndex>(indexJson);
                
                if (characterIndex?.Characters == null)
                {
                    Console.WriteLine("[CharacterSelection] Invalid character index format");
                    LoadDefaultCharacters();
                    return;
                }
                
                characters.Clear();
                
                // 加载每个角色的详细数据
                foreach (var entry in characterIndex.Characters)
                {
                    if (!entry.Enabled) continue;
                    
                    string characterPath = Path.Combine(fullCharacterDataPath, entry.File);
                    Console.WriteLine($"[CharacterSelection] Looking for character file: {characterPath}");
                    
                    if (!File.Exists(characterPath))
                    {
                        Console.WriteLine($"[CharacterSelection] Character file not found: {characterPath}");
                        continue;
                    }
                    
                    try
                    {
                        string characterJson = File.ReadAllText(characterPath);
                        var character = JsonSerializer.Deserialize<CharacterData>(characterJson);
                        
                        if (character != null)
                        {
                            characters.Add(character);
                            Console.WriteLine($"[CharacterSelection] Loaded character: {character.Name}");
                            
                            // 加载角色动画帧
                            LoadCharacterAnimationFrames(character);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CharacterSelection] Error loading character {entry.File}: {ex.Message}");
                    }
                }
                
                if (characters.Count == 0)
                {
                    Console.WriteLine("[CharacterSelection] No characters loaded, using defaults");
                    LoadDefaultCharacters();
                }
                else
                {
                    Console.WriteLine($"[CharacterSelection] Successfully loaded {characters.Count} characters");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CharacterSelection] Error loading character data: {ex.Message}");
                LoadDefaultCharacters();
            }
        }
        
        private void LoadDefaultCharacters()
        {
            // 如果JSON加载失败，使用默认角色数据
            characters.Clear();
            
            var defaultCharacter = new CharacterData
            {
                Id = "default",
                Name = "默认角色",
                Description = "默认角色描述",
                Type = "平衡型",
                Color = new int[] { 100, 100, 100 },
                Stats = new CharacterStats
                {
                    StarRatings = new StarRatings
                    {
                        Attack = 3,
                        Speed = 3,
                        Defense = 3,
                        Jump = 3
                    }
                },
                Gameplay = new CharacterGameplay
                {
                    Weapon = ""
                }
            };
            
            characters.Add(defaultCharacter);
            Console.WriteLine("[CharacterSelection] Loaded default character");
        }
        
        private void LoadCharacterAnimationFrames(CharacterData character)
        {
            try
            {
                if (character == null)
                {
                    Console.WriteLine($"[CharacterSelection] Character is null");
                    return;
                }
                
                // 获取动画文件夹名称，如果未指定则使用角色ID
                string folderName = !string.IsNullOrEmpty(character.Animation.FolderName) 
                    ? character.Animation.FolderName 
                    : character.Id;
                
                // 学习主页面的方式，直接使用帧编号加载动画
                string basePath = $"img/Character/{folderName}/{character.Animation.IdleAnimation}";
                
                Console.WriteLine($"[CharacterSelection] Looking for animation frames at: {basePath}");
                
                List<Texture2D> frames = new List<Texture2D>();
                
                // 尝试加载动画帧，从f000开始
                for (int i = 0; i < 50; i++) // 最多尝试50帧
                {
                    try
                    {
                        string framePath = $"{basePath}/{character.Animation.IdleAnimation}_f{i:000}";
                        var texture = contentManager.Load<Texture2D>(framePath);
                        frames.Add(texture);
                        Console.WriteLine($"[CharacterSelection] Loaded animation frame: {framePath}");
                    }
                    catch (Exception)
                    {
                        // 如果加载失败，说明没有更多帧了
                        if (i == 0)
                        {
                            Console.WriteLine($"[CharacterSelection] No animation frames found for: {basePath}");
                        }
                        break;
                    }
                }
                
                if (frames.Count > 0)
                {
                    characterAnimationFrames[character.Id] = frames.ToArray();
                    Console.WriteLine($"[CharacterSelection] Loaded {frames.Count} animation frames for character: {character.Id}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CharacterSelection] Error loading animation frames for {character?.Id}: {ex.Message}");
            }
        }
        
        private void LoadWeaponData()
        {
            try
            {
                // 获取应用程序目录
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string fullWeaponDataPath = Path.Combine(appDirectory, weaponDataPath);
                
                // 读取武器索引文件
                string indexPath = Path.Combine(fullWeaponDataPath, "weapons.json");
                Console.WriteLine($"[CharacterSelection] Looking for weapon index at: {indexPath}");
                
                if (!File.Exists(indexPath))
                {
                    Console.WriteLine($"[CharacterSelection] Weapon index file not found: {indexPath}");
                    return;
                }
                
                string indexJson = File.ReadAllText(indexPath);
                var weaponIndex = JsonSerializer.Deserialize<WeaponIndex>(indexJson);
                
                if (weaponIndex?.Weapons == null)
                {
                    Console.WriteLine("[CharacterSelection] Invalid weapon index format");
                    return;
                }
                
                weapons.Clear();
                weaponNames.Clear();
                weaponDescriptions.Clear();
                
                // 添加默认武器
                weaponNames[""] = "默认武器";
                weaponDescriptions[""] = "标准攻击武器";
                
                // 加载每个武器的详细数据
                foreach (var entry in weaponIndex.Weapons)
                {
                    if (!entry.Enabled) continue;
                    
                    string weaponPath = Path.Combine(fullWeaponDataPath, entry.File);
                    Console.WriteLine($"[CharacterSelection] Looking for weapon file: {weaponPath}");
                    
                    if (!File.Exists(weaponPath))
                    {
                        Console.WriteLine($"[CharacterSelection] Weapon file not found: {weaponPath}");
                        continue;
                    }
                    
                    try
                    {
                        string weaponJson = File.ReadAllText(weaponPath);
                        var weapon = JsonSerializer.Deserialize<WeaponData>(weaponJson);
                        
                        if (weapon != null)
                        {
                            weapons[weapon.Id] = weapon;
                            weaponNames[weapon.Id] = weapon.Name;
                            weaponDescriptions[weapon.Id] = weapon.Description;
                            
                            // 加载武器贴图
                            if (!string.IsNullOrEmpty(weapon.Visual.SpritePath))
                            {
                                try
                                {
                                    // 移除"Content/"前缀，因为ContentManager会自动添加
                                    string texturePath = weapon.Visual.SpritePath.Replace("Content/", "").Replace(".png", "");
                                    var texture = contentManager.Load<Texture2D>(texturePath);
                                    weaponTextures[weapon.Id] = texture;
                                    Console.WriteLine($"[CharacterSelection] Loaded weapon texture: {texturePath}");
                                }
                                catch (Exception texEx)
                                {
                                    Console.WriteLine($"[CharacterSelection] Error loading weapon texture for {weapon.Name}: {texEx.Message}");
                                }
                            }
                            
                            // 加载弹幕贴图
                            if (!string.IsNullOrEmpty(weapon.Visual.ProjectileSprite))
                            {
                                try
                                {
                                    string projectilePath = weapon.Visual.ProjectileSprite.Replace("Content/", "").Replace(".png", "");
                                    var projectileTexture = contentManager.Load<Texture2D>(projectilePath);
                                    projectileTextures[weapon.Id] = projectileTexture;
                                    Console.WriteLine($"[CharacterSelection] Loaded projectile texture: {projectilePath}");
                                }
                                catch (Exception projEx)
                                {
                                    Console.WriteLine($"[CharacterSelection] Error loading projectile texture for {weapon.Name}: {projEx.Message}");
                                }
                            }
                            
                            Console.WriteLine($"[CharacterSelection] Loaded weapon: {weapon.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CharacterSelection] Error loading weapon {entry.File}: {ex.Message}");
                    }
                }
                
                Console.WriteLine($"[CharacterSelection] Successfully loaded {weapons.Count} weapons");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CharacterSelection] Error loading weapon data: {ex.Message}");
            }
        }
    }
}