// Copyright (c) 2026 Cris0338. Licensed under the CDM Free Redistribution License v1.0.
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CoolifyDeployManager;

/// <summary>
/// Three-second logo reveal. Rendering uses the GPU-backed WPF visual tree;
/// network startup happens only after the splash yields to MainWindow.
/// </summary>
public partial class SplashWindow : Window
{
    private readonly Stopwatch watch = new();
    private bool burstTriggered;
    private bool fading;
    private double lastX = 450;
    private double lastY = 265;

    public SplashWindow()
    {
        InitializeComponent();
        try
        {
            var resource = new Uri("pack://application:,,,/Assets/CDMLogo.png", UriKind.Absolute);
            LogoArtwork.Source = BitmapFrame.Create(resource,
                BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        }
        catch (Exception)
        {
            // The local development build also works before the approved logo
            // asset is installed; the Windows build script explains how to add it.
            FallbackLogo.Visibility = Visibility.Visible;
        }
        Loaded += (_, _) =>
        {
            watch.Start();
            CompositionTarget.Rendering += OnRendering;
        };
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        double seconds = watch.Elapsed.TotalSeconds;
        if (!burstTriggered && seconds >= 2.25)
        {
            burstTriggered = true;
            Burst();
        }
        if (!fading && seconds >= 2.64)
        {
            fading = true;
            BeginAnimation(OpacityProperty,
                new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(360))
                { FillBehavior = FillBehavior.HoldEnd });
        }
        if (burstTriggered) return;

        // Elliptical orbit, roughly twice around the official logo.
        // On the upper half the electron is rendered behind the artwork;
        // on the lower half it crosses IN FRONT of the letters.
        double turns = seconds < 1.35
            ? seconds * 0.62
            : 1.35 * 0.62 + (seconds - 1.35) * 1.13;
        double angle = -Math.PI / 2 + turns * Math.PI * 2;
        double x = 450 + 348 * Math.Cos(angle);
        double y = 265 + 142 * Math.Sin(angle);
        lastX = x;
        lastY = y;
        bool foreground = Math.Sin(angle) > 0;
        var near = foreground ? FrontElectron : BackElectron;
        var far = foreground ? BackElectron : FrontElectron;
        far.Visibility = Visibility.Collapsed;
        near.Visibility = Visibility.Visible;
        var scale = foreground ? 1.05 : 0.68;
        near.Width = 50 * scale;
        near.Height = 50 * scale;
        Canvas.SetLeft(near, x - 25 * scale);
        Canvas.SetTop(near, y - 25 * scale);
        // Accelerating glow before the collision.
        near.Opacity = seconds > 1.35 ? .83 + .17 * Math.Sin(seconds * 28) : .93;
    }

    private void Burst()
    {
        BackElectron.Visibility = Visibility.Collapsed;
        FrontElectron.Visibility = Visibility.Collapsed;
        var rng = new Random(12431);
        for (int i = 0; i < 56; i++)
        {
            double size = rng.Next(4, 15);
            var particle = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(i % 3 == 0
                    ? Color.FromRgb(211, 128, 255)
                    : i % 3 == 1
                        ? Color.FromRgb(53, 166, 255)
                        : Color.FromRgb(154, 244, 255)),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.DeepSkyBlue, BlurRadius = 17, ShadowDepth = 0
                }
            };
            Canvas.SetLeft(particle, lastX - size / 2);
            Canvas.SetTop(particle, lastY - size / 2);
            ParticleLayer.Children.Add(particle);
            double phi = i * 2 * Math.PI / 56 + (rng.NextDouble() - .5) * .3;
            double radius = rng.Next(150, 530);
            double duration = rng.Next(340, 700);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            particle.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(
                lastX, lastX + radius * Math.Cos(phi), TimeSpan.FromMilliseconds(duration))
            { EasingFunction = ease });
            particle.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(
                lastY, lastY + radius * Math.Sin(phi), TimeSpan.FromMilliseconds(duration))
            { EasingFunction = ease });
            particle.BeginAnimation(OpacityProperty, new DoubleAnimation(
                1, 0, TimeSpan.FromMilliseconds(duration)));
        }
        var flare = new Ellipse
        {
            Width = 80, Height = 80,
            Fill = new RadialGradientBrush(Colors.White,
                Color.FromArgb(0, 70, 122, 255)),
            RenderTransformOrigin = new Point(.5, .5),
            RenderTransform = new ScaleTransform(1, 1)
        };
        Canvas.SetLeft(flare, lastX - 40);
        Canvas.SetTop(flare, lastY - 40);
        ParticleLayer.Children.Add(flare);
        var expand = new DoubleAnimation(1, 14, TimeSpan.FromMilliseconds(480))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        ((ScaleTransform)flare.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, expand);
        ((ScaleTransform)flare.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, expand);
        flare.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0,
            TimeSpan.FromMilliseconds(520)));
        var disappear = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(580))
        { BeginTime = TimeSpan.FromMilliseconds(60) };
        LogoArtwork.BeginAnimation(OpacityProperty, disappear);
        FallbackLogo.BeginAnimation(OpacityProperty, disappear);
    }

    protected override void OnClosed(EventArgs e)
    {
        CompositionTarget.Rendering -= OnRendering;
        watch.Stop();
        base.OnClosed(e);
    }
}
