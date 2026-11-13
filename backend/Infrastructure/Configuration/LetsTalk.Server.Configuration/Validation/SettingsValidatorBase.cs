using Microsoft.Extensions.Options;

namespace LetsTalk.Server.Configuration.Validation;

/// <summary>
/// Base class of the settings validators: a validator lists its rules in <see cref="GetErrors"/>, and the
/// failure of the start-up check contains every broken rule at once.
/// </summary>
public abstract class SettingsValidatorBase<TSettings> : IValidateOptions<TSettings>
    where TSettings : class
{
    protected abstract string SectionName { get; }

    /// <summary>Returns the broken rules; null entries (rules that hold) are ignored.</summary>
    protected abstract IEnumerable<string?> GetErrors(TSettings settings);

    protected string Key(string property) => $"{SectionName}:{property}";

    public ValidateOptionsResult Validate(string? name, TSettings options)
    {
        var errors = GetErrors(options).OfType<string>().ToList();
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
