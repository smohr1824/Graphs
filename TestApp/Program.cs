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

            TestBasicMultiLayer();
            return;

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

            //TestConstructor();
            //return;

            NetworkSerializer.WriteNetworkToFile(G, @"..\..\work\nettest.out");
            List<HashSet<string>> communities = Partitioning.SLPA(G, 20, 0.3, DateTime.Now.Millisecond);
            IEnumerable<HashSet<string>> unique = communities.Distinct(new SetEqualityComparer());
            Console.WriteLine($"Found {unique.Count()} communities in a graph of {G.Order} vertices, writing to displays2SLPA.out");
            Console.ReadLine();
            ClusterSerializer.WriteClustersToFileByLine(unique, @"..\..\work\chartest.out");
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

        private static void TestBasicMultiLayer()
        {
            Network G = null;
            Network H = null;
            Network I = null;
            Network J = null;
            Network K = null;
            Network L = null;

            try
            {
                G = NetworkSerializer.ReadNetworkFromFile(@"..\..\work\electrical.dat", true);
                H = NetworkSerializer.ReadNetworkFromFile(@"..\..\work\flow.dat", true);
                I = NetworkSerializer.ReadNetworkFromFile(@"..\..\work\control.dat", true);

                J = NetworkSerializer.ReadNetworkFromFile(@"..\..\work\electrical.dat", true);
                K = NetworkSerializer.ReadNetworkFromFile(@"..\..\work\flow.dat", true);
                L = NetworkSerializer.ReadNetworkFromFile(@"..\..\work\control.dat", true);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e}");
            }

            string[] procIndices = { "electrical", "flow", "control" };
            string[] locIndices = { "PHL", "SLTC" };
            Tuple<string, IEnumerable<string>>[] aspect = { new Tuple<string, IEnumerable<string>>("process", procIndices), new Tuple<string, IEnumerable<string>>("site", locIndices) };
            MultilayerNetwork Q = new MultilayerNetwork(aspect);
            List<string> index = new List<string>();

            // add PHL elementary layers
            index.Add(procIndices[0]);
            index.Add(locIndices[0]);
            Q.AddElementaryLayer(index, G); // electrical,PHL
            index[0] = procIndices[1];
            Q.AddElementaryLayer(index, H); // flow,PHL
            index[0] = procIndices[2];
            Q.AddElementaryLayer(index, I); // control,PHL

            // add SLTC elementary layers
            index[0] = procIndices[0];
            index[1] = locIndices[1];
            Q.AddElementaryLayer(index, J); // electrical,SLTC
            index[0] = procIndices[1];
            Q.AddElementaryLayer(index, K); // flow,SLTC
            index[0] = procIndices[2];
            Q.AddElementaryLayer(index, L); // control,SLTC

            // add interlayer edges
            Q.AddEdge(new NodeTensor("A", "electrical,SLTC"), new NodeTensor("B", "control,SLTC"), 2, true);
            Q.AddEdge(new NodeTensor("C", "control,PHL"), new NodeTensor("A", "control,SLTC"), 4, true);

            // add intralayer edge
            Q.AddEdge(new NodeTensor("D", "flow,SLTC"), new NodeTensor("E", "flow,SLTC"), 2, true);

            // try to add edge with non-existent vertex
            try
            {
                // layer does not exist
                Q.AddEdge(new NodeTensor("G", "fusion,SLTC"), new NodeTensor("H", "fusion, SLTC"), 1, true);
            }
            catch (ArgumentException)
            {
                

            }

            try
            {
                // interlayer, vertex does not exist
                Q.AddEdge(new NodeTensor("Z", "electrical,PHL"), new NodeTensor("A", "flow,PHL"), 2, true);
            }
            catch (ArgumentException)
            {

            }

            try
            {
                // intralayer, vertex does not exist
                Q.AddEdge(new NodeTensor("Z", "flow,SLTC"), new NodeTensor("B", "flow,SLTC"), 2, true);
            }
            catch (ArgumentException)
            {

            }
            double edgeWt = Q.EdgeWeight(new NodeTensor("D", "flow,SLTC"), new NodeTensor("E", "flow,SLTC"));
            edgeWt = Q.EdgeWeight(new NodeTensor("C", "control,PHL"), new NodeTensor("A", "control,SLTC"));
            MultilayerNetworkSerializer.WriteMultiLayerNetworkToFile(Q, @"..\..\work\multilayer_test.dat");
        }

        private static void TestConstructor()
        {
            double[, ] weights = { { 0.0, 1.0, 2.0, 0.0 }, { 0.0, 0.0, 1.0, 3.5 }, { 1.0, 2.1, 0.0, 2.0 }, { 1.0, 0.0, 0.0, 0.0 } };
            string[] nodes = { "A", "B", "C", "D" };
            List<string> vertices = new List<string>(nodes);

            Network G = new Network(vertices, weights);

            double[,] ewts = G.AdjacencyMatrix;
            List<string> outV = G.Vertices;

            bool test = G.HasEdge("B", "C");

            double dtest = G.EdgeWeight("B", "D");

            dtest = G.EdgeWeight("D", "C");

        }
    }
}
