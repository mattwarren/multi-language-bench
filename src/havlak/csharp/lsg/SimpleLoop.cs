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
 * The Havlak loop finding algorithm.
 *
 * @author matt warren (from the Java port by rhundt)
 */

using MultiLanguageBench.cfg;
using System;
using System.Collections.Generic;

namespace MultiLanguageBench.lsg
{
    /**
     * class SimpleLoop
     *
     * Basic representation of loops, a loop has an entry point,
     * one or more exit edges, a set of basic blocks, and potentially
     * an outer loop - a "parent" loop.
     *
     * Furthermore, it can have any set of properties, e.g.,
     * it can be an irreducible loop, have control flow, be
     * a candidate for transformations, and what not.
     */
    public class SimpleLoop
    {
        public SimpleLoop()
        {
            parent = null;
            isRootField = false;
            isReducible = true;
            nestingLevel = 0;
            depthLevel = 0;
            basicBlocks = new List<BasicBlock>();
            children = new List<SimpleLoop>();
        }

        public void addNode(BasicBlock bb)
        {
            basicBlocks.Add(bb);
        }

        public void addChildLoop(SimpleLoop loop)
        {
            children.Add(loop);
        }

        public void dump(int indent)
        {
            for (int i = 0; i < indent; i++)
                Console.Write("  ");

            Console.Write("loop-{0} nest: {1} depth {2} {3}",
                          counter, nestingLevel, depthLevel,
                          isReducible ? "" : "(Irreducible) ");
            if (getChildren().Count != 0)
            {
                Console.Write("Children: ");
                foreach (SimpleLoop loop in getChildren())
                {
                    Console.Write("loop-{0} ", loop.getCounter());
                }
            }
            if (basicBlocks.Count != 0)
            {
                Console.Write("(");
                foreach (BasicBlock bb in basicBlocks)
                {
                    Console.Write("BB#{0}{1}", bb.getName(), header == bb ? "* " : " ");
                }
                Console.Write("\b)");
            }
            Console.Write("\n");
        }

        // Getters/Setters
        public List<SimpleLoop> getChildren()
        {
            return children;
        }
        public SimpleLoop getParent()
        {
            return parent;
        }
        public int getNestingLevel()
        {
            return nestingLevel;
        }
        public int getDepthLevel()
        {
            return depthLevel;
        }
        public int getCounter()
        {
            return counter;
        }
        public bool isRoot()
        {   // Note: fct and var are same!
            return isRootField;
        }
        public void setParent(SimpleLoop parent)
        {
            this.parent = parent;
            this.parent.addChildLoop(this);
        }
        public void setHeader(BasicBlock bb)
        {
            basicBlocks.Add(bb);
            header = bb;
        }
        public void setIsRoot()
        {
            isRootField = true;
        }
        public void setCounter(int value)
        {
            counter = value;
        }
        public void setNestingLevel(int level)
        {
            nestingLevel = level;
            if (level == 0)
            {
                setIsRoot();
            }
        }
        public void setDepthLevel(int level)
        {
            depthLevel = level;
        }
        public void setIsReducible(bool isReducible)
        {
            this.isReducible = isReducible;
        }

        private List<BasicBlock> basicBlocks;
        private List<SimpleLoop> children;
        private SimpleLoop parent;
        private BasicBlock header;

        private bool isRootField;
        private bool isReducible;
        private int counter;
        private int nestingLevel;
        private int depthLevel;
    };
}
