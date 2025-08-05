using System;
using System.Collections.Generic;
using System.Linq;
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
        private PortalManager portalManager;
        
        /// <summary>
        /// 生成进度事件
        /// </summary>
        public event Action<string, float> OnProgressUpdate;
        
        /// <summary>
        /// 世界生成完成事件
        /// </summary>
        public event Action<World> OnWorldGenerationComplete;
        
        public WorldGenerator()
        {
        }
        
        /// <summary>
        /// 在地下随机生成草皮块（类似石头生成逻辑）
        /// </summary>
        private void GenerateUndergroundGrass()
        {
            Random random = new Random(seed + 10);
            int depositCount = world.Width / 8; // 草皮群数量
            
            // 第四行10,11,12列的帧索引 (行3，列9,10,11)
            int[] row4Frames = { 3 * 16 + 9, 3 * 16 + 10, 3 * 16 + 11 };
            
            // 第12行7,8,9列的帧索引 (行11，列6,7,8)
            int[] row12Frames = { 11 * 16 + 6, 11 * 16 + 7, 11 * 16 + 8 };
            
            // 合并所有可用的帧索引
            int[] availableFrames = row4Frames.Concat(row12Frames).ToArray();
            
            Console.WriteLine($"[地下草皮生成] 开始生成 {depositCount} 个草皮群");
            
            for (int i = 0; i < depositCount; i++)
            {
                int centerX = random.Next(20, world.Width - 20);
                int centerY = random.Next(world.SurfaceHeight[centerX] + 5, world.Height - 20);
                int radius = random.Next(2, 8); // 较小的半径
                
                int grassCount = 0;
                
                // 生成不规则草皮群
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    for (int y = centerY - radius; y <= centerY + radius; y++)
                    {
                        if (x >= 0 && x < world.Width && y >= 0 && y < world.Height)
                        {
                            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                            
                            TileType currentType = world.GetTile(x, y).Type;
                            if (distance <= radius && currentType == TileType.Dirt)
                            {
                                // 添加噪声使形状更自然，并控制密度
                                float noise = NoiseGenerator.Noise2D(x * 0.15f, y * 0.15f, 1f, 2, 0.5f);
                                if (noise > 0.4f) // 较高的阈值，控制密度
                                {
                                    // 随机选择一个帧索引
                                    int frameIndex = availableFrames[random.Next(0, availableFrames.Length)];
                                    
                                    var currentTile = world.GetTile(x, y);
                                    world.SetTile(x, y, new Tile(TileType.Grass, 0, 0, (byte)frameIndex));
                                    
                                    grassCount++;
                                }
                            }
                        }
                    }
                }
                
                if (grassCount > 0)
                {
                    Console.WriteLine($"[地下草皮生成] 在 ({centerX},{centerY}) 生成了 {grassCount} 个草皮块");
                }
            }
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
            
            // 初始化传送门管理器
            portalManager = new PortalManager(world, seed);
            
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
                ("生成背景墙", BackgroundWallPass),
                ("表面装饰", SurfaceDecorPass),
                ("生成洞穴", CavesPass),
                ("生成沙漠山", DesertMountainPass),
                ("生成空岛", FloatingIslandsPass),
                ("生成生物群系", BiomesPass),
                ("生成矿物", OresPass),
                ("生成传送门", PortalsPass),
                ("生成液体", LiquidsPass),
                ("最终处理", FinalizePass)
            };
            
            for (int i = 0; i < passes.Count; i++)
            {
                var (name, action) = passes[i];
                float progress = (float)i / passes.Count;
                OnProgressUpdate?.Invoke(name, progress);
                
                action.Invoke();
                
                // 在每个生成步骤后检查并更新方块连接
                if (i > 0) // 跳过重置阶段
                {
                    OnProgressUpdate?.Invoke($"{name} - 更新方块连接", progress + 0.05f / passes.Count);
                    UpdateTileConnections();
                }
            }
            
            OnProgressUpdate?.Invoke("生成完成", 1.0f);
        }
        
        /// <summary>
        /// 更新所有方块的连接样式
        /// 在每个生成步骤后调用，确保方块连接状态正确
        /// 包括特殊连接规则（如石头、大理石、雪地、沙漠与泥土的连接）
        /// </summary>
        private void UpdateTileConnections()
        {
            // 遍历所有方块，重新计算连接帧
            for (int x = 0; x < world.Width; x++)
            {
                for (int y = 0; y < world.Height; y++)
                {
                    var tile = world.GetTile(x, y);
                    if (tile.Type == TileType.Air) continue;
                    
                    // 使用TileFrameProcessor重新计算连接帧（包括特殊连接规则）
                    byte newFrameIndex = TileFrameProcessor.CalcFrame8(world, x, y, tile.Type);
                    
                    // 只有当帧索引发生变化时才更新
                    if (tile.FrameVariant != newFrameIndex)
                    {
                        tile.FrameVariant = newFrameIndex;
                        world.SetTile(x, y, tile);
                        
                        // 标记相邻方块需要重新检查（因为连接状态可能影响邻居）
                        MarkNeighborsForUpdate(x, y);
                    }
                }
            }
        }
        
        /// <summary>
        /// 标记相邻方块需要重新检查连接状态
        /// </summary>
        private void MarkNeighborsForUpdate(int x, int y)
        {
            // 检查8个方向的邻居
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue; // 跳过自己
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (world.IsValidCoordinate(nx, ny))
                    {
                        var neighborTile = world.GetTile(nx, ny);
                        if (neighborTile.Type != TileType.Air)
                        {
                            // 重新计算邻居的连接帧
                            byte neighborFrameIndex = TileFrameProcessor.CalcFrame8(world, nx, ny, neighborTile.Type);
                            if (neighborTile.FrameVariant != neighborFrameIndex)
                            {
                                neighborTile.FrameVariant = neighborFrameIndex;
                                world.SetTile(nx, ny, neighborTile);
                            }
                        }
                    }
                }
            }
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
            }
            
            // 地表高度卷积平滑 - 减少锯齿山脊（三轮平滑）
            for (int round = 0; round < 3; round++)
            {
                // 创建临时数组存储本轮平滑结果
                int[] tempHeight = new int[world.Width];
                Array.Copy(world.SurfaceHeight, tempHeight, world.Width);
                
                for (int x = 1; x < world.Width - 1; x++)
                {
                    // 三点平均平滑
                    int smoothed = (world.SurfaceHeight[x - 1] + 
                                   world.SurfaceHeight[x] + 
                                   world.SurfaceHeight[x + 1]) / 3;
                    tempHeight[x] = smoothed;
                }
                
                // 应用本轮平滑结果到原数组
                for (int x = 1; x < world.Width - 1; x++)
                {
                    world.SurfaceHeight[x] = tempHeight[x];
                }
            }
            
            // 垂直分层填充（根据生物群系调整）
            for (int x = 0; x < world.Width; x++)
            {
                int height = world.SurfaceHeight[x];
                
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
                        float biomeNoise = NoiseGenerator.Noise2D(x * 0.008f, y * 0.003f, 1f, 4, 0.6f);
                        float normalizedX = (float)x / world.Width; // 归一化X坐标 (0-1)
                        
                        // 基础生物群系倾向 + 噪声扰动，创建更自然的边界
                        float biomeValue = normalizedX + biomeNoise * 0.25f;
                        
                        // 使用噪声调整的边界，而不是固定的三等分
                        float leftBoundary = 0.33f + NoiseGenerator.Noise2D(x * 0.005f, 0, 1f, 2, 0.3f) * 0.15f;
                        float rightBoundary = 0.67f + NoiseGenerator.Noise2D(x * 0.005f, 100, 1f, 2, 0.3f) * 0.15f;
                        
                        if (biomeValue < leftBoundary) // 左侧区域：泥土
                        {
                            byte style = (byte)NoiseGenerator.Random.Next(0, 15);   // dirt 有 15 种花纹
                            world.SetTile(x, y, new Tile(TileType.Dirt, style));
                        }
                        else if (biomeValue < rightBoundary) // 中间区域：沙漠
                        {
                            byte style = (byte)NoiseGenerator.Random.Next(0, 15);   // sand 有 15 种花纹
                            world.SetTile(x, y, new Tile(TileType.Sand, style));
                        }
                        else // 右侧区域：雪原
                        {
                            byte style = (byte)NoiseGenerator.Random.Next(0, 15);   // snow 有 15 种花纹
                            world.SetTile(x, y, new Tile(TileType.Snow, style));
                        }
                    }
                    else // 深层岩石
                    {
                        byte style = (byte)NoiseGenerator.Random.Next(0, 15);   // stone 有 15 种花纹
                        world.SetTile(x, y, new Tile(TileType.Stone, style));
                    }
                }
            }
        }
        
        /// <summary>
        /// 阶段2：表面装饰（使用噪声生成自然边界的三个生物群系）
        /// </summary>
        private void SurfaceDecorPass()
        {
            for (int x = 0; x < world.Width; x++)
            {
                int surfaceY = world.SurfaceHeight[x];
                
                // 使用与TerrainPass相同的噪声逻辑来决定地表生物群系
                float biomeNoise = NoiseGenerator.Noise2D(x * 0.01f, surfaceY * 0.005f, 1f, 3, 0.5f);
                float normalizedX = (float)x / world.Width; // 归一化X坐标 (0-1)
                
                // 基础生物群系倾向 + 噪声扰动（减少噪声影响）
                float biomeValue = normalizedX + biomeNoise * 0.1f;
                
                if (normalizedX < 0.3f && world.GetTile(x, surfaceY).Type == TileType.Dirt) // 左侧区域：保持泥土，不生成草地
                {
                    // 不做任何处理，保持原有的泥土
                }
                else if (normalizedX >= 0.3f && normalizedX < 0.7f && world.GetTile(x, surfaceY).Type == TileType.Sand) // 中间：沙漠
                {
                    byte sandStyle = (byte)NoiseGenerator.Random.Next(0, 15);
                        world.SetTile(x, surfaceY, new Tile(TileType.Sand, sandStyle));
                    // 将表面下几层也改为沙子
                    for (int depth = 1; depth <= 3; depth++)
                    {
                        if (surfaceY + depth < world.Height && 
                            world.GetTile(x, surfaceY + depth).Type == TileType.Sand)
                        {
                            var currentTile = world.GetTile(x, surfaceY + depth);
                            byte sandStyle2 = (byte)NoiseGenerator.Random.Next(0, 15);
                            world.SetTile(x, surfaceY + depth, new Tile(TileType.Sand, sandStyle2));
                        }
                    }
                }
                else if (normalizedX >= 0.7f && world.GetTile(x, surfaceY).Type == TileType.Snow) // 右侧区域：强制雪原，不受噪声影响
                {
                    byte snowStyle = (byte)NoiseGenerator.Random.Next(0, 15);
                        world.SetTile(x, surfaceY, new Tile(TileType.Snow, snowStyle));
                    // 将表面下几层也改为雪
                    for (int depth = 1; depth <= 2; depth++)
                    {
                        if (surfaceY + depth < world.Height && 
                            world.GetTile(x, surfaceY + depth).Type == TileType.Snow)
                        {
                            var currentTile = world.GetTile(x, surfaceY + depth);
                            byte snowStyle2 = (byte)NoiseGenerator.Random.Next(0, 15);
                            world.SetTile(x, surfaceY + depth, new Tile(TileType.Snow, snowStyle2));
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 阶段3.5：生成沙漠山
        /// </summary>
        private void DesertMountainPass()
        {
            Console.WriteLine("[沙漠山生成] 开始生成沙漠山");
            
            // 在沙漠区域生成沙漠山
            int desertStartX = (int)(world.Width * 0.3f);
            int desertEndX = (int)(world.Width * 0.7f);
            
            // 在沙漠区域中心生成一座沙漠山
            int mountainCenterX = (desertStartX + desertEndX) / 2;
            GenerateDesertMountain(mountainCenterX);
            
            Console.WriteLine($"[沙漠山生成] 在位置 {mountainCenterX} 生成沙漠山完成");
        }
        
        /// <summary>
        /// 生成单个沙漠山
        /// </summary>
        /// <param name="centerX">山的中心X坐标</param>
        private void GenerateDesertMountain(int centerX)
        {
            // 获取地表高度并下沉50格
            int surfaceY = world.SurfaceHeight[centerX];
            int baseY = surfaceY + 50; // 下沉50格作为山的底部
            
            // 沙漠山参数 - 完全的三角形
            const int BASE_WIDTH = 400; // 底部宽度300格
            const int WIDTH_DECREMENT = 2; // 每层减2格
            
            // 计算山的高度：从300减到0，每层减2，需要150层
            int MOUNTAIN_HEIGHT = BASE_WIDTH / WIDTH_DECREMENT; // 300/2 = 150层
            
            Console.WriteLine($"[沙漠山生成] 在 ({centerX}, {baseY}) 生成完全三角形沙漠山，高度: {MOUNTAIN_HEIGHT}, 底宽: {BASE_WIDTH}");
            
            // 从底部开始向上构建，从现有沙块下方向上延伸
            for (int layer = 0; layer < MOUNTAIN_HEIGHT; layer++)
            {
                // 计算当前层的宽度：每层严格减2
                int currentWidth = BASE_WIDTH - (layer * WIDTH_DECREMENT);
                
                // 如果宽度小于等于8格，停止生成（保留8格沙块在顶部）
                if (currentWidth <= 8) break;
                
                // 计算当前层的Y坐标（从底部向上构建）
                int currentY = baseY - layer;
                
                // 确保不超出世界边界
                if (currentY < 0) break;
                
                // 计算当前层的起始X坐标
                int startX = centerX - currentWidth / 2;
                
                // 放置沙漠块
                for (int x = 0; x < currentWidth; x++)
                {
                    int blockX = startX + x;
                    
                    // 确保在世界范围内
                    if (blockX >= 0 && blockX < world.Width)
                    {
                        // 直接放置沙漠块，不检查是否为空气（从地下向上延伸）
                        // 使用固定样式让沙块看起来一格一格的
                        byte sandStyle = 0; // 使用固定样式
                        world.SetTile(blockX, currentY, new Tile(TileType.Sand, sandStyle));
                    }
                }
                
                // 每10层输出一次进度
                if (layer % 10 == 0)
                {
                    Console.WriteLine($"[沙漠山生成] 第 {layer} 层完成，当前宽度: {currentWidth}");
                }
            }
            
            Console.WriteLine($"[沙漠山生成] 沙漠山构建完成，共 {MOUNTAIN_HEIGHT} 层");
            
            // 在沙漠山顶部生成传送门
            GenerateDesertPortalOnMountain(centerX, baseY, MOUNTAIN_HEIGHT);
        }
        
        /// <summary>
        /// 在沙漠山顶部生成沙漠传送门
        /// </summary>
        /// <param name="centerX">山的中心X坐标</param>
        /// <param name="baseY">山的底部Y坐标</param>
        /// <param name="mountainHeight">山的高度</param>
        private void GenerateDesertPortalOnMountain(int centerX, int baseY, int mountainHeight)
        {
            // 计算山顶位置（8格宽的平台）
            // 找到最后一层沙块的位置（宽度为8格的那一层）
            const int BASE_WIDTH = 400;
            const int WIDTH_DECREMENT = 2;
            
            // 计算到达8格宽度时的层数
            int layersToTop = (BASE_WIDTH - 8) / WIDTH_DECREMENT; // (400-8)/2 = 196层
            int topY = baseY - layersToTop; // 山顶沙块的Y坐标
            
            int portalX = centerX; // 传送门X坐标在山的中心
            int portalY = topY; // 传送门直接贴在山顶沙块上
            
            // 确保传送门位置在世界范围内
            if (portalX >= 0 && portalX < world.Width && portalY >= 0 && portalY < world.Height)
            {
                // 构建传送门结构
                BuildPortalStructureInWorld(portalX, portalY);
                
                // 创建沙漠传送门对象
                var portal = Portal.CreateDesertPortal(new Vector2(portalX, portalY));
                
                Console.WriteLine($"[沙漠山生成] 在沙漠山顶部位置 ({portalX}, {portalY}) 创建沙漠传送门，ID: {portal.Id}，山顶层数: {layersToTop}");
            }
            else
            {
                Console.WriteLine($"[沙漠山生成] 沙漠传送门位置超出世界边界: ({portalX}, {portalY})");
            }
        }
        
        /// <summary>
        /// 阶段4：生成洞穴（确保地表有入口）
        /// </summary>
        private void CavesPass()
        {
            Random random = new Random(seed);
            
            // 生成多个洞穴系统，每个都有地表入口
            int caveCount = world.Width / 150; // 减少洞穴数量（原来是/100）
            
            for (int caveIndex = 0; caveIndex < caveCount; caveIndex++)
            {
                // 随机选择地表入口位置
                int entranceX = random.Next(50, world.Width - 50);
                int entranceY = world.SurfaceHeight[entranceX];
                
                // 生成从地表开始的洞穴系统，不在洞穴中放置传送门
                GenerateCaveSystem(entranceX, entranceY, random, false);
            }
            
            // 在出生点右边固定距离的地表上放置洞穴传送门
            int spawnX = 10; // 出生点X坐标（与GamePlayState中保持一致）
            int portalX = spawnX + 50; // 出生点右边50个方块的位置
            int portalY = world.SurfaceHeight[portalX];
            PlaceCavePortal(portalX, portalY);
            
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
                        world.SetTile(x, y, new Tile(TileType.Air, 0));
                        RemoveBackgroundWallIfShallow(x, y);
                    }
                }
            }
            
            // 生成一些随机的大洞穴
            GenerateRandomCaves();
            
            // 生成从地表向下的垂直通道和地下房间
            GenerateVerticalShaftAndUndergroundRoom(random);
        }
        
        /// <summary>
        /// 生成从地表开始的洞穴系统
        /// </summary>
        private void GenerateCaveSystem(int entranceX, int entranceY, Random random, bool shouldPlacePortal = false)
        {
            // 从地表向下挖掘主通道
            int currentX = entranceX;
            int currentY = entranceY;
            
            // 挖掘垂直入口
            for (int depth = 0; depth < 10; depth++)
            {
                if (currentY + depth < world.Height)
                {
                    int tileY = currentY + depth;
                    
                    // 挖掘中心
                    world.SetTile(currentX, tileY, new Tile(TileType.Air, 0));
                    RemoveBackgroundWallIfShallow(currentX, tileY);
                    
                    // 稍微扩大入口
                    if (currentX > 0) 
                    {
                        world.SetTile(currentX - 1, tileY, new Tile(TileType.Air, 0));
                        RemoveBackgroundWallIfShallow(currentX - 1, tileY);
                    }
                    if (currentX < world.Width - 1) 
                    {
                        world.SetTile(currentX + 1, tileY, new Tile(TileType.Air, 0));
                        RemoveBackgroundWallIfShallow(currentX + 1, tileY);
                    }
                }
            }
            
            // 在洞穴入口放置传送门（只在第一个洞穴系统中放置）
            if (shouldPlacePortal)
            {
                PlaceCavePortal(currentX, currentY + 2);
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
            int caveCount = world.Width / 300; // 减少随机大洞穴数量（原来是/200）
            
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
                                world.SetTile(tileX, tileY, new Tile(TileType.Air, 0));
                                RemoveBackgroundWallIfShallow(tileX, tileY);
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
            // 使用噪声生成自然的生物群系边界
            for (int x = 0; x < world.Width; x++)
            {
                float normalizedX = (float)x / world.Width;
                
                // 使用噪声创建不规则的边界
                float biomeNoise = NoiseGenerator.Noise2D(x * 0.005f, 0, 1f, 3, 0.4f);
                float biomeValue = normalizedX + biomeNoise * 0.2f;
                
                // 动态边界，而不是固定的三等分
                float leftBoundary = 0.33f + NoiseGenerator.Noise2D(x * 0.003f, 50, 1f, 2, 0.3f) * 0.12f;
                float rightBoundary = 0.67f + NoiseGenerator.Noise2D(x * 0.003f, 150, 1f, 2, 0.3f) * 0.12f;
                
                TileType biomeType;
                if (biomeValue < leftBoundary)
                {
                    biomeType = TileType.Snow;      // 左侧雪地
                }
                else if (biomeValue < rightBoundary)
                {
                    biomeType = TileType.Grass;     // 中间普通
                }
                else
                {
                    biomeType = TileType.JungleGrass; // 右侧丛林
                }
                
                // 只替换表面的草地
                int surfaceY = world.SurfaceHeight[x];
                if (world.GetTile(x, surfaceY).Type == TileType.Grass && biomeType != TileType.Grass)
                {
                    // 地表生物群系方块没有背景墙
                    world.SetTile(x, surfaceY, new Tile(biomeType, 0));
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
            int depositCount = world.Width / 75; // 增加一倍大理石群数量（原来是/150）
            
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
                                    var currentTile = world.GetTile(x, y);
                                    world.SetTile(x, y, new Tile(TileType.Marble, 0));
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
            int depositCount = world.Width / 2; // 再增加一倍岩石密度（原来是/4）
            
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
                                    var currentTile = world.GetTile(x, y);
                                    byte stoneStyle = (byte)NoiseGenerator.Random.Next(0, 15);   // stone 有 15 种花纹
                                    world.SetTile(x, y, new Tile(TileType.Stone, stoneStyle));
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
            int depositCount = world.Width / 4; // 再增加一倍泥土密度（原来是/8）
            
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
                                    var currentTile = world.GetTile(x, y);
                                    byte style = (byte)NoiseGenerator.Random.Next(0, 15);   // dirt 有 15 种花纹
                                    world.SetTile(x, y, new Tile(TileType.Dirt, style));
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
                            var currentTile = world.GetTile(x, y);
                            world.SetTile(x, y, new Tile(oreType, 0));
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
                            // 水没有背景墙
                            world.SetTile(x, y, new Tile(TileType.Water, 0));
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
                        // 熔岩没有背景墙
                        world.SetTile(x, y, new Tile(TileType.Lava, 0));
                    }
                }
            }
        }
        
        /// <summary>
        /// 阶段7：最终处理
        /// </summary>
        private void FinalizePass()
        {
            // 移除草地生成逻辑，保持表面泥土不变
            // 不再将表面泥土转换为草地
            
            // 垂直蚀刻 - 削掉90度绝壁
            OnProgressUpdate?.Invoke("垂直蚀刻处理", 0.85f);
            VerticalEtchingPass();
            
            // 泥土草皮处理 - 为暴露在外的泥土添加草皮效果（在帧处理之前）
            OnProgressUpdate?.Invoke("处理泥土草皮", 0.9f);
            ProcessDirtGrassOverlay();
            
            // 处理帧变体和平滑斜坡 - 消除台阶感（但跳过已设置草皮的泥土）
            OnProgressUpdate?.Invoke("处理贴图帧和平滑地形", 0.95f);
            TileFrameProcessor.ProcessWorld(world);
            
            // 触发世界生成完成事件，通知需要重建渲染缓存
            OnWorldGenerationComplete?.Invoke(world);
        }
        
        /// <summary>
        /// 处理泥土草皮 - 为暴露在外的泥土添加草皮效果
        /// 使用288*270草皮纹理的上面288*270部分
        /// </summary>
        private void ProcessDirtGrassOverlay()
        {
            // 创建一个列表来存储没有暴露的泥土块位置
            List<(int x, int y)> unexposedDirtTiles = new List<(int x, int y)>();
            
            for (int x = 0; x < world.Width; x++)
            {
                for (int y = 0; y < world.Height; y++)
                {
                    var tile = world.GetTile(x, y);
                    if (tile.Type != TileType.Dirt) continue;
                    
                    // 检查泥土方块的暴露情况
                    bool exposedUp = !IsSolidTile(x, y - 1);
                    bool exposedDown = !IsSolidTile(x, y + 1);
                    bool exposedLeft = !IsSolidTile(x - 1, y);
                    bool exposedRight = !IsSolidTile(x + 1, y);
                    
                    // 根据暴露方向设置对应的草皮帧
                    // 检查对角暴露情况
                    bool exposedUpLeft = exposedUp && exposedLeft;
                    bool exposedUpRight = exposedUp && exposedRight;
                    bool exposedDownLeft = exposedDown && exposedLeft;
                    bool exposedDownRight = exposedDown && exposedRight;
                    
                    if (exposedUpLeft)
                    {
                        // 上左暴露：使用第四行的1,3,5列 (行3，列0,2,4)
                        int[] columns = {0, 2, 4}; // 1,3,5列对应索引0,2,4
                        int colIndex = NoiseGenerator.NextInt(0, 3);
                        int frameIndex = 3 * 16 + columns[colIndex]; // 第4行(索引3) * 16列 + 列索引
                        world.SetTile(x, y, new Tile(TileType.Grass, 0, 0, (byte)frameIndex));
                        
                        if (x % 20 == 0 && y % 20 == 0)
                        {
                            Console.WriteLine($"[草皮处理] 上左暴露泥土 ({x},{y}) 改为草皮类型，帧变体: {frameIndex} (第4行第{columns[colIndex]+1}列)");
                        }
                    }
                    else if (exposedUpRight)
                    {
                        // 上右暴露：使用第四行的2,4,6列 (行3，列1,3,5)
                        int[] columns = {1, 3, 5}; // 2,4,6列对应索引1,3,5
                        int colIndex = NoiseGenerator.NextInt(0, 3);
                        int frameIndex = 3 * 16 + columns[colIndex]; // 第4行(索引3) * 16列 + 列索引

                        world.SetTile(x, y, new Tile(TileType.Grass, 0, 0, (byte)frameIndex));
                        
                        if (x % 20 == 0 && y % 20 == 0)
                        {
                            Console.WriteLine($"[草皮处理] 上右暴露泥土 ({x},{y}) 改为草皮类型，帧变体: {frameIndex} (第4行第{columns[colIndex]+1}列)");
                        }
                    }
                    else if (exposedDownLeft)
                    {
                        // 下左暴露：使用第五行的1,3,5列 (行4，列0,2,4)
                        int[] columns = {0, 2, 4}; // 1,3,5列对应索引0,2,4
                        int colIndex = NoiseGenerator.NextInt(0, 3);
                        int frameIndex = 4 * 16 + columns[colIndex]; // 第5行(索引4) * 16列 + 列索引

                        world.SetTile(x, y, new Tile(TileType.Grass, 0, 0, (byte)frameIndex));
                        
                        if (x % 20 == 0 && y % 20 == 0)
                        {
                            Console.WriteLine($"[草皮处理] 下左暴露泥土 ({x},{y}) 改为草皮类型，帧变体: {frameIndex} (第5行第{columns[colIndex]+1}列)");
                        }
                    }
                    else if (exposedDownRight)
                    {
                        // 下右暴露：使用第五行的2,4,6列 (行4，列1,3,5)
                        int[] columns = {1, 3, 5}; // 2,4,6列对应索引1,3,5
                        int colIndex = NoiseGenerator.NextInt(0, 3);
                        int frameIndex = 4 * 16 + columns[colIndex]; // 第5行(索引4) * 16列 + 列索引
                        // 地表草皮没有背景墙，地下草皮保持原有墙类型
                        world.SetTile(x, y, new Tile(TileType.Grass, 0, 0, (byte)frameIndex));
                        
                        if (x % 20 == 0 && y % 20 == 0)
                        {
                            Console.WriteLine($"[草皮处理] 下右暴露泥土 ({x},{y}) 改为草皮类型，帧变体: {frameIndex} (第5行第{columns[colIndex]+1}列)");
                        }
                    }
                    else if (exposedUp)
                    {
                        // 仅上面暴露：使用第一行的草皮贴图
                        int grassFrame = NoiseGenerator.NextInt(1, 4); // 1, 2, 3
                        // 地表草皮没有背景墙，地下草皮保持原有墙类型
                        world.SetTile(x, y, new Tile(TileType.Grass, 0, 0, (byte)grassFrame));
                        
                        if (x % 20 == 0 && y % 20 == 0)
                        {
                            Console.WriteLine($"[草皮处理] 上暴露泥土 ({x},{y}) 改为草皮类型，帧变体: {grassFrame}");
                        }
                    }
                    else if (exposedDown)
                    {
                        // 仅下面暴露：使用第三行的草皮贴图
                        int grassFrame = NoiseGenerator.NextInt(1, 4);
                        // 地表草皮没有背景墙，地下草皮保持原有墙类型
                        world.SetTile(x, y, new Tile(TileType.Grass, 0, 0, (byte)(grassFrame + 32)));
                        
                        if (x % 20 == 0 && y % 20 == 0)
                        {
                            Console.WriteLine($"[草皮处理] 下暴露泥土 ({x},{y}) 改为草皮类型，帧变体: {grassFrame + 32}");
                        }
                    }
                    else if (exposedLeft)
                    {
                        // 仅左边暴露：使用第一列的草皮贴图
                        int grassFrame = NoiseGenerator.NextInt(1, 4);
                        // 地表草皮没有背景墙，地下草皮保持原有墙类型
                        world.SetTile(x, y, new Tile(TileType.Grass, 0, 0, (byte)(grassFrame * 16)));
                        
                        if (x % 20 == 0 && y % 20 == 0)
                        {
                            Console.WriteLine($"[草皮处理] 左暴露泥土 ({x},{y}) 改为草皮类型，帧变体: {grassFrame * 16}");
                        }
                    }
                    else if (exposedRight)
                    {
                        // 仅右边暴露：使用第四列的草皮贴图
                        int grassFrame = NoiseGenerator.NextInt(1, 4);
                        // 地表草皮没有背景墙，地下草皮保持原有墙类型
                        world.SetTile(x, y, new Tile(TileType.Grass, 0, 0, (byte)(grassFrame * 16 + 3)));
                        
                        if (x % 20 == 0 && y % 20 == 0)
                        {
                            Console.WriteLine($"[草皮处理] 右暴露泥土 ({x},{y}) 改为草皮类型，帧变体: {grassFrame * 16 + 3}");
                        }
                    }
                    else
                    {
                        // 没有暴露的泥土块，添加到列表中
                        unexposedDirtTiles.Add((x, y));
                    }
                }
            }
            
            // 地下随机草皮生成（类似石头生成逻辑）
            GenerateUndergroundGrass();
        }
        
        /// <summary>
        /// 检查指定位置是否为固体方块
        /// </summary>
        private bool IsSolidTile(int x, int y)
        {
            if (!world.IsValidCoordinate(x, y))
                return false;
                
            var tile = world.GetTile(x, y);
            return tile.Type != TileType.Air && tile.Type != TileType.Water && tile.Type != TileType.Lava;
        }
        

        
        /// <summary>
        /// 垂直蚀刻处理 - 削掉90度绝壁
        /// </summary>
        private void VerticalEtchingPass()
        {
            for (int x = 0; x < world.Width - 1; x++)
            {
                int currentHeight = world.SurfaceHeight[x];
                int nextHeight = world.SurfaceHeight[x + 1];
                int heightDiff = Math.Abs(currentHeight - nextHeight);
                
                // 如果高度差超过4格，进行垂直蚀刻
                if (heightDiff > 4)
                {
                    int higherY = Math.Min(currentHeight, nextHeight);
                    int lowerY = Math.Max(currentHeight, nextHeight);
                    
                    // 确定蚀刻的起始位置和方向
                    int etchX = currentHeight > nextHeight ? x : x + 1;
                    int etchStartY = higherY;
                    
                    // 向下挖掘2-3格的蠕虫洞
                    int etchDepth = NoiseGenerator.NextInt(2, 4);
                    GenerateVerticalEtch(etchX, etchStartY, etchDepth);
                }
            }
        }
        
        /// <summary>
        /// 生成垂直蚀刻洞穴
        /// </summary>
        private void GenerateVerticalEtch(int startX, int startY, int depth)
        {
            float x = startX;
            float y = startY;
            
            for (int i = 0; i < depth * 3; i++)
            {
                // 挖掘小型圆形区域
                int radius = NoiseGenerator.NextInt(2, 4);
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (dx * dx + dy * dy <= radius * radius)
                        {
                            int tileX = (int)x + dx;
                            int tileY = (int)y + dy;
                            if (world.IsValidCoordinate(tileX, tileY) && tileY >= startY)
                            {
                                world.SetTile(tileX, tileY, new Tile(TileType.Air, 0));
                                RemoveBackgroundWallIfShallow(tileX, tileY);
                            }
                        }
                    }
                }
                
                // 主要向下移动，略微随机偏移
                x += NoiseGenerator.NextFloat(-0.5f, 0.5f);
                y += 1.0f + NoiseGenerator.NextFloat(0f, 0.5f);
                
                // 边界检查
                if (x < 5 || x >= world.Width - 5 || y >= world.Height - 5)
                    break;
            }
        }
        
        /// <summary>
        /// 背景墙生成阶段
        /// </summary>
        private void BackgroundWallPass()
        {
            Console.WriteLine("[背景墙生成] 开始生成背景墙数据...");
            
            for (int x = 0; x < world.Width; x++)
            {
                for (int y = 0; y < world.Height; y++)
                {
                    TileType wallType = GetFixedBackgroundWallType(x, y);
                    world.SetBackgroundWall(x, y, wallType);
                }
            }
            
            Console.WriteLine("[背景墙生成] 背景墙数据生成完成");
        }
        
        /// <summary>
        /// 获取固定的背景墙类型（基于地形生成算法）
        /// </summary>
        private TileType GetFixedBackgroundWallType(int x, int y)
        {
            // 获取地表高度
            int surfaceHeight = world.SurfaceHeight[x];
            
            // 地表以上为空气
            if (y < surfaceHeight)
            {
                return TileType.Air;
            }
            
            // 计算深度
            int depth = y - surfaceHeight;
            
            // 深层区域（距离地表30格以下）使用石头背景墙
            if (depth >= 30)
            {
                return TileType.Stone;
            }
            
            // 浅层区域根据生物群系决定（与BiomeBackgroundManager保持一致）
            // 世界被分为3个区域：森林、沙漠、雪地
            int sectionWidth = world.Width / 3;
            int section = x / sectionWidth;
            
            return section switch
            {
                0 => TileType.Dirt,  // 左侧森林 - 泥土背景墙
                1 => TileType.Sand,  // 中间沙漠 - 沙子背景墙
                2 => TileType.Snow,  // 右侧雪地 - 雪背景墙
                _ => TileType.Dirt   // 默认泥土
            };
        }
        
        /// <summary>
        /// 如果位置足够浅，移除背景墙
        /// </summary>
        private void RemoveBackgroundWallIfShallow(int x, int y)
        {
            if (world.IsValidCoordinate(x, y))
            {
                int surfaceHeight = world.SurfaceHeight[x];
                int depth = y - surfaceHeight;
                int maxDepth = world.Height - surfaceHeight;
                if (depth < maxDepth * 1 / 8)
                {
                    world.SetBackgroundWall(x, y, TileType.Air);
                }
            }
        }

        /// <summary>
        /// 生成空岛阶段
        /// </summary>
        private void FloatingIslandsPass()
        {
            var random = new Random(seed);
            
            // 整个世界只生成一个空岛
            int islandCount = 1;
            
            for (int i = 0; i < islandCount; i++)
            {
                GenerateFloatingIsland(random);
            }
        }
        
        /// <summary>
        /// 生成单个空岛
        /// </summary>
        private void GenerateFloatingIsland(Random random)
        {
            // 确定空岛位置 - 固定生成在地图右四分之一的地方
            int rightQuarterStart = world.Width * 3 / 4; // 右四分之一开始位置
            int rightQuarterEnd = world.Width - 100; // 右边界留100格边距
            int centerX = random.Next(rightQuarterStart, rightQuarterEnd);
            int surfaceLevel = settings.GetSurfaceLevel();
            int skyZoneStart = Math.Max(50, surfaceLevel - 400); // 天空区域开始位置，更高
            int centerY = random.Next(skyZoneStart, surfaceLevel - 150); // 确保在地表上方，位置更高
            
            // 生成独特的梯形空岛结构
            int surfaceWidth = 90; // 表面宽度
            int islandHeight = surfaceWidth + 2; // 空岛高度，前两层保持30，然后递减到1
            
            // 生成梯形岛屿主体
            for (int layer = 0; layer < islandHeight; layer++)
            {
                int currentWidth;
                if (layer < 4) // 前两层保持30个方块
                {
                    currentWidth = surfaceWidth;
                }
                else // 从第三层开始递减
                {
                    currentWidth = surfaceWidth - ((layer - 2) * 2); // 每层左右各减少1个
                }
                if (currentWidth <= 0) break;
                
                int startX = centerX - currentWidth / 2;
                int currentY = centerY + layer;
                
                for (int i = 0; i < currentWidth; i++)
                {
                    int x = startX + i;
                    
                    // 检查边界
                    if (x < 0 || x >= world.Width || currentY < 0 || currentY >= world.Height)
                        continue;
                    
                    // 添加边缘噪声（只在边缘位置）
                    bool isEdge = (i == 0 || i == currentWidth - 1);
                    bool shouldPlace = true;
                    
                    if (isEdge && random.NextDouble() < 0.3) // 30%概率在边缘添加噪声
                    {
                        shouldPlace = false;
                    }
                    
                    if (shouldPlace)
                    {
                        TileType tileType;
                        
                        // 确定方块类型
                        if (layer == 0) // 表面层
                        {
                            tileType = TileType.Grass; // 草方块
                        }
                        else if (layer == 1 || layer == 2) // 下面两层
                        {
                            tileType = TileType.Dirt; // 泥土方块
                        }
                        else if (isEdge) // 斜边位置
                        {
                            tileType = TileType.Tiles_189; // 斜边也使用草方块
                        }
                        else // 其余层
                        {
                            tileType = TileType.Tiles_189; // 云层方块
                        }
                        
                        world.SetTile(x, currentY, new Tile(tileType));
                        // 设置背景墙为白色像素块（使用Air表示白色背景）
                        world.SetBackgroundWall(x, currentY, TileType.Air);
                    }
                }
            }
            
            // 在空岛表面中央放置传送门（centerY是空岛顶部，表面应该是centerY）
            PlaceIslandPortal(centerX, centerY);
        }
        
        /// <summary>
        /// 在洞穴入口放置传送门
        /// </summary>
        private void PlaceCavePortal(int x, int y)
        {
            // 在洞穴入口构建完整的传送门结构
            if (x >= 0 && x < world.Width && y >= 0 && y < world.Height)
            {
                // 构建传送门结构
                BuildPortalStructureInWorld(x, y);
                
                // 创建传送门对象
                var portal = Portal.CreateCavePortal(new Vector2(x, y));
                
                Console.WriteLine($"[世界生成器] 在洞穴位置 ({x}, {y}) 放置传送门，ID: {portal.Id}");
            }
        }
        
        /// <summary>
        /// 在空岛表面放置传送门
        /// </summary>
        private void PlaceIslandPortal(int centerX, int centerY)
        {
            // 在空岛表面构建完整的传送门结构
            if (centerX >= 0 && centerX < world.Width && centerY >= 0 && centerY < world.Height)
            {
                // 构建传送门结构
                BuildPortalStructureInWorld(centerX, centerY);
                
                // 创建传送门对象
                var portal = Portal.CreateIslandPortal(new Vector2(centerX, centerY));
                
                Console.WriteLine($"[世界生成器] 在空岛位置 ({centerX}, {centerY}) 放置传送门，ID: {portal.Id}");
            }
        }
        
        /// <summary>
        /// 在空岛内部生成洞穴
        /// </summary>
        private void GenerateIslandCaves(Random random, int centerX, int centerY, int islandWidth, int islandHeight)
        {
            // 生成2-4个小洞穴
            int caveCount = random.Next(2, 5);
            
            for (int i = 0; i < caveCount; i++)
            {
                // 洞穴起始位置在岛屿内部
                int caveStartX = centerX + random.Next(-islandWidth/2, islandWidth/2);
                int caveStartY = centerY + random.Next(2, islandHeight - 2); // 避免在表面和最底层生成洞穴
                
                // 检查起始位置是否在岛屿内
                if (caveStartX < 0 || caveStartX >= world.Width || caveStartY < 0 || caveStartY >= world.Height)
                    continue;
                    
                var tileType = world.GetTile(caveStartX, caveStartY).Type;
                if (tileType != TileType.Tiles_189 && tileType != TileType.Dirt)
                    continue;
                
                // 生成小型蠕虫洞穴
                GenerateSmallWormCave(caveStartX, caveStartY, random.Next(10, 25), random);
            }
        }
        
        /// <summary>
        /// 生成小型蠕虫洞穴（用于空岛内部）
        /// </summary>
        private void GenerateSmallWormCave(int startX, int startY, int length, Random random)
        {
            float currentX = startX;
            float currentY = startY;
            float direction = (float)(random.NextDouble() * Math.PI * 2);
            
            for (int i = 0; i < length; i++)
            {
                // 挖掘当前位置周围的小区域
                int radius = random.Next(1, 3);
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        int x = (int)currentX + dx;
                        int y = (int)currentY + dy;
                        
                        if (x >= 0 && x < world.Width && y >= 0 && y < world.Height)
                        {
                            if (dx * dx + dy * dy <= radius * radius)
                            {
                                world.SetTile(x, y, Tile.Air);
                            }
                        }
                    }
                }
                
                // 更新方向（轻微随机变化）
                direction += (float)(random.NextDouble() - 0.5) * 0.5f;
                
                // 移动到下一个位置
                currentX += (float)Math.Cos(direction) * 1.5f;
                currentY += (float)Math.Sin(direction) * 1.5f;
            }
        }
        
        /// <summary>
        /// 阶段8：生成传送门
        /// </summary>
        private void PortalsPass()
        {
            Console.WriteLine("[传送门生成] 传送门已在地形生成时放置");
            
            // 传送门已在地形生成时放置，这里只需要统计
            var allPortals = Portal.GetAllPortals();
            Console.WriteLine($"[传送门生成] 传送门生成完成，共生成 {allPortals.Count} 个传送门");
            
            // 显示传送门信息
            foreach (var portal in allPortals.Values)
            {
                Console.WriteLine($"[传送门生成] 传送门 ID:{portal.Id}, 位置:({portal.Position.X}, {portal.Position.Y}), 名称:{portal.Name}");
            }
        }
        

        
        /// <summary>
        /// 在世界中构建传送门结构 - 完全悬空，不依赖地表
        /// </summary>
        /// <param name="centerX">传送门中心X坐标</param>
        /// <param name="centerY">传送门中心Y坐标</param>
        private void BuildPortalStructureInWorld(int centerX, int centerY)
        {
            // 传送门结构尺寸
            const int PORTAL_WIDTH = 8;
            
            // 使用固定的悬空位置，不依赖地表
            // 传送门结构将完全悬空，底部位置就是传入的centerY
            int startX = centerX - PORTAL_WIDTH / 2;
            int baseY = centerY; // 直接使用传入的Y坐标作为底部基准
            
            // 第1层（最底层）：8个石块
            for (int x = 0; x < 8; x++)
            {
                world.SetTile(startX + x, baseY, new Tile(TileType.Stone));
            }
            
            // 第2层：6个石块（中间缩进）
            for (int x = 1; x < 7; x++)
            {
                world.SetTile(startX + x, baseY - 1, new Tile(TileType.Stone));
            }
            
            // 左侧柱子：8个大理石块（在6个石块的第一个位置上）
            for (int y = 2; y <= 9; y++)
            {
                world.SetTile(startX + 1, baseY - y, new Tile(TileType.Marble));
            }
            
            // 右侧柱子：8个大理石块（在6个石块的最后一个位置上）
            for (int y = 2; y <= 9; y++)
            {
                world.SetTile(startX + 6, baseY - y, new Tile(TileType.Marble));
            }
            
            // 顶部大理石封顶（第10层，连接两个柱子）
            for (int x = 1; x <= 6; x++)
            {
                world.SetTile(startX + x, baseY - 10, new Tile(TileType.Marble));
            }
            
            // 最顶层：4个石块（中间）
            for (int x = 2; x < 6; x++)
            {
                world.SetTile(startX + x, baseY - 11, new Tile(TileType.Stone));
            }
            
            // 在传送门中间填充雪块
            for (int x = 2; x <= 5; x++) // 两个柱子之间的4格宽度（x=2,3,4,5）
            {
                for (int y = 2; y <= 9; y++) // 从第3层到第10层，完全填充柱子之间的空间
                {
                    world.SetTile(startX + x, baseY - y, new Tile(TileType.Snow));
                }
            }
            
            // 在传送门结构中心位置放置Portal瓦片作为标记
            // 放置在传送门结构的中心高度（第5层），便于交互检测
            world.SetTile(centerX, baseY - 5, new Tile(TileType.Portal, 1));
        }
        
        /// <summary>
        /// 寻找空岛地表位置
        /// </summary>
        /// <param name="centerX">中心X坐标</param>
        /// <param name="centerY">中心Y坐标</param>
        /// <returns>地表Y坐标，如果找不到返回-1</returns>
        private int FindIslandSurface(int centerX, int centerY)
        {
            // 首先检查当前位置是否就是空岛表面
            if (centerY < world.Height - 1)
            {
                var currentTile = world.GetTile(centerX, centerY);
                var belowTile = world.GetTile(centerX, centerY + 1);
                
                // 如果当前是空气，下面是固体，则这里就是地表
                if (currentTile.Type == TileType.Air && belowTile.Type != TileType.Air)
                {
                    return centerY;
                }
            }
            
            // 向下搜索空岛表面（限制搜索范围，避免找到地面）
            for (int y = centerY + 1; y < Math.Min(world.Height - 1, centerY + 50); y++)
            {
                var currentTile = world.GetTile(centerX, y);
                var belowTile = world.GetTile(centerX, y + 1);
                
                // 如果当前是空气，下面是固体，则这里是地表
                if (currentTile.Type == TileType.Air && belowTile.Type != TileType.Air)
                {
                    return y;
                }
            }
            
            // 向上搜索空岛表面
            for (int y = centerY - 1; y >= Math.Max(0, centerY - 50); y--)
            {
                var currentTile = world.GetTile(centerX, y);
                var belowTile = world.GetTile(centerX, y + 1);
                
                // 如果当前是空气，下面是固体，则这里是地表
                if (currentTile.Type == TileType.Air && belowTile.Type != TileType.Air)
                {
                    return y;
                }
            }
            
            // 如果没找到合适的地表，返回传送门位置
            return centerY;
        }
        
        /// <summary>
        /// 获取传送门管理器（供外部访问）
        /// </summary>
        public PortalManager GetPortalManager()
        {
            return portalManager;
        }
        
        /// <summary>
        /// 生成从地表向下的垂直通道和地下房间
        /// </summary>
        private void GenerateVerticalShaftAndUndergroundRoom(Random random)
        {
            // 选择通道的起始位置（在世界左三分之一处）
            int shaftX = world.Width / 4 + random.Next(-50, 50);
            int surfaceY = world.SurfaceHeight[shaftX];
            
            // 通道参数
            const int SHAFT_WIDTH = 20;
            const int SHAFT_DEPTH = 200;
            const int ROOM_SIZE = 150;
            
            Console.WriteLine($"[垂直通道生成] 开始在位置 ({shaftX}, {surfaceY}) 生成垂直通道，宽度: {SHAFT_WIDTH}，深度: {SHAFT_DEPTH}");
            
            // 生成垂直通道
            for (int depth = 0; depth < SHAFT_DEPTH; depth++)
            {
                int currentY = surfaceY + depth;
                
                // 确保不超出世界边界
                if (currentY >= world.Height - ROOM_SIZE - 10)
                    break;
                
                // 挖掘通道宽度
                for (int width = 0; width < SHAFT_WIDTH; width++)
                {
                    int currentX = shaftX - SHAFT_WIDTH / 2 + width;
                    
                    // 确保X坐标在世界范围内
                    if (currentX >= 0 && currentX < world.Width)
                    {
                        world.SetTile(currentX, currentY, new Tile(TileType.Air, 0));
                        RemoveBackgroundWallIfShallow(currentX, currentY);
                    }
                }
            }
            
            // 计算地下房间的起始位置
            int roomStartY = surfaceY + SHAFT_DEPTH;
            int roomCenterX = shaftX;
            
            Console.WriteLine($"[地下房间生成] 开始在位置 ({roomCenterX}, {roomStartY}) 生成地下房间，尺寸: {ROOM_SIZE}x{ROOM_SIZE}");
            
            // 生成地下房间
            for (int x = 0; x < ROOM_SIZE; x++)
            {
                for (int y = 0; y < ROOM_SIZE; y++)
                {
                    int roomX = roomCenterX - ROOM_SIZE / 2 + x;
                    int roomY = roomStartY + y;
                    
                    // 确保坐标在世界范围内
                    if (roomX >= 0 && roomX < world.Width && roomY >= 0 && roomY < world.Height)
                    {
                        world.SetTile(roomX, roomY, new Tile(TileType.Air, 0));
                        RemoveBackgroundWallIfShallow(roomX, roomY);
                    }
                }
            }
            
            Console.WriteLine($"[垂直通道和地下房间] 生成完成，通道深度: {SHAFT_DEPTH}，房间尺寸: {ROOM_SIZE}x{ROOM_SIZE}");
        }
    }
}