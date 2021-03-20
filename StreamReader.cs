using System.IO;
using System.Text;

namespace espjs
{
    internal class StreamReader : System.IO.StreamReader
    {
        public StreamReader(Stream stream, Encoding encoding) : base(stream, encoding)
        {
        }
    }
}