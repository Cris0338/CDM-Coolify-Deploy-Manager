// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
// Position Settings + its modeless companion guide on the monitor containing
// the OWNER dashboard, never on the primary display by assumption.
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace CoolifyDeployManager;

internal static class CompanionWindowPlacement
{
    internal sealed record Pair(Rect Settings, Rect Guide);

    internal static Rect WorkAreaFor(Window owner)
    {
        var handle = new WindowInteropHelper(owner).Handle;
        // Screen.FromHandle selects the nearest monitor to the actual Win32
        // owner even when its desktop coordinates are negative.
        var screen = Forms.Screen.FromHandle(handle);
        var pixelWork = screen.WorkingArea;
        var source = PresentationSource.FromVisual(owner);
        var matrix = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var topLeft = matrix.Transform(new Point(pixelWork.Left, pixelWork.Top));
        var bottomRight = matrix.Transform(new Point(pixelWork.Right, pixelWork.Bottom));
        return new Rect(topLeft, bottomRight);
    }

    internal static Pair Arrange(Rect work, Point anchor, Size settings, Size guide,
        double minSettingsWidth, double minSettingsHeight,
        double minGuideWidth, double minGuideHeight)
    {
        if (work.Width <= 0 || work.Height <= 0)
            throw new ArgumentException("A valid monitor work area is required.");
        const double margin = 8;
        const double gap = 14;
        double availableWidth = Math.Max(1, work.Width - 2 * margin);
        double availableHeight = Math.Max(1, work.Height - 2 * margin);

        double setupWidth = Math.Min(settings.Width, Math.Max(minSettingsWidth, availableWidth));
        double setupHeight = Math.Min(settings.Height, Math.Max(minSettingsHeight, availableHeight));
        double guideHeight = Math.Min(guide.Height, Math.Max(minGuideHeight, availableHeight));

        // Shrink the Guide if this lets the pair fit on an ordinary 1366px
        // monitor. Never make the Guide smaller than its declared MinWidth.
        double sideWidth = availableWidth - setupWidth - gap;
        double guideWidth = sideWidth >= minGuideWidth
            ? Math.Min(guide.Width, sideWidth)
            : Math.Min(guide.Width, Math.Max(minGuideWidth, availableWidth));

        static double Clamp(double value, double min, double max) =>
            Math.Max(min, Math.Min(value, Math.Max(min, max)));

        double centerX = double.IsFinite(anchor.X) ? anchor.X : work.Left + work.Width / 2;
        double centerY = double.IsFinite(anchor.Y) ? anchor.Y : work.Top + work.Height / 2;
        double setupX;
        double guideX;
        if (setupWidth + guideWidth + gap <= availableWidth)
        {
            double total = setupWidth + gap + guideWidth;
            setupX = Clamp(centerX - total / 2,
                work.Left + margin, work.Right - margin - total);
            guideX = setupX + setupWidth + gap;
        }
        else
        {
            // Very narrow displays cannot fit both at their minimum sizes.
            // Keep both on the selected monitor; allow repositioning instead
            // of silently moving the Guide to the primary screen.
            setupX = Clamp(centerX - setupWidth / 2,
                work.Left + margin, work.Right - margin - setupWidth);
            guideX = Clamp(setupX + setupWidth - Math.Min(120, guideWidth / 3),
                work.Left + margin, work.Right - margin - guideWidth);
        }

        double setupY = Clamp(centerY - setupHeight / 2,
            work.Top + margin, work.Bottom - margin - setupHeight);
        double guideY = Clamp(centerY - guideHeight / 2,
            work.Top + margin, work.Bottom - margin - guideHeight);

        return new Pair(new Rect(setupX, setupY, setupWidth, setupHeight),
            new Rect(guideX, guideY, guideWidth, guideHeight));
    }

    internal static void VerifyForSmoke()
    {
        var setups = new Size(730, 830);
        var guide = new Size(640, 820);
        var cases = new[]
        {
            // Second monitor located to the RIGHT, and negative-coordinate monitor.
            new Rect(1920, 0, 2560, 1400),
            new Rect(-1600, -120, 1600, 900),
            // Narrow 1366px monitor should squeeze the guide rather than
            // send either window to another monitor.
            new Rect(0, 0, 1366, 768)
        };
        foreach (var work in cases)
        {
            var pair = Arrange(work, new Point(work.Left + work.Width * 0.55,
                    work.Top + work.Height * 0.5),
                setups, guide, 620, 660, 460, 450);
            foreach (var box in new[] { pair.Settings, pair.Guide })
                if (box.Left < work.Left || box.Top < work.Top ||
                    box.Right > work.Right || box.Bottom > work.Bottom)
                    throw new InvalidOperationException(
                        "Companion windows escaped the selected monitor work area.");
            if (pair.Settings.Left < work.Left + 5 ||
                pair.Guide.Left < work.Left + 5)
                throw new InvalidOperationException("Companion window monitor margin regression.");
        }
        var rightSide = Arrange(cases[0], new Point(3500, 700),
            setups, guide, 620, 660, 460, 450);
        if (rightSide.Settings.Left < 1920 || rightSide.Guide.Left < 1920)
            throw new InvalidOperationException("Secondary monitor placement regression.");
        var leftSide = Arrange(cases[1], new Point(-900, 300),
            setups, guide, 620, 660, 460, 450);
        if (leftSide.Settings.Right > 0 || leftSide.Guide.Right > 0)
            throw new InvalidOperationException("Negative-coordinate monitor regression.");
    }
}
