using Mapster;

namespace Pango.Desktop.Uwp.Common.Mappings;

public class ExampleClassNameMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ExampleClassName, ExampleClassNameDto>()
            .Map(dest => dest.PropertyExample, src => src.ExampleProperty);
    }

    public class ExampleClassName
    {
        public int ExampleProperty { get; set; }
    }

    public class ExampleClassNameDto
    {
        public int PropertyExample { get; set; }
    }
}
