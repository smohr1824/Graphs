﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{
    public partial class MultilayerNetwork
    {
        private List<string> aspects;
        private List<List<string>> axes;

        private bool directed;

        // Network-centric with interlayer edges
        private Dictionary<List<int>, ElementaryLayer> elementaryLayers;

        // concordance of vertex ids and the elementary layers in which they appear
        private Dictionary<string, List<ElementaryLayer>> nodeIdsAndLayers;
        private bool categoricalEdges;

        public MultilayerNetwork(IEnumerable<Tuple<string, IEnumerable<string>>> dimensions, bool isdirected, bool isCategoricallyConnected = true)
        {
            aspects = new List<string>();
            axes = new List<List<string>>();
            nodeIdsAndLayers = new Dictionary<string, List<ElementaryLayer>>();
            elementaryLayers = new Dictionary<List<int>, ElementaryLayer>(new CoordinateTensorEqualityComparer());

            categoricalEdges = isCategoricallyConnected;
            directed = isdirected;

            int index = 0;
            foreach (Tuple<string, IEnumerable<string>> axisValue in dimensions)
            {
                aspects.Add(axisValue.Item1);
                axes.Add(axisValue.Item2.ToList<string>());
                index++;
            }

        }

        #region public properties

        public int Order
        {
            get { return nodeIdsAndLayers.Keys.Count();  }
        }

        #endregion

        #region public methods

        public bool NodeAligned
        {
            get { return categoricalEdges; }
        }
        public bool HasVertex(NodeTensor vertex)
        {
            ResolvedNodeTensor rVertex = ResolveNodeTensor(vertex);

            if (rVertex == null)
                return false;
            else
            {
                if (elementaryLayers.Keys.Contains(rVertex.coordinates))
                {
                    return elementaryLayers[rVertex.coordinates].HasVertex(vertex.nodeId);
                }
                else
                    return false;
            }
        }

        public bool HasEdge(NodeTensor from, NodeTensor to)
        {
            ResolvedNodeTensor rFrom = ResolveNodeTensor(from);
            ResolvedNodeTensor rTo = ResolveNodeTensor(to);

            if (rFrom == null || rTo == null)
                return false;

            return elementaryLayers[rFrom.coordinates].HasEdge(rFrom, rTo);
        }

        public double EdgeWeight(NodeTensor from, NodeTensor to)
        {
            ResolvedNodeTensor rFrom = ResolveNodeTensor(from);
            ResolvedNodeTensor rTo = ResolveNodeTensor(to);

            if (rFrom == null || rTo == null)
                return 0;

            if (ElementaryLayerExists(rFrom.coordinates))
                return elementaryLayers[rFrom.coordinates].EdgeWeight(rFrom, rTo);
            else
                return 0;
        }

        public bool AddElementaryLayer(IEnumerable<string> coordinates, Network G)
        {
            if (coordinates == null)
                throw new ArgumentException(@"Layer coordinates cannot be null");

            if (G == null)
                throw new ArgumentException(@"Graph cannot be null.");

            if (G.Directed != directed)
                throw new ArgumentException($"Layers and the network must both be directed or undirected. Network directed is {directed}, layer directed is {G.Directed}");

            List<int> resolved = ResolveCoordinates(coordinates.ToList<string>());
            if (resolved != null)
            {
                if (AddElementaryNetworkToMultiLayerNetwork(resolved, G))
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public bool RemoveElementaryLayer(IEnumerable<string> coordinates)
        {
            if (coordinates == null)
                throw new ArgumentException(@"Coordinates cannot be null");

            List<int> resolved = ResolveCoordinates(coordinates.ToList());
            if (resolved != null)
            {
                return RemoveElementaryNetworkFromMultiLayerNetwork(resolved);
            }
            else
                return false;
        }



        public void List(TextWriter writer, char delimiter)
        {
            if (categoricalEdges)
                writer.WriteLine(@"Categorically connected");
            else
                writer.WriteLine(@"Not categorically connected");

            for (int i = 0; i < aspects.Count(); i++)
            {
                writer.Write(aspects[i] + ": ");
                writer.WriteLine(string.Join(",", axes[i]));
            }

            foreach (List<int> layerCoord in elementaryLayers.Keys)
            {
                List<string> aspectCoords = UnaliasCoordinates(layerCoord);
                writer.WriteLine(string.Join(",", aspectCoords));
                elementaryLayers[layerCoord].List(writer, delimiter);
                writer.WriteLine(@"");
            }


        }

        public void AddVertex(NodeTensor vertex)
        {
            ResolvedNodeTensor rVertex = ResolveNodeTensor(vertex);
            if (rVertex == null)
                throw new ArgumentException($"Aspect coordinates could not be resolved for {vertex}.");

            if (!ElementaryLayerExists(rVertex.coordinates))
                throw new ArgumentException($"Elementary layer does not exist at {vertex.aspectCoordinates}");

            // elementary layer exists
            ElementaryLayer layer = elementaryLayers[rVertex.coordinates];
            if (!layer.HasVertex(rVertex.nodeId))
            {
                elementaryLayers[rVertex.coordinates].AddVertex(rVertex.nodeId);
                List<ElementaryLayer> layers;
                if (!nodeIdsAndLayers.TryGetValue(rVertex.nodeId, out layers))
                {
                    List<ElementaryLayer> layerList = new List<ElementaryLayer>();
                    layerList.Add(layer);
                    nodeIdsAndLayers.Add(rVertex.nodeId, layerList);
                }
                else
                {
                    // The vertex exists somewhere in the multilayer network.
                    // search to see if it is already in the concordance

                    if (!layers.Contains(layer))
                        layers.Add(layer);
                }
            }
        }

        public void RemoveVertex(NodeTensor vertex)
        {
            ResolvedNodeTensor rVertex = ResolveNodeTensor(vertex);
            if (rVertex == null)
                throw new ArgumentException($"Aspect coordinates could not be resolved for {vertex}.");

            if (!ElementaryLayerExists(rVertex.coordinates))
                throw new ArgumentException($"Elementary layer does not exist at {vertex.aspectCoordinates}");

            ElementaryLayer layer = elementaryLayers[rVertex.coordinates];
            if (layer.HasVertex(rVertex.nodeId))
            {
                // remove from elementary layer
                layer.RemoveVertex(rVertex.nodeId);
                if (nodeIdsAndLayers.Keys.Contains(rVertex.nodeId))
                {
                    nodeIdsAndLayers[rVertex.nodeId].Remove(elementaryLayers[rVertex.coordinates]);
                    if (nodeIdsAndLayers[rVertex.nodeId].Count() == 0)
                        nodeIdsAndLayers.Remove(rVertex.nodeId);
                }
            }
        }

        public void AddEdge(NodeTensor from, NodeTensor to, double wt)
        {
            ResolvedNodeTensor rFrom = ResolveNodeTensor(from);
            ResolvedNodeTensor rTo = ResolveNodeTensor(to);

            // if the tensor cannot be resolved, one or both elementary layers does not exist
            if (rFrom == null || rTo == null || !ElementaryLayerExists(rFrom.coordinates) || !ElementaryLayerExists(rTo.coordinates))
                throw new ArgumentException($"The elementary layer for one or more vertices does not exist (vertices passed {from.ToString()}, {to.ToString()}");

            // ensure the vertices exist in their respective elementary layers; if not, create
            ElementaryLayer fromLayer = elementaryLayers[rFrom.coordinates];
            ElementaryLayer toLayer = elementaryLayers[rTo.coordinates];

            // intralayer add, special case -- network can add vertices and will handle in-edges
            if (fromLayer == toLayer)
            {
                if (!fromLayer.HasVertex(rFrom.nodeId))
                {
                    if (nodeIdsAndLayers.Keys.Contains(rFrom.nodeId))
                        nodeIdsAndLayers[rFrom.nodeId].Add(fromLayer);
                    else
                    {
                        List<ElementaryLayer> layers = new List<ElementaryLayer>();
                        layers.Add(fromLayer);
                        nodeIdsAndLayers.Add(rFrom.nodeId, layers);
                    }
                }
                if (!fromLayer.HasVertex(rTo.nodeId))
                {
                    if (nodeIdsAndLayers.Keys.Contains(rTo.nodeId))
                        nodeIdsAndLayers[rTo.nodeId].Add(fromLayer);
                    else
                    {
                        List<ElementaryLayer> layers = new List<ElementaryLayer>();
                        layers.Add(fromLayer);
                        nodeIdsAndLayers.Add(rTo.nodeId, layers);
                    }
                }
                fromLayer.AddEdge(rFrom, rTo, wt);
            }

            if (!fromLayer.HasVertex(from.nodeId) || !toLayer.HasVertex(to.nodeId))
                throw new ArgumentException($"Edge cannot be added unless both vertices exist (from: {from}, to: {to}).");

            elementaryLayers[rFrom.coordinates].AddEdge(rFrom, rTo, wt);
            elementaryLayers[rTo.coordinates].AddInEdge(rTo, rFrom, wt);

            if (!directed)
            {
                elementaryLayers[rTo.coordinates].AddEdge(rTo, rFrom, wt);
                elementaryLayers[rFrom.coordinates].AddInEdge(rFrom, rTo, wt);
            }
        }


        public void RemoveEdge(NodeTensor from, NodeTensor to)
        {
            ResolvedNodeTensor rFrom = ResolveNodeTensor(from);
            ResolvedNodeTensor rTo = ResolveNodeTensor(to);

            // if the tensor cannot be resolved, one or both elementary layers does not exist
            if (rFrom == null || rTo == null || !ElementaryLayerExists(rFrom.coordinates) || !ElementaryLayerExists(rTo.coordinates))
                throw new ArgumentException($"The elementary layer for one or more vertices does not exist (vertices passed {from.ToString()}, {to.ToString()}");

            ElementaryLayer fromLayer = elementaryLayers[rFrom.coordinates];
            ElementaryLayer toLayer = elementaryLayers[rTo.coordinates];
            if (fromLayer.HasEdge(rFrom, rTo))
            {
                fromLayer.RemoveEdge(rFrom, rTo, directed);
                toLayer.RemoveInEdge(rTo, rFrom);
            }

            // if this is an undirected edge, remove the reciprocal edge
            if (!directed && toLayer.HasEdge(rTo, rFrom))
            {
                toLayer.RemoveEdge(rTo, rFrom, directed);
                fromLayer.RemoveInEdge(rFrom, rTo);
            }
        }

        internal void RemoveOutEdge(ResolvedNodeTensor from, ResolvedNodeTensor to)
        {
            if (ElementaryLayerExists(from.coordinates))
            {
                elementaryLayers[from.coordinates].RemoveOutEdge(from, to);
            }
        }

        #endregion

        #region private methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="G"></param>
        /// <remarks>If a vertex id in G is found in the list of existing vertex ids, it is assumed that this is a node relating to an existing actor. Otherwise, vertices are added 
        /// to the list.
        /// </remarks>
        private bool AddElementaryNetworkToMultiLayerNetwork(List<int> coords, Network G)
        {
            List<string> inVertices = G.Vertices;
            ElementaryLayer layer = new ElementaryLayer(this, G, coords);

            foreach (string vertex in inVertices)
            {
                if (nodeIdsAndLayers.ContainsKey(vertex))
                {
                    nodeIdsAndLayers[vertex].Add(layer);
                }
                else
                {
                    List<ElementaryLayer> layers = new List<ElementaryLayer>();
                    layers.Add(layer);
                    nodeIdsAndLayers.Add(vertex, layers);
                }
            }
            try
            {
                elementaryLayers.Add(coords, layer);
            }
            catch (ArgumentNullException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }

        private bool RemoveElementaryNetworkFromMultiLayerNetwork(List<int> resolved)
        {
            if (ElementaryLayerExists(resolved))
            {
                ElementaryLayer layer = elementaryLayers[resolved];
                List<string> vertices = layer.Vertices;

                foreach (string vertex in vertices)
                {
                    if (nodeIdsAndLayers.Keys.Contains(vertex))
                    {
                        // this should always be true
                        if (nodeIdsAndLayers[vertex].Contains(layer))
                        {
                            nodeIdsAndLayers[vertex].Remove(layer);
                            if (nodeIdsAndLayers[vertex].Count() == 0)
                                nodeIdsAndLayers.Remove(vertex);
                        }
                    }
                }
                elementaryLayers.Remove(resolved);
                return true;
            }
            else
                return false;
        }

        private ResolvedNodeTensor ResolveNodeTensor(NodeTensor tensor)
        {
            ResolvedNodeTensor retVal = new ResolvedNodeTensor();
            retVal.nodeId = tensor.nodeId;

            retVal.coordinates = ResolveCoordinates(tensor.aspectCoordinates);
            if (retVal.coordinates == null)
                retVal = null;

            return retVal;
        }

        private List<int> ResolveCoordinates(List<string> coords)
        {
            List<int> retVal = new List<int>();

            for (int i = 0; i < aspects.Count(); i++)
            {
                int index = axes[i].IndexOf(coords[i]);
                if (index != -1)
                    retVal.Add(index);
                else
                {
                    retVal = null;
                    break;
                }
            }

            return retVal;
        }

        internal List<string> UnaliasCoordinates(List<int> coords)
        {
            List<string> retVal = new List<string>();

            for (int i = 0; i < aspects.Count(); i++)
            {
                if (coords[i] > axes[i].Count() - 1)
                    throw new ArgumentOutOfRangeException($"Coordinate value exceeds maximum defined on aspect {axes[i]}");

                string sCoord = axes[i][coords[i]]; // simply debugging
                retVal.Add(sCoord);
            }

            return retVal;
        }

        internal bool ElementaryLayerExists(List<int> coords)
        {
            return elementaryLayers.Keys.Contains(coords);
        }
        #endregion
    }
}
