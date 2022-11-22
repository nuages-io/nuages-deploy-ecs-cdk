using System.Diagnostics.CodeAnalysis;
using Amazon.CDK.AWS.CodeBuild;
using Amazon.CDK.AWS.CodePipeline;
using Amazon.CDK.AWS.CodePipeline.Actions;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.IAM;
using IStageProps = Amazon.CDK.AWS.CodePipeline.IStageProps;
using StageProps = Amazon.CDK.AWS.CodePipeline.StageProps;

// ReSharper disable ObjectCreationAsStatement

namespace Nuages.Deploy.Ecs.Cdk.Stack;

[SuppressMessage("Performance", "CA1806:Do not ignore method results")]
public partial class NuagesCdkStack
{
    private string ContainerName => $"{StackName}Container";
    
    private string EcrImageDefinitionsZip => $"ecr/{StackName}/imagedefinitions.zip";

    private void CreateBuildToEcr(IBaseService service)
    {
        var repository = new Repository(this, $"{StackName}Repository", new RepositoryProps
        {
            RepositoryName = $"{StackName}Repository"
        });

        // ReSharper disable once ObjectCreationAsStatement
        var project = new Project(this, MakeId($"{StackName}BuildToECR"), new ProjectProps
        {
            ProjectName = MakeId($"{StackName}BuildToECR"),
            Source = Source.GitHub(new GitHubSourceProps
            {
                Owner = DeploymentOptions.GitHubRepository.Split("/").First(),
                Repo = DeploymentOptions.GitHubRepository.Split("/").Last(),
                BranchOrRef = DeploymentOptions.GitHubBranch,
                Webhook = false
            }),
            Environment = new BuildEnvironment
            {
                Privileged = true,
                BuildImage = LinuxBuildImage.STANDARD_4_0,
                EnvironmentVariables = new Dictionary<string, IBuildEnvironmentVariable>
                {
                    {
                        "AWS_DEFAULT_REGION", new BuildEnvironmentVariable
                        {
                            Value = Region
                        }
                    },
                    {
                        "AWS_ACCOUNT_ID", new BuildEnvironmentVariable
                        {
                            Value = Account
                        }
                    },
                    {
                        "IMAGE_REPO_NAME", new BuildEnvironmentVariable
                        {
                            Value =  repository.RepositoryName
                        }
                    },
                    {
                        "IMAGE_TAG", new BuildEnvironmentVariable
                        {
                            Value = "latest"
                        }
                    }
                }
            },
            BuildSpec = BuildSpec.FromObjectToYaml(new Dictionary<string, object>
            {
                {
                    "version", "0.2"
                },
                {
                    "phases", new Dictionary<string, object>
                    {
                        {
                            "pre_build", new Dictionary<string, object>
                            {
                                {
                                    "commands", new[]
                                    {
                                        "echo Logging in to Amazon ECR...",
                                        "echo $AWS_DEFAULT_REGION",
                                        "echo $AWS_ACCOUNT_ID",
                                        "echo $IMAGE_REPO_NAME",
                                        "echo $IMAGE_TAG",
                                        "aws ecr get-login-password --region $AWS_DEFAULT_REGION | docker login --username AWS --password-stdin $AWS_ACCOUNT_ID.dkr.ecr.$AWS_DEFAULT_REGION.amazonaws.com",
                                        "REPOSITORY_URI=$AWS_ACCOUNT_ID.dkr.ecr.$AWS_DEFAULT_REGION.amazonaws.com/$IMAGE_REPO_NAME",
                                        "echo $REPOSITORY_URI",
                                        "echo $CODEBUILD_SRC_DIR_ConfigArtifact",
                                        "cp $CODEBUILD_SRC_DIR_ConfigArtifact/*.* ."
                                    }
                                }
                            }
                        },
                        {
                            "build", new Dictionary<string, object>
                            {
                                {
                                    "commands", new[]
                                    {
                                        "echo Build started on `date`",
                                        "echo Building the Docker image...  ",
                                        "docker build -t $IMAGE_REPO_NAME:$IMAGE_TAG .",
                                        "docker tag $IMAGE_REPO_NAME:$IMAGE_TAG $AWS_ACCOUNT_ID.dkr.ecr.$AWS_DEFAULT_REGION.amazonaws.com/$IMAGE_REPO_NAME:$IMAGE_TAG "
                                    }
                                }
                            }
                        },
                        {
                            "post_build", new Dictionary<string, object>
                            {
                                {
                                    "commands", new[]
                                    {
                                        "echo Build completed on `date`",
                                        "echo Pushing the Docker image...",
                                        "docker push $AWS_ACCOUNT_ID.dkr.ecr.$AWS_DEFAULT_REGION.amazonaws.com/$IMAGE_REPO_NAME:$IMAGE_TAG",
                                        "echo Writing image definitions file...",
                                        "printf '[{\"name\":\"" + ContainerName +
                                        "\",\"imageUri\":\"%s\", \"current\": \"%s\"}]' $AWS_ACCOUNT_ID.dkr.ecr.$AWS_DEFAULT_REGION.amazonaws.com/$IMAGE_REPO_NAME:$IMAGE_TAG \"$(date)\" > imagedefinitions.json"
                                    }
                                }
                            }
                        }
                    }
                },
                {
                    "artifacts", new Dictionary<string, object>
                    {
                        {
                            "files", "imagedefinitions.json"
                        }
                    }
                }
            }),
            Artifacts = Artifacts.S3(new S3ArtifactsProps
            {
                Bucket = BuildBucket,
                IncludeBuildId = false,
                PackageZip = true
            })
        });

        project.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
        {
            Actions = new[]
            {
                "ecr:BatchCheckLayerAvailability",
                "ecr:CompleteLayerUpload",
                "ecr:GetAuthorizationToken",
                "ecr:InitiateLayerUpload",
                "ecr:PutImage",
                "ecr:UploadLayerPart"
            },
            Effect = Effect.ALLOW,
            Resources = new[]
            {
                "*"
            }
        }));

        var sourceArtifact = new Artifact_("SourceArtifact");
        var configArtifact = new Artifact_("ConfigArtifact");
        
        var imageDef = new Artifact_("BuildArtifact");

        var parts = DeploymentOptions.GitHubRepository.Split("/");

        var sourceStageProps = new StageProps
        {
            StageName = "Source",
            Actions = new IAction[]
            {
                new CodeStarConnectionsSourceAction(new CodeStarConnectionsSourceActionProps
                {
                    ConnectionArn = DeploymentOptions.GitHubConnectionArn,
                    Output = sourceArtifact,
                    Owner = parts.First(),
                    Repo = parts.Last(),
                    Branch = DeploymentOptions.GitHubBranch,
                    TriggerOnPush = DeploymentOptions.TriggerOnPush,
                    ActionName = "Source"
                })
            }
        };

        Artifact_[]? extraInputs = null;
        
        if (AdditionalFilesBucket != null)
        {
            sourceStageProps.Actions = sourceStageProps.Actions.Append(new S3SourceAction(new S3SourceActionProps
            {
                Bucket = AdditionalFilesBucket,
                BucketKey = DeploymentOptions.AdditionalFilesZipName!,
                Output = configArtifact,
                Trigger = S3Trigger.EVENTS,
                ActionName = "AdditionalFiles"
            })).ToArray();

            extraInputs = new[]
            {
                configArtifact
            };
        }

        var buildImagePipeline = new Pipeline(this, $"{StackName}ECR", new PipelineProps
        {
            ArtifactBucket = BuildBucket,
            PipelineName = $"{StackName}-Image-To-ECR",
            Stages = new IStageProps[]
            {
                sourceStageProps,
                new StageProps
                {
                    StageName = "Build",
                    Actions = new IAction[]
                    {
                        new CodeBuildAction(new CodeBuildActionProps
                        {
                            Input = sourceArtifact,
                            Project = project,
                            Outputs = new[]
                            {
                                imageDef
                            },
                            ExtraInputs = extraInputs,
                            ActionName = "Build"
                        })
                    }
                },
                new StageProps
                {
                    StageName = "CopyZipToS3",
                    Actions = new IAction[]
                    {
                        new S3DeployAction(new S3DeployActionProps
                        {
                            Bucket = BuildBucket,
                            Input = imageDef,
                            Extract = false,
                            ObjectKey = EcrImageDefinitionsZip,
                            ActionName = "CopyZipToS3"
                        })
                    }
                }
            }
        });
        
        service.Node.AddDependency(buildImagePipeline);

        var deployArtifact = new Artifact_("BuildArtifact");
        var deployPipeline = new Pipeline(this, $"{StackName}ECS", new PipelineProps
        {
            ArtifactBucket = BuildBucket,
            PipelineName = $"{StackName}-DeployTo-ECS",
            Stages = new IStageProps[]
            {
                new StageProps
                {
                    StageName = "Source",
                    Actions = new IAction[]
                    {
                        new S3SourceAction(new S3SourceActionProps
                        {
                            Bucket = BuildBucket,
                            BucketKey = EcrImageDefinitionsZip,
                            Output = deployArtifact,
                            Trigger = S3Trigger.POLL,
                            ActionName = "SourceImageDefinition"
                        })
                    }
                },
                new StageProps
                {
                    StageName = "Deploy",
                    Actions = new IAction[]
                    {
                        new EcsDeployAction(new EcsDeployActionProps
                        {
                            Service = service,
                            Input = deployArtifact,
                            ActionName = "Deploy"
                        })
                    }
                }
            }
        });
        
        deployPipeline.Node.AddDependency(buildImagePipeline);
    }
}