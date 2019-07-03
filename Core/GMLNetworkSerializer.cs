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

namespace Networks.Core
{
    public class GMLNetworkSerializer
    {

        public static void WriteNetworkToFile(Network net, string filename)
        {
            StreamWriter writer = new StreamWriter(filename);
            WriteNetwork(net, writer);
            writer.Close();
        }

        public static void WriteNetwork(Network net, TextWriter writer)
        {
            net.ListGML(writer);
        }

        public static Network ReadNetworkFromFile(string filename)
        {
            StreamReader reader = null;
            Network retVal = null;

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

        public static Network ReadNetwork(TextReader reader)
        {
            Network net = null;
            GMLTokenizer.EatWhitespace(reader);
            string top = GMLTokenizer.ReadNextToken(reader);
            if (top == "graph")
            {
                GMLTokenizer.EatWhitespace(reader);
                string start = GMLTokenizer.ReadNextToken(reader);
                if (start == "[")
                {
                    GMLTokenizer.EatWhitespace(reader);
                    net = ProcessGraph(reader);
                }
            }
            return net;
        }

        private static Network ProcessGraph(TextReader reader)
        {
            uint globalState = 1;
            Network net = null;
            bool unfinished = true;
            //while (reader.Peek() != -1)
            while (unfinished && reader.Peek() != -1)
            {
                GMLTokenizer.EatWhitespace(reader);
                string token = GMLTokenizer.ReadNextToken(reader);
                switch (token)
                {
                    case "directed":
                        GMLTokenizer.EatWhitespace(reader);
                        if (GMLTokenizer.ReadNextValue(reader) == "1")
                            net = new Network(true);
                        else
                            net = new Network(false);
                        break;

                    case "node":
                        if (globalState < 3)
                        {
                            globalState = 2;
                            Dictionary<string, string> nodeDictionary = GMLTokenizer.ReadFlatListProperty(reader);
                            if (nodeDictionary.Keys.Contains("id"))
                            {
                                uint id = ProcessNodeId(nodeDictionary["id"]);
                                net.AddVertex(id);
                            }
                            else
                                throw new NetworkSerializationException(EntityType.node, @"Missing node id", null);
                        }
                        else
                        {
                            throw new NetworkSerializationException(EntityType.node, @"node found out of place in file", null);
                        }
                        break;

                    case "edge":
                        if (globalState <= 3)
                        {
                            globalState = 3;
                            Dictionary<string, string> edgeProps = GMLTokenizer.ReadFlatListProperty(reader);
                            if (edgeProps.Keys.Contains("source") && edgeProps.Keys.Contains("target") && edgeProps.Keys.Contains("weight"))
                            {
                                uint srcId = ProcessNodeId(edgeProps["source"]);
                                uint tgtId = ProcessNodeId(edgeProps["target"]);
                                float wt = 0.0F;
                                try
                                {
                                    wt = GMLTokenizer.ProcessFloatProp(edgeProps["weight"]);
                                }
                                catch (Exception ex)
                                {
                                    throw new NetworkSerializationException(EntityType.edge, $"Error processing weight for edge from {srcId} to {tgtId}", ex);
                                }
                                net.AddEdge(srcId, tgtId, wt);
                            }
                            else
                            {
                                throw new NetworkSerializationException(EntityType.edge, @"Edge missing required properties", null);
                            }
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
             return net;
        }

        private static void ProcessNode(TextReader stIn, ref Network graph)
        {
            string[] fields;
            string line = string.Empty;
            uint ID = 0;

            while ((line = stIn.ReadLine()) != null)
            {
                fields = SplitAndClean(line);
                if (fields.Count() == 1 && fields[0] == "]")
                {
                    graph.AddVertex(ID);
                    return;
                }

                if (fields.Count() == 2)
                {
                    switch (fields[0].ToLower())
                    {
                        case "id":
                            ID = Convert.ToUInt32(fields[1]);
                            break;

                    }
                }
            }
        }

        private static void ProcessEdge(TextReader stIn, ref Network graph)
        {
            string[] fields;
            string line = string.Empty;
            uint src = 0;
            uint tgt = 0;
            float wt = 0.0F;

            while ((line = stIn.ReadLine()) != null)
            {
                fields = SplitAndClean(line);
                if (fields.Count() == 1 && fields[0] == "]")
                {
                    graph.AddEdge(src, tgt, wt);
                    return;
                }
                if (fields.Count() == 2)
                {
                    switch (fields[0].ToLower())
                    {
                        case "source":
                            src = Convert.ToUInt32(fields[1]);
                            break;
                        case "target":
                            tgt = Convert.ToUInt32(fields[1]);
                            break;
                        case "weight":
                            wt = Convert.ToSingle(fields[1]);
                            break;
                    }
                }
            }
        }
    

        // split a line on whitespace -- we expect a name, value pair delimited by whitespace
        private static string[] SplitAndClean(string incoming)
        {
            char[] leading = { '\t', ' ' };
            string line = incoming.Trim(leading);
            string[] fields = Regex.Split(line, @"[\s]+");
            int len = fields.Count();
            for (int i = 0; i < len; i++)
            {
                fields[i] = fields[i].Trim();
            }
            return fields;
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
