using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Mojo.Graphics;
using System;

namespace Example
{
    class Spark
    {
        static System.Random _random = new Random();

        public static float Rnd(float min = 0.0f, float max = 1.0f)
        {
            return (float)_random.Next((int)(min * 100), (int)(max * 100)) * 0.01f;
        }

        public float x, y, vx, vy;
        public Color color;

        public Spark(float x, float y)
        {
            this.x = x;
            this.y = y;
            this.color = new Color(Rnd(), Rnd() / 2, Rnd() / 4, 1);
            var an = Rnd() * (float)MathHelper.Pi * 2;
            var r = Rnd(1, 5);
            this.vx = (float)System.Math.Cos(an) * r;
            this.vy = (float)Math.Sin(an) * r;
        }

        public void Render(Canvas canvas)
        {
            vy += 0.1f;
            x += vx;
            y += vy;
            canvas.Color = color;
            canvas.AddPointLight(x, y, 96, 4 );
        }
    }

    public class Game1 : MojoGame
    {
        private const float RAD_TO_DEG = 57.2957795130823208767981548141052f;

        private Image _floor;

        List<Spark> _sparks = new List<Spark>();

        public Game1() : base(640, 480, false)
        {
            Content.RootDirectory = "Content";
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            base.LightingEnabled = true;
            _floor = new Image("Images/tiles", "Images/tiles_n", "Images/tiles_s")
            {
                Specularity = 1
            };
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            var mouse = Mouse.GetState();

            Canvas.BeginLighting();

            // Set Ambient color used for lighting
            Canvas.TextureFilteringEnabled = true;
            Canvas.AmbientColor = new Color(0,0,0);
            Canvas.Clear(Color.Red);
            Canvas.BlendMode = BlendMode.Alpha;
            Canvas.ShadowEnabled = false;
            Canvas.SpecularEnabled = false;

            // Draw background
            Canvas.Color = Color.White;
            for (int x = 0; x < Width ; x += _floor.Width)
            {
                for (int y = 0; y < Height; y += _floor.Height)
                {
                    Canvas.DrawImage(_floor, x, y);
                }
            }

            if (mouse.LeftButton == ButtonState.Pressed)
            {
                _sparks.Add(new Spark(mouse.X, mouse.Y));
            }

            // Draw sparks
            foreach (var s in _sparks)
            {
                s.Render(this.Canvas);
            }

            // add light
            Canvas.Color = Color.White;
            Canvas.AddPointLight(mouse.X, mouse.Y, 320,2, 12);

            Canvas.EndLighting();


            base.Draw(gameTime);
        }


    }
}
