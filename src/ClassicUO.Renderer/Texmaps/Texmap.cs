using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Texmaps
{
    public sealed class Texmap
    {
        private readonly TextureAtlas _atlas;
        private readonly SpriteInfo[] _spriteInfos;
        private readonly PixelPicker _picker = new PixelPicker();

        public Texmap(GraphicsDevice device)
        {
            _atlas = new TextureAtlas(device, 2048, 2048, SurfaceFormat.Color);
            _spriteInfos = new SpriteInfo[TexmapsLoader.Instance.Entries.Length];
        }

        /// <summary>Atlas pages this cache has taken from the device. Each is
        /// width x height x 4 bytes and is never given back, so it is the number
        /// worth having when the card stops answering.</summary>
        public int AtlasPages => _atlas.TexturesCount;

        public ref readonly SpriteInfo GetTexmap(uint idx)
        {
            if (idx >= _spriteInfos.Length)
                return ref SpriteInfo.Empty;

            ref var spriteInfo = ref _spriteInfos[idx];

            if (spriteInfo.Texture == null)
            {
                var texmapInfo = TexmapsLoader.Instance.GetTexmap(idx);
                if (!texmapInfo.Pixels.IsEmpty)
                {
                    spriteInfo.Texture = _atlas.AddSprite(
                        texmapInfo.Pixels,
                        texmapInfo.Width,
                        texmapInfo.Height,
                        out spriteInfo.UV
                    );

                    _picker.Set(idx, texmapInfo.Width, texmapInfo.Height, texmapInfo.Pixels);
                }
            }

            return ref spriteInfo;
        }
    }
}
