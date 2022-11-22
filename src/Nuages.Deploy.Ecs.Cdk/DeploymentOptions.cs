// ReSharper disable ClassNeverInstantiated.Global

using System.Diagnostics.CodeAnalysis;

namespace Nuages.Deploy.Ecs.Cdk;

[ExcludeFromCodeCoverage]
public class DeploymentOptions
{
    public string StackName { get; set; } = null!;
    public string DomainName { get; set; } = null!;
    public string CertificateArn { get; set; } = null!;
    public string VpcId { get; set; } = null!;
    public string GitHubRepository { get; set; } = null!;
    public string GitHubBranch { get; set; } = null!;
    public string GitHubConnectionArn { get; set; } = null!;
    public string? AdditionalFilesBucketName { get; set; }
    public string? AdditionalFilesZipName { get; set; }
    public string EcrRepositoryName { get; set; } = null!;
    public int EcsCpu { get; set; } = 236;
    public int EcsMemoryLimit { get; set; } = 1024;
    public int EcsDesiredCount { get; set; } = 1;
    public string? SecurityGroupId { get; set; }
  
    public bool TriggerOnPush { get; set; }

    public string[] AppConfigResources { get; set; } = Array.Empty<string>();
    public string[] SesResources { get; set; } = Array.Empty<string>();
    public string[] ParameterStoreResources { get; set; } = Array.Empty<string>();
    public string[] SnsResources { get; set; } = Array.Empty<string>();
    public string[] SecretResources { get; set; } = Array.Empty<string>();
    public string[] CloudWatchResources { get; set; } = Array.Empty<string>();
    public string[] EventBridgeResources { get; set; } = Array.Empty<string>();
}