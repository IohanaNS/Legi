namespace Legi.Identity.Application.Common.Email;

/// <summary>
/// A ready-to-send email with both an HTML and a plain-text rendering.
/// Sending both (multipart/alternative) materially improves deliverability.
/// <paramref name="InlineImages"/> are embedded via Content-ID (cid:) so they render
/// without external hosting — referenced in the HTML as &lt;img src="cid:{ContentId}"&gt;.
/// </summary>
public record EmailContent(
    string Subject,
    string HtmlBody,
    string TextBody,
    IReadOnlyList<InlineImage>? InlineImages = null);

/// <summary>An image embedded inline in an email and referenced by its Content-ID.</summary>
public record InlineImage(string ContentId, string MediaType, byte[] Content);
