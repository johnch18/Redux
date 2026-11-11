using API.Interfaces;

namespace API.Problems.NPComplete.NPC_NUMBERLINK.Solvers;

class NumberlinkBruteForceSolver : ISolver<NUMBERLINK> {

    // --- Fields ---
    public string solverName { get; } = "Brute Force";
    public string solverDefinition { get; } = "Enumerates every assignment of a 'next cell' direction to every cell (the finishing endpoint of each pair is fixed), with no pruning. Each complete assignment is checked by walking every pair from its start: paths must reach their finish without revisiting a cell or entering another pair's endpoint, and together must cover every cell.";
    public string source { get; } = "Exhaustive search (original implementation)";
    public string[] contributors { get; } = { "Andrija Sevaljevic", "Brayden Peck", "Charles Johnson" };
    public SolverType solverType { get; } = SolverType.BruteForce;
    public SolverComplexityBucket complexityBucket { get; } = SolverComplexityBucket.Exponential;
    // Candidate space is at most 4^(R*C); each candidate is checked in O(R*C).
    public string complexity { get; } = "O(4^(R*C) * R*C)";
    public bool timerHasExpired { get; set; }

    // --- Methods Including Constructors ---
    public NumberlinkBruteForceSolver() { }

    public string solve(NUMBERLINK problem) {
        int w = problem.spanX, h = problem.spanY, n = w * h;
        int[] ids = problem.pairIds;
        int k = ids.Length;
        if (k == 0) return NumberlinkCertificate.NoSolution;

        var start = new int[k];
        var fin = new int[k];
        var endpointOf = new int[n];
        Array.Fill(endpointOf, -1);
        var isFinish = new bool[n];

        for (int p = 0; p < k; p++) {
            var (ax, ay, bx, by) = problem.endpoints[ids[p]];
            start[p] = ay * w + ax;
            fin[p] = by * w + bx;
            endpointOf[start[p]] = p;
            endpointOf[fin[p]] = p;
            isFinish[fin[p]] = true;
        }

        // options[c] = the in-bounds neighbours a cell may point at (finish cells are fixed).
        var options = new int[n][];
        for (int c = 0; c < n; c++) {
            if (isFinish[c]) { options[c] = new[] { -1 }; continue; }
            int x = c % w, y = c / w;
            var list = new List<int>();
            for (int d = 0; d < 4; d++) {
                int nx = x + NumberlinkCertificate.Dx[d], ny = y + NumberlinkCertificate.Dy[d];
                if (problem.in_bounds(nx, ny)) list.Add(ny * w + nx);
            }
            options[c] = list.ToArray();
        }

        var pick = new int[n];
        var seen = new bool[n];
        long iterations = 0;

        while (true) {
            if ((++iterations & 0xFF) == 0 && timerHasExpired)
                return NumberlinkCertificate.Timeout;

            if (Evaluate(start, fin, endpointOf, options, pick, seen, n)) {
                var paths = new Dictionary<int, List<int>>();
                for (int p = 0; p < k; p++) {
                    var list = new List<int>();
                    int c = start[p];
                    list.Add(c);
                    while (c != fin[p]) {
                        c = options[c][pick[c]];
                        list.Add(c);
                    }
                    paths[ids[p]] = list;
                }
                return NumberlinkCertificate.Build(problem, paths);
            }

            // Odometer-style increment over the candidate space.
            int idx = n - 1;
            while (idx >= 0) {
                pick[idx]++;
                if (pick[idx] < options[idx].Length) break;
                pick[idx] = 0;
                idx--;
            }
            if (idx < 0) break;
        }

        return NumberlinkCertificate.NoSolution;
    }

    private static bool Evaluate(int[] start, int[] fin, int[] endpointOf, int[][] options, int[] pick, bool[] seen, int n) {
        Array.Clear(seen);
        int covered = 0;
        for (int p = 0; p < start.Length; p++) {
            int c = start[p];
            while (true) {
                if (seen[c]) return false;
                if (endpointOf[c] >= 0 && endpointOf[c] != p) return false;
                seen[c] = true;
                covered++;
                if (c == fin[p]) break;
                c = options[c][pick[c]];
            }
        }
        return covered == n;
    }
}
