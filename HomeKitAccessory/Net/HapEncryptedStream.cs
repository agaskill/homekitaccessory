using System;
using System.IO;
using NLog;

namespace HomeKitAccessory.Net
{
    class HapEncryptedStream : Stream
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private Stream baseStream;
        private Sodium.Key readKey;
        private Sodium.Key writeKey;
        private byte[] readBuffer = null;
        private int readBufferPos = 0;
        private ulong readCounter;
        private ulong writeCounter;
        private bool readEOF;

        public HapEncryptedStream(Stream stream, Sodium.Key readKey, Sodium.Key writeKey)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (readKey == null) throw new ArgumentNullException(nameof(readKey));
            if (writeKey == null) throw new ArgumentNullException(nameof(writeKey));

            this.baseStream = stream;
            this.readKey = readKey;
            this.writeKey = writeKey;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new System.NotImplementedException();

        public override long Position { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public override void Flush()
        {
            baseStream.Flush();
        }

        private void ReadNextFrame()
        {
            logger.Debug("Reading next encrypted frame");
            byte[] frameLengthHeader = new byte[2];
            int readLength = baseStream.Read(frameLengthHeader, 0, 2);
            if (readLength == 0) {
                Cleanup();
                return;
            }
            else if (readLength == 1) {
                readLength = baseStream.Read(frameLengthHeader, 1, 1);
                if (readLength == 0) {
                    Cleanup();
                    throw new EndOfStreamException();
                }
            }

            int frameLength = BitConverter.ToInt16(frameLengthHeader, 0);
            logger.Debug("Frame length is {0}", frameLength);
            byte[] frame = new byte[frameLength + 16];
            int remaining = frame.Length;
            while (remaining > 0) {
                readLength = baseStream.Read(frame, frame.Length - remaining, remaining);
                if (readLength == 0)
                {
                    Cleanup();
                    throw new EndOfStreamException();
                }
                remaining -= readLength;
            }

            byte[] decrypted = Sodium.Decrypt(frame, frameLengthHeader, ReadCounterNonce(), readKey);
            if (decrypted == null)
            {
                logger.Error("Frame failed decryption");
                Cleanup();
                throw new InvalidDataException();
            }

            readBuffer = decrypted;
        }

        private void Cleanup()
        {
            readEOF = true;
            baseStream.Dispose();
            baseStream = null;
        }

        private byte[] CounterNonce(ulong counterValue)
        {
            var nonce = new byte[12];
            int i = 4;
            do {
                nonce[i] = (byte)(counterValue & 255);
                counterValue >>= 8;
                i++;
            } while (counterValue > 0);
            return nonce;
        }

        private byte[] ReadCounterNonce()
        {
            return CounterNonce(readCounter++);
        }

        private byte[] WriteCounterNonce()
        {
            return CounterNonce(writeCounter++);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (readEOF) return 0;

            if (readBuffer == null)
            {
                ReadNextFrame();
                if (readEOF) return 0;
            }
            
            int available = readBuffer.Length - readBufferPos;
            int toRead = Math.Min(available, count);
            Array.Copy(readBuffer, readBufferPos, buffer, offset, toRead);
            readBufferPos += toRead;
            if (readBufferPos == readBuffer.Length)
            {
                readBuffer = null;
                readBufferPos = 0;
            }

            return toRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new System.NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                var toWrite = (short)Math.Min(1024, count);
                var lengthHeader = BitConverter.GetBytes(toWrite);
                byte[] encbuff;
                if (offset == 0 && count == buffer.Length && toWrite == count)
                {
                    encbuff = buffer;
                }
                else
                {
                    encbuff = new byte[toWrite];
                    Array.Copy(buffer, offset, encbuff, 0, toWrite);
                }
                var encdata = Sodium.Encrypt(encbuff, lengthHeader, WriteCounterNonce(), writeKey);
                baseStream.Write(lengthHeader, 0, lengthHeader.Length);
                baseStream.Write(encdata, 0, encdata.Length);

                count -= toWrite;
                offset += toWrite;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (baseStream != null)
                {
                    baseStream.Dispose();
                }
            }
        }
    }
}