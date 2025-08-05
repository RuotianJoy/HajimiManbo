using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace HajimiManbo.World
{
    /// <summary>
    /// 传送门管理器 - 负责生成固定的两个传送门
    /// </summary>
    public class PortalManager
    {
        private World world;
        private Random random;
        
        /// <summary>
        /// 传送门生成事件
        /// </summary>
        public event Action<Portal> OnPortalCreated;
        
        public PortalManager(World world, int seed = 0)
        {
            this.world = world;
            this.random = new Random(seed);
        }
        
        /// <summary>
        /// 生成地表洞穴传送门（ID=1）
        /// </summary>
        /// <returns>地表洞穴传送门</returns>
        public Portal GenerateCavePortal()
        {
            // 寻找地表洞穴位置
            var cavePosition = FindSurfaceCavePosition();
            if (cavePosition == Vector2.Zero)
            {
                Console.WriteLine("[传送门管理器] 未找到合适的地表洞穴位置");
                return null;
            }
            
            // 创建地表洞穴传送门
            var portal = Portal.CreateCavePortal(cavePosition);
            
            // 在世界中放置传送门方块
            PlacePortalInWorld((int)cavePosition.X, (int)cavePosition.Y, portal.Id);
            
            Console.WriteLine($"[传送门管理器] 在位置 ({cavePosition.X}, {cavePosition.Y}) 创建地表洞穴传送门，ID: {portal.Id}");
            
            // 触发事件
            OnPortalCreated?.Invoke(portal);
            
            return portal;
        }
        
        /// <summary>
        /// 生成空岛传送门（ID=2）
        /// </summary>
        /// <returns>空岛传送门</returns>
        public Portal GenerateIslandPortal()
        {
            // 寻找空岛位置
            var islandPosition = FindFloatingIslandPosition();
            if (islandPosition == Vector2.Zero)
            {
                Console.WriteLine("[传送门管理器] 未找到合适的空岛位置");
                return null;
            }
            
            // 创建空岛传送门
            var portal = Portal.CreateIslandPortal(islandPosition);
            
            // 在世界中放置传送门方块
            PlacePortalInWorld((int)islandPosition.X, (int)islandPosition.Y, portal.Id);
            
            Console.WriteLine($"[传送门管理器] 在位置 ({islandPosition.X}, {islandPosition.Y}) 创建空岛传送门，ID: {portal.Id}");
            
            // 触发事件
            OnPortalCreated?.Invoke(portal);
            
            return portal;
        }
        
        /// <summary>
        /// 生成所有传送门
        /// </summary>
        public void GenerateAllPortals()
        {
            GenerateCavePortal();
            GenerateIslandPortal();
        }
        
        /// <summary>
        /// 寻找地表洞穴位置 - 固定悬空位置，不受地形影响
        /// </summary>
        /// <returns>洞穴位置，如果未找到返回Vector2.Zero</returns>
        private Vector2 FindSurfaceCavePosition()
        {
            // 在世界左侧1/3处生成固定悬空传送门
            int fixedX = world.Width / 3;
            int fixedY = 100; // 固定高度，确保悬空
            
            // 确保位置在世界范围内
            if (fixedX >= 0 && fixedX < world.Width && fixedY >= 0 && fixedY < world.Height)
            {
                Console.WriteLine($"[传送门管理器] 地表洞穴传送门固定位置: ({fixedX}, {fixedY})");
                return new Vector2(fixedX, fixedY);
            }
            
            return Vector2.Zero;
        }
        
        /// <summary>
        /// 寻找空岛位置 - 固定悬空位置，不受地形影响
        /// </summary>
        /// <returns>空岛位置，如果未找到返回Vector2.Zero</returns>
        private Vector2 FindFloatingIslandPosition()
        {
            // 在世界右侧2/3处生成固定悬空传送门
            int fixedX = world.Width * 2 / 3;
            int fixedY = 80; // 固定高度，确保悬空且比地表传送门稍高
            
            // 确保位置在世界范围内
            if (fixedX >= 0 && fixedX < world.Width && fixedY >= 0 && fixedY < world.Height)
            {
                Console.WriteLine($"[传送门管理器] 空岛传送门固定位置: ({fixedX}, {fixedY})");
                return new Vector2(fixedX, fixedY);
            }
            
            return Vector2.Zero;
        }
        
        /// <summary>
        /// 检查指定区域是否有足够空间
        /// </summary>
        /// <param name="x">起始X坐标</param>
        /// <param name="y">起始Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>是否有足够空间</returns>
        private bool HasEnoughSpace(int x, int y, int width, int height)
        {
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    int checkX = x + dx;
                    int checkY = y + dy;
                    
                    if (checkX < 0 || checkX >= world.Width || 
                        checkY < 0 || checkY >= world.Height)
                        return false;
                    
                    var tile = world.GetTile(checkX, checkY);
                    if (tile.Type != TileType.Air)
                        return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 在世界中放置传送门方块
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="portalId">传送门ID</param>
        private void PlacePortalInWorld(int x, int y, int portalId)
        {
            var tile = world.GetTile(x, y);
            tile.Type = TileType.Portal;
            tile.Style = (byte)(portalId % 15); // 使用传送门ID作为样式变体
            world.SetTile(x, y, tile);
        }
        
        /// <summary>
        /// 找到指定X坐标的地表高度
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <returns>地表Y坐标</returns>
        private int FindSurfaceLevel(int x)
        {
            for (int y = 0; y < world.Height; y++)
            {
                var tile = world.GetTile(x, y);
                if (tile.IsSolid)
                {
                    return Math.Max(0, y - 1); // 返回固体方块上方的空气位置
                }
            }
            return world.Height - 1;
        }
        

        
        /// <summary>
        /// 获取指定位置的传送门
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>传送门对象，如果不存在则返回null</returns>
        public Portal GetPortalAt(int x, int y)
        {
            var tile = world.GetTile(x, y);
            if (tile.Type != TileType.Portal)
                return null;
            
            var position = new Vector2(x, y);
            return Portal.GetAllPortals().Values
                .FirstOrDefault(p => p.Position == position);
        }
        
        /// <summary>
        /// 清理所有传送门
        /// </summary>
        public void ClearAllPortals()
        {
            Console.WriteLine("[传送门管理器] 清理所有传送门");
            Portal.ClearAllPortals();
        }
    }
}