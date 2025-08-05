using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using HajimiManbo.Lighting;


namespace HajimiManbo.World
{
    /// <summary>
    /// 世界渲染器，负责渲染地形（使用分块优化）和背景
    /// </summary>
    public class WorldRenderer
    {
        private readonly GraphicsDevice graphicsDevice;
        private readonly SpriteBatch spriteBatch;
        private Texture2D pixelTexture;
        private Dictionary<TileType, Color> tileColors;
        private Dictionary<TileType, Texture2D> tileTextures;
        private readonly ContentManager contentManager;
        private World currentWorld; // 用于检查相邻方块
        private ChunkManager chunkManager;
        private BiomeBackgroundManager backgroundManager; // 背景管理器
        private LightingSystem lightingSystem; // 光照系统
        private BackgroundWall backgroundWall; // 背景墙系统

        
        // 方块大小（像素）
        public int TileSize { get; set; } = 16;
        
        public WorldRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager contentManager)
        {
            this.graphicsDevice = graphicsDevice;
            this.spriteBatch = spriteBatch;
            this.contentManager = contentManager;
            
            Initialize();
            
            // 初始化背景管理器
            backgroundManager = new BiomeBackgroundManager(contentManager);
            
            // 初始化背景墙系统
            backgroundWall = new BackgroundWall(graphicsDevice, contentManager, backgroundManager);
        }
        
        /// <summary>
        /// 设置世界并初始化光照系统
        /// </summary>
        public void SetWorld(World world)
        {
            currentWorld = world;
            if (world != null)
            {
                lightingSystem = new LightingSystem(world);
                // 光照系统默认启用，初始为全亮状态
                lightingSystem.LightingEnabled = true;
                lightingSystem.RecalculateLighting();
                
                // 将光照系统传递给背景墙
                backgroundWall?.SetLightingSystem(lightingSystem);
            }
        }
        
        /// <summary>
        /// 初始化渲染器
        /// </summary>
        private void Initialize()
        {
            // 创建1x1像素纹理
            pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
            
            // 初始化方块颜色映射
            InitializeTileColors();
            
            // 初始化方块纹理映射
            InitializeTileTextures();
        }
        

        
        /// <summary>
        /// 初始化方块颜色
        /// </summary>
        private void InitializeTileColors()
        {
            tileColors = new Dictionary<TileType, Color>
            {
                { TileType.Air, Color.Transparent },
                { TileType.Dirt, new Color(139, 69, 19) },      // 棕色
                { TileType.Grass, new Color(34, 139, 34) },     // 绿色
                { TileType.Stone, new Color(128, 128, 128) },   // 灰色
                { TileType.Sand, new Color(238, 203, 173) },    // 沙色
                { TileType.Snow, new Color(255, 250, 250) },    // 雪白色
                { TileType.JungleGrass, new Color(0, 100, 0) }, // 深绿色
                { TileType.Water, new Color(0, 191, 255) },     // 蓝色
                { TileType.Lava, new Color(255, 69, 0) },       // 橙红色
                { TileType.CopperOre, new Color(184, 115, 51) }, // 铜色
                { TileType.IronOre, new Color(169, 169, 169) },  // 铁灰色
                { TileType.GoldOre, new Color(255, 215, 0) },    // 金色
                { TileType.SilverOre, new Color(192, 192, 192) }, // 银色
                { TileType.Coal, new Color(36, 36, 36) },        // 黑色
                { TileType.Diamond, new Color(185, 242, 255) }   // 钻石蓝
            };
        }
        
        /// <summary>
        /// 初始化方块纹理
        /// </summary>
        private void InitializeTileTextures()
        {
            tileTextures = new Dictionary<TileType, Texture2D>();
            
            try
            {
                // 优先加载16格帧图集纹理
                try
                {
                    tileTextures[TileType.Dirt] = contentManager.Load<Texture2D>("Tiles/Dirt_Block_Frames");
                    tileTextures[TileType.Sand] = contentManager.Load<Texture2D>("Tiles/Sand_Block_Frames");
                    tileTextures[TileType.Snow] = contentManager.Load<Texture2D>("Tiles/Snow_Block_Frames");
                    tileTextures[TileType.Stone] = contentManager.Load<Texture2D>("Tiles/Stone_Block_Frames");
                    
                    // 大理石帧图集
                    tileTextures[TileType.Marble] = contentManager.Load<Texture2D>("Tiles/Marble_Block_Frames");
                    
                    Console.WriteLine("[WorldRenderer] 16-frame tilesets loaded successfully");
                }
                catch
                {
                    // 如果帧图集加载失败，回退到单一纹理
                    Console.WriteLine("[WorldRenderer] Frame tilesets not found, falling back to single textures");
                    
                    tileTextures[TileType.Dirt] = contentManager.Load<Texture2D>("Tiles/Dirt_Block_(placed)");
                    tileTextures[TileType.Sand] = contentManager.Load<Texture2D>("Tiles/Sand_Block_(placed)");
                    tileTextures[TileType.Snow] = contentManager.Load<Texture2D>("Tiles/Snow_Block_(placed)");
                    tileTextures[TileType.Stone] = contentManager.Load<Texture2D>("Tiles/Stone_Block_(placed)");
                    
                    // 大理石作为特殊石头类型
                    tileTextures[TileType.Marble] = contentManager.Load<Texture2D>("Tiles/Marble_Block_(placed)");
                }
                
                // 对于没有专门纹理的方块类型，可以复用现有纹理
                tileTextures[TileType.Grass] = tileTextures[TileType.Dirt]; // 草地使用泥土纹理
                tileTextures[TileType.JungleGrass] = tileTextures[TileType.Dirt]; // 丛林草使用泥土纹理
                
                // 矿物可以使用石头纹理作为基础
                tileTextures[TileType.CopperOre] = tileTextures[TileType.Stone];
                tileTextures[TileType.IronOre] = tileTextures[TileType.Stone];
                tileTextures[TileType.GoldOre] = tileTextures.ContainsKey(TileType.Marble) ? tileTextures[TileType.Marble] : tileTextures[TileType.Stone];
                tileTextures[TileType.SilverOre] = tileTextures.ContainsKey(TileType.Marble) ? tileTextures[TileType.Marble] : tileTextures[TileType.Stone];
                tileTextures[TileType.Coal] = tileTextures[TileType.Stone];
                tileTextures[TileType.Diamond] = tileTextures.ContainsKey(TileType.Marble) ? tileTextures[TileType.Marble] : tileTextures[TileType.Stone];
                
                Console.WriteLine("[WorldRenderer] Tile textures loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorldRenderer] Failed to load tile textures: {ex.Message}");
                // 如果加载失败，tileTextures将为空，渲染时会回退到颜色模式
            }
        }
        
        /// <summary>
        /// 渲染世界（使用分块优化）
        /// </summary>
        public void RenderWorld(World world, Rectangle viewportBounds, Matrix cameraMatrix, Vector2 cameraPosition)
        {
            if (world == null) return;
            
            // 初始化光照系统（如果还没有）
            if (lightingSystem == null && world != null)
            {
                SetWorld(world);
            }
            
            // 初始化ChunkManager（如果还没有）
            if (chunkManager == null)
            {
                chunkManager = new ChunkManager(world, graphicsDevice, contentManager, this);
            }
            
            // 保存当前世界引用用于方块连接检测
            currentWorld = world;
            

            
            // 计算基于摄像机位置的视口边界
            Rectangle viewBounds = new Rectangle(
                (int)cameraPosition.X - viewportBounds.Width / 2,
                (int)cameraPosition.Y - viewportBounds.Height / 2,
                viewportBounds.Width,
                viewportBounds.Height
            );
            
            // 创建投影矩阵
            Matrix projection = Matrix.CreateOrthographicOffCenter(
                0, viewportBounds.Width,
                viewportBounds.Height, 0,
                0, 1
            );
            
            // 更新光照系统（根据玩家位置调整表面光照）
            if (lightingSystem != null)
            {
                lightingSystem.RecalculateLighting(cameraPosition);
            }
            
            // 渲染背景
            RenderBackground(world, viewportBounds, cameraMatrix, cameraPosition);
            
            // 渲染背景墙
            RenderBackgroundWalls(world, viewBounds, cameraMatrix, lightingSystem);
            
            // 传统渲染
            chunkManager.Render(cameraMatrix, projection, viewBounds);
        }
        
        /// <summary>
        /// 渲染世界（兼容旧接口）
        /// </summary>
        public void RenderWorld(World world, Rectangle viewportBounds)
        {
            RenderWorld(world, viewportBounds, Matrix.Identity, Vector2.Zero);
        }
        
        /// <summary>
        /// 渲染背景墙
        /// </summary>
        private void RenderBackgroundWalls(World world, Rectangle viewBounds, Matrix cameraMatrix, LightingSystem lightingSystem)
        {
            if (backgroundWall == null || world == null) return;
            
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, cameraMatrix);
            backgroundWall.Render(spriteBatch, world, viewBounds, lightingSystem);
            spriteBatch.End();
        }
        
        /// <summary>
        /// 渲染多层视差背景（泰拉瑞亚风格）
        /// </summary>
        private void RenderBackground(World world, Rectangle viewportBounds, Matrix cameraMatrix, Vector2 cameraPosition)
        {
            if (backgroundManager == null) return;
            
            // 背景始终正常渲染，不受光照系统影响
            
            // 获取视口中心位置的背景层信息
            float viewportCenterX = cameraPosition.X + viewportBounds.Width / 2f;
            float viewportCenterY = cameraPosition.Y + viewportBounds.Height / 2f;
            var (primaryLayers, secondaryLayers, blendFactor) = backgroundManager.GetBlendedBackgroundLayers(world, viewportCenterX, viewportCenterY);
            
            // 使用LinearWrap采样器以获得更好的平铺效果
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            
            // 渲染主背景的所有层
            if (primaryLayers != null)
            {
                foreach (var layer in primaryLayers)
                {
                    RenderBackgroundLayer(layer, viewportBounds, cameraPosition, Color.White);
                }
            }
            
            // 如果有混合，渲染次背景的所有层
            if (blendFactor > 0f && secondaryLayers != null && secondaryLayers != primaryLayers)
            {
                Color blendColor = Color.White * blendFactor;
                foreach (var layer in secondaryLayers)
                {
                    RenderBackgroundLayer(layer, viewportBounds, cameraPosition, blendColor);
                }
            }
            
            spriteBatch.End();
        }
        
        /// <summary>
        /// 渲染单个背景层（泰拉瑞亚风格的多层视差）
        /// </summary>
        private void RenderBackgroundLayer(BiomeBackgroundManager.BackgroundLayer layer, Rectangle viewportBounds, Vector2 cameraPosition, Color tintColor)
        {
            if (layer.Texture == null) return;
            
            // 应用层的颜色调色，并增加透明度营造远景效果
            float alphaMultiplier = 0.6f; // 调整透明度，0.6表示60%的不透明度
            Color finalColor = new Color(
                (int)(layer.TintColor.R * tintColor.R / 255f),
                (int)(layer.TintColor.G * tintColor.G / 255f),
                (int)(layer.TintColor.B * tintColor.B / 255f),
                (int)(layer.TintColor.A * tintColor.A * alphaMultiplier / 255f)
            );
            
            // 计算视差偏移（按泰拉瑞亚的设计）
            float offsetX = cameraPosition.X * layer.ParallaxFactor;
            float offsetY = (cameraPosition.Y * layer.ParallaxFactor * 0.5f) + layer.VerticalOffset;
            
            // 对于地下层，减少垂直移动
            if (layer.IsUnderground)
            {
                offsetY *= 0.3f;
            }
            
            // 计算纹理在屏幕上的重复次数（放大2倍）
            int originalWidth = layer.Texture.Width;
            int originalHeight = layer.Texture.Height;
            int textureWidth = originalWidth * 2;  // 放大2倍
            int textureHeight = originalHeight * 2; // 放大2倍
            
            // 计算需要绘制的瓦片数量（确保完全覆盖视口）
            int tilesX = (viewportBounds.Width / textureWidth) + 3;
            int tilesY = (viewportBounds.Height / textureHeight) + 3;
            
            // 计算起始绘制位置（处理负数偏移）
            float wrappedOffsetX = offsetX % textureWidth;
            float wrappedOffsetY = offsetY % textureHeight;
            
            // 确保负数偏移正确处理
            if (wrappedOffsetX < 0) wrappedOffsetX += textureWidth;
            if (wrappedOffsetY < 0) wrappedOffsetY += textureHeight;
            
            float startX = -wrappedOffsetX - textureWidth;
            float startY = -wrappedOffsetY - textureHeight;
            
            // 平铺渲染背景层
            for (int x = 0; x < tilesX; x++)
            {
                for (int y = 0; y < tilesY; y++)
                {
                    float tileX = startX + (x * textureWidth);
                    float tileY = startY + (y * textureHeight);
                    
                    // 只渲染可能在视口内的瓦片（优化性能）
                    if (tileX + textureWidth >= -50 && tileX <= viewportBounds.Width + 50 &&
                        tileY + textureHeight >= -50 && tileY <= viewportBounds.Height + 50)
                    {
                        Rectangle destRect = new Rectangle(
                            (int)tileX,
                            (int)tileY,
                            textureWidth,
                            textureHeight
                        );
                        
                        spriteBatch.Draw(layer.Texture, destRect, finalColor);
                    }
                }
            }
        }
        
        /// <summary>
        /// 渲染单个方块（使用世界坐标）
        /// </summary>
        private void RenderTile(Tile tile, int worldX, int worldY)
        {
            if (tile.Type == TileType.Air) return;
            
            // 直接使用世界坐标（像素坐标）
            Rectangle destRect = new Rectangle(
                worldX * TileSize,
                worldY * TileSize,
                TileSize,
                TileSize
            );
            
            // 添加深度效果（越深越暗）
            float depthFactor = Math.Max(0.3f, 1.0f - (worldY * 0.001f));
            Color tintColor = Color.Lerp(Color.Black, Color.White, depthFactor);
            
            // 获取方块连接信息来决定使用哪个贴图帧
            Rectangle sourceRect = GetTileSourceRect(tile.Type, worldX, worldY);
            
            // 注意：斜坡渲染现在由Chunk系统处理
            {
                // 特殊渲染逻辑：草地方块的处理
                if (tile.Type == TileType.Grass)
                {
                    RenderGrassTile(destRect, sourceRect, tintColor, worldX, worldY);
                }
                else if (tile.Type == TileType.JungleGrass)
                {
                    RenderJungleGrassTile(destRect, sourceRect, tintColor, worldX, worldY);
                }
                else
                {
                    // 普通方块渲染
                    if (tileTextures != null && tileTextures.TryGetValue(tile.Type, out Texture2D texture) && texture != null)
                    {
                        spriteBatch.Draw(texture, destRect, sourceRect, tintColor);
                    }
                    else
                     {
                         // 回退到颜色模式
                         if (!tileColors.TryGetValue(tile.Type, out Color color))
                         {
                             color = Color.Magenta; // 未知方块用洋红色表示
                         }
                     
                         if (color == Color.Transparent) return;
                         
                         color = Color.Lerp(Color.Black, color, depthFactor);
                         spriteBatch.Draw(pixelTexture, destRect, color);
                     }
                 }
             }
         }
        
        /// <summary>
        /// 当tile发生变化时通知ChunkManager和光照系统
        /// </summary>
        public void OnTileChanged(int tileX, int tileY)
        {
            chunkManager?.OnTileChanged(tileX, tileY);
            lightingSystem?.OnTileChanged(tileX, tileY);
        }
        
        /// <summary>
        /// 获取光照系统
        /// </summary>
        public LightingSystem GetLightingSystem()
        {
            return lightingSystem;
        }
        
        /// <summary>
        /// 添加光源
        /// </summary>
        public void AddLightSource(Vector2 position, float intensity, Color color)
        {
            lightingSystem?.AddLightSource(position, intensity, color);
            // 强制重建所有chunks以应用新的光照
            ForceRebuildChunks();
        }
        
        /// <summary>
        /// 移除光源
        /// </summary>
        public void RemoveLightSource(Vector2 position)
        {
            lightingSystem?.RemoveLightSource(position);
        }
        
        // 光照切换功能已移除
        
        /// <summary>
        /// 强制重建所有chunks，用于世界生成完成后刷新渲染缓存
        /// </summary>
        public void ForceRebuildChunks()
        {
            chunkManager?.RebuildAll();
        }
        
        /// <summary>
        /// 渲染调试信息
        /// </summary>
        public void RenderDebugInfo(SpriteFont font, World world, Vector2 cameraPosition)
        {
            if (world == null || font == null) return;
            
            var renderStats = chunkManager?.GetRenderStats(new Rectangle(0, 0, 1920, 1080)) ?? new RenderStats();
            
            // 获取当前生物群系信息
            string biomeInfo = "未知";
            if (backgroundManager != null)
            {
                var currentBiome = backgroundManager.GetBiomeAtPlayerPosition(world, cameraPosition);
                biomeInfo = backgroundManager.GetBiomeName(currentBiome);
            }
            
            string lightingStatus = lightingSystem?.LightingEnabled == true ? "Enabled" : "Disabled";
            int lightSourceCount = lightingSystem?.GetLightSourceCount() ?? 0;
            
            string debugText = $"World Size: {world.Width} x {world.Height}\n" +
                              $"Visible Chunks: {renderStats.VisibleChunks}/{renderStats.TotalChunks}\n" +
                              $"Chunk Size: {renderStats.ChunkSize}x{renderStats.ChunkSize}\n" +
                              $"Current Biome: {biomeInfo}\n" +
                              $"Lighting: {lightingStatus}\n" +
                              $"Light Sources: {lightSourceCount}";
            
            Vector2 position = new Vector2(10, 100);
            
            // 绘制背景
            Vector2 textSize = font.MeasureString(debugText);
            Rectangle background = new Rectangle(
                (int)position.X - 5,
                (int)position.Y - 5,
                (int)textSize.X + 10,
                (int)textSize.Y + 10
            );
            spriteBatch.Draw(pixelTexture, background, Color.Black * 0.7f);
            
            // 绘制文本
            spriteBatch.DrawString(font, debugText, position, Color.White);
            

        }
        
        /// <summary>
        /// 获取当前位置的生物群系类型
        /// </summary>
        /// <param name="world">世界对象</param>
        /// <param name="position">位置</param>
        /// <returns>生物群系类型</returns>
        public BiomeBackgroundManager.BiomeType GetCurrentBiome(World world, Vector2 position)
        {
            return backgroundManager?.GetBiomeAtPlayerPosition(world, position) ?? BiomeBackgroundManager.BiomeType.Default;
        }
        
        /// <summary>
         /// 获取方块的源矩形（简化版本，现在主要由Chunk系统处理）
         /// </summary>
         private Rectangle GetTileSourceRect(TileType tileType, int worldX, int worldY)
         {
             // 注意：详细的纹理源矩形计算现在由Chunk系统处理
             // 这里只返回默认的完整纹理矩形
             return GetFullTextureRect();
         }
         
         // 注意：帧计算逻辑已移至Chunk.cs中，此处不再重复实现
         
         /// <summary>
         /// 获取完整纹理矩形
         /// </summary>
         private Rectangle GetFullTextureRect()
         {
             // 16x16像素的纹理帧
             return new Rectangle(0, 0, 16, 16);
         }
         
         /// <summary>
         /// 获取方块的纹理变化（基于位置的伪随机）
         /// </summary>
         private int GetTileVariation(int x, int y)
         {
             // 使用位置生成伪随机数来决定纹理变化
             int hash = (x * 374761393 + y * 668265263) % 4;
             return Math.Abs(hash);
         }
        
        /// <summary>
         /// 检查指定位置的方块是否可以与给定类型连接
         /// </summary>
         private bool CanConnectTo(int x, int y, TileType tileType)
         {
             if (currentWorld == null || !currentWorld.IsValidCoordinate(x, y))
                 return false;
                 
             var tile = currentWorld.GetTile(x, y);
             
             // 相同类型的方块可以连接
             if (tile.Type == tileType)
                 return true;
             
             // 特殊连接规则：草地可以与泥土连接
             if (tileType == TileType.Grass && tile.Type == TileType.Dirt)
                 return true;
             if (tileType == TileType.Dirt && tile.Type == TileType.Grass)
                 return true;
             
             // 丛林草可以与泥土连接
             if (tileType == TileType.JungleGrass && tile.Type == TileType.Dirt)
                 return true;
             if (tileType == TileType.Dirt && tile.Type == TileType.JungleGrass)
                 return true;
             
             return false;
          }
          
          /// <summary>
          /// 渲染草地方块（在泥土基础上显示草地纹理）
          /// </summary>
          private void RenderGrassTile(Rectangle destRect, Rectangle sourceRect, Color tintColor, int worldX, int worldY)
          {
              // 先渲染泥土作为基础
              if (tileTextures != null && tileTextures.TryGetValue(TileType.Dirt, out Texture2D dirtTexture) && dirtTexture != null)
              {
                  spriteBatch.Draw(dirtTexture, destRect, sourceRect, tintColor);
              }
              
              // 然后在上面渲染草地纹理（如果有的话）
              // 由于当前草地使用的是泥土纹理，这里可以添加一个绿色的色调
              Color grassTint = Color.Lerp(tintColor, Color.LightGreen, 0.3f);
              if (tileTextures != null && tileTextures.TryGetValue(TileType.Grass, out Texture2D grassTexture) && grassTexture != null)
              {
                  spriteBatch.Draw(grassTexture, destRect, sourceRect, grassTint);
              }
          }
          
          /// <summary>
          /// 渲染丛林草地方块
          /// </summary>
          private void RenderJungleGrassTile(Rectangle destRect, Rectangle sourceRect, Color tintColor, int worldX, int worldY)
          {
              // 先渲染泥土作为基础
              if (tileTextures != null && tileTextures.TryGetValue(TileType.Dirt, out Texture2D dirtTexture) && dirtTexture != null)
              {
                  spriteBatch.Draw(dirtTexture, destRect, sourceRect, tintColor);
              }
              
              // 然后渲染丛林草地纹理，使用深绿色调
              Color jungleGrassTint = Color.Lerp(tintColor, Color.DarkGreen, 0.4f);
              if (tileTextures != null && tileTextures.TryGetValue(TileType.JungleGrass, out Texture2D jungleGrassTexture) && jungleGrassTexture != null)
              {
                  spriteBatch.Draw(jungleGrassTexture, destRect, sourceRect, jungleGrassTint);
              }
          }
          
          // 注意：斜坡渲染逻辑已移至Chunk系统中，此处不再重复实现
          

          

          
  

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            pixelTexture?.Dispose();
            chunkManager?.Dispose();
            backgroundWall?.Dispose();
        }
    }
}