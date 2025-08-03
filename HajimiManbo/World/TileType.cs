using System;

namespace HajimiManbo.World
{
    /// <summary>
    /// 方块类型枚举
    /// </summary>
    public enum TileType : byte
    {
        Air = 0,           // 空气
        Dirt = 1,          // 泥土
        Grass = 2,         // 草地
        Stone = 3,         // 石头
        Sand = 4,          // 沙子
        Snow = 5,          // 雪
        JungleGrass = 6,   // 丛林草
        Water = 7,         // 水
        Lava = 8,          // 熔岩
        CopperOre = 9,     // 铜矿
        IronOre = 10,      // 铁矿
        GoldOre = 11,      // 金矿
        SilverOre = 12,    // 银矿
        Coal = 13,         // 煤炭
        Diamond = 14,      // 钻石
        Marble = 15        // 大理石
    }
}