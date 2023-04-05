using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintDump
{
    interface IEscapeSequence
    {
        void Read(FileStream input);
    }
    class EscapeSequence : IEscapeSequence
    {
        public EscapeSequence(string name, uint numArgs)
        {
            this.name = name;
            this.numArgs = numArgs;
        }

        public void Read(FileStream input)
        {
            Console.Write(name);
            for (uint i = 0; i < numArgs; i++)
            {
                Console.Write(" " + (byte)input.ReadByte());
            }
            Console.WriteLine();
        }

        private string name;
        private uint numArgs;
    }
    class CustomEscapeSequence : IEscapeSequence
    {
        private string name;
        private Reader reader;

        public delegate void Reader(FileStream input);
        public CustomEscapeSequence(string name, Reader reader)
        {
            this.name = name;
            this.reader = reader;
        }

        public void Read(FileStream input)
        {
            Console.Write(name);

            reader(input);
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            var fileStream = File.OpenRead(args[0]);

            var escapeSequences = new Dictionary<byte, IEscapeSequence>();
            escapeSequences.Add(0x40, new EscapeSequence("initialize", 0));
            escapeSequences.Add(0x50, new EscapeSequence("select 10 cpi", 0));
            escapeSequences.Add(0x3C, new EscapeSequence("unidirectional line", 0));
            escapeSequences.Add(0x55, new EscapeSequence("unidirectional", 1));
            escapeSequences.Add(0x2B, new EscapeSequence("set n/360-inch line spacing", 1));
            escapeSequences.Add(0x6C, new EscapeSequence("left margin", 1));
            escapeSequences.Add(0x51, new EscapeSequence("right margin", 1));
            escapeSequences.Add(0x4A, new EscapeSequence("n/180 inch line feed", 1));
            escapeSequences.Add(0x44, new CustomEscapeSequence("set horizontal tabs", input => { byte next; while ((next = (byte)input.ReadByte()) != 0) { Console.Write(" " + next); } Console.WriteLine(); }));
            escapeSequences.Add(0x24, new CustomEscapeSequence("set absolute position", input => { uint n1 = (byte)input.ReadByte(); uint n2 = (byte)input.ReadByte(); Console.WriteLine(" " + (n1 + 256 * n2)); }));
            escapeSequences.Add(0x78, new EscapeSequence("letter quality mode", 1));
            escapeSequences.Add(0x2A, new CustomEscapeSequence("graphics mode", input =>
            {
                byte m = (byte)input.ReadByte();
                byte n1 = (byte)input.ReadByte();
                byte n2 = (byte)input.ReadByte();

                Console.Write(" density: " + m);
                uint columns = n2;
                columns *= 256;
                columns += n1;
                Console.Write(" columns: " + columns);
                
                for (uint i = 0; i < columns; i++)
                {
                    uint column = (byte)input.ReadByte();
                    column <<= 8;
                    column += (byte)input.ReadByte();
                    column <<= 8;
                    column += (byte)input.ReadByte();

                    Console.Write(" {0:x6}", column);
                }

                Console.WriteLine();
            }));

            bool lastWasChar = false;
            while (fileStream.Position != fileStream.Length)
            {
                byte next = (byte)fileStream.ReadByte();

                if (next == 0x1B)
                {
                    byte escape = (byte)fileStream.ReadByte();
                    if (lastWasChar)
                        Console.WriteLine();

                    lastWasChar = false;

                    if (escapeSequences.ContainsKey(escape))
                    {
                        escapeSequences[escape].Read(fileStream);
                    }
                    else
                    {
                        Console.WriteLine("Unknown escape sequence: " + escape);
                        return;
                    }
                }
                else
                {
                    if (!Char.IsControl((char)next))
                    {
                        lastWasChar = true;
                        Console.Write((char)next);
                    }
                    else
                    {
                        switch (next)
                        {
                            case 0x0d:
                                Console.WriteLine("CR");
                                break;
                            case 0x0a:
                                Console.WriteLine("LF");
                                break;
                            case 0x09:
                                Console.WriteLine("horizontal tab");
                                break;
                            case 0x0C:
                                Console.WriteLine("form feed");
                                break;
                            default:
                                Console.WriteLine();
                                Console.WriteLine("unknown special character: " + next);
                                return;
                        }
                    }
                }
            }
            Console.WriteLine("EOF");
        }
    }
}
