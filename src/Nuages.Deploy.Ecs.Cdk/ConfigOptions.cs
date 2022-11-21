// ReSharper disable ClassNeverInstantiated.Global

#nullable disable

using System.Diagnostics.CodeAnalysis;

namespace Nuages.Deploy.Ecs.Cdk;

[ExcludeFromCodeCoverage]
public class ConfigOptions
{
    public string StackName { get; set; }
    public string DomainName { get; set; }
    public string CertificateArn { get; set; } 
    public string VpcId { get; set; }
    public string GitHubRepository { get; set; }
    public string GitHubBranch { get; set; }
    public string GitHubConnectionArn { get; set; }
    public string AdditionalFilesBucketName { get; set; }
    public string AdditionalFilesZipName { get; set; }
    public string EcrRepositoryName { get; set; }
    public int EcsCpu { get; set; } = 236;
    public int EcsMemoryLimit { get; set; } = 1024;
    public int EcsDesiredCount { get; set; } = 1;
    public string SecurityGroupId { get; set; }
    public string AppConfigProfileId { get; set; }
    public bool AppConfigEnabled { get; set; }
    public string CertificateFilename { get; set; }
    public string CertificatePassword { get; set; }
    public bool TriggerOnPush { get; set; }

    public string[] AppConfigResources { get; set; } =  { "*" };

}
