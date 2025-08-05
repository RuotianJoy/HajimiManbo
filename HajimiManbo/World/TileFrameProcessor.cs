using System;
using System.Collections.Generic;

namespace HajimiManbo.World
{
    /// <summary>
    /// 瓦片帧处理器 - 实现Marching Squares 47帧自动瓦片系统
    /// 基于Terraria 1.4的8方向邻域检测和47种帧变体
    /// </summary>
    public static class TileFrameProcessor
    {
        /// <summary>
        /// Terraria 1.4 Blob 47 frame lookup (mask → frame)
        /// Maps 8-bit neighbor mask to frame index (0-46)
        /// Neighbor bits: TL-T-TR-R-BR-B-BL-L (bit 7-0)
        /// </summary>
        private static readonly byte[] MS_TO_FRAME = new byte[256]
        {
            /*  0*/  0,  1,  2,  2,  3,  4,  2,  2,  5,  5,  6,  6,  7,  7,  6,  6,
            /* 16*/  8,  9, 10, 10,  8,  9, 10, 10, 11, 11, 12, 12, 11, 11, 12, 12,
            /* 32*/ 13, 14, 15, 15, 16, 17, 15, 15,  5,  5,  6,  6,  7,  7,  6,  6,
            /* 48*/ 18, 19, 20, 20, 18, 19, 20, 20, 11, 11, 12, 12, 11, 11, 12, 12,
            /* 64*/ 21, 22, 23, 23, 24, 25, 23, 23, 26, 26, 27, 27, 28, 28, 27, 27,
            /* 80*/ 29, 30, 31, 31, 29, 30, 31, 31, 32, 32, 33, 33, 32, 32, 33, 33,
            /* 96*/ 21, 22, 23, 23, 24, 25, 23, 23, 26, 26, 27, 27, 28, 28, 27, 27,
            /*112*/ 29, 30, 31, 31, 29, 30, 31, 31, 32, 32, 33, 33, 32, 32, 33, 33,
            /*128*/ 34, 35, 36, 36, 37, 38, 36, 36, 39, 39, 40, 40, 41, 41, 40, 40,
            /*144*/  8,  9, 10, 10,  8,  9, 10, 10, 11, 11, 12, 12, 11, 11, 12, 12,
            /*160*/ 42, 43, 44, 44, 45, 46, 44, 44, 39, 39, 40, 40, 41, 41, 40, 40,
            /*176*/ 18, 19, 20, 20, 18, 19, 20, 20, 11, 11, 12, 12, 11, 11, 12, 12,
            /*192*/ 21, 22, 23, 23, 24, 25, 23, 23, 26, 26, 27, 27, 28, 28, 27, 27,
            /*208*/ 29, 30, 31, 31, 29, 30, 31, 31, 32, 32, 33, 33, 32, 32, 33, 33,
            /*224*/ 21, 22, 23, 23, 24, 25, 23, 23, 26, 26, 27, 27, 28, 28, 27, 27,
            /*240*/ 29, 30, 31, 31, 29, 30, 31, 31, 32, 32, 33, 33, 32, 32, 33, 33
        };
        /// <summary>
        /// 检测是否为草皮帧变体
        /// </summary>
        /// <param name="frameVariant">帧变体值</param>
        /// <returns>是否为草皮帧变体</returns>
        private static bool IsGrassFrameVariant(byte frameVariant)
        {
            // 草皮帧变体范围：
            // 上暴露：1, 2, 3
            // 下暴露：33, 34, 35 (32+1, 32+2, 32+3)
            // 左暴露：16, 32, 48 (1*16, 2*16, 3*16)
            // 右暴露：19, 35, 51 (1*16+3, 2*16+3, 3*16+3)
            return (frameVariant >= 1 && frameVariant <= 3) ||     // 上暴露
                   (frameVariant >= 33 && frameVariant <= 35) ||   // 下暴露
                   (frameVariant == 16 || frameVariant == 32 || frameVariant == 48) || // 左暴露
                   (frameVariant == 19 || frameVariant == 35 || frameVariant == 51);   // 右暴露
        }
        
        /// <summary>
        /// 处理世界的帧变体 - 使用Marching Squares 47帧系统
        /// </summary>
        /// <param name="world">要处理的世界</param>
        public static void ProcessWorld(World world)
        {
            if (world == null) return;
            
            // 使用8方向邻域检测计算47帧变体
            BuildFrame47Pass(world);
            
            // 可选：曲线插值和重新坡化（后续实现）
            // SmoothCurvePass(world);
            
            // 可选：次级蚀刻和材质融合（后续实现）
            // MorphologyPass(world);
        }
        
        /// <summary>
        /// 构建47帧变体（基于8方向邻域的Marching Squares）
        /// </summary>
        private static void BuildFrame47Pass(World world)
        {
            for (int y = 0; y < world.Height; y++)
            {
                for (int x = 0; x < world.Width; x++)
                {
                    var tile = world.GetTile(x, y);
                    if (tile.Type == TileType.Air) continue;
                    
                    // 跳过已经设置了草皮的方块（现在是TileType.Grass类型）
                    if (tile.Type == TileType.Grass && IsGrassFrameVariant(tile.FrameVariant))
                    {
                        // 调试输出
                        if (x % 20 == 0 && y % 20 == 0)
                        {
                            Console.WriteLine($"[TileFrameProcessor] 跳过草皮方块 ({x},{y}) 帧变体: {tile.FrameVariant}");
                        }
                        continue;
                    }
                    
                    // 计算47帧索引
                    byte frameIndex = CalcFrame8(world, x, y, tile.Type);
                    
                    // 设置帧变体
                    tile.FrameVariant = frameIndex;
                    // 清除斜坡信息，现在由47帧图集处理
                    tile.Slope = 0;
                    world.SetTile(x, y, tile);
                    
                    // 调试输出（仅输出少量样本）
                    if (x % 50 == 0 && y % 50 == 0 && frameIndex > 0)
                    {
                        Console.WriteLine($"[TileFrameProcessor] Tile at ({x},{y}) type={tile.Type} frame={frameIndex}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 计算8方向邻域的47帧索引
        /// </summary>
        /// <param name="world">世界对象</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="tileType">当前瓦片类型</param>
        /// <returns>47帧索引 (0-46)</returns>
        public static byte CalcFrame8(World world, int x, int y, TileType tileType)
        {
            if (tileType == TileType.Air) return 0;

            // 检查是否需要特殊处理（石头、大理石、雪地、沙漠与泥土的连接）
            if (NeedsSpecialDirtConnection(tileType))
            {
                byte specialFrame = CalcSpecialDirtFrame(world, x, y, tileType);
                if (specialFrame != 255) // 255表示没有特殊处理
                    return specialFrame;
            }

            // 先算十字方向
            bool up    = IsSolid(world, x,     y - 1, tileType);
            bool down  = IsSolid(world, x,     y + 1, tileType);
            bool left  = IsSolid(world, x - 1, y,     tileType);
            bool right = IsSolid(world, x + 1, y,     tileType);

            // 获取对角邻居
            bool tl = IsSolid(world, x - 1, y - 1, tileType);
            bool tr = IsSolid(world, x + 1, y - 1, tileType);
            bool bl = IsSolid(world, x - 1, y + 1, tileType);
            bool br = IsSolid(world, x + 1, y + 1, tileType);

            int mask = 0;
            // Set bits according to TL-T-TR-R-BR-B-BL-L (bit 7-0)
            if (left) mask |= 1 << 0;               // L (bit 0)
            if (down && left && bl) mask |= 1 << 1; // BL (bit 1)
            if (down) mask |= 1 << 2;               // B (bit 2)
            if (down && right && br) mask |= 1 << 3; // BR (bit 3)
            if (right) mask |= 1 << 4;              // R (bit 4)
            if (up && right && tr) mask |= 1 << 5;  // TR (bit 5)
            if (up) mask |= 1 << 6;                 // T (bit 6)
            if (up && left && tl) mask |= 1 << 7;   // TL (bit 7)

            return MS_TO_FRAME[mask];
        }
        
        /// <summary>
        /// 检查是否需要特殊的泥土连接处理
        /// </summary>
        private static bool NeedsSpecialDirtConnection(TileType tileType)
        {
            return tileType == TileType.Stone || tileType == TileType.Marble || 
                   tileType == TileType.Snow || tileType == TileType.Sand;
        }
        
        /// <summary>
        /// 计算与泥土连接的特殊帧变体
        /// </summary>
        private static byte CalcSpecialDirtFrame(World world, int x, int y, TileType tileType)
        {
            // 检查四个方向是否有泥土
            bool upDirt = IsDirt(world, x, y - 1);
            bool downDirt = IsDirt(world, x, y + 1);
            bool leftDirt = IsDirt(world, x - 1, y);
            bool rightDirt = IsDirt(world, x + 1, y);
            
            // 检查对角方向是否有泥土
            bool tlDirt = IsDirt(world, x - 1, y - 1);
            bool trDirt = IsDirt(world, x + 1, y - 1);
            bool blDirt = IsDirt(world, x - 1, y + 1);
            bool brDirt = IsDirt(world, x + 1, y + 1);
            
            // 检查四个方向是否暴露（没有相同类型的方块）
            bool upExposed = !IsSolid(world, x, y - 1, tileType);
            bool downExposed = !IsSolid(world, x, y + 1, tileType);
            bool leftExposed = !IsSolid(world, x - 1, y, tileType);
            bool rightExposed = !IsSolid(world, x + 1, y, tileType);
            
            Random random = new Random(x * 1000 + y); // 基于位置的随机种子
            
            // 规则1: 只暴露上面，下方是泥土
            if (upExposed && !downExposed && !leftExposed && !rightExposed && downDirt)
            {
                int[] frames = {13, 14, 15};
                return (byte)(frames[random.Next(3)] + 0 * 16); // (13,0), (14,0), (15,0)
            }
            
            // 规则2: 只暴露下面，上方是泥土
            if (!upExposed && downExposed && !leftExposed && !rightExposed && upDirt)
            {
                int[] frames = {13, 14, 15};
                return (byte)(frames[random.Next(3)] + 1 * 16); // (13,1), (14,1), (15,1)
            }
            
            // 规则3: 只暴露左边，右边是泥土
            if (!upExposed && !downExposed && leftExposed && !rightExposed && rightDirt)
            {
                int[] frames = {13, 14, 15};
                return (byte)(frames[random.Next(3)] + 2 * 16); // (13,2), (14,2), (15,2)
            }
            
            // 规则4: 只暴露右边，左边是泥土
            if (!upExposed && !downExposed && !leftExposed && rightExposed && leftDirt)
            {
                int[] frames = {13, 14, 15};
                return (byte)(frames[random.Next(3)] + 3 * 16); // (13,3), (14,3), (15,3)
            }
            
            // 新规则1: 只暴露左边，下面是泥土
            if (!upExposed && !downExposed && leftExposed && !rightExposed && downDirt)
            {
                int[] frames = {6, 7, 8};
                return (byte)(4 + frames[random.Next(3)] * 16); // (4,6), (4,7), (4,8)
            }
            
            // 新规则2: 只暴露左边，上面是泥土
            if (!upExposed && !downExposed && leftExposed && !rightExposed && upDirt)
            {
                int[] frames = {9, 10, 11};
                return (byte)(4 + frames[random.Next(3)] * 16); // (4,9), (4,10), (4,11)
            }
            
            // 新规则3: 只暴露右边，下面是泥土
            if (!upExposed && !downExposed && !leftExposed && rightExposed && downDirt)
            {
                int[] frames = {6, 7, 8};
                return (byte)(5 + frames[random.Next(3)] * 16); // (5,6), (5,7), (5,8)
            }
            
            // 新规则4: 只暴露右边，上面是泥土
            if (!upExposed && !downExposed && !leftExposed && rightExposed && upDirt)
            {
                int[] frames = {9, 10, 11};
                return (byte)(5 + frames[random.Next(3)] * 16); // (5,9), (5,10), (5,11)
            }
            
            // 新规则5: 上左右暴露，下方是泥土
            if (upExposed && !downExposed && leftExposed && rightExposed && downDirt)
            {
                int[] frames = {6, 7, 8};
                return (byte)(6 + frames[random.Next(3)] * 16); // (6,6), (6,7), (6,8)
            }
            
            // 新规则6: 下左右暴露，上方是泥土
            if (!upExposed && downExposed && leftExposed && rightExposed && upDirt)
            {
                int[] frames = {9, 10, 11};
                return (byte)(6 + frames[random.Next(3)] * 16); // (6,9), (6,10), (6,11)
            }
            
            // 新规则7: 上右下暴露，左边是泥土
            if (upExposed && downExposed && !leftExposed && rightExposed && leftDirt)
            {
                int[] frames = {1, 2, 3};
                return (byte)(frames[random.Next(3)] + 13 * 16); // (1,13), (2,13), (3,13)
            }
            
            // 新规则8: 上左下暴露，右边是泥土
            if (upExposed && downExposed && leftExposed && !rightExposed && rightDirt)
            {
                int[] frames = {4, 5, 6};
                return (byte)(frames[random.Next(3)] + 13 * 16); // (4,13), (5,13), (6,13)
            }
            
            // 新规则9: 左右暴露，上是相同块，下是泥土
            if (!upExposed && !downExposed && leftExposed && rightExposed && 
                IsSolid(world, x, y - 1, tileType) && downDirt)
            {
                int[] frames = {6, 7, 8};
                return (byte)(7 + frames[random.Next(3)] * 16); // (7,6), (7,7), (7,8)
            }
            
            // 新规则10: 左右暴露，下是相同块，上是泥土
            if (!upExposed && !downExposed && leftExposed && rightExposed && 
                IsSolid(world, x, y + 1, tileType) && upDirt)
            {
                int[] frames = {9, 10, 11};
                return (byte)(7 + frames[random.Next(3)] * 16); // (7,9), (7,10), (7,11)
            }
            
            // 新规则11: 上下暴露，左是泥土，右是相同块
            if (upExposed && downExposed && !leftExposed && !rightExposed && 
                leftDirt && IsSolid(world, x + 1, y, tileType))
            {
                int[] frames = {1, 2, 3};
                return (byte)(frames[random.Next(3)] + 14 * 16); // (1,14), (2,14), (3,14)
            }
            
            // 新规则12: 上下暴露，右是泥土，左是相同块
            if (upExposed && downExposed && !leftExposed && !rightExposed && 
                rightDirt && IsSolid(world, x - 1, y, tileType))
            {
                int[] frames = {4, 5, 6};
                return (byte)(frames[random.Next(3)] + 14 * 16); // (4,14), (5,14), (6,14)
            }
            
            // 新规则13: 左右暴露，上下都是泥土
            if (!upExposed && !downExposed && leftExposed && rightExposed && 
                upDirt && downDirt)
            {
                int[] frames = {13, 14, 15};
                return (byte)(6 + frames[random.Next(3)] * 16); // (6,13), (6,14), (6,15)
            }
            
            // 新规则14: 上下暴露，左右都是泥土
            if (upExposed && downExposed && !leftExposed && !rightExposed && 
                leftDirt && rightDirt)
            {
                int[] frames = {10, 11, 12};
                return (byte)(frames[random.Next(3)] + 11 * 16); // (10,11), (11,11), (12,11)
            }
            
            // 规则5: 完全不暴露的情况
            if (!upExposed && !downExposed && !leftExposed && !rightExposed)
            {
                // 优先检查四周全是泥土的情况
                if (upDirt && downDirt && leftDirt && rightDirt)
                {
                    int[] frames = {7, 8, 9};
                    return (byte)(frames[random.Next(3)] + 11 * 16); // (7,11), (8,11), (9,11)
                }
                // 只有下面是泥土
                else if (!upDirt && downDirt && !leftDirt && !rightDirt)
                {
                    int[] frames = {9, 10, 11};
                    return (byte)(frames[random.Next(3)] + 5 * 16); // (9,5), (10,5), (11,5)
                }
                // 只有上面是泥土
                else if (upDirt && !downDirt && !leftDirt && !rightDirt)
                {
                    int[] frames = {9, 10, 11};
                    return (byte)(frames[random.Next(3)] + 6 * 16); // (9,6), (10,6), (11,6)
                }
                // 只有左边是泥土
                else if (!upDirt && !downDirt && leftDirt && !rightDirt)
                {
                    int[] frames = {7, 8, 9};
                    return (byte)(9 + frames[random.Next(3)] * 16); // (9,7), (9,8), (9,9)
                }
                // 只有右边是泥土
                else if (!upDirt && !downDirt && !leftDirt && rightDirt)
                {
                    int[] frames = {7, 8, 9};
                    return (byte)(8 + frames[random.Next(3)] * 16); // (8,7), (8,8), (8,9)
                }
                // 上下都是泥土
                else if (upDirt && downDirt && !leftDirt && !rightDirt)
                {
                    int[] frames = {9, 10, 11};
                    return (byte)(frames[random.Next(3)] + 10 * 16); // (9,10), (10,10), (11,10)
                }
                // 左右都是泥土
                else if (!upDirt && !downDirt && leftDirt && rightDirt)
                {
                    int[] frames = {7, 8, 9};
                    return (byte)(10 + frames[random.Next(3)] * 16); // (10,7), (10,8), (10,9)
                }
                // 只有下方是相同块，其他全是泥土
                else if (upDirt && IsSolid(world, x, y + 1, tileType) && leftDirt && rightDirt)
                {
                    int[] frames = {6, 7, 8};
                    return (byte)(11 + frames[random.Next(3)] * 16); // (11,6), (11,7), (11,8)
                }
                // 只有右边是相同块，其他全是泥土
                else if (upDirt && downDirt && leftDirt && IsSolid(world, x + 1, y, tileType))
                {
                    int[] frames = {6, 7, 8};
                    return (byte)(12 + frames[random.Next(3)] * 16); // (12,6), (12,7), (12,8)
                }
                // 只有上方是相同块，其他全是泥土
                else if (IsSolid(world, x, y - 1, tileType) && downDirt && leftDirt && rightDirt)
                {
                    int[] frames = {9, 10, 11};
                    return (byte)(11 + frames[random.Next(3)] * 16); // (11,9), (11,10), (11,11)
                }
                // 只有左边是相同块，其他全是泥土
                else if (upDirt && downDirt && IsSolid(world, x - 1, y, tileType) && rightDirt)
                {
                    int[] frames = {9, 10, 11};
                    return (byte)(12 + frames[random.Next(3)] * 16); // (12,9), (12,10), (12,11)
                }
                
                // 检查各个角落的泥土情况
                if (brDirt) // 右下角是泥土
                {
                    int[] frames = {5, 7, 9};
                    return (byte)(0 + frames[random.Next(3)] * 16); // (0,5), (0,7), (0,9)
                }
                else if (blDirt) // 左下角是泥土
                {
                    int[] frames = {5, 7, 9};
                    return (byte)(1 + frames[random.Next(3)] * 16); // (1,5), (1,7), (1,9)
                }
                else if (tlDirt) // 左上角是泥土
                {
                    int[] frames = {6, 8, 10};
                    return (byte)(1 + frames[random.Next(3)] * 16); // (1,6), (1,8), (1,10)
                }
                else if (trDirt) // 右上角是泥土
                {
                    int[] frames = {6, 8, 10};
                    return (byte)(0 + frames[random.Next(3)] * 16); // (0,6), (0,8), (0,10)
                }
                else if (leftDirt && upDirt) // 左和上都是泥土
                {
                    int[] frames = {5, 7, 9};
                    return (byte)(2 + frames[random.Next(3)] * 16); // (2,5), (2,7), (2,9)
                }
                else if (rightDirt && upDirt) // 右和上是泥土
                {
                    int[] frames = {5, 7, 9};
                    return (byte)(3 + frames[random.Next(3)] * 16); // (3,5), (3,7), (3,9)
                }
                else if (leftDirt && downDirt) // 左和下是泥土
                {
                    int[] frames = {6, 8, 10};
                    return (byte)(2 + frames[random.Next(3)] * 16); // (2,6), (2,8), (2,10)
                }
                else if (rightDirt && downDirt) // 右和下是泥土
                {
                    int[] frames = {6, 8, 10};
                    return (byte)(3 + frames[random.Next(3)] * 16); // (3,6), (3,8), (3,10)
                }
            }
            
            return 255; // 没有特殊处理，使用默认逻辑
        }
        
        /// <summary>
        /// 检查指定位置是否为泥土
        /// </summary>
        private static bool IsDirt(World world, int x, int y)
        {
            if (!world.IsValidCoordinate(x, y))
                return false;
                
            var tile = world.GetTile(x, y);
            return tile.Type == TileType.Dirt;
        }

        
        /// <summary>
        /// 检查指定位置是否为实心块（用于8方向邻域检测）
        /// </summary>
        private static bool IsSolid(World world, int x, int y, TileType currentType)
        {
            if (!world.IsValidCoordinate(x, y))
                return false;
                
            var tile = world.GetTile(x, y);
            
            // 相同类型直接返回true
            if (tile.Type == currentType)
                return true;
                
            // 特殊连接规则
            return CanTilesConnect(currentType, tile.Type);
        }
        
        /// <summary>
        /// 平滑斜坡处理 - 递归消除所有台阶感
        /// </summary>
        private static void SmoothSlopePass(World world)
        {
            bool changed;
            int pass = 0;
            do
            {
                changed = false;
                pass++;

                // ⬇ 从上往下扫：让"楼梯顶"变斜坡 / 半砖
                for (int y = 1; y < world.Height - 1; y++)
                {
                    for (int x = 1; x < world.Width - 1; x++)
                    {
                        var cur = world.GetTile(x, y);
                        if (!cur.IsSolid) continue;

                        var up = world.GetTile(x, y - 1);
                        var down = world.GetTile(x, y + 1);
                        var left = world.GetTile(x - 1, y);
                        var right = world.GetTile(x + 1, y);
                        var leftUp = world.GetTile(x - 1, y - 1);
                        var rightUp = world.GetTile(x + 1, y - 1);

                        // A. ↘  （左高右低）
                        if (!up.IsSolid && left.IsSolid && leftUp.IsSolid)
                        {
                            if (cur.Slope != 1) { cur.Slope = 1; changed = true; }
                        }
                        // B. ↙  （右高左低）
                        else if (!up.IsSolid && right.IsSolid && rightUp.IsSolid)
                        {
                            if (cur.Slope != 4) { cur.Slope = 4; changed = true; }
                        }
                        // C. 半砖：把斜坡底座削平
                        else if (!up.IsSolid && down.IsSolid && cur.Slope == 0)
                        {
                            // 只有一边靠墙才变半砖，防止整块地下被切掉
                            bool loneStep = (left.IsSolid ^ right.IsSolid) &&
                                           (!leftUp.IsSolid && !rightUp.IsSolid);
                            if (loneStep) { cur.Slope = 5; changed = true; }
                        }

                        if (changed) world.SetTile(x, y, cur);
                    }
                }
                // 跑 3-4 轮就够了；差不多 45° 的连坡会全部成型
            } while (changed && pass < 6);
        }
        
        /// <summary>
        /// 检查指定位置是否有相同类型的方块（保留兼容性）
        /// </summary>
        private static bool HasSameTileType(World world, int x, int y, TileType tileType)
        {
            return IsSolid(world, x, y, tileType);
        }
        
        /// <summary>
        /// 判断两种瓦片类型是否可以连接
        /// </summary>
        private static bool CanTilesConnect(TileType type1, TileType type2)
        {
            // 草地和泥土可以连接
            if ((type1 == TileType.Grass && type2 == TileType.Dirt) ||
                (type1 == TileType.Dirt && type2 == TileType.Grass))
                return true;
                
            // 丛林草和泥土可以连接
            if ((type1 == TileType.JungleGrass && type2 == TileType.Dirt) ||
                (type1 == TileType.Dirt && type2 == TileType.JungleGrass))
                return true;
                
            // 草地和丛林草可以连接
            if ((type1 == TileType.Grass && type2 == TileType.JungleGrass) ||
                (type1 == TileType.JungleGrass && type2 == TileType.Grass))
                return true;
                
            return false;
        }
        
        /// <summary>
        /// 获取47帧变体对应的纹理源矩形
        /// </summary>
        /// <param name="frameVariant">帧变体 (0-46)</param>
        /// <param name="tileSize">单个瓦片尺寸 (默认16)</param>
        /// <param name="tilesPerRow">每行瓦片数 (默认16，对应256像素宽)</param>
        /// <returns>源矩形</returns>
        public static Microsoft.Xna.Framework.Rectangle GetFrameSourceRect(byte frameVariant, int tileSize = 16, int tilesPerRow = 16)
        {
            // 47帧图集布局：16x3 (最后一行只有15帧)
            // 确保frameVariant在有效范围内
            frameVariant = (byte)Math.Min((int)frameVariant, 46);
            
            int frameX = (frameVariant % tilesPerRow) * tileSize;
            int frameY = (frameVariant / tilesPerRow) * tileSize;
            
            return new Microsoft.Xna.Framework.Rectangle(frameX, frameY, tileSize, tileSize);
        }
        
        /// <summary>
        /// 获取47帧类型的描述
        /// </summary>
        /// <param name="frameIndex">帧索引 (0-46)</param>
        /// <returns>描述字符串</returns>
        public static string GetFrameDescription(byte frameIndex)
        {
            return frameIndex switch
            {
                0 => "实心块",
                >= 1 and <= 4 => "外角",
                >= 5 and <= 8 => "边缘",
                >= 9 and <= 12 => "内角",
                >= 13 and <= 16 => "对角线",
                >= 17 and <= 32 => "T型连接",
                >= 33 and <= 45 => "复杂连接",
                46 => "孤立块",
                _ => "未知帧"
            };
        }
        
        /// <summary>
        /// 获取斜坡类型的描述（保留兼容性）
        /// </summary>
        /// <param name="slope">斜坡类型</param>
        /// <returns>描述字符串</returns>
        public static string GetSlopeDescription(byte slope)
        {
            return slope switch
            {
                0 => "平地 (现由47帧处理)",
                1 => "右下斜坡 (↘) - 已弃用",
                2 => "右上斜坡 (↗) - 已弃用",
                3 => "左上斜坡 (↖) - 已弃用",
                4 => "左下斜坡 (↙) - 已弃用",
                5 => "半砖 - 已弃用",
                _ => "未知"
            };
        }
    }
}