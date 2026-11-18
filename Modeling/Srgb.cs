namespace Modeling;

// sRGB's transfer curve, between a color as a color picker gives it and the linear light a glTF
// material's factors hold.
internal static class Srgb
{
    public static float Decode(float value) =>
        value <= 0.04045f ? value / 12.92f : MathF.Pow((value + 0.055f) / 1.055f, 2.4f);
}
