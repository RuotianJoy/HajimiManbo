using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace HajimiManbo.GameStates
{
    /// <summary>
    /// 游戏设置页面（无磨砂面板版本）
    /// 2025‑08‑03：应需求移除半透明面板及相关计算/绘制逻辑。
    ///  - 删除 _panelLeft / _panelRight 字段
    ///  - 移除面板尺寸计算
    ///  - Draw() 不再调用 DrawPane()
    /// </summary>
    public class SettingsState : GameState
    {
        // ────────── 依赖资源 ──────────
        private readonly Texture2D _background;
        private readonly Texture2D _secondBackground;
        private readonly (int w, int h)[] _resolutions = { (1920, 1080), (2560, 1440) };
        private readonly int[] _frameRates = { 30, 60, 120, 144 }; // 可选刷新率

        // ────────── 运行时状态 ──────────
        private MouseState _prevMouse, _curMouse;
        private KeyboardState _prevKey, _curKey;
        private bool _isFullScreen;
        private int _resolutionIdx;
        private int _frameRateIdx = 1; // 默认60FPS
        private float _masterVolume = 1f;
        private int _hoveredSetting = -1;
        private int _hoveredVolumeButton = -1; // -1: 无悬浮, 0: 减号, 1: 加号
        private string? _waitingKey;

        // ────────── UI 元素 ──────────
        private Rectangle[] _settingButtons = null!;
        private Rectangle _volumeDecreaseBtn, _volumeIncreaseBtn;
        private Rectangle[] _keyButtons = null!;

        // ────────── 按键绑定表 ──────────
        private Dictionary<string, Keys> _keyMap = new();

        // ────────── ctor ──────────
        public SettingsState(Game1 game,
                             GraphicsDeviceManager gdm,
                             SpriteBatch spriteBatch,
                             SpriteFont font,
                             Texture2D background,
                             Texture2D secondBackground)
            : base(game, gdm, spriteBatch, font)
        {
            _background = background;
            _secondBackground = secondBackground;
            _isFullScreen = gdm.IsFullScreen;
            _resolutionIdx = GetCurrentResolutionIndex();
            _frameRateIdx = GetCurrentFrameRateIndex(); // 从当前游戏设置获取刷新率
            _masterVolume = Game1.GlobalVolume; // 从全局设置获取音量
            _keyMap = InputManager.GetAllKeys(); // 从InputManager获取按键配置
            RecalculateLayout();
        }

        // ───────────────────────────── Layout helpers ─────────────────────────────
        private int GetCurrentResolutionIndex()
        {
            for (int i = 0; i < _resolutions.Length; i++)
                if (_resolutions[i].w == graphics.PreferredBackBufferWidth &&
                    _resolutions[i].h == graphics.PreferredBackBufferHeight)
                    return i;
            return 0;
        }
        
        private int GetCurrentFrameRateIndex()
        {
            int currentFPS = ((Game1)game).GetCurrentFrameRate();
            // 如果是无上限模式，默认选择60FPS
            if (currentFPS == 0) return 1;
            
            for (int i = 0; i < _frameRates.Length; i++)
                if (_frameRates[i] == currentFPS)
                    return i;
            return 1; // 默认60FPS
        }

        public override void OnResolutionChanged() => RecalculateLayout();

        private void RecalculateLayout()
        {
            // — 设置按钮
            Vector2 btnSize = UIScaleManager.GetRelativeSize(300f / 1920f, 60f / 1080f);
            float btnGap = UIScaleManager.GetRelativeSize(0, 20f / 1080f).Y;
            Vector2 btnStart = UIScaleManager.GetRelativePosition(0.25f, 0.25f);

            _settingButtons = new Rectangle[3];
            for (int i = 0; i < 3; i++)
            {
                _settingButtons[i] = new Rectangle(
                    (int)btnStart.X,
                    (int)(btnStart.Y + i * (btnSize.Y + btnGap)),
                    (int)btnSize.X,
                    (int)btnSize.Y);
            }

            // — 音量加减按钮（减号在左，百分比在中间，加号在右）
            Vector2 volumeBtnSize = UIScaleManager.GetRelativeSize(40f / 1920f, 40f / 1080f);
            Vector2 volumeBtnPos = btnStart +
                                   new Vector2(UIScaleManager.GetRelativeSize(250f / 1920f, 0).X,
                                               UIScaleManager.GetRelativeSize(0, 250f / 1080f).Y);
            // 减号按钮在左边
            _volumeDecreaseBtn = new Rectangle((int)volumeBtnPos.X, (int)volumeBtnPos.Y, (int)volumeBtnSize.X, (int)volumeBtnSize.Y);
            // 加号按钮在右边（预留百分比显示空间）
            _volumeIncreaseBtn = new Rectangle((int)(volumeBtnPos.X + volumeBtnSize.X + 80), (int)volumeBtnPos.Y, (int)volumeBtnSize.X, (int)volumeBtnSize.Y);

            // — 按键绑定按钮
            Vector2 keyBtnSize = UIScaleManager.GetRelativeSize(120f / 1920f, 40f / 1080f);
            float keyGap = UIScaleManager.GetRelativeSize(0, 50f / 1080f).Y;
            Vector2 keyStart = UIScaleManager.GetRelativePosition(0.75f, 0.25f);

            _keyButtons = new Rectangle[_keyMap.Count];
            int idx = 0;
            foreach (var _ in _keyMap)
            {
                _keyButtons[idx] = new Rectangle((int)keyStart.X,
                                                 (int)(keyStart.Y + idx * keyGap),
                                                 (int)keyBtnSize.X,
                                                 (int)keyBtnSize.Y);
                idx++;
            }
        }

        // ───────────────────────────── Update logic ─────────────────────────────
        public override void Update(GameTime gameTime)
        {
            _prevMouse = _curMouse; _curMouse = Mouse.GetState();
            _prevKey = _curKey; _curKey = Keyboard.GetState();

            // 更新全局音量设置
            Game1.GlobalVolume = _masterVolume;
            MediaPlayer.Volume = _masterVolume;
            SoundEffect.MasterVolume = _masterVolume;

            if (_waitingKey != null) { CaptureKeyBinding(); return; }
            if (IsPressed(Keys.Escape)) { game.ReturnToMainMenu(); return; }

            HandleMouse();
        }

        private bool IsPressed(Keys k) => _curKey.IsKeyDown(k) && !_prevKey.IsKeyDown(k);

        private void HandleMouse()
        {
            // 悬浮检测
            _hoveredSetting = -1;
            for (int i = 0; i < _settingButtons.Length; i++)
                if (_settingButtons[i].Contains(_curMouse.Position)) { _hoveredSetting = i; break; }

            // 音量按钮悬浮检测
            _hoveredVolumeButton = -1;
            if (_volumeDecreaseBtn.Contains(_curMouse.Position)) _hoveredVolumeButton = 0;
            else if (_volumeIncreaseBtn.Contains(_curMouse.Position)) _hoveredVolumeButton = 1;

            HandleVolumeButtons();
            HandleKeyButtonClick();

            if (_curMouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                HandleSettingClick();
        }

        private void HandleVolumeButtons()
        {
            if (_curMouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
            {
                if (_hoveredVolumeButton == 0) // 减号按钮
                {
                    _masterVolume = Math.Max(0f, _masterVolume - 0.1f);
                }
                else if (_hoveredVolumeButton == 1) // 加号按钮
                {
                    _masterVolume = Math.Min(1f, _masterVolume + 0.1f);
                }
            }
        }

        private void HandleKeyButtonClick()
        {
            if (_curMouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
            {
                int idx = 0;
                foreach (var kvp in _keyMap)
                {
                    if (_keyButtons[idx].Contains(_curMouse.Position)) { _waitingKey = kvp.Key; break; }
                    idx++;
                }
            }
        }

        private void CaptureKeyBinding()
        {
            foreach (var key in _curKey.GetPressedKeys())
                if (!_prevKey.IsKeyDown(key))
                {
                    if (key != Keys.Escape) 
                    {
                        _keyMap[_waitingKey!] = key;
                        InputManager.SetKey(_waitingKey!, key); // 同步到InputManager
                    }
                    _waitingKey = null;
                    break;
                }
        }

        private void HandleSettingClick()
        {
            switch (_hoveredSetting)
            {
                case 0: // 全屏
                    _isFullScreen = !_isFullScreen;
                    graphics.IsFullScreen = _isFullScreen;
                    ((Game1)game).ApplyGraphicsChanges();
                    break;

                case 1: // 分辨率
                    _resolutionIdx = (_resolutionIdx + 1) % _resolutions.Length;
                    var (w, h) = _resolutions[_resolutionIdx];
                    graphics.PreferredBackBufferWidth = w;
                    graphics.PreferredBackBufferHeight = h;
                    ((Game1)game).ApplyGraphicsChanges();
                    break;

                case 2: // 刷新率
                    _frameRateIdx = (_frameRateIdx + 1) % _frameRates.Length;
                    int targetFPS = _frameRates[_frameRateIdx];
                    ((Game1)game).SetFrameRate(targetFPS);
                    break;
            }
        }

        // ───────────────────────────── Draw ─────────────────────────────
        public override void Draw(GameTime gameTime)
        {
            //背景
            spriteBatch.Draw(_background,
                             new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight),
                             Color.White);

            // 第二个背景图
            spriteBatch.Draw(_secondBackground,
                             new Rectangle((int)(graphics.PreferredBackBufferWidth * 0.2), (int)(graphics.PreferredBackBufferHeight * 0.2), (int)(graphics.PreferredBackBufferWidth*0.65), (int)(graphics.PreferredBackBufferHeight * 0.4)),
                             Color.White * 0.8f); // 使用半透明效果

            // 前景元素
            DrawTitle();
            DrawSettingButtons();
            DrawVolumeControl();
            DrawKeyBindings();
            DrawTip();
        }

        private void DrawTitle()
        {
            string title = "游戏设置";
            Vector2 size = font.MeasureString(title) * UIScaleManager.UniformScale * 3;
            Vector2 pos = UIScaleManager.GetRelativePosition(0.5f, 0.08f) - size / 2f;
            spriteBatch.DrawString(font, title, pos, Color.Gold, 0f, Vector2.Zero, UIScaleManager.UniformScale * 3, SpriteEffects.None, 0f);
        }

        private void DrawSettingButtons()
        {
            var px = GetPixelTexture();
            string[] labels =
            {
                $"全屏: {(_isFullScreen ? "开" : "关")}",
                $"分辨率: {_resolutions[_resolutionIdx].w}×{_resolutions[_resolutionIdx].h}",
                $"刷新率: {_frameRates[_frameRateIdx]}FPS"
            };

            for (int i = 0; i < _settingButtons.Length; i++)
            {
                Color border = _hoveredSetting == i ? Color.Gold : Color.Black;
                Color bg = _hoveredSetting == i ? Color.Gold * 0.3f : Color.White * 0.3f;

                spriteBatch.Draw(px, _settingButtons[i], bg);
                DrawBorder(px, _settingButtons[i], border, 2);

                Vector2 txtSize = font.MeasureString(labels[i]) * UIScaleManager.UniformScale;
                Vector2 txtPos = new(_settingButtons[i].Center.X - txtSize.X / 2,
                                        _settingButtons[i].Center.Y - txtSize.Y / 2);
                spriteBatch.DrawString(font, labels[i], txtPos, Color.Black, 0f, Vector2.Zero, UIScaleManager.UniformScale, SpriteEffects.None, 0f);
            }
        }

        private void DrawVolumeControl()
        {
            var px = GetPixelTexture();

            // 标签
            Vector2 labelOffset = UIScaleManager.GetRelativeSize(220f / 1920f, 0);
            spriteBatch.DrawString(font, "音乐音量",
                                   new Vector2(_volumeDecreaseBtn.X - labelOffset.X, _volumeDecreaseBtn.Y + 5),
                                   Color.Black, 0f, Vector2.Zero,
                                   UIScaleManager.UniformScale, SpriteEffects.None, 0f);

            // 减号按钮
            Color decreaseBtnColor = _hoveredVolumeButton == 0 ? Color.Gold : Color.White;
            spriteBatch.Draw(px, _volumeDecreaseBtn, decreaseBtnColor * 0.8f);
            DrawBorder(px, _volumeDecreaseBtn, Color.Black, 2);
            
            Vector2 minusSize = font.MeasureString("-") * UIScaleManager.UniformScale;
            Vector2 minusPos = new(_volumeDecreaseBtn.Center.X - minusSize.X / 2,
                                   _volumeDecreaseBtn.Center.Y - minusSize.Y / 2);
            spriteBatch.DrawString(font, "-", minusPos, Color.Black, 0f, Vector2.Zero, UIScaleManager.UniformScale, SpriteEffects.None, 0f);
            // 百分比显示（在减号和加号按钮之间）
            string pct = $"{(int)(_masterVolume * 100)}%";
            Vector2 pctSize = font.MeasureString(pct) * UIScaleManager.UniformScale;
            Vector2 pctPos = new((_volumeDecreaseBtn.Right + _volumeIncreaseBtn.Left) / 2f - pctSize.X / 2f,
                                 _volumeDecreaseBtn.Y + (_volumeDecreaseBtn.Height - pctSize.Y) / 2f);
            spriteBatch.DrawString(font, pct, pctPos, Color.Black, 0f, Vector2.Zero,
                                   UIScaleManager.UniformScale, SpriteEffects.None, 0f);

            // 加号按钮
            Color increaseBtnColor = _hoveredVolumeButton == 1 ? Color.Gold : Color.White;
            spriteBatch.Draw(px, _volumeIncreaseBtn, increaseBtnColor * 0.8f);
            DrawBorder(px, _volumeIncreaseBtn, Color.Black, 2);
            
            Vector2 plusSize = font.MeasureString("+") * UIScaleManager.UniformScale;
            Vector2 plusPos = new(_volumeIncreaseBtn.Center.X - plusSize.X / 2,
                                  _volumeIncreaseBtn.Center.Y - plusSize.Y / 2);
            spriteBatch.DrawString(font, "+", plusPos, Color.Black, 0f, Vector2.Zero, UIScaleManager.UniformScale, SpriteEffects.None, 0f);

           
        }

        private void DrawKeyBindings()
        {
            var px = GetPixelTexture();

            // 标题
            spriteBatch.DrawString(
                font, "按键设置",
                UIScaleManager.GetRelativePosition(0.75f, 0.20f),
                Color.Gold, 0f, Vector2.Zero,
                UIScaleManager.UniformScale, SpriteEffects.None, 0f);

            int idx = 0;
            foreach (var kvp in _keyMap)
            {
                // 标签
                Vector2 lblPos = new(_keyButtons[idx].X - 150, _keyButtons[idx].Y + 10);
                spriteBatch.DrawString(font, kvp.Key + ':', lblPos, Color.Black, 0f, Vector2.Zero, UIScaleManager.UniformScale, SpriteEffects.None, 0f);

                // 按钮背景
                Color btnClr = _waitingKey == kvp.Key ? Color.Yellow : _keyButtons[idx].Contains(_curMouse.Position) ? Color.Gold : Color.White;
                spriteBatch.Draw(px, _keyButtons[idx], btnClr * 0.8f);
                DrawBorder(px, _keyButtons[idx], Color.Black, 2);

                // 内容文字
                string txt = _waitingKey == kvp.Key ? "按任意键" : kvp.Value.ToString();
                Vector2 txtS = font.MeasureString(txt) * UIScaleManager.UniformScale;
                Vector2 txtP = new(_keyButtons[idx].Center.X - txtS.X / 2,
                                   _keyButtons[idx].Center.Y - txtS.Y / 2);
                spriteBatch.DrawString(font, txt, txtP, Color.Red, 0f, Vector2.Zero, UIScaleManager.UniformScale, SpriteEffects.None, 0f);
                idx++;
            }
        }

        private void DrawTip()
        {
            string tip = "按 ESC 返回主菜单 | 点击功能项可更改设置";
            Vector2 size = font.MeasureString(tip) * UIScaleManager.UniformScale;
            Vector2 pos = new(graphics.PreferredBackBufferWidth / 2f - size.X / 2f,
                               graphics.PreferredBackBufferHeight - size.Y - 20);
            spriteBatch.DrawString(font, tip, pos, Color.Black, 0f, Vector2.Zero, UIScaleManager.UniformScale, SpriteEffects.None, 0f);
        }


        // ────────────────────────────────────────────────────────
        #region Utility
        private Texture2D _pixelTexture;
        
        private Texture2D GetPixelTexture()
        {
            if (_pixelTexture == null)
            {
                _pixelTexture = new Texture2D(graphics.GraphicsDevice, 1, 1);
                _pixelTexture.SetData(new[] { Color.White });
            }
            return _pixelTexture;
        }

        private void DrawBorder(Texture2D px, Rectangle r, Color c, int t)
        {
            spriteBatch.Draw(px, new Rectangle(r.X, r.Y, r.Width, t), c);
            spriteBatch.Draw(px, new Rectangle(r.X, r.Y + r.Height - t, r.Width, t), c);
            spriteBatch.Draw(px, new Rectangle(r.X, r.Y, t, r.Height), c);
            spriteBatch.Draw(px, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
        }
        #endregion
    }
}
