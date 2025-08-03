using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using HajimiManbo.Network;

namespace HajimiManbo.GameStates
{
    /// <summary>
    /// 输入 IP:Port 并连接服务器
    /// </summary>
    public class JoinRoomState : GameState
    {
        private readonly Texture2D backgroundTexture;
        private readonly Texture2D secondBackgroundTexture;
        private readonly Texture2D roomBackgroundTexture;
        private readonly Rectangle screenBounds;
        private string input = "127.0.0.1:12345";

        private KeyboardState prevKb;
        private double blinkTimer;
        private bool cursorVisible;
        private bool isConnecting;
        private double connectTimer;
        private string errorMessage = "";

        private const float TitleScaleMultiplier = 3f;
        private const float InputScaleMultiplier = 2f;

        // 按钮
        private Rectangle connectButtonRect;
        private MouseState prevMouse;

        private readonly Texture2D pixelTexture;

        public JoinRoomState(Game1 game,
                             GraphicsDeviceManager graphics,
                             SpriteBatch spriteBatch,
                             SpriteFont font,
                             Texture2D backgroundTexture,
                             Texture2D secondBackgroundTexture,
                             Texture2D roomBackgroundTexture)
            : base(game, graphics, spriteBatch, font)
        {
            this.backgroundTexture = backgroundTexture;
            this.secondBackgroundTexture = secondBackgroundTexture;
            this.roomBackgroundTexture = roomBackgroundTexture;
            screenBounds = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);

            // 计算按钮位置（基于屏幕中心下方）
            Vector2 btnSize = UIScaleManager.GetRelativeSize(0.12f, 0.06f);
            Vector2 center = UIScaleManager.GetRelativePosition(0.5f, 0.65f);
            connectButtonRect = new Rectangle((int)(center.X - btnSize.X / 2), (int)(center.Y - btnSize.Y / 2), (int)btnSize.X, (int)btnSize.Y);
            prevMouse = Mouse.GetState();
            pixelTexture = new Texture2D(graphics.GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
            this.roomBackgroundTexture = roomBackgroundTexture;

        }

        private void DoConnect()
        {
            if (isConnecting) return; // 防止重复连接
            
            errorMessage = ""; // 清除之前的错误信息
            string ip = "127.0.0.1";
            ushort port = 12345;
            string[] parts = input.Split(':');
            if (parts.Length >= 1)
                ip = parts[0];
            if (parts.Length == 2 && ushort.TryParse(parts[1], out ushort p))
                port = p;

            NetworkManager.Instance.Connect(ip, port);
            isConnecting = true;
            connectTimer = 0;
        }
        public override void Update(GameTime gameTime)
        {
            blinkTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (blinkTimer >= 0.5f)
            {
                blinkTimer -= 0.5f;
                cursorVisible = !cursorVisible;
            }

            // 检查连接状态
            if (isConnecting)
            {
                connectTimer += gameTime.ElapsedGameTime.TotalSeconds;
                if (NetworkManager.Instance.IsConnected && connectTimer > 0.5) // 检查真正的连接状态
                {
                    // 连接成功，切换到等待房间
                    game.ChangeState(new WaitingRoomState(game, graphics, spriteBatch, font, backgroundTexture, secondBackgroundTexture, roomBackgroundTexture));
                    return;
                }
                else if (connectTimer > 5.0) // 5秒超时
                {
                    // 连接失败，重置状态
                    isConnecting = false;
                    errorMessage = "未找到房间";
                    NetworkManager.Instance.Stop();
                }
            }

            if (!isConnecting) // 只有在非连接状态下才处理输入
            {
                KeyboardState kb = Keyboard.GetState();
                foreach (Keys key in Enum.GetValues(typeof(Keys)))
                {
                    if (IsKeyPressed(kb, key))
                    {
                        HandleKey(key);
                    }
                }
                // —— 鼠标点击按钮 ——
                MouseState mouse = Mouse.GetState();
                if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released && connectButtonRect.Contains(mouse.Position))
                {
                    DoConnect();
                }
                prevMouse = mouse;
                prevKb = kb;
            }
        }

        private bool IsKeyPressed(KeyboardState curr, Keys key) => curr.IsKeyDown(key) && prevKb.IsKeyUp(key);

        private void HandleKey(Keys key)
        {
            if (key == Keys.Enter)
            {
                DoConnect();
                return;
            }
            if (key == Keys.Escape)
            {
                // 返回房间选择
                game.ChangeState(new RoomSelectionState(game, graphics, spriteBatch, font, backgroundTexture, secondBackgroundTexture, roomBackgroundTexture));
                return;
            }
            if (key == Keys.Back)
            {
                if (input.Length > 0)
                    input = input[..^1];
                return;
            }
            // 限制输入字符
            char? c = KeyToChar(key);
            if (c.HasValue)
                input += c.Value;
        }

        private char? KeyToChar(Keys key)
        {
            if (key >= Keys.A && key <= Keys.Z)
                return (char)('a' + (key - Keys.A));
            if (key >= Keys.D0 && key <= Keys.D9)
                return (char)('0' + (key - Keys.D0));
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                return (char)('0' + (key - Keys.NumPad0));
            return key switch
            {
                Keys.OemPeriod or Keys.Decimal => '.',
                Keys.OemSemicolon => ':',
                Keys.OemMinus => '-',
                _ => null
            };
        }

        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Draw(backgroundTexture, screenBounds, Color.White);
            DrawTitle();
            DrawInputBox();
            DrawConnectButton();
            DrawErrorMessage();
        }

        private void DrawTitle()
        {
            const string title = "加入房间";
            Vector2 size = font.MeasureString(title);
            float scale = UIScaleManager.UniformScale * TitleScaleMultiplier;
            Vector2 pos = UIScaleManager.GetRelativePosition(0.5f, 0.2f);
            pos.X -= size.X * scale / 2f;
            pos.Y -= size.Y * scale / 2f;
            spriteBatch.DrawString(font, title, pos, Color.Gold, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private void DrawInputBox()
        {
            string display;
            Color color;
            if (isConnecting)
            {
                display = "连接中...";
                color = Color.Yellow;
            }
            else
            {
                display = input + (cursorVisible ? "|" : "");
                color = Color.White;
            }
            
            Vector2 size = font.MeasureString(display);
            float scale = UIScaleManager.UniformScale * InputScaleMultiplier;
            Vector2 pos = UIScaleManager.GetRelativePosition(0.5f, 0.5f);
            pos.X -= size.X * scale / 2f;
            pos.Y -= size.Y * scale / 2f;
            spriteBatch.DrawString(font, display, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private void DrawConnectButton()
        {
            bool hover = connectButtonRect.Contains(Mouse.GetState().Position);
            Color buttonClr = hover ? Color.LightGreen : Color.White;
            Color borderClr = Color.Gray;
            string label = "连接";

            spriteBatch.Draw(pixelTexture, connectButtonRect, buttonClr * 0.8f);
            int thickness = Math.Max(1, (int)(3 * UIScaleManager.UniformScale));
            // 边框
            spriteBatch.Draw(pixelTexture, new Rectangle(connectButtonRect.X, connectButtonRect.Y, connectButtonRect.Width, thickness), borderClr);
            spriteBatch.Draw(pixelTexture, new Rectangle(connectButtonRect.X, connectButtonRect.Y + connectButtonRect.Height - thickness, connectButtonRect.Width, thickness), borderClr);
            spriteBatch.Draw(pixelTexture, new Rectangle(connectButtonRect.X, connectButtonRect.Y, thickness, connectButtonRect.Height), borderClr);
            spriteBatch.Draw(pixelTexture, new Rectangle(connectButtonRect.X + connectButtonRect.Width - thickness, connectButtonRect.Y, thickness, connectButtonRect.Height), borderClr);

            Vector2 textSize = font.MeasureString(label);
            float scale = UIScaleManager.UniformScale * 1.6f;
            Vector2 textPos = new Vector2(connectButtonRect.X + connectButtonRect.Width / 2f - textSize.X * scale / 2f,
                                          connectButtonRect.Y + connectButtonRect.Height / 2f - textSize.Y * scale / 2f);
            spriteBatch.DrawString(font, label, textPos, Color.Black, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
         }
         
         private void DrawErrorMessage()
         {
             if (!string.IsNullOrEmpty(errorMessage))
             {
                 float scale = UIScaleManager.UniformScale * 1.5f;
                 Vector2 size = font.MeasureString(errorMessage);
                 Vector2 pos = UIScaleManager.GetRelativePosition(0.5f, 0.75f);
                 pos.X -= size.X * scale / 2f;
                 pos.Y -= size.Y * scale / 2f;
                 spriteBatch.DrawString(font, errorMessage, pos, Color.Red, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
             }
         }

 
     }
 }