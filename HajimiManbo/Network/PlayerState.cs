using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace HajimiManbo.Network
{
    /// <summary>
    /// 玩家状态类，用于网络同步
    /// 按照Terraria模式实现固定槽位管理
    /// </summary>
    public class PlayerState
    {
        public bool Active { get; set; } = false;
        public ushort PlayerId { get; set; }
        public string PlayerName { get; set; } = "";
        public string CharacterName { get; set; } = "";
        
        // 位置和移动状态
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public Vector2 SpawnPosition { get; set; }
        
        // 玩家状态
        public bool IsMoving { get; set; }
        public bool IsSprinting { get; set; }
        public bool IsOnGround { get; set; }
        public bool FacingRight { get; set; } = true; // 角色朝向
        
        // 同步控制
        public DateTime LastUpdateTime { get; set; }
        public Vector2 LastSyncPosition { get; set; }
        public Vector2 LastSyncVelocity { get; set; }
        
        // 同步阈值
        public const float PositionThreshold = 2.0f; // 位置变化阈值
        public const float VelocityThreshold = 10.0f; // 速度变化阈值
        public const float SyncInterval = 1.0f / 20.0f; // 20Hz同步频率
        
        public PlayerState()
        {
            Reset();
        }
        
        /// <summary>
        /// 重置玩家状态（离开时调用）
        /// </summary>
        public void Reset()
        {
            Active = false;
            PlayerId = 0;
            PlayerName = "";
            CharacterName = "";
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            SpawnPosition = Vector2.Zero;
            IsMoving = false;
            IsSprinting = false;
            IsOnGround = false;
            FacingRight = true;
            LastUpdateTime = DateTime.MinValue;
            LastSyncPosition = Vector2.Zero;
            LastSyncVelocity = Vector2.Zero;
        }
        
        /// <summary>
        /// 检查是否需要同步（基于变化量和时间间隔）
        /// </summary>
        public bool ShouldSync()
        {
            if (!Active) return false;
            
            var now = DateTime.UtcNow;
            var timeSinceLastSync = (now - LastUpdateTime).TotalSeconds;
            
            // 强制同步间隔
            if (timeSinceLastSync >= SyncInterval)
                return true;
                
            // 位置变化超过阈值
            var positionDelta = Vector2.Distance(Position, LastSyncPosition);
            if (positionDelta > PositionThreshold)
                return true;
                
            // 速度变化超过阈值
            var velocityDelta = Vector2.Distance(Velocity, LastSyncVelocity);
            if (velocityDelta > VelocityThreshold)
                return true;
                
            return false;
        }
        
        /// <summary>
        /// 标记已同步
        /// </summary>
        public void MarkSynced()
        {
            LastUpdateTime = DateTime.UtcNow;
            LastSyncPosition = Position;
            LastSyncVelocity = Velocity;
        }
        
        /// <summary>
        /// 从另一个PlayerState复制数据
        /// </summary>
        public void CopyFrom(PlayerState other)
        {
            if (other == null) return;
            
            Active = other.Active;
            PlayerId = other.PlayerId;
            PlayerName = other.PlayerName;
            CharacterName = other.CharacterName;
            Position = other.Position;
            Velocity = other.Velocity;
            SpawnPosition = other.SpawnPosition;
            IsMoving = other.IsMoving;
            IsSprinting = other.IsSprinting;
            IsOnGround = other.IsOnGround;
        }
    }
    
    /// <summary>
    /// 玩家状态管理器，实现固定槽位数组
    /// </summary>
    public class PlayerStateManager
    {
        public const int MaxPlayers = 8; // 最大玩家数
        private readonly PlayerState[] _playerStates;
        
        public PlayerStateManager()
        {
            _playerStates = new PlayerState[MaxPlayers];
            for (int i = 0; i < MaxPlayers; i++)
            {
                _playerStates[i] = new PlayerState();
            }
        }
        
        /// <summary>
        /// 获取指定槽位的玩家状态
        /// </summary>
        public PlayerState GetPlayerState(int slot)
        {
            if (slot < 0 || slot >= MaxPlayers)
                return null;
            return _playerStates[slot];
        }
        
        /// <summary>
        /// 分配一个空闲槽位给新玩家
        /// </summary>
        public int AllocateSlot(ushort playerId, string playerName, string characterName)
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (!_playerStates[i].Active)
                {
                    var state = _playerStates[i];
                    state.Active = true;
                    state.PlayerId = playerId;
                    state.PlayerName = playerName;
                    state.CharacterName = characterName;
                    state.LastUpdateTime = DateTime.UtcNow;
                    return i;
                }
            }
            return -1; // 没有空闲槽位
        }
        
        /// <summary>
        /// 释放指定槽位
        /// </summary>
        public void FreeSlot(int slot)
        {
            if (slot >= 0 && slot < MaxPlayers)
            {
                _playerStates[slot].Reset();
            }
        }
        
        /// <summary>
        /// 根据玩家ID查找槽位
        /// </summary>
        public int FindSlotByPlayerId(ushort playerId)
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (_playerStates[i].Active && _playerStates[i].PlayerId == playerId)
                {
                    return i;
                }
            }
            return -1;
        }
        
        /// <summary>
        /// 获取所有活跃的玩家状态
        /// </summary>
        public PlayerState[] GetActivePlayers()
        {
            var activePlayers = new List<PlayerState>();
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (_playerStates[i].Active)
                {
                    activePlayers.Add(_playerStates[i]);
                }
            }
            return activePlayers.ToArray();
        }
        
        /// <summary>
        /// 获取需要同步的玩家状态
        /// </summary>
        public List<(int slot, PlayerState state)> GetPlayersNeedingSync()
        {
            var needSync = new List<(int, PlayerState)>();
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (_playerStates[i].Active && _playerStates[i].ShouldSync())
                {
                    needSync.Add((i, _playerStates[i]));
                }
            }
            return needSync;
        }
        
        /// <summary>
        /// 清空所有槽位
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                _playerStates[i].Reset();
            }
        }
    }
}