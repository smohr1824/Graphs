using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
