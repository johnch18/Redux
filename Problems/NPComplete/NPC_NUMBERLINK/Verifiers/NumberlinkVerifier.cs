using API.Interfaces;

namespace API.Problems.NPComplete.NPC_NUMBERLINK.Verifiers;


class NumberlinkVerifier : IVerifier<NUMBERLINK> {
    public const string CertificateGrammar = "TODO: CertificateGrammar";
    public const string CertificateExample = "TODO: CertificateExample";

    public string verifierName { get; } = "TODO: verifierName";
    public string verifierDefinition { get; } = "TODO: verifierDefinition";
    public string source { get; } = "TODO: source";
    public string certificate { get; } = "TODO: certificate";
    public string[] contributors { get; } = {
	"Andrija Sevaljevic",
	"Brayden Peck",
	"Charles Johnson"
    };

    public NumberlinkVerifier() { /* ... */ }

    public bool verify(NUMBERLINK problem, string cert) {
	/// TODO: Verify
	return true;
    }
}
