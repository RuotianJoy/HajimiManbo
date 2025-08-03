using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace HajimiManbo
{
    public static class UIScaleManager
    {
        // 基准分辨率（设计时使用的分辨率）
        private static readonly int BaseWidth = 1920;
        private static readonly int BaseHeight = 1080;
        
        // 当前屏幕信息
        private static int currentWidth;
        private static int currentHeight;
        private static float scaleX;
        private static float scaleY;
        private static float uniformScale;
        
        public static void Initialize(GraphicsDeviceManager graphics)
        {
            UpdateScale(graphics);
        }
        
        public static void UpdateScale(GraphicsDeviceManager graphics)
        {
            currentWidth = graphics.PreferredBackBufferWidth;
            currentHeight = graphics.PreferredBackBufferHeight;
            
            scaleX = (float)currentWidth / BaseWidth;
            scaleY = (float)currentHeight / BaseHeight;
            
            // 使用较小的缩放比例来保持宽高比
            uniformScale = Math.Min(scaleX, scaleY);
        }
        
        // 获取缩放后的位置
        public static Vector2 ScalePosition(Vector2 basePosition)
        {
            return new Vector2(
                basePosition.X * scaleX,
                basePosition.Y * scaleY
            );
        }
        
        // 获取缩放后的尺寸
        public static Vector2 ScaleSize(Vector2 baseSize)
        {
            return new Vector2(
                baseSize.X * scaleX,
                baseSize.Y * scaleY
            );
        }
        
        // 获取缩放后的矩形
        public static Rectangle ScaleRectangle(Rectangle baseRect)
        {
            return new Rectangle(
                (int)(baseRect.X * scaleX),
                (int)(baseRect.Y * scaleY),
                (int)(baseRect.Width * scaleX),
                (int)(baseRect.Height * scaleY)
            );
        }
        
        // 获取统一缩放后的矩形（保持宽高比）
        public static Rectangle ScaleRectangleUniform(Rectangle baseRect)
        {
            return new Rectangle(
                (int)(baseRect.X * uniformScale),
                (int)(baseRect.Y * uniformScale),
                (int)(baseRect.Width * uniformScale),
                (int)(baseRect.Height * uniformScale)
            );
        }
        
        // 获取缩放后的字体大小
        public static float ScaleFontSize(float baseFontSize)
        {
            return baseFontSize * uniformScale;
        }
        
        // 获取居中位置
        public static Vector2 GetCenterPosition(Vector2 size)
        {
            return new Vector2(
                (currentWidth - size.X) / 2,
                (currentHeight - size.Y) / 2
            );
        }
        
        // 获取相对位置（百分比）
        public static Vector2 GetRelativePosition(float xPercent, float yPercent)
        {
            return new Vector2(
                currentWidth * xPercent,
                currentHeight * yPercent
            );
        }
        
        // 获取相对尺寸（百分比）
        public static Vector2 GetRelativeSize(float widthPercent, float heightPercent)
        {
            return new Vector2(
                currentWidth * widthPercent,
                currentHeight * heightPercent
            );
        }
        
        // 属性
        public static int CurrentWidth => currentWidth;
        public static int CurrentHeight => currentHeight;
        public static float ScaleX => scaleX;
        public static float ScaleY => scaleY;
        public static float UniformScale => uniformScale;
    }
}