using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace HajimiManbo.World
{
    /// <summary>
    /// 世界生成器 - 实现Terraria式的分阶段地图生成
    /// </summary>
    public class WorldGenerator
    {
        private World world;
        private WorldSettings settings;
        private int seed;
        
        /// <summary>
        /// 生成进度事件
        /// </summary>
        public event Action<string, float> OnProgressUpdate;
        
        public WorldGenerator()
        {
        }
        
        /// <summary>
        /// 生成世界
        /// </summary>
        public World Generate(int seed, WorldSettings settings)
        {
            this.seed = seed;
            this.settings = settings;
            
            // 初始化噪声生成器
            NoiseGenerator.Initialize(seed);
            
            // 获取世界尺寸
            var (width, height) = settings.GetWorldSize();
            world = new World(width, height, seed, settings);
            
            // 执行生成阶段
            ExecuteGenerationPasses();
            
            return world;
        }
        
        /// <summary>
        /// 执行所有生成阶段
        /// </summary>
        private void ExecuteGenerationPasses()
        {
            var passes = new List<(string name, Action action)>
            {
                ("重置世界", ResetPass),
                ("生成地形", TerrainPass),
                ("表面装饰", SurfaceDecorPass),
                ("生成洞穴", CavesPass),
                ("生成生物群系", BiomesPass),
                ("生成矿物", OresPass),
                ("生成液体", LiquidsPass),
                ("最终处理", FinalizePass)
            };
            
            for (int i = 0; i < passes.Count; i++)
            {
                var (name, action) = passes[i];
                float progress = (float)i / passes.Count;
                OnProgressUpdate?.Invoke(name, progress);
                
                action.Invoke();
            }
            
            OnProgressUpdate?.Invoke("生成完成", 1.0f);
        }
        
        /// <summary>
        /// 阶段0：重置世界
        /// </summary>
        private void ResetPass()
        {
            // 已在World构造函数中完成，这里可以做额外的初始化
            for (int x = 0; x < world.Width; x++)
            {
                world.SurfaceHeight[x] = settings.GetSurfaceLevel();
            }
        }
        
        /// <summary>
        /// 阶段1：生成地形（新的三分区域设计）
        /// </summary>
        private void TerrainPass()
        {
            int surfaceLevel = settings.GetSurfaceLevel();
            float heightVariation = 30f; // 减少高度变化，地图更平坦
            
            // 生成地表高度图
            for (int x = 0; x < world.Width; x++)
            {
                float noise = NoiseGenerator.Noise1D(x * 0.008f, 1f, 3, 0.4f);
                int height = (int)(surfaceLevel + noise * heightVariation);
                height = Math.Max(5, Math.Min(world.Height - 20, height));
                world.SurfaceHeight[x] = height;
                
                // 垂直分层填充（根据生物群系调整）
                for (int y = height; y < world.Height; y++)
                {
                    // 计算相对深度
                    float depthRatio = (float)(y - height) / (world.Height - height);
                    
                    // 添加倾斜和断层效果
                    float layerNoise = NoiseGenerator.Noise2D(x * 0.005f, y * 0.003f, 1f, 2, 0.3f);
                    float adjustedDepth = depthRatio + layerNoise * 0.2f;
                    
                    // 根据生物群系决定图块类型
                    if (adjustedDepth < 0.75f) // 生物群系图块延伸到岩石层的一半深度
                    {
                        // 使用噪声来决定生物群系，创建随机的横向边界
                        float biomeNoise = NoiseGenerator.Noise2D(x * 0.01f, y * 0.005f, 1f, 3, 0.5f);
                        float normalizedX = (float)x / world.Width; // 归一化X坐标 (0-1)
                        
                        // 基础生物群系倾向 + 噪声扰动
                        float biomeValue = normalizedX + biomeNoise * 0.3f;
                        
                        if (biomeValue < 0.33f) // 左侧区域：土地（使用泥土）
                        {
                            world.SetTile(x, y, Tile.Dirt);
                        }
                        else if (biomeValue < 0.67f) // 中间区域：沙漠
                        {
                            world.SetTile(x, y, Tile.Sand);
                        }
                        else // 右侧区域：雪原
                        {
                            world.SetTile(x, y, Tile.Snow);
                        }
                    }
                    else // 深层岩石
                    {
                        world.SetTile(x, y, Tile.Stone);
                    }
                }
            }
        }
        
        /// <summary>
        /// 阶段2：表面装饰（三个生物群系）
        /// </summary>
        private void SurfaceDecorPass()
        {
            for (int x = 0; x < world.Width; x++)
            {
                int surfaceY = world.SurfaceHeight[x];
                
                // 使用与TerrainPass相同的噪声逻辑来决定地表生物群系
                float biomeNoise = NoiseGenerator.Noise2D(x * 0.01f, surfaceY * 0.005f, 1f, 3, 0.5f);
                float normalizedX = (float)x / world.Width; // 归一化X坐标 (0-1)
                
                // 基础生物群系倾向 + 噪声扰动
                float biomeValue = normalizedX + biomeNoise * 0.3f;
                
                if (biomeValue < 0.33f && world.GetTile(x, surfaceY).Type == TileType.Dirt) // 左侧区域：土地（草地）
                {
                    world.SetTile(x, surfaceY, Tile.Grass);
                }
                else if (biomeValue < 0.67f && world.GetTile(x, surfaceY).Type == TileType.Sand) // 中间：沙漠
                {
                    world.SetTile(x, surfaceY, Tile.Sand);
                    // 将表面下几层也改为沙子
                    for (int depth = 1; depth <= 3; depth++)
                    {
                        if (surfaceY + depth < world.Height && 
                            world.GetTile(x, surfaceY + depth).Type == TileType.Sand)
                        {
                            world.SetTile(x, surfaceY + depth, Tile.Sand);
                        }
                    }
                }
                else if (biomeValue >= 0.67f && world.GetTile(x, surfaceY).Type == TileType.Snow) // 右侧区域：雪原
                {
                    world.SetTile(x, surfaceY, Tile.Snow);
                    // 将表面下几层也改为雪
                    for (int depth = 1; depth <= 2; depth++)
                    {
                        if (surfaceY + depth < world.Height && 
                            world.GetTile(x, surfaceY + depth).Type == TileType.Snow)
                        {
                            world.SetTile(x, surfaceY + depth, Tile.Snow);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 阶段3：生成洞穴（确保地表有入口）
        /// </summary>
        private void CavesPass()
        {
            Random random = new Random(seed);
            
            // 生成多个洞穴系统，每个都有地表入口
            int caveCount = world.Width / 100; // 根据世界宽度决定洞穴数量
            
            for (int caveIndex = 0; caveIndex < caveCount; caveIndex++)
            {
                // 随机选择地表入口位置
                int entranceX = random.Next(50, world.Width - 50);
                int entranceY = world.SurfaceHeight[entranceX];
                
                // 生成从地表开始的洞穴系统
                GenerateCaveSystem(entranceX, entranceY, random);
            }
            
            // 额外的小型洞穴使用噪声生成
            for (int x = 0; x < world.Width; x++)
            {
                for (int y = world.SurfaceHeight[x] + 5; y < world.Height - 5; y++)
                {
                    float caveNoise = NoiseGenerator.Noise2D(x * 0.015f, y * 0.015f, 1f, 3, 0.5f);
                    
                    // 洞穴阈值，越深洞穴越多
                    float depth = (float)(y - world.SurfaceHeight[x]) / (world.Height - world.SurfaceHeight[x]);
                    float threshold = 0.4f + depth * 0.1f;
                    
                    if (caveNoise > threshold)
                    {
                        world.SetTile(x, y, Tile.Air);
                    }
                }
            }
            
            // 生成一些随机的大洞穴
            GenerateRandomCaves();
        }
        
        /// <summary>
        /// 生成从地表开始的洞穴系统
        /// </summary>
        private void GenerateCaveSystem(int entranceX, int entranceY, Random random)
        {
            // 从地表向下挖掘主通道
            int currentX = entranceX;
            int currentY = entranceY;
            
            // 挖掘垂直入口
            for (int depth = 0; depth < 10; depth++)
            {
                if (currentY + depth < world.Height)
                {
                    world.SetTile(currentX, currentY + depth, Tile.Air);
                    // 稍微扩大入口
                    if (currentX > 0) world.SetTile(currentX - 1, currentY + depth, Tile.Air);
                    if (currentX < world.Width - 1) world.SetTile(currentX + 1, currentY + depth, Tile.Air);
                }
            }
            
            // 生成分支洞穴
            int branchCount = random.Next(2, 5);
            for (int branch = 0; branch < branchCount; branch++)
            {
                int branchStartY = currentY + random.Next(10, 30);
                int branchLength = random.Next(20, 60);
                float branchDirection = (float)(random.NextDouble() * Math.PI * 2);
                
                GenerateWormCave(currentX, branchStartY, branchLength, branchDirection);
            }
        }
        
        /// <summary>
        /// 生成随机大洞穴
        /// </summary>
        private void GenerateRandomCaves()
        {
            int caveCount = world.Width / 200; // 根据世界大小决定洞穴数量
            
            for (int i = 0; i < caveCount; i++)
            {
                int startX = NoiseGenerator.NextInt(50, world.Width - 50);
                int startY = NoiseGenerator.NextInt(world.SurfaceHeight[startX] + 20, world.Height - 50);
                
                GenerateWormCave(startX, startY, NoiseGenerator.NextInt(100, 300), NoiseGenerator.NextFloat(0, (float)(Math.PI * 2)));
            }
        }
        
        /// <summary>
        /// 生成蠕虫式洞穴
        /// </summary>
        private void GenerateWormCave(int startX, int startY, int length, float initialDirection)
        {
            float x = startX;
            float y = startY;
            float direction = initialDirection;
            
            for (int i = 0; i < length; i++)
            {
                // 挖掘圆形区域
                int radius = NoiseGenerator.NextInt(3, 8);
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (dx * dx + dy * dy <= radius * radius)
                        {
                            int tileX = (int)x + dx;
                            int tileY = (int)y + dy;
                            if (world.IsValidCoordinate(tileX, tileY))
                            {
                                world.SetTile(tileX, tileY, Tile.Air);
                            }
                        }
                    }
                }
                
                // 更新方向和位置
                direction += NoiseGenerator.NextFloat(-0.3f, 0.3f);
                x += (float)Math.Cos(direction) * 2f;
                y += (float)Math.Sin(direction) * 2f;
                
                // 边界检查
                if (x < 10 || x >= world.Width - 10 || y < 10 || y >= world.Height - 10)
                    break;
            }
        }
        
        /// <summary>
        /// 阶段4：生成生物群系
        /// </summary>
        private void BiomesPass()
        {
            // 简单的生物群系生成：根据X坐标划分区域
            int sectionWidth = world.Width / 3;
            
            for (int x = 0; x < world.Width; x++)
            {
                int section = x / sectionWidth;
                TileType biomeType = section switch
                {
                    0 => TileType.Snow,      // 左侧雪地
                    1 => TileType.Grass,     // 中间普通
                    2 => TileType.JungleGrass, // 右侧丛林
                    _ => TileType.Grass
                };
                
                // 只替换表面的草地
                int surfaceY = world.SurfaceHeight[x];
                if (world.GetTile(x, surfaceY).Type == TileType.Grass && biomeType != TileType.Grass)
                {
                    world.SetTile(x, surfaceY, new Tile(biomeType));
                }
            }
        }
        
        /// <summary>
        /// 阶段5：生成矿物和大理石群
        /// </summary>
        private void OresPass()
        {
            // 生成大理石群（大型区域）
            GenerateMarbleDeposits();
            
            // 生成大量岩石群（替换部分石头）
            GenerateStoneDeposits();
            
            // 生成泥土群
            GenerateDirtDeposits();
            
            // 生成传统矿物（如果有的话）
            // GenerateOre(TileType.CopperOre, 0.02f, 3, 8);   // 铜矿
            // GenerateOre(TileType.IronOre, 0.015f, 4, 10);   // 铁矿
        }
        
        /// <summary>
        /// 生成大理石矿床
        /// </summary>
        private void GenerateMarbleDeposits()
        {
            Random random = new Random(seed + 1);
            int depositCount = world.Width / 150; // 根据世界宽度决定大理石群数量
            
            for (int i = 0; i < depositCount; i++)
            {
                int centerX = random.Next(50, world.Width - 50);
                int centerY = random.Next(world.SurfaceHeight[centerX] + 20, world.Height - 30);
                int radius = random.Next(15, 35);
                
                // 生成椭圆形大理石矿床
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    for (int y = centerY - radius / 2; y <= centerY + radius / 2; y++)
                    {
                        if (x >= 0 && x < world.Width && y >= 0 && y < world.Height)
                        {
                            float distance = (float)Math.Sqrt(
                                Math.Pow((x - centerX) / (float)radius, 2) + 
                                Math.Pow((y - centerY) / (float)(radius / 2), 2)
                            );
                            
                            if (distance <= 1.0f && world.GetTile(x, y).Type == TileType.Stone)
                            {
                                // 添加一些随机性，使边缘更自然
                                float noise = NoiseGenerator.Noise2D(x * 0.1f, y * 0.1f, 1f, 1, 1f);
                                if (distance <= 0.8f || (distance <= 1.0f && noise > 0.3f))
                                {
                                    world.SetTile(x, y, Tile.Marble);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 生成岩石矿床（增加岩石密度）
        /// </summary>
        private void GenerateStoneDeposits()
        {
            Random random = new Random(seed + 2);
            int depositCount = world.Width / 8; // 再增加一倍密度
            
            for (int i = 0; i < depositCount; i++)
            {
                int centerX = random.Next(20, world.Width - 20);
                int centerY = random.Next(world.SurfaceHeight[centerX] + 10, world.Height - 20);
                int radius = random.Next(4, 16); // 调整半径范围
                
                // 生成不规则岩石群
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    for (int y = centerY - radius; y <= centerY + radius; y++)
                    {
                        if (x >= 0 && x < world.Width && y >= 0 && y < world.Height)
                        {
                            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                            
                            TileType currentType = world.GetTile(x, y).Type;
                            if (distance <= radius && (currentType == TileType.Dirt || currentType == TileType.Sand || currentType == TileType.Snow))
                            {
                                // 添加噪声使形状更自然
                                float noise = NoiseGenerator.Noise2D(x * 0.1f, y * 0.1f, 1f, 2, 0.5f);
                                if (noise > 0.08f) // 再次降低阈值，增加一倍密度
                                {
                                    world.SetTile(x, y, Tile.Stone);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 生成泥土矿床（增加泥土密度）
        /// </summary>
        private void GenerateDirtDeposits()
        {
            Random random = new Random(seed + 3);
            int depositCount = world.Width / 8; // 再增加一倍密度
            
            for (int i = 0; i < depositCount; i++)
            {
                int centerX = random.Next(20, world.Width - 20);
                int centerY = random.Next(world.SurfaceHeight[centerX] + 5, world.Height - 20);
                int radius = random.Next(4, 16); // 调整半径范围
                
                // 生成不规则泥土群
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    for (int y = centerY - radius; y <= centerY + radius; y++)
                    {
                        if (x >= 0 && x < world.Width && y >= 0 && y < world.Height)
                        {
                            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                            
                            TileType currentType = world.GetTile(x, y).Type;
                            if (distance <= radius && (currentType == TileType.Stone || currentType == TileType.Sand || currentType == TileType.Snow))
                            {
                                // 添加噪声使形状更自然
                                float noise = NoiseGenerator.Noise2D(x * 0.08f, y * 0.08f, 1f, 2, 0.5f);
                                if (noise > 0.15f) // 再次降低阈值，增加一倍密度
                                {
                                    world.SetTile(x, y, Tile.Dirt);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 生成指定类型的矿物
        /// </summary>
        private void GenerateOre(TileType oreType, float frequency, int minDepth, int maxDepth)
        {
            for (int x = 0; x < world.Width; x++)
            {
                int startY = world.SurfaceHeight[x] + minDepth;
                int endY = Math.Min(world.Height - 5, world.SurfaceHeight[x] + maxDepth);
                
                for (int y = startY; y < endY; y++)
                {
                    if (world.GetTile(x, y).Type == TileType.Stone)
                    {
                        float oreNoise = NoiseGenerator.Noise2D(x * 0.05f, y * 0.05f, 1f, 2, 0.5f);
                        if (oreNoise > (1f - frequency))
                        {
                            world.SetTile(x, y, new Tile(oreType));
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 阶段6：生成液体
        /// </summary>
        private void LiquidsPass()
        {
            // 在低洼处生成水
            for (int x = 1; x < world.Width - 1; x++)
            {
                int surfaceY = world.SurfaceHeight[x];
                int leftSurface = world.SurfaceHeight[x - 1];
                int rightSurface = world.SurfaceHeight[x + 1];
                
                // 如果是低洼地形，生成水
                if (surfaceY > leftSurface + 3 && surfaceY > rightSurface + 3)
                {
                    for (int y = surfaceY; y < surfaceY + 5 && y < world.Height; y++)
                    {
                        if (world.GetTile(x, y).Type == TileType.Air)
                        {
                            world.SetTile(x, y, new Tile(TileType.Water));
                        }
                    }
                }
            }
            
            // 在深层生成熔岩
            int lavaLevel = world.Height - 50;
            for (int x = 0; x < world.Width; x++)
            {
                for (int y = lavaLevel; y < world.Height; y++)
                {
                    if (world.GetTile(x, y).Type == TileType.Air)
                    {
                        world.SetTile(x, y, new Tile(TileType.Lava));
                    }
                }
            }
        }
        
        /// <summary>
        /// 阶段7：最终处理
        /// </summary>
        private void FinalizePass()
        {
            // 确保表面的泥土变成草地
            for (int x = 0; x < world.Width; x++)
            {
                int surfaceY = world.SurfaceHeight[x];
                if (world.GetTile(x, surfaceY).Type == TileType.Dirt)
                {
                    // 检查上方是否有空气
                    if (surfaceY > 0 && world.GetTile(x, surfaceY - 1).Type == TileType.Air)
                    {
                        world.SetTile(x, surfaceY, Tile.Grass);
                    }
                }
            }
        }
    }
}