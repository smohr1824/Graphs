using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{
    public class NodeTensorEqualityComparer : IEqualityComparer<ResolvedNodeTensor>
    {
        bool IEqualityComparer<ResolvedNodeTensor>.Equals(ResolvedNodeTensor a, ResolvedNodeTensor b)
        {
            if (a.nodeId == b.nodeId)
            {
                if (a.coordinates.Count() == b.coordinates.Count())
                {
                    for (int i = 0; i < a.coordinates.Count(); i++)
                    {
                        if (a.coordinates[i] != b.coordinates[i])
                            return false;
                    }
                    return true;
                }
                else return false;
            }
            else
                return false;
        }

        int IEqualityComparer<ResolvedNodeTensor>.GetHashCode(ResolvedNodeTensor tensor)
        {
            int code = tensor.nodeId.GetHashCode();
            string field = string.Empty;
            foreach (int coord in tensor.coordinates)
                field += coord.ToString();
            code += field.GetHashCode();
            return code;
        }
    }
}
