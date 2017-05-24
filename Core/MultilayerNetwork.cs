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
        private Dictionary<List<int>, Network> elementaryLayers;
        private Dictionary<ResolvedNodeTensor, Dictionary<ResolvedNodeTensor, double>> interLayerEdgeList;

        private Dictionary<string, List<List<int>>> nodeIdsAndLayers;
        private bool categoricalEdges;

        public MultilayerNetwork(IEnumerable<Tuple<string, IEnumerable<string>>> dimensions, bool isCategoricallyConnected = true)
        {
            aspects = new List<string>();
            axes = new List<List<string>>();
            nodeIdsAndLayers = new Dictionary<string, List<List<int>>>();
            elementaryLayers = new Dictionary<List<int>, Network>(new CoordinateTensorEqualityComparer());
            interLayerEdgeList = new Dictionary<ResolvedNodeTensor, Dictionary<ResolvedNodeTensor, double>>(new NodeTensorEqualityComparer());
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

            if (rFrom.IsSameElementaryLayer(rTo))
            {
                // get the elementary layer and check for the edge
                return elementaryLayers[rFrom.coordinates].HasEdge(rFrom.nodeId, rTo.nodeId);
            }
            else
            {
                if (interLayerEdgeList.Keys.Contains(rFrom))
                {
                    if (interLayerEdgeList[rFrom].Keys.Contains(rTo))
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
        }

        public double EdgeWeight(NodeTensor from, NodeTensor to)
        {
            ResolvedNodeTensor rFrom = ResolveNodeTensor(from);
            ResolvedNodeTensor rTo = ResolveNodeTensor(to);

            if (rFrom == null || rTo == null)
                return 0;

            if (rFrom.IsSameElementaryLayer(rTo))
            {
                // get the elementary layer and check for the edge
                return elementaryLayers[rFrom.coordinates].EdgeWeight(rFrom.nodeId, rTo.nodeId);
            }
            else
            {
                if (interLayerEdgeList.Keys.Contains(rFrom))
                {
                    if (interLayerEdgeList[rFrom].Keys.Contains(rTo))
                    {
                        double retVal;
                        interLayerEdgeList[rFrom].TryGetValue(rTo, out retVal);
                        return retVal;
                    }
                    else
                        return 0;
                }
                else
                    return 0;
            }
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

        /*public bool RemoveElementaryLayer(IEnumerable<string> coordinates)
        {
            List<int> resolvedCoords = ResolveCoordinates(coordinates.ToList<string>());

            
            if (resolvedCoords == null)
                return false;

            if (RemoveElementaryNetworkFromMultiLayerNetwork(resolvedCoords))
                return true;
            else
                return false;

        }

        private bool RemoveElementaryNetworkFromMultiLayerNetwork(List<int> resolved)
        {
            int foo;
            foreach (ResolvedNodeTensor key in interLayerEdgeList.Keys)
            {
                if (CoordinateTensorEqualityComparer.Equals(key.coordinates, resolved))
                    foo = 5;
                else
                    foo = 2;
            }


            return true;
        }*/

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

            foreach (ResolvedNodeTensor from in interLayerEdgeList.Keys)
            {
                Dictionary<ResolvedNodeTensor, double> targets = interLayerEdgeList[from];
                foreach (ResolvedNodeTensor to in targets.Keys)
                {
                    writer.WriteLine(from.nodeId + ":" + string.Join(",", UnaliasCoordinates(from.coordinates)) + delimiter + to.nodeId + ":" + string.Join(",", UnaliasCoordinates(to.coordinates)) + delimiter + targets[to].ToString());
                }
            }
        }

        public void AddEdge(NodeTensor from, NodeTensor to, double wt, bool directed)
        {
            ResolvedNodeTensor rFrom = ResolveNodeTensor(from);
            ResolvedNodeTensor rTo = ResolveNodeTensor(to);

            // if the tensor cannot be resolved, one or both elementary layers does not exist
            if (rFrom == null || rTo == null)
                throw new ArgumentException($"The elementary layer for one or more vertices does not exist (vertices passed {from.ToString()}, {to.ToString()}");

            if (!rFrom.IsSameElementaryLayer(rTo))
            {
                // interlayer edge

                // ensure the vertices exist in their respective elementary layers; if not, create
                Network fromNetwork = elementaryLayers[rFrom.coordinates];
                Network toNetwork = elementaryLayers[rTo.coordinates];

                if (!fromNetwork.HasVertex(from.nodeId))
                {
                    fromNetwork.AddVertex(from.nodeId);
                }
                if (!toNetwork.HasVertex(to.nodeId))
                {
                    toNetwork.AddVertex(to.nodeId);
                }

                if (interLayerEdgeList.ContainsKey(rFrom))
                {
                    if (interLayerEdgeList[rFrom].ContainsKey(rTo))
                    {
                        // contains the edge, update the weight
                        interLayerEdgeList[rFrom][rTo] = wt;
                    }
                    else
                    {
                        // contains from, add the weight
                        interLayerEdgeList[rFrom].Add(rTo, wt);
                    }
                }
                else
                {
                    // to and from are new, create the inner dictionary, create the inner dictionary and add the from vertex entry
                    Dictionary<ResolvedNodeTensor, double> rtDict = new Dictionary<ResolvedNodeTensor, double>(new NodeTensorEqualityComparer());
                    rtDict.Add(rTo, wt);
                    interLayerEdgeList.Add(rFrom, rtDict);
                }

                if (!directed)
                {
                    if (interLayerEdgeList.ContainsKey(rTo))
                    {
                        if (interLayerEdgeList[rTo].ContainsKey(rFrom))
                        {
                            interLayerEdgeList[rFrom][rTo] = wt;
                        }
                        else
                        {
                            interLayerEdgeList[rTo].Add(rFrom, wt);
                        }
                    }
                    else
                    {
                        Dictionary<ResolvedNodeTensor, double> rtDict = new Dictionary<ResolvedNodeTensor, double>(new NodeTensorEqualityComparer());
                        rtDict.Add(rFrom, wt);
                        interLayerEdgeList.Add(rTo, rtDict);
                    }
                }
            }
            else
            {
                // within the same elementary layer
                Network G = elementaryLayers[rFrom.coordinates];
                G.AddEdge(rFrom.nodeId, rTo.nodeId, wt, directed);
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
                    List<List<int>> tensors;
                    if (nodeIdsAndLayers.TryGetValue(vertex, out tensors))
                    {
                        if (!tensors.Contains(coords))
                            tensors.Add(coords);
                    }
                    else
                        return false;
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
                elementaryLayers.Add(coords, G);
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

        private void AddInterlayerEdge(ResolvedNodeTensor from, ResolvedNodeTensor to, double wt)
        {
            if (!ElementaryLayerExists(from.coordinates) || !ElementaryLayerExists(to.coordinates))
                return;

            if (interLayerEdgeList.ContainsKey(from))
            {
                if (interLayerEdgeList[from].ContainsKey(to))
                    interLayerEdgeList[from][to] = wt;
                else
                    interLayerEdgeList[from].Add(to, wt);
            }
            else
            {
                Dictionary<ResolvedNodeTensor, double> newWts = new Dictionary<ResolvedNodeTensor, double>(new NodeTensorEqualityComparer());
                newWts.Add(to, wt);
                interLayerEdgeList.Add(from, newWts);
            }
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

        private List<string> UnaliasCoordinates(List<int> coords)
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

        private bool ElementaryLayerExists(List<int> coords)
        {
            return elementaryLayers.Keys.Contains(coords);
        }
        #endregion
    }
}
