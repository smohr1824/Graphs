﻿// MIT License

// Copyright(c) 2017 Stephen Mohr and OSIsoft, LLC

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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{
    public class NetworkSerializer
    {
        public static Network ReadNetworkFromFile(string filename, bool directed, char delimiter ='|')
        {
            
            StreamReader reader = new StreamReader(filename);   // don't catch any exceptions, let the caller respond
            Network retVal = ReadNetwork(reader, directed, delimiter);
            reader.Close();
            return retVal;
        }

        public static Network ReadNetwork(TextReader stIn, bool directed, char delimiter = '|' )
        {
            Network retVal = new Network(directed);
            string[] fields;
            string line = string.Empty;
            while ((line = stIn.ReadLine()) != null)
            {
                fields = SplitAndClean(line, delimiter);
                int ct = fields.Count();
                if (ct == 1)
                {
                    // vertex only, so just add it
                    retVal.AddVertex(fields[0]);
                    continue;
                }
                if (ct > 3)
                    continue;

                int wt = 1;
                if (ct == 3)
                {
                    try
                    {
                        wt = Convert.ToInt32(fields[2]);
                    }
                    catch (Exception)
                    {
                        // default to 1
                    }
                }
                retVal.AddEdge(fields[0], fields[1], wt);
            }

            return retVal;
        }

        public static void WriteNetworkToFile(Network net, string filename, char delimiter = '|')
        {
            StreamWriter writer = new StreamWriter(filename);
            WriteNetwork(net, writer, delimiter);
            writer.Close();
        }

        /// <summary>
        /// Serializes the network with all edges shown, i.e., an undirected graph will have edges in both directions even if it was read
        /// from a file with directed == false to create the back edges
        /// </summary>
        /// <param name="net"></param>
        /// <param name="writer"></param>
        /// <param name="delimiter"></param>
        public static void WriteNetwork(Network net, TextWriter writer, char  delimiter = '|')
        {
            net.List(writer, delimiter);
        }

        private static string[] SplitAndClean(string line, char delimiter)
        {
            string[] fields = line.Split(delimiter);
            int len = fields.Count();
            for (int i = 0; i < len; i++)
            {
                fields[i] = fields[i].Trim();
            }
            return fields;
        }
    }
}
