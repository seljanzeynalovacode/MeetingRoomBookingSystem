using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using MRBSystem.Application.Mapper;

namespace MRBSystem.Testing;

public class AutoMapperConfigurationTests
{
    [Fact]
    public void CustomProfile_Configuration_IsValid()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<CustomProfile>(),
            NullLoggerFactory.Instance);

        config.AssertConfigurationIsValid();
    }
}
