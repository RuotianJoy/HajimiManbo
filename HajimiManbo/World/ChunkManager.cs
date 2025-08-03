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
        
        public ChunkManager(World world, GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            _world = world;
            _graphicsDevice = graphicsDevice;
            _contentManager = contentManager;
            
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
                    _chunks[x, y] = new Chunk(x, y, world, graphicsDevice, _tileTextures);
                }
            }
            
            // 创建基础效果器
            _effect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = false,
                TextureEnabled = true
            };
            
            // 构建所有chunks的顶点缓冲
            BuildAllChunks();
        }
        
        /// <summary>
        /// 加载tile贴图
        /// </summary>
        private void LoadTileTextures()
        {
            _tileTextures = new Dictionary<TileType, Texture2D>();
            
            try
            {
                // 加载带(placed)后缀的贴图文件
                _tileTextures[TileType.Dirt] = _contentManager.Load<Texture2D>("Tiles/Dirt_Block_(placed)");
                _tileTextures[TileType.Stone] = _contentManager.Load<Texture2D>("Tiles/Stone_Block_(placed)");
                _tileTextures[TileType.Sand] = _contentManager.Load<Texture2D>("Tiles/Sand_Block_(placed)");
                _tileTextures[TileType.Snow] = _contentManager.Load<Texture2D>("Tiles/Snow_Block_(placed)");
                _tileTextures[TileType.Marble] = _contentManager.Load<Texture2D>("Tiles/Marble_Block_(placed)");
                
                // 为草地和丛林草地使用泥土贴图作为基础
                _tileTextures[TileType.Grass] = _contentManager.Load<Texture2D>("Tiles/Dirt_Block_(placed)");
                _tileTextures[TileType.JungleGrass] = _contentManager.Load<Texture2D>("Tiles/Dirt_Block_(placed)");
                
                Console.WriteLine("[ChunkManager] Successfully loaded tile textures with (placed) suffix");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChunkManager] Failed to load tile textures: {ex.Message}");
                
                // 创建默认16x16白色纹理作为后备
                var defaultTexture = new Texture2D(_graphicsDevice, 16, 16);
                var colorData = new Color[16 * 16];
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
            // 设置效果器矩阵
            _effect.View = viewMatrix;
            _effect.Projection = projectionMatrix;
            _effect.World = Matrix.Identity;
            
            // 计算可见的chunk范围
            int startChunkX = Math.Max(0, viewBounds.Left / (Chunk.CHUNK_SIZE * Chunk.TILE_SIZE));
            int endChunkX = Math.Min(_chunksWidth - 1, viewBounds.Right / (Chunk.CHUNK_SIZE * Chunk.TILE_SIZE));
            int startChunkY = Math.Max(0, viewBounds.Top / (Chunk.CHUNK_SIZE * Chunk.TILE_SIZE));
            int endChunkY = Math.Min(_chunksHeight - 1, viewBounds.Bottom / (Chunk.CHUNK_SIZE * Chunk.TILE_SIZE));
            
            // 渲染可见的chunks
            for (int x = startChunkX; x <= endChunkX; x++)
            {
                for (int y = startChunkY; y <= endChunkY; y++)
                {
                    var chunk = _chunks[x, y];
                    if (chunk.IsInView(viewBounds))
                    {
                        // 如果chunk是脏的，重新构建顶点缓冲
                        chunk.BuildVertexBuffer();
                        
                        // 渲染chunk
                        chunk.Render(_graphicsDevice, _effect);
                    }
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