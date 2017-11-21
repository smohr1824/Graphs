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
        private Dictionary<string, Dictionary<string, float>> EdgeList;

        // We need to keep track of edges into any given vertex so that we can handle in-edge deletion when deleting a vertex.  Since we are
        // taking a memory and performance hit for this, keeping the edge weight allows us to cheaply calculate the weight of inbound edges.
        private Dictionary<string, Dictionary<string, float>> InEdges;

        private bool directed;

        public Network(bool bDirected)
        {
            EdgeList = new Dictionary<string, Dictionary<string, float>>();
            InEdges = new Dictionary<string, Dictionary<string, float>>();
            directed = bDirected;
        }

        public Network(List<string> vertices, float[,] weights, bool bDirected)
        {
            if (vertices == null)
                throw new ArgumentNullException("Vertex list must be non-null");

            if (weights == null)
                throw new ArgumentNullException("Adjacency matrix must be non-null");

            int vertexCount = vertices.Count();
            if (vertexCount != weights.GetLength(0) || vertexCount != weights.GetLength(1) || vertexCount == 0)
                throw new ArgumentException($"Adjacency matrix must be square, have the same dimensions as the vertex list, and be non-zero; vertices count: {vertices.Count()}, weights row count: {weights.GetLength(0)}, weights column count: {weights.GetLength(1)}");

            EdgeList = new Dictionary<string, Dictionary<string, float>>();
            InEdges = new Dictionary<string, Dictionary<string, float>>();
            directed = bDirected;

            for (int i = 0; i < vertexCount; i++)
            {
                Dictionary<string, float> adjacencyList = new Dictionary<string, float>();

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
                            Dictionary<string, float> dict = new Dictionary<string, float>();
                            dict.Add(vertices[i], weights[i, k]);
                            InEdges.Add(vertices[k], dict);
                        }
                    }

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
             get { return EdgeList.Keys.ToList<string>(); }
        }

        public bool Directed
        {
            get { return directed; }
        }

        /// <summary>
        /// Returns true if the graph is connected, i.e., there are no unreachable vertices
        /// </summary>
        public bool Connected
        {
            get { return IsConnected(); }
        }

        /// <summary>
        /// Order (vertex count) of the graph
        /// </summary>
        public int Order
        {
            get { return EdgeList.Keys.Count; }
        }

        /// <summary>
        /// Calculates the density of the network.  For undirected networks, this is 2|E|/(|V| * (|V| - 1 )), where |E| is the number of edges, |V| is the number of vertices (order),
        /// and thus |V| * (|V| - 1) is the number of possible edges in the network.  For a directed network, the density is |E|/(|V| * (|V| - 1)). Note, this method iterates through 
        /// all vertices, so very large networks will have a performance hit O(n).
        /// </summary>
        public double Density
        {
            get
            {
                int edgeCt = CountEdges();
                int order = EdgeList.Keys.Count();

                // since our undirected edges are stored as reciprocal directed edges, the computation is the same, i.e., DO NOT multiply by 2 in the undirected case
                return ((double) edgeCt / ((double) order * ((double) order - 1)));

            }
        }

        public int Size
        {
            get { return CountEdges(); }
        }

        /// <summary>
        /// Creates a new adjacency matrix current as of the time of calling this method
        /// </summary>
        public float[,] AdjacencyMatrix
        {
            get { return MakeAdjacencyMatrix(); }
        }

        #endregion

        #region public methods
        public void AddVertex(string id)
        {
            Dictionary<string, float> neighbors;

            // make sure EdgeList and InEdges have entries for the vertex
            if (!EdgeList.TryGetValue(id, out neighbors))
            {
                neighbors = new Dictionary<string, float>();
                EdgeList.Add(id, neighbors);
            }

            if (!InEdges.Keys.Contains(id))
            {
                InEdges.Add(id, new Dictionary<string, float>());
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

        public void AddEdge(string from, string to, float weight)
        {
            if (from == to)
                throw new ArgumentException($"Self-edges are not permitted (vertex {from})");

            Dictionary<string, float> neighbors;

            // multiple edges between the same two vertices are not permitted
            if (HasEdge(from, to))
                return;

            if (!EdgeList.TryGetValue(from, out neighbors))
            {
                neighbors = new Dictionary<string, float>(); 
                neighbors.Add(to, weight);
                EdgeList.Add(from, neighbors);
                InEdges.Add(from, new Dictionary<string, float>());
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
                neighbors = new Dictionary<string, float>();
                EdgeList.Add(to, neighbors);
                InEdges.Add(to, new Dictionary<string, float>());
            }

            if (InEdges.Keys.Contains(to))
            {
                InEdges[to].Add(from, weight);
            }
            else
            {
                Dictionary<string, float> dict = new Dictionary<string, float>();
                dict.Add(from, weight);
                InEdges.Add(to, dict);
            }

            // if this is an undirected edge, add the reciprocal edge as well
            if (!directed)
            {
                // The first clause should never be hit since we added the check for the to vertex above
                if (!EdgeList.TryGetValue(to, out neighbors))
                {
                    neighbors = new Dictionary<string, float>(); 
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
                    Dictionary<string, float> dict = new Dictionary<string, float>();
                    dict.Add(to, weight);
                    InEdges.Add(from, dict);
                }
            }
            }

        public void RemoveEdge(string from, string to)
        {
            Dictionary<string, float> neighbors = new Dictionary<string, float>();

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
        public Dictionary<string, float> GetNeighbors(string vertex)
        {
            Dictionary<string, float> nhood = null;
            if (!EdgeList.TryGetValue(vertex, out nhood))
            {
                return new Dictionary<string, float>();
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
        public Dictionary<string, float> GetSources(string vertex)
        {
            Dictionary<string, float> nhood = null;
            if (!InEdges.TryGetValue(vertex, out nhood))
            {
                return new Dictionary<string, float>();
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
            Dictionary<string, float> adjList;
            if (EdgeList.TryGetValue(from, out adjList))
            {
                float wt = 0.0F;
                if (adjList.TryGetValue(to, out wt))
                    return true;
            }

            return false;
        }

        public float EdgeWeight(string from, string to)
        {
            if (HasEdge(from, to))
                return EdgeList[from][to];
            else
                return 0.0F;
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
                Dictionary<string, float> neighbors = null;
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
        
        // TODO: How valuable is this? Keeping the edge weights nearly doubles the storage.
        public float InWeights(string vertex)
        {
            if (!HasVertex(vertex))
                throw new ArgumentException($"Vertex {vertex} is not a member of the graph.");

            float retVal = 0F;
            if (!directed)
            {
                foreach (KeyValuePair<string, float> edge in EdgeList[vertex])
                    retVal += edge.Value;

                return retVal;
            }


            foreach (KeyValuePair<string, float> inEdge in InEdges[vertex])
            {
                retVal += inEdge.Value;
            }

            return retVal;
        }

        
        public float OutWeights(string vertex)
        {
            float retVal = 0F;

            if (!HasVertex(vertex))
                throw new ArgumentException($"Vertex {vertex} is not a member of the graph.");

            if (!directed)
            {
                foreach (KeyValuePair<string, float> edge in EdgeList[vertex])
                    retVal += edge.Value;

                return retVal;
            }

            foreach (KeyValuePair<string, float> kvp in EdgeList[vertex])
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
                Dictionary<string, float> value = new Dictionary<string, float>(EdgeList[key]);
                retVal.EdgeList.Add(key, value);
                Dictionary<string, float> srcs = new Dictionary<string, float>(InEdges[key]);
                retVal.InEdges.Add(key, srcs);
            }
            return retVal;
        }
        public void List(TextWriter writer, char delimiter)
        {
            foreach (string key in EdgeList.Keys)
            {
                Dictionary<string, float> targets = EdgeList[key];
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

        private float[,] MakeAdjacencyMatrix()
        {
            int dimension = EdgeList.Keys.Count();
            float[,] retVal = new float[dimension, dimension];
            List<string> vertices = Vertices;

            foreach (string vertex in EdgeList.Keys)
            {
                int i = vertices.IndexOf(vertex);

                foreach (string to in EdgeList[vertex].Keys)
                {
                    int j = vertices.IndexOf(to);
                    Dictionary<string, float> edges = EdgeList[vertex];
                    float weight = 0.0F;
                    if (edges.TryGetValue(to, out weight))
                    {
                        retVal[i, j] = weight;
                    }
                }
            }

            return retVal;
        }

        private int CountEdges()
        {
            int retVal = 0;
            foreach (Dictionary<string, float> adjList in EdgeList.Values)
                retVal += adjList.Keys.Count;

            return retVal;
        }

        private bool IsConnected()
        {
            bool retVal = true;

            foreach (Dictionary<string, float> adjList in InEdges.Values)
            {
                if (adjList.Keys.Count == 0)
                    return false;
            }

            return retVal;
        }
        #endregion
    }
}
