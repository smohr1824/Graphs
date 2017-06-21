using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networks.Core;

namespace CoreTests
{
    [TestClass]
    public class CoreTests
    {
        [TestCategory("Basic")]
        [TestMethod]
        public void TestAdjacencyConstructor()
        {
            double[,] weights = { { 0.0, 1.0, 2.0, 0.0 }, { 0.0, 0.0, 1.0, 3.5 }, { 1.0, 2.1, 0.0, 2.0 }, { 1.0, 0.0, 0.0, 0.0 } };
            string[] nodes = { "A", "B", "C", "D" };
            List<string> vertices = new List<string>(nodes);

            Network G = new Network(vertices, weights, true);

            double[,] ewts = G.AdjacencyMatrix;
            List<string> outV = G.Vertices;

            Assert.AreEqual(nodes.Length, outV.Count, $"Count of vertices input {nodes.Length} does not equal count of vertices output {outV.Count}");

            Assert.AreEqual(G.HasEdge("B", "C"), true);

            Assert.AreEqual(G.EdgeWeight("B", "D"), 3.5, 0.05);

            Assert.AreEqual(G.EdgeWeight("D", "C"), 0.0, 0.05);
        }

        [TestCategory("Multilayer")]
        [TestMethod]
        public void TestMultilayerSources()
        {
            MultilayerNetwork Q = MultilayerNetworkSerializer.ReadMultilayerNetworkFromFile(@"..\..\work\multilayer_test.dat", true);
            Dictionary<NodeTensor, double> sources = Q.GetSources(new NodeTensor("B", "control,SLTC"));
            Assert.AreEqual(sources.Count, 5);
            NodeTensor test = new NodeTensor("A", "control,PHL");
            bool check = sources.ContainsKey(test);
            double wt = sources[test];
            Assert.AreEqual(sources.ContainsKey(test), true);
            Assert.AreEqual(sources[test], 1.0, 0.01);

            sources = Q.CategoricalGetSources(new NodeTensor("A", "control,SLTC"), "process");
            Assert.AreEqual(sources.Count, 1);
            test = new NodeTensor("C", "control,PHL");
            Assert.AreEqual(sources.ContainsKey(test), true);
            Assert.AreEqual(sources[test], 4.0, 0.01);

            sources = Q.CategoricalGetSources(new NodeTensor("D", "flow,PHL"), "site");
            test = new NodeTensor("A", "flow,PHL");
            NodeTensor test2 = new NodeTensor("B", "control,PHL");
            Assert.AreEqual(sources.Count, 2);
            Assert.AreEqual(sources.ContainsKey(test), true);
            Assert.AreEqual(sources[test], 1.0, 0.01);
            Assert.AreEqual(sources.ContainsKey(test2), true);

            Dictionary<NodeTensor, double> neighbors = Q.CategoricalGetNeighbors(new NodeTensor("A", "control,PHL"), "site");
            Assert.AreEqual(neighbors.Count, 6);
            Assert.AreEqual(neighbors.ContainsKey(test2), true);
        }
    }
}
