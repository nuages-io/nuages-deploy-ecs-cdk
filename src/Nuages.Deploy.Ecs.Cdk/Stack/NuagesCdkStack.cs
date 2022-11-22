using System.Diagnostics.CodeAnalysis;
using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Constructs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;

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
        var deploymentOptions = configuration.GetSection("DeploymentOptions").Get<DeploymentOptions>()!;
        var runtimeOptions = configuration.GetSection("RuntimeOptions").Get<RuntimeOptions>()!;

        if (string.IsNullOrEmpty(deploymentOptions.StackName))
            throw new Exception("StackName must be provided");
        
        var stack = new NuagesCdkStack(scope, deploymentOptions.StackName + "Stack", new StackProps
        {
            StackName = deploymentOptions.StackName,
            Env = new Amazon.CDK.Environment
            {
                Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
            }
        })
        {
            DeploymentOptions = deploymentOptions,
            RuntimeOptions = runtimeOptions,
            // DomainName = configOptions.DomainName,
            // CertificateArn = configOptions.CertificateArn,
            // VpcId = configOptions.VpcId,
            // SecurityGroupId = configOptions.SecurityGroupId,
            // GitHubRepository = configOptions.GitHubRepository,
            // GitHubBranch = configOptions.GitHubBranch,
            // GitHubConnectionArn = configOptions.GitHubConnectionArn,
            // AdditionalFilesBucketName = configOptions.AdditionalFilesBucketName,
            // AdditionalFilesZipName = configOptions.AdditionalFilesZipName,
            // EcsCpu = configOptions.EcsCpu,
            // EcsMemoryLimit = configOptions.EcsMemoryLimit,
            // EcsDesiredCount = configOptions.EcsDesiredCount,
            // Runtime_AppConfigProfileId = runtimeOptions.AppConfigProfileId,
            // Runtime_CertificateFilename = runtimeOptions.CertificateFilename,
            // Runtime_CertificatePassword = runtimeOptions.CertificatePassword,
            // EcrRepositoryName = configOptions.EcrRepositoryName,
            // TriggerOnPush = configOptions.TriggerOnPush,
            // AppConfigResources = configOptions.AppConfigResources,
            // SesResources = configOptions.SesResources,
            // ParameterStoreResources = configOptions.ParameterStoreResources,
            // SnsResources = configOptions.SnsResources,
            // SecretResources = configOptions.SecretResources,
            // CloudWatchResources = configOptions.CloudWatchResources,
            // EventBridgeResources = configOptions.EventBridgeResources
            
        };
        
        stack.CreateTemplate();
    }

    private RuntimeOptions RuntimeOptions { get; set; } = new ();

    private DeploymentOptions DeploymentOptions { get; set; }= new ();

    // private string[] AppConfigResources { get; set; } = Array.Empty<string>();
    // private string[] SesResources { get; set; } = Array.Empty<string>();
    // private string[] ParameterStoreResources { get; set; } = Array.Empty<string>();
    // private string[] SnsResources { get; set; } = Array.Empty<string>();
    // private string[] SecretResources { get; set; } = Array.Empty<string>();
    // private string[] CloudWatchResources { get; set; } = Array.Empty<string>();
    // private string[] EventBridgeResources { get; set; } = Array.Empty<string>();
    
    //private string EcrRepositoryName { get; init; }  = string.Empty;
    //private int EcsDesiredCount { get; init; }
    // private string Runtime_AppConfigProfileId { get; init; } = string.Empty;
    // private string Runtime_CertificateFilename { get; init; } = string.Empty;
    // private string Runtime_CertificatePassword { get; init; } = string.Empty;
    //private string GitHubConnectionArn { get; init; } = string.Empty;
   // private int EcsMemoryLimit { get; init; }
    //private int EcsCpu { get; init; }
    //private string? AdditionalFilesZipName { get; init; }
   // private string? AdditionalFilesBucketName { get; init; }
   // private string CertificateArn { get; init; } = string.Empty;
    //private string DomainName { get; init; } = string.Empty;
    //private string GitHubRepository { get; init; } = string.Empty;
    //private string GitHubBranch { get; init; } = string.Empty;
    private IBucket BuildBucket { get; set; } = null!;
    private IBucket? AdditionalFilesBucket { get; set; }
   // private string? VpcId { get; init; }
    //private string? SecurityGroupId { get; init; }
    //private bool TriggerOnPush { get; set; }
    

    private void CreateTemplate()
    {
        BuildBucket = new Bucket(this, $"{StackName}BuildBucket", new BucketProps
        {
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true,
            Versioned = true
        });
        
        if (!string.IsNullOrEmpty(DeploymentOptions.AdditionalFilesBucketName))
        {
            AdditionalFilesBucket = Bucket.FromBucketName(this, $"{StackName}AddtionalFilesBucket", DeploymentOptions.AdditionalFilesBucketName);
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