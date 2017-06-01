// Copyright 2017 -- Stephen T. Mohr, OSIsoft, LLC
// MIT License

// Copyright(c) 2017 Stephen Mohr and OSIsoft, LLC

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

        // We need to keep track of edges into any given vertex so that we can handle in-edge deletion when deleting a vertex.  Since we are
        // taking a memory and performance hit for this, keeping the edge weight allows us to cheaply calculate the weight of inbound edges.
        private Dictionary<string, Dictionary<string, double>> InEdges;

        private bool directed;

        public Network(bool bDirected)
        {
            EdgeList = new Dictionary<string, Dictionary<string, double>>();
            InEdges = new Dictionary<string, Dictionary<string, double>>();
            directed = bDirected;
        }

        public Network(List<string> vertices, double[,] weights, bool bDirected)
        {
            if (vertices == null)
                throw new ArgumentNullException("Vertex list must be non-null");

            if (weights == null)
                throw new ArgumentNullException("Adjacency matrix must be non-null");

            int vertexCount = vertices.Count();
            if (vertexCount != weights.GetLength(0) || vertexCount != weights.GetLength(1) || vertexCount == 0)
                throw new ArgumentException($"Adjacency matrix must be square, have the same dimensions as the vertex list, and be non-zero; vertices count: {vertices.Count()}, weights row count: {weights.GetLength(0)}, weights column count: {weights.GetLength(1)}");

            EdgeList = new Dictionary<string, Dictionary<string, double>>();
            InEdges = new Dictionary<string, Dictionary<string, double>>();
            directed = bDirected;

            for (int i = 0; i < vertexCount; i++)
            {
                Dictionary<string, double> adjacencyList = new Dictionary<string, double>();
                Dictionary<string, double> inVertices = new Dictionary<string, double>();
                for (int k = 0; k < vertexCount; k++)
                {
                    if (k == i)
                        continue;

                    if (weights[i, k] != 0)
                    {
                        adjacencyList.Add(vertices[k], weights[i, k]);
                        if (InEdges.Keys.Contains(vertices[k]))
                        {
                            InEdges[vertices[k]].Add(vertices[i], weights[i, k]);
                        }
                        else
                        {
                            Dictionary<string, double> dict = new Dictionary<string, double>();
                            dict.Add(vertices[i], weights[i, k]);
                            InEdges.Add(vertices[k], dict);
                        }
                    }

                }

                EdgeList.Add(vertices[i], adjacencyList);
                //InEdges.Add(vertices[i], inVertices);

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

        public bool Directed
        {
            get { return directed; }
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
            if (InEdges.Keys.Contains(id))
            {
                foreach (string from in InEdges[id].Keys)
                {
                    EdgeList[from].Remove(id);
                    if (!directed)
                    {
                        if (EdgeList.Keys.Contains(id))
                            EdgeList[id].Remove(from);

                        if (InEdges.Keys.Contains(from))
                            InEdges[from].Remove(id);
                    }
                }
                InEdges.Remove(id);
            }

            // remove any out edges, and coincidentally remove any awareness of this vertex from the network
            EdgeList.Remove(id);
        }

        public void AddEdge(string from, string to, double weight)
        {
            if (from == to)
                throw new ArgumentException($"Self-edges are not permitted (vertex {from})");

            Dictionary<string, double> neighbors;

            // multiple edges between the same two vertices are not permitted
            if (HasEdge(from, to))
                return;

            if (!EdgeList.TryGetValue(from, out neighbors))
            {
                neighbors = new Dictionary<string, double>(); 
                neighbors.Add(to, weight);
                EdgeList.Add(from, neighbors);
                InEdges.Add(from, new Dictionary<string, double>());
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
                InEdges.Add(to, new Dictionary<string, double>());
            }

            if (InEdges.Keys.Contains(to))
            {
                InEdges[to].Add(from, weight);
            }
            else
            {
                Dictionary<string, double> dict = new Dictionary<string, double>();
                dict.Add(from, weight);
                InEdges.Add(to, dict);
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

                if (InEdges.Keys.Contains(from))
                {
                    InEdges[from].Add(to, weight);
                }
                else
                {
                    Dictionary<string, double> dict = new Dictionary<string, double>();
                    dict.Add(to, weight);
                    InEdges.Add(from, dict);
                }
            }
        }

        public void RemoveEdge(string from, string to)
        {
            Dictionary<string, double> neighbors = new Dictionary<string, double>();

            if (HasEdge(from, to))
            {
                EdgeList.TryGetValue(from, out neighbors);
                neighbors.Remove(to);
                InEdges[to].Remove(from);

                if (!directed)
                {
                    if (EdgeList.TryGetValue(to, out neighbors))
                        neighbors.Remove(from);
                    InEdges[from].Remove(to);
                }
            }
        }

        /// <summary>
        ///  Returns vertices neighboring the given vertex such that there are edges from the source to the neighboring target vertices
        /// </summary>
        /// <param name="vertex">source vertex</param>
        /// <returns></returns>
        public Dictionary<string, double> GetNeighbors(string vertex)
        {
            Dictionary<string, double> nhood = null;
            if (!EdgeList.TryGetValue(vertex, out nhood))
            {
                return new Dictionary<string, double>();
            }
            else
            {
                return nhood;
            }
        }

        /// <summary>
        /// Returns a dictionary of vertices with directed edges terminating at the given vertex
        /// </summary>
        /// <param name="vertex">vertex that is the target of the inbound edges</param>
        /// <returns></returns>
        /// <remarks>Supports back-tracking for bideirectional search</remarks>
        public Dictionary<string, double> GetSources(string vertex)
        {
            Dictionary<string, double> nhood = null;
            if (!InEdges.TryGetValue(vertex, out nhood))
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

        public int Degree(string vertex)
        {
            if (!HasVertex(vertex))
                throw new ArgumentException($"Vertex {vertex} is not a member of this graph.");

            if (directed)
            {
                // return the total of in and out edges
                int retVal = 0;
                if (EdgeList.Keys.Contains(vertex))
                    retVal = EdgeList[vertex].Count();
                if (InEdges.Keys.Contains(vertex))
                    retVal += InEdges[vertex].Count();

                return retVal;
            }
            else
            {
                // only return the number of neighbors, as the practice of adding a reciprocal, directed edge
                // is an implementation decision; the actual degree is the number of edges incident on the vertex
                Dictionary<string, double> neighbors = null;
                if (!EdgeList.TryGetValue(vertex, out neighbors))
                    return 0;
                else
                    return neighbors.Count();
            }

        }

        public int OutDegree(string vertex)
        {
            if (!HasVertex(vertex))
                throw new ArgumentException($"Vertex {vertex} is not a member of the network.");

            return EdgeList[vertex].Count();
        }

        public int InDegree(string vertex)
        {
            if (!HasVertex(vertex))
                throw new ArgumentException($"Vertex {vertex} is not a member of the network.");

            return InEdges[vertex].Count();
        }

        public double InWeights(string vertex)
        {
            if (!HasVertex(vertex))
                throw new ArgumentException($"Vertex {vertex} is not a member of the graph.");

            double retVal = 0;
            if (!directed)
            {
                foreach (KeyValuePair<string, double> edge in EdgeList[vertex])
                    retVal += edge.Value;

                return retVal;
            }


            foreach (KeyValuePair<string, double> inEdge in InEdges[vertex])
            {
                retVal += inEdge.Value;
            }

            return retVal;
        }

        public double OutWeights(string vertex)
        {
            double retVal = 0;

            if (!HasVertex(vertex))
                throw new ArgumentException($"Vertex {vertex} is not a member of the graph.");

            if (!directed)
            {
                foreach (KeyValuePair<string, double> edge in EdgeList[vertex])
                    retVal += edge.Value;

                return retVal;
            }

            foreach (KeyValuePair<string, double> kvp in EdgeList[vertex])
            {
                retVal += kvp.Value;
            }

            return retVal;
        }

        public Network Clone()
        {
            Network retVal = new Network(directed);
            foreach (string key in EdgeList.Keys)
            {
                Dictionary<string, double> value = new Dictionary<string, double>(EdgeList[key]);
                retVal.EdgeList.Add(key, value);
                Dictionary<string, double> srcs = new Dictionary<string, double>(InEdges[key]);
                retVal.InEdges.Add(key, srcs);
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
