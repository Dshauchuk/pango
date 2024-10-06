using CommunityToolkit.Mvvm.Messaging.Messages;
using Pango.Application.UseCases.Data.Commands.Import;

namespace Pango.Desktop.Uwp.Mvvm.Messages;

public class ImportCompletedMessage : ValueChangedMessage<ImportResult>
{
    public ImportCompletedMessage(ImportResult value) : base(value)
    {
    }
}
