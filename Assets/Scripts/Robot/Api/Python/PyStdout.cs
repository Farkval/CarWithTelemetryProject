namespace Assets.Scripts.Robot.Api.Python
{
    using Assets.Scripts.Utils;
    using System.Text;

    public class PyStdout
    {
        private readonly StringBuilder _buf = new();

        public void write(string text)
        {
            _buf.Append(text);

            int nl;
            while ((nl = _buf.ToString().IndexOf('\n')) != -1)
            {
                var line = _buf.ToString(0, nl).TrimEnd('\r');
                if (line.Length > 0)
                    Logger.Log(line);

                _buf.Remove(0, nl + 1);
            }
        }

        public void flush() { }
    }

}
