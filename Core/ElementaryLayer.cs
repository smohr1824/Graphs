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
        private Dictionary<List<int>, int> InLayers;
        private Network G;
        private MultilayerNetwork M;
        private List<int> layerCoordinates;
        internal ElementaryLayer(MultilayerNetwork m, Network g, List<int> coordinates)
        {
            M = m;
            G = g;
            EdgeList = new Dictionary<ResolvedNodeTensor, Dictionary<ResolvedNodeTensor, double>>(new NodeTensorEqualityComparer());
            InLayers = new Dictionary<List<int>, int>(new CoordinateTensorEqualityComparer());
            layerCoordinates = coordinates;
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

        }

        public void RemoveAnyEdgesTo(ResolvedNodeTensor target)
        {
            foreach (ResolvedNodeTensor from in EdgeList.Keys)
            {
                if (EdgeList[from].Keys.Contains(target))
                {
                    EdgeList[from].Remove(target);
                    M.DecrementEdgesFrom(target.coordinates, layerCoordinates);
                }
            }
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

        public void AddEdge(ResolvedNodeTensor from, ResolvedNodeTensor to, double wt, bool directed)
        {
            if (!from.coordinates.SequenceEqual(layerCoordinates))
                throw new ArgumentException($"Trying to add an edge to the wrong elementary layer. Source vertex is {from.nodeId + ":" + string.Join(",", M.UnaliasCoordinates(from.coordinates))}, layer aspect coordinates are {string.Join(",", M.UnaliasCoordinates(layerCoordinates))}");

            if (from.IsSameElementaryLayer(to))
            {
                G.AddEdge(from.nodeId, to.nodeId, wt, directed);
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
                G.RemoveEdge(from.nodeId, to.nodeId, directed);
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

        public void IncrementEdgeFrom(List<int> layerCoordinates)
        {
            // reference count the inlayers entry
            if (M.ElementaryLayerExists(layerCoordinates))
            {
                if (InLayers.Keys.Contains(layerCoordinates))
                    InLayers[layerCoordinates]++;
                else
                    InLayers.Add(layerCoordinates, 1);
            }
        }

        public void DecrementEdgeFrom(List<int> layerCoordinates)
        {
            // reference count the inlayers entry
            if (M.ElementaryLayerExists(layerCoordinates))
            {
                if (InLayers.Keys.Contains(layerCoordinates))
                {
                    InLayers[layerCoordinates]--;
                    if (InLayers[layerCoordinates] == 0)
                        InLayers.Remove(layerCoordinates);
                }
            }
        }
    }
}
