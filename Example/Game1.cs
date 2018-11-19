using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Mojo.Graphics;

namespace Example
{
    public class Game1 : MojoGame
    {
        private const float RAD_TO_DEG = 57.2957795130823208767981548141052f;

        private Image _floor;
        private Image _logo;
        private float _viewRot;
        private float _lightRot;

        public Game1() : base(640,480,false)
        {
            Content.RootDirectory = "Content";
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            base.LightingEnabled = true;

            _floor = new Image("Images/tiles");
            _logo = new Image("Images/logo");
            _logo.ShadowCaster = new ShadowCaster(_logo.Width / 2, 24);
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
          
            _viewRot += 0.001f;
            _lightRot += 0.02f;

            // Set Ambient color used for lighting
            Canvas.TextureFilteringEnabled = true;
            Canvas.AmbientColor = new Color(32, 32, 32);
            Canvas.Clear(Color.Black);

            Canvas.Translate(Width / 2, Height / 2);
            Canvas.Scale((float)System.Math.Sin(_viewRot * 3) * 0.25f + 1.25f, (float)System.Math.Cos(_viewRot * 5) * 0.25f + 1.25f);
            Canvas.Rotate(_viewRot* RAD_TO_DEG);
            Canvas.Translate(-Width / 2, -Height / 2);

            // Draw background
            Canvas.Color = Color.White;
            for (int x = 0; x < Width; x += _floor.Width)
            {
                for (int y = 0; y < Height; y += _floor.Height)
                {
                    Canvas.DrawImage(_floor, x, y);
                }
            }

            // Draw sprites
            for (float an = 0; an < System.Math.PI * 2; an += (float)System.Math.PI * 2 / 8)
            {
                float xx = Width / 2 + (float)System.Math.Cos(an) * Width / 4;
                float yy = Height / 2 + (float)System.Math.Sin(an) * Height / 4;
                Canvas.DrawImage(_logo, xx, yy);
            }

            var mat = Canvas.Matrix;
            Canvas.ResetMatrix();

            // add lights
            var rnd = new System.Random(0);
            for(int i = 0; i < 4; ++i)
            {
                Canvas.Color = new Color(rnd.Next(128, 255), rnd.Next(128, 255), rnd.Next(128, 255));
                Canvas.AddSpotLight(mouse.X, mouse.Y, _lightRot * RAD_TO_DEG + i * 90, 512, 15, 35, 4, 16);
            }

            Canvas.SetMatrix(mat);

            base.Draw(gameTime);
        }
    }
}
