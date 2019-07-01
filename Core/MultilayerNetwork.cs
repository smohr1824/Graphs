//// MIT License

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
    public class MultilayerNetwork
    {
        // list of aspect names
        private List<string> aspects;

        // each inner list is a list of the indices along one aspect, hence the list of list contains all the indices in aspect order
        private List<List<string>> indices;

        private bool directed;

        // Network-centric with interlayer edges
        private Dictionary<List<int>, ElementaryLayer> elementaryLayers;

        // concordance of vertex ids and the elementary layers in which they appear
        private Dictionary<uint, List<ElementaryLayer>> nodeIdsAndLayers;

        public MultilayerNetwork(IEnumerable<Tuple<string, IEnumerable<string>>> dimensions, bool isdirected = true)
        {
            aspects = new List<string>();
            indices = new List<List<string>>();
            nodeIdsAndLayers = new Dictionary<uint, List<ElementaryLayer>>();
            elementaryLayers = new Dictionary<List<int>, ElementaryLayer>(new CoordinateTupleEqualityComparer());

            directed = isdirected;

            foreach (Tuple<string, IEnumerable<string>> axisValue in dimensions)
            {
                aspects.Add(axisValue.Item1);
                indices.Add(axisValue.Item2.ToList<string>());
            }

        }

        #region public properties

        public int Order
        {
            get { return nodeIdsAndLayers.Keys.Count();  }
        }

        public string[] Aspects()
        {
            return aspects.ToArray<string>();
        }

        public string[] Indices(string aspect)
        {
            int index = aspects.IndexOf(aspect);
            if (index == -1)
                throw new ArgumentException($"{aspect} is not one of the aspects of this network.");
            else
            {
                return indices[index].ToArray<string>();
            }

        }
        // lists all the vertices in the multilayer network once, i.e., does not worry about how often a vertex appears in elementary layers
        public List<uint> UniqueVertices()
        {
            return nodeIdsAndLayers.Keys.ToList();
        }

        // This holds iff. vertices are unique across the entire network and within any given elementary layer.  This is currently a constraint of the model.
        public bool IsNodeAligned()
        {
            int vertexCountGlobal = nodeIdsAndLayers.Keys.Count;
            foreach (ElementaryLayer layer in elementaryLayers.Values)
            {
                if (layer.Order() != vertexCountGlobal)
                    return false;
            }
            return true;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Finds the degree of a vertex in a specific elementary layer, to include interlayer edges
        /// </summary>
        /// <param name="vertex">Fully qualified node layer tuple denoting the vertex</param>
        /// <returns>Zero or positive integer count of incident edges</returns>
        /// <remarks>Throws ArgumentException if the referenced elementary layer does not exist or the vertex is not a member of that layer.</remarks>
        public int Degree(NodeLayerTuple vertex)
        {
            List<int> resolved = ResolveCoordinates(vertex.coordinates);
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

        public int InDegree(NodeLayerTuple vertex)
        {
            List<int> resolved = ResolveCoordinates(vertex.coordinates);
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

        public int OutDegree(NodeLayerTuple vertex)
        {
            List<int> resolved = ResolveCoordinates(vertex.coordinates);
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

        // Indicates whether a possible elementary layer is actually populated
        public bool HasElementaryLayer(List<string> coords)
        {
            List<int> rcoords = ResolveCoordinates(coords);
            if (rcoords == null)
                return false;
            return ElementaryLayerExists(rcoords);
        }

        // indicates whether a vertex appears in a particular elementary layer
        public bool HasVertex(NodeLayerTuple vertex)
        {
            ResolvedNodeLayerTuple rVertex = ResolveNodeLayerTuple(vertex);

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

        public bool HasEdge(NodeLayerTuple from, NodeLayerTuple to)
        {
            ResolvedNodeLayerTuple rFrom = ResolveNodeLayerTuple(from);
            ResolvedNodeLayerTuple rTo = ResolveNodeLayerTuple(to);

            if (rFrom == null || rTo == null)
                return false;

            return elementaryLayers[rFrom.coordinates].HasEdge(rFrom, rTo);
        }

        public float EdgeWeight(NodeLayerTuple from, NodeLayerTuple to)
        {
            ResolvedNodeLayerTuple rFrom = ResolveNodeLayerTuple(from);
            ResolvedNodeLayerTuple rTo = ResolveNodeLayerTuple(to);

            if (rFrom == null || rTo == null)
                return 0;

            if (ElementaryLayerExists(rFrom.coordinates))
                return elementaryLayers[rFrom.coordinates].EdgeWeight(rFrom, rTo);
            else
                return 0F;
        }

        public List<NodeLayerTuple> GetVertexInstances(uint id)
        {
            List<NodeLayerTuple> retVal = new List<NodeLayerTuple>();

            List<ElementaryLayer> layers;
            if (nodeIdsAndLayers.TryGetValue(id, out layers))
            {
                foreach (ElementaryLayer layer in layers)
                {
                    retVal.Add(new NodeLayerTuple(id, layer.AspectCoordinates));
                }
            }
            return retVal;
        }

        /// <summary>
        /// Returns a dictionary of vertices and the weights to reach them such that the vertices are adjacent to the given vertex, to include node-coupled neighbors.
        /// In other words, the neighbors will include those vertices neighboring any vertex with the same id in other layers.
        /// </summary>
        /// <param name="vertex">Vertex qualified by aspect coordinates</param>
        /// <returns>Dictionary of layer-qualified vertices and the weight of the edge from the given vertex to the neighboring vertex.</returns>
        public Dictionary<NodeLayerTuple, float> GetNeighbors(NodeLayerTuple vertex)
        {
            if (!nodeIdsAndLayers.Keys.Contains(vertex.nodeId))
                throw new ArgumentException($"Vertex {vertex.nodeId} does not exist anywhere in the multilayer network.");

            List<int> resolvedCoords = ResolveCoordinates(vertex.coordinates);
            if (resolvedCoords == null || !ElementaryLayerExists(resolvedCoords))
                throw new ArgumentException($"Layer {string.Join(",", vertex.coordinates)} does not exist.");

            // Get the neighbors in the elementary layer and those neighbors explicitly linked by an interlayer edge
            Dictionary<NodeLayerTuple, float> retVal = new Dictionary<NodeLayerTuple, float>(elementaryLayers[resolvedCoords].GetNeighbors(vertex.nodeId), new NodeLayerTupleEqualityComparer());

            // Add node-coupled neighbors (zero-length, bidirectional)
            // node coupling as implemented enables inter-aspect coupling
            foreach (ElementaryLayer layer in nodeIdsAndLayers[vertex.nodeId])
            {
                if (layer.ResolvedCoordinates.SequenceEqual(resolvedCoords))
                    continue;

                Dictionary<NodeLayerTuple, float> nghrs = layer.GetNeighbors(vertex.nodeId);
                foreach (KeyValuePair<NodeLayerTuple, float> kvp in nghrs)
                {
                    // an explicit interlayer edge may duplicate a node-coupled edge;
                    // if so, it replaces it
                    if (!retVal.ContainsKey(kvp.Key))
                        retVal.Add(kvp.Key, kvp.Value);
                    else
                    {
                        retVal.Remove(kvp.Key);
                        retVal.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            return retVal;
        }

        public Dictionary<NodeLayerTuple, float> GetSources(NodeLayerTuple vertex, bool coupled)
        {
            if (!nodeIdsAndLayers.Keys.Contains(vertex.nodeId))
                throw new ArgumentException($"Vertex {vertex.nodeId} does not exist anywhere in the multilayer network.");

            List<int> resolvedCoords = ResolveCoordinates(vertex.coordinates);
            if (resolvedCoords == null || !ElementaryLayerExists(resolvedCoords))
                throw new ArgumentException($"Layer {string.Join(",", vertex.coordinates)} does not exist.");

            // Get the neighbors in the elementary layer and those neighbors explicitly linked by an interlayer edge
            Dictionary<NodeLayerTuple, float> retVal = new Dictionary<NodeLayerTuple, float>(elementaryLayers[resolvedCoords].GetSources(vertex.nodeId), new NodeLayerTupleEqualityComparer());

            if (coupled)
            {
                // Add node-coupled neighbors (zero-length, bidirectional)
                // node coupling as implemented enables inter-aspect coupling
                foreach (ElementaryLayer layer in nodeIdsAndLayers[vertex.nodeId])
                {
                    if (layer.ResolvedCoordinates.SequenceEqual(resolvedCoords))
                        continue;

                    Dictionary<NodeLayerTuple, float> nghrs = layer.GetSources(vertex.nodeId);
                    foreach (KeyValuePair<NodeLayerTuple, float> kvp in nghrs)
                    {
                        // an explicit interlayer edge may duplicate a node-coupled edge;
                        // if so, it replaces it
                        if (!retVal.ContainsKey(kvp.Key))
                            retVal.Add(kvp.Key, kvp.Value);
                        else
                        {
                            retVal.Remove(kvp.Key);
                            retVal.Add(kvp.Key, kvp.Value);
                        }
                    }
                }
            }

            return retVal;
        }

        // TODO: Can GetNeighbors and CategoricalGetNeighbors be refactored so that the latter is a constrained version of the former and both can be implemented by a common private method?  Simplicity
        /// <summary>
        /// Returns a dictionary of vertices and weights such that the vertices are adjacent to the given vertex within the specified aspect.
        /// Additionally, if ordinal is true, the any vertex returned must be in an elementary layer immediately adjacent to the elementary layer of the given vertex.
        /// Ordinal = true allows for ordinal coupling assuming the indices of an aspect are in ordinal order.
        /// </summary>
        /// <param name="vertex">Vertex qualified by aspect coordinates</param>
        /// <param name="aspectCategory">Name of the aspect to which to limit node coupling</param>
        /// <param name="ordinal">True if candidate vertices must be in a layer adjacent to the layer of the specified vertex.</param>
        /// <returns>Dictionary of layer-qualified vertices and the edge weight between them and the originating vertex.</returns>
        public Dictionary<NodeLayerTuple, float> CategoricalGetNeighbors(NodeLayerTuple vertex, string aspectCategory, bool ordinal = false)
        {
            if (!nodeIdsAndLayers.Keys.Contains(vertex.nodeId))
                throw new ArgumentException($"Vertex {vertex.nodeId} does not exist anywhere in the multilayer network.");

            List<int> resolvedCoords = ResolveCoordinates(vertex.coordinates);
            if (resolvedCoords == null || !ElementaryLayerExists(resolvedCoords))
                throw new ArgumentException($"Layer {string.Join(",", vertex.coordinates)} does not exist.");

            if (!aspects.Contains(aspectCategory))
                throw new ArgumentException($"Aspect {aspectCategory} cannot be used as a category as it is not an aspect in the network.");

            // Get the neighbors in the elementary layer and those neighbors explicitly linked by an interlayer edge
            Dictionary<NodeLayerTuple, float> retVal = new Dictionary<NodeLayerTuple, float>(elementaryLayers[resolvedCoords].GetNeighbors(vertex.nodeId), new NodeLayerTupleEqualityComparer());

            int indexOfAspect = aspects.IndexOf(aspectCategory);
            int epsilon = 1;

            if (!ordinal)
                epsilon = indices[indexOfAspect].Count();

            // Get node-coupled neighbors given the constrainsts on node coupling
            foreach (ElementaryLayer layer in nodeIdsAndLayers[vertex.nodeId])
            {
                if (layer.ResolvedCoordinates.SequenceEqual(resolvedCoords))
                    continue;

                bool outOfAspect = false;

                // determine if we are out of the aspect
                for (int k = 0; k < layer.ResolvedCoordinates.Count(); k++)
                {
                    if (k != indexOfAspect)
                        continue;

                    if (layer.ResolvedCoordinates[k] != resolvedCoords[k])
                    {
                        outOfAspect = true;
                        break;
                    }
                }

                if (outOfAspect)
                    continue;

                // any layer that survives to this point is in aspect; see if it is ordinal if required
                if (Math.Abs(resolvedCoords[indexOfAspect] - layer.ResolvedCoordinates[indexOfAspect]) > epsilon)
                    continue;

                Dictionary<NodeLayerTuple, float> nghrs = layer.GetNeighbors(vertex.nodeId);
                foreach (KeyValuePair<NodeLayerTuple, float> kvp in nghrs)
                {
                    // an explicit interlayer edge may duplicate a node-coupled edge;
                    // if so, it replaces it
                    if (!retVal.ContainsKey(kvp.Key))
                        retVal.Add(kvp.Key, kvp.Value);
                    else
                    {
                        retVal.Remove(kvp.Key);
                        retVal.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            return retVal;
        }

        public Dictionary<NodeLayerTuple, float> CategoricalGetSources(NodeLayerTuple vertex, string aspectCategory, bool ordinal = false)
        {
            if (!nodeIdsAndLayers.Keys.Contains(vertex.nodeId))
                throw new ArgumentException($"Vertex {vertex.nodeId} does not exist anywhere in the multilayer network.");

            List<int> resolvedCoords = ResolveCoordinates(vertex.coordinates);
            if (resolvedCoords == null || !ElementaryLayerExists(resolvedCoords))
                throw new ArgumentException($"Layer {string.Join(",", vertex.coordinates)} does not exist.");

            if (!aspects.Contains(aspectCategory))
                throw new ArgumentException($"Aspect {aspectCategory} cannot be used as a category as it is not an aspect in the network.");

            // Get the neighbors in the elementary layer and those neighbors explicitly linked by an interlayer edge
            Dictionary<NodeLayerTuple, float> retVal = new Dictionary<NodeLayerTuple, float>(elementaryLayers[resolvedCoords].GetSources(vertex.nodeId), new NodeLayerTupleEqualityComparer());

            int indexOfAspect = aspects.IndexOf(aspectCategory);
            int epsilon = 1;

            if (!ordinal)
                epsilon = indices[indexOfAspect].Count();

            // Get node-coupled neighbors given the constraints on node coupling
            foreach (ElementaryLayer layer in nodeIdsAndLayers[vertex.nodeId])
            {
                if (layer.ResolvedCoordinates.SequenceEqual(resolvedCoords))
                    continue;

                bool outOfAspect = false;

                // determine if we are out of the aspect
                for (int k = 0; k < layer.ResolvedCoordinates.Count(); k++)
                {
                    if (k != indexOfAspect)
                        continue;

                    if (layer.ResolvedCoordinates[k] != resolvedCoords[k])
                    {
                        outOfAspect = true;
                        break;
                    }
                }

                if (outOfAspect)
                    continue;

                // any layer that survives to this point is in aspect; see if it is ordinal if required
                if (Math.Abs(resolvedCoords[indexOfAspect] - layer.ResolvedCoordinates[indexOfAspect]) > epsilon)
                    continue;

                Dictionary<NodeLayerTuple, float> nghrs = layer.GetSources(vertex.nodeId);
                foreach (KeyValuePair<NodeLayerTuple, float> kvp in nghrs)
                {
                    // an explicit interlayer edge may duplicate a node-coupled edge;
                    // if so, it replaces it
                    if (!retVal.ContainsKey(kvp.Key))
                        retVal.Add(kvp.Key, kvp.Value);
                    else
                    {
                        retVal.Remove(kvp.Key);
                        retVal.Add(kvp.Key, kvp.Value);
                    }
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
                writer.WriteLine(string.Join(",", indices[i]));
            }

            foreach (List<int> layerCoord in elementaryLayers.Keys)
            {
                writer.WriteLine(@"");
                List<string> aspectCoords = UnaliasCoordinates(layerCoord);
                writer.WriteLine(@":Layer (" + string.Join(",", aspectCoords) + @")");
                elementaryLayers[layerCoord].List(writer, delimiter);
            }


        }

        public void ListGML(TextWriter writer)
        {
            writer.WriteLine(@"multilayer_network [");
            if (directed)
                writer.WriteLine("\tdirected 1");
            else
                writer.WriteLine("\tdirected 0");

            writer.WriteLine("\taspects [");

            for (int i = 0; i < aspects.Count(); i++)
            {
                writer.Write("\t\t" + aspects[i] + " ");
                writer.WriteLine(string.Join(",", indices[i]));
            }
            writer.WriteLine("\t]");

            // Serialize the coordinates of the elementary layer and its constituent graph, but defer writing the interlayer edges
            foreach (List<int> layerCoord in elementaryLayers.Keys)
            {
                writer.WriteLine("\tlayer [");
                List<string> aspectCoords = UnaliasCoordinates(layerCoord);
                writer.WriteLine("\t\tcoordinates " + string.Join(",", aspectCoords));
                elementaryLayers[layerCoord].ListLayerGML(writer, 2);
                writer.WriteLine("\t]");
            }

            // Now that all the layers are written, do the interlayer edges -- this is similar to the node-first, then edge philosophy of GML
            // This requires two passes of the elementary layers as well as unaliasing the coords twice, but the cardinality of elementary layers should be relatively low
            foreach (List<int> layerCoord in elementaryLayers.Keys)
            {
                elementaryLayers[layerCoord].ListInterlayerGML(writer);
            }
            writer.WriteLine(@"]");

        }

        public void ListAllLayersGML(TextWriter writer, int level)
        {
            foreach (KeyValuePair<List<int>, ElementaryLayer> kvp in elementaryLayers)
            {
                writer.WriteLine("\tlayer [");
                List<string> aspectCoords = UnaliasCoordinates(kvp.Key);
                writer.WriteLine("\t\tcoordinates " + string.Join(",", aspectCoords));
                kvp.Value.ListLayerGML(writer, level);
                writer.WriteLine("\t]");
            }
        }

        public void ListAllInterlayerEdges(TextWriter writer)
        {
            foreach (List<int> layerCoord in elementaryLayers.Keys)
            {
                elementaryLayers[layerCoord].ListInterlayerGML(writer);
            }
        }

        public void AddVertex(NodeLayerTuple vertex)
        {
            ResolvedNodeLayerTuple rVertex = ResolveNodeLayerTuple(vertex);
            if (rVertex == null)
                throw new ArgumentException($"Aspect coordinates could not be resolved for {vertex}.");

            if (!ElementaryLayerExists(rVertex.coordinates))
                throw new ArgumentException($"Elementary layer does not exist at {vertex.coordinates}");

            // elementary layer exists
            ElementaryLayer layer = elementaryLayers[rVertex.coordinates];
            if (!layer.HasVertex(rVertex.nodeId))
            {
                elementaryLayers[rVertex.coordinates].AddVertex(rVertex.nodeId);
                List<ElementaryLayer> layers;

                // update the concordance of unique vertex ids and the elementary layers in which they appear
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

        public void RemoveVertex(NodeLayerTuple vertex)
        {
            ResolvedNodeLayerTuple rVertex = ResolveNodeLayerTuple(vertex);
            if (rVertex == null)
                throw new ArgumentException($"Aspect coordinates could not be resolved for {vertex}.");

            if (!ElementaryLayerExists(rVertex.coordinates))
                throw new ArgumentException($"Elementary layer does not exist at {vertex.coordinates}");

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

        public void AddEdge(NodeLayerTuple from, NodeLayerTuple to, float wt)
        {
            // same vertex
            if (from.nodeId == to.nodeId && from.coordinates.SequenceEqual(to.coordinates))
                throw new ArgumentException($"Self-edges are not permitted (vertex {from}, {to}");

            // same vertex, different layers -- layers are node-coupled
            if (from.nodeId == to.nodeId)
                throw new ArgumentException($"Categorical edges are implicit and have weight zero.");

            ResolvedNodeLayerTuple rFrom = ResolveNodeLayerTuple(from);
            ResolvedNodeLayerTuple rTo = ResolveNodeLayerTuple(to);

            // if the tuple cannot be resolved, one or both elementary layers does not exist
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
            else
            {

                if (!fromLayer.HasVertex(from.nodeId) || !toLayer.HasVertex(to.nodeId))
                    throw new ArgumentException($"Edge cannot be added unless both vertices exist (from: {from}, to: {to}).");

                elementaryLayers[rFrom.coordinates].AddEdge(rFrom, rTo, wt);
                elementaryLayers[rTo.coordinates].AddInEdge(rTo, rFrom, wt);
            }

        }


        public void RemoveEdge(NodeLayerTuple from, NodeLayerTuple to)
        {
            ResolvedNodeLayerTuple rFrom = ResolveNodeLayerTuple(from);
            ResolvedNodeLayerTuple rTo = ResolveNodeLayerTuple(to);

            // if the tuple cannot be resolved, one or both elementary layers does not exist
            if (rFrom == null || rTo == null || !ElementaryLayerExists(rFrom.coordinates) || !ElementaryLayerExists(rTo.coordinates))
                throw new ArgumentException($"The elementary layer for one or more vertices does not exist (vertices passed {from.ToString()}, {to.ToString()}");

            ElementaryLayer fromLayer = elementaryLayers[rFrom.coordinates];
            ElementaryLayer toLayer = elementaryLayers[rTo.coordinates];
            if (fromLayer.HasEdge(rFrom, rTo))
            {
                fromLayer.RemoveEdge(rFrom, rTo, directed);
                toLayer.RemoveInEdge(rTo, rFrom);
            }
        }

        public float[,] MakeSupraAdjacencyMatrix()
        {
            if (!IsNodeAligned())
                return null;

            int dimension = GetDimension();
            float[,] retVal = new float[dimension, dimension];
            List<List<string>> layerList = new List<List<string>>();
            string aspect = aspects[0];
            List<string> coords = new List<string>();
            foreach (string mark in indices[0])
            {
                coords.Add(mark);
                RecurseAdjacencyMatrix(ref layerList, aspect, 0, 0, coords);
                coords.RemoveAt(0);
            }

            // now we have a list of all elementary layer coordinates (row/column indices); build the supraadjacency matrix
            int blockCt = layerList.Count();
            for (int row = 0; row < blockCt; row++)
            {
                for (int column = 0; column < blockCt; column++)
                {
                    List<string> rowCoord = layerList[row];
                    List<string> colCoord = layerList[column];
                    List<int> rowResolved = ResolveCoordinates(rowCoord);
                    List<int> colResolved = ResolveCoordinates(colCoord);

                    ElementaryLayer layer = elementaryLayers[rowResolved];
                    if (row == column)
                    {
                        // insert the elementary layer adjacency matrix in the appropriate location
                        InsertLayerAdjacencies(ref retVal, layer.LayerAdjacencyMatrix, row, column);
                    }
                    else
                    {
                        // insert the interlayer edges from row to column
                        InsertLayerAdjacencies(ref retVal, layer.InterlayerAdjacencies(colResolved), row, column);
                    }
                }
            }
            return retVal;
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
        private int GetDimension()
        {
            int elemSize = nodeIdsAndLayers.Keys.Count();
            int ct = 1;
            foreach (string aspect in aspects)
                ct *= indices[aspects.IndexOf(aspect)].Count();

            return ct * elemSize;
        }

        private void RecurseAdjacencyMatrix(ref List<List<string>> allCoords, string aspect, uint blockRow, uint blockColumn, List<string> layerCoords)
        {
            int index = aspects.IndexOf(aspect) + 1;
            if (index == aspects.Count() - 1)
            {
                // innermost aspect
                foreach (string stop in indices[index])
                {
                    List<string> elemlayerCoords = new List<string>(layerCoords);
                    elemlayerCoords.Add(stop);
                    allCoords.Add(elemlayerCoords);
                }
            }
            else
            {
                string curAspect = aspects.ElementAt<string>(index);
                foreach (string stop in indices[index])
                {
                    layerCoords.Add(stop);
                    RecurseAdjacencyMatrix(ref allCoords, curAspect, blockRow, blockColumn, layerCoords);
                    layerCoords.RemoveAt(layerCoords.Count - 1);
                }
            }
        }

        private void InsertLayerAdjacencies(ref float[,] supra, float[,] layerMatrix, int rowBlock, int colBlock)
        {
            int size = layerMatrix.GetLength(0);
            int rowOffset = size * rowBlock;
            int colOffset = size * colBlock;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    supra[rowOffset + i, colOffset + j] = layerMatrix[i, j];
                }
            }
        }
        internal void RemoveOutEdge(ResolvedNodeLayerTuple from, ResolvedNodeLayerTuple to)
        {
            if (ElementaryLayerExists(from.coordinates))
            {
                elementaryLayers[from.coordinates].RemoveOutEdge(from, to);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="G"></param>
        /// <remarks>If a vertex id in G is found in the list of existing vertex ids, it is assumed that this is a node relating to an existing actor. Otherwise, vertices are added 
        /// to the list.
        /// </remarks>
        private bool AddElementaryNetworkToMultiLayerNetwork(List<int> coords, Network G)
        {
            List<uint> inVertices = G.Vertices;
            ElementaryLayer layer = new ElementaryLayer(this, G, coords);

            foreach (uint vertex in inVertices)
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
                List<uint> vertices = layer.Vertices;

                foreach (uint vertex in vertices)
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

        private ResolvedNodeLayerTuple ResolveNodeLayerTuple(NodeLayerTuple tuple)
        {
            ResolvedNodeLayerTuple retVal = new ResolvedNodeLayerTuple();
            retVal.nodeId = tuple.nodeId;

            retVal.coordinates = ResolveCoordinates(tuple.coordinates);
            if (retVal.coordinates == null)
                retVal = null;

            return retVal;
        }

        private List<int> ResolveCoordinates(List<string> coords)
        {
            List<int> retVal = new List<int>();

            for (int i = 0; i < aspects.Count(); i++)
            {
                int index = indices[i].IndexOf(coords[i]);
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

        private string IndentForLevel(int level)
        {
            string retVal = "";
            for (int i = 0; i < level; i++)
            {
                retVal += "\t";
            }
            return retVal;
        }

        internal List<string> UnaliasCoordinates(List<int> coords)
        {
            List<string> retVal = new List<string>();

            for (int i = 0; i < aspects.Count(); i++)
            {
                if (coords[i] > indices[i].Count() - 1)
                    throw new ArgumentOutOfRangeException($"Coordinate value exceeds maximum defined on aspect {indices[i]}");

                string sCoord = indices[i][coords[i]]; // simply debugging
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
