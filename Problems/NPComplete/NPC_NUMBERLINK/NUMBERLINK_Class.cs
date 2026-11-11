using API.Interfaces;
using API.DummyClasses;

using API.Problems.NPComplete.NPC_NUMBERLINK.Solvers;
using API.Problems.NPComplete.NPC_NUMBERLINK.Verifiers;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace API.Problems.NPComplete.NPC_NUMBERLINK;

class NUMBERLINK : IProblem<NumberlinkIntelligentSolver, NumberlinkVerifier, DummyVisualization> {

    public string problemName { get; } = "Zig-Zag Numberlink";
    public string problemLink { get; } = "https://en.wikipedia.org/wiki/Numberlink";

    public string formalDefinition { get; } = "{(G, T) | G is an M x N grid graph, T = {(s_1,t_1),...,(s_k,t_k)} is a set of k pairs of distinct cells of G (all 2k cells distinct), and there exist k vertex-disjoint paths P_1,...,P_k in G, with P_i from s_i to t_i, such that every cell of G lies on exactly one P_i}";

    public string problemDefinition { get; } = "Numberlink (Flow Free style) is a pathing problem: given an MxN grid and a collection of numbered cell pairs, connect each pair with an orthogonal path so that no two paths share a cell and together the paths cover every cell of the grid. Paths may bend freely (the 'zig-zag' variant).";

    public string inputDescription { get; } = "A grid of non-negative numbers, where each non-zero number appears exactly twice. Zero indicates an empty grid cell.";

    public string outputDescription { get; } = "A grid in which every cell holds its path number and the direction (R, L, U, D) of the next cell on its path, or F at the path's finishing endpoint.";

    public string source { get; } = "Adcock, A., Demaine, E. D., Demaine, M. L., O'Brien, M. P., Reidl, F., Sanchez Villaamil, F., Sullivan, B. D. (2015). Zig-Zag Numberlink is NP-Complete. Journal of Information Processing, 23(3), 239-245.";

    public string sourceLink { get; } = "https://arxiv.org/abs/1410.5845";

    public const string InstanceGrammar = "N semicolon (or newline) separated rows of M comma separated non-negative numbers, 0 representing an empty space. Each non-zero number must appear exactly twice.";

    private static readonly string _defaultInstance = "1,0,0,2,3;\n0,0,0,4,0;\n0,0,4,0,0;\n0,2,3,0,5;\n0,1,5,0,0;";

    private static readonly char[] RowSeparators = { ';', '\n', '\r' };

    public string defaultInstance { get; } = _defaultInstance;

    public string instanceFormat { get; } = $"Format: {InstanceGrammar} Example: {_defaultInstance}";

    public string certificateFormat { get; } = $"Format: {NumberlinkVerifier.CertificateGrammar} Example: {NumberlinkVerifier.CertificateExample}";

    public string wikiName { get; } = "Numberlink";

    public NumberlinkIntelligentSolver defaultSolver { get; } = new NumberlinkIntelligentSolver();
    public NumberlinkVerifier defaultVerifier { get; } = new NumberlinkVerifier();
    public DummyVisualization defaultVisualization { get; } = new DummyVisualization();

    public ComplexityClass complexityClass { get; } = ComplexityClass.NPComplete;
    public ProblemType problemType { get; } = ProblemType.GamesAndPuzzles;

    public string[] contributors { get; } = {
        "Andrija Sevaljevic",
        "Brayden Peck",
        "Charles Johnson"
    };

    public string instance { get; set; } = string.Empty;

    //v Our input grid of enumerated cells (row-major: grid[y][x])
    public int[][] grid { get; set; } = Array.Empty<int[]>();

    //v Spans in each dimension (width = spanX, height = spanY)
    public int spanX { get; set; }
    public int spanY { get; set; }

    //v Hash set of our colorings/numberings
    public HashSet<int> numbersPresent { get; set; } = new HashSet<int>();

    //v The pair numbers in ascending order
    public int[] pairIds { get; set; } = Array.Empty<int>();

    //v Per pair number: (x, y) of the first endpoint and (x, y) of the second, in row-major order
    public Dictionary<int, (int ax, int ay, int bx, int by)> endpoints { get; set; } = new();

    public NUMBERLINK() : this(_defaultInstance) { }

    public NUMBERLINK(string inp) {
        if (string.IsNullOrWhiteSpace(inp))
            throw new ProblemParseException(problemName, inp ?? string.Empty, "instance is empty");

        instance = inp;

        string[] rows = inp.Split(RowSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (rows.Length == 0)
            throw new ProblemParseException(problemName, inp, "instance contains no rows");

        int width = -1;
        var parsed = new int[rows.Length][];
        var positions = new Dictionary<int, List<(int x, int y)>>();

        for (int y = 0; y < rows.Length; y++) {
            string[] cells = rows[y].Split(',', StringSplitOptions.TrimEntries);

            if (width < 0) width = cells.Length;
            else if (cells.Length != width)
                throw new ProblemParseException(problemName, inp, "all grid rows must have the same number of cells");

            parsed[y] = new int[width];
            for (int x = 0; x < width; x++) {
                if (!int.TryParse(cells[x], NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                    throw new ProblemParseException(problemName, inp, $"cell ({x},{y}) is not a valid integer: '{cells[x]}'");
                if (v < 0)
                    throw new ProblemParseException(problemName, inp, $"cell ({x},{y}) is negative");

                parsed[y][x] = v;
                if (v == 0) continue;

                if (!positions.TryGetValue(v, out var list)) {
                    list = new List<(int x, int y)>();
                    positions[v] = list;
                }
                list.Add((x, y));
            }
        }

        foreach (var kv in positions) {
            if (kv.Value.Count != 2)
                throw new ProblemParseException(problemName, inp, $"number {kv.Key} must appear exactly twice but appears {kv.Value.Count} time(s)");
        }

        grid = parsed;
        spanX = width;
        spanY = rows.Length;
        numbersPresent = new HashSet<int>(positions.Keys);
        pairIds = positions.Keys.ToArray();
        Array.Sort(pairIds);
        endpoints = new Dictionary<int, (int ax, int ay, int bx, int by)>();
        foreach (var kv in positions) {
            var (a, b) = (kv.Value[0], kv.Value[1]);
            endpoints[kv.Key] = (a.x, a.y, b.x, b.y);
        }
    }

    /*
     * Gets the cell at x, y
     * Cells are row-major, so this is a convenience for x,y ordering
     * Returns -1 on OOB
     */
    public int at(int x, int y) {
        if (!in_bounds(x, y))
            return -1;
        return grid[y][x];
    }

    public bool in_bounds(int x, int y) {
        return (0 <= x && x < spanX) && (0 <= y && y < spanY);
    }
}
