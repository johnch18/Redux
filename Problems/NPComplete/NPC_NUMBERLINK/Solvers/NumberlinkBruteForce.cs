using API.Interfaces;

namespace API.Problems.NPComplete.NPC_NUMBERLINK.Solvers;

class NumberlinkBruteForce : ISolver<NUMBERLINK> {
    public string solverName { get; } = "Numberlink Brute-Force";
    public string solverDefinition { get; } = "TODO: solverDefinition";
    public string source { get; } = "TODO: source";
    public string[] contributors { get; } = {
    "Andrija Sevaljevic",
    "Brayden Peck",
    "Charles Johnson"
    };


    public bool timerHasExpired { get; set; }
    public SolverType solverType { get; } = SolverType.BruteForce;

    ///v TODO: complexityBucket
    public SolverComplexityBucket complexityBucket { get; } = SolverComplexityBucket.Unclassified;

    public string complexity { get; } = "TODO: complexity";

    public NumberlinkBruteForce() { }

    public string solve(NUMBERLINK problem) {
        return "TODO: string solve(NUMBERLINK problem)";
    }
}
