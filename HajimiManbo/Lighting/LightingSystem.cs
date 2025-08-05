using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using HajimiManbo.World;

namespace HajimiManbo.Lighting
{
    /// <summary>
    /// 光照系统，负责计算和管理世界中的光照
    /// </summary>
    public class LightingSystem
    {
        private World.World _world;
        private float[,] _lightMap;
        private bool _lightingEnabled;
        private List<LightSource> _lightSources;
        
        // 光照常量
        private const float AMBIENT_LIGHT = 0.01f; // 环境光强度（完全黑暗）
        private const float MAX_LIGHT = 1.0f; // 最大光照强度
        private const float LIGHT_FALLOFF = 0.2f; // 光照衰减率
        
        // 状态跟踪
    private bool _hasInitializedLighting = false;
        
        public bool LightingEnabled 
        { 
            get => _lightingEnabled; 
            set 
            { 
                _lightingEnabled = value;
                if (value)
                {
                    RecalculateLighting();
                }
            } 
        }
        
        public LightingSystem(World.World world)
        {
            _world = world;
            _lightSources = new List<LightSource>();
            _lightingEnabled = true; // 默认启用光照
            
            if (_world != null)
            {
                _lightMap = new float[_world.Width, _world.Height];
                InitializeLightMap();
            }
        }
        
        /// <summary>
        /// 初始化光照图，所有位置设为环境光
        /// </summary>
        private void InitializeLightMap()
        {
            if (_lightMap == null) return;
            
            for (int x = 0; x < _world.Width; x++)
            {
                for (int y = 0; y < _world.Height; y++)
                {
                    _lightMap[x, y] = AMBIENT_LIGHT;
                }
            }
        }
        
        /// <summary>
        /// 添加光源
        /// </summary>
        public void AddLightSource(Vector2 position, float intensity, Color color)
        {
            _lightSources.Add(new LightSource
            {
                Position = position,
                Intensity = intensity,
                Color = color,
                IsActive = true
            });
            
            if (_lightingEnabled)
            {
                RecalculateLighting();
            }
        }
        
        /// <summary>
        /// 移除光源
        /// </summary>
        public void RemoveLightSource(Vector2 position)
        {
            _lightSources.RemoveAll(ls => Vector2.Distance(ls.Position, position) < 1.0f);
            
            if (_lightingEnabled)
            {
                RecalculateLighting();
            }
        }
        
        /// <summary>
        /// 获取指定位置的光照强度
        /// </summary>
        public float GetLightLevel(int x, int y)
        {
            if (!_lightingEnabled)
                return MAX_LIGHT;
                
            if (_lightMap == null || x < 0 || y < 0 || x >= _world.Width || y >= _world.Height)
                return AMBIENT_LIGHT;
                
            return _lightMap[x, y];
        }
        
        /// <summary>
        /// 获取指定位置的光照颜色
        /// </summary>
        public Color GetLightColor(int x, int y)
        {
            if (!_lightingEnabled)
                return Color.White;
                
            float lightLevel = GetLightLevel(x, y);
            
            // 确保光照值在有效范围内
            lightLevel = Math.Max(0.0f, Math.Min(1.0f, lightLevel));
                
            // 根据光照强度调整颜色，环境光也应该有对应的可见度
            return Color.Lerp(Color.Black, Color.White, lightLevel);
        }
        
        /// <summary>
        /// 重新计算整个光照图
        /// </summary>
        public void RecalculateLighting(Vector2? playerPosition = null)
        {
            if (!_lightingEnabled || _lightMap == null)
                return;
                
            // 只在首次初始化时计算光照
            if (!_hasInitializedLighting)
            {
                _hasInitializedLighting = true;
                
                // 重置光照图为环境光
                InitializeLightMap();
                
                // 计算天空光照
                CalculateSkyLight();
                
                // 计算每个光源的影响
                foreach (var lightSource in _lightSources)
                {
                    if (lightSource.IsActive)
                    {
                        CalculateLightFromSource(lightSource);
                    }
                }
            }
        }
        
        /// <summary>
        /// 计算天空光照，从暴露表面向内传播
        /// </summary>
        private void CalculateSkyLight()
        {
            var queue = new Queue<(int x, int y, float lightLevel, int layer)>();
            var visited = new bool[_world.Width, _world.Height];
            
            // 第一步：找到所有暴露在表面的图块
            for (int x = 0; x < _world.Width; x++)
            {
                for (int y = 0; y < _world.Height; y++)
                {
                    var tile = _world.GetTile(x, y);
                    
                    // 如果是实体方块且暴露在表面（至少有一个相邻位置是空气或世界边界）
                    if (tile.Type != TileType.Air && IsExposedToSurface(x, y))
                    {
                        // 根据深度计算光照强度衰减
                        float initialLight = CalculateDepthBasedLight(x, y);
                        _lightMap[x, y] = Math.Max(_lightMap[x, y], initialLight);
                        
                        // 将此图块加入传播队列，层级为0（表面）
                        queue.Enqueue((x, y, initialLight, 0));
                        visited[x, y] = true;
                    }
                }
            }
            
            // 第二步：从暴露表面向内传播光照
            var directions = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
            
            while (queue.Count > 0)
            {
                var (currentX, currentY, currentLight, currentLayer) = queue.Dequeue();
                
                // 向四个方向传播光照
                foreach (var (dx, dy) in directions)
                {
                    int newX = currentX + dx;
                    int newY = currentY + dy;
                    int newLayer = currentLayer + 1;
                    
                    // 检查边界
                    if (newX < 0 || newX >= _world.Width || newY < 0 || newY >= _world.Height)
                        continue;
                        
                    if (visited[newX, newY])
                        continue;
                        
                    var neighborTile = _world.GetTile(newX, newY);
                    
                    // 第六层及以后完全黑暗
                    if (newLayer >= 6)
                    {
                        _lightMap[newX, newY] = 0f;
                        visited[newX, newY] = true;
                        continue;
                    }
                    
                    // 根据层级计算光照衰减
                    float layerFactor = 1.0f - (newLayer / 6.0f); // 从1.0逐渐衰减到0
                    float newLightLevel;
                    
                    if (neighborTile.Type == TileType.Air)
                    {
                        // 空气中光照衰减较少
                        newLightLevel = currentLight * 0.9f * layerFactor;
                    }
                    else if (IsOpaqueTile(neighborTile.Type))
                    {
                        // 实体方块中光照衰减较多
                        newLightLevel = currentLight * 0.75f * layerFactor;
                    }
                    else
                    {
                        // 半透明方块（如水）
                        newLightLevel = currentLight * 0.85f * layerFactor;
                    }
                    
                    // 如果新的光照强度足够强且比当前值更亮
                    if (newLightLevel > 0.01f && newLightLevel > _lightMap[newX, newY])
                    {
                        _lightMap[newX, newY] = newLightLevel;
                        visited[newX, newY] = true;
                        
                        // 如果还没到第六层，继续传播
                        if (newLayer < 6)
                        {
                            queue.Enqueue((newX, newY, newLightLevel, newLayer));
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 检查图块是否暴露在表面（至少有一个相邻位置是空气或世界边界）
        /// </summary>
        private bool IsExposedToSurface(int x, int y)
        {
            var directions = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
            
            foreach (var (dx, dy) in directions)
            {
                int checkX = x + dx;
                int checkY = y + dy;
                
                // 如果相邻位置是世界边界，认为暴露
                if (checkX < 0 || checkX >= _world.Width || checkY < 0 || checkY >= _world.Height)
                    return true;
                    
                // 如果相邻位置是空气，认为暴露
                var neighborTile = _world.GetTile(checkX, checkY);
                if (neighborTile.Type == TileType.Air)
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 根据深度计算光照强度，实现渐进式衰减
        /// </summary>
        private float CalculateDepthBasedLight(int x, int y)
        {
            if (x < 0 || x >= _world.Width || y < 0)
                return MAX_LIGHT;
                
            int surfaceLevel = _world.SurfaceHeight != null && x < _world.SurfaceHeight.Length ? 
                _world.SurfaceHeight[x] : _world.Height / 3;
            
            // 计算相对于地表的深度
            int depthBelowSurface = y - surfaceLevel;
            
            // 如果在地表或地表以上，保持最大光照
            if (depthBelowSurface <= 0)
                return MAX_LIGHT;
            
            // 定义衰减参数
            int fadeStartDepth = _world.Height / 32; // 开始衰减的深度（更浅）
            int fadeEndDepth = _world.Height / 6;    // 完全变暗的深度
            
            // 如果深度小于开始衰减深度，保持最大光照
            if (depthBelowSurface < fadeStartDepth)
                return MAX_LIGHT;
            
            // 如果深度大于完全变暗深度，使用环境光
            if (depthBelowSurface >= fadeEndDepth)
                return AMBIENT_LIGHT;
            
            // 在衰减区间内，线性插值
            float fadeProgress = (float)(depthBelowSurface - fadeStartDepth) / (fadeEndDepth - fadeStartDepth);
            return MAX_LIGHT * (1.0f - fadeProgress) + AMBIENT_LIGHT * fadeProgress;
        }
        
        /// <summary>
        /// 计算单个光源的光照影响
        /// </summary>
        private void CalculateLightFromSource(LightSource lightSource)
        {
            int lightX = (int)(lightSource.Position.X / 16); // 转换为瓦片坐标
            int lightY = (int)(lightSource.Position.Y / 16);
            int radius = (int)(lightSource.Intensity * 10); // 光照半径
            
            for (int x = Math.Max(0, lightX - radius); x < Math.Min(_world.Width, lightX + radius + 1); x++)
            {
                for (int y = Math.Max(0, lightY - radius); y < Math.Min(_world.Height, lightY + radius + 1); y++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(lightX, lightY));
                    
                    if (distance <= radius)
                    {
                        // 计算光照强度（距离衰减）
                        float intensity = lightSource.Intensity * (1.0f - (distance / radius));
                        intensity = Math.Max(0, Math.Min(MAX_LIGHT, intensity));
                        
                        // 检查光线是否被阻挡
                        if (!IsLightBlocked(lightX, lightY, x, y))
                        {
                            _lightMap[x, y] = Math.Max(_lightMap[x, y], intensity);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 检查光线是否被阻挡
        /// </summary>
        private bool IsLightBlocked(int fromX, int fromY, int toX, int toY)
        {
            // 简单的光线投射检测
            int dx = Math.Abs(toX - fromX);
            int dy = Math.Abs(toY - fromY);
            int x = fromX;
            int y = fromY;
            int n = 1 + dx + dy;
            int x_inc = (toX > fromX) ? 1 : -1;
            int y_inc = (toY > fromY) ? 1 : -1;
            int error = dx - dy;
            
            dx *= 2;
            dy *= 2;
            
            for (; n > 0; --n)
            {
                if (x >= 0 && x < _world.Width && y >= 0 && y < _world.Height)
                {
                    var tile = _world.GetTile(x, y);
                    if (tile.Type != TileType.Air && IsOpaqueTile(tile.Type))
                    {
                        return true; // 光线被阻挡
                    }
                }
                
                if (error > 0)
                {
                    x += x_inc;
                    error -= dy;
                }
                else
                {
                    y += y_inc;
                    error += dx;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 检查瓦片是否不透明（阻挡光线）
        /// </summary>
        private bool IsOpaqueTile(TileType tileType)
        {
            return tileType switch
            {
                TileType.Air => false,
                TileType.Water => false, // 水是半透明的
                _ => true // 其他所有瓦片都阻挡光线
            };
        }
        
        /// <summary>
        /// 当瓦片改变时更新光照
        /// </summary>
        public void OnTileChanged(int x, int y)
        {
            if (_lightingEnabled)
            {
                // 重新计算受影响区域的光照
                RecalculateLighting();
            }
        }
        
        /// <summary>
        /// 获取光源数量
        /// </summary>
        public int GetLightSourceCount()
        {
            return _lightSources?.Count ?? 0;
        }
        
        /// <summary>
        /// 光源数据结构
        /// </summary>
        public class LightSource
        {
            public Vector2 Position { get; set; }
            public float Intensity { get; set; }
            public Color Color { get; set; }
            public bool IsActive { get; set; }
        }
    }
}