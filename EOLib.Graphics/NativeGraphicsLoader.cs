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
            var fileBytes = ReadOnlyMemory<byte>.Empty;
            try
            {
                fileBytes = _modules[file].GetEmbeddedBitmapResourceByID(resourceValue + 100);
            }
            catch (ArgumentException)
            {
                // Missing custom item/NPC/etc. art is common while pubs are ahead of gfx.
                // Return empty so the manager can show a 1x1 placeholder instead of crashing
                // Debug builds (spawn-to-inventory used to hard-crash here).
                return Array.Empty<byte>();
            }

            if (fileBytes.Length == 0)
                return Array.Empty<byte>();

            return fileBytes;
        }
    }
}
