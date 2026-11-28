using System;
using System.Collections.Generic;
using System.Linq;
using EOLib.IO.Pub;

namespace EOLib.IO.Extensions
{
    public static class PubFileExtensions
    {
        /// <summary>
        /// Returns true when <paramref name="id"/> is a valid 1-based record index in the pub file.
        /// </summary>
        public static bool ContainsId<TRecord>(this IPubFile<TRecord> file, int id)
            where TRecord : class, IPubRecord, new()
            => file != null && id >= 1 && id <= file.Length;

        /// <summary>
        /// Try to read a record by ID. Returns false for out-of-range / missing IDs instead of throwing.
        /// </summary>
        public static bool TryGetRecord<TRecord>(this IPubFile<TRecord> file, int id, out TRecord record)
            where TRecord : class, IPubRecord, new()
        {
            if (!file.ContainsId(id))
            {
                record = null;
                return false;
            }

            record = file[id];
            return true;
        }

        /// <summary>
        /// Get an item record, or a 1x1 placeholder named "Unknown Item #id" when the client's
        /// dat001.eif is behind the server (common while adding custom items).
        /// </summary>
        public static EIFRecord GetItemOrUnknown(this IPubFile<EIFRecord> file, int id)
        {
            if (file.TryGetRecord(id, out var record))
                return record;

            return new EIFRecord(id, $"Unknown Item #{id}");
        }

        /// <summary>
        /// Merge the set of pub files into a single pub file.
        /// <para>The first pub file has subsequent pub files' records added to the end of its set of records.</para>
        /// <para>This operation creates a deep copy.</para>
        /// </summary>
        /// <typeparam name="TRecord">The record type.</typeparam>
        /// <param name="pubFiles">The list of files to merge.</param>
        /// <returns>The merged pub file.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="pubFiles"/> does not have at least one element.</exception>
        public static IPubFile<TRecord> Merge<TRecord>(IEnumerable<IPubFile<TRecord>> pubFiles)
            where TRecord : class, IPubRecord, new()
        {
            if (pubFiles.Count() < 1)
                throw new ArgumentException("pubFiles must have at least 1 element", nameof(pubFiles));

            if (pubFiles.Count() == 1)
                return pubFiles.ElementAt(0);

            var mergedFile = (IPubFile<TRecord>)pubFiles.ElementAt(0).Clone();

            var itemId = mergedFile.Length;
            foreach (var extraRecord in pubFiles.Skip(1).SelectMany(x => x))
                mergedFile = mergedFile.WithAddedRecord((TRecord)extraRecord.WithID(++itemId));

            return (IPubFile<TRecord>)mergedFile.WithTotalLength(pubFiles.Select(x => x.Length).Sum());
        }
    }
}
