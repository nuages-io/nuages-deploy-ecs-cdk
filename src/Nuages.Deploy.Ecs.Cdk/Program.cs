using System.Diagnostics.CodeAnalysis;
using Amazon.CDK;
using Microsoft.Extensions.Configuration;
using Nuages.AWS.Secrets;
using Nuages.Web;
using NuagesCdkStack = Nuages.Deploy.Ecs.Cdk.Stack.NuagesCdkStack;

// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ObjectCreationAsStatement

namespace Nuages.Deploy.Ecs.Cdk;

[ExcludeFromCodeCoverage]
// ReSharper disable once ClassNeverInstantiated.Global
partial class Program
{
    // ReSharper disable once UnusedParameter.Global
    public static void Main(string[] args)
    {
        var configManager = new ConfigurationManager();

        var builder = configManager
            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile("appsettings.deploy.json", false, true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();

        var config = configuration.GetSection("ApplicationConfig").Get<ApplicationConfig>()!;
        
        if (config.ParameterStore.Enabled)
        {
            builder.AddSystemsManager(configureSource =>
            {
                configureSource.Path = config.ParameterStore.Path;
                configureSource.Optional = false;
            });
        }

        if (config.AppConfig.Enabled)
        {
            builder.AddAppConfig(config.AppConfig.ApplicationId,  
                config.AppConfig.EnvironmentId, 
                config.AppConfig.ConfigProfileId,false);
        }

        DoAdditionnalConfiguration(builder);
        
        var secretProvider = new AWSSecretProvider();
        secretProvider.TransformSecrets(configManager);
        
        var app = new App();

        NuagesCdkStack.CreateStack(app, configuration);
        
        app.Synth();
    }
    
    static partial void DoAdditionnalConfiguration(IConfigurationBuilder builder);
}