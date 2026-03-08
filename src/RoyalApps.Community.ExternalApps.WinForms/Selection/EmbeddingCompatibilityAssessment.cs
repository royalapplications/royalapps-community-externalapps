namespace RoyalApps.Community.ExternalApps.WinForms.Selection;

internal sealed class EmbeddingCompatibilityAssessment
{
    public static readonly EmbeddingCompatibilityAssessment Default = new(false, string.Empty);

    public EmbeddingCompatibilityAssessment(bool prefersExternalHosting, string warning)
    {
        PrefersExternalHosting = prefersExternalHosting;
        Warning = warning;
    }

    public bool PrefersExternalHosting { get; }

    public string Warning { get; }
}
