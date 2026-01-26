using ErrorOr;
using MediatR;

namespace Pango.Application.UseCases.Password.Commands.GeneratePassword
{
    public record GeneratePasswordCommand : IRequest<ErrorOr<string>>
    {
        public GeneratePasswordCommand(int length, bool useUppercase, bool useLowercase, bool useDigits, bool useSpecial, bool excludeAmbiguous)
        {
            Length = length;
            UseUppercase = useUppercase;
            UseLowercase = useLowercase;
            UseDigits = useDigits;
            UseSpecial = useSpecial;
            ExcludeAmbiguous = excludeAmbiguous;
        }
        public int Length { get; set; }
        public bool UseUppercase { get; set; }
        public bool UseLowercase { get; set; }
        public bool UseDigits { get; set; }
        public bool UseSpecial { get; set; }
        public bool ExcludeAmbiguous { get; set; }
    }
}
