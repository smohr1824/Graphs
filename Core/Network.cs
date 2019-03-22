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

namespace Networks.Core
{
    /// <summary>
    /// Class modelling a graph.  All edges are treated as directed. Neighborhoods for undirected networks are
    /// handled using the in edges.
    /// </summary>
    public class Network
    {
        // each key in the edge list is the id of a from vertex.  The value is an adjacency list composed of a dictionary of to vertex ids and the edge weight
        // May need to revisit if scaling becomes an issue.  Uniqueness of entries is paramount, but Dictionary is resource intensive.
        private Dictionary<uint, Dictionary<uint, float>> EdgeList;

        // We need to keep track of edges into any given vertex so that we can handle in-edge deletion when deleting a vertex.  Since we are
        // taking a memory and performance hit for this, keeping the edge weight allows us to cheaply calculate the weight of inbound edges.
        private Dictionary<uint, Dictionary<uint, float>> InEdges;

        private bool directed;

        public Network(bool bDirected)
        {
            EdgeList = new Dictionary<uint, Dictionary<uint, float>>();
            InEdges = new Dictionary<uint, Dictionary<uint, float>>();
            directed = bDirected;
        }

        public Network(List<uint> vertices, float[,] weights, bool bDirected)
        {
            if (vertices == null)
                throw new ArgumentNullException("Vertex list must be non-null");

            if (weights == null)
                throw new ArgumentNullException("Adjacency matrix must be non-null");

            int vertexCount = vertices.Count();
            if (vertexCount != weights.GetLength(0) || vertexCount != weights.GetLength(1) || vertexCount == 0)
                throw new ArgumentException($"Adjacency matrix must be square, have the same dimensions as the vertex list, and be non-zero; vertices count: {vertices.Count()}, weights row count: {weights.GetLength(0)}, weights column count: {weights.GetLength(1)}");

            EdgeList = new Dictionary<uint, Dictionary<uint, float>>();
            InEdges = new Dictionary<uint, Dictionary<uint, float>>();
            directed = bDirected;

            for (int i = 0; i < vertexCount; i++)
            {
                Dictionary<uint, float> adjacencyList = new Dictionary<uint, float>();

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
                            Dictionary<uint, float> dict = new Dictionary<uint, float>();
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
        public List<uint> Vertices
        {
            get
            {
                List<uint> retVal = EdgeList.Keys.ToList<uint>();
                retVal.Sort();
                return retVal;
            }
        }

        /// <summary>
        /// Provides a vertex id for algorithms requiring an arbitrary starting point, e.g., "do X to any vertex v, then for each adjacent vertex..."
        /// </summary>
        /// <param name="random">if true, randomizes which vertex is selected, otherwise always returns the same vertex assuming the graph has not been changed</param>
        /// <returns>id of a vertex in G</returns>
        public uint StartingVertex(bool random = false)
        {
            if (random)
            {
                Random rnd = new Random();
                return EdgeList.Keys.ElementAt<uint>(rnd.Next(Order));
            }
            else
                return EdgeList.Keys.ElementAt<uint>(0);
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
                double retVal = ((double)edgeCt / ((double)order * ((double)order - 1)));
                if (!Directed)
                    retVal = 2 * retVal;
                return retVal;

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
        public void AddVertex(uint id)
        {
            Dictionary<uint, float> neighbors;

            // make sure EdgeList and InEdges have entries for the vertex
            if (!EdgeList.TryGetValue(id, out neighbors))
            {
                neighbors = new Dictionary<uint, float>();
                EdgeList.Add(id, neighbors);
            }

            if (!InEdges.Keys.Contains(id))
            {
                InEdges.Add(id, new Dictionary<uint, float>());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public void RemoveVertex(uint id)
        {
            if (EdgeList.Keys.Contains(id))
            {
                // traverse the out edges and remove the corresponding in edge entries
                foreach (uint to in EdgeList[id].Keys)
                {
                    InEdges[to].Remove(id);
                }
                // remove the list of outgoing edges for this vertex
                EdgeList.Remove(id);
            }

            if (InEdges.Keys.Contains(id))
            {
                // traverse the list of incoming edges and delete the corresponding out edge entries
                foreach (uint from in InEdges[id].Keys)
                {
                    EdgeList[from].Remove(id);
                }
                // remove the list of incoming edges
                InEdges.Remove(id);
            }

        }

        public void AddEdge(uint from, uint to, float weight)
        {
            if (from == to)
                throw new ArgumentException($"Self-edges are not permitted (vertex {from})");

            Dictionary<uint, float> neighbors;

            // multiple edges between the same two vertices are not permitted
            if (HasEdge(from, to))
                return;

            if (!EdgeList.TryGetValue(from, out neighbors))
            {
                neighbors = new Dictionary<uint, float>(); 
                neighbors.Add(to, weight);
                EdgeList.Add(from, neighbors);
                InEdges.Add(from, new Dictionary<uint, float>());
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
                neighbors = new Dictionary<uint, float>();
                EdgeList.Add(to, neighbors);
                InEdges.Add(to, new Dictionary<uint, float>());
            }

            if (InEdges.Keys.Contains(to))
            {
                InEdges[to].Add(from, weight);
            }
            else
            {
                Dictionary<uint, float> dict = new Dictionary<uint, float>();
                dict.Add(from, weight);
                InEdges.Add(to, dict);
            }
        }

        public void RemoveEdge(uint from, uint to)
        {
            Dictionary<uint, float> neighbors = new Dictionary<uint, float>();

            if (HasEdge(from, to))
            {
                EdgeList.TryGetValue(from, out neighbors);
                neighbors.Remove(to);
                InEdges[to].Remove(from);
            }
        }

        /// <summary>
        ///  Returns vertices neighboring the given vertex such that there are edges from the source to the neighboring target vertices
        ///  Undirected networks return all edges incident on the given vertex.
        /// </summary>
        /// <param name="vertex">source vertex</param>
        /// <returns></returns>
        public Dictionary<uint, float> GetNeighbors(uint vertex)
        {
            Dictionary<uint, float> nhood = null;
            Dictionary<uint, float> inhood = null;
            Dictionary<uint, float> retVal = new Dictionary<uint, float>();

            EdgeList.TryGetValue(vertex, out nhood);
            if (!Directed)
            {
                InEdges.TryGetValue(vertex, out inhood);
            }

            if (nhood != null)
            {
                foreach (KeyValuePair<uint, float> kvp in nhood)
                    retVal.Add(kvp.Key, kvp.Value);
            }

            if (!Directed && inhood != null)
            {
                foreach (KeyValuePair<uint, float> kvp in inhood)
                    retVal.Add(kvp.Key, kvp.Value);
            }

            return retVal;
        }

        /// <summary>
        /// Returns a dictionary of vertices with directed edges terminating at the given vertex.
        /// If working with an undirected network, call GetNeighbors to get all incident edges.
        /// </summary>
        /// <param name="vertex">vertex that is the target of the inbound edges</param>
        /// <returns></returns>
        /// <remarks>Supports back-tracking for bideirectional search</remarks>
        public Dictionary<uint, float> GetSources(uint vertex)
        {
            Dictionary<uint, float> nhood = null;
            if (!InEdges.TryGetValue(vertex, out nhood))
            {
                return new Dictionary<uint, float>();
            }
            else
            {
                return nhood;
            }
        }

        public bool HasVertex(uint node)
        {
            return EdgeList.Keys.Contains(node);
        }
        public bool HasEdge(uint from, uint to)
        {
            Dictionary<uint, float> adjList;
            if (EdgeList.TryGetValue(from, out adjList))
            {
                float wt = 0.0F;
                if (adjList.TryGetValue(to, out wt))
                    return true;
                else
                {
                    // if directed, there is no edge
                    // if undirected, check the in edges
                    if (!Directed)
                    {
                        if (InEdges.TryGetValue(from, out adjList))
                        {
                            if (adjList.TryGetValue(to, out wt))
                                return true;
                            else
                                return false;
                        }
                    }
                    else
                        return false;
                }
            }

            return false;
        }

        public float EdgeWeight(uint from, uint to)
        {
            if (HasEdge(from, to))
            {
                try
                {
                    return EdgeList[from][to];
                }
                catch (Exception)
                {
                    if (!Directed)
                    {
                        try
                        {
                            return InEdges[from][to];
                        }
                        catch (Exception)
                        {
                            return 0.0F;
                        }
                    }
                    else
                        return 0.0F;
                }
            }
            else
                return 0.0F;
        }

        public int Degree(uint vertex)
        {
            if (!HasVertex(vertex))
                throw new ArgumentException($"Vertex {vertex} is not a member of this graph.");

            // return the total of in and out edges
            int retVal = 0;
            if (EdgeList.Keys.Contains(vertex))
                retVal = EdgeList[vertex].Count();
            if (InEdges.Keys.Contains(vertex))
                retVal += InEdges[vertex].Count();

            return retVal;
        }

        public int OutDegree(uint vertex)
        {
            if (!HasVertex(vertex))
                throw new ArgumentException($"Vertex {vertex} is not a member of the network.");

            return EdgeList[vertex].Count();
        }

        public int InDegree(uint vertex)
        {
            if (!HasVertex(vertex))
                throw new ArgumentException($"Vertex {vertex} is not a member of the network.");

            return InEdges[vertex].Count();
        }
        
        public float InWeights(uint vertex)
        {
            if (!HasVertex(vertex))
                throw new ArgumentException($"Vertex {vertex} is not a member of the graph.");

            float retVal = 0F;

            foreach (KeyValuePair<uint, float> inEdge in InEdges[vertex])
            {
                retVal += inEdge.Value;
            }

            if (!directed)
            {
                foreach (KeyValuePair<uint, float> edge in InEdges[vertex])
                    retVal += edge.Value;

                return retVal;
            }

            return retVal;
        }

        
        public float OutWeights(uint vertex)
        {
            float retVal = 0F;

            if (!HasVertex(vertex))
                throw new ArgumentException($"Vertex {vertex} is not a member of the graph.");

            foreach (KeyValuePair<uint, float> kvp in EdgeList[vertex])
            {
                retVal += kvp.Value;
            }

            if (!directed)
            {
                foreach (KeyValuePair<uint, float> edge in InEdges[vertex])
                    retVal += edge.Value;
            }

            return retVal;
        }

        public Network Clone()
        {
            Network retVal = new Network(directed);
            foreach (uint key in EdgeList.Keys)
            {
                Dictionary<uint, float> value = new Dictionary<uint, float>(EdgeList[key]);
                retVal.EdgeList.Add(key, value);
                Dictionary<uint, float> srcs = new Dictionary<uint, float>(InEdges[key]);
                retVal.InEdges.Add(key, srcs);
            }
            return retVal;
        }
        public void List(TextWriter writer, char delimiter)
        {
            foreach (uint key in EdgeList.Keys)
            {
                Dictionary<uint, float> targets = EdgeList[key];
                if (targets.Count() == 0)
                {
                    writer.WriteLine(key.ToString());
                }
                else
                {
                    foreach (uint to in targets.Keys)
                    {
                        writer.Write(key.ToString() + delimiter + to.ToString() + delimiter + targets[to].ToString() + Environment.NewLine);
                    }
                }
            }
        }

        public void ListGML(TextWriter writer)
        {
            writer.WriteLine(@"graph [");
            if (Directed)
                writer.WriteLine("\tdirected 1");
            else
                writer.WriteLine("\tdirected 0");

            ListGMLNodes(writer);
            ListGMLEdges(writer);

            writer.WriteLine(@"]");
        }

        public void ListGMLNodes(TextWriter writer)
        {
            foreach (uint key in Vertices)
            {
                writer.WriteLine("\tnode [");
                writer.WriteLine("\t\tid " + key);
                writer.WriteLine("\t]");
            }
        }

        public void ListGMLEdges(TextWriter writer)
        {
            foreach (uint key in EdgeList.Keys)
            {
                Dictionary<uint, float> edges = EdgeList[key];
                foreach (KeyValuePair<uint, float> kvp in edges)
                {
                    writer.WriteLine("\tedge [");
                    writer.WriteLine("\t\tsource " + key);
                    writer.WriteLine("\t\ttarget " + kvp.Key);
                    writer.WriteLine("\t\tweight " + kvp.Value.ToString("F4"));
                    writer.WriteLine("\t]");
                }
            }
        }
        #endregion

        #region private methods

        private float[,] MakeAdjacencyMatrix()
        {
            int dimension = EdgeList.Keys.Count();
            float[,] retVal = new float[dimension, dimension];
            List<uint> vertices = Vertices;

            foreach (uint vertex in EdgeList.Keys)
            {
                int i = vertices.IndexOf(vertex);

                foreach (uint to in EdgeList[vertex].Keys)
                {
                    int j = vertices.IndexOf(to);
                    Dictionary<uint, float> edges = EdgeList[vertex];
                    float weight = 0.0F;
                    if (edges.TryGetValue(to, out weight))
                    {
                        retVal[i, j] = weight;
                    }
                }
            }

            if (!Directed)
            {
                // pick up the in edges
                foreach (uint vertex in InEdges.Keys)
                {
                    int i = vertices.IndexOf(vertex);

                    foreach (uint to in InEdges[vertex].Keys)
                    {
                        int j = vertices.IndexOf(to);
                        Dictionary<uint, float> edges = InEdges[vertex];
                        float weight = 0.0F;
                        if (edges.TryGetValue(to, out weight))
                        {
                            retVal[i, j] = weight;
                        }
                    }
                }
            }

            return retVal;
        }

        private int CountEdges()
        {
            int retVal = 0;
            foreach (Dictionary<uint, float> adjList in EdgeList.Values)
                retVal += adjList.Keys.Count;

            return retVal;
        }

        private bool IsConnected()
        {
            bool retVal = true;

            foreach (Dictionary<uint, float> adjList in InEdges.Values)
            {
                if (adjList.Keys.Count == 0)
                    return false;
            }

            return retVal;
        }
        #endregion
    }
}
