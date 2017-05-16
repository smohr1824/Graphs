// Copyright 2017 -- Stephen T. Mohr, OSIsoft, LLC
// Licensed under the MIT license

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{
    /// <summary>
    /// Class modelling a graph.  All edges are treated as directed.  If an undirected graph is loaded using the serializer, reciprocal edges are added.
    /// In this way, we do not need to distinguish between directed and undirected graphs at the class level.
    /// </summary>
    public class Network
    {
        // each key in the edge list is the id of a from vertex.  The value is an adjacency list composed of a dictionary of to vertex ids and the edge weight
        // May need to revisit if scaling becomes an issue.  Uniqueness of entries is paramount, but Dictionary is resource intensive.
        private Dictionary<string, Dictionary<string, double>> EdgeList;

        public Network()
        {
            EdgeList = new Dictionary<string, Dictionary<string, double>>();
        }

        public Network(List<string> vertices, double[,] weights)
        {
            if (vertices == null)
                throw new ArgumentNullException("Vertex list must be non-null");

            if (weights == null)
                throw new ArgumentNullException("Adjacency matrix must be non-null");

            int vertexCount = vertices.Count();
            if (vertexCount != weights.GetLength(0) || vertexCount != weights.GetLength(1) || vertexCount == 0)
                throw new ArgumentException($"Adjacency matrix must be square, have the same dimensions as the vertex list, and be non-zero; vertices count: {vertices.Count()}, weights row count: {weights.GetLength(0)}, weights column count: {weights.GetLength(1)}");

            EdgeList = new Dictionary<string, Dictionary<string, double>>();

            for (int i = 0; i < vertexCount; i++)
            {
                Dictionary<string, double> adjacencyList = new Dictionary<string, double>();
                for (int k = 0; k < vertexCount; k++)
                {
                    if (k == i)
                        continue;

                    if (weights[i, k] != 0)
                        adjacencyList.Add(vertices[k], weights[i, k]);
                }

                EdgeList.Add(vertices[i], adjacencyList);
            }
        }

        #region public properties

        /// <summary>
        /// List of vertices in the graph -- makes a new copy, so use cautiously
        /// </summary>
        public List<string> Vertices
        {
             get { return EdgeList.Keys.ToList(); }
        }

        /// <summary>
        /// Order (vertex count) of the graph
        /// </summary>
        public int Order
        {
            get { return EdgeList.Keys.Count; }
        }

        /// <summary>
        /// Creates a new adjacency matrix current as of the time of calling this method
        /// </summary>
        public double[,] AdjacencyMatrix
        {
            get { return MakeAdjacencyMatrix(); }
        }

        #endregion

        #region public methods
        public void AddVertex(string id)
        {
            Dictionary<string, double> neighbors;
            if (!EdgeList.TryGetValue(id, out neighbors))
            {
                neighbors = new Dictionary<string, double>();
                EdgeList.Add(id, neighbors);
            }
        }

        public void RemoveVertex(string id)
        {
            // Remove the vertex only after checking each neighboring node for reciprocal edges
            Dictionary<string, double> neighbors, nextNeighbors;
            if (EdgeList.TryGetValue(id, out neighbors))
            {
                // specified vertex exists and we have a dictionary of neighboring vertices
                foreach (string vertex in neighbors.Keys)
                {
                    if (EdgeList.TryGetValue(vertex, out nextNeighbors))
                    {
                        // no need to check if there is a reciprocal edge, i.e., this is an undirected graph, Remove can simply fail with return value false
                        nextNeighbors.Remove(id);
                    }
                    //neighbors.Remove(vertex);
                }

                EdgeList.Remove(id);
            }
        }

        public void AddEdge(string from, string to, double weight, bool directed)
        {
            Dictionary<string, double> neighbors;
            if (!EdgeList.TryGetValue(from, out neighbors))
            {
                neighbors = new Dictionary<string, double>(); 
                neighbors.Add(to, weight);
                EdgeList.Add(from, neighbors);
            }
            else
            {
                // found from, check for to
                if (!neighbors.ContainsKey(to))
                {
                    neighbors.Add(to, weight);
                }
                else
                {
                    neighbors[to] = weight;
                }
            }

            // check for the existence of the to vertex and create if needed
            if (!EdgeList.TryGetValue(to, out neighbors))
            {
                neighbors = new Dictionary<string, double>();
                EdgeList.Add(to, neighbors);
            }

            // if this is an undirected edge, add the reciprocal edge as well
            if (!directed)
            {
                // The first clause should never be hit since we added the check for the to vertex above
                if (!EdgeList.TryGetValue(to, out neighbors))
                {
                    neighbors = new Dictionary<string, double>(); 
                    neighbors.Add(from, weight);
                    EdgeList.Add(to, neighbors);
                }
                else
                {
                    if (!neighbors.ContainsKey(from))
                    {
                        neighbors.Add(from, weight);
                    }
                    else
                    {
                        neighbors[from] = weight;
                    }
                }
            }
        }

        public void RemoveEdge(string from, string to, bool directed)
        {
            Dictionary<string, double> neighbors = new Dictionary<string, double>();
            if (HasEdge(from, to))
            {
                EdgeList.TryGetValue(from, out neighbors);
                neighbors.Remove(to);

                if (!directed)
                {
                    if (EdgeList.TryGetValue(to, out neighbors))
                        neighbors.Remove(from);
                }
            }
        }

        public Dictionary<string, double> GetNeighbors(string node)
        {
            Dictionary<string, double> nhood = null;
            if (!EdgeList.TryGetValue(node, out nhood))
            {
                return new Dictionary<string, double>();
            }
            else
            {
                return nhood;
            }
        }

        public bool HasEdge(string from, string to)
        {
            Dictionary<string, double> adjList;
            if (EdgeList.TryGetValue(from, out adjList))
            {
                double wt = 0.0;
                if (adjList.TryGetValue(to, out wt))
                    return true;
            }

            return false;
        }

        public double EdgeWeight(string from, string to)
        {
            if (HasEdge(from, to))
                return EdgeList[from][to];
            else
                return 0.0;
        }

        public int Degree(string node)
        {
            Dictionary<string, double> neighbors = null;
            if (!EdgeList.TryGetValue(node, out neighbors))
                return 0;
            else
                return neighbors.Count();

        }


        public void List(TextWriter writer, char delimiter)
        {
            foreach (string key in EdgeList.Keys)
            {
                Dictionary<string, double> targets = EdgeList[key];
                foreach (string to in targets.Keys)
                {
                    writer.Write(key + delimiter + to + delimiter + targets[to].ToString() + Environment.NewLine);
                }
            }
        }
        #endregion

        #region private methods

        private double[,] MakeAdjacencyMatrix()
        {
            int dimension = EdgeList.Keys.Count();
            double[,] retVal = new double[dimension, dimension];
            List<string> vertices = Vertices;

            foreach (string vertex in EdgeList.Keys)
            {
                int i = vertices.IndexOf(vertex);

                foreach (string to in EdgeList[vertex].Keys)
                {
                    int j = vertices.IndexOf(to);
                    Dictionary<string, double> edges = EdgeList[vertex];
                    double weight = 0.0;
                    if (edges.TryGetValue(to, out weight))
                    {
                        retVal[i, j] = weight;
                    }
                }
            }

            return retVal;
        }
        #endregion
    }
}
