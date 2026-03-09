using ProjectDora.Core.Abstractions;

namespace ProjectDora.Core.Tests;

public class AbstractionInterfaceTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void CoreAbstractions_AllInterfacesExist()
    {
        var abstractionTypes = new[]
        {
            typeof(IContentService),
            typeof(IContentTypeService),
            typeof(IQueryService),
            typeof(IWorkflowService),
            typeof(IAuthService),
            typeof(IUserService),
            typeof(IRoleService),
            typeof(IAuditService),
        };

        foreach (var type in abstractionTypes)
        {
            Assert.True(type.IsInterface, $"{type.Name} should be an interface");
        }
    }
}
