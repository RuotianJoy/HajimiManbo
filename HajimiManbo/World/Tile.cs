using System;

namespace HajimiManbo.World
{
    /// <summary>
    /// 单个方块的数据结构
    /// </summary>
    public struct Tile
    {
        /// <summary>
        /// 方块类型
        /// </summary>
        public TileType Type { get; set; }
        
        /// <summary>
        /// 背景墙类型
        /// </summary>
        public byte Wall { get; set; }
        
        /// <summary>
        /// 液体类型和数量（高4位表示类型，低4位表示数量）
        /// </summary>
        public byte Liquid { get; set; }
        
        /// <summary>
        /// 是否为固体方块
        /// </summary>
        public bool IsSolid => Type != TileType.Air && Type != TileType.Water && Type != TileType.Lava;
        
        /// <summary>
        /// 是否为液体
        /// </summary>
        public bool IsLiquid => Type == TileType.Water || Type == TileType.Lava;
        
        /// <summary>
        /// 是否为矿物
        /// </summary>
        public bool IsOre => Type >= TileType.CopperOre && Type <= TileType.Diamond;
        
        public Tile(TileType type, byte wall = 0, byte liquid = 0)
        {
            Type = type;
            Wall = wall;
            Liquid = liquid;
        }
        
        /// <summary>
        /// 创建空气方块
        /// </summary>
        public static Tile Air => new Tile(TileType.Air);
        
        /// <summary>
        /// 创建泥土方块
        /// </summary>
        public static Tile Dirt => new Tile(TileType.Dirt);
        
        /// <summary>
        /// 创建草地方块
        /// </summary>
        public static Tile Grass => new Tile(TileType.Grass);
        
        /// <summary>
        /// 创建石头方块
        /// </summary>
        public static Tile Stone => new Tile(TileType.Stone);
        
        /// <summary>
        /// 创建沙子方块
        /// </summary>
        public static Tile Sand => new Tile(TileType.Sand);
        
        /// <summary>
        /// 创建雪方块
        /// </summary>
        public static Tile Snow => new Tile(TileType.Snow);
        
        /// <summary>
        /// 创建丛林草方块
        /// </summary>
        public static Tile JungleGrass => new Tile(TileType.JungleGrass);
        
        /// <summary>
        /// 创建大理石方块
        /// </summary>
        public static Tile Marble => new Tile(TileType.Marble);
        
        /// <summary>
        /// 创建水方块
        /// </summary>
        public static Tile Water => new Tile(TileType.Water);
        
        /// <summary>
        /// 创建熔岩方块
        /// </summary>
        public static Tile Lava => new Tile(TileType.Lava);
    }
}