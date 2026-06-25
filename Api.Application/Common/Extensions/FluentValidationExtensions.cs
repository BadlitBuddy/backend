namespace Api.Application.Common.Extensions;

public static class FluentValidationExtensions
{
    public static IRuleBuilderOptions<T, TProperty> IsInEnumWithValues<T, TProperty>(
        this IRuleBuilder<T, TProperty> ruleBuilder) where TProperty : struct, Enum
    {
        return ruleBuilder.IsInEnum().WithMessage(_ =>
        {
            var allowedValues = Enum.GetValues<TProperty>()
                .Select(e => $"{Convert.ToInt32(e)} = {e}");

            return $"Status must be one of: {string.Join(", ", allowedValues)}.";
        });
    }
}