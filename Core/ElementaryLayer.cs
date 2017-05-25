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
    internal class ElementaryLayer
    {
        // adjacency list of interlayer edges
        private Dictionary<ResolvedNodeTensor, Dictionary<ResolvedNodeTensor, double>> EdgeList;
        
        // reference count of inbound interlayer edges
        private Dictionary<ResolvedNodeTensor, Dictionary<ResolvedNodeTensor, double>> InEdges;
        private Network G;
        private MultilayerNetwork M;
        private List<int> layerCoordinates;
        internal ElementaryLayer(MultilayerNetwork m, Network g, List<int> coordinates)
        {
            M = m;
            G = g;
            EdgeList = new Dictionary<ResolvedNodeTensor, Dictionary<ResolvedNodeTensor, double>>(new NodeTensorEqualityComparer());
            InEdges = new Dictionary<ResolvedNodeTensor, Dictionary<ResolvedNodeTensor, double>>(new NodeTensorEqualityComparer());
            layerCoordinates = coordinates;
        }

        public List<string> Vertices
        {
            get { return G.Vertices;  }
        }
        public bool HasEdge(ResolvedNodeTensor from, ResolvedNodeTensor to)
        {
            if (from.IsSameElementaryLayer(to))
            {
                // get the elementary layer and check for the edge
                return G.HasEdge(from.nodeId, to.nodeId);
            }
            else
            {
                if (EdgeList.Keys.Contains(from))
                {
                    if (EdgeList[from].Keys.Contains(to))
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
        }

        public bool HasVertex(string vertex)
        {
            return G.HasVertex(vertex);
        }

        public void AddVertex(string vertex)
        {
            G.AddVertex(vertex);
        }

        public void RemoveVertex(string vertex)
        {
            // remove from the network
            G.RemoveVertex(vertex);

            // remove any interlayer edges
            ResolvedNodeTensor nodeT = new ResolvedNodeTensor();
            nodeT.nodeId = vertex;
            nodeT.coordinates = layerCoordinates;

            if (EdgeList.Keys.Contains(nodeT))
            {
                EdgeList.Remove(nodeT);
            }

            if (InEdges.Keys.Contains(nodeT))
            {
                foreach (ResolvedNodeTensor target in InEdges[nodeT].Keys)
                {
                    M.RemoveOutEdge(target, nodeT);
                }
                InEdges.Remove(nodeT);
            }

        }

        public void AddInEdge(ResolvedNodeTensor from, ResolvedNodeTensor to, double wt)
        {
            if (HasVertex(from.nodeId))
            {
                if (InEdges.Keys.Contains(from))
                    InEdges[from].Add(to, wt);
                else
                {
                    Dictionary<ResolvedNodeTensor, double> dict = new Dictionary<ResolvedNodeTensor, double>(new NodeTensorEqualityComparer());
                    dict.Add(to, wt);
                    InEdges.Add(from, dict);
                }
            }

        }
        public void RemoveOutEdge(ResolvedNodeTensor from, ResolvedNodeTensor to)
        {
            // Don't use RemoveEdge so as to avoid endless cycle of DeleteInEdge
            if (EdgeList.Keys.Contains(from))
                EdgeList[from].Remove(to);
        }

        public void RemoveInEdge(ResolvedNodeTensor tgt, ResolvedNodeTensor src)
        {
            // src is the from vertex with an edge to this layer's tgt vertex
            if (InEdges.Keys.Contains(tgt))
                InEdges[tgt].Remove(src);
        }

        public double EdgeWeight(ResolvedNodeTensor rFrom, ResolvedNodeTensor rTo)
        {
            if (rFrom.IsSameElementaryLayer(rTo))
            {
                // get the elementary layer and check for the edge
                return G.EdgeWeight(rFrom.nodeId, rTo.nodeId);
            }
            else
            {
                if (EdgeList.Keys.Contains(rFrom))
                {
                    if (EdgeList[rFrom].Keys.Contains(rTo))
                    {
                        double retVal;
                        EdgeList[rFrom].TryGetValue(rTo, out retVal);
                        return retVal;
                    }
                    else
                        return 0;
                }
                else
                    return 0;
            }
        }

        public void AddEdge(ResolvedNodeTensor from, ResolvedNodeTensor to, double wt)
        {
            if (!from.coordinates.SequenceEqual(layerCoordinates))
                throw new ArgumentException($"Trying to add an edge to the wrong elementary layer. Source vertex is {from.nodeId + ":" + string.Join(",", M.UnaliasCoordinates(from.coordinates))}, layer aspect coordinates are {string.Join(",", M.UnaliasCoordinates(layerCoordinates))}");

            if (from.IsSameElementaryLayer(to))
            {
                G.AddEdge(from.nodeId, to.nodeId, wt);
            }
            else
            {
                if (EdgeList.Keys.Contains(from))
                {
                    if (EdgeList[from].Keys.Contains(to))
                        EdgeList[from][to] = wt;
                    else
                        EdgeList[from].Add(to, wt);
                }
                else
                {
                    Dictionary<ResolvedNodeTensor, double> dict = new Dictionary<ResolvedNodeTensor, double>(new NodeTensorEqualityComparer());
                    dict.Add(to, wt);
                    EdgeList.Add(from, dict);
                }
            }

        }

        public void RemoveEdge(ResolvedNodeTensor from, ResolvedNodeTensor to, bool directed)
        {
            if (!from.coordinates.SequenceEqual(layerCoordinates))
                throw new ArgumentException($"Trying to remove an edge from the wrong elementary layer. Source vertex is {from.nodeId + ":" + string.Join(",", M.UnaliasCoordinates(from.coordinates))}, layer aspect coordinates are {string.Join(",", M.UnaliasCoordinates(layerCoordinates))}");

            if (from.IsSameElementaryLayer(to))
            {
                G.RemoveEdge(from.nodeId, to.nodeId);
            }
            else
            {
                if (EdgeList.Keys.Contains(from))
                {
                    EdgeList[from].Remove(to);
                }
            }
                
        }

        public void List(TextWriter writer, char delimiter)
        {
            G.List(writer, delimiter);

            foreach (ResolvedNodeTensor from in EdgeList.Keys)
            {
                Dictionary<ResolvedNodeTensor, double> targets = EdgeList[from];
                foreach (ResolvedNodeTensor to in targets.Keys)
                {
                    writer.WriteLine(from.nodeId + ":" + string.Join(",", M.UnaliasCoordinates(from.coordinates)) + delimiter + to.nodeId + ":" + string.Join(",", M.UnaliasCoordinates(to.coordinates)) + delimiter + targets[to].ToString());
                }
            }
        }

    }
}
