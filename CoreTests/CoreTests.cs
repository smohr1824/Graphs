using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networks.Core;

namespace CoreTests
{
    [TestClass]
    public class CoreTests
    {
        private TestContext testContextInstance;

        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        private MultilayerNetwork M;
        private Network G;

        [TestInitialize]
        public void Initialize()
        {
            // uncomment when we have multiple tests requiring large, random networks
            // MakeRandomGraph(ref G, 100, 10);
            // MakeRandomMultilayerNetwork(ref M, 4, 5, 1000, 100, 10);
        }

        [Ignore]
        [TestCategory("Performance")]
        [TestMethod]
        public void BasicNumberNetwork()
        {
            
            for (int i = 1000; i <= 1000000; i*=10)
            {
                long start, end;
                //G = new Network(true);

                start = DateTime.Now.Ticks;
                MakeRandomGraph(ref G, i, 10, false);
                end = DateTime.Now.Ticks;
                TestContext.WriteLine($"Graph degree {G.Order}, {G.Order * 10} edges time to create {(end - start)/TimeSpan.TicksPerSecond} seconds.");
                TestContext.WriteLine("");
            }
        }
        [TestCategory("Basic")]
        [TestMethod]
        public void TestAdjacencyConstructor()
        {
            float[,] weights = { { 0.0F, 1.0F, 2.0F, 0.0F }, { 0.0F, 0.0F, 1.0F, 3.5F }, { 1.0F, 2.1F, 0.0F, 2.0F }, { 1.0F, 0.0F, 0.0F, 0.0F } };
            string[] nodes = { "A", "B", "C", "D" };
            List<string> vertices = new List<string>(nodes);

            Network G = new Network(vertices, weights, true);

            float[,] ewts = G.AdjacencyMatrix;
            List<string> outV = G.Vertices;

            Assert.AreEqual(nodes.Length, outV.Count, $"Count of vertices input {nodes.Length} does not equal count of vertices output {outV.Count}");

            Assert.AreEqual(G.HasEdge("B", "C"), true);

            Assert.AreEqual(G.EdgeWeight("B", "D"), 3.5, 0.05);

            Assert.AreEqual(G.EdgeWeight("D", "C"), 0.0, 0.05);
        }

        [TestCategory("Basic")]
        [TestMethod]
        public void TestConnectedProperty()
        {
            Network G = new Network(false);
            G.AddEdge("A", "B", 1);
            G.AddVertex("C"); // C is unreachable
            Assert.IsTrue(!G.Connected);

            G = new Network(true);
            G.AddEdge("A", "B", 1);
            G.AddVertex("C"); // C is unreachable
            Assert.IsTrue(!G.Connected);
        }
        

        [TestCategory("Basic")]
        [TestMethod]
        public void TestNetworkVertex()
        {
            Network G = new Network(true);
            G.AddEdge("A", "B", 1);
            G.AddEdge("A", "C", 1);
            G.AddEdge("B", "C", 2);
            G.AddEdge("A", "D", 3);

            Assert.AreEqual(4, G.Order);
            Assert.AreEqual(3, G.GetNeighbors("A").Keys.Count);
            Assert.AreEqual(true, G.HasEdge("B", "C"));
            Assert.AreEqual(false, G.HasEdge("B", "A"));
            Assert.AreEqual(3, G.OutDegree("A"));

            G.RemoveVertex("C");
            Assert.AreEqual(false, G.HasVertex("C"));
            Assert.AreEqual(false, G.HasEdge("A", "C"));
        }

        [TestCategory("Basic")]
        [TestMethod]
        public void TestDensity()
        {
            Network G = new Network(true);
            G.AddEdge("A", "B", 1);
            G.AddEdge("A", "C", 1);
            G.AddEdge("B", "C", 2);
            G.AddEdge("A", "D", 3);

            double density = G.Density;
            Assert.AreEqual(0.33, density, 0.01);

            G = new Network(false);
            G.AddEdge("A", "B", 1);
            G.AddEdge("A", "C", 1);
            G.AddEdge("B", "C", 2);
            G.AddEdge("A", "D", 3);

            density = G.Density;
            Assert.AreEqual(0.66, density, 0.01);
        }

        [TestCategory("Basic")]
        [TestMethod]
        public void TestSize()
        {
            Network G = MakeSimple(true);
            int size = G.Size;
            Assert.AreEqual(9, size);
            G = MakeSimple(false);
            size = G.Size;
            Assert.AreEqual(9, size);
        }

        private Network MakeSimple(Boolean directed)
        {
            Network G = new Network(directed);
            G.AddEdge("A", "B", 1);
            G.AddEdge("A", "C", 1);
            G.AddEdge("A", "F", 1);
            G.AddEdge("B", "D", 1);
            G.AddEdge("D", "F", 1);
            G.AddEdge("C", "E", 1);
            G.AddEdge("E", "F", 1);
            G.AddEdge("B", "E", 1);
            G.AddEdge("C", "D", 1);
            return G;
        }
        [TestCategory("Multilayer")]
        [TestMethod]
        public void TestMultilayerSources()
        {
            MultilayerNetwork Q = MultilayerNetworkSerializer.ReadMultilayerNetworkFromFile(@"..\..\work\multilayer_test.dat", true);
            Dictionary<NodeTensor, float> sources = Q.GetSources(new NodeTensor("B", "control,SLTC"));
            Assert.AreEqual(sources.Count, 5);
            NodeTensor test = new NodeTensor("A", "control,PHL");
            bool check = sources.ContainsKey(test);
            float wt = sources[test];
            Assert.AreEqual(sources.ContainsKey(test), true);
            Assert.AreEqual(sources[test], 1.0F, 0.01F);

            sources = Q.CategoricalGetSources(new NodeTensor("A", "control,SLTC"), "process");
            Assert.AreEqual(sources.Count, 1);
            test = new NodeTensor("C", "control,PHL");
            Assert.AreEqual(sources.ContainsKey(test), true);
            Assert.AreEqual(sources[test], 4.0F, 0.01F);

            sources = Q.CategoricalGetSources(new NodeTensor("D", "flow,PHL"), "site");
            test = new NodeTensor("A", "flow,PHL");
            NodeTensor test2 = new NodeTensor("B", "control,PHL");
            Assert.AreEqual(sources.Count, 2);
            Assert.AreEqual(sources.ContainsKey(test), true);
            Assert.AreEqual(sources[test], 1.0F, 0.01F);
            Assert.AreEqual(sources.ContainsKey(test2), true);

            Dictionary<NodeTensor, float> neighbors = Q.CategoricalGetNeighbors(new NodeTensor("A", "control,PHL"), "site");
            Assert.AreEqual(neighbors.Count, 6);
            Assert.AreEqual(neighbors.ContainsKey(test2), true);
        }

        //[TestMethod]
        //public void TestRandomGraph()
        //{
            //Network n = MakeRandomGraph(5, 4);
        //    MultilayerNetwork M = MakeRandomMultilayerNetwork(3, 4, 20, 5, 6);
        //}

        [TestMethod]
        public void TestMultilayerSerialization()
        {
            MakeRandomMultilayerNetwork(ref M, 3, 4, 20, 5, 6);

            TestContext.WriteLine($"Random network created, {M.Aspects().Count()} aspects, Order {M.Order}");
            foreach (string aspect in M.Aspects())
            {
                TestContext.WriteLine($"Aspect {aspect}:");
                foreach (string index in M.Indices(aspect))
                {
                    TestContext.WriteLine($"\t{index}");
                }
            }
            int aspectCt = M.Aspects().Count();

            int order = M.Order;
            List<string> coord1 = new List<string>();
            List<string> coord2 = new List<string>();

            // lowest and hightest NodeTensors
            for (int i = 0; i < aspectCt; i++)
            {
                coord1.Add(M.Indices(M.Aspects().ElementAt(i)).ElementAt(0));
                coord2.Add(M.Indices(M.Aspects().ElementAt(i)).ElementAt(M.Indices(M.Aspects().ElementAt(i)).Count() - 1));
            }

            int vtx = 0;
            while (!M.HasVertex(new NodeTensor($"V{vtx}", coord1)) && vtx < M.UniqueVertices().Count())
                vtx++;

            int degree1 = M.Degree(new NodeTensor($"V{vtx}", coord1));

            int vtx2 = 0;
            while (!M.HasVertex(new NodeTensor($"V{vtx2}", coord1)) && vtx2 < M.UniqueVertices().Count())
                vtx2++;

            int degree2 = M.Degree(new NodeTensor($"V{vtx2}", coord2));

            string testFile = "TestMulti.txt";

            try
            {
                MultilayerNetworkSerializer.WriteMultiLayerNetworkToFile(M, testFile);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to write network to {testFile}: " + ex.Message);
            }

            MultilayerNetwork MPrime = null;

            try
            {
                MPrime = MultilayerNetworkSerializer.ReadMultilayerNetworkFromFile(testFile, true);
                TestContext.WriteLine($"Random network read, {MPrime.Aspects().Count()} aspects, Order {MPrime.Order}");
                foreach (string aspect in M.Aspects())
                {
                    TestContext.WriteLine($"Aspect {aspect}:");
                    foreach (string index in M.Indices(aspect))
                    {
                        TestContext.WriteLine($"\t{index}");
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to read network from {testFile}: " + ex.Message);
                File.Delete(testFile);
            }

            //
            int orderPrime = -1;
            int degree1Prime = -1;
            int degree2Prime = -1;
            try
            {
                orderPrime = MPrime.Order;
                degree1Prime = M.Degree(new NodeTensor($"V{vtx}", coord1));
                degree2Prime = M.Degree(new NodeTensor($"V{vtx2}", coord2));
            }
            catch (Exception ex)
            {
                Assert.Fail($"Error getting order or degree: {ex.Message}");
                File.Delete(testFile);
            }

            Assert.AreEqual(order, orderPrime, $"Orders do not agree");
            Assert.AreEqual(degree1, degree1Prime, $"Degrees of lowest node tensor do not agree");
            Assert.AreEqual(degree2, degree2Prime, $"Degrees of highest node tensor do not agree");
            File.Delete(testFile);
            
            //
        }

        #region initialization for random multilayer networks

        /// <summary>
        /// Creates a multilayer network for testing.  There will be a high degree of node coupling (identity edges) due to the way vertices are named.
        /// While the MultilayerNetwork class permits the number of actual elementary layers to be less than the possible number of  elementary layers (i.e., the number of possible 
        /// permutations of aspect indices), the networks returned from this method will have all possible elementary layers instantiated.
        /// </summary>
        /// <param name="aspectCt">Maximum number of aspects; range is [2..aspectCt]</param>
        /// <param name="maxIndices">Maximum number of discrete values per aspect, range is [1..maxIndices]</param>
        /// <param name="maxVertices">Maximum number of vertices to create per elementary layer.  Given the naming convention for vertices, this will
        ///  also be the maximum order for the network.</param>
        /// <param name="maxDegree">Maximum degree per vertex, neglecting node coupling, i.e., maximum number of explicit edges to create.</param>
        /// <param name="maxInterLayerEdges">Maximum number of explicit interlayer edges to create per elementary layer.</param>
        /// <returns>Instance of a multilayer network with no omitted elementary layers and parameters as above.</returns>
        private void MakeRandomMultilayerNetwork(ref MultilayerNetwork mNet, int aspectCt, int maxIndices, int maxVertices, int maxDegree, int maxInterLayerEdges)
        {
            // Create the aspects and their indices, then instantiate the multilayer network and determine how many elementary layers it can contain.
            IEnumerable<Tuple<string, IEnumerable<string>>> aspects = MakeRandomAspects(aspectCt, maxIndices);
            mNet = new MultilayerNetwork(aspects, true);
            int elementaryCt = CountCross(aspects);

            // Generate a list of elementary layer tensors such that each possible permutation is created.
            List<List<string>> coords = new List<List<string>>();

            for (int i = 0; i < elementaryCt; i++)
            {
                coords.Add(new List<string>());
            }

            int step = 1;
            int indicesLength = 1;
            foreach (Tuple<string, IEnumerable<string>> tpl in aspects)
            {
                // This ugly code is needed to ensure we generate all possible permutations of the indices on each aspect.
                // There are 2..n possible aspects, and each aspect will have 1..k discrete indices.  Index values are arbitrary strings.
                // The first aspect rotates through its indices on each iteration.  Each subsequent aspect rotates to a new index value each j iterations of the loop, where j is the product 
                // of the number of indices of each prior aspect.  Yes, it is ugly.  Yes, it works.  Go ahead, try it out on paper.
                int ptr = 0;
                indicesLength = tpl.Item2.Count<string>();

                List<string> indices = tpl.Item2.ToList();
                for (int k = 0; k < elementaryCt; k++)
                {
                    coords[k].Add(indices[ptr]);
                    if (k % step == 0)
                    {
                        ptr++;
                        if (ptr == indicesLength)
                            ptr = 0;
                    }
                }
                step *= indicesLength;
            }

            // Make a random network to form the basis of each elementary layer, then add an elementary layer using the network and
            // one of the elementary layer tensors for the previosuly created list.
            for (int i = 0; i < elementaryCt; i++)
            {
                Network G = new Network(true);
                MakeRandomGraph(ref G, maxVertices, maxDegree);
                mNet.AddElementaryLayer(coords[i], G);
            }

            // Now that all the elemntary layers exist, create interlayer edges.  The while loops are needed to ensure the vertices
            // generated exist in the elementary layers and that no self-edges are created.
            Random rand = new Random(DateTime.UtcNow.Millisecond);
            for (int j = 0; j < maxInterLayerEdges; j++)
            {
                int source = rand.Next(elementaryCt);
                int target = rand.Next(elementaryCt);
                while (target == source)
                    target = rand.Next(elementaryCt);

                int srcV = rand.Next(maxVertices);
                while (!mNet.HasVertex(new NodeTensor($"V{srcV}", coords[source])))
                    srcV = rand.Next(maxVertices);

                int tgtV = rand.Next(maxVertices);
                while (!mNet.HasVertex(new NodeTensor($"V{tgtV}", coords[target])) || srcV == tgtV)
                    tgtV = rand.Next(maxVertices);

                // If the edge already exists, skip it.
                if (!mNet.HasEdge(new NodeTensor($"V{srcV}", coords[source]), new NodeTensor($"V{tgtV}", coords[target])))
                    mNet.AddEdge(new NodeTensor($"V{srcV}", coords[source]), new NodeTensor($"V{tgtV}", coords[target]), 1);
                
            }

        }

        /// <summary>
        /// Construct a graph with a randomly selected number of vertices and randomly selected degree per vertex created.
        /// The target of each edge is randomly selected and is guaranteed not to be a self-edge or duplicate.  If a duplicate is generated,
        /// the edge will not be created and the final degree of the vertex will be less than the randomly selected degree.
        /// </summary>
        /// <param name="maxVertices">Maximum number of vertices to create in the graph.  Actual order will be [1..maxVertices]</param>
        /// <param name="maxDegree">Maximum degree.  Actual number of edges will be [0..maxDegree]</param>
        /// <returns></returns>
        private void MakeRandomGraph(ref Network net, int maxVertices, int maxDegree, bool randomMax = true)
        {
            net = new Network(true);
            try
            {
                Random rand = new Random(DateTime.UtcNow.Millisecond);
                int order = maxVertices;
                if (randomMax)
                    order = rand.Next(1, maxVertices + 1);

                for (int i = 0; i < order; i++)
                {
                    net.AddVertex($"V{i}");
                }

                for (int j = 0; j < order; j++)
                {
                    int degree = maxDegree;
                    if (randomMax)
                        degree = rand.Next(maxDegree + 1);

                    for (int k = 0; k < degree; k++)
                    {
                        int tgt = rand.Next(maxVertices);
                        while (tgt == j)
                            tgt = rand.Next(maxVertices);

                        if (!net.HasEdge($"V{j}", $"V{tgt}"))
                            net.AddEdge($"V{j}", $"V{tgt}", 1);
                    }
                }
            }
            catch (OutOfMemoryException)
            {
                TestContext.WriteLine($"Out of memory: order {net.Order}");
                Assert.Fail($"Failure to generate a random network, maxVertices {maxVertices}, maxDegree {maxDegree}, randomMax is {randomMax}");
            }
            catch (IOException)
            { }

        }

        /// <summary>
        /// Randomly create [2..aspects] number of aspects and [1..indices] index values per aspect
        /// and return as a colleciton of aspects and indices suitable for use in instantiation of a multilayer network
        /// </summary>
        /// <param name="aspects">Maximum number of aspects to create.  Since we are testing a multilayer network and wish to 
        /// test multiple aspects, the number of aspects created will be [2..aspects].  Aspect names are of the form "Aspect_[n]</param>
        /// <param name="indices">Maximum number of discrete indices to create per aspect.  The actual number will be [1..indices].
        /// The name of each index value for an aspect named "Aspect_[n] will be of the form "A[n]_[k]". </param>
        /// <returns>Enumerable collection of tuples, each of which describes an aspect and consists of the aspect name in Item1
        /// and an enumerable collection of indices in Item2.</returns>
        private IEnumerable<Tuple<string, IEnumerable<string>>> MakeRandomAspects(int aspects, int indices)
        {
            List<Tuple<string, IEnumerable<string>>> aspectCollection = new List<Tuple<string, IEnumerable<string>>>();
            Random rand = new Random(DateTime.UtcNow.Millisecond);
            int aspectCt = rand.Next(2, aspects + 1);

            for (int i = 0; i < aspectCt; i++)
            {
                string aspectName = $"Aspect_{i}";
                int indexCt = rand.Next(1, indices + 1);
                List<string> indexNames = new List<string>();
                for (int k = 0; k < indexCt; k++)
                {
                    string indexName = $"A{i}_{k}";
                    indexNames.Add(indexName);
                }
                aspectCollection.Add(new Tuple<string, IEnumerable<string>>(aspectName, indexNames));
            }

            return aspectCollection;
        }

        private int CountCross(IEnumerable<Tuple<string, IEnumerable<string>>> collection)
        {
            int count = 1;
            foreach (Tuple<string, IEnumerable<string>> tple in collection)
            {
                count *= tple.Item2.Count<string>();
            }

            return count;
        }
        #endregion

        
    }
}
