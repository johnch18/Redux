using API.Interfaces;

namespace API.Problems.NPComplete.NPC_NUMBERLINK.Solvers;

class NumberlinkMRVBacktracking : ISolver<NUMBERLINK> {
    public string solverName { get; } = "Minimum Remaining Values Backtracking";
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
    "Andrija Sevaljevic"
    };

    public bool timerHasExpired { get; set; }
    public SolverType solverType { get; } = SolverType.Backtracking;
    public SolverComplexityBucket complexityBucket { get; } = SolverComplexityBucket.Exponential;
    public string complexity { get; } = "O(n * 3^n), n = |cells|";

    public NumberlinkMRVBacktracking() { }

    // Grid state
    int W, H;
    int[] g = Array.Empty<int>();
    char[] dir = Array.Empty<char>();
    Dictionary<int, int> head = new(), target = new();
    HashSet<int> done = new();

    static readonly (int dx, int dy, char c)[] Moves = { (1, 0, 'R'), (-1, 0, 'L'), (0, 1, 'D'), (0, -1, 'U') };

    public string solve(NUMBERLINK problem) {
        W = problem.spanX;
        H = problem.spanY;
        g = new int[W * H];
        dir = new char[W * H];
        head = new();
        target = new();
        done = new();

        // Find all heads
        for (int y = 0; y < H; y++) {
            for (int x = 0; x < W; x++) {
                int c = problem.grid[y][x];
                int p = y * W + x;
                g[p] = c;
                if (c == 0) continue;
                if (!head.ContainsKey(c)) head[c] = p;
                else {
                    target[c] = p;
                    dir[p] = 'F';
                }
            }
        }

        if (!Dfs()) return "No solution";

        var rows = new List<string>();
        for (int y = 0; y < H; y++) {
            var cells = new List<string>();
            for (int x = 0; x < W; x++) cells.Add($"{g[y * W + x]}{dir[y * W + x]}");
            rows.Add(string.Join(", ", cells) + ";");
        }

        string certificate = string.Join("\n", rows);
        if(!problem.defaultVerifier.verify(problem, certificate)) certificate = "{}";
        return certificate;
    }

    bool Dfs() {
        if (HasDeadCell()) return false;

        // MRV: pick the unfinished color with the fewest legal moves
        int bestColor = -1;
        List<int>? bestMoves = null;
        foreach (int c in head.Keys) {
            if (done.Contains(c)) continue;
            var moves = LegalMoves(c);
            if (moves.Count == 0) return false;
            if (bestMoves == null || moves.Count < bestMoves.Count) {
                bestColor = c; bestMoves = moves;
                if (moves.Count == 1) break; // forced move
            }
        }

        if (bestMoves == null) return Array.IndexOf(g, 0) < 0; // all paths done: grid must be full

        int c0 = bestColor, from = head[c0];
        foreach (int to in bestMoves) {
            dir[from] = DirChar(from, to);
            if (to == target[c0]) {
                done.Add(c0);
                if (Dfs()) return true;
                done.Remove(c0);
            } else {
                g[to] = c0; head[c0] = to;
                if (Dfs()) return true;
                g[to] = 0; head[c0] = from;
            }
            dir[from] = '\0';
        }
        return false;
    }

    List<int> LegalMoves(int c) {
        var res = new List<int>(4);
        int p = head[c], x = p % W, y = p / W;
        foreach (var (dx, dy, _) in Moves) {
            int nx = x + dx, ny = y + dy;
            if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
            int n = ny * W + nx;
            if (g[n] == 0 || n == target[c]) res.Add(n);
        }
        return res;
    }

    // An empty cell needs at least 2 neighbors that could connect to it (empty or open path end)
    bool HasDeadCell() {
        for (int p = 0; p < g.Length; p++) {
            if (g[p] != 0) continue;
            int x = p % W, y = p / W, cnt = 0;
            foreach (var (dx, dy, _) in Moves) {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                int n = ny * W + nx, k = g[n];
                if (k == 0 || (!done.Contains(k) && (n == head[k] || n == target[k]))) cnt++;
            }
            if (cnt < 2) return true;
        }
        return false;
    }

    char DirChar(int from, int to) {
        int d = to - from;
        return d == 1 ? 'R' : d == -1 ? 'L' : d == W ? 'D' : 'U';
    }
}