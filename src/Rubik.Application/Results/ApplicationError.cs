namespace Rubik.Application.Results;

/// <summary>Base of the expected failures a use case reports as values (AD-6). Api maps each subtype to an HTTP status in one place.</summary>
public abstract record ApplicationError;
