using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

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
        
        private VertexBuffer _vertexBuffer;
        private IndexBuffer _indexBuffer;
        private bool _isDirty = true;
        private int _vertexCount;
        private int _indexCount;
        private GraphicsDevice _graphicsDevice;
        private World _world;
        
        private Dictionary<TileType, Texture2D> _tileTextures;
        
        public Chunk(int chunkX, int chunkY, World world, GraphicsDevice graphicsDevice, Dictionary<TileType, Texture2D> tileTextures)
        {
            ChunkX = chunkX;
            ChunkY = chunkY;
            _world = world;
            _graphicsDevice = graphicsDevice;
            _tileTextures = tileTextures;
            
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
        /// 生成顶点缓冲
        /// </summary>
        public void BuildVertexBuffer()
        {
            if (!_isDirty) return;
            
            var vertices = new List<VertexPositionColorTexture>();
            var indices = new List<int>();
            
            int startX = ChunkX * CHUNK_SIZE;
            int startY = ChunkY * CHUNK_SIZE;
            int endX = Math.Min(startX + CHUNK_SIZE, _world.Width);
            int endY = Math.Min(startY + CHUNK_SIZE, _world.Height);
            
            int vertexIndex = 0;
            
            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    var tile = _world.GetTile(x, y);
                    if (tile.Type == TileType.Air) continue;
                    
                    // 计算tile的世界坐标
                    float worldX = x * TILE_SIZE;
                    float worldY = y * TILE_SIZE;
                    
                    // 创建四个顶点（quad）使用白色，贴图将提供颜色
                    vertices.Add(new VertexPositionColorTexture(
                        new Vector3(worldX, worldY, 0),
                        Color.White,
                        new Vector2(0, 0)
                    ));
                    vertices.Add(new VertexPositionColorTexture(
                        new Vector3(worldX + TILE_SIZE, worldY, 0),
                        Color.White,
                        new Vector2(1, 0)
                    ));
                    vertices.Add(new VertexPositionColorTexture(
                        new Vector3(worldX, worldY + TILE_SIZE, 0),
                        Color.White,
                        new Vector2(0, 1)
                    ));
                    vertices.Add(new VertexPositionColorTexture(
                        new Vector3(worldX + TILE_SIZE, worldY + TILE_SIZE, 0),
                        Color.White,
                        new Vector2(1, 1)
                    ));
                    
                    // 创建两个三角形的索引
                    indices.Add(vertexIndex);
                    indices.Add(vertexIndex + 1);
                    indices.Add(vertexIndex + 2);
                    
                    indices.Add(vertexIndex + 1);
                    indices.Add(vertexIndex + 3);
                    indices.Add(vertexIndex + 2);
                    
                    vertexIndex += 4;
                }
            }
            
            _vertexCount = vertices.Count;
            _indexCount = indices.Count;
            
            if (_vertexCount > 0)
            {
                // 释放旧的缓冲区
                _vertexBuffer?.Dispose();
                _indexBuffer?.Dispose();
                
                // 创建新的缓冲区
                _vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionColorTexture), _vertexCount, BufferUsage.WriteOnly);
                _vertexBuffer.SetData(vertices.ToArray());
                
                _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.ThirtyTwoBits, _indexCount, BufferUsage.WriteOnly);
                _indexBuffer.SetData(indices.ToArray());
            }
            
            _isDirty = false;
        }
        
        /// <summary>
        /// 渲染chunk
        /// </summary>
        public void Render(GraphicsDevice graphicsDevice, BasicEffect effect)
        {
            if (_vertexBuffer == null || _indexBuffer == null || _vertexCount == 0) return;
            
            // 按tile类型分组渲染
            RenderByTileType(graphicsDevice, effect);
        }
        
        /// <summary>
        /// 按tile类型分组渲染
        /// </summary>
        private void RenderByTileType(GraphicsDevice graphicsDevice, BasicEffect effect)
        {
            int startX = ChunkX * CHUNK_SIZE;
            int startY = ChunkY * CHUNK_SIZE;
            int endX = Math.Min(startX + CHUNK_SIZE, _world.Width);
            int endY = Math.Min(startY + CHUNK_SIZE, _world.Height);
            
            // 为每种tile类型收集顶点
            var tileGroups = new Dictionary<TileType, List<VertexPositionColorTexture>>();
            var tileIndices = new Dictionary<TileType, List<int>>();
            
            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    var tile = _world.GetTile(x, y);
                    if (tile.Type == TileType.Air) continue;
                    
                    if (!tileGroups.ContainsKey(tile.Type))
                    {
                        tileGroups[tile.Type] = new List<VertexPositionColorTexture>();
                        tileIndices[tile.Type] = new List<int>();
                    }
                    
                    float worldX = x * TILE_SIZE;
                    float worldY = y * TILE_SIZE;
                    
                    int vertexIndex = tileGroups[tile.Type].Count;
                    
                    // 添加四个顶点
                    tileGroups[tile.Type].AddRange(new[]
                    {
                        new VertexPositionColorTexture(new Vector3(worldX, worldY, 0), Color.White, new Vector2(0, 0)),
                        new VertexPositionColorTexture(new Vector3(worldX + TILE_SIZE, worldY, 0), Color.White, new Vector2(1, 0)),
                        new VertexPositionColorTexture(new Vector3(worldX, worldY + TILE_SIZE, 0), Color.White, new Vector2(0, 1)),
                        new VertexPositionColorTexture(new Vector3(worldX + TILE_SIZE, worldY + TILE_SIZE, 0), Color.White, new Vector2(1, 1))
                    });
                    
                    // 添加索引
                    tileIndices[tile.Type].AddRange(new[]
                    {
                        vertexIndex, vertexIndex + 1, vertexIndex + 2,
                        vertexIndex + 1, vertexIndex + 3, vertexIndex + 2
                    });
                }
            }
            
            // 渲染每种tile类型
            foreach (var kvp in tileGroups)
            {
                var tileType = kvp.Key;
                var vertices = kvp.Value;
                var indices = tileIndices[tileType];
                
                if (vertices.Count == 0) continue;
                
                // 设置对应的贴图
                if (_tileTextures.ContainsKey(tileType))
                {
                    effect.Texture = _tileTextures[tileType];
                }
                
                // 创建临时缓冲区
                using (var vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColorTexture), vertices.Count, BufferUsage.WriteOnly))
                using (var indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly))
                {
                    vertexBuffer.SetData(vertices.ToArray());
                    indexBuffer.SetData(indices.ToArray());
                    
                    graphicsDevice.SetVertexBuffer(vertexBuffer);
                    graphicsDevice.Indices = indexBuffer;
                    
                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        graphicsDevice.DrawIndexedPrimitives(
                            PrimitiveType.TriangleList,
                            0,
                            0,
                            indices.Count / 3
                        );
                    }
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
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
        }
    }
}