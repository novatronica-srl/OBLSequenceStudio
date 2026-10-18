#nullable disable
namespace OBLSequenceStudio.STBootLib
{
    /* response codes*/
    /// <summary>STM32 bootloader acknowledgement bytes used to accept or reject command phases.</summary>
    public enum STResps
    {
        /* command accepted */
        ACK = 0x79,
        /* command discarded */
        NACK = 0x1F,
    }
}


