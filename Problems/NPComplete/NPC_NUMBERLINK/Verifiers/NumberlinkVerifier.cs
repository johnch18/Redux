using System.Globalization;
using System.Text.Json.Serialization;
using API.Interfaces;

namespace API.Problems.NPComplete.NPC_NUMBERLINK.Verifiers;

class NumberlinkVerifier : IVerifier<NUMBERLINK> {

    public const string CertificateGrammar = "Same layout as the instance (rows separated by semicolons or newlines, cells by commas). Every cell is its path number followed by one letter: R, L, U or D (the direction of the next cell on that path) or F (this cell is the finishing endpoint of the path). Every cell must be filled.";

    public const string CertificateExample = "1D,2D,2L,2L,3D;\n1D,2D,4D,4L,3D;\n1D,2D,4F,3D,3L;\n1D,2F,3F,3L,5D;\n1R,1F,5F,5L,5L;";

    // --- Fields ---
    public string verifierName { get; } = "Numberlink Path Verifier";
    public string verifierDefinition { get; } = "For each numbered pair, one endpoint (the one not marked F) is the start. Follow the direction letters from the start; the walk must stay inside the grid, never revisit a cell, only use cells labelled with the pair's number, and end on the other endpoint marked F. Given numbers must be preserved. Finally the total number of cells covered by all paths must equal the number of cells in the grid.";
    public string source { get; } = "Adcock, A., et al. (2015). Zig-Zag Numberlink is NP-Complete. Journal of Information Processing, 23(3), 239-245.";
    public string sourceLink { get; } = "https://arxiv.org/abs/1410.5845";
    public string[] contributors { get; } = { "Andrija Sevaljevic", "Brayden Peck", "Charles Johnson" };
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? complexity { get; set; } = "O(R*C)";
    private string _certificate = "";

    public string certificate {
        get {
            return _certificate;
        }
    }

    private static readonly char[] RowSeparators = { ';', '\n', '\r' };

    // --- Methods Including Constructors ---
    public NumberlinkVerifier() { }

    public bool verify(NUMBERLINK problem, string certificate) {
        _certificate = certificate;
        ParseCertificate(problem, certificate, out int[,] ids, out char[,] dirs);

        int w = problem.spanX, h = problem.spanY;

        // Every cell filled, and the given numbers preserved.
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                if (ids[y, x] == 0) return false;
                int given = problem.grid[y][x];
                if (given != 0 && ids[y, x] != given) return false;
            }
        }

        var visited = new bool[h, w];
        int covered = 0;

        foreach (int id in problem.pairIds) {
            var (ax, ay, bx, by) = problem.endpoints[id];

            // Exactly one endpoint is the finish; the other is the start.
            bool aFinish = dirs[ay, ax] == 'F';
            bool bFinish = dirs[by, bx] == 'F';
            if (aFinish == bFinish) return false;

            (int sx, int sy, int fx, int fy) = aFinish ? (bx, by, ax, ay) : (ax, ay, bx, by);

            int cx = sx, cy = sy;
            while (true) {
                if (visited[cy, cx] || ids[cy, cx] != id) return false;
                visited[cy, cx] = true;
                covered++;

                char d = dirs[cy, cx];
                if (d == 'F') {
                    if (cx != fx || cy != fy) return false;
                    break;
                }

                int dir = NumberlinkCertificate.Letters.IndexOf(d);
                cx += NumberlinkCertificate.Dx[dir];
                cy += NumberlinkCertificate.Dy[dir];
                if (!problem.in_bounds(cx, cy)) return false;
            }
        }

        return covered == w * h;
    }

    private static void ParseCertificate(NUMBERLINK problem, string certificate, out int[,] ids, out char[,] dirs) {
        if (string.IsNullOrWhiteSpace(certificate))
            throw new CertificateParseException(problem, certificate ?? string.Empty, "certificate is empty");

        int w = problem.spanX, h = problem.spanY;
        string[] rows = certificate.Split(RowSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (rows.Length != h)
            throw new CertificateParseException(problem, certificate, $"expected {h} rows but found {rows.Length}");

        ids = new int[h, w];
        dirs = new char[h, w];

        for (int y = 0; y < h; y++) {
            string[] cells = rows[y].Split(',', StringSplitOptions.TrimEntries);
            if (cells.Length != w)
                throw new CertificateParseException(problem, certificate, $"row {y} has {cells.Length} cells but {w} were expected");

            for (int x = 0; x < w; x++) {
                string token = cells[x];
                string expected = $"cell ({x},{y}) '{token}' must be a path number followed by one of R, L, U, D, F";

                if (token.Length < 2)
                    throw new CertificateParseException(problem, certificate, expected);

                char letter = char.ToUpperInvariant(token[^1]);
                if ("RLUDF".IndexOf(letter) < 0)
                    throw new CertificateParseException(problem, certificate, expected);

                if (!int.TryParse(token.AsSpan(0, token.Length - 1), NumberStyles.None, CultureInfo.InvariantCulture, out int id))
                    throw new CertificateParseException(problem, certificate, expected);

                ids[y, x] = id;
                dirs[y, x] = letter;
            }
        }
    }
}
