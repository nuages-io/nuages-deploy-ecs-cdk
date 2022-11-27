namespace Nuages.Deploy.Ecs.Cdk;

public class SourceOptions
{
    public string GitHubRepository { get; set; } = null!;
    public string GitHubBranch { get; set; } = null!;
    public string GitHubConnectionArn { get; set; } = null!;
    public string GitHubPersonnalAccessTokenSecretName { get; set; } = null!;
}