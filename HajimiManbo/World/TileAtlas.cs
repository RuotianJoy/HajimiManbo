using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HajimiManbo.World
{
    /// <summary>
    /// 瓦片图集工具类 - 泰拉瑞亚标准47帧连接系统
    /// 纹理尺寸：288*270像素，15行16列，每个格子18*18像素
    /// 从每个18*18格子中提取中间的16*16像素
    /// </summary>
    public static class TileAtlas
    {
        private const int GRID = 18;   // 每个格子18px
        private const int TILE = 16;   // 实际瓦片尺寸16px
        private const int COLS_PER_ROW = 16; // 每行16列
        private const int ROWS_TOTAL = 15;    // 总共15行
        private const int MAX_FRAMES = 47;    // 泰拉瑞亚标准47帧
        
        /// <summary>
        /// 获取指定帧的源矩形（泰拉瑞亚标准）
        /// 从18*18格子中提取中间的16*16像素
        /// </summary>
        /// <param name="style">未使用，保持兼容性</param>
        /// <param name="frame">帧索引 (0-46)</param>
        /// <param name="tex">纹理对象</param>
        /// <returns>源矩形</returns>
        public static Rectangle GetSourceRect(byte style, byte frame, Texture2D tex)
        {
            // 限制帧范围
            frame = (byte)(frame % MAX_FRAMES);
            
            // 计算行列位置：47帧按16列排列
            int col = frame % COLS_PER_ROW;
            int row = frame / COLS_PER_ROW;
            
            // 计算18*18格子的起始位置
            int gridX = col * GRID;
            int gridY = row * GRID;
            
            // 从18*18格子中提取中间的16*16像素
            // 居中偏移：(18-16)/2 = 1像素
            int x = gridX + 1;
            int y = gridY + 1;
            
            return new Rectangle(x, y, TILE, TILE);
        }
        
        /// <summary>
        /// 计算UV坐标
        /// </summary>
        /// <param name="style">材质风格 (0-14)</param>
        /// <param name="frame">帧索引 (0-46)</param>
        /// <param name="tex">纹理对象</param>
        /// <returns>UV坐标 (u0, v0, u1, v1)</returns>
        public static (float u0, float v0, float u1, float v1) GetUVCoords(byte style, byte frame, Texture2D tex)
        {
            var src = GetSourceRect(style, frame, tex);
            
            // 添加小的偏移量避免纹理采样边缘问题
            const float epsilon = 1.5f; // 1.5px 内缩防缝隙，避免浮点精度问题
            
            float u0 = (src.X + epsilon) / (float)tex.Width;
            float v0 = (src.Y + epsilon) / (float)tex.Height;
            float u1 = (src.X + src.Width - epsilon) / (float)tex.Width;
            float v1 = (src.Y + src.Height - epsilon) / (float)tex.Height;
            
            return (u0, v0, u1, v1);
        }
    }
}