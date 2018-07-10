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

            StreamReader reader = new StreamReader(filename);   // don't catch any exceptions, let the caller respond
            Network retVal = ReadNetwork(reader);
            reader.Close();
            return retVal;
        }

        public static Network ReadNetwork(TextReader stIn)
        {
            throw new NotImplementedException();

        }



        // split a line on whitespace -- we expect a name, value pair delimited by whitespace
        private static string[] SplitAndClean()
        {
            string[] fields = Regex.Split("your string here", @"\s+");
            int len = fields.Count();
            for (int i = 0; i < len; i++)
            {
                fields[i] = fields[i].Trim();
            }
            return fields;
        }
    }
}
