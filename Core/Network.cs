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

        // Adjacency list of vertices who have edges into the key vertex
        private Dictionary<string, List<string>> InVertices;


        public Network()
        {
            EdgeList = new Dictionary<string, Dictionary<string, double>>();
            InVertices = new Dictionary<string, List<string>>();
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
            InVertices = new Dictionary<string, List<string>>();

            for (int i = 0; i < vertexCount; i++)
            {
                Dictionary<string, double> adjacencyList = new Dictionary<string, double>();
                List<string> inVertices = new List<string>();
                for (int k = 0; k < vertexCount; k++)
                {
                    if (k == i)
                        continue;

                    if (weights[i, k] != 0)
                    {
                        adjacencyList.Add(vertices[k], weights[i, k]);
                        inVertices.Add(vertices[k]);
                    }

                }

                EdgeList.Add(vertices[i], adjacencyList);
                InVertices.Add(vertices[i], inVertices);

            }
        }

        #region public properties

        /// <summary>
        /// List of vertices in the graph -- makes a new copy, so use cautiously
        /// </summary>
        public List<string> Vertices
        {
             get { return EdgeList.Keys.ToList<string>(); }
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
            // remove any edge to the vertex we are removing
            List<string> referencingVertices;
            List<string> nodes = EdgeList[id].Keys.ToList<string>();
            foreach (string node in nodes)
            {
                InVertices[node].Remove(id);
            }
            if (InVertices.TryGetValue(id, out referencingVertices))
            {
                foreach(string refVertex in referencingVertices)
                {
                    Dictionary<string, double> edges;
                    if (EdgeList.TryGetValue(refVertex, out edges))
                    {
                        edges.Remove(id);
                    }
                    InVertices[refVertex].Remove(id);
                }
            }
            EdgeList.Remove(id);
            InVertices.Remove(id);
        }

        public void AddEdge(string from, string to, double weight, bool directed)
        {
            Dictionary<string, double> neighbors;
            List<string> nodes;

            // multiple edges between the same two vertices are not permitted
            if (HasEdge(from, to))
                return;

            if (!EdgeList.TryGetValue(from, out neighbors))
            {
                neighbors = new Dictionary<string, double>(); 
                neighbors.Add(to, weight);
                EdgeList.Add(from, neighbors);
                InVertices.Add(from, new List<string>());
                InVertices[from].Add(to);
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
                nodes = InVertices[from];
                if (!nodes.Contains(to))
                    nodes.Add(to);
            }

            // check for the existence of the to vertex and create if needed
            if (!EdgeList.TryGetValue(to, out neighbors))
            {
                neighbors = new Dictionary<string, double>();
                EdgeList.Add(to, neighbors);
                InVertices.Add(to, new List<string>());
            }

            nodes = InVertices[to];
            if (!nodes.Contains(from))
                nodes.Add(from);

            // if this is an undirected edge, add the reciprocal edge as well
            if (!directed)
            {
                // The first clause should never be hit since we added the check for the to vertex above
                if (!EdgeList.TryGetValue(to, out neighbors))
                {
                    neighbors = new Dictionary<string, double>(); 
                    neighbors.Add(from, weight);
                    EdgeList.Add(to, neighbors);
                    InVertices.Add(to, new List<string>());
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
                nodes = InVertices[to];
                if (!nodes.Contains(from))
                    nodes.Add(from);
            }
        }

        public void RemoveEdge(string from, string to, bool directed)
        {
            Dictionary<string, double> neighbors = new Dictionary<string, double>();

            if (HasEdge(from, to))
            {
                EdgeList.TryGetValue(from, out neighbors);
                neighbors.Remove(to);
                InVertices[to].Remove(from);

                if (!directed)
                {
                    if (EdgeList.TryGetValue(to, out neighbors))
                        neighbors.Remove(from);
                    InVertices[from].Remove(to);
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

        public bool HasVertex(string node)
        {
            return EdgeList.Keys.Contains(node);
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

        public Network Clone()
        {
            Network retVal = new Network();
            foreach (string key in EdgeList.Keys)
            {
                Dictionary<string, double> value = new Dictionary<string, double>(EdgeList[key]);
                retVal.EdgeList.Add(key, value);
                List<string> verts = new List<string>(InVertices[key]);
                retVal.InVertices.Add(key, verts);
            }
            return retVal;
        }
        public void List(TextWriter writer, char delimiter)
        {
            foreach (string key in EdgeList.Keys)
            {
                Dictionary<string, double> targets = EdgeList[key];
                if (targets.Count() == 0)
                {
                    writer.WriteLine(key);
                }
                else
                {
                    foreach (string to in targets.Keys)
                    {
                        writer.Write(key + delimiter + to + delimiter + targets[to].ToString() + Environment.NewLine);
                    }
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
