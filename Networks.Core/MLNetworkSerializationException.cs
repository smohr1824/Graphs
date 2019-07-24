using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{
    public enum MLEntityType
    {
        property,
        layer,
        node,
        edge,
        interlayerEdge
    }
    public class MLNetworkSerializationException : Exception
    {
        public MLEntityType Target { get; }
        public string MLNetworkMessage { get; }
        public Exception RawException { get; }
        public NetworkSerializationException NetworkException { get;}

        public MLNetworkSerializationException(MLEntityType tgt, string msg, NetworkSerializationException nsx, Exception ex = null) : base()
        {
            Target = tgt;
            MLNetworkMessage = msg;
            NetworkException = nsx;
            RawException = ex;
        }
    }
}
