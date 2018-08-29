using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networks.Algorithms;
using Networks.Core;

namespace AlgorithmTests
{
    [TestClass]
    public class AlgorithmTests
    {
        [TestMethod]
        public void BipartiteTestBasic()
        {
            Network G = new Network(false);
            G.AddVertex("A");
            G.AddVertex("B");
            G.AddVertex("C");
            G.AddVertex("D");
            G.AddVertex("E");
            G.AddVertex("F");
            G.AddVertex("G");

            // add edges to make a bipartite graph

            G.AddEdge("A", "E", 1.0F);
            G.AddEdge("A", "F", 1.0F);
            G.AddEdge("B", "E", 1.0F);
            G.AddEdge("C", "F", 1.0F);
            G.AddEdge("C", "G", 1.0F);
            G.AddEdge("D", "G", 1.0F);

            List<string> R = null;
            List<string> B = null;
            bool res = Bipartite.IsBipartite(G, out R, out B);
            Assert.IsTrue(res, "Determined not bipartite when it should be bipartite.");

            // add an edge such that G is no longer bipartite
            G.AddEdge("E", "F", 1.0F);
            res = Bipartite.IsBipartite(G, out R, out B);
            Assert.IsFalse(res, "Determined to be bipartite when it should not be");

            // make a digraph corresponding to a Petri net -- it should be bipartite and the R, B lists should be places and transitions
            G = new Network(true);
            G.AddVertex("A");
            G.AddVertex("B");
            G.AddVertex("C");
            G.AddVertex("D");
            G.AddVertex("E");
            G.AddVertex("F");

            G.AddEdge("A", "B", 1.0F);
            G.AddEdge("B", "C", 1.0F);
            G.AddEdge("B", "D", 1.0F);
            G.AddEdge("C", "E", 1.0F);
            G.AddEdge("D", "E", 1.0F);
            G.AddEdge("E", "F", 1.0F);

            res = Bipartite.IsBipartite(G, out R, out B);
            Assert.IsTrue(res, "Petri net found not bipartite");
        }

        [TestMethod]
        public void BipartiteTestDisconnected()
        {
            // create a directed network in which the first two vertices are disconnected, but the network is bipartite
            Network G = new Network(true);
            G.AddVertex("A");
            G.AddVertex("B");
            G.AddVertex("C");
            G.AddVertex("D");
            G.AddVertex("E");

            G.AddEdge("C", "E", 1.0F);
            G.AddEdge("D", "E", 1.0F);

            List<string> R, B;
            bool res = Bipartite.IsBipartite(G, out R, out B);
            Assert.IsTrue(res, "Found bipartite directed network with disconnected vertices to be not bipartite.");
            Assert.AreEqual(2, R.Count, $"Expected two vertices in the Red list of directed network, found {R.Count}");

            // add an edge making the network not bipartite
            G.AddEdge("C", "D", 1.0F);
            res = Bipartite.IsBipartite(G, out R, out B);
            Assert.IsFalse(res, "Found non-bipartite directed network to be bipartite");

            // repeat for undirected network
            G = new Network(false);
            G.AddVertex("A");
            G.AddVertex("B");
            G.AddVertex("C");
            G.AddVertex("D");
            G.AddVertex("E");

            G.AddEdge("C", "E", 1.0F);
            G.AddEdge("D", "E", 1.0F);

            res = Bipartite.IsBipartite(G, out R, out B);
            Assert.IsTrue(res, "Found bipartite undirected network with disconnected vertices to be not bipartite.");
            Assert.AreEqual(2, R.Count, $"Expected two vertices in the Red list of undirected network, found {R.Count}");

            // add an edge making the network not bipartite
            G.AddEdge("C", "D", 1.0F);
            res = Bipartite.IsBipartite(G, out R, out B);
            Assert.IsFalse(res, "Found non-bipartite undirected network to be bipartite");

        }
    }
}
