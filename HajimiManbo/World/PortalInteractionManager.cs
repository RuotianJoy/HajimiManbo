using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using HajimiManbo.Network;
using HajimiManbo.Gameplay;

namespace HajimiManbo.World
{
    /// <summary>
    /// 传送门交互管理器 - 处理玩家与传送门的交互逻辑
    /// </summary>
    public class PortalInteractionManager
    {
        private World world;
        private PortalManager portalManager;
        private SpriteFont font;
        private Texture2D pixelTexture;
        
        // 交互检测范围（像素）
        private const float INTERACTION_RANGE = 500f;
        
        // 当前可交互的传送门
        private Portal currentInteractablePortal = null;
        
        // 键盘状态
        private KeyboardState previousKeyboardState;
        
        public PortalInteractionManager(World world, PortalManager portalManager, SpriteFont font, GraphicsDevice graphicsDevice)
        {
            this.world = world;
            this.portalManager = portalManager;
            this.font = font;
            
            // 创建1x1白色像素纹理用于绘制UI背景
            pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
            
            previousKeyboardState = Keyboard.GetState();
        }
        
        /// <summary>
        /// 更新传送门交互逻辑
        /// </summary>
        /// <param name="gameTime">游戏时间</param>
        /// <param name="localPlayer">本地玩家</param>
        /// <param name="networkPlayers">网络玩家字典</param>
        public void Update(GameTime gameTime, Player localPlayer, Dictionary<int, Player> networkPlayers)
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();
            
            // 调试输出：显示更新调用
            if (gameTime.TotalGameTime.TotalSeconds % 2 < 0.016) // 每2秒输出一次
            {
                Console.WriteLine($"[传送门交互] Update调用，玩家位置: ({localPlayer.Position.X:F1}, {localPlayer.Position.Y:F1})");
            }
            
            // 检查玩家是否在传送门附近
            currentInteractablePortal = GetNearestPortal(localPlayer.Position);
            
            // 如果有可交互的传送门且按下F键
            if (currentInteractablePortal != null && 
                currentKeyboardState.IsKeyDown(Keys.F) && 
                !previousKeyboardState.IsKeyDown(Keys.F))
            {
                Console.WriteLine($"[传送门交互] 尝试与传送门 ID:{currentInteractablePortal.Id} 交互");
                TryInteractWithPortal(localPlayer, networkPlayers);
            }
            
            previousKeyboardState = currentKeyboardState;
        }
        
        /// <summary>
        /// 获取玩家附近最近的传送门
        /// </summary>
        /// <param name="playerPosition">玩家位置</param>
        /// <returns>最近的传送门，如果没有则返回null</returns>
        private Portal GetNearestPortal(Vector2 playerPosition)
        {
            var allPortals = Portal.GetAllPortals();
            Portal nearestPortal = null;
            float nearestDistance = float.MaxValue;
            
            // 调试输出：显示传送门数量和玩家位置
            Console.WriteLine($"[传送门交互] 检测传送门，玩家位置: ({playerPosition.X:F1}, {playerPosition.Y:F1}), 传送门数量: {allPortals.Count}");
            
            foreach (var portal in allPortals.Values)
            {
                if (!portal.IsActive) continue;
                
                // 计算玩家与传送门的距离
                Vector2 portalPixelPos = portal.Position * 16; // 转换为像素坐标
                float distance = Vector2.Distance(playerPosition, portalPixelPos);
                
                // 调试输出：显示每个传送门的信息
                Console.WriteLine($"[传送门交互] 传送门 ID:{portal.Id}, 位置:({portal.Position.X}, {portal.Position.Y}), 像素位置:({portalPixelPos.X}, {portalPixelPos.Y}), 距离:{distance:F1}, 范围:{INTERACTION_RANGE}");
                
                if (distance <= INTERACTION_RANGE && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPortal = portal;
                    Console.WriteLine($"[传送门交互] 找到可交互传送门 ID:{portal.Id}, 距离:{distance:F1}");
                }
            }
            
            if (nearestPortal == null)
            {
                Console.WriteLine("[传送门交互] 没有找到可交互的传送门");
            }
            
            return nearestPortal;
        }
        
        /// <summary>
        /// 检查所有玩家是否都在传送门附近
        /// </summary>
        /// <param name="portal">传送门</param>
        /// <param name="localPlayer">本地玩家</param>
        /// <param name="networkPlayers">网络玩家字典</param>
        /// <returns>是否所有玩家都在附近</returns>
        private bool AreAllPlayersNearPortal(Portal portal, Player localPlayer, Dictionary<int, Player> networkPlayers)
        {
            Vector2 portalPixelPosition = portal.Position * 16;
            
            // 检查本地玩家
            if (Vector2.Distance(localPlayer.Position, portalPixelPosition) > INTERACTION_RANGE)
            {
                return false;
            }
            
            // 检查所有网络玩家
            var networkManager = NetworkManager.Instance;
            var playerStateManager = networkManager.GetPlayerStateManager();
            
            for (int slot = 0; slot < 8; slot++)
            {
                var playerState = playerStateManager.GetPlayerState(slot);
                if (playerState != null && playerState.Active)
                {
                    // 跳过本地玩家（已经检查过）
                    if (slot == networkManager.GetLocalPlayerSlot())
                        continue;
                    
                    // 检查网络玩家是否在范围内
                    if (Vector2.Distance(playerState.Position, portalPixelPosition) > INTERACTION_RANGE)
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 尝试与传送门交互
        /// </summary>
        /// <param name="localPlayer">本地玩家</param>
        /// <param name="networkPlayers">网络玩家字典</param>
        private void TryInteractWithPortal(Player localPlayer, Dictionary<int, Player> networkPlayers)
        {
            var networkManager = NetworkManager.Instance;
            
            // 只有房主可以进行交互
            if (!networkManager.IsServer)
            {
                Console.WriteLine("[传送门交互] 只有房主可以使用传送门");
                return;
            }
            
            // 检查所有玩家是否都在传送门附近
            if (!AreAllPlayersNearPortal(currentInteractablePortal, localPlayer, networkPlayers))
            {
                Console.WriteLine("[传送门交互] 所有玩家必须都在传送门附近才能使用");
                return;
            }
            
            // 执行传送门交互
            ExecutePortalInteraction(currentInteractablePortal);
        }
        
        /// <summary>
        /// 执行传送门交互逻辑
        /// </summary>
        /// <param name="portal">传送门</param>
        private void ExecutePortalInteraction(Portal portal)
        {
            Console.WriteLine($"[传送门交互] 传送门交互功能已禁用 - ID: {portal.Id}, 名称: {portal.Name}");
            // 传送门跳转世界功能已被移除
        }
        

        
        /// <summary>
        /// 渲染传送门交互UI
        /// </summary>
        /// <param name="spriteBatch">精灵批次</param>
        /// <param name="camera">摄像机矩阵</param>
        /// <param name="localPlayer">本地玩家</param>
        /// <param name="networkPlayers">网络玩家字典</param>
        public void Render(SpriteBatch spriteBatch, Matrix camera, Player localPlayer, Dictionary<int, Player> networkPlayers)
        {
            if (currentInteractablePortal == null) return;
            
            // 检查是否所有玩家都在附近
            bool allPlayersNear = AreAllPlayersNearPortal(currentInteractablePortal, localPlayer, networkPlayers);
            
            // 计算传送门在屏幕上的位置
            Vector2 portalWorldPos = currentInteractablePortal.Position * 16;
            Vector2 portalScreenPos = Vector2.Transform(portalWorldPos, camera);
            
            // 渲染交互提示
            RenderInteractionPrompt(spriteBatch, portalScreenPos, allPlayersNear);
        }
        
        /// <summary>
        /// 渲染交互提示
        /// </summary>
        /// <param name="spriteBatch">精灵批次</param>
        /// <param name="position">屏幕位置</param>
        /// <param name="allPlayersNear">是否所有玩家都在附近</param>
        private void RenderInteractionPrompt(SpriteBatch spriteBatch, Vector2 position, bool allPlayersNear)
        {
            var networkManager = NetworkManager.Instance;
            
            string promptText;
            Color textColor;
            Color backgroundColor;
            
            if (!networkManager.IsServer)
            {
                promptText = "只有房主可以使用传送门";
                textColor = Color.White;
                backgroundColor = Color.Red * 0.9f;
            }
            else if (!allPlayersNear)
            {
                promptText = "等待所有玩家靠近传送门";
                textColor = Color.Black;
                backgroundColor = Color.Yellow * 0.9f;
            }
            else
            {
                promptText = "按 F 键使用传送门";
                textColor = Color.White;
                backgroundColor = Color.Green * 0.9f;
            }
            
            // 测量文本大小
            Vector2 textSize = font.MeasureString(promptText);
            
            // 计算提示框位置（传送门上方，确保在屏幕内）
            Vector2 promptPosition = new Vector2(
                Math.Max(10, Math.Min(position.X - textSize.X / 2, 1920 - textSize.X - 10)),
                Math.Max(10, position.Y - 80) // 传送门上方80像素，确保不超出屏幕顶部
            );
            
            // 绘制背景和边框
            Rectangle backgroundRect = new Rectangle(
                (int)(promptPosition.X - 10),
                (int)(promptPosition.Y - 5),
                (int)(textSize.X + 20),
                (int)(textSize.Y + 10)
            );
            
            // 绘制边框（白色）
            Rectangle borderRect = new Rectangle(
                backgroundRect.X - 2,
                backgroundRect.Y - 2,
                backgroundRect.Width + 4,
                backgroundRect.Height + 4
            );
            spriteBatch.Draw(pixelTexture, borderRect, Color.White);
            
            // 绘制背景
            spriteBatch.Draw(pixelTexture, backgroundRect, backgroundColor);
            
            // 绘制文本
            spriteBatch.DrawString(font, promptText, promptPosition, textColor);
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            pixelTexture?.Dispose();
        }
    }
}