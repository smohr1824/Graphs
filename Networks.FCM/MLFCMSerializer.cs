using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Networks.Core;

namespace Networks.FCM
{
    public class MLFCMSerializer
    {
        public static void WriteMultiLayerNetworkToFile(MultilayerFuzzyCognitiveMap net, string filename)
        {
            StreamWriter writer = new StreamWriter(filename);
            WriteMultilayerNetwork(net, writer);
            writer.Close();
        }

        public static void WriteMultilayerNetwork(MultilayerFuzzyCognitiveMap net, TextWriter writer)
        {
            net.ListGML(writer);
        }

        public static MultilayerFuzzyCognitiveMap ReadNetworkFromFile(string filename)
        {
            StreamReader reader = null;
            MultilayerFuzzyCognitiveMap retVal = null;
            try
            {
                reader = new StreamReader(filename);   // don't catch any exceptions, let the caller respond
                retVal = ReadNetwork(reader);
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

        public static MultilayerFuzzyCognitiveMap ReadNetwork(TextReader reader)
        {
            MultilayerFuzzyCognitiveMap fcm = null;

            GMLTokenizer.EatWhitespace(reader);
            string top = GMLTokenizer.ReadNextToken(reader);
            if (top == "multilayer_network")
            {
                GMLTokenizer.EatWhitespace(reader);
                string start = GMLTokenizer.ReadNextToken(reader);
                if (start == "[")
                {
                    GMLTokenizer.EatWhitespace(reader);
                    fcm = ProcessGraph(reader);
                }
            }
            return fcm;

        }

        private static MultilayerFuzzyCognitiveMap ProcessGraph(TextReader reader)
        {
            MultilayerFuzzyCognitiveMap graph = null;

            uint globalState = 1;
            bool unfinished = true;
            thresholdType type = thresholdType.BIVALENT;
            bool modified = false;

            while (unfinished && reader.Peek() != -1)
            {
                GMLTokenizer.EatWhitespace(reader);
                string token = GMLTokenizer.ReadNextToken(reader);
                switch (token.ToLower())
                {

                    case "directed":
                        if (globalState == 1)
                        {
                            GMLTokenizer.EatWhitespace(reader);
                            // directed is serialized so that the graph can be read as such even if the FCM data is not retained/understood
                            GMLTokenizer.ReadNextValue(reader);
                        }
                        else
                        {
                            throw new NetworkSerializationException(EntityType.property, $"Property {token} found out of order", null);
                        }
                        break;

                    case "threshold":
                        if (globalState == 1)
                        {
                            GMLTokenizer.EatWhitespace(reader);
                            string threshname = GMLTokenizer.ReadNextValue(reader);
                            switch (threshname.ToLower())
                            {
                                case "bivalent":
                                    type = thresholdType.BIVALENT;
                                    break;
                                case "trivalent":
                                    type = thresholdType.TRIVALENT;
                                    break;
                                case "logistic":
                                    type = thresholdType.LOGISTIC;
                                    break;
                                case "custom":
                                    type = thresholdType.CUSTOM;
                                    break;
                            }
                        }
                        else
                        {
                            throw new NetworkSerializationException(EntityType.property, $"Property {token} found out of order", null);
                        }
                        break;

                    case "rule":
                        if (globalState == 1)
                        {
                            GMLTokenizer.EatWhitespace(reader);
                            string rulename = GMLTokenizer.ReadNextValue(reader);
                            if (rulename.ToLower() == "modified")
                                modified = true;
                            else
                                modified = false;
                        }
                        else
                        {
                            throw new NetworkSerializationException(EntityType.property, $"Property {token} found out of order", null);
                        }
                        break;

                    case "aspects":
                        if (globalState == 1)
                        {
                            globalState = 2;
                            List<Tuple<string, IEnumerable<string>>> aspects = MultilayerNetworkGMLSerializer.ReadAspects(reader);
                            graph = new MultilayerFuzzyCognitiveMap(aspects, type, modified);
                        }
                        else
                        {
                            throw new MLNetworkSerializationException(MLEntityType.property, @"Aspects record found out of order", null);
                        }
                        break;

                    case "concept":
                        if (globalState >= 2 && globalState <= 3)
                        {
                            globalState = 3;
                            Dictionary<string, string> nodeDictionary = GMLTokenizer.ReadListRecord(reader);
                            if (nodeDictionary.Keys.Contains("id"))
                            {
                                MultilayerCognitiveConcept concept = ProcessConcept(nodeDictionary);
                                try
                                {
                                    uint id = ProcessNodeId(nodeDictionary["id"]);
                                    if (!graph.AddConcept(concept, id))
                                        throw new MLNetworkSerializationException(MLEntityType.node, $"Concept {concept.Name} or id {id.ToString()} already exists in the network and cannot be added", null);
                                }
                                catch (Exception ex)
                                {
                                    string idlabel = nodeDictionary["id"];
                                    throw new MLNetworkSerializationException(MLEntityType.node, $"Error converting concept id {idlabel}", null, ex);
                                }
                            }
                        }
                        else
                        {
                            throw new MLNetworkSerializationException(MLEntityType.node, @"Concept found out of order", null);
                        }
                        break;

                    case "]":
                        unfinished = false;
                        break;

                    case "layer":
                        if (globalState >= 3 && globalState <= 4)
                        {
                            globalState = 4;
                            ReadLayer(reader, ref graph);
                        }
                        else
                        {
                            throw new MLNetworkSerializationException(MLEntityType.layer, @"Layer record found out of order", null);
                        }
                        break;

                    case "edge":
                        if (globalState >= 4 && globalState <= 5)
                        {
                            globalState = 5;
                            ReadInterlayerEdge(reader, ref graph);
                        }
                        else
                        {
                            throw new MLNetworkSerializationException(MLEntityType.interlayerEdge, @"Interlayer edge record found out of order", null);
                        }
                        break;
                    default:
                        GMLTokenizer.ConsumeUnknownValue(reader);
                        break;
                }
            }
            foreach (MultilayerCognitiveConcept concept in graph.Concepts.Values)
            {
                graph.RecomputeAggregateActivationLevel(concept.Name);
            }
            return graph;
        }

        public static void ReadLayer(TextReader reader, ref MultilayerFuzzyCognitiveMap fcm)
        {
            string[] aspectCoords;
            string coords = string.Empty;

            if (GMLTokenizer.PositionStartOfRecordOrArray(reader) != -1)
            {
                string key = GMLTokenizer.ReadNextToken(reader);
                if (key == "coordinates")
                {
                    GMLTokenizer.EatWhitespace(reader);
                    coords = GMLTokenizer.ReadNextValue(reader);
                    aspectCoords = coords.Split(',');
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
                    fcm.AddElementaryLayer(aspectCoords, network);
                    GMLTokenizer.EatWhitespace(reader);
                    GMLTokenizer.ReadNextToken(reader); // end bracket
                }
                else
                {
                    throw new MLNetworkSerializationException(MLEntityType.layer, @"Missing coordinates on layer record", null);
                }
            }
            else
            {
                throw new MLNetworkSerializationException(MLEntityType.layer, $"Malformed elementary layer for coordinates {coords}", null);
            }

        }

        public static void ReadInterlayerEdge(TextReader reader, ref MultilayerFuzzyCognitiveMap fcm)
        {
            NodeLayerTuple src = null;
            NodeLayerTuple tgt = null;
            float edgeWt = 1.0F;
            StringReader sreader = null;

            Dictionary<string, string> edgeProps = GMLTokenizer.ReadListRecord(reader);
            if (edgeProps.Keys.Contains("source") && edgeProps.Keys.Contains("target"))
            {
                foreach (KeyValuePair<string, string> kvp in edgeProps)
                {
                    switch (kvp.Key.ToLower())
                    {
                        case "source":
                            sreader = new StringReader(kvp.Value);
                            src = MultilayerNetworkGMLSerializer.ProcessQualifiedNode(sreader);
                            break;

                        case "target":
                            sreader = new StringReader(kvp.Value);
                            tgt = MultilayerNetworkGMLSerializer.ProcessQualifiedNode(sreader);
                            break;

                        case "weight":
                            try
                            {
                                edgeWt = GMLTokenizer.ProcessFloatProp(kvp.Value);
                            }
                            catch (Exception ex)
                            {
                                throw new MLNetworkSerializationException(MLEntityType.interlayerEdge, @"Error converting weight for interlayer edge", null, ex);
                            }
                            break;

                    }
                }
                if (src != null & tgt != null)
                {
                    try
                    {
                        fcm.AddInfluence(src, tgt, edgeWt);
                    }
                    catch (Exception ex)
                    {
                        throw new MLNetworkSerializationException(MLEntityType.interlayerEdge, @"Error adding interlayer influence", null, ex);
                    }
                }
                else
                {
                    throw new MLNetworkSerializationException(MLEntityType.interlayerEdge, @"Missing source and/or target node for interlayer edge", null);
                }

            }
            else
            {
                throw new MLNetworkSerializationException(MLEntityType.interlayerEdge, @"Interlayer edge missing source or target", null);
            }
        }
        private static MultilayerCognitiveConcept ProcessConcept(Dictionary<string, string> props)
        {
            string name = string.Empty;
            float initial = 0.0F;
            float aggregate = 0.0F;
            Dictionary<string, string> layerLevels = new Dictionary<string, string>();
            MultilayerCognitiveConcept concept = null;

            if (props.Keys.Contains("label") && props.Keys.Contains("initial"))
            {
                foreach (KeyValuePair<string, string> kvp in props)
                {
                    switch (kvp.Key.ToLower())
                    {
                        case "id":
                            break;
                        case "label":
                            name = kvp.Value;
                            break;

                        case "initial":
                            try
                            {
                                initial = GMLTokenizer.ProcessFloatProp(kvp.Value);
                            }
                            catch (Exception ex)
                            {
                                throw new MLNetworkSerializationException(MLEntityType.node, @"Error converting concept initial value", null, ex);
                            }
                            break;

                        case "aggregate":
                            try
                            {
                                aggregate = GMLTokenizer.ProcessFloatProp(kvp.Value);
                            }
                            catch (Exception ex)
                            {
                                throw new MLNetworkSerializationException(MLEntityType.node, @"Error converting concept initial value", null, ex);
                            }
                            break;

                        case "levels":
                            TextReader nestedReader = new StringReader(kvp.Value);
                            layerLevels = GMLTokenizer.ReadListRecord(nestedReader);

                            break;
                    }
                }

                if (!props.Keys.Contains("aggregate"))
                    aggregate = initial;

                concept = new MultilayerCognitiveConcept(name, initial, aggregate);
                foreach (KeyValuePair<string, string> layerLevelPair in layerLevels)
                {
                    try
                    {
                        List<string> coords = new List<string>(layerLevelPair.Key.Split(','));
                        float levelValue = GMLTokenizer.ProcessFloatProp(layerLevelPair.Value);
                        concept.SetLayerLevel(coords, levelValue);
                    }
                    catch (Exception ex)
                    {
                        throw new MLNetworkSerializationException(MLEntityType.node, $"Error setting level activation value for concept {name} on level {layerLevelPair.Value}", null, ex);
                    }
                }
            }
            else
            {
                throw new MLNetworkSerializationException(MLEntityType.node, @"Incomplete concept found", null);
            }

            return concept;
        }

        private static uint ProcessNodeId(string sId)
        {
            uint id;
            try
            {
                id = Convert.ToUInt32(sId);
            }
            catch (FormatException fx)
            {
                throw new NetworkSerializationException(EntityType.node, $"Formatting error trying to convert id = {sId}", fx);
            }
            catch (OverflowException ovx)
            {
                throw new NetworkSerializationException(EntityType.node, $"Overflow error trying to convert id = {sId}", ovx);
            }
            return id;
        }
    }
}
