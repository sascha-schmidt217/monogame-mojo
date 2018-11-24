using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mojo.Graphics
{
    class DefaultEffect : Effect
    {
        private EffectParameter _pDiffuseTexture;
        private EffectParameter _pNormalTexture;
        private EffectParameter _pSpecularTexture;
        private EffectParameter _pWorldViewProjection;

        private bool _textureEnabled = true;
        private bool _normalEnabled = true;
        private bool _specularEnabled = true;
        private bool _updateShaderIndex = true;

        public DefaultEffect(Effect cloneSource) : base(cloneSource)
        {
            _pDiffuseTexture = Parameters["DiffuseSampler"];
            _pNormalTexture = Parameters["NormalSampler"];
            _pSpecularTexture = Parameters["SpecularSampler"];
            _pWorldViewProjection = Parameters["WorldViewProjection"];
        }

        public Matrix WorldViewProjection
        {
            get { return _pWorldViewProjection.GetValueMatrix(); }
            set { _pWorldViewProjection.SetValue(value); }
        }

        public Texture2D Texture
        {
            get { return _pDiffuseTexture.GetValueTexture2D(); }
            set { _pDiffuseTexture.SetValue(value); }
        }

        public Texture2D Normalmap
        {
            get { return _pNormalTexture.GetValueTexture2D(); }
            set { _pNormalTexture.SetValue(value); }
        }

        public Texture2D Specularmap
        {
            get { return _pSpecularTexture.GetValueTexture2D(); }
            set { _pSpecularTexture.SetValue(value); }
        }

        public bool TextureEnabled
        {
            get
            {
                return _textureEnabled;
            }
            set
            {
                if(_textureEnabled != value )
                {
                    _textureEnabled = value;
                    _updateShaderIndex = true;
                }
            }
        }


        public bool NormalEnabled
        {
            get
            {
                return _normalEnabled;
            }
            set
            {
                if (_normalEnabled != value)
                {
                    _normalEnabled = value;
                    _updateShaderIndex = true;
                }
            }
        }

        public bool SpecularEnabled
        {
            get
            {
                return _specularEnabled;
            }
            set
            {
                if (_specularEnabled != value)
                {
                    _specularEnabled = value;
                    _updateShaderIndex = true;
                }
            }
        }

        protected override void OnApply()
        {
            if (_updateShaderIndex)
            {
                int shaderIndex = 0;

                if (_normalEnabled)
                {
                    shaderIndex += 1;

                    if(_specularEnabled)
                    {
                        shaderIndex += 1;
                    }
                }

                if (TextureEnabled)
                {
                    shaderIndex += 3;
                }

                _updateShaderIndex = false;

                CurrentTechnique = Techniques[shaderIndex];
            }
        }

    }
}
