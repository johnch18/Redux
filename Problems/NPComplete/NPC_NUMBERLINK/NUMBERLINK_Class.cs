using API.Interfaces;
using API.DummyClasses;

using API.Problems.NPComplete.NPC_NUMBERLINK.Solvers;
using API.Problems.NPComplete.NPC_NUMBERLINK.Verifiers;
///v For later
// using API.Problems.NPComplete.NPC_NUMBERLINK.Vizualizations;

using SPADE;

namespace API.Problems.NPComplete.NPC_NUMBERLINK;

class NUMBERLINK : IProblem<NumberlinkBruteForce, NumberlinkVerifier, DummyVisualization> {
    public string problemName { get; } = "Numberlink";
    public string problemLink { get; } = "https://en.wikipedia.org/wiki/Numberlink";

    ///v Want to draft this a bit
    public string formalDefinition { get; } = "TODO: formalDefinition";

    public string problemDefinition { get; } = "Numberlink is a pathing problem where given an MxN grid, some collection of numbered (sometimes colored) node pairs, the goal being to construct orthogonal paths between each pair while also filling the grid.";
    
    public string inputDescription { get; } = "A grid of non-negative numbers, where each non-zero number appears exactly twice. Zero indicates an empty grid cell.";

    public string outputDescription { get; } = "A new grid where all 0 values have been replaced by numbers to reflect the resulting paths.";

    /// We have multiple so I'm holding off for now
    public string source { get; } = "TODO: source";
    
    public string sourceLink { get; } = "TODO: sourceLink";

    public const string InstanceGrammar = "N rows of M comma separated non-negative numbers, 0 representing an empty space.";

    private static readonly string _defaultInstance = "1, 0, 0, 2, 3\n0, 0, 0, 4, 0\n0, 0, 4, 0, 0\n0, 2, 3, 0, 5\n0, 1, 5, 0, 0";

    
    public string defaultInstance { get; } = _defaultInstance;

    public string instanceFormat { get; } = $"Format: {InstanceGrammar} Example: {_defaultInstance}";

    public string certificateFormat { get; } = $"Format: {NumberlinkVerifier.CertificateGrammar} Example: {NumberlinkVerifier.CertificateExample}";

    public string instance { get; set; } = string.Empty;
    public string wikiName { get; } = "Numberlink";

    public NumberlinkBruteForce defaultSolver { get; } = new NumberlinkBruteForce();
    public NumberlinkVerifier defaultVerifier { get; } new NumberlinkVerifier();

    public ComplexityClass complexityClass { get; } = ComplexityClass.NPComplete;
    public ProblemType problemType { get; } = ProblemType.GameAndPuzzles;
    
    public string[] contributors { get; } = {
	"Andrija Sevaljevic",
	"Brayden Peck",
	"Charles Johnson"
    };

    /// TODO: Internal DSA, probably just an enumerated grid + span?

    public NUMBERLINK() : this(_defaultInstance) { /* ... */ }
    
    public NUMBERLINK(string inp) {
	instance = inp;
	/// TODO: Parsing
    }
    
    
   
}
