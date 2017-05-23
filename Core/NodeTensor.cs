using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{
    public class NodeTensor
    {
        public string nodeId;
        public List<string> aspectCoordinates;
        public NodeTensor(string id, string coords)
        {
            nodeId = id;
            aspectCoordinates = new List<string>(coords.Split(','));
        }

        public override string ToString()
        {
            return nodeId + ":" + string.Join(",", aspectCoordinates);
        }
    }

    
}
