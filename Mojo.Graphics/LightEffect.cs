using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mojo.Graphics
{
    class LightEffect : Effect
    {
        internal enum EffectDirtyFlags
        {
            Light = 1,
            ShaderIndex = 128,
            All = -1
        };

        EffectDirtyFlags dirtyFlags = EffectDirtyFlags.All;

        private bool _normalmapEnabled = true;
        private bool _shadowEnabled = true;
        private bool _useSpotLight = false;
        private float _outer;
        private float _inner;
        private float _range;
        private float _intensity;
        private float _depth;
        private Vector2 _position;
        private Vector2 _lightDir;

        private EffectParameter _pRange;
        private EffectParameter _pDepth;
        private EffectParameter _pIntensity;
        private EffectParameter _pPosition;
        private EffectParameter _pWorldViewProj;
        private EffectParameter _pShadowMap;
        private EffectParameter _pInvTexSize;
        private EffectParameter _pNormalMap;
        private EffectParameter _pUseNormalmap;
        private EffectParameter _pInner;
        private EffectParameter _pOuter;
        private EffectParameter _pLightDir;


        public LightEffect(Effect cloneSource) : base(cloneSource)
        {
            _pDepth = Parameters["m_LightDepth"];
            _pRange = Parameters["radius"];
            _pIntensity = Parameters["intensity"];
            _pPosition = Parameters["lightPos"];
            _pWorldViewProj = Parameters["WorldViewProj"];
            _pShadowMap = Parameters["shadowMapSampler"];
            _pInvTexSize = Parameters["inv_tex_size"];
            _pNormalMap = Parameters["normalMapSampler"];
            _pUseNormalmap = Parameters["useNormalmap"];
            _pInner = Parameters["inner"];
            _pOuter = Parameters["outer"];
            _pLightDir = Parameters["lightDir"];
        }

        public bool NormalmapEnabled
        {
            get
            {
                return _normalmapEnabled;
            }
            set
            {
                if (_normalmapEnabled != value)
                {
                    _pUseNormalmap.SetValue(value ? 1.0f : 0.0f);
                    _normalmapEnabled = value;
                    dirtyFlags |= EffectDirtyFlags.ShaderIndex;
                }
            }
        }

        public Texture2D Normalmap
        {
            get { return _pNormalMap.GetValueTexture2D(); }
            set { _pNormalMap.SetValue(value); }
        }

        public bool ShadowEnabled
        {
            get
            {
                return _shadowEnabled;
            }
            set
            {
                if (_shadowEnabled != value)
                {
                    _shadowEnabled = value;
                    dirtyFlags |= EffectDirtyFlags.ShaderIndex;
                }
            }
        }

        public bool UseSpotLight
        {
            get
            {
                return _useSpotLight;
            }
            set
            {
                if (_useSpotLight != value)
                {
                    _useSpotLight = value;
                    dirtyFlags |= EffectDirtyFlags.ShaderIndex;
                }
            }
        }

        public Texture2D Shadowmap
        {
            get { return _pShadowMap.GetValueTexture2D(); }
            set { _pShadowMap.SetValue(value); }
        }

        public Vector2 InvTexSize
        {
            get { return _pInvTexSize.GetValueVector2(); }
            set { _pInvTexSize.SetValue(value); }
        }

        public Matrix WorldViewProj
        {
            get { return _pWorldViewProj.GetValueMatrix();}
            set { _pWorldViewProj.SetValue(value); }
        }

        public float Range
        {
            get { return _range; }
            set { _range = value; }
        }

        public float Intensity
        {
            get { return _intensity; }
            set { _intensity = value; }
        }

        public Vector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public float Depth
        {
            get { return _depth; }
            set { _depth = value; }
        }

        public Vector2 LightDir
        {
            get { return _lightDir; }
            set { _lightDir = value; }
        }

        public float Inner
        {
            get { return _inner; }
            set { _inner = value; }
        }

        public float Outer
        {
            get { return _outer; }
            set { _outer = value; }
        }

        protected override void OnApply()
        {
            _pRange.SetValue(_range);
            _pIntensity.SetValue(_intensity);
            _pPosition.SetValue(_position);
            _pDepth.SetValue(_depth);
            _pLightDir.SetValue(_lightDir);
            _pInner.SetValue(_inner);
            _pOuter.SetValue(_outer);


            if ((dirtyFlags & EffectDirtyFlags.ShaderIndex) != 0)
            {
                int shaderIndex = 0;

                if (_shadowEnabled)
                    shaderIndex += 1;

                if (_normalmapEnabled)
                    shaderIndex += 2;

                if (_useSpotLight)
                    shaderIndex+=4;

                dirtyFlags &= ~EffectDirtyFlags.ShaderIndex;

                CurrentTechnique = Techniques[shaderIndex];
            }
        }

    }
}
