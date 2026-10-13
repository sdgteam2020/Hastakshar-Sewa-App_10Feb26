namespace SignService.Enums
{
    public enum DocumentValidationResult
    {
        Valid,
        FileNotFound,
        ZeroByte,
        TooLarge,
        PasswordProtected,
        Corrupted,
        Unsupported
    }
}