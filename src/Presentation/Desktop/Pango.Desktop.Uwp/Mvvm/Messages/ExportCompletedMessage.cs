using CommunityToolkit.Mvvm.Messaging.Messages;
using Pango.Application.UseCases.Data.Commands.Export;

namespace Pango.Desktop.Uwp.Mvvm.Messages;

public class ExportCompletedMessage : ValueChangedMessage<ExportResult>
{
    public ExportCompletedMessage(ExportResult value) : base(value)
    {
    }
}
