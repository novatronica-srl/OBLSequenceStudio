#nullable disable
namespace OBLSequenceStudio.STBootLib
{
    public class STBootException : Exception
    {
        /* constructor */
        public STBootException()
        {
        }

        /* with message */
        public STBootException(string message)
            : base(message)
        {
        }
    }
}


