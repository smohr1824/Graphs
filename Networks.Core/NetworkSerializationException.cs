using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{
    public enum EntityType
    {
        property,
        node,
        edge
    }
    public class NetworkSerializationException : Exception
    {
        public EntityType Target {  get; }
        public string NetworkMessage {  get; }
        public Exception RawException { get; }

        public NetworkSerializationException(EntityType tgt, string msg, Exception ex = null) : base()
        {
            Target = tgt;
            NetworkMessage = msg;
            RawException = ex;
        }
    }
}
