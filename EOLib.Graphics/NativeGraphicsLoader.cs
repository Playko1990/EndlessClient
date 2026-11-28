using System;
using AutomaticTypeMapper;

namespace EOLib.Graphics
{
    [MappedType(BaseType = typeof(INativeGraphicsLoader), IsSingleton = true)]
    public class NativeGraphicsLoader : INativeGraphicsLoader
    {
        private readonly IPEFileCollection _modules;

        public NativeGraphicsLoader(IPEFileCollection modules)
        {
            _modules = modules;
        }

        public ReadOnlyMemory<byte> LoadGFX(GFXTypes file, int resourceValue)
        {
            try
            {
                if (resourceValue < 0)
                    return Array.Empty<byte>();

                var fileBytes = _modules[file].GetEmbeddedBitmapResourceByID(resourceValue + 100);
                return fileBytes.Length == 0 ? Array.Empty<byte>() : fileBytes;
            }
            catch
            {
                // Missing / corrupt custom art must never take down login or inventory redraw.
                // Catch everything (not only ArgumentException) — PELoader can throw other types.
                return Array.Empty<byte>();
            }
        }
    }
}
