#nullable disable
namespace OBLSequenceStudio.STBootLib
{
    /// <summary>Reports a bootloader command rejection or a command unsupported by the detected target.</summary>
    public class STBootException : Exception
    {
        /* constructor */
        /// <summary>Creates a bootloader protocol exception without additional context.</summary>
        public STBootException()
        {
        }

        /* with message */
        /// <summary>Creates a bootloader protocol exception with command-specific diagnostic text.</summary>
        public STBootException(string message)
            : base(message)
        {
        }
    }
}


