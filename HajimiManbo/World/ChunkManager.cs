using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace HajimiManbo.World
{
    /// <summary>
    /// Chunk管理器，负责管理所有地图分块和优化渲染
    /// </summary>
    public class ChunkManager
    {
        private Chunk[,] _chunks;
        private int _chunksWidth;
        private int _chunksHeight;
        private World _world;
        private GraphicsDevice _graphicsDevice;
        private BasicEffect _effect;
        private Dictionary<TileType, Texture2D> _tileTextures;
        private ContentManager _contentManager;
        private WorldRenderer _worldRenderer;
        
        public ChunkManager(World world, GraphicsDevice graphicsDevice, ContentManager contentManager, WorldRenderer worldRenderer = null)
        {
            _world = world;
            _graphicsDevice = graphicsDevice;
            _contentManager = contentManager;
            _worldRenderer = worldRenderer;
            
            // 计算需要的chunk数量
            _chunksWidth = (int)Math.Ceiling((float)world.Width / Chunk.CHUNK_SIZE);
            _chunksHeight = (int)Math.Ceiling((float)world.Height / Chunk.CHUNK_SIZE);
            
            // 加载贴图
            LoadTileTextures();
            
            // 创建chunks数组
            _chunks = new Chunk[_chunksWidth, _chunksHeight];
            
            // 初始化所有chunks
            for (int x = 0; x < _chunksWidth; x++)
            {
                for (int y = 0; y < _chunksHeight; y++)
                {
                    _chunks[x, y] = new Chunk(x, y, world, graphicsDevice, _tileTextures, this, _worldRenderer);
                }
            }
            
            // 创建基础效果器
            _effect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true,   // 启用顶点颜色以支持背景墙半透明效果
                TextureEnabled = true
            };
            
            // 构建所有chunks的顶点缓冲
            BuildAllChunks();
        }
        
        /// <summary>
        /// 加载47帧图集贴图
        /// </summary>
        private void LoadTileTextures()
        {
            _tileTextures = new Dictionary<TileType, Texture2D>();
            
            try
            {
                // 按照泰拉瑞亚标准加载贴图：TileID对应Tiles_X.png
                _tileTextures[TileType.Dirt] = _contentManager.Load<Texture2D>("Tiles/Tiles_0");      // TileID 0: 土
                _tileTextures[TileType.Stone] = _contentManager.Load<Texture2D>("Tiles/Tiles_1");     // TileID 1: 石
                _tileTextures[TileType.Grass] = _contentManager.Load<Texture2D>("Tiles/Tiles_2");     // TileID 2: 草
                _tileTextures[TileType.Sand] = _contentManager.Load<Texture2D>("Tiles/Tiles_53");     // TileID 53: 沙
                _tileTextures[TileType.Snow] = _contentManager.Load<Texture2D>("Tiles/Tiles_147");    // TileID 147: 雪
                _tileTextures[TileType.JungleGrass] = _contentManager.Load<Texture2D>("Tiles/Tiles_2"); // TileID 367: 丛林草
                _tileTextures[TileType.Marble] = _contentManager.Load<Texture2D>("Tiles/Tiles_367");    // 大理石使用石头图集
                _tileTextures[TileType.Tiles_189] = _contentManager.Load<Texture2D>("Tiles/Tiles_189"); // 空岛方块使用189号图集
                
                Console.WriteLine("[ChunkManager] Successfully loaded 47-frame tile atlases");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChunkManager] Failed to load 47-frame tile atlases: {ex.Message}");
                Console.WriteLine("[ChunkManager] Falling back to single tile textures...");
                
                try
                {
                    // 后备方案：加载单个瓦片纹理
                    _tileTextures[TileType.Dirt] = _contentManager.Load<Texture2D>("Tiles/Dirt_Block_(placed)");
                    _tileTextures[TileType.Stone] = _contentManager.Load<Texture2D>("Tiles/Stone_Block_(placed)");
                    _tileTextures[TileType.Sand] = _contentManager.Load<Texture2D>("Tiles/Sand_Block_(placed)");
                    _tileTextures[TileType.Snow] = _contentManager.Load<Texture2D>("Tiles/Snow_Block_(placed)");
                    _tileTextures[TileType.Marble] = _contentManager.Load<Texture2D>("Tiles/Marble_Block_(placed)");
                    _tileTextures[TileType.Grass] = _contentManager.Load<Texture2D>("Tiles/Dirt_Block_(placed)");
                    _tileTextures[TileType.JungleGrass] = _contentManager.Load<Texture2D>("Tiles/Dirt_Block_(placed)");
                    _tileTextures[TileType.Tiles_189] = _contentManager.Load<Texture2D>("Tiles/Stone_Block_(placed)");
                    
                    Console.WriteLine("[ChunkManager] Successfully loaded fallback single tile textures");
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"[ChunkManager] Fallback also failed: {fallbackEx.Message}");
                    
                    // 最终后备方案：创建默认纹理
                    var defaultTexture = new Texture2D(_graphicsDevice, 256, 192); // 47帧图集尺寸
                    var colorData = new Color[256 * 192];
                    for (int i = 0; i < colorData.Length; i++)
                        colorData[i] = Color.White;
                    defaultTexture.SetData(colorData);
                    
                    foreach (TileType tileType in Enum.GetValues<TileType>())
                    {
                        if (tileType != TileType.Air && !_tileTextures.ContainsKey(tileType))
                        {
                            _tileTextures[tileType] = defaultTexture;
                        }
                    }
                }
            }
        }
        

        
        /// <summary>
        /// 获取指定瓦片类型的纹理
        /// </summary>
        /// <param name="tileType">瓦片类型</param>
        /// <returns>对应的纹理</returns>
        public Texture2D GetTileTexture(TileType tileType)
        {
            return _tileTextures.TryGetValue(tileType, out var texture) ? texture : _tileTextures[TileType.Dirt];
        }
        

        
        /// <summary>
        /// 构建所有chunks的顶点缓冲
        /// </summary>
        public void BuildAllChunks()
        {
            for (int x = 0; x < _chunksWidth; x++)
            {
                for (int y = 0; y < _chunksHeight; y++)
                {
                    _chunks[x, y].BuildVertexBuffer();
                }
            }
        }
        
        /// <summary>
        /// 标记指定区域的chunks为脏
        /// </summary>
        public void MarkChunksDirty(int tileX, int tileY, int radius = 1)
        {
            int chunkX = tileX / Chunk.CHUNK_SIZE;
            int chunkY = tileY / Chunk.CHUNK_SIZE;
            
            for (int x = Math.Max(0, chunkX - radius); x <= Math.Min(_chunksWidth - 1, chunkX + radius); x++)
            {
                for (int y = Math.Max(0, chunkY - radius); y <= Math.Min(_chunksHeight - 1, chunkY + radius); y++)
                {
                    _chunks[x, y].MarkDirty();
                }
            }
        }
        
        /// <summary>
        /// 渲染可见的chunks
        /// </summary>
        public void Render(Matrix viewMatrix, Matrix projectionMatrix, Rectangle viewBounds)
{
    // 保持正确的 GPU 状态
    _graphicsDevice.RasterizerState   = RasterizerState.CullNone;
    _graphicsDevice.DepthStencilState = DepthStencilState.Default; // 启用深度测试
    _graphicsDevice.BlendState        = BlendState.AlphaBlend;  // 使用Alpha混合

    // 效果器设定
    _effect.View           = viewMatrix;
    _effect.Projection     = projectionMatrix;
    _effect.World          = Matrix.Identity;
    _effect.TextureEnabled = true;
    _effect.VertexColorEnabled = true;
    
    // Debug: 打印相机矩阵验证
    Console.WriteLine($"View 矩阵：\n{viewMatrix}");
    Console.WriteLine($"Projection 矩阵：\n{projectionMatrix}");

    // 计算可见的chunk范围
    int startChunkX = Math.Max(0, viewBounds.Left   / (Chunk.CHUNK_SIZE * Chunk.TILE_SIZE));
    int endChunkX   = Math.Min(_chunksWidth - 1,
                              viewBounds.Right  / (Chunk.CHUNK_SIZE * Chunk.TILE_SIZE));
    int startChunkY = Math.Max(0, viewBounds.Top    / (Chunk.CHUNK_SIZE * Chunk.TILE_SIZE));
    int endChunkY   = Math.Min(_chunksHeight - 1,
                              viewBounds.Bottom / (Chunk.CHUNK_SIZE * Chunk.TILE_SIZE));

    // 遍历并渲染所有可见的chunks
    for (int x = startChunkX; x <= endChunkX; x++)
    {
        for (int y = startChunkY; y <= endChunkY; y++)
        {
            var chunk = _chunks[x, y];
            if (!chunk.IsInView(viewBounds))
                continue;

            // 如果chunk是脏的，重新构建顶点缓冲（包含背景墙和前景方块）
            chunk.BuildVertexBuffer();

            // 渲染该chunk（内部已按“先墙后块”的顺序绘制）
            chunk.Render(_graphicsDevice, _effect);
        }
    }
}

        
        /// <summary>
        /// 当tile发生变化时调用
        /// </summary>
        public void OnTileChanged(int tileX, int tileY)
        {
            MarkChunksDirty(tileX, tileY);
        }
        
        /// <summary>
        /// 获取渲染统计信息
        /// </summary>
        public RenderStats GetRenderStats(Rectangle viewBounds)
        {
            int visibleChunks = 0;
            int totalChunks = _chunksWidth * _chunksHeight;
            
            int startChunkX = Math.Max(0, viewBounds.Left / (Chunk.CHUNK_SIZE * Chunk.TILE_SIZE));
            int endChunkX = Math.Min(_chunksWidth - 1, viewBounds.Right / (Chunk.CHUNK_SIZE * Chunk.TILE_SIZE));
            int startChunkY = Math.Max(0, viewBounds.Top / (Chunk.CHUNK_SIZE * Chunk.TILE_SIZE));
            int endChunkY = Math.Min(_chunksHeight - 1, viewBounds.Bottom / (Chunk.CHUNK_SIZE * Chunk.TILE_SIZE));
            
            for (int x = startChunkX; x <= endChunkX; x++)
            {
                for (int y = startChunkY; y <= endChunkY; y++)
                {
                    if (_chunks[x, y].IsInView(viewBounds))
                    {
                        visibleChunks++;
                    }
                }
            }
            
            return new RenderStats
            {
                VisibleChunks = visibleChunks,
                TotalChunks = totalChunks,
                ChunkSize = Chunk.CHUNK_SIZE
            };
        }
        
        /// <summary>
        /// 标记所有chunk为脏状态
        /// </summary>
        public void MarkAllDirty()
        {
            for (int x = 0; x < _chunksWidth; x++)
            {
                for (int y = 0; y < _chunksHeight; y++)
                {
                    _chunks[x, y].MarkDirty();
                }
            }
        }
        
        /// <summary>
        /// 重建所有chunk的顶点缓冲区
        /// </summary>
        public void RebuildAll()
        {
            Console.WriteLine("[ChunkManager] Starting rebuild of all chunks...");
            MarkAllDirty();
            
            for (int x = 0; x < _chunksWidth; x++)
            {
                for (int y = 0; y < _chunksHeight; y++)
                {
                    _chunks[x, y].BuildVertexBuffer();
                }
            }
            
            Console.WriteLine("[ChunkManager] Rebuild completed.");
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            for (int x = 0; x < _chunksWidth; x++)
            {
                for (int y = 0; y < _chunksHeight; y++)
                {
                    _chunks[x, y]?.Dispose();
                }
            }
            
            _effect?.Dispose();
            
            // 释放贴图资源
            if (_tileTextures != null)
            {
                foreach (var texture in _tileTextures.Values)
                {
                    texture?.Dispose();
                }
            }
        }
    }
    
    /// <summary>
    /// 渲染统计信息
    /// </summary>
    public struct RenderStats
    {
        public int VisibleChunks;
        public int TotalChunks;
        public int ChunkSize;
    }
}