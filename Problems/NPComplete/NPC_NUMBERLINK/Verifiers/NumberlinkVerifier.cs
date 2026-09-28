using API.Interfaces;

namespace API.Problems.NPComplete.NPC_NUMBERLINK.Verifiers;


class NumberlinkVerifier : IVerifier<NUMBERLINK> {
    public const string CertificateGrammar = "TODO: CertificateGrammar";
    public const string CertificateExample = "TODO: CertificateExample";

    public string verifierName { get; } = "TODO: verifierName";
    public string verifierDefinition { get; } = "TODO: verifierDefinition";
    public string source { get; } = "TODO: source";
    public string certificate { get; } = "1D,2D,2L,2L,3D;\n1D,2D,4D,4L,3D;\n1D,2D,4F,3D,3L;\n1D,2F,3F,3L,5D;\n1R,1F,5F,5L,5L;";
    public string[] contributors { get; } = {
    "Andrija Sevaljevic",
    "Brayden Peck",
    "Charles Johnson"
    };

    public NumberlinkVerifier() { /* ... */ }

    public bool verify(NUMBERLINK problem, string cert) {
        /*
         * Conditions:
         * - No 0s
         * - No nodes w/ 4 alt-color neighbors
         */


        cert = cert.ReplaceLineEndings(string.Empty);
        var rows = cert.Split(";", StringSplitOptions.RemoveEmptyEntries);
        int spanY = rows.Length;

        //v If the row count doesn't match, it's WRONG
        if (spanY != problem.spanY) return false;

        string[][] grid = new string[spanY][];

        for (int y = 0; y < spanY; y++) {
            grid[y] = rows[y].Split(",", StringSplitOptions.RemoveEmptyEntries);

            //v If the row length isn't equal to spanX, WRONG
            if (grid[y].Length != problem.spanX) return false;
        }

        //v Counter for cells traversed
        int ntraced = 0;
        int ntracedTot = problem.spanX * problem.spanY;

        for (int y = 0; y < spanY; y++) {
            for (int x = 0; x < problem.spanX; x++) {
                int color = problem.at(x, y);
                //v Ignore 0
                if (color == 0) continue;
                //v OOB = WRONG
                if (color < 0) return false;

                string cell = grid[y][x];

                //v We don't start from the finish line, DUH
                if (cell.EndsWith("F")) continue;

                //v Temp positions, and cell
                int tx = x;
                int ty = y;
                string tcell = cell;

                while (!tcell.EndsWith("F")) {
                    var suf = tcell[^1];
                    ntraced += 1;

                    //v We use directional codes, F means done.
                    //v Invalid direction is WRONG.
                    //v Since we check for F in the while loop, we do not
                    //v need a dead case statement for it.
                    switch (suf) {
                        case 'R': {
                                tx += 1;
                                break;
                            }
                        case 'U': {
                                ty -= 1;
                                break;
                            }
                        case 'L': {
                                tx -= 1;
                                break;
                            }
                        case 'D': {
                                ty += 1;
                                break;
                            }
                        default: return false;
                    }

                    //v Ignore invalid positions
                    if (!problem.in_bounds(tx, ty)) return false;

                    //v Grab the prior characters to the direction
                    var intStr = tcell[..^1];

                    //v If this isn't an integer or it's the wrong one, WRONG
                    if (!int.TryParse(intStr, out int intVal) || intVal != color) {
                        return false;
                    }

                    //v Onto the next cell!
                    tcell = grid[ty][tx];
                }

                //v We HATE Off by One Errors in this House.
                //v Since we're not using a do-while style loop, we have
                //v to account for this.
                ntraced += 1;

            }
        }

        //v Result is whether we touched all the grid cells or nah.
        return ntraced == ntracedTot;
    }
}
