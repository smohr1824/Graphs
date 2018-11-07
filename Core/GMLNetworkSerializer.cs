// MIT License

// Copyright(c) 2017 - 2018 Stephen Mohr

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;

namespace Networks.Core
{
    public class GMLNetworkSerializer
    {

        public static void WriteNetworkToFile(Network net, string filename)
        {
            StreamWriter writer = new StreamWriter(filename);
            WriteNetwork(net, writer);
            writer.Close();
        }

        public static void WriteNetwork(Network net, TextWriter writer)
        {
            net.ListGML(writer);
        }

        public static Network ReadNetworkFromFile(string filename)
        {

            StreamReader reader = new StreamReader(filename);   // don't catch any exceptions, let the caller respond
            Network retVal = ReadNetwork(reader);
            reader.Close();
            return retVal;
        }

        public static Network ReadNetwork(TextReader stIn)
        {
            throw new NotImplementedException();

        }



        // split a line on whitespace -- we expect a name, value pair delimited by whitespace
        private static string[] SplitAndClean()
        {
            string[] fields = Regex.Split("your string here", @"\s+");
            int len = fields.Count();
            for (int i = 0; i < len; i++)
            {
                fields[i] = fields[i].Trim();
            }
            return fields;
        }
    }
}
