using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{
    public class EdgeTensor
    {
        private string from;
        private string to;
        List<Tuple<string, string>> coordinates;

        public string From { get { return from; } }
        public string To { get { return to; } }
    }
}
