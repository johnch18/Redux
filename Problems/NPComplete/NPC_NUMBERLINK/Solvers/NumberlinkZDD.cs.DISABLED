using System.Collections.Generic;
using API.Interfaces;

namespace API.Problems.NPComplete.NPC_NUMBERLINK.Solvers;

class NumberlinkZDD : ISolver<NUMBERLINK> {
    public class Edge {
        public int Id { get; set; }
        public int u { get; set; }
        public int v { get; set; }

        public Edge(int ID, int U, int V) {
            Id = ID; u = U; v = V;
        }
    };

    public class Frontier : IEquatable<Frontier> {
        public int[] degrees { get; }

        public Frontier(int vcount) {
            degrees = new int[vcount];
        }

        public Frontier(int[] degs) {
            degrees = degs;
        }

        public bool Equals(Frontier oth) {
            return oth != null && degrees.SequenceEqual(oth.degrees);
        }

        public override bool Equals(object o) => Equals(o as Frontier);

        public override int GetHashCode() {
            int hash = 17;
            foreach (var d in degrees) {
                hash *= 23;
                hash += d;
            }
            return hash;
        }
    };

    public class ZddNode {
        public string Id { get; set; }
        public Frontier State { get; set; }
        public ZddNode Low { get; set; }
        public ZddNode High { get; set; }

        public ZddNode(string id, Frontier state) {
            Id = id;
            State = state;
        }
    };


    // TODO: This was copy pasted mostly.
    public string solverName { get; } = "Frontier Based Search";
    public string solverDefinition { get; } = "Depth-first backtracking search that grows one path at a time."
    + " At each node it computes the legal moves for every unfinished color's path head and expands the"
    + " color with the fewest options (minimum remaining values), so forced moves are taken immediately."
    + " Before recursing, it prunes the branch if any color has no legal move or if any empty cell has"
    + " fewer than two neighbors that could connect to it, since such a cell can never be part of a valid"
    + " path. Moves are undone on failure, and the search succeeds when every color is connected and"
    + " the grid is full.";
    public string source { get; } = "Robert M. Haralick and Gordon L. Elliott. 1980. Increasing tree search efficiency for constraint satisfaction problems. Artificial Intelligence 14, 3 (October 1980), 263-313. https://doi.org/10.1016/0004-3702(80)90051-X";
    public string sourceLink { get; } = "https://doi.org/10.1016/0004-3702(80)90051-X";
    public string[] contributors { get; } = {
    "Andrija Sevaljevic", "Brayden Peck", "Charles W. Johnson"
    };

    public bool timerHasExpired { get; set; }
    public SolverType solverType { get; } = SolverType.Backtracking;
    public SolverComplexityBucket complexityBucket { get; } = SolverComplexityBucket.Exponential;
    public string complexity { get; } = "O(n * 3^n), n = |cells|";

    private int nodeCtr = 0;

    public NumberlinkZDD() { }

    public string solve(NUMBERLINK prob) {
        int spanX = prob.spanX;
        int spanY = prob.spanY;
        int nnodes = spanX * spanY;

        HashSet<int> terminals = new HashSet<int>();
        for (int y = 0; y < spanY; y++) {
            for (int x = 0; x < spanX; x++) {
                int id = spanX * y + x;
                int atloc = prob.at(x, y);
                if (atLoc > 0) terminals.Add(id);
            }
        }

        //v 2 unique edges per node, minus those on the outer ring of the graph
        List<Edge> edges = constructEdges(spanX, spanY);

        HashSet<int> verts = new HashSet<int>(Enumerable.Range(0, nnodes));
        ZddNode tTerm = new ZddNode("T", null);
        ZddNode fTerm = new ZddNode("F", null);

        Dictionary<Frontier, ZddNode> currLayer = new Dictionary<Frontier, ZddNode>();

        Frontier rootState = new Frontier(nnodes);
        ZddNode rootNode = new ZddNode($"N{_nodeCtr++}", rootState);
        currLayer[rootState] = rootNode;

        for (int i = 0; i < edges.Count; i++) {
            Edge e = edges[i];
            Dictionary<Frontier, ZddNode> nextLayer = new Dictionary<Frontier, ZddNode>();
            var remEdges = edges.Skip(i + 1).ToList();
            HashSet<int> deadVerts = new HashSet<int(verts);
            foreach (var remEdge in remEdges) {
                deadVerts.Remove(remEdge.u);
                deadVerts.Remove(remEdge.v);
            }

            foreach (var kvp in currLayer) {
                ZddNode parentNode = kvp.Value;
                Frontier parentState = kvp.Key;

                int[] lowDeg = (int[])parentState.Degrees.Clone();
                fork(terminals, lowDeg, deadVerts, nextLayer, parentNode, false, tTerm, fTerm);

                int[] highDeg = (int[])parentState.Degrees.Clone();
                highDeg[edge.u]++;
                highDeg[edge.v]++;
                fork(terminals, highDeg, deadVerts, nextLayer, parentNode, true, tTerm, fTerm);
            }
        }
    }

    private void fork(HashSet<int> terminals, int[] degrees, HashSet<int> deadVerts, Dictionary<FrontierState, ZddNode> nextLayer, ZddNode parentNode, bool isHigh, ZddNode tTerm, ZddNode fTerm) {
        for (int v = 0; v < degrees.Length; v++) {
            bool isTerminal = terminals.Contains(v);
            bool isDead = deadVerts.Contains(v);
            if ((isTerminal && degrees[v] > 1)
            || (!isTerminal && degrees[v] > 2)) {
                link(parentNode, fTerm, isHigh);
                return;
            }
            if (!isDead) continue;

            if ((isTerminal && degrees[v] != 1)
            || (!isTerminal && degrees[v] != 0 && degrees[v] != 2)) {
                link(parentNode, fTerm, isHigh);
                return;
            }
        }

        bool allDead = deadVerts.Count == degrees.Length;

        if (isHigh && nextLayer.Count == 0 && allDead) {
            // TODO: I don't know.
        }
        if (allDead) {
            link(parentNode, tTerm, isHigh);
            return;
        }
        Frontier nextState = new Frontier(degrees);
        if (!nextLayer.TryGetValue(nextState, out ZddNode target)) {
            target = new ZddNode($"N(nodeCtr++)", nextState);
            nextLayer[nextState] = target;
        }
        LinkNode(parentNode, target, isHigh);
    }

    private static List<Edge> constructEdges(int spanX, int spanY) {
        int edgeCt = 0;
        List<Edge> edges = new List<Edge>();
        for (int y = 0; y < spanY - 1; y++) {
            for (int x = 0; x < spanX - 1; x++) {
                //v We use a horizontal ravel for id<->pos
                int thisNode = spanX * y + x;
                //v Right is next
                int rightNode = thisNode + 1;
                //v Down is next row
                int downNode = thisNode + spanX;
                //
                Edge rightEdge = new Edge(edgeCt++, thisNode, rightNode);
                Edge downEdge = new Edge(edgeCt++, thisNode, downNode);
                //
                edges.Add(rightEdge);
                edges.Add(downEdge);
            }
        }
        return edges;
    }

    private static void link(ZddNode parent, ZddNode child, bool isHigh) {
        if (isHigh) parent.High = child;
        else parent.Low = child;
    }

}


