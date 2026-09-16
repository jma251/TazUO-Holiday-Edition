using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StbRectPackSharp;
using System;
using System.Collections.Generic;

namespace ClassicUO.Renderer
{
    public class TextureAtlas : IDisposable
    {
        private readonly int _width,
            _height;
        private readonly SurfaceFormat _format;
        private readonly GraphicsDevice _device;
        private readonly List<Texture2D> _textureList;
        private Packer _packer;

        public TextureAtlas(GraphicsDevice device, int width, int height, SurfaceFormat format)
        {
            _device = device;
            _width = width;
            _height = height;
            _format = format;

            _textureList = new List<Texture2D>();
        }

        public int TexturesCount => _textureList.Count;

        /// <summary>Said once per atlas, not once per frame, because the caller retries.</summary>
        private bool _warnedOversize;

        public unsafe Texture2D AddSprite(
            ReadOnlySpan<uint> pixels,
            int width,
            int height,
            out Rectangle pr
        )
        {
            // A sprite larger than the atlas can never be packed, and the loop below
            // has no other way out: PackRect fails against the fresh empty packer as
            // well, so it allocates another page - 4096x4096x4 is 64 MB for the art
            // and animation atlases - and tries again, and again, until the driver
            // stops answering. The exception then surfaces from inside
            // CreateNewTexture2D, which is where it looks like the fault is.
            //
            // Nothing upstream of here bounds the size. Animation frame dimensions are
            // read as Int16 from the animation files and checked only for <= 0, so any
            // value from 4097 to 32767 arrives here intact.
            //
            // A null texture is what every caller already uses to mean "not loaded",
            // so refusing the sprite is a sprite that does not draw rather than a
            // client that stops.
            if (width <= 0 || height <= 0 || width > _width || height > _height)
            {
                if (!_warnedOversize)
                {
                    _warnedOversize = true;

                    Utility.Logging.Log.Warn(
                        $"sprite {width}x{height} does not fit a {_width}x{_height} atlas and was skipped"
                    );
                }

                pr = Rectangle.Empty;

                return null;
            }

            var index = _textureList.Count - 1;

            // True once the page being packed into is one this call just made, and so
            // is empty. The dimension test above is the reason a sprite normally fails
            // to fit, but it reasons about the packer's arithmetic from outside it; if
            // that reasoning is ever wrong - a border the packer reserves, a rect the
            // exact size of the page - the loop below would run forever on a sprite
            // this method believed was fine. A sprite that will not go into an empty
            // page will not go into the next empty page either, so one is enough to
            // decide, and the loop cannot fail to end.
            bool onFreshPage = false;

            if (index < 0)
            {
                index = 0;
                CreateNewTexture2D();
                onFreshPage = true;
            }

            while (!_packer.PackRect(width, height, out pr))
            {
                if (onFreshPage)
                {
                    if (!_warnedOversize)
                    {
                        _warnedOversize = true;

                        Utility.Logging.Log.Warn(
                            $"sprite {width}x{height} would not pack into an empty {_width}x{_height} atlas page and was skipped"
                        );
                    }

                    pr = Rectangle.Empty;

                    return null;
                }

                CreateNewTexture2D();
                index = _textureList.Count - 1;
                onFreshPage = true;
            }

            Texture2D texture = _textureList[index];

            fixed (uint* src = pixels)
            {
                texture.SetDataPointerEXT(0, pr, (IntPtr)src, sizeof(uint) * width * height);
            }

            return texture;
        }

        /// <summary>
        /// Throws if the device refuses, having first said so somewhere useful. With
        /// sprite sizes bounded above, a refusal here is the device being gone rather
        /// than a bad request, and that is worth one clear line in the log instead of a
        /// stack trace pointing at whichever creature happened to walk on screen.
        /// </summary>
        private void CreateNewTexture2D()
        {
            Utility.Logging.Log.Trace($"creating texture: {_width}x{_height} {_format}");

            Texture2D texture;

            try
            {
                texture = new Texture2D(_device, _width, _height, false, _format);
            }
            catch (Exception ex)
            {
                GpuDeviceWatch.Report($"TextureAtlas page {_textureList.Count + 1} ({_width}x{_height} {_format})", ex);

                throw;
            }

            _textureList.Add(texture);

            _packer?.Dispose();
            _packer = new Packer(_width, _height);
        }

        public void SaveImages(string name)
        {
            for (int i = 0, count = TexturesCount; i < count; ++i)
            {
                var texture = _textureList[i];

                using (var stream = System.IO.File.Create($"atlas/{name}_atlas_{i}.png"))
                {
                    texture.SaveAsPng(stream, texture.Width, texture.Height);
                }
            }
        }

        public void Dispose()
        {
            foreach (Texture2D texture in _textureList)
            {
                if (!texture.IsDisposed)
                {
                    texture.Dispose();
                }
            }

            // Null when no sprite was ever added - CreateNewTexture2D is the only
            // thing that builds one, and with the guard above a client that only ever
            // saw oversized sprites would reach here without it.
            _packer?.Dispose();
            _textureList.Clear();
        }
    }
}
