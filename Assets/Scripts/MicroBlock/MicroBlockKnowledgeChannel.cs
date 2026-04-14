/// <summary>
/// How a fact became known; drives credibility rules later (rumor vs eyewitness).
/// </summary>
public enum MicroBlockKnowledgeChannel
{
    /// <summary>Crew assumes place exists from living nearby (facade only).</summary>
    AmbientPresence = 0,

    Seen,
    Heard,
    Read,
    IntelGathering
}
