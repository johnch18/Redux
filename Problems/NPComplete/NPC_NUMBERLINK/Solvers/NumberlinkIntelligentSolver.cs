using API.Interfaces;

namespace API.Problems.NPComplete.NPC_NUMBERLINK.Solvers;

class NumberlinkIntelligentSolver : ISolver<NUMBERLINK> {

    // --- Fields ---
    public string solverName { get; } = "Constraint Propagation";
    public string solverDefinition { get; } = "Models the puzzle as choosing which grid edges are used. Every endpoint cell must use exactly 1 edge and every other cell exactly 2. Propagation forces edges in or out when a cell's degree is decided, and rejects any edge that would close a cycle or join fragments belonging to two different pairs. When propagation stalls, the solver branches on an edge of the most constrained cell and recurses.";
    public string source { get; } = "Constraint propagation with search (original implementation)";
    public string[] contributors { get; } = { "Andrija Sevaljevic", "Brayden Peck", "Charles Johnson" };
    public SolverType solverType { get; } = SolverType.Backtracking;
    public SolverComplexityBucket complexityBucket { get; } = SolverComplexityBucket.Exponential;
    public bool timerHasExpired { get; set; }

    // --- Methods Including Constructors ---
    public NumberlinkIntelligentSolver() { }

    public string solve(NUMBERLINK problem) {
        if (problem.pairIds.Length == 0) return NumberlinkCertificate.NoSolution;

        var engine = new Engine(problem, () => timerHasExpired);
        sbyte[]? result = engine.Solve(new sbyte[engine.EdgeCount]);
        if (engine.TimedOut) return NumberlinkCertificate.Timeout;
        if (result == null) return NumberlinkCertificate.NoSolution;
        return NumberlinkCertificate.Build(problem, engine.ExtractPaths(result));
    }

    private sealed class Engine {
        // Edge states
        private const sbyte Unknown = 0, In = 1, Out = -1;

        private readonly NUMBERLINK problem;
        private readonly Func<bool> expired;
        private readonly int w, n;
        private readonly int[] ids;
        private readonly int[] color;        // pair number at each endpoint cell, 0 otherwise
        private readonly int[] required;     // 1 for endpoints, 2 for every other cell
        private readonly int[] edgeA, edgeB;
        private readonly int[][] cellEdges;
        private readonly int[] parent;
        private readonly int[] compColor;
        public readonly int EdgeCount;
        public bool TimedOut;

        public Engine(NUMBERLINK problem, Func<bool> expired) {
            this.problem = problem;
            this.expired = expired;
            w = problem.spanX;
            int h = problem.spanY;
            n = w * h;
            ids = problem.pairIds;
            color = new int[n];
            required = new int[n];
            parent = new int[n];
            compColor = new int[n];

            foreach (int id in ids) {
                var (ax, ay, bx, by) = problem.endpoints[id];
                color[ay * w + ax] = id;
                color[by * w + bx] = id;
            }
            for (int c = 0; c < n; c++) required[c] = color[c] != 0 ? 1 : 2;

            var a = new List<int>();
            var b = new List<int>();
            var incident = new List<int>[n];
            for (int c = 0; c < n; c++) incident[c] = new List<int>();
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) {
                    int c = y * w + x;
                    if (x + 1 < w) { incident[c].Add(a.Count); incident[c + 1].Add(a.Count); a.Add(c); b.Add(c + 1); }
                    if (y + 1 < h) { incident[c].Add(a.Count); incident[c + w].Add(a.Count); a.Add(c); b.Add(c + w); }
                }
            }
            edgeA = a.ToArray();
            edgeB = b.ToArray();
            EdgeCount = edgeA.Length;
            cellEdges = new int[n][];
            for (int c = 0; c < n; c++) cellEdges[c] = incident[c].ToArray();
        }

        private int Find(int x) {
            while (parent[x] != x) {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }
            return x;
        }

        /// <summary>Applies the propagation rules to a fixpoint. Returns false on contradiction.</summary>
        private bool Propagate(sbyte[] s) {
            while (true) {
                bool changed = false;

                // Degree rule
                for (int c = 0; c < n; c++) {
                    int inCount = 0, unk = 0;
                    foreach (int e in cellEdges[c]) {
                        if (s[e] == In) inCount++;
                        else if (s[e] == Unknown) unk++;
                    }
                    int d = required[c];
                    if (inCount > d || inCount + unk < d) return false;
                    if (unk > 0) {
                        if (inCount == d) {
                            foreach (int e in cellEdges[c]) if (s[e] == Unknown) s[e] = Out;
                            changed = true;
                        } else if (inCount + unk == d) {
                            foreach (int e in cellEdges[c]) if (s[e] == Unknown) s[e] = In;
                            changed = true;
                        }
                    }
                }

                // Fragment rule: union the chosen edges
                for (int c = 0; c < n; c++) { parent[c] = c; compColor[c] = 0; }
                for (int e = 0; e < EdgeCount; e++) {
                    if (s[e] != In) continue;
                    int ra = Find(edgeA[e]), rb = Find(edgeB[e]);
                    if (ra == rb) return false;   // closed a cycle
                    parent[ra] = rb;
                }
                for (int c = 0; c < n; c++) {
                    if (color[c] == 0) continue;
                    int r = Find(c);
                    if (compColor[r] != 0 && compColor[r] != color[c]) return false;   // two pairs joined
                    compColor[r] = color[c];
                }
                for (int e = 0; e < EdgeCount; e++) {
                    if (s[e] != Unknown) continue;
                    int ra = Find(edgeA[e]), rb = Find(edgeB[e]);
                    if (ra == rb || (compColor[ra] != 0 && compColor[rb] != 0 && compColor[ra] != compColor[rb])) {
                        s[e] = Out;
                        changed = true;
                    }
                }

                if (!changed) return true;
            }
        }

        public sbyte[]? Solve(sbyte[] s) {
            if (TimedOut) return null;
            if (expired()) { TimedOut = true; return null; }
            if (!Propagate(s)) return null;

            // Branch on an edge of the most constrained cell (fewest unknown edges).
            int best = -1, bestUnknown = int.MaxValue;
            for (int c = 0; c < n; c++) {
                int unk = 0;
                foreach (int e in cellEdges[c]) if (s[e] == Unknown) unk++;
                if (unk > 0 && unk < bestUnknown) { bestUnknown = unk; best = c; }
            }
            if (best < 0) return s;   // every edge decided and every constraint satisfied

            int edge = -1;
            foreach (int e in cellEdges[best]) {
                if (s[e] == Unknown) { edge = e; break; }
            }

            foreach (sbyte value in new sbyte[] { In, Out }) {
                var copy = (sbyte[])s.Clone();
                copy[edge] = value;
                var result = Solve(copy);
                if (result != null) return result;
                if (TimedOut) return null;
            }
            return null;
        }

        public Dictionary<int, List<int>> ExtractPaths(sbyte[] s) {
            var adj = new List<int>[n];
            for (int c = 0; c < n; c++) adj[c] = new List<int>();
            for (int e = 0; e < EdgeCount; e++) {
                if (s[e] != In) continue;
                adj[edgeA[e]].Add(edgeB[e]);
                adj[edgeB[e]].Add(edgeA[e]);
            }

            var paths = new Dictionary<int, List<int>>();
            foreach (int id in ids) {
                var (ax, ay, bx, by) = problem.endpoints[id];
                int startCell = ay * w + ax, finishCell = by * w + bx;
                var list = new List<int> { startCell };
                int prev = -1, cur = startCell;
                while (cur != finishCell && list.Count <= n) {
                    int nxt = -1;
                    foreach (int q in adj[cur]) {
                        if (q != prev) { nxt = q; break; }
                    }
                    prev = cur;
                    cur = nxt;
                    list.Add(cur);
                }
                paths[id] = list;
            }
            return paths;
        }
    }
}
