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

            StreamReader reader = new StreamReader(filename);   // don't catch any exceptions, let the caller respond
            FuzzyCognitiveMap retVal = ReadNetwork(reader);
            reader.Close();
            return retVal;
        }

        public static FuzzyCognitiveMap ReadNetwork(TextReader stIn)
        {
            FuzzyCognitiveMap fcm = null;
            string[] fields;
            string line = string.Empty;
            while ((line = stIn.ReadLine()) != null)
            {
                fields = SplitAndClean(line);
                if (fields.Count() == 2)
                {
                    switch (fields[0])
                    {
                        case "#":
                            break;
                        case "graph":
                            fcm = new FuzzyCognitiveMap();
                            ProcessGraph(stIn, ref fcm);
                            continue;
                        //break;
                        // anything other than graph or a comment is invalid, so return
                        default:
                            continue;
                            //break;
                    }
                }
                else
                    return fcm;
            }
            return fcm;
        }

        private static void ProcessGraph(TextReader stIn, ref FuzzyCognitiveMap graph)
        {
            string[] fields;
            string line = string.Empty;
            bool modified = false;
            thresholdType type = thresholdType.BIVALENT;
            Dictionary<uint, string> conceptLookup = new Dictionary<uint, string>();

            while ((line = stIn.ReadLine()) != null)
            {
                fields = SplitAndClean(line);
                if (fields.Count() == 1 && fields[0] == "]")
                    return;
                if (fields.Count() == 2)
                {
                    try
                    {
                        switch (fields[0].ToLower())
                        {
                            case "directed":
                                if (fields[1] == "0")
                                    graph = null;
                                break;
                            case "threshold":
                                switch (fields[1].ToLower())
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
                                break;
                            case "rule":
                                if (fields[1].ToLower() == "modified")
                                    modified = true;
                                else
                                    modified = false;
                                break;

                            case "node":
                                if (graph == null)
                                {
                                    graph = new FuzzyCognitiveMap(type, modified);
                                }  
                                ProcessNode(stIn, ref graph, ref conceptLookup);
                                break;
                            case "edge":
                                ProcessEdge(stIn, ref graph, ref conceptLookup);
                                break;
                        }
                    }
                    // catch conversion exceptions from ProcessNode and ProcessEdge only
                    catch (FormatException)
                    {
                        return;
                    }
                    catch (OverflowException)
                    {
                        return;
                    }

                }
            }
        }

        private static void ProcessNode(TextReader stIn, ref FuzzyCognitiveMap graph, ref Dictionary<uint, string> lookup)
        {
            string[] fields;
            string line = string.Empty;
            uint ID = 0;
            string label = string.Empty;
            float initial = 0.0F;
            float level = 0.0F;

            while ((line = stIn.ReadLine()) != null)
            {
                fields = SplitAndClean(line);
                if (fields.Count() == 1 && fields[0] == "]")
                {
                    
                    // nb: the id is reassigned by the FCM, hence the specific uint may change. This will
                    // not effect deserialization as the ids are consistent in the file and withing the in-memory FCM.
                    lookup.Add(ID, label);
                    graph.AddConcept(label, initial, level);
                    return;
                }

                if (fields.Count() == 2)
                {
                    switch (fields[0].ToLower())
                    {
                        case "id":
                            ID = Convert.ToUInt32(fields[1]);
                            break;
                        case "label":
                            label = fields[1];
                            break;
                        case "initial":
                            initial = (float)Convert.ToDouble(fields[1]);
                            break;
                        case "activation":
                            level = (float)Convert.ToDouble(fields[1]);
                            break;

                    }
                }
            }
        }
        private static void ProcessEdge(TextReader stIn, ref FuzzyCognitiveMap graph, ref Dictionary<uint, string> lookup)
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

                    graph.AddInfluence(lookup[src], lookup[tgt], wt);
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
                            wt = (float)Convert.ToDouble(fields[1]);
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
            string[] fields = Regex.Split(line, @"\s+");
            // remove quotes from quoted strings
            char[] quotes = {'"', '\''};
            int len = fields.Count();
            for (int i = 0; i < len; i++)
            {
                fields[i] = fields[i].Trim(quotes);
            }
            return fields;
        }
    }
}
