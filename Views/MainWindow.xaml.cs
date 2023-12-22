using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Fireworks.Utils;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MessageBox = System.Windows.MessageBox;

namespace Fireworks.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += MainWindow_SourceInitialized;
        Loaded += MainWindow_Loaded;

        InitializeTrayMenu();
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
        var (scaleFactor, scaleY) = DpiUtil.GetScale(this);
        var taskBarHeight = GetTaskBarHeight();
        _taskBarHeight = (int)(taskBarHeight / scaleFactor);
        
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
            var y = verticalVelocity * time - 0.5 * GRAVITY * time * time + _taskBarHeight;

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

    private void ParticleGenerator_Tick(object sender, EventArgs e)
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
            < 30 => GoogleYellow,       // 15% probability
            < 45 => GoogleGreen,        // 15% probability
            < 60 => GoogleBlue,         // 15% probability
            < 75 => GoogleRed,          // 15% probability
            _ => Brushes.Gold
        };
    }

    private static readonly Brush GoogleBlue = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4285F4")!);
    private static readonly Brush GoogleRed = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EA4335")!);
    private static readonly Brush GoogleGreen = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34A853")!);
    private static readonly Brush GoogleYellow = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBC05")!);

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
    
    #region 鼠标穿透
    
    // 导入 Windows API
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    
    // 常量定义
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x20;
    
    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        // 获取当前窗口句柄
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        // 设置窗口样式为透明
        SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_TRANSPARENT);
    }
    
    #endregion
    
    #region 托盘功能区

    // 初始化托盘菜单
    private void InitializeTrayMenu()
    {
        // 创建托盘图标
        _trayIcon = new NotifyIcon();
        var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/ico_main.ico"))?.Stream;
        _trayIcon.Icon = new Icon(iconStream!);
        _trayIcon.Text = "Do you like fireworks?";
        _trayIcon.Visible = true;
        
        // 菜单-雪花样式
        var trayMenu = new ContextMenuStrip();
        
        trayMenu.Items.Add("关于", null, OnTrayIconAboutClicked);
        trayMenu.Items.Add("反馈", null, OnTrayIconTouchClicked);
        trayMenu.Items.Add("退出", null, OnTrayIconExitClicked);

        _trayIcon.ContextMenuStrip = trayMenu;
    }

    private void OnTrayIconAboutClicked(object sender, EventArgs e)
    {
        // 获取当前程序集的版本号
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        MessageBox.Show($"版本：{version}");
    }

    private static void OnTrayIconTouchClicked(object? sender, EventArgs e)
    {
        var window = new FeedbackWindow();
        window.ShowDialog();
    }

    private NotifyIcon _trayIcon;

    private void OnTrayIconExitClicked(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        Application.Current.Shutdown();
    }
    
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _trayIcon.Dispose();
    }

    #endregion
    
    #region 获得任务栏高度以摆放物件
    
    private int _taskBarHeight;
    
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref Rect pvParam, uint fWinIni);

    private const uint SpiGetWorkArea = 0x0030;

    private struct Rect
    {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        public int Left, Top, Right, Bottom;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    }

    private static int GetTaskBarHeight()
    {
        var rect = new Rect();
        if (!SystemParametersInfo(SpiGetWorkArea, 0, ref rect, 0)) return 0;
        var screenHeight = Screen.PrimaryScreen.Bounds.Height;
        var taskBarHeight = screenHeight - rect.Bottom;
        return taskBarHeight;
    }

    #endregion
}