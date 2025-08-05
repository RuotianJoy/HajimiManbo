using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace HajimiManbo.World
{
    /// <summary>
    /// 传送门类 - 固定ID的传送门系统
    /// </summary>
    public class Portal
    {
        private static readonly Dictionary<int, Portal> _allPortals = new Dictionary<int, Portal>();
        
        /// <summary>
        /// 传送门的唯一ID（固定：1=地表洞穴传送门，2=空岛传送门）
        /// </summary>
        public int Id { get; private set; }
        
        /// <summary>
        /// 传送门在世界中的位置
        /// </summary>
        public Vector2 Position { get; set; }
        
        /// <summary>
        /// 传送门的名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 传送门是否激活
        /// </summary>
        public bool IsActive { get; set; }
        
        /// <summary>
        /// 传送门的创建时间
        /// </summary>
        public DateTime CreatedTime { get; private set; }
        
        /// <summary>
        /// 私有构造函数
        /// </summary>
        private Portal(int id, Vector2 position, string name)
        {
            Id = id;
            Position = position;
            Name = name;
            IsActive = true;
            CreatedTime = DateTime.Now;
            
            // 将传送门添加到全局字典中
            _allPortals[Id] = this;
        }
        
        /// <summary>
        /// 创建地表洞穴传送门（ID=1）
        /// </summary>
        /// <param name="position">传送门位置</param>
        /// <returns>地表洞穴传送门</returns>
        public static Portal CreateCavePortal(Vector2 position)
        {
            if (_allPortals.ContainsKey(1))
            {
                _allPortals[1].Position = position;
                return _allPortals[1];
            }
            return new Portal(1, position, "地表洞穴传送门");
        }
        
        /// <summary>
        /// 创建空岛传送门（ID=2）
        /// </summary>
        /// <param name="position">传送门位置</param>
        /// <returns>空岛传送门</returns>
        public static Portal CreateIslandPortal(Vector2 position)
        {
            if (_allPortals.ContainsKey(2))
            {
                _allPortals[2].Position = position;
                return _allPortals[2];
            }
            return new Portal(2, position, "空岛传送门");
        }
        
        /// <summary>
        /// 创建沙漠传送门（ID=3）
        /// </summary>
        /// <param name="position">传送门位置</param>
        /// <returns>沙漠传送门</returns>
        public static Portal CreateDesertPortal(Vector2 position)
        {
            if (_allPortals.ContainsKey(3))
            {
                _allPortals[3].Position = position;
                return _allPortals[3];
            }
            return new Portal(3, position, "沙漠传送门");
        }
        
        /// <summary>
        /// 根据ID获取传送门
        /// </summary>
        /// <param name="id">传送门ID</param>
        /// <returns>传送门实例，如果不存在则返回null</returns>
        public static Portal GetPortalById(int id)
        {
            _allPortals.TryGetValue(id, out Portal portal);
            return portal;
        }
        
        /// <summary>
        /// 获取所有传送门
        /// </summary>
        /// <returns>所有传送门的字典</returns>
        public static Dictionary<int, Portal> GetAllPortals()
        {
            return new Dictionary<int, Portal>(_allPortals);
        }
        
        /// <summary>
        /// 检查传送门是否可以使用
        /// </summary>
        /// <returns>是否可以传送</returns>
        public bool CanTeleport()
        {
            return IsActive;
        }
        
        /// <summary>
        /// 获取传送门信息字符串
        /// </summary>
        /// <returns>传送门信息</returns>
        public override string ToString()
        {
            var status = IsActive ? "激活" : "未激活";
            return $"传送门 [ID: {Id}] {Name} - 位置: ({Position.X}, {Position.Y}) - 状态: {status}";
        }
        
        /// <summary>
        /// 清除所有传送门（用于重置世界）
        /// </summary>
        public static void ClearAllPortals()
        {
            _allPortals.Clear();
        }
    }
}