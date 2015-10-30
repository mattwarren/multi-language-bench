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
 * A simple class simulating the concept of Basic Blocks
 *
 * @author matt warren (from the Java port by rhundt)
 */

using System;
using System.Collections.Generic;

namespace MultiLanguageBench.cfg
{
    /**
     * class BasicBlock
     *
     * BasicBlock only maintains a vector of in-edges and
     * a vector of out-edges.
     */
    public class BasicBlock
    {
        static int numBasicBlocks = 0;

        public static int getNumBasicBlocks()
        {
            return numBasicBlocks;
        }

        public BasicBlock(int name)
        {
            this.name = name;
            inEdges = new List<BasicBlock>(2);
            outEdges = new List<BasicBlock>(2);
            ++numBasicBlocks;
        }

        public void dump()
        {
            Console.Write("BB#{0,3}: ", getName()); //"%03d"
            if (inEdges.Count > 0)
            {
                Console.Write("in : ");
                foreach (BasicBlock bb in inEdges)
                {
                    Console.Write("BB#{0,3} ", bb.getName()); // %03d
                }
            }
            if (outEdges.Count > 0)
            {
                Console.Write("out: ");
                foreach (BasicBlock bb in outEdges)
                {
                    Console.Write("BB#{0,3} ", bb.getName()); // %03d
                }
            }
            Console.WriteLine();
        }

        public int getName()
        {
            return name;
        }

        public List<BasicBlock> getInEdges()
        {
            return inEdges;
        }
        public List<BasicBlock> getOutEdges()
        {
            return outEdges;
        }

        public int getNumPred()
        {
            return inEdges.Count;
        }
        public int getNumSucc()
        {
            return outEdges.Count;
        }

        public void addOutEdge(BasicBlock to)
        {
            outEdges.Add(to);
        }
        public void addInEdge(BasicBlock from)
        {
            inEdges.Add(from);
        }

        private List<BasicBlock> inEdges, outEdges;
        private int name;
    };
}
