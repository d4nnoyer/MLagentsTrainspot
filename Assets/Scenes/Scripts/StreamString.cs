using System.IO;
using System.Text;

namespace Assets
{
    public class StreamString
    {
        private Stream io;
        private UnicodeEncoding strEncode;

        public StreamString(Stream io)
        {
            this.io = io;
            strEncode = new UnicodeEncoding();
        }

        public string ReadString()
        {
            int len = io.ReadByte() * 256;
            len += io.ReadByte();
            byte[] inBuffer = new byte[len];
            io.Read(inBuffer, 0, len);

            return strEncode.GetString(inBuffer);
        }

        public int WriteString(string outString)
        {
            byte[] outBuffer = strEncode.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > ushort.MaxValue) len = ushort.MaxValue;
            io.WriteByte((byte)(len / 256));
            io.WriteByte((byte)(len & 255));
            io.Write(outBuffer, 0, len);
            io.Flush();

            return outBuffer.Length + 2;
        }
    }
}