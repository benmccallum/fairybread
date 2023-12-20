﻿namespace FairyBread;

internal static class WellKnownContextData
{
    public const string Prefix = "FairyBread.";

    public const string DontValidate =
        Prefix + "DontValidate";

    public const string DontValidateImplicitly =
        Prefix + "DontValidateImplicitly";

    public const string ExplicitValidatorTypes =
        Prefix + "ExplicitValidatorTypes";

    public const string ValidatorDescriptors =
        Prefix + "Validators";

    public const string UsesInlineErrors =
        Prefix + "UsesInlineErrors";

    public const string UsesGlobalErrors =
        Prefix + "UsesGlobalErrors";
}
