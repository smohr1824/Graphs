using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networks.Core
{
    public class LayerSet 
    {
        private string aspect;
        private Dictionary<string, Network> elementaryLayers;


        public LayerSet(string feature)
        {
            aspect = feature;
            elementaryLayers = new Dictionary<string, Network>();
        }

        public void AddLayer(string aspectValue, Network elementaryLayer)
        {
            try
            {
                elementaryLayers.Add(aspectValue, elementaryLayer);
            }
            catch (ArgumentNullException exn)
            {
                throw exn;
            }
            catch (ArgumentException ex)
            {

                throw ex;
            }

        }
    }
}
