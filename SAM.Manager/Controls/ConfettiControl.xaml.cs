/* Copyright (c) 2024-2026 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using System.Numerics;
using Windows.UI;

namespace SAM.Manager.Controls;

/// <summary>
/// A control that displays confetti particles to celebrate achievements.
/// </summary>
public sealed partial class ConfettiControl : UserControl
{
    private readonly Random _random = new();
    private readonly List<Rectangle> _particles = [];
    private DispatcherTimer? _cleanupTimer;
    
    // Confetti colors (celebration palette)
    private static readonly Color[] ConfettiColors =
    [
        Color.FromArgb(255, 255, 215, 0),   // Gold
        Color.FromArgb(255, 255, 105, 180), // Hot Pink
        Color.FromArgb(255, 0, 191, 255),   // Deep Sky Blue
        Color.FromArgb(255, 50, 205, 50),   // Lime Green
        Color.FromArgb(255, 255, 69, 0),    // Red Orange
        Color.FromArgb(255, 138, 43, 226),  // Blue Violet
        Color.FromArgb(255, 255, 255, 255), // White
        Color.FromArgb(255, 0, 255, 255),   // Cyan
    ];

    public ConfettiControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Setup cleanup timer
        _cleanupTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _cleanupTimer.Tick += CleanupParticles;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _cleanupTimer?.Stop();
        ClearParticles();
    }

    /// <summary>
    /// Triggers the confetti celebration animation.
    /// </summary>
    public async Task PlayCelebrationAsync()
    {
        // Spawn confetti particles
        SpawnConfetti(100);
        
        // Start cleanup timer
        _cleanupTimer?.Start();
        
        // Wait for animation to complete
        await Task.Delay(3500);
    }

    private void SpawnConfetti(int count)
    {
        var width = ActualWidth > 0 ? ActualWidth : 800;
        var height = ActualHeight > 0 ? ActualHeight : 600;
        
        for (int i = 0; i < count; i++)
        {
            var particle = CreateParticle();
            _particles.Add(particle);
            ConfettiCanvas.Children.Add(particle);
            
            // Random starting position (top of screen, spread horizontally)
            var startX = _random.NextDouble() * width;
            var startY = -20 - _random.NextDouble() * 100;
            
            Canvas.SetLeft(particle, startX);
            Canvas.SetTop(particle, startY);
            
            // Animate the particle falling
            AnimateParticle(particle, startX, startY, height + 50, TimeSpan.FromMilliseconds(i * 30));
        }
    }

    private Rectangle CreateParticle()
    {
        var color = ConfettiColors[_random.Next(ConfettiColors.Length)];
        var size = 6 + _random.NextDouble() * 8;
        var isRect = _random.NextDouble() > 0.5;
        
        return new Rectangle
        {
            Width = isRect ? size * 0.6 : size,
            Height = isRect ? size * 1.5 : size,
            Fill = new SolidColorBrush(color),
            RadiusX = isRect ? 1 : size / 2,
            RadiusY = isRect ? 1 : size / 2,
            RenderTransform = new CompositeTransform
            {
                Rotation = _random.NextDouble() * 360
            },
            RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
        };
    }

    private void AnimateParticle(Rectangle particle, double startX, double startY, double endY, TimeSpan delay)
    {
        var storyboard = new Storyboard
        {
            BeginTime = delay
        };
        
        // Vertical fall animation
        var fallAnimation = new DoubleAnimation
        {
            From = startY,
            To = endY,
            Duration = TimeSpan.FromMilliseconds(2000 + _random.NextDouble() * 1500),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(fallAnimation, particle);
        Storyboard.SetTargetProperty(fallAnimation, "(Canvas.Top)");
        storyboard.Children.Add(fallAnimation);
        
        // Horizontal drift animation (sine wave effect)
        var driftDistance = 30 + _random.NextDouble() * 60;
        var driftDirection = _random.NextDouble() > 0.5 ? 1 : -1;
        var driftAnimation = new DoubleAnimationUsingKeyFrames();
        driftAnimation.KeyFrames.Add(new LinearDoubleKeyFrame
        {
            KeyTime = TimeSpan.Zero,
            Value = startX
        });
        driftAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
        {
            KeyTime = TimeSpan.FromMilliseconds(800),
            Value = startX + driftDistance * driftDirection,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        });
        driftAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
        {
            KeyTime = TimeSpan.FromMilliseconds(1600),
            Value = startX - driftDistance * 0.5 * driftDirection,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        });
        driftAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
        {
            KeyTime = TimeSpan.FromMilliseconds(2500),
            Value = startX + driftDistance * 0.3 * driftDirection,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        });
        Storyboard.SetTarget(driftAnimation, particle);
        Storyboard.SetTargetProperty(driftAnimation, "(Canvas.Left)");
        storyboard.Children.Add(driftAnimation);
        
        // Rotation animation
        var transform = particle.RenderTransform as CompositeTransform;
        if (transform != null)
        {
            var rotationAnimation = new DoubleAnimation
            {
                From = transform.Rotation,
                To = transform.Rotation + 360 * (_random.NextDouble() > 0.5 ? 1 : -1) * (1 + _random.NextDouble()),
                Duration = TimeSpan.FromMilliseconds(2000 + _random.NextDouble() * 1500),
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(rotationAnimation, transform);
            Storyboard.SetTargetProperty(rotationAnimation, "Rotation");
            storyboard.Children.Add(rotationAnimation);
        }
        
        // Fade out at end
        var fadeAnimation = new DoubleAnimationUsingKeyFrames();
        fadeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame
        {
            KeyTime = TimeSpan.Zero,
            Value = 1
        });
        fadeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame
        {
            KeyTime = TimeSpan.FromMilliseconds(2000),
            Value = 1
        });
        fadeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame
        {
            KeyTime = TimeSpan.FromMilliseconds(3000),
            Value = 0
        });
        Storyboard.SetTarget(fadeAnimation, particle);
        Storyboard.SetTargetProperty(fadeAnimation, "Opacity");
        storyboard.Children.Add(fadeAnimation);
        
        storyboard.Begin();
    }

    private void CleanupParticles(object? sender, object e)
    {
        ClearParticles();
        _cleanupTimer?.Stop();
    }

    private void ClearParticles()
    {
        foreach (var particle in _particles)
        {
            ConfettiCanvas.Children.Remove(particle);
        }
        _particles.Clear();
    }
}
