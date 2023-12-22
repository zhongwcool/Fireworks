using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Fireworks.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    #region 粒子系统

    private readonly Random _random = new();
    private readonly List<Ellipse> _particles = new();
    private readonly DispatcherTimer _timer = new();
    private readonly DispatcherTimer _particleGenerator = new();
    private const double GRAVITY = 98;
    private const double MAX_HORIZONTAL_DISTANCE = 200; // 水平方向的最大距离
    private const double MAX_VERTICAL_DISTANCE = 100; // 竖直方向的最大距离

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _timer.Interval = TimeSpan.FromMilliseconds(20);
        _timer.Tick += Timer_Tick;
        _timer.Start();

        _particleGenerator.Interval = TimeSpan.FromMilliseconds(20);
        _particleGenerator.Tick += ParticleGenerator_Tick;
        _particleGenerator.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        foreach (var particle in _particles.ToArray())
        {
            var time = (DateTime.Now - (DateTime)particle.Tag).TotalSeconds;
            var (initialVelocity, angle) = ((double, double))particle.DataContext;

            var radian = angle * Math.PI / 180;
            var horizontalVelocity = initialVelocity * Math.Cos(radian);
            var verticalVelocity = initialVelocity * Math.Sin(radian);
            var x = horizontalVelocity * time;
            var y = verticalVelocity * time - 0.5 * GRAVITY * time * time;

            if (Math.Abs(x) > MAX_HORIZONTAL_DISTANCE || y < -MAX_VERTICAL_DISTANCE)
            {
                ParticleCanvas.Children.Remove(particle);
                _particles.Remove(particle);
            }
            else
            {
                Canvas.SetLeft(particle, x + ParticleCanvas.ActualWidth / 2);
                Canvas.SetTop(particle, ParticleCanvas.ActualHeight - y);
            }
        }
    }

    private void ParticleGenerator_Tick(object? sender, EventArgs e)
    {
        CreateParticle();
    }

    private void CreateParticle()
    {
        var ellipse = new Ellipse
        {
            Fill = GetRandomColor(),
            Width = 5,
            Height = 5,
            DataContext = (InitialVelocity(), RandomAngle()),
            Tag = DateTime.Now
        };

        _particles.Add(ellipse);
        ParticleCanvas.Children.Add(ellipse);
    }
    
    private static readonly Random Random2 = new Random();

    private static Brush GetRandomColor()
    {
        var randomNumber = Random2.Next(100); // Generates a number between 0 and 99
        return randomNumber switch
        {
            < 15 => Brushes.LightGray,// 15% probability
            < 30 => GoogleYellow,// 15% probability
            < 45 => GoogleGreen,// 15% probability
            < 60 => GoogleBlue,// 15% probability
            < 75 => GoogleRed,// 15% probability
            _ => Brushes.Gold
        };
    }

    private static readonly Brush GoogleBlue = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4285F4"));
    private static readonly Brush GoogleRed = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EA4335"));
    private static readonly Brush GoogleGreen = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34A853"));
    private static readonly Brush GoogleYellow = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBC05"));

    private double InitialVelocity()
    {
        return _random.NextDouble() * 100 + 200; // 降低初始速度的范围以适应屏幕动画
    }

    private double RandomAngle()
    {
        // 角度从45到135度
        return _random.NextDouble() * 90 + 45;
    }

    #endregion
}