
namespace Nuages.Deploy.Ecs.Cdk;

public class RuntimeOptions
{
    public string? AppConfigProfileId { get; set; }
    public string? AppApplicationId { get; set; }
    public string? AppEnvironmentId { get; set; }
    
    public string? AppParameterStorePath { get; set; }
    
    public string? CertificateFilename { get; set; }
    public string? CertificatePassword { get; set; }
    
}