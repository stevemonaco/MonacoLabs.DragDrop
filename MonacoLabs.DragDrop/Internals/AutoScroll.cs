namespace MonacoLabs.DragDrop;

internal static class AutoScroll
{
    public const double MaxStep = 14;

    /// <summary>
    /// The scroll step along one axis of a viewer for a pointer at <paramref name="position"/>: negative towards the start, positive towards the end.
    /// </summary>
    public static double Step(double position, double extent, double margin)
    {
        if (margin <= 0)
            return 0;

        margin = Math.Min(margin, extent / 3);
        if (position < margin)
            return -MaxStep * Math.Clamp((margin - position) / margin, 0, 1);

        if (position > extent - margin)
            return MaxStep * Math.Clamp((position - (extent - margin)) / margin, 0, 1);

        return 0;
    }
}
