using Amazon.CDK.AWS.IAM;

// ReSharper disable InconsistentNaming
// ReSharper disable VirtualMemberNeverOverridden.Global

namespace Nuages.Deploy.Ecs.Cdk.Stack;

public partial class NuagesCdkStack
{
    private Policy CreateSESRolePolicy(string suffix)
    {
        return new Policy(this, MakeId("SESRole" + suffix), new PolicyProps
        {
            Document = new PolicyDocument(new PolicyDocumentProps
            {
                Statements = new[]
                {
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Effect = Effect.ALLOW,
                        Actions = new[]
                        {
                            "ses:ListTemplates",
                            "ses:SendEmail",
                            "ses:SendTemplatedEmail",
                            "ses:UpdateTemplate",
                            "ses:SendRawEmail",
                            "ses:CreateTemplate",
                            "ses:GetTemplate"
                        },
                        Resources = new[] { "*" }
                    })
                }
            })
        });
    }

    private Policy CreateSnsRolePolicy(string suffix)
    {
        return new Policy(this, MakeId("SNSRole" + suffix), new PolicyProps
        {
            Document = new PolicyDocument(new PolicyDocumentProps
            {
                Statements = new[]
                {
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Effect = Effect.ALLOW,
                        Actions = new[] { "sns:Publish" },
                        Resources = new[] { "*" }
                    })
                }
            })
        });
    }

    private Policy CreateSystemsManagerParameterRolePolicy(string suffix)
    {
        return new Policy(this, MakeId("SystemsManagerParametersRole" + suffix), new PolicyProps
        {
            Document = new PolicyDocument(new PolicyDocumentProps
            {
                Statements = new[]
                {
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Effect = Effect.ALLOW,
                        Actions = new[] { "ssm:GetParametersByPath", "ssm:PutParameter" },
                        Resources = new[] { "*" }
                    })
                }
            })
        });
    }
    
    private Policy CreateSystemsManagerAppConfigRolePolicy(string suffix)
    {
        return new Policy(this, MakeId("SystemsManagerAppConfigRole" + suffix), new PolicyProps
        {
            Document = new PolicyDocument(new PolicyDocumentProps
            {
                Statements = new[]
                {
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Effect = Effect.ALLOW,
                        Actions = new[] {  "appconfig:StartConfigurationSession", "appconfig:GetLatestConfiguration" },
                        Resources = DeploymentOptions.AppConfigResources
                    })
                }
            })
        });
    }

    private Policy CreateSecretsManagerPolicy(string suffix = "")
    {
        return new Policy(this, MakeId("SecretsManagerRole" + suffix), new PolicyProps {
            Document = new PolicyDocument(new PolicyDocumentProps
            {
                Statements = new[]
                {
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Effect = Effect.ALLOW,
                        Actions = new[] { "secretsmanager:GetSecretValue" },
                        Resources = new[] { "*" }
                    })
                }
            })
        });
    }

    private Policy CreateCloudWatchRolePolicy(string suffix)
    {
        return new Policy(this, MakeId("CloudWatch" + suffix), new PolicyProps
        {
            Document = new PolicyDocument(new PolicyDocumentProps
            {
                Statements = new[]
                {
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Effect = Effect.ALLOW,
                        Actions = new[] { 
                            "logs:*"
                        },
                        Resources = new []{ "*"}
                    })
                }
            })
        });
    }
    
    private Policy CreateEventBridgeRolePolicy(string suffix)
    {
        return new Policy(this, MakeId("EventBridge" + suffix), new PolicyProps
        {
            Document = new PolicyDocument(new PolicyDocumentProps
            {
                Statements = new[]
                {
                    new PolicyStatement(new PolicyStatementProps
                    {
                        Effect = Effect.ALLOW,
                        Actions = new[] { 
                            "events:PutEvents"
                        },
                        Resources = new []{ "*"}
                    })
                }
            })
        });
    }

}