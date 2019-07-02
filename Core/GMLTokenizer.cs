using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{
    public class GMLTokenizer
    {
        private static string deadchars = "\t \r\n";
        // advance the reader through whitespace characters and leave the reader poised to read the next significant character
        public static void EatWhitespace(TextReader reader)
        {
            int ch = reader.Peek();
            while (ch != -1 && deadchars.Contains((char)ch))
            {
                if ((char)ch == '#')
                {
                    // blow off comments 
                    // comments with the comment key word are treated as key value properties and may be ignored by the application
                    reader.ReadLine();
                }
                else
                {
                    reader.Read();
                    ch = reader.Peek();
                }
            }
        }

        public static string ReadNextToken(TextReader reader)
        {
            string token = "";
            int ch = reader.Peek();
            while (ch != -1 && !deadchars.Contains((char)ch))
            {
                token += (char)reader.Read();

                // return start and end brackets as tokens
                if ((char)ch == '[' || (char)ch == ']')
                    break;
                ch = reader.Peek();
            }
            return token;
        }

        public static Dictionary<string, string> ReadFlatListProperty(TextReader reader)
        {
            Dictionary<string, string> props = new Dictionary<string, string>();
            EatWhitespace(reader);

            string key = ReadNextToken(reader);
            // courtesy check to see is the calling routine left the reader waiting to read the opening bracket
            if (key == "[")
            {
                EatWhitespace(reader);
                key = ReadNextToken(reader);
            }

            while (key != "]")
            {
                EatWhitespace(reader);
                string value = ReadNextValue(reader);
                props.Add(key, value);
                EatWhitespace(reader);
                key = ReadNextToken(reader);
            }

            return props;
        }

        public static int PositionStartOfRecordOrArray(TextReader reader)
        {
            int retVal = 0; // no error

            EatWhitespace(reader);
            string key = ReadNextToken(reader);
            // courtesy check to see is the calling routine left the reader waiting to read the opening bracket
            if (key == "[")
            {
                EatWhitespace(reader);
            }
            else
            {
                retVal = -1;
            }

            return retVal;
        }

        public static string ReadNextValue(TextReader reader)
        {
            string value = "";

            int first = reader.Peek();
            if (first != -1)
            {
                int nextChar = reader.Read();

                if ((char)nextChar != '\'' && (char)nextChar != '"')
                {
                    while (nextChar != -1)
                    {
                        if (!deadchars.Contains((char)nextChar))
                        {
                            value += (char)nextChar;
                            nextChar = reader.Read();
                        }
                        else
                        {
                            break;
                        }
                    }

                }
                else
                {
                    // next char indicates a quoted string literal
                    char ch = (char)reader.Read();
                    // assume the string is closed before EOF
                    while (ch != '\'' && ch != '"')
                    {
                        value += ch;
                        ch = (char)reader.Read();
                    }
                }
            }
            return value;
        }

        public static float ProcessFloatProp(string prop)
        {
            // FormatException, OverflowException caught by caller so that context may be captured in the eventual exception
            return Convert.ToSingle(prop);
        }

        public static void ConsumeUnknownValue(TextReader reader)
        {
            // assumes unknown key has been read and we need to advance past either a simple value or a record value
            EatWhitespace(reader);
            string value = ReadNextValue(reader);
            if (value == "[")
                ReadFlatListProperty(reader);
        }
    }
}
