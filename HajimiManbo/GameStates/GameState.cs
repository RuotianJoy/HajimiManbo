using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HajimiManbo.GameStates
{
    public abstract class GameState
    {
        protected Game1 game;
        protected GraphicsDeviceManager graphics;
        protected SpriteBatch spriteBatch;
        protected SpriteFont font;

        public GameState(Game1 game, GraphicsDeviceManager graphics,
                         SpriteBatch spriteBatch, SpriteFont font)
        {
            this.game = game;
            this.graphics = graphics;
            this.spriteBatch = spriteBatch;
            this.font = font;
        }

        public abstract void Update(GameTime gameTime);
        public abstract void Draw(GameTime gameTime);

        /// <summary>
        /// 当分辨率或全屏状态发生变化时由 Game1 触发，
        /// 各状态在此方法里重算自身布局。
        /// </summary>
        public virtual void OnResolutionChanged() { }
    }
}
