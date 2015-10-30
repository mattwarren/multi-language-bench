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

/**
 * A simple class simulating the concept of
 * a control flow graph.
 *
 * @author matt warren (from the Java port by rhundt)
 */

using System.Collections.Generic;

namespace MultiLanguageBench.cfg
{
    /**
     * class CFG
     *
     * CFG maintains a list of nodes, plus a start node.
     * That's it.
     */
    public class CFG
    {
        public CFG()
        {
            startNode = null;
            basicBlockMap = new Dictionary<int, BasicBlock>();
            edgeList = new List<BasicBlockEdge>();
        }

        public BasicBlock createNode(int name)
        {
            BasicBlock node;
            if (!basicBlockMap.ContainsKey(name))
            {
                node = new BasicBlock(name);
                basicBlockMap.Add(name, node);
            }
            else
            {
                node = basicBlockMap[name];
            }

            if (getNumNodes() == 1)
            {
                startNode = node;
            }

            return node;
        }

        public void dump()
        {
            foreach (BasicBlock bb in basicBlockMap.Values)
            {
                bb.dump();
            }
        }

        public void addEdge(BasicBlockEdge edge)
        {
            edgeList.Add(edge);
        }

        public int getNumNodes()
        {
            return basicBlockMap.size();
        }

        public BasicBlock getStartBasicBlock()
        {
            return startNode;
        }

        public BasicBlock getDst(BasicBlockEdge edge)
        {
            return edge.getDst();
        }

        public BasicBlock getSrc(BasicBlockEdge edge)
        {
            return edge.getSrc();
        }

        public Dictionary<int, BasicBlock> getBasicBlocks()
        {
            return basicBlockMap;
        }

        private Dictionary<int, BasicBlock> basicBlockMap;
        private BasicBlock startNode;
        private List<BasicBlockEdge> edgeList;
    };
}
