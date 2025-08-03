using System;
using Microsoft.Xna.Framework;

namespace HajimiManbo.World
{
    /// <summary>
    /// 世界数据类，存储整个地图的方块信息
    /// </summary>
    public class World
    {
        /// <summary>
        /// 世界宽度（方块数）
        /// </summary>
        public int Width { get; private set; }
        
        /// <summary>
        /// 世界高度（方块数）
        /// </summary>
        public int Height { get; private set; }
        
        /// <summary>
        /// 方块数据数组 [x, y]
        /// </summary>
        public Tile[,] Tiles { get; private set; }
        
        /// <summary>
        /// 世界种子
        /// </summary>
        public int Seed { get; private set; }
        
        /// <summary>
        /// 地表高度数组
        /// </summary>
        public int[] SurfaceHeight { get; private set; }
        
        /// <summary>
        /// 世界生成设置
        /// </summary>
        public WorldSettings Settings { get; private set; }
        
        public World(int width, int height, int seed, WorldSettings settings)
        {
            Width = width;
            Height = height;
            Seed = seed;
            Settings = settings;
            Tiles = new Tile[width, height];
            SurfaceHeight = new int[width];
            
            // 初始化为空气
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tiles[x, y] = Tile.Air;
                }
            }
        }
        
        /// <summary>
        /// 获取指定位置的方块
        /// </summary>
        public Tile GetTile(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return Tile.Air;
            return Tiles[x, y];
        }
        
        /// <summary>
        /// 设置指定位置的方块
        /// </summary>
        public void SetTile(int x, int y, Tile tile)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                Tiles[x, y] = tile;
            }
        }
        
        /// <summary>
        /// 检查坐标是否在世界范围内
        /// </summary>
        public bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }
        
        /// <summary>
        /// 获取世界边界矩形
        /// </summary>
        public Rectangle GetBounds()
        {
            return new Rectangle(0, 0, Width, Height);
        }
    }
    
    /// <summary>
    /// 世界生成设置
    /// </summary>
    public class WorldSettings
    {
        public int MapSize { get; set; } = 1; // 0=小, 1=中, 2=大
        public int MonsterDifficulty { get; set; } = 1; // 0=简单, 1=普通, 2=困难
        public int MonsterCount { get; set; } = 1; // 0=少, 1=中等, 2=多
        
        /// <summary>
        /// 根据地图大小获取世界尺寸
        /// </summary>
        public (int width, int height) GetWorldSize()
        {
            return MapSize switch
            {
                0 => (800, 400),   // 小地图
                1 => (1200, 600),  // 中地图
                2 => (1600, 800),  // 大地图
                _ => (1200, 600)
            };
        }
        
        /// <summary>
        /// 获取地表基准高度
        /// </summary>
        public int GetSurfaceLevel()
        {
            var (_, height) = GetWorldSize();
            return height / 3; // 地表在世界高度的1/3处
        }
    }
}