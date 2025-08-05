using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using HajimiManbo.World;

namespace HajimiManbo.Lighting
{
    /// <summary>
    /// 背景墙系统，用于渲染物块后面的背景墙
    /// </summary>
    public class BackgroundWall
    {
        private GraphicsDevice _graphicsDevice;
        private ContentManager _contentManager;
        private Texture2D _defaultWallTexture;
        private LightingSystem _lightingSystem;
        private BiomeBackgroundManager _biomeManager;
        private const int WALL_SIZE = 16; // 背景墙大小
        private const int TEXTURE_SIZE = 48; // 原始贴图大小
        private const int TILES_PER_ROW = 3; // 每行的16x16瓦片数量
        private Random _random; // 用于随机选择瓦片
        
        // 不同方块类型对应的背景墙纹理
        private readonly Dictionary<TileType, Texture2D> _wallTextures = new Dictionary<TileType, Texture2D>();
        
        // 不同方块类型对应的背景墙颜色（作为后备）
        private readonly Dictionary<TileType, Color> _wallColors = new Dictionary<TileType, Color>
        {
            { TileType.Stone, Color.Gray },           // 石头 - 灰色
            { TileType.Dirt, new Color(139, 69, 19) }, // 泥土 - 棕色
            { TileType.Sand, Color.Yellow },          // 沙子 - 黄色
            { TileType.Snow, Color.LightBlue }        // 雪 - 淡蓝色
        };
        
        public BackgroundWall(GraphicsDevice graphicsDevice, ContentManager contentManager, BiomeBackgroundManager biomeManager)
        {
            _graphicsDevice = graphicsDevice;
            _contentManager = contentManager;
            _biomeManager = biomeManager;
            _random = new Random();
            CreateWallTextures();
        }
        
        /// <summary>
        /// 设置光照系统
        /// </summary>
        public void SetLightingSystem(LightingSystem lightingSystem)
        {
            _lightingSystem = lightingSystem;
        }
        
        /// <summary>
        /// 获取随机的源矩形区域（从48x48贴图中选择16x16区域）
        /// </summary>
        /// <param name="x">世界X坐标</param>
        /// <param name="y">世界Y坐标</param>
        /// <returns>源矩形区域</returns>
        private Rectangle GetRandomSourceRect(int x, int y)
        {
            // 使用坐标作为种子，确保同一位置总是选择相同的瓦片
            Random localRandom = new Random(x * 1000 + y);
            
            // 随机选择3x3网格中的一个位置
            int tileX = localRandom.Next(0, TILES_PER_ROW);
            int tileY = localRandom.Next(0, TILES_PER_ROW);
            
            return new Rectangle(
                tileX * WALL_SIZE,
                tileY * WALL_SIZE,
                WALL_SIZE,
                WALL_SIZE
            );
        }
        
        /// <summary>
        /// 根据生物群系和前景方块情况动态选择背景墙类型
        /// </summary>
        private TileType GetDynamicWallType(World.World world, int x, int y, TileType originalWallType)
        {
            // 检查当前位置的前景方块
            var currentTile = world.GetTile(x, y);
            bool hasForegroundBlock = currentTile.Type != TileType.Air;
            
            // 如果有前景方块，使用原始的背景墙类型
            if (hasForegroundBlock)
            {
                return originalWallType;
            }
            
            // 如果没有前景方块，根据生物群系选择背景墙类型
            if (_biomeManager != null)
            {
                var biomeType = _biomeManager.GetBiomeAtPosition(world, x, y);
                
                // 检查是否在下层岩石层（地下深处）
                int surfaceHeight = world.SurfaceHeight[x];
                int depthBelowSurface = y - surfaceHeight;
                int rockLayerDepth = world.Height / 4; // 岩石层深度阈值，调整为更深
                
                // 如果在深层岩石层，使用石头背景墙
                if (depthBelowSurface > rockLayerDepth)
                {
                    return TileType.Stone;
                }
                
                // 根据生物群系选择对应的背景墙
                return biomeType switch
                {
                    BiomeBackgroundManager.BiomeType.Desert => TileType.Sand,
                    BiomeBackgroundManager.BiomeType.Snow => TileType.Snow,
                    BiomeBackgroundManager.BiomeType.Forest => TileType.Dirt,
                    BiomeBackgroundManager.BiomeType.Jungle => TileType.Dirt,
                    BiomeBackgroundManager.BiomeType.Underground => TileType.Stone,
                    _ => originalWallType
                };
            }
            
            return originalWallType;
        }
        
        /// <summary>
        /// 创建背景墙纹理
        /// </summary>
        private void CreateWallTextures()
        {
            // 创建默认白色纹理（作为后备）
            _defaultWallTexture = new Texture2D(_graphicsDevice, WALL_SIZE, WALL_SIZE);
            Color[] colorData = new Color[WALL_SIZE * WALL_SIZE];
            
            for (int i = 0; i < colorData.Length; i++)
            {
                colorData[i] = Color.White;
            }
            
            _defaultWallTexture.SetData(colorData);
            
            // 加载特定的背景墙贴图
            try
            {
                // 加载石头背景墙贴图
                _wallTextures[TileType.Stone] = _contentManager.Load<Texture2D>("img/Wall/Stone_Wall_(placed)");
                
                // 加载沙地背景墙贴图
                _wallTextures[TileType.Sand] = _contentManager.Load<Texture2D>("img/Wall/Sandstone_Brick_Wall_(placed)");
                
                // 加载雪地背景墙贴图
                _wallTextures[TileType.Snow] = _contentManager.Load<Texture2D>("img/Wall/Snow_Wall_(placed)");
                
                // 加载泥土背景墙贴图
                _wallTextures[TileType.Dirt] = _contentManager.Load<Texture2D>("img/Wall/Dirt_Wall_(placed)");
            }
            catch (Exception ex)
            {
                // 如果加载失败，使用默认纹理
                Console.WriteLine($"Failed to load wall texture: {ex.Message}");
                if (!_wallTextures.ContainsKey(TileType.Stone))
                    _wallTextures[TileType.Stone] = _defaultWallTexture;
                if (!_wallTextures.ContainsKey(TileType.Sand))
                    _wallTextures[TileType.Sand] = _defaultWallTexture;
                if (!_wallTextures.ContainsKey(TileType.Snow))
                    _wallTextures[TileType.Snow] = _defaultWallTexture;
                if (!_wallTextures.ContainsKey(TileType.Dirt))
                    _wallTextures[TileType.Dirt] = _defaultWallTexture;
            }
        }
        
        /// <summary>
        /// 渲染背景墙
        /// </summary>
        /// <param name="spriteBatch">精灵批次</param>
        /// <param name="world">世界对象</param>
        /// <param name="viewBounds">视图边界</param>
        /// <param name="lightingSystem">光照系统</param>
        public void Render(SpriteBatch spriteBatch, World.World world, Rectangle viewBounds, LightingSystem lightingSystem)
        {
            if (world == null || _defaultWallTexture == null) return;
            
            // 计算需要渲染的瓦片范围，向外扩展2个像素
            int startX = Math.Max(0, (viewBounds.Left - 2) / WALL_SIZE);
            int endX = Math.Min(world.Width - 1, (viewBounds.Right + 2) / WALL_SIZE + 1);
            int startY = Math.Max(0, (viewBounds.Top - 2) / WALL_SIZE);
            int endY = Math.Min(world.Height - 1, (viewBounds.Bottom + 2) / WALL_SIZE + 1);
            
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    // 检查是否应该渲染背景墙
                    if (!ShouldRenderBackgroundWall(world, x, y))
                        continue;
                    
                    // 获取预生成的背景墙类型
                    var originalWallType = world.GetBackgroundWall(x, y);
                    
                    // 根据生物群系和前景方块情况动态调整背景墙类型
                    var wallType = GetDynamicWallType(world, x, y, originalWallType);
                    
                    if (wallType != TileType.Air)
                    {
                        // 渲染16x16像素的背景墙
                        Rectangle destRect = new Rectangle(
                            x * WALL_SIZE,     // 标准位置
                            y * WALL_SIZE,     // 标准位置
                            WALL_SIZE,         // 标准宽度16px
                            WALL_SIZE          // 标准高度16px
                        );
                        
                        // 获取对应的纹理
                        Texture2D wallTexture = _wallTextures.TryGetValue(wallType, out Texture2D texture) ? texture : _defaultWallTexture;
                        
                        // 获取颜色（用于非贴图类型的颜色调制）
                        Color wallColor = _wallColors.TryGetValue(wallType, out Color color) ? color : Color.White;
                        
                        // 检查当前位置是否为洞穴（前景方块为空气）
                        var currentTile = world.GetTile(x, y);
                        bool isCave = currentTile.Type == TileType.Air;
                        
                        float brightness;
                        if (isCave)
                        {
                            // 洞穴中的背景墙：使用与物块完全相同的深度变暗逻辑
                            const float MAX_LIGHT = 1.0f;
                            const float AMBIENT_LIGHT = 0.01f;
                            
                            int surfaceHeight = world.SurfaceHeight[x];
                            int depthBelowSurface = y - surfaceHeight;
                            
                            // 如果在地表或地表以上，保持最大光照
                            if (depthBelowSurface <= 0)
                            {
                                brightness = MAX_LIGHT * 0.4f; // 背景墙比前景稍暗
                            }
                            else
                            {
                                // 定义衰减参数（与LightingSystem.CalculateDepthBasedLight完全一致）
                                int fadeStartDepth = world.Height / 32; // 开始衰减的深度
                                int fadeEndDepth = world.Height / 6;    // 完全变暗的深度
                                
                                // 如果深度小于开始衰减深度，保持最大光照
                                if (depthBelowSurface < fadeStartDepth)
                                {
                                    brightness = MAX_LIGHT * 0.4f;
                                }
                                // 如果深度大于完全变暗深度，使用环境光
                                else if (depthBelowSurface >= fadeEndDepth)
                                {
                                    brightness = AMBIENT_LIGHT * 0.4f;
                                }
                                // 在衰减区间内，线性插值
                                else
                                {
                                    float fadeProgress = (float)(depthBelowSurface - fadeStartDepth) / (fadeEndDepth - fadeStartDepth);
                                    float lightLevel = MAX_LIGHT * (1.0f - fadeProgress) + AMBIENT_LIGHT * fadeProgress;
                                    brightness = lightLevel * 0.4f; // 背景墙比前景稍暗
                                }
                            }
                        }
                        else
                        {
                            // 非洞穴区域（有前景方块）：仿照未暴露物块的光照亮度渲染方法
                            // 使用光照系统获取该位置的光照强度
                            if (_lightingSystem != null)
                            {
                                float lightLevel = _lightingSystem.GetLightLevel(x, y);
                                // 背景墙比前景方块稍暗，并且有一个最小亮度保证可见性
                                brightness = Math.Max(0.1f, lightLevel * 0.6f);
                            }
                            else
                            {
                                // 如果没有光照系统，使用原来的暗色调
                                brightness = 0.4f;
                            }
                        }
                        
                        Color finalColor;
                        
                        // 如果是有专用48x48贴图的背景墙类型，使用亮度调制而不是颜色调制
                        if ((wallType == TileType.Stone || wallType == TileType.Sand || wallType == TileType.Snow || wallType == TileType.Dirt) && wallTexture != _defaultWallTexture)
                        {
                            finalColor = new Color(
                                (int)(255 * brightness),
                                (int)(255 * brightness),
                                (int)(255 * brightness),
                                255 // 完全不透明
                            );
                        }
                        else
                        {
                            // 其他类型使用颜色调制
                            finalColor = new Color(
                                (int)(wallColor.R * brightness),
                                (int)(wallColor.G * brightness),
                                (int)(wallColor.B * brightness),
                                255 // 完全不透明
                            );
                        }
                        
                        // 渲染背景墙
                        if ((wallType == TileType.Stone || wallType == TileType.Sand || wallType == TileType.Snow || wallType == TileType.Dirt) && wallTexture != _defaultWallTexture)
                        {
                            // 有专用48x48贴图的背景墙：使用随机选择的源矩形区域
                            Rectangle sourceRect = GetRandomSourceRect(x, y);
                            spriteBatch.Draw(wallTexture, destRect, sourceRect, finalColor);
                        }
                        else
                        {
                            // 其他类型：使用整个纹理
                            spriteBatch.Draw(wallTexture, destRect, finalColor);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 检查是否应该渲染背景墙
        /// </summary>
        /// <param name="world">世界对象</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>是否应该渲染背景墙</returns>
        private bool ShouldRenderBackgroundWall(World.World world, int x, int y)
        {
            // 背景墙应该在所有位置都渲染，除非没有背景墙类型
            var wallType = world.GetBackgroundWall(x, y);
            return wallType != TileType.Air;
        }
        
        /// <summary>
        /// 获取背景墙的透明度因子（用于噪声平滑过渡）
        /// </summary>
        /// <param name="world">世界对象</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>透明度因子（0.0-1.0）</returns>
        private float GetBackgroundWallAlphaFactor(World.World world, int x, int y)
        {
            // 获取地表高度
            if (world.SurfaceHeight == null || x < 0 || x >= world.SurfaceHeight.Length)
                return 1.0f;
                
            int surfaceHeight = world.SurfaceHeight[x];
            
            // 在地表以上一定高度开始应用噪声平滑
            int heightThreshold = -80; // 地表上方80格开始应用噪声平滑
            int heightAboveSurface = surfaceHeight - y; // 距离地表的高度（正数表示在地表上方）
            
            if (heightAboveSurface > heightThreshold)
            {
                // 使用噪声来创建平滑过渡
                float smoothNoise = NoiseGenerator.Noise2D(x * 0.05f, y * 0.05f, 1f, 3, 0.5f);
                
                // 根据高度调整基础透明度，越高越透明
                float heightFactor = Math.Min(1.0f, (heightAboveSurface - heightThreshold) / 50.0f);
                float baseAlpha = 1.0f - heightFactor * 0.7f; // 基础透明度从1.0到0.3
                
                // 使用噪声调制透明度，创建自然的过渡效果
                float noiseModulation = (smoothNoise + 1.0f) * 0.5f; // 将噪声从[-1,1]转换到[0,1]
                float finalAlpha = baseAlpha * (0.5f + noiseModulation * 0.5f); // 噪声调制范围50%-100%
                
                return Math.Max(0.0f, Math.Min(1.0f, finalAlpha));
            }
            
            return 1.0f; // 地表以下或阈值以下，完全不透明
        }
        
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _defaultWallTexture?.Dispose();
            
            // 释放所有背景墙纹理（除了默认纹理，因为已经释放了）
            foreach (var texture in _wallTextures.Values)
            {
                if (texture != _defaultWallTexture)
                {
                    texture?.Dispose();
                }
            }
            _wallTextures.Clear();
        }
    }
}