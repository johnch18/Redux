using API.Interfaces;

namespace API.Problems.NPComplete.NPC_NUMBERLINK.Solvers;

class NumberlinkBacktrackingSolver : ISolver<NUMBERLINK> {

    // --- Fields ---
    public string solverName { get; } = "Backtracking";
    public string solverDefinition { get; } = "Builds the paths one pair at a time. The current path is extended one cell at a time into unoccupied cells; when it reaches its partner endpoint the next pair is started. Dead ends are undone and other moves tried. Once every pair is linked, the solution is accepted only if every cell is covered.";
    public string source { get; } = "Depth-first backtracking search (original implementation)";
    public string[] contributors { get; } = { "Andrija Sevaljevic", "Brayden Peck", "Charles Johnson" };
    public SolverType solverType { get; } = SolverType.Backtracking;
    public SolverComplexityBucket complexityBucket { get; } = SolverComplexityBucket.Exponential;
    public bool timerHasExpired { get; set; }

    // --- Methods Including Constructors ---
    public NumberlinkBacktrackingSolver() { }

    public string solve(NUMBERLINK problem) {
        if (problem.pairIds.Length == 0) return NumberlinkCertificate.NoSolution;

        var search = new Search(problem, () => timerHasExpired);
        bool found = search.Run(0, search.Start[0]);
        if (search.TimedOut) return NumberlinkCertificate.Timeout;
        if (!found) return NumberlinkCertificate.NoSolution;
        return NumberlinkCertificate.Build(problem, search.ExtractPaths());
    }

    private sealed class Search {
        private readonly NUMBERLINK problem;
        private readonly Func<bool> expired;
        private readonly int w, h, k;
        private readonly int[] ids;
        public readonly int[] Start;
        private readonly int[] fin;
        private readonly int[] owner;   // -1 = free, otherwise index of the pair occupying the cell
        private readonly int[] next;    // successor of each path cell
        private int free;
        public bool TimedOut;

        public Search(NUMBERLINK problem, Func<bool> expired) {
            this.problem = problem;
            this.expired = expired;
            w = problem.spanX;
            h = problem.spanY;
            ids = problem.pairIds;
            k = ids.Length;
            Start = new int[k];
            fin = new int[k];
            owner = new int[w * h];
            next = new int[w * h];
            Array.Fill(owner, -1);
            Array.Fill(next, -1);
            for (int p = 0; p < k; p++) {
                var (ax, ay, bx, by) = problem.endpoints[ids[p]];
                Start[p] = ay * w + ax;
                fin[p] = by * w + bx;
                owner[Start[p]] = p;
                owner[fin[p]] = p;
            }
            free = w * h - 2 * k;
        }

        public bool Run(int color, int cur) {
            if (TimedOut) return false;
            if (expired()) { TimedOut = true; return false; }

            int cx = cur % w, cy = cur / w;
            for (int d = 0; d < 4; d++) {
                int nx = cx + NumberlinkCertificate.Dx[d], ny = cy + NumberlinkCertificate.Dy[d];
                if (!problem.in_bounds(nx, ny)) continue;
                int n = ny * w + nx;

                if (n == fin[color]) {
                    next[cur] = n;
                    bool done = color + 1 == k ? free == 0 : Run(color + 1, Start[color + 1]);
                    if (done) return true;
                    next[cur] = -1;
                    if (TimedOut) return false;
                    continue;
                }

                if (owner[n] != -1) continue;

                owner[n] = color;
                free--;
                next[cur] = n;
                if (Run(color, n)) return true;
                next[cur] = -1;
                owner[n] = -1;
                free++;
                if (TimedOut) return false;
            }
            return false;
        }

        public Dictionary<int, List<int>> ExtractPaths() {
            var paths = new Dictionary<int, List<int>>();
            for (int p = 0; p < k; p++) {
                var list = new List<int>();
                int c = Start[p];
                list.Add(c);
                while (c != fin[p]) {
                    c = next[c];
                    list.Add(c);
                }
                paths[ids[p]] = list;
            }
            return paths;
        }
    }
}
