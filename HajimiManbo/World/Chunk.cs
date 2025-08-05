using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using HajimiManbo.Lighting;

namespace HajimiManbo.World
{
    /// <summary>
    /// 地图分块，用于优化大地图渲染性能
    /// 每个Chunk包含16x16个tile
    /// </summary>
    public class Chunk
    {
        public const int CHUNK_SIZE = 16; // 每个chunk的大小
        public const int TILE_SIZE = 16; // 每个tile的像素大小
        
        public int ChunkX { get; private set; }
        public int ChunkY { get; private set; }
        public Rectangle WorldBounds { get; private set; }
        
        private Dictionary<TileType, VertexBuffer> _vertexBuffers;
        private Dictionary<TileType, IndexBuffer> _indexBuffers;
        private Dictionary<TileType, int> _vertexCounts;
        private Dictionary<TileType, int> _indexCounts;
        

        private bool _isDirty = true;
        private GraphicsDevice _graphicsDevice;
        private World _world;
        private ChunkManager _chunkManager;
        
        private Dictionary<TileType, Texture2D> _tileTextures;
        private WorldRenderer _worldRenderer; // 用于获取光照信息
        
        public Chunk(int chunkX, int chunkY, World world, GraphicsDevice graphicsDevice, Dictionary<TileType, Texture2D> tileTextures, ChunkManager chunkManager, WorldRenderer worldRenderer = null)
        {
            ChunkX = chunkX;
            ChunkY = chunkY;
            _world = world;
            _graphicsDevice = graphicsDevice;
            _tileTextures = tileTextures;
            _chunkManager = chunkManager;
            _worldRenderer = worldRenderer;
            
            // 初始化缓冲区字典
            _vertexBuffers = new Dictionary<TileType, VertexBuffer>();
            _indexBuffers = new Dictionary<TileType, IndexBuffer>();
            _vertexCounts = new Dictionary<TileType, int>();
            _indexCounts = new Dictionary<TileType, int>();
            

            
            // 计算世界坐标边界
            WorldBounds = new Rectangle(
                chunkX * CHUNK_SIZE * TILE_SIZE,
                chunkY * CHUNK_SIZE * TILE_SIZE,
                CHUNK_SIZE * TILE_SIZE,
                CHUNK_SIZE * TILE_SIZE
            );
        }
        
        /// <summary>
        /// 标记chunk为脏，需要重新生成顶点缓冲
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }
        
        /// <summary>
        /// 构建顶点缓冲区 - 使用共享边缘避免浮点精度问题
        /// </summary>
        public void BuildVertexBuffer()
        {
            if (!_isDirty) return;
            
            // 释放旧的缓冲区
            foreach (var vb in _vertexBuffers.Values) vb?.Dispose();
            foreach (var ib in _indexBuffers.Values) ib?.Dispose();
            _vertexBuffers.Clear();
            _indexBuffers.Clear();
            _vertexCounts.Clear();
            _indexCounts.Clear();
            

            
            int startX = ChunkX * CHUNK_SIZE;
            int startY = ChunkY * CHUNK_SIZE;
            int endX = Math.Min(startX + CHUNK_SIZE, _world.Width);
            int endY = Math.Min(startY + CHUNK_SIZE, _world.Height);
            
            // 预计算所有的x和y坐标以避免浮点精度问题
            var tileXs = new float[endX - startX + 1];
            var tileYs = new float[endY - startY + 1];
            
            for (int i = 0; i < tileXs.Length; i++)
            {
                tileXs[i] = (startX + i) * TILE_SIZE;
            }
            for (int i = 0; i < tileYs.Length; i++)
            {
                tileYs[i] = (startY + i) * TILE_SIZE;
            }
            
            // 按瓦片类型分组收集顶点
            var tileGroups = new Dictionary<TileType, List<VertexPositionColorTexture>>();
            var tileIndices = new Dictionary<TileType, List<int>>();
            
            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    var tile = _world.GetTile(x, y);
                    
                    // 处理前景方块
                    if (tile.Type == TileType.Air) continue;
                    
                    if (!tileGroups.ContainsKey(tile.Type))
                    {
                        tileGroups[tile.Type] = new List<VertexPositionColorTexture>();
                        tileIndices[tile.Type] = new List<int>();
                    }
                    
                    // 使用预计算的坐标（共享边缘）
                    int localX = x - startX;
                    int localY = y - startY;
                    float worldX = tileXs[localX];
                    float worldY = tileYs[localY];
                    float worldX1 = tileXs[localX + 1];
                    float worldY1 = tileYs[localY + 1];
                    
                    // 使用泰拉瑞亚47帧连接系统
                    var tex = _chunkManager.GetTileTexture(tile.Type);
                    
                    // 计算正确的连接帧
                    byte frameIndex = CalculateConnectionFrame(tile.Type, x, y);
                    var srcRect = TileAtlas.GetSourceRect(0, frameIndex, tex);
                    
                    // 计算UV坐标，添加微小内缩防止纹理出血（泰拉瑞亚标准）
                    const float epsilon = 1.5f; // 1.5像素内缩，防止采样相邻瓦片和浮点精度问题
                    float u0 = (srcRect.X + epsilon) / (float)tex.Width;
                    float v0 = (srcRect.Y + epsilon) / (float)tex.Height;
                    float u1 = (srcRect.X + srcRect.Width - epsilon) / (float)tex.Width;
                    float v1 = (srcRect.Y + srcRect.Height - epsilon) / (float)tex.Height;
                    
                    int vertexIndex = tileGroups[tile.Type].Count;
                    
                    // 获取光照颜色
                    Color lightColor = Color.White;
                    if (_worldRenderer?.GetLightingSystem() != null)
                    {
                        lightColor = _worldRenderer.GetLightingSystem().GetLightColor(x, y);
                    }
                    
                    // 添加四个顶点（使用共享边缘坐标和光照）
                    tileGroups[tile.Type].AddRange(new[]
                    {
                        new VertexPositionColorTexture(new Vector3(worldX, worldY, 0), lightColor, new Vector2(u0, v0)),
                        new VertexPositionColorTexture(new Vector3(worldX1, worldY, 0), lightColor, new Vector2(u1, v0)),
                        new VertexPositionColorTexture(new Vector3(worldX, worldY1, 0), lightColor, new Vector2(u0, v1)),
                        new VertexPositionColorTexture(new Vector3(worldX1, worldY1, 0), lightColor, new Vector2(u1, v1))
                    });
                    
                    // 添加索引
                    tileIndices[tile.Type].AddRange(new[]
                    {
                        vertexIndex, vertexIndex + 1, vertexIndex + 2,
                        vertexIndex + 1, vertexIndex + 3, vertexIndex + 2
                    });
                }
            }
            

            
            // 为每种瓦片类型创建缓冲区
            foreach (var kvp in tileGroups)
            {
                var tileType = kvp.Key;
                var vertices = kvp.Value;
                var indices = tileIndices[tileType];
                
                if (vertices.Count > 0)
                {
                    _vertexCounts[tileType] = vertices.Count;
                    _indexCounts[tileType] = indices.Count;
                    
                    _vertexBuffers[tileType] = new VertexBuffer(_graphicsDevice, typeof(VertexPositionColorTexture), vertices.Count, BufferUsage.WriteOnly);
                    _vertexBuffers[tileType].SetData(vertices.ToArray());
                    
                    _indexBuffers[tileType] = new IndexBuffer(_graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
                    _indexBuffers[tileType].SetData(indices.ToArray());
                }
            }
            

            
            _isDirty = false;
        }
        
        /// <summary>
        /// 渲染chunk
        /// </summary>
        public void Render(GraphicsDevice graphicsDevice, BasicEffect effect)
        {
            foreach (var kvp in _vertexBuffers)
            {
                var tileType = kvp.Key;
                var vertexBuffer = kvp.Value;
                var indexBuffer = _indexBuffers[tileType];
                var indexCount = _indexCounts[tileType];
                
                if (indexCount == 0 || !_tileTextures.ContainsKey(tileType)) continue;
                
                effect.Texture = _tileTextures[tileType];
                effect.DiffuseColor = Vector3.One;
                effect.TextureEnabled = true;
                effect.VertexColorEnabled = true;
                effect.Alpha = 1.0f;
                
                graphicsDevice.SetVertexBuffer(vertexBuffer);
                graphicsDevice.Indices = indexBuffer;
                
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        0,
                        indexCount / 3
                    );
                }
            }
        }
        
        /// <summary>
        /// 检查chunk是否在视口内
        /// </summary>
        public bool IsInView(Rectangle viewBounds)
        {
            return WorldBounds.Intersects(viewBounds);
        }
        
        /// <summary>
        /// 获取tile颜色
        /// </summary>
        private Color GetTileColor(TileType tileType)
        {
            return tileType switch
            {
                TileType.Dirt => Color.SaddleBrown,
                TileType.Stone => Color.Gray,
                TileType.Grass => Color.Green,
                TileType.Sand => Color.Yellow,
                TileType.Snow => Color.White,
                TileType.JungleGrass => Color.DarkGreen,
                _ => Color.White
            };
        }
        

        
        /// <summary>
        /// 计算表面砖块的帧索引（基于暴露面的自定义规则）
        /// </summary>
        private byte CalculateConnectionFrame(TileType tileType, int worldX, int worldY)
        {
            if (_world == null)
                return 0;
            
            // 检查四个方向的连接情况（连接表示有相同方块，不连接表示暴露）
            bool up = CanConnectTo(worldX, worldY - 1, tileType);
            bool down = CanConnectTo(worldX, worldY + 1, tileType);
            bool left = CanConnectTo(worldX - 1, worldY, tileType);
            bool right = CanConnectTo(worldX + 1, worldY, tileType);
            
            // 暴露面检测（true表示该方向暴露）
            bool exposedUp = !up;
            bool exposedDown = !down;
            bool exposedLeft = !left;
            bool exposedRight = !right;
            
            // 生成伪随机数用于选择
            int hash = Math.Abs((worldX * 374761393 + worldY * 668265263) % 3);
            
            // 根据暴露情况选择对应的帧
            if (exposedLeft && !exposedRight && !exposedUp && !exposedDown)
            {
                // 只有左边暴露：(0,1), (0,2), (0,3)
                byte[] frames = { (byte)(1 * 16 + 0), (byte)(2 * 16 + 0), (byte)(3 * 16 + 0) };
                return frames[hash];
            }
            else if (exposedRight && !exposedLeft && !exposedUp && !exposedDown)
            {
                // 只有右边暴露：(4,1), (4,2), (4,3)
                byte[] frames = { (byte)(1 * 16 + 4), (byte)(2 * 16 + 4), (byte)(3 * 16 + 4) };
                return frames[hash];
            }
            else if (exposedUp && !exposedDown && !exposedLeft && !exposedRight)
            {
                // 只有上面暴露：(1,0), (2,0), (3,0)
                byte[] frames = { (byte)(0 * 16 + 1), (byte)(0 * 16 + 2), (byte)(0 * 16 + 3) };
                return frames[hash];
            }
            else if (exposedDown && !exposedUp && !exposedLeft && !exposedRight)
            {
                // 只有下面暴露：(1,2), (2,2), (3,2)
                byte[] frames = { (byte)(2 * 16 + 1), (byte)(2 * 16 + 2), (byte)(2 * 16 + 3) };
                return frames[hash];
            }
            else if (exposedUp && exposedDown && !exposedLeft && !exposedRight)
            {
                // 上下暴露：(6,4), (7,4), (8,4)
                byte[] frames = { (byte)(4 * 16 + 6), (byte)(4 * 16 + 7), (byte)(4 * 16 + 8) };
                return frames[hash];
            }
            else if (exposedLeft && exposedRight && !exposedUp && !exposedDown)
            {
                // 左右暴露：(5,0), (5,1), (5,2)
                byte[] frames = { (byte)(0 * 16 + 5), (byte)(1 * 16 + 5), (byte)(2 * 16 + 5) };
                return frames[hash];
            }
            else if (exposedUp && exposedLeft && !exposedDown && !exposedRight)
            {
                // 上左暴露：(0,3), (2,3), (4,3)
                byte[] frames = { (byte)(3 * 16 + 0), (byte)(3 * 16 + 2), (byte)(3 * 16 + 4) };
                return frames[hash];
            }
            else if (exposedUp && exposedRight && !exposedDown && !exposedLeft)
            {
                // 右上暴露：(1,3), (3,3), (5,3)
                byte[] frames = { (byte)(3 * 16 + 1), (byte)(3 * 16 + 3), (byte)(3 * 16 + 5) };
                return frames[hash];
            }
            else if (exposedLeft && exposedDown && !exposedUp && !exposedRight)
            {
                // 左下暴露：(0,4), (2,4), (4,4)
                byte[] frames = { (byte)(4 * 16 + 0), (byte)(4 * 16 + 2), (byte)(4 * 16 + 4) };
                return frames[hash];
            }
            else if (exposedRight && exposedDown && !exposedUp && !exposedLeft)
            {
                // 右下暴露：(1,4), (3,4), (5,4)
                byte[] frames = { (byte)(4 * 16 + 1), (byte)(4 * 16 + 3), (byte)(4 * 16 + 5) };
                return frames[hash];
            }
            else if (exposedUp && exposedLeft && exposedDown && !exposedRight)
            {
                // 上左下暴露：(9,0), (9,1), (9,2)
                byte[] frames = { (byte)(0 * 16 + 9), (byte)(1 * 16 + 9), (byte)(2 * 16 + 9) };
                return frames[hash];
            }
            else if (exposedUp && exposedRight && exposedDown && !exposedLeft)
            {
                // 上右下暴露：(12,0), (12,1), (12,2)
                byte[] frames = { (byte)(0 * 16 + 12), (byte)(1 * 16 + 12), (byte)(2 * 16 + 12) };
                return frames[hash];
            }
            else if (exposedLeft && exposedUp && exposedRight && !exposedDown)
            {
                // 左上右暴露：(6,0), (7,0), (8,0)
                byte[] frames = { (byte)(0 * 16 + 6), (byte)(0 * 16 + 7), (byte)(0 * 16 + 8) };
                return frames[hash];
            }
            else if (exposedLeft && exposedDown && exposedRight && !exposedUp)
            {
                // 左下右暴露：(6,3), (7,3), (8,3)
                byte[] frames = { (byte)(3 * 16 + 6), (byte)(3 * 16 + 7), (byte)(3 * 16 + 8) };
                return frames[hash];
            }
            else if (exposedUp && exposedDown && exposedLeft && exposedRight)
            {
                // 全部暴露：(9,3), (10,3), (11,3)
                byte[] frames = { (byte)(3 * 16 + 9), (byte)(3 * 16 + 10), (byte)(3 * 16 + 11) };
                return frames[hash];
            }
            else
            {
                // 完全连接（四周都有相同物块）：使用之前定义的随机选择
                byte[] availableFrames = {
                    (byte)(1 * 16 + 1),   // (1,1) = 17
                    (byte)(1 * 16 + 2),   // (2,1) = 18
                    (byte)(1 * 16 + 3),   // (3,1) = 19
                    (byte)(1 * 16 + 6),   // (6,1) = 22
                    (byte)(1 * 16 + 7),   // (7,1) = 23
                    (byte)(1 * 16 + 8),   // (8,1) = 24
                    (byte)(0 * 16 + 10),  // (10,0) = 10
                    (byte)(0 * 16 + 11),  // (11,0) = 11
                    (byte)(1 * 16 + 10),  // (10,1) = 26
                    (byte)(1 * 16 + 11),  // (11,1) = 27
                    (byte)(2 * 16 + 6),   // (6,2) = 38
                    (byte)(2 * 16 + 7),   // (7,2) = 39
                    (byte)(2 * 16 + 8),   // (8,2) = 40
                    (byte)(2 * 16 + 10),  // (10,2) = 42
                    (byte)(2 * 16 + 11)   // (11,2) = 43
                };
                
                int fullHash = Math.Abs((worldX * 374761393 + worldY * 668265263) % availableFrames.Length);
                return availableFrames[fullHash];
            }
        }
        
        /// <summary>
        /// 检查指定位置的方块是否可以与给定类型连接
        /// </summary>
        private bool CanConnectTo(int x, int y, TileType tileType)
        {
            if (_world == null || !_world.IsValidCoordinate(x, y))
                return false;
                
            var tile = _world.GetTile(x, y);
            
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
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            foreach (var buffer in _vertexBuffers.Values)
            {
                buffer?.Dispose();
            }
            foreach (var buffer in _indexBuffers.Values)
            {
                buffer?.Dispose();
            }
            _vertexBuffers.Clear();
            _indexBuffers.Clear();
            _vertexCounts.Clear();
            _indexCounts.Clear();
            

        }
    }
}