// Copyright 2017 -- Stephen T. Mohr, OSIsoft, LLC
// Licensed under the MIT license

// Methods to read/write graphs in textual adjacency list format into/out of a Network instance
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{
    public class ClusterSerializer
    {
        public static HashSet<HashSet<string>> ReadClustersFromFile(string filename, char delimiter = '|')
        {
            StreamReader reader = new StreamReader(filename);   // don't catch any exceptions, let the caller respond
            HashSet<HashSet<string>> retVal = ReadClusters(reader, delimiter);
            reader.Close();
            return retVal;
        }

        public static HashSet<HashSet<string>> ReadClusters(TextReader stIn, char delimiter = '|')
        {
            HashSet<HashSet<string>> retVal = new HashSet<HashSet<string>>();
            string line = string.Empty;
            while ((line = stIn.ReadLine()) != null)
            {
                retVal.Add(ReadCluster(line, delimiter));
            }

            return retVal;
        }

        public static HashSet<string> ReadCluster(string clusterLine, char delimiter = '|')
        {
            HashSet<string> retVal = new HashSet<string>();
            string[] nodes = clusterLine.Split(delimiter);
            foreach (string node in nodes)
                retVal.Add(node);

            return retVal;
        }

        public static void WriteClustersToFile(IEnumerable<HashSet<string>> clusters, string filename, char delimiter = '|')
        {
            StreamWriter writer = new StreamWriter(filename);
            WriteClusters(clusters, writer, delimiter);
            writer.Close();
        }

        public static void WriteClusters(IEnumerable<HashSet<string>> clusters, TextWriter writer, char delimiter = '|')
        {
            // write each cluster to a separate line, delimited on the line by the delimiter character

            foreach(HashSet<string> cluster in clusters)
            {
                WriteCluster(cluster, writer, delimiter);
            }
        }

        public static void WriteCluster(HashSet<string> cluster, TextWriter writer, char delimiter = '|')
        {
            for (int i = 0; i < cluster.Count() - 1; i++)
            {
                writer.Write(cluster.ElementAt(i) + delimiter);
            }
            writer.WriteLine(cluster.ElementAt(cluster.Count() - 1));
        }
    }
}
