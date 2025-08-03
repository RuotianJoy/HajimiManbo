using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace HajimiManbo
{
    /// <summary>
    /// 输入管理器，负责管理自定义按键配置
    /// </summary>
    public static class InputManager
    {
        // 默认按键配置
        private static readonly Dictionary<string, Keys> _defaultKeyMap = new()
        {
            { "左移", Keys.A },
            { "右移", Keys.D },
            { "冲刺", Keys.LeftShift },
            { "跳跃", Keys.Space },
            { "确认", Keys.Enter },
            { "取消", Keys.Escape }
        };

        // 当前按键配置
        private static Dictionary<string, Keys> _keyMap = new(_defaultKeyMap);

        /// <summary>
        /// 获取指定动作的按键
        /// </summary>
        /// <param name="action">动作名称</param>
        /// <returns>对应的按键</returns>
        public static Keys GetKey(string action)
        {
            return _keyMap.TryGetValue(action, out Keys key) ? key : Keys.None;
        }

        /// <summary>
        /// 设置指定动作的按键
        /// </summary>
        /// <param name="action">动作名称</param>
        /// <param name="key">新的按键</param>
        public static void SetKey(string action, Keys key)
        {
            if (_keyMap.ContainsKey(action))
            {
                _keyMap[action] = key;
            }
        }

        /// <summary>
        /// 获取所有按键配置的副本
        /// </summary>
        /// <returns>按键配置字典</returns>
        public static Dictionary<string, Keys> GetAllKeys()
        {
            return new Dictionary<string, Keys>(_keyMap);
        }

        /// <summary>
        /// 批量更新按键配置
        /// </summary>
        /// <param name="newKeyMap">新的按键配置</param>
        public static void UpdateKeys(Dictionary<string, Keys> newKeyMap)
        {
            foreach (var kvp in newKeyMap)
            {
                if (_keyMap.ContainsKey(kvp.Key))
                {
                    _keyMap[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// 重置为默认按键配置
        /// </summary>
        public static void ResetToDefault()
        {
            _keyMap = new Dictionary<string, Keys>(_defaultKeyMap);
        }

        /// <summary>
        /// 检查按键是否被按下
        /// </summary>
        /// <param name="action">动作名称</param>
        /// <param name="keyboardState">键盘状态</param>
        /// <returns>是否被按下</returns>
        public static bool IsKeyDown(string action, KeyboardState keyboardState)
        {
            Keys key = GetKey(action);
            return key != Keys.None && keyboardState.IsKeyDown(key);
        }

        /// <summary>
        /// 检查按键是否刚被按下（单次触发）
        /// </summary>
        /// <param name="action">动作名称</param>
        /// <param name="currentState">当前键盘状态</param>
        /// <param name="previousState">上一帧键盘状态</param>
        /// <returns>是否刚被按下</returns>
        public static bool IsKeyPressed(string action, KeyboardState currentState, KeyboardState previousState)
        {
            Keys key = GetKey(action);
            return key != Keys.None && currentState.IsKeyDown(key) && !previousState.IsKeyDown(key);
        }
    }
}