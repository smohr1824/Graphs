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
    }
}
