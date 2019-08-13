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

namespace Networks.Core
{
    internal class ElementaryLayer
    {
        // adjacency list of interlayer edges
        private Dictionary<uint, Dictionary<ResolvedNodeLayerTuple, float>> EdgeList;
        
        // reference count of inbound interlayer edges
        private Dictionary<uint, Dictionary<ResolvedNodeLayerTuple, float>> InEdges;
        private Network G;
        private MultilayerNetwork M;
        private List<int> layerCoordinates;
        internal ElementaryLayer(MultilayerNetwork m, Network g, List<int> coordinates)
        {
            M = m;
            G = g;
            EdgeList = new Dictionary<uint, Dictionary<ResolvedNodeLayerTuple, float>>();
            InEdges = new Dictionary<uint, Dictionary<ResolvedNodeLayerTuple, float>>();
            layerCoordinates = coordinates;
        }

        public List<uint> Vertices
        {
            get { return G.Vertices;  }
        }

        public List<string> AspectCoordinates
        {
            get { return M.UnaliasCoordinates(layerCoordinates); }
        }

        internal List<int> ResolvedCoordinates
        {
            get { return layerCoordinates; }
        }

        public bool HasEdge(ResolvedNodeLayerTuple from, ResolvedNodeLayerTuple to)
        {
            if (from.IsSameElementaryLayer(to))
            {
                // get the elementary layer and check for the edge
                return G.HasEdge(from.nodeId, to.nodeId);
            }
            else
            {
                if (EdgeList.Keys.Contains(from.nodeId))
                {
                    if (EdgeList[from.nodeId].Keys.Contains(to))
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
        }

        public float[,] InterlayerAdjacencies(List<int> to)
        {
            int size = G.Order;
            List<uint> vertices = G.Vertices;
            float[,] retVal = new float[size,size];
            
            foreach (uint from in EdgeList.Keys)
            {
                Dictionary<ResolvedNodeLayerTuple, float> adjList = EdgeList[from];
                int fromIndex = vertices.IndexOf(from);
                foreach (ResolvedNodeLayerTuple tgt in adjList.Keys)
                {
                    if (tgt.IsSameElementaryLayer(to))
                    {
                        // place the edge weight at the correct location
                        int toIndex = vertices.IndexOf(tgt.nodeId);
                        retVal[fromIndex, toIndex] = adjList[tgt];
                    }
                }
            }
            return retVal;

        }

        public float[,] LayerAdjacencyMatrix => G.AdjacencyMatrix;

        public Network CopyGraph()
        {
            return G.Clone();
        }

        public bool HasVertex(uint vertex)
        {
            return G.HasVertex(vertex);
        }

        public void AddVertex(uint vertex)
        {
            G.AddVertex(vertex);
        }

        public int Order()
        {
            return G.Order;
        }

        public int Degree(uint vertex)
        {
            return G.Degree(vertex);
        }

        internal int InterLayerDegree(uint vertex)
        {
            if (!HasVertex(vertex))
                throw new ArgumentException($"Vertex {vertex} is not a member of the graph.");

            //ResolvedNodeLayerTuple tuple = new ResolvedNodeLayerTuple();
            //tuple.nodeId = vertex;
            //tuple.coordinates = layerCoordinates;

            int retVal = 0;

            if (EdgeList.Keys.Contains(vertex))
               retVal = EdgeList[vertex].Keys.Count;

            if (InEdges.Keys.Contains(vertex))
                retVal += InEdges[vertex].Keys.Count;

            return retVal;
        }

        internal int InDegree(uint vertex)
        {
            //ResolvedNodeLayerTuple tuple = new ResolvedNodeLayerTuple();
            //tuple.nodeId = vertex;
            //tuple.coordinates = layerCoordinates;

            int retVal = 0;

            try
            {
                retVal = G.InDegree(vertex);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Vertex {vertex} is not a member of the graph.");
            }

            if (InEdges.Keys.Contains(vertex))
                retVal += InEdges[vertex].Keys.Count;

            return retVal;
        }

        internal int OutDegree(uint vertex)
        {
            //ResolvedNodeLayerTuple tuple = new ResolvedNodeLayerTuple();
            //tuple.nodeId = vertex;
            //tuple.coordinates = layerCoordinates;

            int retVal = 0;

            try
            {
                retVal = G.OutDegree(vertex);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Vertex {vertex} is not a member of the graph.");
            }

            if (EdgeList.Keys.Contains(vertex))
                retVal += EdgeList[vertex].Keys.Count;

            return retVal;
        }

        public void RemoveVertex(uint vertex)
        {
            // remove from the network
            G.RemoveVertex(vertex);

            // remove any interlayer edges
            ResolvedNodeLayerTuple nodeT = new ResolvedNodeLayerTuple();
            nodeT.nodeId = vertex;
            nodeT.coordinates = layerCoordinates;

            if (EdgeList.Keys.Contains(vertex))
            {
                EdgeList.Remove(vertex);
            }

            if (InEdges.Keys.Contains(vertex))
            {
                foreach (ResolvedNodeLayerTuple target in InEdges[vertex].Keys)
                {
                    M.RemoveOutEdge(target, nodeT);
                }
                InEdges.Remove(vertex);
            }

        }

        public void AddInEdge(ResolvedNodeLayerTuple from, ResolvedNodeLayerTuple to, float wt)
        {
            if (HasVertex(from.nodeId))
            {
                if (InEdges.Keys.Contains(from.nodeId))
                    InEdges[from.nodeId].Add(to, wt);
                else
                {
                    Dictionary<ResolvedNodeLayerTuple, float> dict = new Dictionary<ResolvedNodeLayerTuple, float>(new ResolvedNodeLayerTupleEqualityComparer());
                    dict.Add(to, wt);
                    InEdges.Add(from.nodeId, dict);
                }
            }

        }
        public void RemoveOutEdge(ResolvedNodeLayerTuple from, ResolvedNodeLayerTuple to)
        {
            // Don't use RemoveEdge so as to avoid endless cycle of DeleteInEdge
            if (EdgeList.Keys.Contains(from.nodeId))
                EdgeList[from.nodeId].Remove(to);
        }

        public void RemoveInEdge(ResolvedNodeLayerTuple tgt, ResolvedNodeLayerTuple src)
        {
            // src is the from vertex with an edge to this layer's tgt vertex
            if (InEdges.Keys.Contains(tgt.nodeId))
                InEdges[tgt.nodeId].Remove(src);
        }

        public float EdgeWeight(ResolvedNodeLayerTuple rFrom, ResolvedNodeLayerTuple rTo)
        {
            if (rFrom.IsSameElementaryLayer(rTo))
            {
                // get the elementary layer and check for the edge
                return G.EdgeWeight(rFrom.nodeId, rTo.nodeId);
            }
            else
            {
                if (EdgeList.Keys.Contains(rFrom.nodeId))
                {
                    if (EdgeList[rFrom.nodeId].Keys.Contains(rTo))
                    {
                        float retVal;
                        EdgeList[rFrom.nodeId].TryGetValue(rTo, out retVal);
                        return retVal;
                    }
                    else
                        return 0;
                }
                else
                    return 0;
            }
        }

        public void AddEdge(ResolvedNodeLayerTuple from, ResolvedNodeLayerTuple to, float wt)
        {
            if (!from.coordinates.SequenceEqual(layerCoordinates))
                throw new ArgumentException($"Trying to add an edge to the wrong elementary layer. Source vertex is {from.nodeId + ":" + string.Join(",", M.UnaliasCoordinates(from.coordinates))}, layer aspect coordinates are {string.Join(",", M.UnaliasCoordinates(layerCoordinates))}");

            if (from.IsSameElementaryLayer(to))
            {
                G.AddEdge(from.nodeId, to.nodeId, wt);
            }
            else
            {
                if (EdgeList.Keys.Contains(from.nodeId))
                {
                    if (EdgeList[from.nodeId].Keys.Contains(to))
                        EdgeList[from.nodeId][to] = wt;
                    else
                        EdgeList[from.nodeId].Add(to, wt);
                }
                else
                {
                    Dictionary<ResolvedNodeLayerTuple, float> dict = new Dictionary<ResolvedNodeLayerTuple, float>(new ResolvedNodeLayerTupleEqualityComparer());
                    dict.Add(to, wt);
                    EdgeList.Add(from.nodeId, dict);
                }
            }

        }

        public void RemoveEdge(ResolvedNodeLayerTuple from, ResolvedNodeLayerTuple to)
        {
            if (!from.coordinates.SequenceEqual(layerCoordinates))
                throw new ArgumentException($"Trying to remove an edge from the wrong elementary layer. Source vertex is {from.nodeId + ":" + string.Join(",", M.UnaliasCoordinates(from.coordinates))}, layer aspect coordinates are {string.Join(",", M.UnaliasCoordinates(layerCoordinates))}");

            if (from.IsSameElementaryLayer(to))
            {
                G.RemoveEdge(from.nodeId, to.nodeId);
            }
            else
            {
                if (EdgeList.Keys.Contains(from.nodeId))
                {
                    EdgeList[from.nodeId].Remove(to);
                }
            }
                
        }

        internal Dictionary<NodeLayerTuple, float> GetNeighbors(uint vertex)
        {
            Dictionary<NodeLayerTuple, float> retVal = new Dictionary<NodeLayerTuple, float>();
            if (!HasVertex(vertex))
                return retVal;

            Dictionary<uint, float> graphNeighbors = G.GetNeighbors(vertex);

            List<string> layerAspectCoords = M.UnaliasCoordinates(layerCoordinates);

            // get the neighbors in the layer
            foreach (uint node in graphNeighbors.Keys)
            {
                NodeLayerTuple local = new NodeLayerTuple(node, layerAspectCoords);
                retVal.Add(local, graphNeighbors[node]);
            }

            // add out of layer neighbors, i.e., targets of interlayer edges
            //ResolvedNodeLayerTuple refTuple = new ResolvedNodeLayerTuple(vertex, layerCoordinates);

            if (EdgeList.Keys.Contains(vertex))
            {
                foreach (ResolvedNodeLayerTuple tuple in EdgeList[vertex].Keys)
                {
                    NodeLayerTuple tgt = new NodeLayerTuple(tuple.nodeId, M.UnaliasCoordinates(tuple.coordinates));
                    retVal.Add(tgt, EdgeList[vertex][tuple]);
                }
            }

            return retVal;
        }

        internal Dictionary<NodeLayerTuple, float> GetSources(uint vertex)
        {
            Dictionary<NodeLayerTuple, float> retVal = new Dictionary<NodeLayerTuple, float>();

            if (!HasVertex(vertex))
                return retVal;

            Dictionary<uint, float> graphSources = G.GetSources(vertex);

            List<string> layerAspectCoords = M.UnaliasCoordinates(layerCoordinates);

            // get the neighbors in the layer
            foreach (uint node in graphSources.Keys)
            {
                NodeLayerTuple local = new NodeLayerTuple(node, layerAspectCoords);
                retVal.Add(local, graphSources[node]);
            }

            // add out of layer neighbors, i.e., targets of interlayer edges
            //ResolvedNodeLayerTuple refTuple = new ResolvedNodeLayerTuple(vertex, layerCoordinates);

            if (InEdges.Keys.Contains(vertex))
            {
                foreach (ResolvedNodeLayerTuple tuple in InEdges[vertex].Keys)
                {
                    NodeLayerTuple tgt = new NodeLayerTuple(tuple.nodeId, M.UnaliasCoordinates(tuple.coordinates));
                    retVal.Add(tgt, InEdges[vertex][tuple]);
                }
            }

            return retVal;
        }

        public void List(TextWriter writer, char delimiter)
        {
            G.List(writer, delimiter);

            if (EdgeList.Keys.Count() > 0)
                writer.WriteLine(@":Interlayer edges");
            foreach (uint from in EdgeList.Keys)
            {
                Dictionary<ResolvedNodeLayerTuple, float> targets = EdgeList[from];
                foreach (ResolvedNodeLayerTuple to in targets.Keys)
                {
                    writer.WriteLine(from.ToString() + ":" + string.Join(",", M.UnaliasCoordinates(layerCoordinates)) + delimiter + to.nodeId.ToString() + ":" + string.Join(",", M.UnaliasCoordinates(to.coordinates)) + delimiter + targets[to].ToString());
                }
            }
        }

        public void ListLayerGML(TextWriter writer, int level)
        {
            G.ListGML(writer, level);

        }

        public void ListInterlayerGML(TextWriter writer)
        {
            string indent = "\t";
            foreach (uint from in EdgeList.Keys)
            {
                Dictionary<ResolvedNodeLayerTuple, float> targets = EdgeList[from];
                foreach (ResolvedNodeLayerTuple to in targets.Keys)
                {
                    writer.WriteLine(indent + "edge [");

                    writer.WriteLine(indent + "\tsource [");
                    writer.WriteLine(indent + "\t\tid " + from.ToString());
                    writer.WriteLine(indent + "\t\tcoordinates " + string.Join(",", M.UnaliasCoordinates(layerCoordinates)));
                    writer.WriteLine(indent + "\t]");

                    writer.WriteLine(indent + "\ttarget [");
                    writer.WriteLine(indent + "\t\tid " + to.nodeId.ToString());
                    writer.WriteLine(indent + "\t\tcoordinates " + string.Join(",", M.UnaliasCoordinates(to.coordinates)));
                    writer.WriteLine(indent + "\t]");

                    writer.WriteLine(indent + "\tweight " + targets[to].ToString());
                    writer.WriteLine(indent + "]");
                }
            }

        }

    }
}
