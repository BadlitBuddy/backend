namespace Api.Application.Transcripts.Querries;

public class GetTranscriptsValidator : AbstractValidator<GetTranscriptsQuery>
{
    public GetTranscriptsValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page must be greater than 0");
        RuleFor(x => x.Limit).LessThanOrEqualTo(50).WithMessage("Limit must be less  than or equal to 50");
    }
}
