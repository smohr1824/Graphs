//// MIT License

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
    public partial class MultilayerNetwork
    {
        private List<string> aspects;
        private List<List<string>> axes;

        private bool directed;

        // Network-centric with interlayer edges
        private Dictionary<List<int>, ElementaryLayer> elementaryLayers;

        // concordance of vertex ids and the elementary layers in which they appear
        private Dictionary<string, List<ElementaryLayer>> nodeIdsAndLayers;

        public MultilayerNetwork(IEnumerable<Tuple<string, IEnumerable<string>>> dimensions, bool isdirected = true)
        {
            aspects = new List<string>();
            axes = new List<List<string>>();
            nodeIdsAndLayers = new Dictionary<string, List<ElementaryLayer>>();
            elementaryLayers = new Dictionary<List<int>, ElementaryLayer>(new CoordinateTensorEqualityComparer());

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

        public List<string> UniqueVertices()
        {
            return nodeIdsAndLayers.Keys.ToList<string>();
        }

        #endregion

        #region public methods

        /// <summary>
        /// Finds the degree of a vertex in a specific elementary layer, to include interlayer edges
        /// </summary>
        /// <param name="vertex">Fully qualified node tensor denoting the vertex</param>
        /// <returns>Zero or positive integer count of incident edges</returns>
        /// <remarks>Throws ArgumentException if the referenced elementary layer does not exist or the vertex is not a member of that layer.</remarks>
        public int Degree(NodeTensor vertex)
        {
            List<int> resolved = ResolveCoordinates(vertex.aspectCoordinates);
            if (resolved == null)
                throw new ArgumentException($"Elementary layer referenced by vertex {vertex} does not exist.");

            ElementaryLayer layer = elementaryLayers[resolved];

            int retVal = 0;
            try
            {
                retVal = layer.Degree(vertex.nodeId);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Vertex {vertex} is not a member of this elementary layer.");
            }

            return retVal;
        }

        public int InDegree(NodeTensor vertex)
        {
            List<int> resolved = ResolveCoordinates(vertex.aspectCoordinates);
            if (resolved == null)
                throw new ArgumentException($"Elementary layer referenced by vertex {vertex} does not exist.");

            int retVal = 0;

            ElementaryLayer layer = elementaryLayers[resolved];

            try
            {
                retVal = layer.InDegree(vertex.nodeId);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Vertex {vertex} is not a member of this elementary layer.");
            }

            return retVal;
        }

        public int OutDegree(NodeTensor vertex)
        {
            List<int> resolved = ResolveCoordinates(vertex.aspectCoordinates);
            if (resolved == null)
                throw new ArgumentException($"Elementary layer referenced by vertex {vertex} does not exist.");

            int retVal = 0;

            ElementaryLayer layer = elementaryLayers[resolved];

            try
            {
                retVal = layer.OutDegree(vertex.nodeId);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Vertex {vertex} is not a member of this elementary layer.");
            }

            return retVal;
        }

        public bool HasElementaryLayer(List<string> coords)
        {
            List<int> rcoords = ResolveCoordinates(coords);
            if (rcoords == null)
                return false;
            return ElementaryLayerExists(rcoords);
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

        /// <summary>
        /// Returns a dictionary of resolved vertices and the weights to reach them that are adjacent to the given vertex, to include node-coupled neighbors.
        /// In other words, the neighbors will include those vertices neighboring any vertex with the same id in other layers.
        /// </summary>
        /// <param name="vertex">Vertex qualified by aspect coordinates</param>
        /// <returns>Dictionary of layer-qualified vertices and the weight of the edge from the given vertex to the neighboring vertex.</returns>
        public Dictionary<NodeTensor, double> GetNeighbors(NodeTensor vertex)
        {
            if (!nodeIdsAndLayers.Keys.Contains(vertex.nodeId))
                throw new ArgumentException($"Vertex {vertex.nodeId} does not exist anywhere in the multilayer network.");

            List<int> resolvedCoords = ResolveCoordinates(vertex.aspectCoordinates);
            if (resolvedCoords == null || !ElementaryLayerExists(resolvedCoords))
                throw new ArgumentException($"Layer {string.Join(",", vertex.aspectCoordinates)} does not exist.");

            // Get the neighbors in the elementary layer and those neighbors explicitly linked by an interlayer edge
            Dictionary<NodeTensor, double> retVal = elementaryLayers[resolvedCoords].GetNeighbors(vertex.nodeId);

            // Add node-coupled neighbors
            foreach (ElementaryLayer layer in nodeIdsAndLayers[vertex.nodeId])
            {
                if (layer.ResolvedCoordinates.SequenceEqual(resolvedCoords))
                    continue;

                Dictionary<NodeTensor, double> nghrs = layer.GetNeighbors(vertex.nodeId);
                foreach (KeyValuePair<NodeTensor, double> kvp in nghrs)
                {
                    retVal.Add(kvp.Key, kvp.Value);
                }
            }

            return retVal;
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
            writer.WriteLine(@":Aspects");
            for (int i = 0; i < aspects.Count(); i++)
            {
                writer.Write(aspects[i] + ": ");
                writer.WriteLine(string.Join(",", axes[i]));
            }

            foreach (List<int> layerCoord in elementaryLayers.Keys)
            {
                writer.WriteLine(@"");
                List<string> aspectCoords = UnaliasCoordinates(layerCoord);
                writer.WriteLine(@":Layer (" + string.Join(",", aspectCoords) + @")");
                elementaryLayers[layerCoord].List(writer, delimiter);
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
            // same vertex
            if (from.nodeId == to.nodeId && from.aspectCoordinates.SequenceEqual(to.aspectCoordinates))
                throw new ArgumentException($"Self-edges are not permitted (vertex {from}, {to}");

            // same vertex, different layers -- layers are node-coupled
            if (from.nodeId == to.nodeId)
                throw new ArgumentException($"Categorical edges are implicit and have weight zero.");

            ResolvedNodeTensor rFrom = ResolveNodeTensor(from);
            ResolvedNodeTensor rTo = ResolveNodeTensor(to);

            // if the tensor cannot be resolved, one or both elementary layers does not exist
            if (rFrom == null || rTo == null || !ElementaryLayerExists(rFrom.coordinates) && !ElementaryLayerExists(rTo.coordinates))
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

        public Network GetLayer(string layerCoordinates)
        {
            List<string> coords = new List<string>(layerCoordinates.Split(','));
            List<int> resolved = ResolveCoordinates(coords);
            if (resolved == null || !ElementaryLayerExists(resolved))
                throw new ArgumentException($"No elementary layer exists at {layerCoordinates}.");

            return elementaryLayers[resolved].CopyGraph();
        }

        public Network GetLayer(List<string> layerCoordinates)
        {
            List<int> resolved = ResolveCoordinates(layerCoordinates);
            if (resolved == null || !ElementaryLayerExists(resolved))
                throw new ArgumentException($"No elementary layer exists at {string.Join(",", layerCoordinates)}.");

            return elementaryLayers[resolved].CopyGraph();
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
