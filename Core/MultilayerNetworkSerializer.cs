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
    }
}
