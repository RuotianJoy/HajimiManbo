using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace HajimiManbo.Gameplay
{
    /// <summary>
    /// 像 Terraria 一样的二维摄像机：
    ///   • 默认 1:1 缩放（Zoom=1），方块 16 px 时，1080p 能看到约 120×68 个方块  
    ///   • 支持 0.5–2.0 缩放，与 Terraria “视距”滑杆相同  
    ///   • 像素-完美锁定：避免 1 px 闪缝
    /// </summary>
    public class Camera2D
    {
        // ────────── 基础字段 ──────────
        private Vector2 _position;        // 世界像素坐标，指摄像机中心
        private float _zoom = 1f;  // 0.5–2.0
        private float _rotation = 0f;  // 旋转一般用不到，保留接口
        private Viewport _viewport;
        private Rectangle _worldBounds = Rectangle.Empty; // 像素单位

        // ────────── 公共属性 ──────────
        public Vector2 Position => _position;        // 只读，改用 CenterOn / Move
        public float Zoom
        {
            get => _zoom;
            set => _zoom = MathHelper.Clamp(value, 0.5f, 2.0f);
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

        // ────────── 构造 ──────────
        public Camera2D(Viewport viewport) => _viewport = viewport;

        // ────────── 基础功能 ──────────
        /// <summary>把摄像机中心直接对准目标（立即跟随）。</summary>
        public void CenterOn(Vector2 targetCenter)
        {
            _position = targetCenter;
            ClampInsideWorld();
        }

        /// <summary>以 Lerp 方式平滑跟随。</summary>
        public void SmoothFollow(Vector2 targetCenter, float lerpAmount = 0.15f)
        {
            _position = Vector2.Lerp(_position, targetCenter, MathHelper.Clamp(lerpAmount, 0f, 1f));
            ClampInsideWorld();
        }

        /// <summary>在当前缩放下，把摄像机限制在世界范围内。</summary>
        private void ClampInsideWorld()
        {
            if (_worldBounds == Rectangle.Empty) return;

            // 计算“半视口”尺寸（世界单位）
            float halfW = _viewport.Width * 0.5f / _zoom;
            float halfH = _viewport.Height * 0.5f / _zoom;

            float minX = _worldBounds.Left + halfW;
            float maxX = _worldBounds.Right - halfW;
            float minY = _worldBounds.Top + halfH;
            float maxY = _worldBounds.Bottom - halfH;

            // 若地图比视口还小，直接居中
            if (minX > maxX) { _position.X = (_worldBounds.Left + _worldBounds.Right) * 0.5f; }
            else { _position.X = MathHelper.Clamp(_position.X, minX, maxX); }

            if (minY > maxY) { _position.Y = (_worldBounds.Top + _worldBounds.Bottom) * 0.5f; }
            else { _position.Y = MathHelper.Clamp(_position.Y, minY, maxY); }
        }

        // ────────── 视矩阵 ──────────
        public Matrix GetViewMatrix()
        {
            // 注意顺序：先把世界-摄像机中心移到原点 → 旋转 → 缩放 → 把屏幕中心移回视口中心
            // 最后再对 平移×缩放 做 Floor，像素锁定
            Vector3 translate = new Vector3(
                -MathF.Floor(_position.X * _zoom) / _zoom,
                -MathF.Floor(_position.Y * _zoom) / _zoom,
                0f);

            return Matrix.CreateTranslation(translate) *
                   Matrix.CreateRotationZ(_rotation) *
                   Matrix.CreateScale(_zoom, _zoom, 1f) *
                   Matrix.CreateTranslation(_viewport.Width * 0.5f, _viewport.Height * 0.5f, 0f);
        }

        // ────────── 辅助坐标转换 ──────────
        public Vector2 ScreenToWorld(Vector2 screenPos) =>
            Vector2.Transform(screenPos, Matrix.Invert(GetViewMatrix()));

        public Vector2 WorldToScreen(Vector2 worldPos) =>
            Vector2.Transform(worldPos, GetViewMatrix());

        /// <summary>判断某点（世界坐标）是否在当前可见范围内。</summary>
        public bool IsInView(Vector2 worldPos, float marginPx = 0f)
        {
            Vector2 screen = WorldToScreen(worldPos);
            return screen.X >= -marginPx && screen.X <= _viewport.Width + marginPx &&
                   screen.Y >= -marginPx && screen.Y <= _viewport.Height + marginPx;
        }
    }
}
