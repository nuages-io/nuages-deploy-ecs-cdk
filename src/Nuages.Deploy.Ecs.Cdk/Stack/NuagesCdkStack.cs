using System.Diagnostics.CodeAnalysis;
using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Constructs;
using Microsoft.Extensions.Configuration;

// ReSharper disable ObjectCreationAsStatement

namespace Nuages.Deploy.Ecs.Cdk.Stack;

[ExcludeFromCodeCoverage]
public partial class NuagesCdkStack : Amazon.CDK.Stack
{
    // ReSharper disable once MemberCanBeProtected.Global
    private NuagesCdkStack(Construct scope, string id, IStackProps props) : base(scope, id, props)
    {
    }

    public static void CreateStack(Construct scope, IConfiguration configuration)
    {
        var options = configuration.Get<ConfigOptions>()!;
        
        var stack = new NuagesCdkStack(scope, options.StackName + "Stack", new StackProps
        {
            StackName = options.StackName,
            Env = new Amazon.CDK.Environment
            {
                Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
            }
        })
        {
            DomainName = options.DomainName,
            CertificateArn = options.CertificateArn,
            VpcId = options.VpcId,
            SecurityGroupId = options.SecurityGroupId,
            GitHubRepository = options.GitHubRepository,
            GitHubBranch = options.GitHubBranch,
            GitHubConnectionArn = options.GitHubConnectionArn,
            AdditionalFilesBucketName = options.AdditionalFilesBucketName,
            AdditionalFilesZipName = options.AdditionalFilesZipName,
            EcsCpu = options.EcsCpu,
            EcsMemoryLimit = options.EcsMemoryLimit,
            EcsDesiredCount = options.EcsDesiredCount,
            AppConfigProfileId = options.AppConfigProfileId,
            AppConfigEnabled = options.AppConfigEnabled.ToString().ToLower(),
            CertificateFilename = options.CertificateFilename,
            CertificatePassword = options.CertificatePassword,
            EcrRepositoryName = options.EcrRepositoryName,
            TriggerOnPush = options.TriggerOnPush,
            AppConfigResources = options.AppConfigResources
        };
        
        stack.CreateTemplate();
    }

    public string[] AppConfigResources { get; set; }


    private string EcrRepositoryName { get; init; }  = string.Empty;
    private int EcsDesiredCount { get; init; }
    private string AppConfigProfileId { get; init; } = string.Empty;
    private string AppConfigEnabled { get; init; } = string.Empty;
    private string CertificateFilename { get; init; } = string.Empty;
    private string CertificatePassword { get; init; } = string.Empty;
    private string GitHubConnectionArn { get; init; } = string.Empty;
    private int EcsMemoryLimit { get; init; }
    private int EcsCpu { get; init; }
    private string? AdditionalFilesZipName { get; init; }
    private string? AdditionalFilesBucketName { get; init; }
    private string CertificateArn { get; init; } = string.Empty;
    private string DomainName { get; init; } = string.Empty;
    private string GitHubRepository { get; init; } = string.Empty;
    private string GitHubBranch { get; init; } = string.Empty;
    private IBucket BuildBucket { get; set; } = null!;
    private IBucket? AdditionalFilesBucket { get; set; }
    private string? VpcId { get; init; }
    private string? SecurityGroupId { get; init; }
    private bool TriggerOnPush { get; set; }
    

    private void CreateTemplate()
    {
        BuildBucket = new Bucket(this, $"{StackName}BuildBucket", new BucketProps
        {
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true,
            Versioned = true
        });
        
        if (!string.IsNullOrEmpty(AdditionalFilesBucketName))
        {
            AdditionalFilesBucket = Bucket.FromBucketName(this, $"{StackName}AddtionalFilesBucket", AdditionalFilesBucketName);
        }

        CreateEcs();
    }

    private static string GetBaseDomain(string domainName)
    {
        var tokens = domainName.Split('.');

        if (tokens.Length != 3)
            return domainName;

        var tok = new List<string>(tokens);
        var remove = tokens.Length - 2;
        tok.RemoveRange(0, remove);

        return tok[0] + "." + tok[1];
    }
    
    private string MakeId(string id)
    {
        return $"{StackName}-{id}";
    }
}