using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace HajimiManbo.Gameplay
{
    /// <summary>
    /// 2D摄像机类，用于处理视图矩阵、跟随玩家、边界限制等功能
    /// </summary>
    public class Camera2D
    {
        private Vector2 _position;
        private float _zoom;
        private float _rotation;
        private Viewport _viewport;
        private Rectangle _worldBounds;
        
        public Vector2 Position
        {
            get => _position;
            set => _position = value;
        }
        
        public float Zoom
        {
            get => _zoom;
            set => _zoom = MathHelper.Clamp(value, 0.1f, 3.0f);
        }
        
        public float Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }
        
        public Rectangle WorldBounds
        {
            get => _worldBounds;
            set => _worldBounds = value;
        }
        
        public Camera2D(Viewport viewport)
        {
            _viewport = viewport;
            _zoom = 1.5f;  // 增加默认缩放级别，使视口更近
            _rotation = 0.0f;
            _position = Vector2.Zero;
            _worldBounds = Rectangle.Empty;
        }
        
        /// <summary>
        /// 让摄像机跟随目标位置
        /// </summary>
        /// <param name="targetPosition">目标位置</param>
        public void Follow(Vector2 targetPosition)
        {
            _position = targetPosition;
            
            // 如果设置了世界边界，限制摄像机不超出边界
            if (_worldBounds != Rectangle.Empty)
            {
                ClampToWorldBounds();
            }
        }
        
        /// <summary>
        /// 平滑跟随目标位置
        /// </summary>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="lerpAmount">插值量(0-1)</param>
        public void SmoothFollow(Vector2 targetPosition, float lerpAmount)
        {
            _position = Vector2.Lerp(_position, targetPosition, lerpAmount);
            
            if (_worldBounds != Rectangle.Empty)
            {
                ClampToWorldBounds();
            }
        }
        
        /// <summary>
        /// 限制摄像机在世界边界内
        /// </summary>
        private void ClampToWorldBounds()
        {
            Vector2 cameraWorldMin = Vector2.Transform(Vector2.Zero, Matrix.Invert(GetViewMatrix()));
            Vector2 cameraWorldMax = Vector2.Transform(new Vector2(_viewport.Width, _viewport.Height), Matrix.Invert(GetViewMatrix()));
            
            Vector2 adjustedPosition = _position;
            
            if (cameraWorldMin.X < _worldBounds.Left)
                adjustedPosition.X = _position.X + (_worldBounds.Left - cameraWorldMin.X);
            if (cameraWorldMax.X > _worldBounds.Right)
                adjustedPosition.X = _position.X + (_worldBounds.Right - cameraWorldMax.X);
            if (cameraWorldMin.Y < _worldBounds.Top)
                adjustedPosition.Y = _position.Y + (_worldBounds.Top - cameraWorldMin.Y);
            if (cameraWorldMax.Y > _worldBounds.Bottom)
                adjustedPosition.Y = _position.Y + (_worldBounds.Bottom - cameraWorldMax.Y);
            
            _position = adjustedPosition;
        }
        
        /// <summary>
        /// 获取视图矩阵
        /// </summary>
        /// <returns>视图变换矩阵</returns>
        public Matrix GetViewMatrix()
        {
            return Matrix.CreateTranslation(new Vector3(-_position, 0)) *
                   Matrix.CreateRotationZ(_rotation) *
                   Matrix.CreateScale(_zoom) *
                   Matrix.CreateTranslation(new Vector3(_viewport.Width * 0.5f, _viewport.Height * 0.5f, 0));
        }
        
        /// <summary>
        /// 将屏幕坐标转换为世界坐标
        /// </summary>
        /// <param name="screenPosition">屏幕坐标</param>
        /// <returns>世界坐标</returns>
        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return Vector2.Transform(screenPosition, Matrix.Invert(GetViewMatrix()));
        }
        
        /// <summary>
        /// 将世界坐标转换为屏幕坐标
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>屏幕坐标</returns>
        public Vector2 WorldToScreen(Vector2 worldPosition)
        {
            return Vector2.Transform(worldPosition, GetViewMatrix());
        }
        
        /// <summary>
        /// 检查世界坐标是否在摄像机视野内
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="margin">边距</param>
        /// <returns>是否可见</returns>
        public bool IsInView(Vector2 worldPosition, float margin = 0)
        {
            Vector2 screenPos = WorldToScreen(worldPosition);
            return screenPos.X >= -margin && screenPos.X <= _viewport.Width + margin &&
                   screenPos.Y >= -margin && screenPos.Y <= _viewport.Height + margin;
        }
    }
}