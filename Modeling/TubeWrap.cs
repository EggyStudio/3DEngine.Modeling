namespace Modeling;

/// <summary>
/// How a tube's texture coordinates carry a picture, how many times round it and how many meters of
/// the tube one repeat runs along it.
/// </summary>
/// <remarks>
/// A bark or a pipe's picture is laid at the density its texels were made for, so a stem whose
/// picture repeats every 0.6 m keeps that along a trunk and a twig alike. A picture's texels stay
/// square where a repeat round the tube is as long as a repeat along it, the circumference over the
/// count round equal to the length along. A tube made with no wrap carries the picture once round
/// and once along every length of its first ring's circumference.
/// </remarks>
/// <param name="Around">How many times the picture goes round the tube, above zero.</param>
/// <param name="Along">
/// How many meters along the tube one repeat of the picture runs, above zero.
/// </param>
public readonly record struct TubeWrap(float Around, float Along);
