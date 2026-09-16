using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Gumps
{
    public sealed class Gump
    {
        private readonly TextureAtlas _atlas;
        private readonly SpriteInfo[] _spriteInfos;

        /// <summary>
        /// Indices already found to have nothing behind them.
        ///
        /// A null Texture is how this cache says "not loaded yet", so an index the files
        /// do not contain was asked for again on every single call - and callers that
        /// log a miss logged it every time with it. A shard whose items use paperdoll
        /// graphics the client does not ship produced over a thousand error lines in one
        /// session that way, each one a write to disk. The gump files do not change while
        /// the client runs, so a miss is final and worth remembering.
        /// </summary>
        private readonly bool[] _known_missing;

        private readonly PixelPicker _picker = new PixelPicker();

        public Gump(GraphicsDevice device)
        {
            _atlas = new TextureAtlas(device, 4096, 4096, SurfaceFormat.Color);
            _spriteInfos = new SpriteInfo[GumpsLoader.Instance.Entries.Length];
            _known_missing = new bool[_spriteInfos.Length];
        }

        /// <summary>Atlas pages this cache has taken from the device. Each is
        /// width x height x 4 bytes and is never given back, so it is the number
        /// worth having when the card stops answering.</summary>
        public int AtlasPages => _atlas.TexturesCount;

        public ref readonly SpriteInfo GetGump(uint idx)
        {
            if (idx >= _spriteInfos.Length)
                return ref SpriteInfo.Empty;

            ref var spriteInfo = ref _spriteInfos[idx];

            if (spriteInfo.Texture == null)
            {
                if (_known_missing[idx])
                {
                    return ref spriteInfo;
                }

                var gumpInfo = ExternalImageLoader.Instance.LoadGumpTexture(idx);
                bool loadedFromPNG = gumpInfo.Pixels != null && !gumpInfo.Pixels.IsEmpty;

                if (gumpInfo.Pixels == null || gumpInfo.Pixels.IsEmpty)
                {
                    gumpInfo = GumpsLoader.Instance.GetGump(idx);
                }
                if (!gumpInfo.Pixels.IsEmpty)
                {
                    spriteInfo.Texture = _atlas.AddSprite(
                        gumpInfo.Pixels,
                        gumpInfo.Width,
                        gumpInfo.Height,
                        out spriteInfo.UV
                    );

                    _picker.Set(idx, gumpInfo.Width, gumpInfo.Height, gumpInfo.Pixels);

                    // Clear the pixel cache from PNG Loader since it's now in the atlas
                    if (loadedFromPNG)
                    {
                        ExternalImageLoader.Instance.ClearGumpPixelCache(idx);
                    }

                    // AddSprite refuses a sprite it cannot pack and answers null, which
                    // reads here exactly like "not loaded yet". Without this the refusal
                    // would be retried for the life of the client.
                    if (spriteInfo.Texture == null)
                    {
                        _known_missing[idx] = true;
                    }
                }
                else
                {
                    _known_missing[idx] = true;
                }
            }

            return ref spriteInfo;
        }

        public bool PixelCheck(uint idx, int x, int y, double scale = 1f) => _picker.Get(idx, x, y, scale: scale);
    }
}
