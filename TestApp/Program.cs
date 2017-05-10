using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Networks.Core;
using Networks.Algorithms;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Network G = new Network();

            HashSet<HashSet<string>> seeds = null;
            try
            {
                G = NetworkSerializer.ReadNetworkFromFile(@"..\..\work\displays2.dat", false);
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
                 seeds = ClusterSerializer.ReadClustersFromFile(@"..\..\work\displays1seeds4.dat");
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

            List<HashSet<string>> communities = Partitioning.SLPA(G, 20, 0.3, DateTime.Now.Millisecond);
            IEnumerable<HashSet<string>> unique = communities.Distinct(new SetEqualityComparer());
            Console.WriteLine($"Found {unique.Count()} communities in a graph of {G.Order} vertices, writing to displays2SLPA.out");
            Console.ReadLine();
            ClusterSerializer.WriteClustersToFile(unique, @"..\..\work\displays2SLPA.out");
            return;
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
                Partitioning.CISExpandSeed(ref seed, G, 0.5);
            }

            IEnumerable<HashSet<string>> best = seeds.Distinct<HashSet<string>>(new SetEqualityComparer());
            ClusterSerializer.WriteClustersToFile(best, @"..\..\work\displays1clusters_test.out");
        }
    }
}
