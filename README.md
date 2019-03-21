# Graphs
Research code for working with graphs

## Basic classes
Graphs are represented by the Network class.  Clusters are represented by HashSet&lt;uint&gt;, where the uint is a vertex id.  Graphs are loaded
via the static NetworkSerializer class, while the similar ClusterSerializer class exists for clusters. 

Multilayer graphs are implemented via the MultilayerNetwork class and serialized via the MultilayerNetworkSerializer class.  Multilayer graphs explicitly store intralayer and non-node coupled interlayer edges in adjacency lists.  Node coupled interlayer edges are supported algorithmically.  Node coupling may be limited to a single aspect.  When so constrained, node coupling may be further limited to ordinal coupling, herein defined as a layer immediately adjacent to the source layer.
Elementary layers may be flattened to create a supra-adjacency matrix if and only if the multilayer network is node-aligned.  The capability to pad non-node-aligned networks when creating this matrix is planned.

### Serialization Format
Each line of a graph represents an edge adjacency list.  The first uint is the from vertex, followed by the delimiter character, followed by,
the to vertex, followed by the delimiter and the edge weight.  Edge weights are floats.  Graphs are assumed to be directed, unless the 
file is loaded with the directed parameter of LoadNetwork set to false.  

# Community detection algorithms 
Presently, the Partitioning class implements the following community detection algorithms:
1. Connected Iterative Scan (CIS)
2. Speaker-Listener Propagation Algorithm (SLPA)
3. Louvain (Modularity, Goldberg, and Resolution quality metrics)

The Connected Iterative Scan algorithm is described in Baumes J., Goldberg M., Krishnamoorthy M., Magdon-Ismail M., Preston N. Finding Communities by Clustering a Graph into Overlapping Subgraphs. Proceedings of the IADIS International Conference on Applied Computing, :97-104, Feb-2005.
The algorithmic code itself is adapted from the standard C++ code provided at http://www.cs.rpi.edu/~magdon/LFDlabpublic.html/software/CIS/CIS.tar.gz.

SLPA is described in Xie, Jierui and Szymanski, Boleslaw, Towards Linear Time Overlapping Community Detection in Social Networks, Proceedings of the Pacific-Asiz Conference on Knowledge Discovery and Data Mining, :25-36, 2012.

Louvain is described in Campigotto, R., Céspedes, P. , and Guillaume, JL., A Generalized and Adaptive Method for Community Detection,
arxiv:1406.2518v1, 2014 and Blondel, V., Guillaume JL., Lambiotte, R., and Lefebvre, E, Fast Unfolding of Communities in Large Networks,
Journal of Statistical Mechanics: Theory and Experiment, Issue 10, pp. 10008, 2008.  The algorithmic code is greatly adapted under the GPL Lesser Public License from https://sourceforge.net/projects/louvain/.

IsBipartite tests a network for bipartness.  If successful, the two sets of vertices are returned as List&lt;uint&gt; where the uint is the vertex id.

Additional algorithm implementations are planned.

The FCM namespace adds basic fuzzy cognitive map capability utilizing the Network class behind the scenes. The threshold function for map inference may be set to bivalent, trivalent, or logistic by specifying an enumerated type, or the user may implement a custom function by creating a delegate of the form float f(float sum). If no threshold function is specified, the map defaults to bivalent. Similarly, the user may select the classic or modified Kosko equation for map inference.  The default is classic. Multiple cores are used to improve performance when the number of cencepts exceeds 100.
The Step method of the FuzzyCognitiveMap class performs one generation of inference using algebraic methods, while the StepWalk method performs the same task by walking the list of concepts and performing the calculation algorithmically.  Step executes with O(N^3) complexity, while
StepWalk executes with O(|V| + |E|) complexity.

Multilayer fuzzy cognitive maps are added as well. Presently, inference is only supported algorithmically.

Serialization of fuzzy cognitive maps and multilayer fuzzy cognitive maps is planned.


