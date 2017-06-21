Overview
==========
Networks
==========
Networks may be directed or undirected.  The determination is made at the time of instantiation.  If an edge is added to an undirected network, a reciprocal edge from the target back to the source is created, 
i.e., all edges are represented internally as directed. In weights and out weights may be returned for directed networks.  When these methods are called for an undirected
network, the total weight of the outgoing edges is returned.  This matches the expected definition of weights for undirected networks and hides the internal representation using directed edges.
Similarly, in degree and out degree for undirected networks hides the internal representation.

Adding an edge when one or both vertices does not exist results in the creation of the needed vertices.

MultilayerNetworks
==================
This implementation of generalized multilayer networks is intended for uses in which elementary layers may be developed and used independently,
but layers and aspects may be desired to add semantic richness to the model.  Aspects are fixed at the time of network creation. Aspects are defined by string labels, 
and layer sets within an aspect are similarly addressed by a string value.  Tensor notation is supported 
for brevity and convenience.  Vertices in the multilayer network are addressed in the form <node id>:a1,a2,..,ad where a1..ad are the aspect coordinates for the d aspects.

Multilayer networks may be directed or undirected.  The decision is made at the time of instantiation.  Each elementary layer added to the multilayer network 
must be the same (i.e., directed or undirected) as the multilayer network or an exception will be thrown.

Elementary layers are implicitly diagonally coupled.  Any given vertex is assumed to be coupled to any vertex with the same id on any other layer.  The coupling has zero length. 
Thus, any vertex is adjacent to the neighbors of a vertex with the same id on other layers. Neighboring vertices with intra-aspect only node coupling is also permitted using the CategoricalGetNeighbors method. In the event a node-coupled edge duplicates an explicit interlayer edge, GetSources and GetNeighbors will return the node-coupled edge.  The same will occur in CategoricalGetSources and CategoricalGetNeighbors if and only if the node-coupling in within the designated aspect.

Internally, elementary layers contain a Network instance and properties to manage interlayer edges.  This permits us to easily distinguish between in-layer neighbors and edges  and interlayer 
neighbors and edges.  This comes at the expense of some overhead.  The opposite approach, storing all vertices and edges as layer-qualified entities with no internal networks 
avoids the overhead but makes it difficult to distinguish between individual layers.  We shall monitor the performance of our chosen approach, but the intended use cases strongly argue for maintaining
the identity of the elementary layers as networks.

Once a network is added to the multilayer network as an elementary layer, further addition or removal of nodes and edges is through the multilayer network. If an
elementary layer is returned, it is returned as a copy of the graph in that layer, i.e., all interlayer edges are lost.  Changes made to the copy are not reflected in the multilayer network.  This supports applications which wish
to work with an elementary layer (e.g., looking at a particular process represented as a graph) but retain access to the additional semantics of a multilayer network.

Addition of an edge in which one or both vertices does not exist results in the creation of the needed vertex or vertices in the designated elementary layer.  Attempting to
add an edge in which one or both vertices specifies an elementary layer which does not exist, however, results in an Argument exception, i.e., elementary layers are only created through
the addition of an existing Network.

