using Mapster;
using Pango.Application.Models;
using Pango.Domain.Entities;

namespace Pango.Application.Common.Mappings;

public class UserMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PangoUser, PangoUserDto>();
    }
}
