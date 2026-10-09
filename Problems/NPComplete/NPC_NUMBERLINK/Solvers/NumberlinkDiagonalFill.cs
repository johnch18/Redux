// C# port of the solver in https://github.com/thomasahle/numberlink (paper.go)
// Original work Copyright (C) Thomas Dybdahl Ahle, licensed under the GNU Affero GPL v3.
// This port is a derivative work and is therefore also subject to AGPL-3.0.

using API.Interfaces;
using System;
using System.Collections.Generic;

namespace API.Problems.NPComplete.NPC_NUMBERLINK.Solvers;

class NumberlinkAhleDiagonal : ISolver<NUMBERLINK> {
    public string solverName { get; } = "Ahle Diagonal Fill with Corner Heuristics";
    public string solverDefinition { get; } = "Fills the paper cell by cell along SW-diagonals starting at the "
    + "top-left corner, choosing connections (N/E/S/W) for each cell. Partial links are tracked with an "
    + "end[] array (the other end of each link head), which prevents loops and joining different "
    + "sources. A corner-dual heuristic (SW/SE corners must form spikes rooted at sources) prunes "
    + "illegal self-touching states early, and a final validation pass rejects the remaining false positives. "
    + "Assumes the solution covers every cell and no link touches itself.";
    public string source { get; } = "Ahle, Thomas Dybdahl. Numberlink (Go implementation, 2012).";
    public string sourceLink { get; } = "https://github.com/thomasahle/numberlink";
    public string[] contributors { get; } = {
        "Andrija Sevaljevic"
    };

    public bool timerHasExpired { get; set; }
    public SolverType solverType { get; } = SolverType.Backtracking;
    public SolverComplexityBucket complexityBucket { get; } = SolverComplexityBucket.Exponential;
    public string complexity { get; } = "Exponential worst case (NP-complete); heavily pruned backtracking";

    /// <summary>Number of recursive calls in the last solve (for benchmarking).</summary>
    public long Calls { get; private set; }

    private const int GRASS = -1;
    private const int EMPTY = 0;
    private const int N = 1, E = 2, S = 4, W = 8;

    private static readonly int[] Dirs = { N, E, S, W };
    private static readonly int[] Mir = new int[16];
    private static readonly bool[] Diag = new bool[16];

    static NumberlinkAhleDiagonal() {
        Mir[N] = S; Mir[E] = W; Mir[S] = N; Mir[W] = E;
        Diag[N | E] = true; Diag[N | W] = true; Diag[S | E] = true; Diag[S | W] = true;
    }

    private int _w, _h;
    private readonly int[] _vctr = new int[16];
    private readonly int[] _crnr = new int[16];
    private int[] _table = Array.Empty<int>();
    private int[] _con = Array.Empty<int>();
    private bool[] _source = Array.Empty<bool>();
    private int[] _end = Array.Empty<int>();
    private bool[] _canSE = Array.Empty<bool>();
    private bool[] _canSW = Array.Empty<bool>();
    private int[] _next = Array.Empty<int>();

    public string solve(NUMBERLINK problem) {
        const string fail = "No Valid Solution Found.";
        int w0 = problem.spanX, h0 = problem.spanY;
        var cells = new int[w0 * h0];
        var counts = new Dictionary<int, int>();
        for (int y = 0; y < h0; y++)
            for (int x = 0; x < w0; x++) {
                int v = problem.grid[y][x];
                cells[y * w0 + x] = v;
                if (v != 0) counts[v] = counts.GetValueOrDefault(v) + 1;
            }
        foreach (int c in counts.Values) if (c != 2) return fail;

        Init(w0, h0, cells);
        Calls = 0;

        string certificate = Choose(_crnr[N | W]) ? Render(w0, h0) : fail;
        if(problem.defaultVerifier.verify(problem, certificate)) return certificate;
        return fail;
    }

    private void Init(int w0, int h0, int[] cells) {
        int w = w0 + 2, h = h0 + 2;
        _w = w; _h = h;

        // Pad with GRASS so boundary checks are unnecessary.
        _table = new int[w * h];
        Array.Fill(_table, GRASS);
        for (int y = 0; y < h0; y++)
            for (int x = 0; x < w0; x++)
                _table[(y + 1) * w + (x + 1)] = cells[y * w0 + x];

        for (int dir = 0; dir < 16; dir++) {
            _vctr[dir] = 0;
            if ((dir & N) != 0) _vctr[dir] -= w;
            if ((dir & E) != 0) _vctr[dir] += 1;
            if ((dir & S) != 0) _vctr[dir] += w;
            if ((dir & W) != 0) _vctr[dir] -= 1;
        }

        _crnr[N | W] = w + 1;
        _crnr[N | E] = 2 * w - 2;
        _crnr[S | E] = h * w - w - 2;
        _crnr[S | W] = h * w - 2 * w + 1;

        _source = new bool[w * h];
        for (int p = 0; p < w * h; p++) _source[p] = _table[p] != EMPTY && _table[p] != GRASS;

        // Pivot tables: cells on a diagonal ray from a source.
        _canSE = new bool[w * h];
        _canSW = new bool[w * h];
        for (int pos = 0; pos < w * h; pos++) {
            if (!_source[pos]) continue;
            int d = _vctr[N | W];
            for (int p = pos + d; _table[p] == EMPTY; p += d) _canSE[p] = true;
            d = _vctr[N | E];
            for (int p = pos + d; _table[p] == EMPTY; p += d) _canSW[p] = true;
        }

        // Diagonal visiting order: next[last] = pos.
        _next = new int[w * h];
        int last = 0;
        var starts = new List<int>();
        for (int i = _crnr[N | W]; i < _crnr[N | E]; i += 1) starts.Add(i);
        for (int i = _crnr[N | E]; i < _crnr[S | E] + 1; i += w) starts.Add(i);
        foreach (int start in starts) {
            int pos = start;
            while (_table[pos] != GRASS) {
                _next[last] = pos;
                last = pos;
                pos += w - 1;
            }
        }

        _end = new int[w * h];
        for (int p = 0; p < w * h; p++) _end[p] = p;
        _con = new int[w * h];
    }

    private bool Choose(int pos) {
        Calls++;
        if (pos == 0) return Validate();

        int w = _w;
        if (_source[pos]) {
            switch (_con[pos]) {
                case 0:
                    // Can't connect E if we have a NE corner.
                    if (_con[pos - w + 1] != (S | W) && Try(pos, E)) return true;
                    // South connections can create a forced SE position.
                    if (CheckImplicitSE(pos) && Try(pos, S)) return true;
                    break;
                case N:
                case W:
                    return Choose(_next[pos]);
            }
            return false;
        }

        switch (_con[pos]) {
            case 0: // SE
                if (_canSE[pos]) return Try(pos, E | S);
                break;
            case W: // SW or WE
                if (_canSW[pos] && CheckSWLane(pos) && CheckImplicitSE(pos) && Try(pos, S)) return true;
                if (_con[pos - w + 1] != (S | W) && _con[pos - w - 1] != (S | E))
                    return Try(pos, E);
                break;
            case N | W: // NW
                if (_con[pos - w - 1] == (N | W) || _source[pos - w - 1])
                    return Choose(_next[pos]);
                break;
            case N: // NE or NS
                if (_con[pos - w + 1] == (N | E) ||
                    (_source[pos - w + 1] && (_con[pos - w + 1] & (N | E)) != 0)) {
                    if (Try(pos, E)) return true;
                }
                if (_con[pos - w + 1] != (S | W) && _con[pos - w - 1] != (S | E) && CheckImplicitSE(pos))
                    return Try(pos, S);
                break;
        }
        return false;
    }

    // A SW line of corners starting at pos must not intersect a SE or NW line.
    private bool CheckSWLane(int pos) {
        for (; !_source[pos]; pos += _w - 1)
            if (_con[pos] != W) return false;
        return true;
    }

    // A south connection at pos must not create a forced, illegal SE corner at pos+1.
    private bool CheckImplicitSE(int pos) =>
        _con[pos + 1] != 0 || _canSE[pos + 1] || _table[pos + 1] != EMPTY;

    private bool Try(int pos1, int dirs) {
        int dir = dirs & -dirs; // lowest set bit
        int pos2 = pos1 + _vctr[dir];
        int end1 = _end[pos1], end2 = _end[pos2];

        if (_table[pos2] == GRASS) return false;

        // Different sources must not be connected.
        if (_table[end1] != EMPTY && _table[end2] != EMPTY && _table[end1] != _table[end2])
            return false;

        // No loops.
        if (end1 == pos2 && end2 == pos1) return false;

        // No tight corners (optimization).
        if (_con[pos1] != 0) {
            int dir2 = _con[pos1 + _vctr[_con[pos1]]];
            int dir3 = _con[pos1] | dir;
            if (Diag[dir2] && Diag[dir3] && (dir2 & dir3) != 0) return false;
        }

        int old1 = _con[pos1], old2 = _con[pos2];
        _con[pos1] |= dir;
        _con[pos2] |= Mir[dir];

        int old3 = _end[end1], old4 = _end[end2];
        _end[end1] = end2;
        _end[end2] = end1;

        int rest = dirs & ~dir;
        bool res = rest == 0 ? Choose(_next[pos1]) : Try(pos1, rest);

        // Restore state unless a solution was found.
        if (!res) {
            _con[pos1] = old1;
            _con[pos2] = old2;
            _end[end1] = old3;
            _end[end2] = old4;
        }
        return res;
    }

    // The search can be tricked into some self-touching links; filter them here.
    private bool Validate() {
        int total = _w * _h;
        var vtable = new int[total];
        for (int pos = 0; pos < total; pos++) {
            if (!_source[pos]) continue;
            int alpha = _table[pos];
            int p = pos, old = pos, next = pos;
            while (true) {
                vtable[p] = alpha;
                foreach (int dir in Dirs) {
                    int cand = p + _vctr[dir];
                    if ((_con[p] & dir) != 0) {
                        if (cand != old) next = cand;
                    } else if (vtable[cand] == alpha) {
                        return false; // not connected but same colour: self-touch
                    }
                }
                if (old != p && _source[p]) break;
                old = p;
                p = next;
            }
        }
        return true;
    }

    private string Render(int w0, int h0) {
        int total = _w * _h;
        // Each cell becomes "<colour><dir>": dir points to the next cell along the path,
        // walking from the first endpoint (row-major) of each colour; F marks the finish.
        var outv = new string[total];
        var seen = new HashSet<int>();
        for (int pos = 0; pos < total; pos++) {
            if (!_source[pos] || !seen.Add(_table[pos])) continue;
            int color = _table[pos];
            int p = pos, old = -1;
            while (true) {
                int next = -1;
                char d = 'F';
                foreach (int dir in Dirs) {
                    if ((_con[p] & dir) == 0) continue;
                    int cand = p + _vctr[dir];
                    if (cand == old) continue;
                    next = cand;
                    d = dir == N ? 'U' : dir == E ? 'R' : dir == S ? 'D' : 'L';
                }
                outv[p] = color + d.ToString();
                if (next == -1) break;
                old = p;
                p = next;
            }
        }

        var rows = new List<string>();
        for (int y = 0; y < h0; y++) {
            var cells = new List<string>();
            for (int x = 0; x < w0; x++) cells.Add(outv[(y + 1) * _w + (x + 1)]);
            rows.Add(string.Join(",", cells) + ";");
        }
        return string.Join("\n", rows);
    }
}