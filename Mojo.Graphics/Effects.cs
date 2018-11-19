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
        private EffectParameter _pRange;
        private EffectParameter _pIntensity;
        private EffectParameter _pPosition;
        private EffectParameter _pWorldViewProj;
        private EffectParameter _pShadowMap;
        private EffectParameter _pInvTexSize;

        public LightEffect(Effect cloneSource) : base(cloneSource)
        {
            _pRange = Parameters["radius"];
            _pIntensity = Parameters["intensity"];
            _pPosition = Parameters["lightPos"];
            _pWorldViewProj = Parameters["WorldViewProj"];
            _pShadowMap = Parameters["shadowMapSampler"];
            _pInvTexSize = Parameters["inv_tex_size"];
        }

        public Vector2 InvTexSize
        {
            set
            {
                _pInvTexSize.SetValue(value);
            }
        }
        public Matrix WorldViewProj
        {
            get
            {
                return _pWorldViewProj.GetValueMatrix();
            }
            set
            {
                _pWorldViewProj.SetValue(value);
            }
        }
        public float Range
        {
            get
            {
                return _pRange.GetValueSingle();
            }
            set
            {
                _pRange.SetValue(value);
            }
        }
        public float Intensity
        {
            get
            {
                return _pIntensity.GetValueSingle();
            }
            set
            {
                _pIntensity.SetValue(value);
            }
        }
        public Vector2 Position
        {
            get
            {
                return _pPosition.GetValueVector2();
            }
            set
            {
                _pPosition.SetValue(value);
            }
        }
        public Texture2D Shadowmap
        {
            set
            {
                _pShadowMap.SetValue(value);
            }
            get
            {
                return _pShadowMap.GetValueTexture2D();
            }
        }
    }

    class SpotLightEffect : LightEffect
    {
        private EffectParameter _pInner;
        private EffectParameter _pOuter;
        private EffectParameter _pLightDir;

        public SpotLightEffect(Effect cloneSource) : base(cloneSource)
        {
            _pInner = Parameters["inner"];
            _pOuter = Parameters["outer"];
            _pLightDir = Parameters["lightDir"];
        }

        public Vector2 LightDir
        {
            set
            {
                _pLightDir.SetValue(value);
            }
        }
        public float Inner
        {
            get
            {
                return _pInner.GetValueSingle();
            }
            set
            {
                _pInner.SetValue(value);
            }
        }
        public float Outer
        {
            get
            {
                return _pOuter.GetValueSingle();
            }
            set
            {
                _pOuter.SetValue(value);
            }
        }
    }

    class PointLightEffect : LightEffect
    {
        public PointLightEffect(Effect cloneSource) : base(cloneSource)
        {
        }
    }
}
