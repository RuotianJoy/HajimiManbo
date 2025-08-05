using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HajimiManbo.World
{
    /// <summary>
    /// 传送门渲染器 - 负责传送门的瓦片块渲染
    /// </summary>
    public class PortalRenderer
    {
        private ChunkManager chunkManager;
        private Texture2D purplePixelTexture;
        
        // 传送门结构尺寸
        private const int PORTAL_WIDTH = 8;   // 底部宽度
        private const int PORTAL_HEIGHT = 12; // 总高度
        
        public PortalRenderer(ChunkManager chunkManager, GraphicsDevice graphicsDevice)
        {
            this.chunkManager = chunkManager;
            
            // 创建紫色像素纹理
            purplePixelTexture = new Texture2D(graphicsDevice, 1, 1);
            purplePixelTexture.SetData(new[] { Color.Purple });
        }
        
        /// <summary>
        /// 找到空岛的地表位置
        /// </summary>
        /// <param name="world">世界对象</param>
        /// <param name="centerX">搜索中心X坐标</param>
        /// <param name="centerY">搜索中心Y坐标</param>
        /// <returns>地表Y坐标，如果找不到返回-1</returns>
        private int FindIslandSurface(World world, int centerX, int centerY)
        {
            // 从传送门位置向上和向下搜索，找到空岛的地表
            // 地表定义为：上方是空气，当前位置是固体方块
            
            // 先向上搜索
            for (int y = centerY; y >= Math.Max(0, centerY - 50); y--)
            {
                if (world.IsValidCoordinate(centerX, y) && world.IsValidCoordinate(centerX, y - 1))
                {
                    var currentTile = world.GetTile(centerX, y);
                    var aboveTile = world.GetTile(centerX, y - 1);
                    
                    // 如果当前是固体方块，上方是空气，这就是地表
                    if (currentTile.IsSolid && aboveTile.Type == TileType.Air)
                    {
                        return y;
                    }
                }
            }
            
            // 再向下搜索
            for (int y = centerY + 1; y <= Math.Min(world.Height - 1, centerY + 50); y++)
            {
                if (world.IsValidCoordinate(centerX, y) && world.IsValidCoordinate(centerX, y - 1))
                {
                    var currentTile = world.GetTile(centerX, y);
                    var aboveTile = world.GetTile(centerX, y - 1);
                    
                    // 如果当前是固体方块，上方是空气，这就是地表
                    if (currentTile.IsSolid && aboveTile.Type == TileType.Air)
                    {
                        return y;
                    }
                }
            }
            
            return -1; // 找不到地表
        }
        
        /// <summary>
        /// 构建传送门的瓦片结构
        /// </summary>
        /// <param name="world">世界对象</param>
        /// <param name="centerX">传送门中心X坐标</param>
        /// <param name="centerY">传送门中心Y坐标</param>
        public void BuildPortalStructure(World world, int centerX, int centerY)
        {
            // 找到空岛的地表位置
            int surfaceY = FindIslandSurface(world, centerX, centerY);
            if (surfaceY == -1)
            {
                // 如果找不到地表，使用传送门位置
                surfaceY = centerY;
            }
            
            // 计算起始位置（传送门底部应该在地表上）
            int startX = centerX - PORTAL_WIDTH / 2;
            int baseY = surfaceY; // 地表位置作为底部基准
            
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
            
            // 左侧柱子：8个大理石块（从第2层开始往上）
            for (int y = 1; y <= 8; y++)
            {
                world.SetTile(startX, baseY - y, new Tile(TileType.Marble));
            }
            
            // 右侧柱子：8个大理石块（从第2层开始往上）
            for (int y = 1; y <= 8; y++)
            {
                world.SetTile(startX + 7, baseY - y, new Tile(TileType.Marble));
            }
            
            // 顶部大理石封顶（第9层）
            for (int x = 0; x < 8; x++)
            {
                world.SetTile(startX + x, baseY - 9, new Tile(TileType.Marble));
            }
            
            // 最顶层：4个石块（中间）
            for (int x = 2; x < 6; x++)
            {
                world.SetTile(startX + x, baseY - 10, new Tile(TileType.Stone));
            }
        }
        
        /// <summary>
        /// 渲染传送门的紫色内部区域 - 使用固定悬空位置
        /// </summary>
        /// <param name="spriteBatch">精灵批次</param>
        /// <param name="worldPosition">世界坐标位置</param>
        /// <param name="portalId">传送门ID（用于显示标识）</param>
        /// <param name="tileSize">瓦片大小</param>
        /// <param name="camera">摄像机变换</param>
        /// <param name="world">世界对象</param>
        public void RenderPortal(SpriteBatch spriteBatch, Vector2 worldPosition, int portalId, int tileSize, Matrix camera, World world)
        {
            // 直接使用传入的世界位置，不再查找地表
            int centerX = (int)worldPosition.X;
            int centerY = (int)worldPosition.Y;
            
            // 计算传送门结构的底部位置（直接使用传入位置）
            int structureStartX = centerX - PORTAL_WIDTH / 2;
            int baseY = centerY; // 直接使用传入的Y坐标作为底部基准
            
            // 传送门中间区域现在由WorldGenerator中的雪块填充，不再需要紫色像素渲染
            
            // 渲染传送门ID标识（在传送门中心位置）
            Vector2 centerWorldPos = new Vector2(centerX, baseY - 5); // 传送门中心高度
            Vector2 centerScreenPos = Vector2.Transform(centerWorldPos * tileSize, camera);
            RenderPortalId(spriteBatch, centerScreenPos, portalId, tileSize);
        }
        
        /// <summary>
        /// 渲染传送门ID标识
        /// </summary>
        /// <param name="spriteBatch">精灵批次</param>
        /// <param name="position">位置</param>
        /// <param name="portalId">传送门ID</param>
        /// <param name="tileSize">瓦片大小</param>
        private void RenderPortalId(SpriteBatch spriteBatch, Vector2 position, int portalId, int tileSize)
        {
            // 创建一个简单的数字标识背景
            var idBackgroundRect = new Rectangle(
                (int)position.X + tileSize / 4,
                (int)position.Y + tileSize / 4,
                tileSize / 2,
                tileSize / 2
            );
            
            // 渲染背景（使用纯色纹理或创建一个简单的矩形）
            // 这里需要一个1x1的白色纹理来绘制背景
            // spriteBatch.Draw(whitePixelTexture, idBackgroundRect, Color.Black * 0.7f);
            
            // 渲染ID数字
            // 这里需要字体来渲染文字
            // spriteBatch.DrawString(font, portalId.ToString(), 
            //     new Vector2(idBackgroundRect.X + 4, idBackgroundRect.Y + 4), Color.White);
        }
        

        
        /// <summary>
        /// 获取传送门渲染尺寸
        /// </summary>
        /// <returns>渲染尺寸</returns>
        public Vector2 GetPortalRenderSize()
        {
            return new Vector2(PORTAL_WIDTH * 16, PORTAL_HEIGHT * 16); // 假设瓦片大小为16
        }
        
        /// <summary>
        /// 检查传送门是否在屏幕可见范围内
        /// </summary>
        /// <param name="worldPosition">世界位置</param>
        /// <param name="tileSize">瓦片大小</param>
        /// <param name="camera">摄像机变换</param>
        /// <param name="screenBounds">屏幕边界</param>
        /// <returns>是否可见</returns>
        public bool IsPortalVisible(Vector2 worldPosition, int tileSize, Matrix camera, Rectangle screenBounds)
        {
            Vector2 screenPosition = Vector2.Transform(worldPosition * tileSize, camera);
            
            Rectangle portalBounds = new Rectangle(
                (int)screenPosition.X - (PORTAL_WIDTH * tileSize) / 2,
                (int)screenPosition.Y - (PORTAL_HEIGHT * tileSize) / 2,
                PORTAL_WIDTH * tileSize,
                PORTAL_HEIGHT * tileSize
            );
            
            return screenBounds.Intersects(portalBounds);
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            purplePixelTexture?.Dispose();
        }
    }
}