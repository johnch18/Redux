using API.Interfaces;
using API.DummyClasses;

using API.Problems.NPComplete.NPC_NUMBERLINK.Solvers;
using API.Problems.NPComplete.NPC_NUMBERLINK.Verifiers;
//v For later
//v using API.Problems.NPComplete.NPC_NUMBERLINK.Vizualizations;
//v using SPADE;

using System.Collections.Generic;

namespace API.Problems.NPComplete.NPC_NUMBERLINK;

class NUMBERLINK : IProblem<DummySolver, NumberlinkVerifier, DummyVisualization> {
    public string problemName { get; } = "Numberlink";
    public string problemLink { get; } = "https://en.wikipedia.org/wiki/Numberlink";

    //v Want to draft this a bit
    public string formalDefinition { get; } = "TODO: formalDefinition";

    public string problemDefinition { get; } = "Numberlink is a pathing problem where given an MxN grid, some collection of numbered (sometimes colored) node pairs, the goal being to construct orthogonal paths between each pair while also filling the grid.";
    
    public string inputDescription { get; } = "A grid of non-negative numbers, where each non-zero number appears exactly twice. Zero indicates an empty grid cell.";

    public string outputDescription { get; } = "A new grid where all 0 values have been replaced by numbers to reflect the resulting paths.";

    //v We have multiple so I'm holding off for now
    public string source { get; } = "TODO: source";
    
    public string sourceLink { get; } = "TODO: sourceLink";

    public const string InstanceGrammar = "N semicolon separated rows of M comma separated non-negative numbers, 0 representing an empty space.";

    private static readonly string _defaultInstance = "1, 0, 0, 2, 3;\n0, 0, 0, 4, 0;\n0, 0, 4, 0, 0;\n0, 2, 3, 0, 5;\n0, 1, 5, 0, 0";

    
    public string defaultInstance { get; } = _defaultInstance;

    public string instanceFormat { get; } = $"Format: {InstanceGrammar} Example: {_defaultInstance}";

    public string certificateFormat { get; } = $"Format: {NumberlinkVerifier.CertificateGrammar} Example: {NumberlinkVerifier.CertificateExample}";

    public string wikiName { get; } = "Numberlink";

    ///v TODO: Change to real solver
    public DummySolver defaultSolver { get; } = new DummySolver();
    public NumberlinkVerifier defaultVerifier { get; } = new NumberlinkVerifier();
    public DummyVisualization defaultVisualization { get; } = new DummyVisualization();

    public ComplexityClass complexityClass { get; } = ComplexityClass.NPComplete;
    public ProblemType problemType { get; } = ProblemType.GamesAndPuzzles;
    
    public string[] contributors { get; } = {
	"Andrija Sevaljevic",
	"Brayden Peck",
	"Charles Johnson"
    };

    //v TODO: Internal DSA, probably just an enumerated grid + span?

    
    public string instance { get; set; } = string.Empty;

    //v Our input grid of enumerated cells
    public int[][] grid { get; set; }

    //v Spans in each dimension
    public int spanX { get; set; }
    public int spanY { get; set; }

    //v Hash set of our colorings/numberings
    public HashSet<int> numbersPresent { get; set; }

    public NUMBERLINK() : this(_defaultInstance) { /* ... */ }
    
    public NUMBERLINK(string inp) {
	//v I made this based on the SUDOKU problem instance
	instance = inp;
	numbersPresent = new HashSet<int>();
	
	inp = inp.ReplaceLineEndings(string.Empty);
	var rows = inp.Split(";", StringSplitOptions.RemoveEmptyEntries);

	//v Sentinel vqlue to help parse the row lengths
	spanX = -1;
	spanY = rows.Length;
	grid = new int[spanY][];

	Dictionary<int, int> popCounts = new Dictionary<int, int>();

	for (int i = 0; i < spanY; i++) {
	    var nums = rows[i].Split(",", StringSplitOptions.RemoveEmptyEntries);
	    try {
		grid[i] = Array.ConvertAll(nums, int.Parse);
	    } catch (FormatException) {
		throw new InvalidOperationException("Cell entries must be valid integers.");
	    }

	    //v If we have our sentinel value 
	    if (spanX < 0) spanX = grid[i].Length;

	    if (grid[i].Length != spanX) {
		throw new InvalidOperationException("All grid row lengths must be uniform.");
	    }


	    for (int j = 0; j < spanX; j++) {
		int num = grid[i][j];
		//v Ignore 0, it's fine.
		if (num == 0) continue;
		//v We don't want negative numbers.
		if (num < 0) {
		    throw new InvalidOperationException("Grid cells cannot be negative.");
		}
		
		int popNum = 0;
		if (popCounts.ContainsKey(num)) popNum = popCounts[num];
		popCounts[num] = 1 + popNum;
	    }
	}

	//v Validate that non-zero entries don't have counts less than 2
	//v We already check for > 2 in the above loop.
	foreach (int v in popCounts.Values) {
	    if (v < 2)
		throw new InvalidOperationException("Non-zero numbers must occur exactly twice.");
	}
	
    }
}
