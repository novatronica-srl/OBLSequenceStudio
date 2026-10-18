#nullable disable
namespace OBLSequenceStudio.STBootLib
{
    /// <summary>Immutable transfer counters reported after bootloader write chunks.</summary>
    public class STBootProgress
    {
        /* total number of bytes */
        /// <summary>Total requested transfer size in bytes; the field name preserves the embedded library API.</summary>
        public readonly int bytesTotal;
        /* number of bytes processed */
        /// <summary>Cumulative bytes transferred so far; the field name preserves the embedded library API.</summary>
        public readonly int bytesProcessed;

        /// <summary>Captures cumulative and total byte counts for one progress notification.</summary>
        public STBootProgress(int bytesProcessed, int bytesTotal)
        {
            /* set the number of bytes processed */
            this.bytesProcessed = bytesProcessed;
            /* set the total number of bytes */
            this.bytesTotal = bytesTotal;
        }
    }
}


