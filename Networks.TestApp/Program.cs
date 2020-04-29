// MIT License

// Copyright(c) 2017 - 2019 Stephen Mohr

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
using Networks.Core;
using Networks.Algorithms;
using Networks.FCM;

namespace Networks.TestApp
{
    class Program
    {
        static void Main(string[] args)
        {

            //TestReadMultilayer();

            //TestBasicMultiLayer();

            //TestLouvain();
            //TestLouvainResolution();
            //TestRemove();
            //TestCommunityDetection();
            //TestGephiOutput();
            //TestBig();
            //TestNewAdj();
            //WriteGML();
            //ReadGML();
            //ReadGMLWithUnknown();
            //TestWriteMultilayerGML();
            //TestReadMultilayerGML();
            //TestEdgeWeight();
            //TestBigBipartite();
            //TestFCM();
            //PerfTestFCM();
            MultilayerNetwork Q = MultilayerNetworkSerializer.ReadMultilayerNetworkFromFile(@"..\..\..\work\multilayer_three_aspects.dat", true);
            Q.MakeSupraAdjacencyMatrix();
            MultilayerNetworkGMLSerializer.WriteMultiLayerNetworkToFile(Q, @"..\..\..\work\multilayer_three_aspects.gml");
            TestMLFCMBasic();
            //TestReadIterateMLFCMBasic();
            //TestWriteFCM();
            //TestWriteMLFCM();
            //TestReadMLFCM();
            Console.ReadLine();
            return;


        }

        // Needs porting to replace names with uint ids for packages
        private static void TestBig()
        {
            Network G = NetworkSerializer.ReadNetworkFromFile(@"..\..\..\work\python_reqs.csv", true, ',');
            Console.WriteLine($"Read {G.Order} vertices");
            DateTime start = DateTime.Now;
            List<HashSet<uint>> communities = CommunityDetection.SLPA(G, 20, .4, 123456);
            DateTime end = DateTime.Now;
            TimeSpan dur = end - start;
            Console.WriteLine($"Found {communities.Count} in {dur.TotalSeconds} seconds");
            Console.ReadLine();
        }

        private static void TestBigBipartite()
        {
            Random rnd = new Random();
            Network G = new Network(true);
            Dictionary<string, uint> concordance = new Dictionary<string, uint>();
            for (uint i = 0; i < 100000; i++)
            {
                concordance.Add($"{i}A", (uint)i);
                concordance.Add($"A{i}", (uint)i +100000);
            }
            long start = DateTime.UtcNow.Ticks;
            for (int i = 0; i < 100000; i++)
            {
                int degree = rnd.Next(21); // up to 20 additional edges
                string id = i.ToString();
                // make a graph in which numbers first nodes are matched to "A"+ numbers second nodes, hence known bipartite
                G.AddEdge(concordance[id + "A"], concordance["A" + id], 1.0F);
                // make degree edges while maintaining the bipartite nature -- goal is to compare to concurrent version, so must have enough neighbors to make it worthwhile
                for (int j = 0; j < degree; j++)
                {
                    int right = rnd.Next(100000);
                    G.AddEdge(concordance[id + "A"], concordance["A" + right.ToString()], 1.0F);
                }
            }
            long end = DateTime.UtcNow.Ticks;
            Console.WriteLine($"Building 200,000 node graph took {(end - start) / 10000} milliseconds");

            List<uint> R = null;
            List<uint> B = null;
            bool res = false;
            start = DateTime.UtcNow.Ticks;
            res = Bipartite.IsBipartite(G, out R, out B);
            end = DateTime.UtcNow.Ticks;
            Console.WriteLine($"Graph of 200,000 nodes is  bipartite {res}, took {(end - start) / 10000} milliseconds");
        }

        private static FuzzyCognitiveMap MakeBasicFCM()
        {
            FuzzyCognitiveMap fcm = new FuzzyCognitiveMap(false);
            fcm.AddConcept("A", 1.0F, 1.0F);
            fcm.AddConcept("B", 0.0F, 0.0F);
            fcm.AddConcept("C", 1.0F, 1.0F);
            fcm.AddConcept("D", 0.0F, 0.0F);
            fcm.AddConcept("E", 0.0F, 0.0F);

            fcm.AddInfluence("B", "A", 1.0F);
            fcm.AddInfluence("A", "C", 1.0F);
            fcm.AddInfluence("C", "E", 1.0F);
            fcm.AddInfluence("E", "D", 1.0F);
            fcm.AddInfluence("D", "C", -1.0F);
            fcm.AddInfluence("B", "E", -1.0F);
            fcm.AddInfluence("E", "A", -1.0F);
            fcm.AddInfluence("D", "B", 1.0F);

            return fcm;
        }
        private static void TestFCM()
        {
            FuzzyCognitiveMap fcm = MakeBasicFCM();

            Console.WriteLine("Algebraic inference");
            WriteStateVector(fcm);
            for (int i = 0; i < 5; i++)
            {
                fcm.Step();
                WriteStateVector(fcm);
            }

            fcm.Reset();
            Console.WriteLine("Algorithmic inference");
            WriteStateVector(fcm);
            for (int i = 0; i < 5; i++)
            {
                fcm.StepWalk();
                WriteStateVector(fcm);
            }

            Console.WriteLine("Logistic");
            fcm.Reset();
            fcm.SwitchThresholdFunction(thresholdType.LOGISTIC);

            WriteStateVector(fcm);
            for (int i = 0; i < 5; i++)
            {
                fcm.Step();
                WriteStateVector(fcm);
            }
        }

        private static void PerfTestFCM()
        {
            FuzzyCognitiveMap fcm = new FuzzyCognitiveMap(false);
            fcm.AddConcept("A", 1.0F, 1.0F);
            fcm.AddConcept("B", 0.0F, 0.0F);
            fcm.AddConcept("C", 1.0F, 1.0F);
            fcm.AddConcept("D", 0.0F, 0.0F);
            fcm.AddConcept("E", 0.0F, 0.0F);
            fcm.AddConcept("F", 1.0F, 1.0F);
            fcm.AddConcept("G", 1.0F, 1.0F);
            fcm.AddConcept("H", 0.0F, 0.0F);
            fcm.AddConcept("I", 0.0F, 0.0F);
            fcm.AddConcept("J", 0.0F, 0.0F);

            fcm.AddInfluence("B", "A", 1.0F);
            fcm.AddInfluence("A", "C", 1.0F);
            fcm.AddInfluence("C", "E", 1.0F);
            fcm.AddInfluence("E", "D", 1.0F);
            fcm.AddInfluence("D", "C", -1.0F);
            fcm.AddInfluence("B", "E", -1.0F);
            fcm.AddInfluence("E", "A", -1.0F);
            fcm.AddInfluence("D", "B", 1.0F);
            fcm.AddInfluence("F", "E", 1.0F);
            fcm.AddInfluence("F", "B", 1.0F);
            fcm.AddInfluence("F", "A", 1.0F);
            fcm.AddInfluence("G", "C", -1.0F);
            fcm.AddInfluence("H", "G", -1.0F);
            fcm.AddInfluence("C", "H", 1.0F);
            fcm.AddInfluence("H", "J", 1.0F);
            fcm.AddInfluence("H", "I", 1.0F);
            fcm.AddInfluence("I", "J", -1.0F);
            fcm.AddInfluence("E", "J", 1.0F);

            long start = DateTime.UtcNow.Ticks;
            for (int i = 0; i < 100000; i++)
            {
                fcm.Step();
            }
            long end = DateTime.UtcNow.Ticks;
            Console.WriteLine($"100,000 inferences algebraically took {(end - start) / 10000} milliseconds");
            WriteStateVector(fcm);

            fcm.Reset();
            start = DateTime.UtcNow.Ticks;
            for (int k = 0; k < 100000; k++)
            {
                fcm.StepWalk();
            }
            end = DateTime.UtcNow.Ticks;
            Console.WriteLine($"100,000 inferences algorithmically took {(end - start) / 10000} milliseconds");
            WriteStateVector(fcm);
            Console.ReadLine();
        }

        private static void TestWriteFCM()
        {
            FuzzyCognitiveMap fcm = MakeBasicFCM();
            FCMSerializer.WriteNetworkToFile(fcm, @"..\..\..\work\basic.fcm");

            FuzzyCognitiveMap newFcm = FCMSerializer.ReadNetworkFromFile(@"..\..\..\work\basic.fcm");

            for (int i = 0; i < 4; i++)
            {
                fcm.StepWalk();
                newFcm.StepWalk();
                Console.WriteLine($"Original: ");
                WriteStateVector(fcm);
                Console.WriteLine("Read: ");
                WriteStateVector(newFcm);
            }
        }

        private static void TestWriteMLFCM()
        {
            List<string> indices = new List<string>();
            indices.Add("I");
            indices.Add("II");
            List<Tuple<string, IEnumerable<string>>> dimensions = new List<Tuple<string, IEnumerable<string>>>();
            dimensions.Add(new Tuple<string, IEnumerable<string>>("levels", indices));

            MultilayerFuzzyCognitiveMap fcm = new MultilayerFuzzyCognitiveMap(dimensions);
            List<string> layer1 = new List<string>();
            layer1.Add("I");
            List<string> layer2 = new List<string>();
            layer2.Add("II");
            fcm.AddConcept("A", layer1, 1.0F, 1.0F);
            fcm.AddConcept("B", layer1, 0.0F, 0.0F);
            fcm.AddConcept("C", layer1, 0.0F, 0.0F);

            fcm.AddConcept("A", layer2, 1.0F, 1.0F);
            fcm.AddConcept("D", layer2, 0.0F, 0.0F);
            fcm.AddConcept("E", layer2, 0.0F, 0.0F);

            fcm.AddInfluence("A", layer1, "B", layer1, 1.0F);
            fcm.AddInfluence("A", layer1, "C", layer1, 1.0F);
            fcm.AddInfluence("A", layer2, "D", layer2, 1.0F);
            fcm.AddInfluence("D", layer2, "E", layer2, 1.0F);
            fcm.AddInfluence("E", layer2, "A", layer1, 1.0F);

            MLFCMSerializer.WriteMultiLayerNetworkToFile(fcm, @"..\..\..\work\MLbasic.fcm");
        }

        private static void TestReadMLFCM()
        {
            MultilayerFuzzyCognitiveMap fcm = MLFCMSerializer.ReadNetworkFromFile(@"..\..\..\work\MLBasic.fcm");
        }

        private static void TestMLFCMBasic()
        {

            List<string> indices = new List<string>();
            indices.Add("I");
            indices.Add("II");
            List<Tuple<string, IEnumerable<string>>> dimensions = new List<Tuple<string, IEnumerable<string>>>();
            dimensions.Add(new Tuple<string, IEnumerable<string>>("levels", indices));

            MultilayerFuzzyCognitiveMap fcm = new MultilayerFuzzyCognitiveMap(dimensions, thresholdType.LOGISTIC, false);
            
            List<string> layer1 = new List<string>();
            layer1.Add("I");
            List<string> layer2 = new List<string>();
            layer2.Add("II");
            fcm.AddConcept("A", layer1, 1.0F, 1.0F);
            fcm.AddConcept("B", layer1, 0.0F, 0.0F);
            fcm.AddConcept("C", layer1, 0.0F, 0.0F);

            fcm.AddConcept("A", layer2, 1.0F, 1.0F);
            fcm.AddConcept("D", layer2, 0.0F, 0.0F);
            fcm.AddConcept("E", layer2, 0.0F, 0.0F);

            fcm.AddInfluence("A", layer1, "B", layer1, 1.0F);
            fcm.AddInfluence("A", layer1, "C", layer1, 1.0F);
            fcm.AddInfluence("A", layer2, "D", layer2, 1.0F);
            fcm.AddInfluence("D", layer2, "E", layer2, 1.0F);
            fcm.AddInfluence("E", layer2, "A", layer1, 1.0F);
            foreach (KeyValuePair<uint, MultilayerCognitiveConcept> kvp in fcm.Concepts)
            {
                fcm.RecomputeAggregateActivationLevel(kvp.Value.Name);
            }
            List<string> concepts = fcm.ListConcepts();

            string[] layerI = { "I" };
            string[] layerII = { "II" };
            List<string> levelICoords = new List<string>(layerI);
            List<string> levelIICoords = new List<string>(layerII);
            Console.WriteLine("Starting State");
            //FCMState initial = fcm.ReportLayerState(levelICoords);
            //WriteLayerState(levelICoords, initial);

            //initial = fcm.ReportLayerState(levelIICoords);
            //WriteLayerState(levelIICoords, initial);
            WriteState(fcm);
            Console.WriteLine();

            for (int i = 1; i < 4; i++)
            {
                fcm.StepWalk();
                Console.WriteLine($"Iteration {i}");
                //FCMState state = fcm.ReportLayerState(levelICoords);
                //WriteLayerState(levelICoords, state);
                //state = fcm.ReportLayerState(levelIICoords);
                //WriteLayerState(levelIICoords, state);
                WriteState(fcm);
                Console.WriteLine();
            }

            Console.WriteLine("Reset and re-run");
            fcm.Reset();
            
            WriteState(fcm);

            for (int i = 1; i < 4; i++)
            {
                fcm.StepWalk();
                Console.WriteLine($"Iteration {i}");
                //FCMState state = fcm.ReportLayerState(levelICoords);
                //WriteLayerState(levelICoords, state);
                //state = fcm.ReportLayerState(levelIICoords);
                //WriteLayerState(levelIICoords, state);
                WriteState(fcm);
                Console.WriteLine();
            }
        }

        private static void WriteState(MultilayerFuzzyCognitiveMap fcm)
        {
            List<string> concepts = fcm.ListConcepts();
            string sMain = string.Empty;
            foreach (string concept in concepts)
            {
                sMain += concept + ": " + fcm.GetActivationLevel(concept).ToString("F1") + " ";
            }
            Console.WriteLine(sMain);

            List<List<string>> layers = fcm.ListLayers();
            foreach (List<string> layer in layers)
            {
                Dictionary<string, float> levels = fcm.GetLayerActivationLevels(layer);
                Console.WriteLine(string.Join(",", layer));
                string sLevel = string.Empty;
                foreach (KeyValuePair<string, float> kvp in levels)
                {
                    sLevel += kvp.Key + ": " + kvp.Value.ToString("F1") +" ";
                }
                Console.WriteLine(sLevel);
            }
        }

        private static void TestReadIterateMLFCMBasic()
        {
            MultilayerFuzzyCognitiveMap fcm1 = BuildBasic();
            MultilayerFuzzyCognitiveMap fcm2 = MLFCMSerializer.ReadNetworkFromFile(@"..\..\..\work\MLbasic.fcm");

            string[] layerI = { "I" };
            string[] layerII = { "II" };
            List<string> levelICoords = new List<string>(layerI);
            List<string> levelIICoords = new List<string>(layerII);
            Console.WriteLine("Starting State");
            WriteState(fcm1);

            Console.WriteLine("Start State as read");
            WriteState(fcm2);
            Console.WriteLine();

            for (int i = 1; i < 4; i++)
            {
                // Console.WriteLine($"Step {i}");
                fcm1.StepWalk();
                fcm2.StepWalk();

                Console.WriteLine($"Iteration {i} in memory");
                WriteState(fcm1);

                Console.WriteLine($"Iteration {i} as read");
                WriteState(fcm2);
                Console.WriteLine();
            }


        }

        private static MultilayerFuzzyCognitiveMap BuildBasic()
        {
            List<string> indices = new List<string>();
            indices.Add("I");
            indices.Add("II");
            List<Tuple<string, IEnumerable<string>>> dimensions = new List<Tuple<string, IEnumerable<string>>>();
            dimensions.Add(new Tuple<string, IEnumerable<string>>("levels", indices));

            MultilayerFuzzyCognitiveMap fcm = new MultilayerFuzzyCognitiveMap(dimensions);
            List<string> layer1 = new List<string>();
            layer1.Add("I");
            List<string> layer2 = new List<string>();
            layer2.Add("II");
            fcm.AddConcept("A", layer1, 1.0F, 1.0F);
            fcm.AddConcept("B", layer1, 0.0F, 0.0F);
            fcm.AddConcept("C", layer1, 0.0F, 0.0F);

            fcm.AddConcept("A", layer2, 1.0F, 1.0F);
            fcm.AddConcept("D", layer2, 0.0F, 0.0F);
            fcm.AddConcept("E", layer2, 0.0F, 0.0F);

            fcm.AddInfluence("A", layer1, "B", layer1, 1.0F);
            fcm.AddInfluence("A", layer1, "C", layer1, 1.0F);
            fcm.AddInfluence("A", layer2, "D", layer2, 1.0F);
            fcm.AddInfluence("D", layer2, "E", layer2, 1.0F);
            fcm.AddInfluence("E", layer2, "A", layer1, 1.0F);

            return fcm;
        }
        private static void WriteStateVector(FuzzyCognitiveMap map)
        {
            /*FCMState state = map.ReportState();
            Console.Write("( ");
            for (int i = 0; i < state.ConceptNames.Length; i++)
            {
                Console.Write(state.ConceptNames[i] + ": " + state.ActivationValues[i].ToString("F1") + " ");
            }
            Console.WriteLine(")");*/
            Dictionary<string, float> state = map.ReportState();
            Console.Write("(");
            foreach (KeyValuePair<string, float> concept in state)
            {
                Console.Write(concept.Key + ": " + concept.Value.ToString("F1") + " ");
            }
            Console.WriteLine(")");
        }

        private static void TestNewAdj()
        {
            Network G = NetworkSerializer.ReadNetworkFromFile(@"..\..\..\work\newadjtest.dat", false);
            Console.WriteLine($"Read {G.Order} vertices");
            Console.WriteLine($"Edge count is {G.Size}");
            Dictionary<uint, float> nghrs = G.GetNeighbors(4);
            Console.Write(@"Neighbors of 4: ");
            foreach (KeyValuePair<uint, float> kvp in nghrs)
            {
                Console.Write(kvp.Key + " ");
            }
            Console.WriteLine();
            float[,] matrix = G.AdjacencyMatrix;
            Console.WriteLine();
        }

        private static void TestEdgeWeight()
        {
            Network G = NetworkSerializer.ReadNetworkFromFile(@"..\..\..\work\hasedgestest.dat", false);
            Console.WriteLine($"Read graph with {G.Order} vertices and {G.Size} edges");

            if (!G.HasEdge(3, 1))
            {
                Console.WriteLine(@"Well, shoot, that's bad...");
                return;
            }
            else
            {
                float wt = G.EdgeWeight(3, 1);
                wt = G.EdgeWeight(1, 2);
                bool test = G.HasEdge(1, 2);
            }

        }

        private static void TestRemove()
        {
            Network G = new Network(false);
            G.AddEdge(1, 2, 1.0F);
            G.AddEdge(2, 3, 2.0F);
            G.AddEdge(2, 4, 3.0F);
            G.AddEdge(3, 4, 2.0F);
            G.AddEdge(4, 2, 1.0F);
            G.RemoveVertex(3);
        }
        private static void TestLouvain()
        {
            Network G = NetworkSerializer.ReadNetworkFromFile(@"..\..\..\work\louvain_prime.dat", false);

            List<HashSet<uint>> res = CommunityDetection.Louvain(G, LouvainMetric.Modularity);
            res = CommunityDetection.Louvain(G, LouvainMetric.Goldberg);
        }

        private static void TestLouvainResolution()
        {
            Network G = NetworkSerializer.ReadNetworkFromFile(@"..\..\..\work\louvain_prime.dat", false);

            List<HashSet<uint>> res = CommunityDetection.Louvain(G, 0.5);
            res = CommunityDetection.Louvain(G, 0.6);
            res = CommunityDetection.Louvain(G, 0.7);
            res = CommunityDetection.Louvain(G, 0.8);
            res = CommunityDetection.Louvain(G, 0.9);
            res = CommunityDetection.Louvain(G, 1.0);
        }

        private static void TestGephiOutput()
        {
            Network G = NetworkSerializer.ReadNetworkFromFile(@"..\..\..\work\louvain_prime.dat", false);
            NetworkSerializer.WriteAdjacencyMatrixToGephiFile(G.Vertices, G.AdjacencyMatrix, @"..\..\..\work\louvain_forgephi.csv");
        }
        private static void TestCommunityDetection()
        {
            Network G = new Network(false);

            HashSet<HashSet<uint>> seeds = null;
            try
            {
                G = NetworkSerializer.ReadNetworkFromFile(@"..\..\..\work\displays2.dat", false);
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

            Dictionary<uint, float> sources = G.GetSources(5);

            NetworkSerializer.WriteNetworkToFile(G, @"..\..\..\work\nettest.out");
            List<HashSet<uint>> communities = CommunityDetection.SLPA(G, 20, 0.3, DateTime.Now.Millisecond);
            IEnumerable<HashSet<uint>> unique = communities.Distinct(new SetEqualityComparer());
            Console.WriteLine($"Found {unique.Count()} communities via SLPA in a graph of {G.Order} vertices, writing to slpa.out");
            Console.ReadLine();

            ClusterSerializer.WriteClustersToFileByLine(unique, @"..\..\..\work\slpa.out");
            List<HashSet<uint>> expanded = new List<HashSet<uint>>();
            for (int i = 0; i< unique.Count(); i++)
            {
                HashSet < uint > work = unique.ElementAt(i);
                CommunityDetection.CISExpandSeed(ref work, G, 0.5);
                expanded.Add(work);
            }

            unique = expanded.Distinct(new SetEqualityComparer());
            Console.WriteLine($"Found {unique.Count()} communities after expansion with CIS, writing to slpa_cis.out");
            Console.ReadLine();
            ClusterSerializer.WriteClustersToFileByLine(unique, @"..\..\..\work\slpa_cis.out");

            seeds = new HashSet<HashSet<uint>>();
            foreach (uint vertex in G.Vertices)
            {
                HashSet<uint> seed = new HashSet<uint>();
                seed.Add(vertex);
                seeds.Add(seed);

            }
            for (int i = 0; i < seeds.Count(); i++)
            {
                HashSet<uint> seed = seeds.ElementAt(i);
                CommunityDetection.CISExpandSeed(ref seed, G, 0.5);
            }

            IEnumerable<HashSet<uint>> best = seeds.Distinct<HashSet<uint>>(new SetEqualityComparer());
            Console.WriteLine($"Found {best.Count()} communities with CIS, writing to cis.out");
            Console.ReadLine();
            ClusterSerializer.WriteClustersToFileByLine(best, @"..\..\..\work\cis.out");

            communities = CommunityDetection.Louvain(G, LouvainMetric.Goldberg);
            Console.WriteLine($"Found {communities.Count()} communities with Louvain Goldberg, writing to louvain_g.out");
            Console.ReadLine();
            ClusterSerializer.WriteClustersToFileByLine(communities, @"..\..\..\work\louvain_g.out");

            communities = CommunityDetection.Louvain(G, LouvainMetric.Modularity);
            Console.WriteLine($"Found {communities.Count()} communities with Louvain Modularity, writing to louvain_m.out");
            Console.ReadLine();
            ClusterSerializer.WriteClustersToFileByLine(communities, @"..\..\..\work\louvain_m.out");

            communities = CommunityDetection.Louvain(G, 0.9);
            Console.WriteLine($"Found {communities.Count()} communities with Louvain Resolution (r = 0.9), writing to louvain_r.out");
            Console.ReadLine();
            ClusterSerializer.WriteClustersToFileByLine(communities, @"..\..\..\work\louvain_r.out");
        }

        private static void WriteGML()
        {
            Network G = NetworkSerializer.ReadNetworkFromFile(@"..\..\..\work\newadjtest.dat", false);
            GMLNetworkSerializer.WriteNetworkToFile(G, @"..\..\..\work\newadjtest.gml");
        }

        private static void ReadGML()
        {
            Network G = NetworkSerializer.ReadNetworkFromFile(@"..\..\..\work\newadjtest.dat", false);
            GMLNetworkSerializer.WriteNetworkToFile(G, @"..\..\..\work\newadjtest.gml");

            Network N = GMLNetworkSerializer.ReadNetworkFromFile(@"..\..\..\work\newadjtest.gml");
            Console.WriteLine($"Input graph has {G.Order} nodes and {G.Size} edges");
            Console.WriteLine($"Read graph has {N.Order} nodes and {N.Size} edges");
        }

        private static void ReadGMLWithUnknown()
        {
            try
            {
                Network G = GMLNetworkSerializer.ReadNetworkFromFile(@"..\..\..\work\basic.fcm");
            }
            catch (Exception ex)
            {
                string foo = ex.Message;
                foo = foo + ";";
            }

        }

        private static void TestReadMultilayer()
        {
            MultilayerNetwork Q = MultilayerNetworkSerializer.ReadMultilayerNetworkFromFile(@"..\..\..\work\multilayer_test.dat", true);
            string[] aspects = Q.Aspects();
            string[] indicesType = Q.Indices("process");
            string[] indicesSite = Q.Indices("site");
            MultilayerNetworkSerializer.WriteMultiLayerNetworkToFile(Q, @"..\..\..\work\multitest_out.dat");

        }

        private static void TestWriteMultilayerGML()
        {
            MultilayerNetwork Q = MultilayerNetworkSerializer.ReadMultilayerNetworkFromFile(@"..\..\..\work\multilayer_test.dat", true);
            MultilayerNetworkGMLSerializer.WriteMultiLayerNetworkToFile(Q, @"..\..\..\work\multilayer_test.gml");
        }

        private static void TestReadMultilayerGML()
        {
            try
            {
                MultilayerNetwork Q = MultilayerNetworkGMLSerializer.ReadNetworkFromFile(@"..\..\..\work\multilayer_test.gml");
                MultilayerNetworkGMLSerializer.WriteMultiLayerNetworkToFile(Q, @"..\..\..\work\multilayer_test_regurgitated.gml");
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
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
                G = NetworkSerializer.ReadNetworkFromFile(@"..\..\..\work\electrical.dat", true);
                H = NetworkSerializer.ReadNetworkFromFile(@"..\..\..\work\flow.dat", true);
                I = NetworkSerializer.ReadNetworkFromFile(@"..\..\..\work\control.dat", true);

                J = NetworkSerializer.ReadNetworkFromFile(@"..\..\..\work\electrical.dat", true);
                K = NetworkSerializer.ReadNetworkFromFile(@"..\..\..\work\flow.dat", true);
                L = NetworkSerializer.ReadNetworkFromFile(@"..\..\..\work\control.dat", true);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e}");
            }

            string[] procIndices = { "electrical", "flow", "control" };
            string[] locIndices = { "PHL", "SLTC" };
            Tuple<string, IEnumerable<string>>[] aspect = { new Tuple<string, IEnumerable<string>>("process", procIndices), new Tuple<string, IEnumerable<string>>("site", locIndices) };
            MultilayerNetwork Q = new MultilayerNetwork(aspect, true);
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
            Q.AddEdge(new NodeLayerTuple(1, "electrical,SLTC"), new NodeLayerTuple(2, "control,SLTC"), 2);
            Q.AddEdge(new NodeLayerTuple(2, "electrical,SLTC"), new NodeLayerTuple(3, "control,SLTC"), 2);
            Q.AddEdge(new NodeLayerTuple(3, "control,PHL"), new NodeLayerTuple(1, "control,SLTC"), 4);

            // add intralayer edge
            Q.AddEdge(new NodeLayerTuple(4, "flow,SLTC"), new NodeLayerTuple(5, "flow,SLTC"), 2);

            // try to add edge with non-existent vertex
            try
            {
                // layer does not exist
                Q.AddEdge(new NodeLayerTuple(7, "fusion,SLTC"), new NodeLayerTuple(8, "fusion, SLTC"), 1);
            }
            catch (ArgumentException)
            {
                

            }

            try
            {
                // interlayer, vertex does not exist -- vertex should NOT be added
                Q.AddEdge(new NodeLayerTuple(26, "electrical,PHL"), new NodeLayerTuple(1, "flow,PHL"), 2);
            }
            catch (ArgumentException)
            {

            }

            try
            {
                // intralayer, vertex does not exist -- vertex should be added
                Q.AddEdge(new NodeLayerTuple(26, "flow,SLTC"), new NodeLayerTuple(2, "flow,SLTC"), 2);
            }
            catch (ArgumentException)
            {

            }
            double edgeWt = Q.EdgeWeight(new NodeLayerTuple(4, "flow,SLTC"), new NodeLayerTuple(5, "flow,SLTC"));
            edgeWt = Q.EdgeWeight(new NodeLayerTuple(3, "control,PHL"), new NodeLayerTuple(1, "control,SLTC"));

            Dictionary<NodeLayerTuple, float> neighbors = Q.GetNeighbors(new NodeLayerTuple(1, "electrical,SLTC"));
            neighbors = Q.GetNeighbors(new NodeLayerTuple(26, "flow,SLTC"));
            neighbors = Q.GetNeighbors(new NodeLayerTuple(4, "flow,SLTC"));
            neighbors = Q.GetNeighbors(new NodeLayerTuple(3, "electrical,PHL"));

            neighbors = Q.CategoricalGetNeighbors(new NodeLayerTuple(2, "electrical,PHL"), "process");
            neighbors = Q.CategoricalGetNeighbors(new NodeLayerTuple(2, "electrical,PHL"), "process", true);

            neighbors = Q.CategoricalGetNeighbors(new NodeLayerTuple(1, "flow,PHL"), "process");

            List<uint> verts = Q.UniqueVertices();

           // Q.RemoveVertex(new NodeLayerTuple(1, "control,SLTC"));
            Q.AddVertex(new NodeLayerTuple(19, "control,PHL"));
            //Q.RemoveVertex(new NodeLayerTuple(19, "control,PHL"));

            //Q.RemoveEdge(new NodeLayerTuple(1, "electrical,SLTC"), new NodeLayerTuple(2, "control,SLTC"));
           // Q.RemoveElementaryLayer(index); // remove (control, SLTC)
            string[] coord = { "electrical", "SLTC" };
            MultilayerNetworkSerializer.WriteMultiLayerNetworkToFile(Q, @"..\..\..\work\multilayer_test.dat");
        }

    }
}
