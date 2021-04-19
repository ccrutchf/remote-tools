using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTools.Cli.Core
{
    public class ConsoleErrorWriterDecorator : TextWriter
    {
        private TextWriter m_OriginalConsoleStream;

        public ConsoleErrorWriterDecorator(TextWriter consoleTextWriter)
        {
            m_OriginalConsoleStream = consoleTextWriter;
        }

        public override void WriteLine(string value)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            m_OriginalConsoleStream.WriteLine(value);

            Console.ForegroundColor = originalColor;
        }

        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }

        public static void SetToConsole()
        {
            Console.SetError(new ConsoleErrorWriterDecorator(Console.Error));
        }
    }
}
