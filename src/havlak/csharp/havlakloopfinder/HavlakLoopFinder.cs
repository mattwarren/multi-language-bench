// Copyright 2011 Google Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

//======================================================
// Main Algorithm
//======================================================

/**
 * The Havlak loop finding algorithm.
 *
 * @author matt warren (from the Java port by rhundt)
 */

using MultiLanguageBench.cfg;
using MultiLanguageBench.lsg;
using System.Collections.Generic;
using System;

namespace MultiLanguageBench.havlakloopfinder
{
    /**
     * class HavlakLoopFinder
     *
     * This class encapsulates the complete finder algorithm
     */
    public class HavlakLoopFinder
    {
        public HavlakLoopFinder(CFG cfg, LSG lsg)
        {
            this.cfg = cfg;
            this.lsg = lsg;
        }

        public long getMaxMillis()
        {
            return maxMillis;
        }

        public long getMinMillis()
        {
            return minMillis;
        }

        /**
         * enum BasicBlockClass
         *
         * Basic Blocks and Loops are being classified as regular, irreducible,
         * and so on. This enum contains a symbolic name for all these classifications
         */
        public enum BasicBlockClass
        {
            BB_TOP,          // uninitialized
            BB_NONHEADER,    // a regular BB
            BB_REDUCIBLE,    // reducible loop
            BB_SELF,         // single BB loop
            BB_IRREDUCIBLE,  // irreducible loop
            BB_DEAD,         // a dead BB
            BB_LAST          // Sentinel
        }

        /**
         * class UnionFindNode
         *
         * The algorithm uses the Union/Find algorithm to collapse
         * complete loops into a single node. These nodes and the
         * corresponding functionality are implemented with this class
         */
        public class UnionFindNode
        {
            public UnionFindNode()
            {
            }

            // Initialize this node.
            //
            public void initNode(BasicBlock bb, int dfsNumber)
            {
                this.parent = this;
                this.bb = bb;
                this.dfsNumber = dfsNumber;
                this.loop = null;
            }

            // Union/Find Algorithm - The find routine.
            //
            // Implemented with Path Compression (inner loops are only
            // visited and collapsed once, however, deep nests would still
            // result in significant traversals).
            //
            public UnionFindNode findSet()
            {
                List<UnionFindNode> nodeList = new List<UnionFindNode>(2);

                UnionFindNode node = this;
                while (node != node.getParent())
                {
                    if (node.getParent() != node.getParent().getParent())
                    {
                        nodeList.Add(node);
                    }
                    node = node.getParent();
                }

                // Path Compression, all nodes' parents point to the 1st level parent.
                int len = nodeList.Count;
                for (int i = 0; i < len; i++)
                {
                    // for (UnionFindNode iter : nodeList)
                    UnionFindNode iter = nodeList[i];
                    iter.setParent(node.getParent());
                }
                return node;
            }

            // Union/Find Algorithm - The union routine.
            //
            // Trivial. Assigning parent pointer is enough,
            // we rely on path compression.
            //
            void union(UnionFindNode basicBlock)
            {
                setParent(basicBlock);
            }

            // Getters/Setters
            //
            UnionFindNode getParent()
            {
                return parent;
            }
            internal BasicBlock getBb()
            {
                return bb;
            }
            internal SimpleLoop getLoop()
            {
                return loop;
            }
            internal int getDfsNumber()
            {
                return dfsNumber;
            }

            void setParent(UnionFindNode parent)
            {
                this.parent = parent;
            }
            void setLoop(SimpleLoop loop)
            {
                this.loop = loop;
            }

            private UnionFindNode parent;
            private BasicBlock bb;
            private SimpleLoop loop;
            private int dfsNumber;
        }

        //
        // Constants
        //
        // Marker for uninitialized nodes.
        static readonly int UNVISITED = int.MaxValue;

        // Safeguard against pathologic algorithm behavior.
        static readonly int MAXNONBACKPREDS = (32 * 1024);

        //
        // IsAncestor
        //
        // As described in the paper, determine whether a node 'w' is a
        // "true" ancestor for node 'v'.
        //
        // Dominance can be tested quickly using a pre-order trick
        // for depth-first spanning trees. This is why DFS is the first
        // thing we run below.
        //
        bool isAncestor(int w, int v, int[] last)
        {
            return ((w <= v) && (v <= last[w]));
        }

        //
        // DFS - Depth-First-Search
        //
        // DESCRIPTION:
        // Simple depth first traversal along out edges with node numbering.
        //
        int doDFS(BasicBlock currentNode,
                  UnionFindNode[] nodes,
                  Dictionary<BasicBlock, int> number,
                  int[] last,
                  /*final*/ int current)
        {
            nodes[current].initNode(currentNode, current);
            number.Add(currentNode, current);

            int lastid = current;
            // for (BasicBlock target : currentNode.getOutEdges()) {
            int len = currentNode.getOutEdges().size();
            for (int i = 0; i < len; i++)
            {
                BasicBlock target = currentNode.getOutEdges().get(i);
                if (number[target] == UNVISITED)
                {
                    lastid = doDFS(target, nodes, number, last, lastid + 1);
                }
            }
            last[number[currentNode]] = lastid;
            return lastid;
        }

        static List<HashSet<int>> nonBackPreds = new List<HashSet<int>>();
        static List<List<int>> backPreds = new List<List<int>>();
        static Dictionary<BasicBlock, int> number = new Dictionary<BasicBlock, int>();
        static int maxSize = 0;
        static int[] header;
        static BasicBlockClass[] type;
        static int[] last;
        static UnionFindNode[] nodes;
        static LinkedList<HashSet<int>> freeListSet = new LinkedList<HashSet<int>>();
        static LinkedList<List<int>> freeListList = new LinkedList<List<int>>();

        //
        // findLoops
        //
        // Find loops and build loop forest using Havlak's algorithm, which
        // is derived from Tarjan. Variable names and step numbering has
        // been chosen to be identical to the nomenclature in Havlak's
        // paper (which, in turn, is similar to the one used by Tarjan).
        //
        public void findLoops()
        {
            if (cfg.getStartBasicBlock() == null)
            {
                return;
            }

            long startMillis = CurrentTimeMillis();

            int size = cfg.getNumNodes();

            nonBackPreds.Clear();
            backPreds.Clear();
            number.Clear();
            if (size > maxSize)
            {
                header = new int[size];
                type = new BasicBlockClass[size];
                last = new int[size];
                nodes = new UnionFindNode[size];
                maxSize = size;
            }

            /*
            List<Set<Integer>>       nonBackPreds = new ArrayList<Set<Integer>>();
            List<List<Integer>>      backPreds = new ArrayList<List<Integer>>();

            Map<BasicBlock, Integer> number = new HashMap<BasicBlock, Integer>();
            int[]                    header = new int[size];
            BasicBlockClass[]        type = new BasicBlockClass[size];
            int[]                    last = new int[size];
            UnionFindNode[]          nodes = new UnionFindNode[size];
            */

            for (int i = 0; i < size; ++i)
            {
                //nonBackPreds.Add(freeListSet.Count == 0 ? new HashSet<int>() : freeListSet.RemoveFirst().Clear());
                if (freeListList.Count == 0)
                {
                    nonBackPreds.Add(new HashSet<int>());
                }
                else
                {
                    var node = freeListSet.First;
                    freeListSet.RemoveFirst();
                    node.Value.Clear();
                    nonBackPreds.Add(node.Value);
                }

                //backPreds.Add(freeListList.Count == 0 ? new List<int>() : freeListList.RemoveFirst().Clear());
                if (freeListList.Count == 0)
                {
                    backPreds.Add(new List<int>());
                }
                else
                {
                    var node = freeListList.First;
                    freeListList.RemoveFirst();
                    node.Value.Clear();
                    backPreds.Add(node.Value);
                }

                nodes[i] = new UnionFindNode();
            }

            // Step a:
            //   - initialize all nodes as unvisited.
            //   - depth-first traversal and numbering.
            //   - unreached BB's are marked as dead.
            //
            foreach (var bbIter in cfg.getBasicBlocks().values())
            {
                number.Add(bbIter, UNVISITED);
            }

            doDFS(cfg.getStartBasicBlock(), nodes, number, last, 0);

            // Step b:
            //   - iterate over all nodes.
            //
            //   A backedge comes from a descendant in the DFS tree, and non-backedges
            //   from non-descendants (following Tarjan).
            //
            //   - check incoming edges 'v' and add them to either
            //     - the list of backedges (backPreds) or
            //     - the list of non-backedges (nonBackPreds)
            //
            for (int w = 0; w < size; w++)
            {
                header[w] = 0;
                type[w] = BasicBlockClass.BB_NONHEADER;

                BasicBlock nodeW = nodes[w].getBb();
                if (nodeW == null)
                {
                    type[w] = BasicBlockClass.BB_DEAD;
                    continue;  // dead BB
                }

                if (nodeW.getNumPred() > 0)
                {
                    int len1 = nodeW.getInEdges().size();
                    for (int i = 0; i < len1; i++)
                    {
                        // for (BasicBlock nodeV : nodeW.getInEdges()) {
                        BasicBlock nodeV = nodeW.getInEdges().get(i);
                        int v = number[nodeV];
                        if (v == UNVISITED)
                        {
                            continue;  // dead node
                        }

                        if (isAncestor(w, v, last))
                        {
                            backPreds[w].Add(v);
                        }
                        else
                        {
                            nonBackPreds[w].Add(v);
                        }
                    }
                }
            }

            // Start node is root of all other loops.
            header[0] = 0;

            // Step c:
            //
            // The outer loop, unchanged from Tarjan. It does nothing except
            // for those nodes which are the destinations of backedges.
            // For a header node w, we chase backward from the sources of the
            // backedges adding nodes to the set P, representing the body of
            // the loop headed by w.
            //
            // By running through the nodes in reverse of the DFST preorder,
            // we ensure that inner loop headers will be processed before the
            // headers for surrounding loops.
            //
            for (int w = size - 1; w >= 0; w--)
            {
                // this is 'P' in Havlak's paper
                LinkedList<UnionFindNode> nodePool = new LinkedList<UnionFindNode>();

                BasicBlock nodeW = nodes[w].getBb();
                if (nodeW == null)
                {
                    continue;  // dead BB
                }

                // Step d:
                int len = backPreds[w].Count;
                for (int i = 0; i < len; i++)
                {
                    int v = backPreds[w][i];
                    // for (int v : backPreds.get(w)) {
                    if (v != w)
                    {
                        nodePool.AddLast(nodes[v].findSet());
                    }
                    else
                    {
                        type[w] = BasicBlockClass.BB_SELF;
                    }
                }

                // Copy nodePool to workList.
                //
                LinkedList<UnionFindNode> workList = new LinkedList<UnionFindNode>();

                foreach (var niter in nodePool)
                    workList.AddLast(niter);

                if (nodePool.Count != 0)
                {
                    type[w] = BasicBlockClass.BB_REDUCIBLE;
                }

                // work the list...
                //
                while (workList.Count != 0) // while (!workList.isEmpty())
                {
                    LinkedListNode<UnionFindNode> x = workList.First;
                    workList.RemoveFirst();

                    // Step e:
                    //
                    // Step e represents the main difference from Tarjan's method.
                    // Chasing upwards from the sources of a node w's backedges. If
                    // there is a node y' that is not a descendant of w, w is marked
                    // the header of an irreducible loop, there is another entry
                    // into this loop that avoids w.
                    //

                    // The algorithm has degenerated. Break and
                    // return in this case.
                    //
                    int nonBackSize = nonBackPreds[x.Value.getDfsNumber()].Count;
                    if (nonBackSize > MAXNONBACKPREDS)
                    {
                        return;
                    }

                    HashSet<int> curr = nonBackPreds[x.Value.getDfsNumber()];
                    foreach (var iter in curr)
                    {
                        UnionFindNode y = nodes[iter];
                        UnionFindNode ydash = y.findSet();

                        if (!isAncestor(w, ydash.getDfsNumber(), last))
                        {
                            type[w] = BasicBlockClass.BB_IRREDUCIBLE;
                            nonBackPreds[w].Add(ydash.getDfsNumber());
                        }
                        else
                        {
                            if (ydash.getDfsNumber() != w)
                            {
                                if (!nodePool.Contains(ydash))
                                {
                                    workList.AddLast(ydash);
                                    nodePool.AddLast(ydash);
                                }
                            }
                        }
                    }
                }

                // Collapse/Unionize nodes in a SCC to a single node
                // For every SCC found, create a loop descriptor and link it in.
                //
                if ((nodePool.Count > 0) || (type[w] == BasicBlockClass.BB_SELF))
                {
                    SimpleLoop loop = lsg.createNewLoop();

                    loop.setHeader(nodeW);
                    loop.setIsReducible(type[w] != BasicBlockClass.BB_IRREDUCIBLE);

                    // At this point, one can set attributes to the loop, such as:
                    //
                    // the bottom node:
                    //    iter  = backPreds[w].begin();
                    //    loop bottom is: nodes[iter].node);
                    //
                    // the number of backedges:
                    //    backPreds[w].size()
                    //
                    // whether this loop is reducible:
                    //    type[w] != BasicBlockClass.BB_IRREDUCIBLE
                    //
                    nodes[w].setLoop(loop);

                    foreach (var node in nodePool)
                    {
                        // Add nodes to loop descriptor.
                        header[node.getDfsNumber()] = w;
                        node.union(nodes[w]);

                        // Nested loops are not added, but linked together.
                        if (node.getLoop() != null)
                        {
                            node.getLoop().setParent(loop);
                        }
                        else
                        {
                            loop.addNode(node.getBb());
                        }
                    }

                    lsg.addLoop(loop);
                }  // nodePool.size
            }  // Step c

            long totalMillis = CurrentTimeMillis() - startMillis;

            if (totalMillis > maxMillis)
            {
                maxMillis = totalMillis;
            }
            if (totalMillis < minMillis)
            {
                minMillis = totalMillis;
            }
            for (int i = 0; i < size; ++i)
            {
                freeListSet.AddLast(nonBackPreds[i]); // Add() in Java LinkedList adds to the end
                freeListList.AddLast(backPreds[i]);
                nodes[i] = new UnionFindNode();
            }
        }  // findLoops

        // From http://stackoverflow.com/questions/290227/java-system-currenttimemillis-equivalent-in-c-sharp/290265#290265
        private static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long CurrentTimeMillis()
        {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
        }

        private CFG cfg;      // Control Flow Graph
        private LSG lsg;      // Loop Structure Graph

        private static long maxMillis = 0;
        private static long minMillis = int.MaxValue;
    }
}
