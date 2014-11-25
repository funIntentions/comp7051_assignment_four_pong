#if WINDEMO
#define WINDEMO
#endif

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
using Microsoft.Xna.Framework.Storage;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

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

        int[] topTenScores;
        int startTime;

        IAsyncResult result;

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

        public struct SaveGameData
        {
            public int one;
            public int two;
            public int three;
            public int four;
            public int five;
            public int six;
            public int seven;
            public int eight;
            public int nine;
            public int ten;
        }

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
            topTenScores = new int[10];
            startTime = -1;

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

            LoadHighScores();

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
        /// Updates the current high score.
        /// </summary>
        /// <param name="gameTime">The total time until the chickens were all caught.</param>
        private void UpdateHighScore(GameTime gameTime)
        {
            // Write the new high score if we beat it
            float seconds = ((float)gameTime.TotalGameTime.TotalMilliseconds - startTime) / 1000;

            if (seconds <= 0) seconds = 0.0001f;

            int score = (int)((1000 / seconds) * 100);

            //showHighScoreMsg = true;
            result = StorageDevice.BeginShowSelector(
                        PlayerIndex.One, null, null);
            StorageDevice device = StorageDevice.EndShowSelector(result);
            if (device != null && device.IsConnected)
            {
                SaveGame(device, score);
            }            
        }

        /// <summary>
        /// Gets the current high scores.
        /// </summary>
        private void LoadHighScores()
        {
            //showHighScoreMsg = true;
            result = StorageDevice.BeginShowSelector(
                        PlayerIndex.One, null, null);
            StorageDevice device = StorageDevice.EndShowSelector(result);
            if (device != null && device.IsConnected)
            {
                LoadGame(device);
            }

        }

        /// <summary>
        /// This method loads a serialized data object
        /// from the StorageContainer for this game.
        /// </summary>
        /// <param name="device"></param>
        private void LoadGame(StorageDevice device)
        {
            // Open a storage container.
            IAsyncResult result =
                device.BeginOpenContainer("PongHighscores", null, null);

            // Wait for the WaitHandle to become signaled.
            result.AsyncWaitHandle.WaitOne();

            StorageContainer container = device.EndOpenContainer(result);

            // Close the wait handle.
            result.AsyncWaitHandle.Close();

            string filename = "highscores" + ".sav";

            // Check to see whether the save exists.
            if (!container.FileExists(filename))
            {
                // If not, dispose of the container and return.
                container.Dispose();
                return;
            }

            // Open the file.
            Stream stream = container.OpenFile(filename, FileMode.Open);

#if WINDEMO
            // Read the data from the file.
            XmlSerializer serializer = new XmlSerializer(typeof(SaveGameData));
            SaveGameData data = (SaveGameData)serializer.Deserialize(stream);
#else
            using (StreamReader sr = new StreamReader(stream))
            {
                String input;
                int i = 0;
                while ((input = sr.ReadLine()) != null && i < 10)
                {
                    topTenScores[i] = Convert.ToInt32(input);
                    i++;
                }

                sr.Close();
            }
#endif

            // Close the file.
            stream.Close();

            // Dispose the container.
            container.Dispose();

#if WINDEMO
            // Report the data to the console.
            Debug.WriteLine("Name:     " + data.PlayerName);
            Debug.WriteLine("Level:    " + data.Level.ToString());
            Debug.WriteLine("Score:    " + data.Score.ToString());
            Debug.WriteLine("Position: " + data.AvatarPosition.ToString());
#endif
        }

        /// <summary>
        /// This method serializes a data object into
        /// the StorageContainer for this game.
        /// </summary>
        /// <param name="device"></param>
        private void SaveGame(StorageDevice device, int theScore)
        {
            // Create the data to save.
            SaveGameData data = new SaveGameData();
                        
            for (int i = 0; i < 10; i++)
            {
                if (topTenScores[i] < theScore)
                {
                    for (int j = 9; j != i; j--)
                    {
                        topTenScores[j] = topTenScores[j - 1];
                    }

                    topTenScores[i] = theScore;
                    break;
                }
            }

            int num = 0;
            data.one = topTenScores[num++];
            data.two = topTenScores[num++];
            data.three = topTenScores[num++];
            data.four = topTenScores[num++];
            data.five = topTenScores[num++];
            data.six = topTenScores[num++];
            data.seven = topTenScores[num++];
            data.eight = topTenScores[num++];
            data.nine = topTenScores[num++];
            data.ten = topTenScores[num++];
            
            // Open a storage container.
            IAsyncResult result =
                device.BeginOpenContainer("PongHighscores", null, null);

            // Wait for the WaitHandle to become signaled.
            result.AsyncWaitHandle.WaitOne();

            StorageContainer container = device.EndOpenContainer(result);

            // Close the wait handle.
            result.AsyncWaitHandle.Close();

            string filename = "highscores" + ".sav";

            // Check to see whether the save exists.
            if (container.FileExists(filename))
                // Delete it so that we can create one fresh.
                container.DeleteFile(filename);

            // Create the file.
            Stream stream = container.CreateFile(filename);


            #if WINDEMO
            // Convert the object to XML data and put it in the stream.
            XmlSerializer serializer = new XmlSerializer(typeof(SaveGameData));
            serializer.Serialize(stream, data);
            #else
            using (StreamWriter sw = new StreamWriter(stream))
            {
                sw.WriteLine(data.one);
                sw.WriteLine(data.two);
                sw.WriteLine(data.three);
                sw.WriteLine(data.four);
                sw.WriteLine(data.five);
                sw.WriteLine(data.six);
                sw.WriteLine(data.seven);
                sw.WriteLine(data.eight);
                sw.WriteLine(data.nine);
                sw.WriteLine(data.ten);
                sw.Close();
            }
            #endif

            // Close the file.
            stream.Close();

            // Dispose the container, to commit changes.
            container.Dispose();
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

            if (startTime < 0)
            {
                startTime = (int)gameTime.TotalGameTime.TotalMilliseconds;
            }

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
                        UpdateHighScore(gameTime);
                        startTime = -1;
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
                        UpdateHighScore(gameTime);
                        startTime = -1;
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
                infoText += "\n\n - High Scores - \n" +
                    "1. " + topTenScores[0] + "\n" +
                    "2. " + topTenScores[1] + "\n" +
                    "3. " + topTenScores[2] + "\n" +
                    "4. " + topTenScores[3] + "\n" +
                    "5. " + topTenScores[4] + "\n" +
                    "6. " + topTenScores[5] + "\n" +
                    "7. " + topTenScores[6] + "\n" +
                    "8. " + topTenScores[7] + "\n" +
                    "9. " + topTenScores[8] + "\n" +
                    "10. " + topTenScores[9];
            }
            else if (gameOver)
            {
                infoText = champ + " is champion!\nPress Spacebar or A to play again.\n";
                infoText += "\n\n - High Scores - \n" + 
                    "1. " + topTenScores[0] + "\n" + 
                    "2. " + topTenScores[1] + "\n" + 
                    "3. " + topTenScores[2] + "\n" + 
                    "4. " + topTenScores[3] + "\n" +
                    "5. " + topTenScores[4] + "\n" +
                    "6. " + topTenScores[5] + "\n" + 
                    "7. " + topTenScores[6] + "\n" + 
                    "8. " + topTenScores[7] + "\n" + 
                    "9. " + topTenScores[8] + "\n" + 
                    "10. " + topTenScores[9];
            }
            else if (scored)
            {
                infoText = "Press Spacebar or A to continue.";
            }

            if (gameOver || gameStart || scored)
            {
                spriteBatch.DrawString(infoFont, infoText, new Vector2(safeBounds.Left + (safeBounds.Width / 5), safeBounds.Height / 2 - 150), Color.DarkGray);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
