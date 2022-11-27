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
        var deploymentOptions = configuration.GetSection("DeploymentOptions").Get<DeploymentOptions>()!;
        var runtimeOptions = configuration.GetSection("RuntimeOptions").Get<RuntimeOptions>()!;
        var sourceOptions = configuration.GetSection("SourceOptions").Get<SourceOptions>()!;
        
        var stackName = configuration.GetValue<string>("StackName");
        
        if (string.IsNullOrEmpty(stackName))
            throw new Exception("StackName must be provided");
        
        var stack = new NuagesCdkStack(scope, stackName + "Stack", new StackProps
        {
            StackName = stackName,
            Env = new Amazon.CDK.Environment
            {
                Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
            }
        })
        {
            DeploymentOptions = deploymentOptions,
            RuntimeOptions = runtimeOptions,
            SourceOptions = sourceOptions
        };
        
        stack.CreateTemplate();
    }

    private RuntimeOptions RuntimeOptions { get; set; } = new ();
    private DeploymentOptions DeploymentOptions { get; set; } = new ();
    private SourceOptions SourceOptions { get; set; } = new ();
    
    private IBucket BuildBucket { get; set; } = null!;
    private IBucket? AdditionalFilesBucket { get; set; }

    private void CreateTemplate()
    {
        BuildBucket = new Bucket(this, $"{StackName}BuildBucket", new BucketProps
        {
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true,
            Versioned = true
        });
        
        if (!string.IsNullOrEmpty(DeploymentOptions.CertificateBucketName))
        {
            AdditionalFilesBucket = Bucket.FromBucketName(this, $"{StackName}CertificateBucket", DeploymentOptions.CertificateBucketName);
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