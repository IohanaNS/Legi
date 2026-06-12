namespace Legi.Identity.Application.Common.Exceptions;

public class HumanVerificationRequiredException : Exception
{
    public HumanVerificationRequiredException()
        : base("Human verification is required.")
    {
    }
}
