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
            VpcId = VpcId
        });

        ISecurityGroup[]? securityGroups = null;

        if (!string.IsNullOrEmpty(SecurityGroupId))
        {
            securityGroups = new[] { SecurityGroup.FromLookupById(this, "WebApiSGDefault", SecurityGroupId!) };
        }

        var cluster = new Cluster(this, $"{StackName}Cluster", new ClusterProps
        {
            Vpc = vpc
        });

        var repository = Repository.FromRepositoryName(this, $"{StackName}ImageRepository", EcrRepositoryName);

        var taskOptions = new ApplicationLoadBalancedTaskImageOptions
        {
            Image = ContainerImage.FromEcrRepository(repository, "latest"),
            ContainerName = ContainerName,
            ContainerPort = 443,
            EnableLogging = true,
            Environment = new Dictionary<string, string>
            {
                {
                    "Nuages__ApplicationConfig__AppConfig__ConfigProfileId", AppConfigProfileId
                },
                {
                    "Nuages__ApplicationConfig__AppConfig__Enabled", AppConfigEnabled
                },
                {
                    "Nuages__UseAWS", "true"
                },
                {
                    "ASPNETCORE_Kestrel__Certificates__Default__Password", CertificatePassword
                },
                {
                    "ASPNETCORE_Kestrel__Certificates__Default__Path", CertificateFilename
                },
                {
                    "ASPNETCORE_URLS", "https://+;http://+"
                }
            }
        };

        var hostedZone = HostedZone.FromLookup(this, "LookupZoneECS", new HostedZoneProviderProps
        {
            DomainName = GetBaseDomain(DomainName)
        });

        // Create a load-balanced Fargate service and make it public
        var service = new ApplicationLoadBalancedFargateService(this, $"{StackName}FargateService",
            new ApplicationLoadBalancedFargateServiceProps
            {
                Cpu = EcsCpu, //.25 .vCPU
                Cluster = cluster,
                DomainName = DomainName,
                DomainZone = hostedZone,
                ListenerPort = 443,
                TargetProtocol = ApplicationProtocol.HTTPS,
                Certificate = Certificate.FromCertificateArn(this, $"{StackName}Cert", CertificateArn),
                DesiredCount = EcsDesiredCount,
                TaskImageOptions = taskOptions,
                MemoryLimitMiB = EcsMemoryLimit,
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

        role.AttachInlinePolicy(CreateSystemsManagerParameterRolePolicy("Task"));
        role.AttachInlinePolicy(CreateSystemsManagerAppConfigRolePolicy("Task"));
        role.AttachInlinePolicy(CreateSESRolePolicy("Task"));
        role.AttachInlinePolicy(CreateSnsRolePolicy("Task"));
        role.AttachInlinePolicy(CreateCloudWatchRolePolicy("Task"));
        role.AttachInlinePolicy(CreateSecretsManagerPolicy("Task"));
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