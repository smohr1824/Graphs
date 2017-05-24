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

        // Network-centric with interlayer edges
        private Dictionary<List<int>, ElementaryLayer> elementaryLayers;

        // concordance of vertex ids and the elementary layers in which they appear
        private Dictionary<string, List<List<int>>> nodeIdsAndLayers;
        private bool categoricalEdges;

        public MultilayerNetwork(IEnumerable<Tuple<string, IEnumerable<string>>> dimensions, bool isCategoricallyConnected = true)
        {
            aspects = new List<string>();
            axes = new List<List<string>>();
            nodeIdsAndLayers = new Dictionary<string, List<List<int>>>();
            elementaryLayers = new Dictionary<List<int>, ElementaryLayer>(new CoordinateTensorEqualityComparer());

            categoricalEdges = isCategoricallyConnected;

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

        public bool AddElementaryLayer(IEnumerable<string> coordinates, Network layer)
        {
            if (coordinates == null || layer == null)
                return false;

            List<int> resolved = ResolveCoordinates(coordinates.ToList<string>());
            if (resolved != null)
            {
                if (AddElementaryNetworkToMultiLayerNetwork(resolved, layer))
                    return true;
                else
                    return false;
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
                List<List<int>> layers;
                if (!nodeIdsAndLayers.TryGetValue(rVertex.nodeId, out layers))
                {
                    List<List<int>> coords = new List<List<int>>();
                    coords.Add(rVertex.coordinates);
                    nodeIdsAndLayers.Add(rVertex.nodeId, coords);
                }
                else
                {
                    // The vertex exists somewhere in the multilayer network.
                    // search to see if it is already in the concordance
                    bool found = false;
                    int i = 0;
                    while (i < layers.Count() && !layers[i].SequenceEqual(rVertex.coordinates))
                        i++;

                    if (!found)
                        layers.Add(rVertex.coordinates);
                }
            }

        }

        public void AddEdge(NodeTensor from, NodeTensor to, double wt, bool directed)
        {
            ResolvedNodeTensor rFrom = ResolveNodeTensor(from);
            ResolvedNodeTensor rTo = ResolveNodeTensor(to);

            // if the tensor cannot be resolved, one or both elementary layers does not exist
            if (rFrom == null || rTo == null || !ElementaryLayerExists(rFrom.coordinates) || !ElementaryLayerExists(rTo.coordinates))
                throw new ArgumentException($"The elementary layer for one or more vertices does not exist (vertices passed {from.ToString()}, {to.ToString()}");

            // ensure the vertices exist in their respective elementary layers; if not, create
            ElementaryLayer fromLayer = elementaryLayers[rFrom.coordinates];
            ElementaryLayer toLayer = elementaryLayers[rTo.coordinates];

            if (!fromLayer.HasVertex(from.nodeId) || !toLayer.HasVertex(to.nodeId))
                throw new ArgumentException($"Edge cannot be added unless both vertices exist (from: {from}, to: {to}).");

            elementaryLayers[rFrom.coordinates].AddEdge(rFrom, rTo, wt, directed);
            if (!rFrom.IsSameElementaryLayer(rTo))
                elementaryLayers[rTo.coordinates].IncrementEdgeFrom(rFrom.coordinates);

            if (!directed)
            {
                elementaryLayers[rTo.coordinates].AddEdge(rTo, rFrom, wt, true);
                if (!rFrom.IsSameElementaryLayer(rTo))
                    elementaryLayers[rFrom.coordinates].IncrementEdgeFrom(rTo.coordinates);
            }
        }


        public void RemoveEdge(NodeTensor from, NodeTensor to, bool directed)
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
                toLayer.DecrementEdgeFrom(rFrom.coordinates);
            }

            // if this is an undirected edge, remove the reciprocal edge
            if (!directed && toLayer.HasEdge(rTo, rFrom))
            {
                toLayer.RemoveEdge(rTo, rFrom, directed);
                fromLayer.DecrementEdgeFrom(rTo.coordinates);
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

            foreach (string vertex in inVertices)
            {
                if (nodeIdsAndLayers.ContainsKey(vertex))
                {
                    nodeIdsAndLayers[vertex].Add(coords);
                }
                else
                {
                    List<List<int>> layerCoords = new List<List<int>>();
                    layerCoords.Add(coords);
                    nodeIdsAndLayers.Add(vertex, layerCoords);
                }
            }
            try
            {
                elementaryLayers.Add(coords, new ElementaryLayer(this, G, coords));
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
