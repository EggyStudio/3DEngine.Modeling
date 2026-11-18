namespace Modeling;

/// <summary>
/// How a tube's texture coordinates carry a picture, how many times round it and how many meters of
/// the tube one repeat runs along it.
/// </summary>
/// <remarks>
/// <para>
/// A bark or a pipe's picture is laid at the density its texels were made for, so a stem whose
/// picture repeats every 0.6 m keeps that along a trunk and a twig alike. A picture's texels stay
/// square where a repeat round the tube is as long as a repeat along it, the circumference over the
/// count round equal to the length along. A tube made with no wrap carries the picture once round
/// and once along every length of its first ring's circumference.
/// </para>
/// <para>
/// A tube that tapers keeps its count round, so its texels grow narrower round it as it narrows, a
/// stem from 0.3 m to 0.05 m holding a sixth of their shape at its tip. Where the wrap follows the
/// radius, each segment's repeat along is as much shorter than <see cref="Along"/> as its mean
/// radius is than the first ring's, so the texels keep the shape they have at the base the whole
/// way and a picture's features grow finer toward the tip, as a bark's do.
/// </para>
/// </remarks>
/// <param name="Around">How many times the picture goes round the tube, above zero.</param>
/// <param name="Along">
/// How many meters along the tube one repeat of the picture runs at its first ring, above zero.
/// </param>
/// <param name="FollowsRadius">
/// Whether a repeat along grows shorter as the tube narrows, in step with its radius.
/// </param>
public readonly record struct TubeWrap(float Around, float Along, bool FollowsRadius = false);
