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

// Methods to read/write graphs in textual adjacency list format into/out of a Network instance
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{
    // specific to community detection
    public class ClusterSerializer
    {
        public static HashSet<HashSet<uint>> ReadClustersFromFile(string filename, char delimiter = '|')
        {
            StreamReader reader = new StreamReader(filename);   // don't catch any exceptions, let the caller respond
            HashSet<HashSet<uint>> retVal = ReadClusters(reader, delimiter);
            reader.Close();
            return retVal;
        }

        public static HashSet<HashSet<uint>> ReadClusters(TextReader stIn, char delimiter = '|')
        {
            HashSet<HashSet<uint>> retVal = new HashSet<HashSet<uint>>();
            string line = string.Empty;
            while ((line = stIn.ReadLine()) != null)
            {
                retVal.Add(ReadCluster(line, delimiter));
            }

            return retVal;
        }

        public static HashSet<uint> ReadCluster(string clusterLine, char delimiter = '|')
        {
            HashSet<uint> retVal = new HashSet<uint>();
            string[] nodes = clusterLine.Split(delimiter);
            foreach (string node in nodes)
                retVal.Add(Convert.ToUInt32(node));

            return retVal;
        }

        public static void WriteClustersToFile(IEnumerable<HashSet<uint>> clusters, string filename, char delimiter = '|')
        {
            StreamWriter writer = new StreamWriter(filename);
            WriteClusters(clusters, writer, delimiter);
            writer.Close();
        }

        public static void WriteClustersToFileByLine(IEnumerable<HashSet<uint>> clusters, string filename)
        {
            StreamWriter writer = new StreamWriter(filename);
            WriteClustersByLine(clusters, writer);
            writer.Close();
        }

        public static void WriteClusters(IEnumerable<HashSet<uint>> clusters, TextWriter writer, char delimiter = '|')
        {
            // write each cluster to a separate line, delimited on the line by the delimiter character

            foreach(HashSet<uint> cluster in clusters)
            {
                WriteCluster(cluster, writer, delimiter);
            }
        }

        public static void WriteClustersByLine(IEnumerable<HashSet<uint>> clusters, TextWriter writer)
        {
            // write each cluster to a separate line, delimited on the line by the delimiter character

            foreach (HashSet<uint> cluster in clusters)
            {
                WriteClusterByLine(cluster, writer);
                writer.WriteLine(Environment.NewLine);
            }
        }

        public static void WriteCluster(HashSet<uint> cluster, TextWriter writer, char delimiter = '|')
        {
            for (int i = 0; i < cluster.Count() - 1; i++)
            {
                writer.Write(cluster.ElementAt(i) + delimiter);
            }
            writer.WriteLine(cluster.ElementAt(cluster.Count() - 1));
        }

        public static void WriteClusterByLine(HashSet<uint> cluster, TextWriter writer)
        {
            for (int i = 0; i < cluster.Count(); i++)
            {
                writer.Write(cluster.ElementAt(i) + Environment.NewLine);
            }
        }
    }
}
