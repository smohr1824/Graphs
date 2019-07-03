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
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;
using Networks.Core;

namespace Networks.FCM
{
    public class FCMSerializer
    {

        public static void WriteNetworkToFile(FuzzyCognitiveMap net, string filename)
        {
            StreamWriter writer = new StreamWriter(filename);
            WriteNetwork(net, writer);
            writer.Close();
        }

        public static void WriteNetwork(FuzzyCognitiveMap net, TextWriter writer)
        {
            net.ListGML(writer);
        }

        public static FuzzyCognitiveMap ReadNetworkFromFile(string filename)
        {
            StreamReader reader = null;
            FuzzyCognitiveMap retVal = null;
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

        public static FuzzyCognitiveMap ReadNetwork(TextReader reader)
        {
            FuzzyCognitiveMap fcm = null;

            GMLTokenizer.EatWhitespace(reader);
            string top = GMLTokenizer.ReadNextToken(reader);
            if (top == "graph")
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

        private static FuzzyCognitiveMap ProcessGraph(TextReader reader)
        {
            uint globalState = 1;
            bool unfinished = true;
            thresholdType type = thresholdType.BIVALENT;
            FuzzyCognitiveMap graph = null;
            Dictionary<uint, string> conceptLookup = new Dictionary<uint, string>();
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

                    case "node":
                        if (globalState <= 2)
                        {
                            globalState = 2;
                            if (graph == null)
                            {
                                graph = new FuzzyCognitiveMap(type, modified);
                            }
                            Dictionary<string, string> nodeDictionary = GMLTokenizer.ReadFlatListProperty(reader);
                            ProcessConcept(nodeDictionary, ref graph, ref conceptLookup);
                        }
                        else
                        {
                            throw new NetworkSerializationException(EntityType.node, @"Node record found out of order", null);
                        }
                        break;

                    case "edge":
                        if (globalState > 1 && globalState <= 3)
                        {
                            globalState = 3;
                            Dictionary<string, string> edgeDictionary = GMLTokenizer.ReadFlatListProperty(reader);
                            ProcessEdge(edgeDictionary, ref graph, ref conceptLookup);
                        }
                        else
                        {
                            throw new NetworkSerializationException(EntityType.edge, @"Edgerecord found out of order", null);
                        }
                        break;

                    case "]":
                        unfinished = false;
                        break;

                    default:
                        GMLTokenizer.ConsumeUnknownValue(reader);
                        break;
                }
            }

            return graph;
        }

        private static void ProcessConcept(Dictionary<string, string> properties, ref FuzzyCognitiveMap net, ref Dictionary<uint, string> lookup)
        {
            // id, label, and initial are required. If activation does not appear, default to initial
            if (properties.Keys.Contains("id") && properties.Keys.Contains("label") && properties.Keys.Contains("initial"))
            {
                uint id;
                float initial, activation;

                id = ProcessNodeId(properties["id"]);
                try
                {
                    initial = GMLTokenizer.ProcessFloatProp(properties["initial"]);
                    if (properties.Keys.Contains("activation"))
                        activation = GMLTokenizer.ProcessFloatProp(properties["activation"]);
                    else
                        activation = initial;
                }
                catch (FormatException fx)
                {
                    throw new NetworkSerializationException(EntityType.node, $"Error converting property value for concept {properties["label"]}", fx);
                }
                catch (OverflowException ovx)
                {
                    throw new NetworkSerializationException(EntityType.node, $"Error converting property value for concept {properties["label"]}", ovx);
                }

                lookup.Add(id, properties["label"]);
                net.AddConcept(properties["label"], initial, activation);

            }
            else
            {
                string concept = string.Empty;
                if (properties.Keys.Contains("label"))
                    concept = properties["label"];

                throw new NetworkSerializationException(EntityType.node, $"Concept {concept} missing one or more required properties");
            }
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

        private static void ProcessEdge(Dictionary<string, string> properties, ref FuzzyCognitiveMap net, ref Dictionary<uint, string> lookup)
        {
            // source, target, and weight are required. 
            if (properties.Keys.Contains("source") && properties.Keys.Contains("target") && properties.Keys.Contains("weight"))
            {
                uint srcId, tgtId;
                float weight;

                srcId = ProcessNodeId(properties["source"]);
                tgtId = ProcessNodeId(properties["target"]);
                try
                {
                    weight = GMLTokenizer.ProcessFloatProp(properties["weight"]);
                }
                catch (FormatException fx)
                {
                    throw new NetworkSerializationException(EntityType.node, $"Error converting influence weight from concept, value is {properties["weight"]}", fx);
                }
                catch (OverflowException ovx)
                {
                    throw new NetworkSerializationException(EntityType.node, $"Error converting influence weight from concept, value is {properties["weight"]}", ovx);
                }

                net.AddInfluence(lookup[srcId], lookup[tgtId], weight);

            }
            else
            {
                string concept = string.Empty;
                if (properties.Keys.Contains("label"))
                    concept = properties["label"];

                throw new NetworkSerializationException(EntityType.node, $"Concept {concept} missing one or more required properties");
            }
        }
    }
}
