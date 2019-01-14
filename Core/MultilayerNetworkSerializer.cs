// MIT License

// Copyright(c) 2017 - 2018 Stephen Mohr

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
    public class MultilayerNetworkSerializer
    {
        private const uint Abort = 0;
        private const uint AspectsBlock = 1;
        private const uint LayerBlock = 2;
        private const uint InterlayerBlock = 3;

        private const string AspectsCommand = @"Aspects";
        private const string LayerCommand = @"Layer";
        private const string InterlayerEdgesCommand = @"Interlayer edges";

        #region public write methods
        public static void WriteMultiLayerNetworkToFile(MultilayerNetwork net, string filename, char delimiter = '|')
        {
            StreamWriter writer = new StreamWriter(filename);
            WriteMultiLayerNetwork(net, writer, delimiter);
            writer.Close();
        }

        public static void WriteMultiLayerNetwork(MultilayerNetwork G, TextWriter writer, char delimiter)
        {
            G.List(writer, delimiter);
        }

        #endregion

        #region public read methods

        public static MultilayerNetwork ReadMultilayerNetworkFromFile(string filename, bool directed, char delimiter = '|')
        {

            StreamReader reader = new StreamReader(filename);   // don't catch any exceptions, let the caller respond
            MultilayerNetwork retVal = ReadMultilayerNetwork(reader, directed, delimiter);
            reader.Close();
            return retVal;
        }

        public static MultilayerNetwork ReadMultilayerNetwork(TextReader stIn, bool directed, char delimiter = '|')
        {
            // TODO: turn this into a proper state machine for robustness
            string line = string.Empty;
            MultilayerNetwork retVal = null;
            List<Tuple<NodeLayerTuple, NodeLayerTuple, float>> interEdges = new List<Tuple<NodeLayerTuple, NodeLayerTuple, float>>();

            line = MoveToNextBlock(stIn);
            uint block; 
            while ((block = DetectBlock(line)) != Abort)
            {
                switch (block)
                {
                    // read in and prepare the aspects
                    case AspectsBlock:
                        List<Tuple<string, IEnumerable<string>>> aspects = new List<Tuple<string, IEnumerable<string>>>();
                       
                        while ((line = stIn.ReadLine()) != null && line.Trim() != string.Empty)
                        {
                            List<string> dimensions = new List<string>();
                            string[] aspectFields = SplitAndCleanAspect(line);
                            if (aspectFields.Count() != 2)
                                continue;
                            string[] indices = aspectFields[1].Split(',');
                            foreach (string index in indices)
                                dimensions.Add(index);
                            aspects.Add(new Tuple<string, IEnumerable<string>>(aspectFields[0], dimensions));
                        }

                        retVal = new MultilayerNetwork(aspects, directed);
                        line = MoveToNextBlock(stIn);
                        break;

                    // read a single elementary layer into a memory stream, then pass it off to NetworkSerializer
                    case LayerBlock:
                        string[] coords = SplitAndCleanLayer(line);
                        MemoryStream graphStream = new MemoryStream();
                        StreamWriter writer = new StreamWriter(graphStream, Encoding.UTF8);
                        while ((line = stIn.ReadLine()) != null && line != string.Empty && !IsCommandLine(line))
                            writer.WriteLine(line);
                        writer.Flush();
                        graphStream.Position = 0;
                        StreamReader reader = new StreamReader(graphStream);
                        Network net = NetworkSerializer.ReadNetwork(reader, true);
                        retVal.AddElementaryLayer(coords, net);
                        if (!IsCommandLine(line))
                            line = MoveToNextBlock(stIn);
                        else
                            line = TrimControlCharacter(line);
                        break;

                    // Process an elementary layer's interlayer edges.
                    // Since the to-vertex's elementary layer may not exist yet, stick it into a list for later processing
                    case InterlayerBlock:
                        string[] edgeParams;
                        while ((line = stIn.ReadLine()) != null && line != string.Empty && !IsCommandLine(line))
                        {
                            edgeParams = SplitAndCleanInterlayer(line, delimiter);
                            if (edgeParams.Length == 3)
                            {
                                string[] tupleParams = edgeParams[0].Split(':');
                                NodeLayerTuple tupleFrom = new NodeLayerTuple(tupleParams[0], tupleParams[1]);
                                tupleParams = edgeParams[1].Split(':');
                                NodeLayerTuple tupleTo = new NodeLayerTuple(tupleParams[0], tupleParams[1]);
                                float wt = Convert.ToSingle(edgeParams[2]);
                                Tuple<NodeLayerTuple, NodeLayerTuple, float> tpl = new Tuple<NodeLayerTuple, NodeLayerTuple, float>(tupleFrom, tupleTo, wt);

                                // If the target elementary layer exists, add the edge, otherwise, add to the list to be resolved later
                                if (retVal.HasElementaryLayer(tupleTo.coordinates))
                                    retVal.AddEdge(tupleFrom, tupleTo, wt);
                                else
                                    interEdges.Add(tpl);
                            }
                        }
                        if (!IsCommandLine(line))
                            line = MoveToNextBlock(stIn);
                        else
                            line = TrimControlCharacter(line);
                        break;
                }
            }

            // now add the accumulated interlayer edges
            // TODO: add an opportunistic test after each elementary layer is complete so as to keep the length of this list down
            // for multilayer networks with lots of interlayer edges -- perf test to find a balance of entries in list and number of elementary layers instantiated
            foreach (Tuple<NodeLayerTuple, NodeLayerTuple, float> tuple in interEdges)
                retVal.AddEdge(tuple.Item1, tuple.Item2, tuple.Item3);
            return retVal;
        }
        #endregion

        #region private methods

        private static string TrimControlCharacter(string line)
        {
            if (line != null)
            {
                if (line.Substring(0, 1) == ":")
                    return line.Substring(1).Trim();
            }
            return line;
        }

        private static uint DetectBlock(string line)
        {
            if (line == null)
                return Abort;

            if (line.StartsWith(AspectsCommand))
                return AspectsBlock;

            if (line.StartsWith(LayerCommand))
                return LayerBlock;

            if (line.StartsWith(InterlayerEdgesCommand))
                return InterlayerBlock;

            return Abort;
        }

        private static bool IsCommandLine(string line)
        {
            if (line != null && line != string.Empty && line.Substring(0, 1) == ":")
                return true;
            else
                return false;
        }

        private static string MoveToNextBlock(TextReader reader)
        {
            string line = string.Empty;
            while ((line = reader.ReadLine()) != null && !IsCommandLine(line)) ;
            return TrimControlCharacter(line);
        }

        #region split and clean methods

        private static string[] SplitAndCleanAspect(string line)
        {
            string[] fields = line.Split(':');
            int len = fields.Count();
            for (int i = 0; i < len; i++)
            {
                fields[i] = fields[i].Trim();
            }
            return fields;
        }

        private static string[] SplitAndCleanLayer(string line)
        {
            int firstParen = line.IndexOf('(');
            line = line.Substring(firstParen + 1);
            string[] aspectCoords = line.Split(',');
            string last = aspectCoords[aspectCoords.Count() - 1];
            if (last[last.Length - 1] == ')')
                last = last.Substring(0, last.Length - 1);
            aspectCoords[aspectCoords.Count() - 1] = last;
            return aspectCoords;
        }

        private static string[] SplitAndCleanInterlayer(string line, char delimiter)
        {
            return line.Split(delimiter);
            
        }


        #endregion

        #endregion
    }
}
