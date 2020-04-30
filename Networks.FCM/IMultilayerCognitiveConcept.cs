using System;
using System.Collections.Generic;
using System.Text;

namespace Networks.FCM
{
    public interface IMultilayerCognitiveConcept: ICognitiveConcept
    {
        IEnumerable<ILayerActivationLevel> LayerActivationLevels { get; }
    }

    public interface ILayerActivationLevel
    {
        string Coordinates { get; }
        float Level { get; }
    }

    internal class layerActivationLevel : ILayerActivationLevel
    {
        private string _coords;
        private float _level;
        public string Coordinates { get { return _coords; } }
        public float Level { get { return _level; } }

        internal layerActivationLevel(string coords, float level)
        {
            _coords = coords;
            _level = level;
        }
    }

}
