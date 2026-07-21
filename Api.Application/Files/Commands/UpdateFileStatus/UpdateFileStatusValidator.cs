namespace Api.Application.Files.Commands.UpdateFileStatus;

public class UpdateFileStatusValidator : AbstractValidator<UpdateFileStatusCommand>
{
    public UpdateFileStatusValidator()
    {
        RuleFor(x => x.UnprocessedObjectKey)
            .NotEmpty().WithMessage("The Unprocessed Object Key is required");
        RuleFor(x => x.TranscriptionJobStatus)
            .IsInEnumWithValues();
    }
}
