using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace HajimiManbo.World
{
    /// <summary>
    /// 生物群系背景管理器 - 实现泰拉瑞亚式多层视差背景系统
    /// </summary>
    public class BiomeBackgroundManager
    {
        private readonly ContentManager contentManager;
        private readonly Dictionary<BiomeType, BackgroundLayer[]> biomeBackgroundLayers;
        private BackgroundLayer[] defaultBackgroundLayers;
        
        /// <summary>
        /// 生物群系类型枚举
        /// </summary>
        public enum BiomeType
        {
            Forest,     // 森林（普通草地区域）
            Snow,       // 雪地
            Jungle,     // 丛林
            Desert,     // 沙漠
            Underground, // 地下
            Default     // 默认背景
        }
        
        /// <summary>
        /// 背景层数据结构
        /// </summary>
        public class BackgroundLayer
        {
            public Texture2D Texture { get; set; }
            public float ParallaxFactor { get; set; }  // 视差系数 (0-1)
            public float VerticalOffset { get; set; }  // 垂直偏移
            public bool IsUnderground { get; set; }    // 是否为地下层
            public Color TintColor { get; set; } = Color.White;
            
            public BackgroundLayer(Texture2D texture, float parallaxFactor, float verticalOffset = 0f, bool isUnderground = false)
            {
                Texture = texture;
                ParallaxFactor = parallaxFactor;
                VerticalOffset = verticalOffset;
                IsUnderground = isUnderground;
            }
        }
        
        public BiomeBackgroundManager(ContentManager contentManager)
        {
            this.contentManager = contentManager;
            this.biomeBackgroundLayers = new Dictionary<BiomeType, BackgroundLayer[]>();
            LoadBackgrounds();
        }
        
        /// <summary>
        /// 加载所有生物群系背景层
        /// </summary>
        private void LoadBackgrounds()
        {
            try
            {
                // 加载背景纹理
                var forestTexture = contentManager.Load<Texture2D>("img/BackGround/mapbg_forest");
                var snowTexture = contentManager.Load<Texture2D>("img/BackGround/mapbg_snow");
                var desertTexture = contentManager.Load<Texture2D>("img/BackGround/mapbg_sand");
                var undergroundTexture = contentManager.Load<Texture2D>("img/BackGround/mapgd_underground");
                var defaultTexture = contentManager.Load<Texture2D>("img/BackGround/BackGroundPicture");
                
                // 创建多层背景系统（按泰拉瑞亚的设计）
                // 每个生物群系有多层，视差系数从远到近递增
                
                // 森林群系 - 多层视差
                biomeBackgroundLayers[BiomeType.Forest] = new BackgroundLayer[]
                {
                    new BackgroundLayer(forestTexture, 0.1f, 0f),    // 最远层
                    new BackgroundLayer(forestTexture, 0.3f, 0f),    // 中层
                    new BackgroundLayer(forestTexture, 0.5f, 0f)     // 近层
                };
                
                // 雪地群系
                biomeBackgroundLayers[BiomeType.Snow] = new BackgroundLayer[]
                {
                    new BackgroundLayer(snowTexture, 0.1f, 0f),
                    new BackgroundLayer(snowTexture, 0.3f, 0f),
                    new BackgroundLayer(snowTexture, 0.5f, 0f)
                };
                
                // 沙漠群系
                biomeBackgroundLayers[BiomeType.Desert] = new BackgroundLayer[]
                {
                    new BackgroundLayer(desertTexture, 0.1f, 0f),
                    new BackgroundLayer(desertTexture, 0.3f, 0f),
                    new BackgroundLayer(desertTexture, 0.5f, 0f)
                };
                
                // 丛林群系（暂时使用森林纹理）
                biomeBackgroundLayers[BiomeType.Jungle] = new BackgroundLayer[]
                {
                    new BackgroundLayer(forestTexture, 0.1f, 0f),
                    new BackgroundLayer(forestTexture, 0.3f, 0f),
                    new BackgroundLayer(forestTexture, 0.5f, 0f)
                };
                
                // 地下群系（使用专用的地下背景纹理）
                biomeBackgroundLayers[BiomeType.Underground] = new BackgroundLayer[]
                {
                    new BackgroundLayer(undergroundTexture, 0.05f, 0f, true)
                };
                
                // 默认背景（使用森林纹理）
                defaultBackgroundLayers = new BackgroundLayer[]
                {
                    new BackgroundLayer(forestTexture, 0.3f, 0f)
                };
                biomeBackgroundLayers[BiomeType.Default] = defaultBackgroundLayers;
                
                Console.WriteLine("[BiomeBackgroundManager] 多层背景系统加载完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BiomeBackgroundManager] 加载背景时出错: {ex.Message}");
                CreateFallbackTextures();
            }
        }
        
        /// <summary>
        /// 创建后备纹理（当资源加载失败时使用）
        /// </summary>
        private void CreateFallbackTextures()
        {
            // 这里可以创建简单的颜色纹理作为后备
            // 暂时留空，实际项目中可以实现
        }
        
        /// <summary>
        /// 根据世界坐标获取当前位置的生物群系类型
        /// </summary>
        /// <param name="world">世界对象</param>
        /// <param name="worldX">世界X坐标（以tile为单位）</param>
        /// <param name="worldY">世界Y坐标（以tile为单位）</param>
        /// <returns>生物群系类型</returns>
        public BiomeType GetBiomeAtPosition(World world, int worldX, int worldY = -1)
        {
            if (world == null) return BiomeType.Default;
            
            // 检查是否在地下（如果提供了Y坐标）
            if (worldY >= 0)
            {
                int surfaceLevel = world.SurfaceHeight != null && worldX < world.SurfaceHeight.Length ? 
                    world.SurfaceHeight[worldX] : world.Height / 3;
                
                // 如果在地表以下八分之一世界高度，使用地下背景
                int undergroundThreshold = world.Height / 8;
                if (worldY > surfaceLevel + undergroundThreshold)
                {
                    return BiomeType.Underground;
                }
            }
            
            // 根据用户要求的生物群系分布：左边森林，中间沙漠，右边雪原
            // 世界被分为3个区域：森林、沙漠、雪地
            int sectionWidth = world.Width / 3;
            int section = worldX / sectionWidth;
            
            return section switch
            {
                0 => BiomeType.Forest,    // 左侧森林
                1 => BiomeType.Desert,    // 中间沙漠
                2 => BiomeType.Snow,      // 右侧雪地
                _ => BiomeType.Default
            };
        }
        
        /// <summary>
        /// 获取两个生物群系之间的混合权重（支持地下检测）
        /// </summary>
        /// <param name="world">世界对象</param>
        /// <param name="worldX">世界X坐标（以tile为单位）</param>
        /// <param name="worldY">世界Y坐标（以tile为单位）</param>
        /// <param name="transitionWidth">过渡区域宽度（以tile为单位）</param>
        /// <returns>返回(主要群系, 次要群系, 混合权重)，权重0表示完全是主要群系，1表示完全是次要群系</returns>
        public (BiomeType primary, BiomeType secondary, float blendFactor) GetBiomeBlend(World world, int worldX, int worldY = -1, int transitionWidth = 100)
        {
            if (world == null) return (BiomeType.Default, BiomeType.Default, 0f);
            
            // 首先检查是否在地下
             if (worldY >= 0)
             {
                 int surfaceLevel = world.SurfaceHeight != null && worldX < world.SurfaceHeight.Length ? 
                     world.SurfaceHeight[worldX] : world.Height / 3;
                 
                 // 如果在地表以下八分之一世界高度，使用地下背景
                  int undergroundThreshold = world.Height / 8;
                 if (worldY > surfaceLevel + undergroundThreshold)
                 {
                     return (BiomeType.Underground, BiomeType.Underground, 0f);
                 }
             }
            
            // 地表群系混合逻辑
            int sectionWidth = world.Width / 3;
            float normalizedX = (float)worldX / sectionWidth;
            int section = (int)normalizedX;
            float sectionProgress = normalizedX - section;
            
            // 获取当前区域的群系类型
            BiomeType currentBiome = section switch
            {
                0 => BiomeType.Forest,
                1 => BiomeType.Desert,
                2 => BiomeType.Snow,
                _ => BiomeType.Default
            };
            
            // 计算过渡区域（按泰拉瑞亚的设计，过渡区域更宽）
            float transitionRatio = (float)transitionWidth / sectionWidth;
            float halfTransition = transitionRatio / 2f;
            
            // 检查是否在过渡区域
            if (sectionProgress < halfTransition && section > 0)
            {
                // 靠近前一个区域的过渡
                BiomeType prevBiome = (section - 1) switch
                {
                    0 => BiomeType.Forest,
                    1 => BiomeType.Desert,
                    2 => BiomeType.Snow,
                    _ => BiomeType.Default
                };
                // 使用平滑的插值曲线
                float t = sectionProgress / halfTransition;
                float blendFactor = 1f - (t * t * (3f - 2f * t)); // 平滑步函数
                return (currentBiome, prevBiome, blendFactor);
            }
            else if (sectionProgress > (1f - halfTransition) && section < 2)
            {
                // 靠近后一个区域的过渡
                BiomeType nextBiome = (section + 1) switch
                {
                    0 => BiomeType.Forest,
                    1 => BiomeType.Desert,
                    2 => BiomeType.Snow,
                    _ => BiomeType.Default
                };
                float t = (sectionProgress - (1f - halfTransition)) / halfTransition;
                float blendFactor = t * t * (3f - 2f * t); // 平滑步函数
                return (currentBiome, nextBiome, blendFactor);
            }
            
            // 不在过渡区域，返回纯群系
            return (currentBiome, currentBiome, 0f);
        }
        
        /// <summary>
        /// 根据玩家位置获取当前生物群系类型
        /// </summary>
        /// <param name="world">世界对象</param>
        /// <param name="playerPosition">玩家位置（像素坐标）</param>
        /// <returns>生物群系类型</returns>
        public BiomeType GetBiomeAtPlayerPosition(World world, Vector2 playerPosition)
        {
            // 将像素坐标转换为tile坐标
            int tileX = (int)(playerPosition.X / 16); // 假设每个tile是16像素
            int tileY = (int)(playerPosition.Y / 16);
            return GetBiomeAtPosition(world, tileX, tileY);
        }
        
        /// <summary>
        /// 获取指定生物群系的背景层
        /// </summary>
        /// <param name="biomeType">生物群系类型</param>
        /// <returns>背景层数组</returns>
        public BackgroundLayer[] GetBackgroundLayers(BiomeType biomeType)
        {
            if (biomeBackgroundLayers.TryGetValue(biomeType, out BackgroundLayer[] layers))
            {
                return layers;
            }
            
            // 如果找不到指定的背景，返回默认背景层
            return defaultBackgroundLayers;
        }
        
        /// <summary>
        /// 获取当前玩家位置的背景层
        /// </summary>
        /// <param name="world">世界对象</param>
        /// <param name="playerPosition">玩家位置</param>
        /// <returns>背景层数组</returns>
        public BackgroundLayer[] GetCurrentBackgroundLayers(World world, Vector2 playerPosition)
        {
            BiomeType currentBiome = GetBiomeAtPlayerPosition(world, playerPosition);
            return GetBackgroundLayers(currentBiome);
        }
        
        /// <summary>
        /// 获取当前位置的背景层混合信息（用于渐变过渡）
        /// </summary>
        /// <param name="world">世界对象</param>
        /// <param name="viewportCenterX">视口中心X坐标（像素坐标）</param>
        /// <param name="viewportCenterY">视口中心Y坐标（像素坐标）</param>
        /// <returns>返回(主背景层, 次背景层, 混合权重)</returns>
        public (BackgroundLayer[] primary, BackgroundLayer[] secondary, float blendFactor) GetBlendedBackgroundLayers(World world, float viewportCenterX, float viewportCenterY = -1)
        {
            // 将像素坐标转换为tile坐标
            int tileX = (int)(viewportCenterX / 16);
            int tileY = viewportCenterY >= 0 ? (int)(viewportCenterY / 16) : -1;
            
            // 获取混合信息
            var (primaryBiome, secondaryBiome, blendFactor) = GetBiomeBlend(world, tileX, tileY);
            
            // 获取对应的背景层
            BackgroundLayer[] primaryLayers = GetBackgroundLayers(primaryBiome);
            BackgroundLayer[] secondaryLayers = GetBackgroundLayers(secondaryBiome);
            
            return (primaryLayers, secondaryLayers, blendFactor);
        }
        
        /// <summary>
        /// 获取生物群系的显示名称
        /// </summary>
        /// <param name="biomeType">生物群系类型</param>
        /// <returns>显示名称</returns>
        public string GetBiomeName(BiomeType biomeType)
        {
            return biomeType switch
            {
                BiomeType.Forest => "森林",
                BiomeType.Snow => "雪地",
                BiomeType.Jungle => "丛林",
                BiomeType.Desert => "沙漠",
                BiomeType.Underground => "地下",
                BiomeType.Default => "默认",
                _ => "未知"
            };
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // ContentManager会自动管理纹理的释放，这里不需要手动释放
        }
    }
}