using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;
using Algorithms;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Network G;
            HashSet<HashSet<string>> seeds = null;
            try
            {
                G = NetworkSerializer.ReadNetworkFromFile(@"..\..\work\displays1.dat", false);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Unable to find network file for input");
                Console.ReadLine();
                return;
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("Unable to find working directory");
                Console.ReadLine();
                return;
            }

            /* try
             {
                 seeds = ClusterSerializer.ReadClustersFromFile(@"..\..\work\displays1seeds3.dat");
             }
             catch (FileNotFoundException)
             {
                 Console.WriteLine("Unable to find seed file for input");
                 Console.ReadLine();
                 return;
             }
             catch (DirectoryNotFoundException)
             {
                 Console.WriteLine("Unable to find working directory");
                 Console.ReadLine();
                 return;
             }*/

            seeds = new HashSet<HashSet<string>>();
            foreach (string vertex in G.Vertices)
            {
                HashSet<string> seed = new HashSet<string>();
                seed.Add(vertex);
                seeds.Add(seed);
            }
            for (int i = 0; i < seeds.Count(); i++)
            {
                HashSet<string> seed = seeds.ElementAt(i);
                Partitioning.ExpandSeed(ref seed, G, 0.5);
            }

            ClusterSerializer.WriteClustersToFile(seeds, @"..\..\work\displays1clusters2.out");
        }
    }
}
