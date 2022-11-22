using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Route53;
using HealthCheck = Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck;
using Protocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.Protocol;

namespace Nuages.Deploy.Ecs.Cdk.Stack;

public partial class NuagesCdkStack
{
    private void CreateEcs()
    {
        var vpc = Vpc.FromLookup(this, "ClusterVpc", new VpcLookupOptions
        {
            VpcId = DeploymentOptions.VpcId
        });

        ISecurityGroup[]? securityGroups = null;

        if (!string.IsNullOrEmpty(DeploymentOptions.SecurityGroupId))
        {
            securityGroups = new[] { SecurityGroup.FromLookupById(this, "WebApiSGDefault", DeploymentOptions.SecurityGroupId!) };
        }

        var cluster = new Cluster(this, $"{StackName}Cluster", new ClusterProps
        {
            Vpc = vpc
        });

        var repository = Repository.FromRepositoryName(this, $"{StackName}ImageRepository", DeploymentOptions.EcrRepositoryName);

        var taskOptions = new ApplicationLoadBalancedTaskImageOptions
        {
            Image = ContainerImage.FromEcrRepository(repository, "latest"),
            ContainerName = ContainerName,
            ContainerPort = 443,
            EnableLogging = true,
            Environment = new Dictionary<string, string>
            {
                {
                    "Nuages__ApplicationConfig__AppConfig__ConfigProfileId", RuntimeOptions.AppConfigProfileId
                },
                {
                    "Nuages__ApplicationConfig__AppConfig__Enabled", DeploymentOptions.AppConfigResources.Any().ToString()
                },
                {
                    "Nuages__UseAWS", "true"
                },
                {
                    "ASPNETCORE_Kestrel__Certificates__Default__Password", RuntimeOptions.CertificatePassword
                },
                {
                    "ASPNETCORE_Kestrel__Certificates__Default__Path", RuntimeOptions.CertificateFilename
                },
                {
                    "ASPNETCORE_URLS", "https://+;http://+"
                }
            }
        };

        var hostedZone = HostedZone.FromLookup(this, "LookupZoneECS", new HostedZoneProviderProps
        {
            DomainName = GetBaseDomain(DeploymentOptions.DomainName)
        });

        // Create a load-balanced Fargate service and make it public
        var service = new ApplicationLoadBalancedFargateService(this, $"{StackName}FargateService",
            new ApplicationLoadBalancedFargateServiceProps
            {
                Cpu = DeploymentOptions.EcsCpu, //.25 .vCPU
                Cluster = cluster,
                DomainName = DeploymentOptions.DomainName,
                DomainZone = hostedZone,
                ListenerPort = 443,
                TargetProtocol = ApplicationProtocol.HTTPS,
                Certificate = Certificate.FromCertificateArn(this, $"{StackName}Cert", DeploymentOptions.CertificateArn),
                DesiredCount = DeploymentOptions.EcsDesiredCount,
                TaskImageOptions = taskOptions,
                MemoryLimitMiB = DeploymentOptions.EcsMemoryLimit,
                PublicLoadBalancer = true,
                SecurityGroups = securityGroups
            }
        );

        service.TargetGroup.ConfigureHealthCheck(new HealthCheck
        {
            Path = "/health",
            Port = "443",
            Interval = Duration.Seconds(30),
            Timeout = Duration.Seconds(5),
            HealthyHttpCodes = "200",
            UnhealthyThresholdCount = 3,
            Protocol = Protocol.HTTPS
        });

        var role = service.TaskDefinition.TaskRole;

        if (DeploymentOptions.ParameterStoreResources.Any())
            role.AttachInlinePolicy(CreateSystemsManagerParameterRolePolicy("Task"));
        
        if (DeploymentOptions.AppConfigResources.Any())
            role.AttachInlinePolicy(CreateSystemsManagerAppConfigRolePolicy("Task"));
        
        if (DeploymentOptions.SesResources.Any())
            role.AttachInlinePolicy(CreateSESRolePolicy("Task"));
        
        if (DeploymentOptions.SnsResources.Any())
            role.AttachInlinePolicy(CreateSnsRolePolicy("Task"));
        
        if (DeploymentOptions.CloudWatchResources.Any())
            role.AttachInlinePolicy(CreateCloudWatchRolePolicy("Task"));
        
        if (DeploymentOptions.SecretResources.Any())
            role.AttachInlinePolicy(CreateSecretsManagerPolicy("Task"));
        
        if (DeploymentOptions.EventBridgeResources.Any())
            role.AttachInlinePolicy(CreateEventBridgeRolePolicy("Task"));

        CreateAdditionalRoles(role);

        service.TaskDefinition.ExecutionRole!.AttachInlinePolicy(new Policy(this, "EcrPolicy", new PolicyProps
        {
            Document = new PolicyDocument(new PolicyDocumentProps
            {
                Statements = new[]
                {
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Effect = Effect.ALLOW,
                        Actions = new[] { "ecr:*" },
                        Resources = new[] { "*" }
                    })
                }
            })
        }));

        CreateBuildToEcr(service.Service);
    }

    static partial void CreateAdditionalRoles(IRole taskRole);
}