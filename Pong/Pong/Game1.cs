using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Pong
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        GameTime gameTime;

        // The images we will draw
        Texture2D player1Texture;
        Texture2D player2Texture;
        Texture2D blockTexture;

        SoundEffect collisionSound;
        Song backgroundMusic;

        SpriteFont scoreFont;
        SpriteFont infoFont;
        Vector2 player1ScorePos;
        Vector2 player2ScorePos;

        // The color data for the images; used for per pixel collision
        Color[] player1TextureData;
        Color[] player2TextureData;
        Color[] blockTextureData;
        Texture2D centerLineDash;

        Color background;

        // Player
        Vector2 player1Position;
        Vector2 player2Position;
        const int playerMoveSpeed = 5;

        bool aiOn;

        private int player1Score;
        private int player2Score;

        private string infoText;
        private bool gameOver;
        private bool scored;
        private bool gameStart;
        private bool isPaused = false;
        private string champ;

        // Block
        Vector2 blockPosition;
        int blockXSpeed;
        int blockYSpeed;
        const int blockSpeed = 5;

        float spawnTimeout = 1.5f;
        float spawnTimer;
        bool timeout;

        SpriteFont consoleFont;
        MouseState oldMouseState;
        GamePadState oldGamePadState;
        GamePadState oldGamePadState2;
        Boolean consoleDisplaying;
        KeyboardState oldState;

        string test = "";
        char previousChar = (char)0;
        float CurrentCharPause;
        float CharPause = 1950; //in milliseconds for the character timer

        // The sub-rectangle of the drawable area which should be visible on all TVs
        Rectangle safeBounds;
        // Percentage of the screen on every side is the safe area
        const float SafeAreaPortion = 0.05f;

        Random random = new Random();

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            gameTime = new GameTime();

            oldState = Keyboard.GetState();
            oldMouseState = Mouse.GetState();
            consoleDisplaying = false;
            aiOn = true;

            background = Color.WhiteSmoke;

            gameOver = false;
            gameStart = true;
            scored = false;

            // Load textures
            blockTexture = Content.Load<Texture2D>("Block");
            player1Texture = Content.Load<Texture2D>("Player1");
            player2Texture = Content.Load<Texture2D>("Player2");
            collisionSound = Content.Load<SoundEffect>("cymbolTing");
            backgroundMusic = Content.Load<Song>("Bare Necessities");
            
            // Calculate safe bounds based on current resolution
            Viewport viewport = graphics.GraphicsDevice.Viewport;
            safeBounds = new Rectangle(
                (int)(viewport.Width * SafeAreaPortion),
                (int)(viewport.Height * SafeAreaPortion),
                (int)(viewport.Width * (1 - (2 * SafeAreaPortion))),
                (int)(viewport.Height * (1 - (2 * SafeAreaPortion))));
            // Start the player in the center along the bottom of the screen
            player1Position.X = safeBounds.Left - player1Texture.Width/2;
            player1Position.Y = (safeBounds.Height - player1Texture.Height) / 2;
            player2Position.X = safeBounds.Right - (player2Texture.Width / 2);
            player2Position.Y = (safeBounds.Height - player2Texture.Height) / 2;


            centerLineDash = new Texture2D(graphics.GraphicsDevice, 6, viewport.Height/30);

            Color[] data = new Color[6 * (viewport.Height / 30)];
            for (int i = 0; i < data.Length; ++i) data[i] = new Color(40, 40, 40, 120);
            centerLineDash.SetData(data);

            PlaceBlock();

            player1Score = 0;
            player2Score = 0;
            player1ScorePos = new Vector2((float)((safeBounds.Width / 2) - (safeBounds.Width / 4)), (float)(safeBounds.Height / 6));
            player2ScorePos = new Vector2((float)((safeBounds.Width / 2) + (safeBounds.Width / 4)), (float)(safeBounds.Height / 6));

            timeout = true;

            //MediaPlayer.Play(backgroundMusic);
            MediaPlayer.IsRepeating = true;

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            scoreFont = Content.Load<SpriteFont>("ScoreFont");
            consoleFont = Content.Load<SpriteFont>("ConsoleFont");
            infoFont = Content.Load<SpriteFont>("InfoFont");

            // Extract collision data
            blockTextureData =
                new Color[blockTexture.Width * blockTexture.Height];
            blockTexture.GetData(blockTextureData);
            
            player1TextureData =
                new Color[player1Texture.Width * player1Texture.Height];
            player1Texture.GetData(player1TextureData);
            
            player2TextureData =
                new Color[player2Texture.Width * player2Texture.Height];
            player2Texture.GetData(player2TextureData);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboard = Keyboard.GetState();
            GamePadState gamePad = GamePad.GetState(PlayerIndex.One);
            GamePadState gamePad2 = GamePad.GetState(PlayerIndex.Two);

            if((keyboard.IsKeyUp(Keys.P) && oldState.IsKeyDown(Keys.P))
                || (gamePad.IsButtonUp(Buttons.Start) && oldGamePadState.IsButtonDown(Buttons.Start))
                && !(gameOver || scored || gameStart))
            {
                isPaused = !isPaused;
                if (isPaused && !gameStart && !gameOver && !scored)
                    MediaPlayer.Pause();
                else if (!gameStart && !gameOver && !scored)
                    MediaPlayer.Resume();
            }

            if (keyboard.IsKeyDown(Keys.Tab) && oldState.IsKeyUp(Keys.Tab))
            {
                consoleDisplaying = !consoleDisplaying;
            }

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                GamePad.GetState(PlayerIndex.Two).Buttons.Back == ButtonState.Pressed ||
                keyboard.IsKeyDown(Keys.Escape))
                this.Exit();

            if (CurrentCharPause > CharPause)
            {
                previousChar = (char)0;
                CurrentCharPause = 0;
            }
            CurrentCharPause += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            // Console
            if (consoleDisplaying)
            {
                if (keyboard.IsKeyDown(Keys.Enter) && !oldState.IsKeyDown(Keys.Enter))
                {
                    ExecuteCommand(test);
                    test = "";
                }
                else if (keyboard.IsKeyDown(Keys.Back) && !oldState.IsKeyDown(Keys.Back))
                {
                    if (test.Length > 0)
                        test = test.Remove(test.Length - 1, 1);
                }
                else if (!keyboard.IsKeyDown(Keys.Back)
                    && GetKeyboardChars() != (char)0
                    && GetKeyboardChars() != previousChar)
                {
                    char testChar = GetKeyboardChars();

                    test += testChar;
                    previousChar = testChar;
                }
            }

            if (!isPaused)
            {
                if (gameStart)
                {
                    if ((keyboard.IsKeyDown(Keys.Space) && !oldState.IsKeyDown(Keys.Space))
                        || gamePad.IsButtonDown(Buttons.A) && !oldGamePadState.IsButtonDown(Buttons.A)
                        || gamePad2.IsButtonDown(Buttons.A) && !oldGamePadState2.IsButtonDown(Buttons.A))
                    {
                        gameStart = false;
                        MediaPlayer.Play(backgroundMusic);
                    }
                }
                else if (gameOver)
                {
                    if ((keyboard.IsKeyDown(Keys.Space) && !oldState.IsKeyDown(Keys.Space))
                        || gamePad.IsButtonDown(Buttons.A) && !oldGamePadState.IsButtonDown(Buttons.A)
                        || gamePad2.IsButtonDown(Buttons.A) && !oldGamePadState2.IsButtonDown(Buttons.A))
                    {
                        player1Score = 0;
                        player2Score = 0;
                        gameOver = false;
                        MediaPlayer.Play(backgroundMusic);
                    }
                }
                else if (scored)
                {
                    if ((keyboard.IsKeyDown(Keys.Space) && !oldState.IsKeyDown(Keys.Space))
                        || gamePad.IsButtonDown(Buttons.A) && !oldGamePadState.IsButtonDown(Buttons.A)
                        || gamePad2.IsButtonDown(Buttons.A) && !oldGamePadState2.IsButtonDown(Buttons.A))
                    {
                        scored = false;
                        MediaPlayer.Resume();
                    }
                }
                else
                {
                    // update the block
                    if (!timeout)
                    {
                        blockPosition.X += blockXSpeed;
                        blockPosition.Y += blockYSpeed;
                    }
                    else
                    {
                        spawnTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

                        if (spawnTimer <= 0)
                        {
                            timeout = false;
                        }
                    }
                }

                // Move the player and AI
                if (keyboard.IsKeyDown(Keys.W) ||
                    gamePad.DPad.Down == ButtonState.Pressed ||
                    (gamePad.ThumbSticks.Left.Y > 0))
                {
                    player1Position.Y -= playerMoveSpeed;
                }
                if (keyboard.IsKeyDown(Keys.S) ||
                    gamePad.DPad.Up == ButtonState.Pressed ||
                    (gamePad.ThumbSticks.Left.Y < 0))
                {
                    player1Position.Y += playerMoveSpeed;
                }


                if (aiOn)
                {
                    AIMove();
                }
                else
                {
                    if (keyboard.IsKeyDown(Keys.Up) ||
                    gamePad2.DPad.Down == ButtonState.Pressed ||
                    (gamePad2.ThumbSticks.Left.Y > 0))
                    {
                        player2Position.Y -= playerMoveSpeed;
                    }
                    if (keyboard.IsKeyDown(Keys.Down) ||
                        gamePad2.DPad.Up == ButtonState.Pressed ||
                        (gamePad2.ThumbSticks.Left.Y < 0))
                    {
                        player2Position.Y += playerMoveSpeed;
                    }
                }

                if (blockPosition.Y < safeBounds.Top || blockPosition.Y > (safeBounds.Bottom - blockTexture.Height))
                {
                    blockYSpeed *= -1;
                }

                if (blockPosition.X > Window.ClientBounds.Width)
                {
                    player1Score++;

                    if (player1Score == 3)
                    {
                        gameOver = true;
                        champ = "player1";
                        MediaPlayer.Stop();
                    }
                    else
                    {
                        scored = true;
                        MediaPlayer.Pause();
                    }

                    PlaceBlock();
                }
                else if (blockPosition.X < -blockTexture.Width)
                {
                    player2Score++;

                    if (player2Score == 3)
                    {
                        gameOver = true;
                        champ = "player2";
                        MediaPlayer.Stop();
                    }
                    else
                    {
                        scored = true;
                        MediaPlayer.Pause();
                    }

                    PlaceBlock();
                }

                // Prevent the person from moving off of the screen
                player1Position.Y = MathHelper.Clamp(player1Position.Y, safeBounds.Top, safeBounds.Bottom - player1Texture.Height);

                player2Position.Y = MathHelper.Clamp(player2Position.Y, safeBounds.Top, safeBounds.Bottom - player2Texture.Height);

                // Get the bounding rectangle of the person
                Rectangle player1Rectangle =
                    new Rectangle((int)player1Position.X, (int)player1Position.Y,
                    player1Texture.Width, player1Texture.Height);

                Rectangle player2Rectangle =
                    new Rectangle((int)player2Position.X, (int)player2Position.Y,
                    player2Texture.Width, player2Texture.Height);


                // Get the bounding rectangle of this block
                Rectangle blockRectangle =
                    new Rectangle((int)blockPosition.X, (int)blockPosition.Y,
                    blockTexture.Width, blockTexture.Height);

                // Check collision with person
                if (player1Rectangle.Intersects(blockRectangle))
                {
                    collisionSound.Play();

                    if (blockPosition.X < player1Position.X + player1Rectangle.Width / 1.5)
                    {
                        blockYSpeed *= -1;
                    }
                    else
                    {
                        blockXSpeed *= -1;
                    }
                }
                else if (player2Rectangle.Intersects(blockRectangle))
                {
                    collisionSound.Play();

                    if (blockPosition.X > player2Position.X)
                    {
                        blockYSpeed *= -1;
                    }
                    else
                    {
                        blockXSpeed *= -1;
                    }
                }
            }
            oldState = keyboard;
            oldGamePadState = gamePad;
            oldGamePadState2 = gamePad2;
            base.Update(gameTime);
        }

        private void AIMove()
        {
            float yDifference = (player2Position.Y - blockPosition.Y);

            if (yDifference < -10)
            {
                player2Position.Y += playerMoveSpeed;
            }
            else if (yDifference > 10)
            {
                player2Position.Y -= playerMoveSpeed;
            }
        }

        private void PlaceBlock()
        {

            spawnTimer = spawnTimeout;
            timeout = true;
            
            double startXDir = random.NextDouble() - 0.01;

            if (startXDir >= 0.50)
            {
                blockXSpeed = -blockSpeed;
            }
            else
            {
                blockXSpeed = blockSpeed;
            }

            double startYDir = random.NextDouble() - 0.01;

            if (startYDir >= 0.50)
            {
                blockYSpeed = -blockSpeed;
            }
            else
            {
                blockYSpeed = blockSpeed;
            }

            blockPosition.X = (safeBounds.Left + safeBounds.Width - blockTexture.Width) / 2;
            blockPosition.Y = (safeBounds.Height - blockTexture.Height) / 2;
        }

        public static char GetKeyboardChars()
        {
            Keys[] pressedKeys = Keyboard.GetState().GetPressedKeys();

            for (int i = 0; i < pressedKeys.Length; i++)
            {
                int keyNumber = (int)pressedKeys[i];

                if (keyNumber >= 65 && keyNumber <= 90)
                    return (char)(keyNumber + 32);

                if (keyNumber == 32) //if it's a space, return it too
                    return (char)keyNumber;
            }

            return (char)0;
        }

        public void ExecuteCommand(string command)
        {
            string entered = command.ToLower();

            switch (entered)
            {
                case "background blue":
                {
                    background = Color.DodgerBlue;
                    break;
                }
                case "background red":
                {
                    background = Color.Crimson;
                    break;
                }
                case "background green":
                {
                    background = Color.ForestGreen;
                    break;
                }
                case "background gold":
                {
                    background = Color.Gold;
                    break;
                }
                case "background white":
                {
                    background = Color.WhiteSmoke;
                    break;
                }
                case "ai on":
                {
                    aiOn = true;
                    break;
                }
                case "ai off":
                {
                    aiOn = false;
                    break;
                }
                default:
                {
                    break;
                }
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(background);
            Viewport viewport = graphics.GraphicsDevice.Viewport;
            // TODO: Add your drawing code here
            spriteBatch.Begin();

            int xCoord = (safeBounds.Left + safeBounds.Width - centerLineDash.Width) / 2;

            for (int i = 0; i < viewport.Height; i += centerLineDash.Height + 4)
            {
                Vector2 coord = new Vector2(xCoord, i);
                spriteBatch.Draw(centerLineDash, coord, Color.White);             
            }

            if (consoleDisplaying)
            {
                Texture2D rect = new Texture2D(graphics.GraphicsDevice, safeBounds.Width, 100);

                Color[] data = new Color[safeBounds.Width * 100];
                for (int i = 0; i < data.Length; ++i) data[i] = new Color(40, 40, 40, 100);
                rect.SetData(data);

                Vector2 coor = new Vector2(safeBounds.Left, safeBounds.Bottom - 100);
                spriteBatch.DrawString(consoleFont,">> " + test, coor, Color.Black);
                spriteBatch.Draw(rect, coor, Color.White);
            }

            string p1Score = player1Score.ToString();
            spriteBatch.DrawString(scoreFont, p1Score, player1ScorePos, Color.DarkGray);
            string p2Score = player2Score.ToString();
            spriteBatch.DrawString(scoreFont, p2Score, player2ScorePos, Color.DarkGray);

            // Draw person
            spriteBatch.Draw(player1Texture, player1Position, Color.White);

            spriteBatch.Draw(player2Texture, player2Position, Color.White);

            // Draw blocks
            spriteBatch.Draw(blockTexture, blockPosition, Color.White);

            if (gameStart)
            {
                infoText = "First to 3.\nPress Spacebar or A to begin.";
            }
            else if (gameOver)
            {
                infoText = champ + " is champion!\nPress Spacebar or A to play again.";
            }
            else if (scored)
            {
                infoText = "Press Spacebar or A to continue.";
            }

            if (gameOver || gameStart || scored)
            {
                spriteBatch.DrawString(infoFont, infoText, new Vector2(safeBounds.Left + (safeBounds.Width / 5), safeBounds.Height / 2 - 50), Color.DarkGray);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
