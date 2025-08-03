using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.IO;

namespace HajimiManbo.Gameplay
{
    /// <summary>
    /// 动画状态枚举
    /// </summary>
    public enum AnimationState
    {
        Idle,
        Moving,
        Sprinting
    }

    /// <summary>
    /// 动画类，处理帧动画的加载和播放
    /// </summary>
    public class Animation
    {
        private List<Texture2D> _frames;
        private float _frameTime;
        private float _currentTime;
        private int _currentFrame;
        private bool _isLooping;
        
        public int FrameCount => _frames?.Count ?? 0;
        public bool IsPlaying { get; private set; }
        
        public Animation(float frameTime = 0.1f, bool isLooping = true)
        {
            _frames = new List<Texture2D>();
            _frameTime = frameTime;
            _isLooping = isLooping;
            _currentTime = 0f;
            _currentFrame = 0;
            IsPlaying = false;
        }
        
        /// <summary>
        /// 添加帧到动画
        /// </summary>
        public void AddFrame(Texture2D frame)
        {
            _frames.Add(frame);
        }
        
        /// <summary>
        /// 开始播放动画
        /// </summary>
        public void Play()
        {
            IsPlaying = true;
            _currentTime = 0f;
            _currentFrame = 0;
        }
        
        /// <summary>
        /// 停止动画
        /// </summary>
        public void Stop()
        {
            IsPlaying = false;
            _currentTime = 0f;
            _currentFrame = 0;
        }
        
        /// <summary>
        /// 更新动画
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!IsPlaying || _frames.Count == 0)
                return;
                
            _currentTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            if (_currentTime >= _frameTime)
            {
                _currentTime -= _frameTime;
                _currentFrame++;
                
                if (_currentFrame >= _frames.Count)
                {
                    if (_isLooping)
                    {
                        _currentFrame = 0;
                    }
                    else
                    {
                        _currentFrame = _frames.Count - 1;
                        IsPlaying = false;
                    }
                }
            }
        }
        
        /// <summary>
        /// 获取当前帧的纹理
        /// </summary>
        public Texture2D GetCurrentFrame()
        {
            if (_frames.Count == 0)
                return null;
                
            return _frames[_currentFrame];
        }
    }
    
    /// <summary>
    /// 角色动画管理器
    /// </summary>
    public class CharacterAnimator
    {
        private Dictionary<AnimationState, Animation> _animations;
        private AnimationState _currentState;
        private Animation _currentAnimation;
        private ContentManager _contentManager;
        private string _characterName;
        
        public AnimationState CurrentState => _currentState;
        
        public CharacterAnimator(ContentManager contentManager, string characterName)
        {
            _contentManager = contentManager;
            _characterName = characterName;
            _animations = new Dictionary<AnimationState, Animation>();
            _currentState = AnimationState.Idle;
            
            LoadAnimations();
        }
        
        /// <summary>
        /// 加载角色的所有动画
        /// </summary>
        private void LoadAnimations()
        {
            try
            {
                // 加载静止动画
                var idleAnimation = LoadAnimationFrames("noMoveAnimation", 0.15f);
                _animations[AnimationState.Idle] = idleAnimation;
                
                // 加载移动动画
                var moveAnimation = LoadAnimationFrames("MoveAnimation", 0.1f);
                _animations[AnimationState.Moving] = moveAnimation;
                
                // 加载冲刺动画
                var sprintAnimation = LoadAnimationFrames("SprintAnimation", 0.08f);
                _animations[AnimationState.Sprinting] = sprintAnimation;
                
                // 设置默认动画
                _currentAnimation = _animations[AnimationState.Idle];
                _currentAnimation.Play();
                
                Console.WriteLine($"[CharacterAnimator] Successfully loaded animations for {_characterName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CharacterAnimator] Failed to load animations for {_characterName}: {ex.Message}");
                CreateFallbackAnimation();
            }
        }
        
        /// <summary>
        /// 加载指定动画文件夹的帧
        /// </summary>
        private Animation LoadAnimationFrames(string animationName, float frameTime)
        {
            var animation = new Animation(frameTime, true);
            
            // 尝试加载帧文件
            int frameIndex = 0;
            while (true)
            {
                try
                {
                    string framePath = $"img/Character/{_characterName}/{animationName}/{animationName}_f{frameIndex:D3}";
                    var frameTexture = _contentManager.Load<Texture2D>(framePath);
                    animation.AddFrame(frameTexture);
                    frameIndex++;
                }
                catch
                {
                    // 没有更多帧了
                    break;
                }
            }
            
            if (animation.FrameCount == 0)
            {
                throw new Exception($"No frames found for animation {animationName}");
            }
            
            return animation;
        }
        
        /// <summary>
        /// 创建后备动画（纯色方块）
        /// </summary>
        private void CreateFallbackAnimation()
        {
            // 这里可以创建一个简单的后备动画
            // 暂时留空，让调用者处理
        }
        
        /// <summary>
        /// 设置动画状态
        /// </summary>
        public void SetState(AnimationState newState)
        {
            if (_currentState == newState)
                return;
                
            _currentState = newState;
            
            if (_animations.ContainsKey(newState))
            {
                _currentAnimation?.Stop();
                _currentAnimation = _animations[newState];
                _currentAnimation.Play();
            }
        }
        
        /// <summary>
        /// 更新动画
        /// </summary>
        public void Update(GameTime gameTime)
        {
            _currentAnimation?.Update(gameTime);
        }
        
        /// <summary>
        /// 获取当前帧纹理
        /// </summary>
        public Texture2D GetCurrentFrame()
        {
            return _currentAnimation?.GetCurrentFrame();
        }
    }
}