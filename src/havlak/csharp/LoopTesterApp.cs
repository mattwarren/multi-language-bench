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
// Test Code
//======================================================

/**
 * Test Program for the Havlak loop finder.
 *
 * This program constructs a fairly large control flow
 * graph and performs loop recognition. This is the C#
 * version.
 *
 * @author matt warren (from the Java port by rhundt)
 */

using MultiLanguageBench.cfg;
using MultiLanguageBench.havlakloopfinder;
using MultiLanguageBench.lsg;
using System;

namespace MultiLanguageBench
{
    class LoopTesterApp
    {
        public LoopTesterApp()
        {
            cfg = new CFG();
            lsg = new LSG();
            root = cfg.createNode(0);
        }

        // Create 4 basic blocks, corresponding to and if/then/else clause
        // with a CFG that looks like a diamond
        public int buildDiamond(int start)
        {
            int bb0 = start;
            new BasicBlockEdge(cfg, bb0, bb0 + 1);
            new BasicBlockEdge(cfg, bb0, bb0 + 2);
            new BasicBlockEdge(cfg, bb0 + 1, bb0 + 3);
            new BasicBlockEdge(cfg, bb0 + 2, bb0 + 3);

            return bb0 + 3;
        }

        // Connect two existing nodes
        public void buildConnect(int start, int end)
        {
            new BasicBlockEdge(cfg, start, end);
        }

        // Form a straight connected sequence of n basic blocks
        public int buildStraight(int start, int n)
        {
            for (int i = 0; i < n; i++)
            {
                buildConnect(start + i, start + i + 1);
            }
            return start + n;
        }

        // Construct a simple loop with two diamonds in it
        public int buildBaseLoop(int from)
        {
            int header = buildStraight(from, 1);
            int diamond1 = buildDiamond(header);
            int d11 = buildStraight(diamond1, 1);
            int diamond2 = buildDiamond(d11);
            int footer = buildStraight(diamond2, 1);
            buildConnect(diamond2, d11);
            buildConnect(diamond1, header);

            buildConnect(footer, from);
            footer = buildStraight(footer, 1);
            return footer;
        }

        public void getMem()
        {
            long val = GC.GetTotalMemory(forceFullCollection: false) / 1024;
            Console.WriteLine("  Total Memory: " + val + " KB");
        }

        public static void main(String[] args)
        {
            Console.WriteLine("Welcome to LoopTesterApp, C# edition");

            Console.WriteLine("Constructing App...");
            LoopTesterApp app = new LoopTesterApp();
            app.getMem();

            Console.WriteLine("Constructing Simple CFG...");
            app.cfg.createNode(0);
            app.buildBaseLoop(0);
            app.cfg.createNode(1);
            new BasicBlockEdge(app.cfg, 0, 2);

            Console.WriteLine("15000 dummy loops");
            for (int dummyloop = 0; dummyloop < 15000; dummyloop++)
            {
                HavlakLoopFinder finder = new HavlakLoopFinder(app.cfg, app.lsg);
                finder.findLoops();
            }

            Console.WriteLine("Constructing CFG...");
            int n = 2;

            for (int parlooptrees = 0; parlooptrees < 10; parlooptrees++)
            {
                app.cfg.createNode(n + 1);
                app.buildConnect(2, n + 1);
                n = n + 1;

                for (int i = 0; i < 100; i++)
                {
                    int top = n;
                    n = app.buildStraight(n, 1);
                    for (int j = 0; j < 25; j++)
                    {
                        n = app.buildBaseLoop(n);
                    }
                    int bottom = app.buildStraight(n, 1);
                    app.buildConnect(n, top);
                    n = bottom;
                }
                app.buildConnect(n, 1);
            }

            app.getMem();
            Console.Write("Performing Loop Recognition\n1 Iteration\n");
            HavlakLoopFinder finder1 = new HavlakLoopFinder(app.cfg, app.lsg);
            finder1.findLoops();
            app.getMem();

            Console.WriteLine("Another 50 iterations...");
            for (int i = 0; i < 50; i++)
            {
                Console.Write(".");
                HavlakLoopFinder finder2 = new HavlakLoopFinder(app.cfg, new LSG());
                finder2.findLoops();
            }

            Console.WriteLine("");
            app.getMem();
            Console.WriteLine("# of loops: " + app.lsg.getNumLoops() +
                               " (including 1 artificial root node)");
            Console.WriteLine("# of BBs  : " + BasicBlock.getNumBasicBlocks());
            Console.WriteLine("# max time: " + finder1.getMaxMillis());
            Console.WriteLine("# min time: " + finder1.getMinMillis());
            app.lsg.calculateNestingLevel();
            //app.lsg.Dump();
        }

        public CFG cfg;
        private LSG lsg;
        private BasicBlock root;
    }
}
