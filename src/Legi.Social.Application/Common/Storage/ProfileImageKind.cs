namespace Legi.Social.Application.Common.Storage;

/// <summary>
/// Which profile image is being uploaded. Drives target dimensions and the
/// storage key layout (avatars/… vs banners/…).
/// </summary>
public enum ProfileImageKind
{
    Avatar,
    Banner
}
