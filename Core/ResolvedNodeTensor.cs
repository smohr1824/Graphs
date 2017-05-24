using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{
    internal class ResolvedNodeTensor
    {
        public string nodeId;
        public List<int> coordinates;

        public ResolvedNodeTensor()
        {
            coordinates = new List<int>();
        }

        public bool IsSameElementaryLayer(ResolvedNodeTensor b)
        {
            for (int i = 0; i < coordinates.Count(); i++)
            {
                if (coordinates[i] != b.coordinates[i])
                {
                    return false;
                }
            }
            return true;
        }

        public void List(TextWriter writer)
        {
            writer.Write(this.ToString());
        }

        public override string ToString()
        {
            return nodeId + ":" + string.Join(",", coordinates);
        }

    }

}
