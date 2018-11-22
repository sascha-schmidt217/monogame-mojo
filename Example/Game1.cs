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
        private Image _logo2;
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

            _floor = new Image("Images/tiles", "Images/tiles_n");
            _logo = new Image("Images/logo", "Images/logo_n", 24,24);
            _logo2 = new Image("Images/logo2", "Images/logo2_n", 24,24);
            _logo.ShadowCaster = new ShadowCaster(_logo.Width / 2, 24);
            _logo2.ShadowCaster = new ShadowCaster(new Rectangle(-_logo2.Width/2, -_logo2.Height/2, _logo2.Width, _logo2.Height));
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
          
            _viewRot += 0.004f;
            _lightRot += 0.02f;

            Canvas.BeginLighting();

            // Set Ambient color used for lighting
            Canvas.TextureFilteringEnabled = true;
            Canvas.AmbientColor = new Color(32,32,32);
            Canvas.Clear(Color.Red);

            // Draw background
            Canvas.Color = Color.White;
            for (int x = -_floor.Width; x < Width + _floor.Width; x += _floor.Width)
            {
                for (int y = -_floor.Height; y < Height + _floor.Height; y += _floor.Height)
                {
                    Canvas.DrawImage(_floor, x, y);
                }
            }

            // Draw sprites
            int k = 0;
            for (float an = 0; an < System.Math.PI * 2; an += (float)System.Math.PI * 2 / 8)
            {
                float xx = Width / 2 + (float)System.Math.Cos(an) * 128;
                float yy = Height / 2 + (float)System.Math.Sin(an) * 128;
                Canvas.DrawImage((k++) % 2 == 0 ? _logo : _logo2, xx, yy);
            }

            // add light
            Canvas.AddPointLight( mouse.X, mouse.Y, 512, 1,12);
          
            Canvas.EndLighting();
            base.Draw(gameTime);
        }
    }
}
