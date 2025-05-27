using System;
using System.IO;
using System.Text;

namespace Assets.Scripts.Robot.Logger
{
    public class LogOutputStream : Stream
    {
        private readonly MemoryStream _buffer = new MemoryStream();

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _buffer.Length;
        public override long Position
        {
            get => _buffer.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            // ничего не делаем
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            // копируем байты во внутренний буфер
            _buffer.Write(buffer, offset, count);

            // смотрим, есть ли символ перевода строки
            var bytes = _buffer.ToArray();
            var text = Encoding.UTF8.GetString(bytes);
            if (text.Contains("\n"))
            {
                // логируем всё до последнего '\n'
                var parts = text.Split(new[] { '\n' }, StringSplitOptions.None);
                for (int i = 0; i < parts.Length - 1; i++)
                    Logger.Instance.Log(parts[i]);
                // сбрасываем буфер, оставив «хвост» после последнего '\n'
                var remainder = parts[parts.Length - 1];
                _buffer.SetLength(0);
                var remBytes = Encoding.UTF8.GetBytes(remainder);
                _buffer.Write(remBytes, 0, remBytes.Length);
            }
        }

        public override void WriteByte(byte value) =>
            Write(new[] { value }, 0, 1);
    }
}
