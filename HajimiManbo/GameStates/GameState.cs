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
        /// ���ֱ��ʻ�ȫ��״̬�����仯ʱ�� Game1 ������
        /// ��״̬�ڴ˷��������������֡�
        /// </summary>
        public virtual void OnResolutionChanged() { }
    }
}
