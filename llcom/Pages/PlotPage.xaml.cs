using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace llcom.Pages
{
    /// <summary>
    /// PlotPage.xaml 的交互逻辑
    /// </summary>
    public partial class PlotPage : Page
    {
        public PlotPage()
        {
            InitializeComponent();
        }

        //最多十个图像
        private double[][] Data = new double[10][];
        private double[] DataX = null;
        //最大点数量
        private static int MaxPoints = 1000;

        private ScottPlot.Plottable.Crosshair ch = null;

        private ScottPlot.Styles.IStyle[] Styles = ScottPlot.Style.GetStyles();
        private int StyleNow = -1;

        private bool NeedRefresh = true;

        bool first = true;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!first)
                return;
            //暂时先定1000个点吧
            DataX = new double[MaxPoints];
            for (int i = 0; i < MaxPoints; i++)
                DataX[i] = i - MaxPoints + 1;
            for (int i = 0; i < Data.Length; i++)
            {
                if(Data[i] == null)
                    Data[i] = new double[MaxPoints];
                Plot.Plot.AddSignalXY(DataX, Data[i]);
            }
            Plot.Plot.SetAxisLimitsX(-MaxPoints, 0);
            ch = Plot.Plot.AddCrosshair(0,0);

            ch.Color = System.Drawing.Color.LightGray;
            ch.LineWidth = 2;

            new Thread(() =>
            {
                var r = new Random();
                while (true)
                {
                    AddPoint(r.Next(-10, 10), 1);
                    Task.Delay(10).Wait();
                }
            }).Start();

            new Thread(() =>
            {
                while (true)
                {
                    if(NeedRefresh)
                    {
                        NeedRefresh = false;
                        this.Dispatcher.Invoke(new Action(delegate
                        {
                            try
                            {
                                Plot.Render();
                            }
                            catch { }
                        }));
                    }
                    Thread.Sleep(100);
                }
            }).Start();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < Data.Length; i++)
                for(int j = 0; j < Data[i].Length; j++)
                    Data[i][j] = 0;
            Refresh();
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            StyleNow++;
            if(StyleNow >= Styles.Length)
                StyleNow = 0;
            Plot.Plot.Style(Styles[StyleNow]);
            Refresh();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Plot.Plot.SetAxisLimitsX(-MaxPoints, 0);
            Plot.Plot.SetAxisLimitsY(Data.Min(x => x.Min()), Data.Max(x => x.Max()));
            Refresh();
        }

        private void Plot_MouseMove(object sender, MouseEventArgs e)
        {
            var p = Plot.GetMouseCoordinates();
            ch.X = p.x;
            ch.Y = p.y;
            Refresh();
        }

        private void Refresh() => NeedRefresh = true;

        private void AddPoint(double d, int line)
        {
            if(Data[line] == null)
                Data[line] = new double[MaxPoints];
            for(int i = 0;i < MaxPoints - 1;i++)
                Data[line][i] = Data[line][i + 1];
            Data[line][MaxPoints - 1] = d;
            Refresh();
        }
    }
}
