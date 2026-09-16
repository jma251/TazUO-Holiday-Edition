using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Lights
{
    public sealed class Light
    {
        private readonly TextureAtlas _atlas;
        private readonly SpriteInfo[] _spriteInfos;

        public Light(GraphicsDevice device)
        {
            _atlas = new TextureAtlas(device, 2048, 2048, SurfaceFormat.Color);
            _spriteInfos = new SpriteInfo[LightsLoader.Instance.Entries.Length];
        }

        /// <summary>Atlas pages this cache has taken from the device. Each is
        /// width x height x 4 bytes and is never given back, so it is the number
        /// worth having when the card stops answering.</summary>
        public int AtlasPages => _atlas.TexturesCount;

        public ref readonly SpriteInfo GetLight(uint idx)
        {
            if (idx >= _spriteInfos.Length)
                return ref SpriteInfo.Empty;

            ref var spriteInfo = ref _spriteInfos[idx];

            if (spriteInfo.Texture == null)
            {
                var lightInfo = LightsLoader.Instance.GetLight(idx);
                if (!lightInfo.Pixels.IsEmpty)
                {
                    spriteInfo.Texture = _atlas.AddSprite(
                        lightInfo.Pixels,
                        lightInfo.Width,
                        lightInfo.Height,
                        out spriteInfo.UV
                    );
                }
            }

            return ref spriteInfo;
        }
    }
}
