using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{
    public class MultilayerNetworkGMLSerializer
    {
        
        public static void WriteMultiLayerNetworkToFile(MultilayerNetwork net, string filename)
        {
            StreamWriter writer = new StreamWriter(filename);
            WriteMultiLayerNetwork(net, writer);
            writer.Close();
        }

        public static void WriteMultiLayerNetwork(MultilayerNetwork G, TextWriter writer)
        {
            G.ListGML(writer);
        }

        public static MultilayerNetwork ReadNetworkFromFile(string filename)
        {
            StreamReader reader = null;
            MultilayerNetwork retVal = null;
            try
            {
                reader = new StreamReader(filename);   // don't catch any exceptions, let the caller respond
                retVal = ReadMultilayerNetwork(reader);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                reader.Close();
                
            }
            return retVal;
        }

        public static MultilayerNetwork ReadMultilayerNetwork(TextReader reader)
        {
            MultilayerNetwork Q = null;
            GMLTokenizer.EatWhitespace(reader);
            string top = GMLTokenizer.ReadNextToken(reader);
            if (top == "multilayer_network")
            {
                GMLTokenizer.EatWhitespace(reader);
                string start = GMLTokenizer.ReadNextToken(reader);
                if (start == "[")
                {
                    GMLTokenizer.EatWhitespace(reader);
                    Q = ProcessMLNetwork(reader);
                }
            }
            return Q;
        }

        #region private
        private static MultilayerNetwork ProcessMLNetwork(TextReader reader)
        {
            uint globalState = 1;
            bool directed = true;
            MultilayerNetwork Q = null;
            while (reader.Peek() != -1)
            {
                GMLTokenizer.EatWhitespace(reader);
                string token = GMLTokenizer.ReadNextToken(reader);
                switch (token)
                {
                    case "directed":
                        GMLTokenizer.EatWhitespace(reader);
                        if (GMLTokenizer.ReadNextValue(reader) == "1")
                            directed = true;
                        else
                            directed = false;
                        break;

                    case "aspects":
                        if (globalState == 1)
                        {
                            Dictionary<string, string> aspectDictionary = GMLTokenizer.ReadFlatListProperty(reader);
                            List<Tuple<string, IEnumerable<string>>> aspects = new List<Tuple<string, IEnumerable<string>>>();

                            foreach (KeyValuePair<string, string> aspect in aspectDictionary)
                            {
                                List<string> dimensions = new List<string>();
                                string[] indices = aspect.Value.Split(',');
                                foreach (string index in indices)
                                    dimensions.Add(index);
                                aspects.Add(new Tuple<string, IEnumerable<string>>(aspect.Key, dimensions));
                            }
                            if (aspects.Count == 0)
                            {
                                throw new MLNetworkSerializationException(MLEntityType.property, @"No aspects read found", null);
                            }
                            Q = new MultilayerNetwork(aspects, directed);
                        }
                        else
                        {
                            throw new MLNetworkSerializationException(MLEntityType.property, @"aspects record found out of place in file", null);
                        }
                        break;

                    case "layer":
                        if (globalState < 3)
                        {
                            globalState = 2;
                            if (GMLTokenizer.PositionStartOfRecordOrArray(reader) != -1)
                            {
                                string key = GMLTokenizer.ReadNextToken(reader);
                                if (key == "coordinates")
                                {
                                    GMLTokenizer.EatWhitespace(reader);
                                    string coords = GMLTokenizer.ReadNextValue(reader);
                                    string[] aspectCoords = coords.Split(',');
                                    GMLTokenizer.EatWhitespace(reader);
                                    Network network = null;
                                    try
                                    {
                                        network = GMLNetworkSerializer.ReadNetwork(reader);
                                    }
                                    catch (NetworkSerializationException nsx)
                                    {
                                        throw new MLNetworkSerializationException(MLEntityType.layer, $"Error deserializing network for elementary layer {coords}", nsx);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new MLNetworkSerializationException(MLEntityType.layer, $"Unknown error deserializing network for elementary layer {coords}", null, ex);
                                    }
                                    Q.AddElementaryLayer(aspectCoords, network);
                                }
                                else
                                {
                                    throw new MLNetworkSerializationException(MLEntityType.layer, @"Malformed layer record, elementary coordinates not found", null);
                                }
                            }
                        }
                        else
                        {
                            throw new MLNetworkSerializationException(MLEntityType.layer, @"layer record found out of place in file", null);
                        }
                        break;

                    case "edge":
                        // expect records for source, target, simple property for weight
                        NodeLayerTuple src = null;
                        NodeLayerTuple tgt = null;
                        float edgeWt = 1.0F;
                        GMLTokenizer.EatWhitespace(reader);
                        string open = GMLTokenizer.ReadNextToken(reader);
                        if (open == "[")
                        {
                            bool unfinished = true;

                            while (unfinished)
                            {
                                GMLTokenizer.EatWhitespace(reader);
                                string propName = GMLTokenizer.ReadNextToken(reader);

                                switch (propName.ToLower())
                                {
                                    case "source":
                                        if (src == null)
                                        {
                                            src = ProcessQualifiedNode(reader);
                                        }
                                        else
                                        {
                                            throw new MLNetworkSerializationException(MLEntityType.interlayerEdge, "Duplicate source node specified while reading interlayer edge", null);
                                        }
                                        break;

                                    case "target":
                                        if (tgt == null)
                                        {
                                            tgt = ProcessQualifiedNode(reader);
                                        }
                                        else
                                            throw new MLNetworkSerializationException(MLEntityType.interlayerEdge, "Duplicate target node specified while reading interlayer edges", null);
                                        break;

                                    case "weight":
                                        GMLTokenizer.EatWhitespace(reader);
                                        string sWt = GMLTokenizer.ReadNextValue(reader);
                                        try
                                        {
                                            edgeWt = Convert.ToSingle(sWt);
                                        }
                                        catch (FormatException)
                                        {
                                            ThrowInterlayerEdgeError("weight", sWt, "(formatting error)");
                                        }
                                        catch (OverflowException)
                                        {
                                            ThrowInterlayerEdgeError("weight", sWt, "(overflow error)");
                                        }
                                        break;

                                    case "]":
                                        unfinished = false;
                                        break;

                                    // safety check for missing end bracket
                                    case "edge":
                                        throw new MLNetworkSerializationException(MLEntityType.interlayerEdge, @"Missing end bracket", null);
                                        //break;
                                }
                            }
                            if (src != null & tgt != null)
                            {
                                Q.AddEdge(src, tgt, edgeWt);
                            }
                            else
                            {
                                throw new MLNetworkSerializationException(MLEntityType.interlayerEdge, @"Missing source and/or target node for interlayer edge", null);
                            }
                        }

                        
                        break;
                }
            }

            return Q;
        }

        private static NodeLayerTuple ProcessQualifiedNode(TextReader reader)
        {
            NodeLayerTuple nodeTuple = null;

            Dictionary<string, string> nodeProps = GMLTokenizer.ReadFlatListProperty(reader);
            if (!nodeProps.Keys.Contains<string>("id"))
                ThrowInterlayerEdgeError("id", "<missing>");
            if (!nodeProps.Keys.Contains<string>("coordinates"))
                ThrowInterlayerEdgeError("coordinates", "<missing>");

            uint id = 0;
            try
            {
                id = Convert.ToUInt32(nodeProps["id"]);
            }
            catch (FormatException)
            {
                ThrowInterlayerEdgeError("id", nodeProps["id"], "(formatting error)");
            }
            catch (OverflowException)
            {
                ThrowInterlayerEdgeError("id", nodeProps["id"], "(overflow error)");
            }

            nodeTuple = new NodeLayerTuple(id, nodeProps["coordinates"]);

            return nodeTuple;
        }

        private static void ThrowInterlayerEdgeError(string keyword, string id, string additional = "")
        {
            throw new MLNetworkSerializationException(MLEntityType.interlayerEdge, $"Error formatting or converting interlayer edge {keyword} = {id} {additional}", null);
        }

        #endregion
    }
}
