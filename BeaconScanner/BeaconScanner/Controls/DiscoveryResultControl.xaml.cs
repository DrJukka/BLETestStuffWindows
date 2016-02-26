using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BeaconScanner.Controls
{
    public sealed partial class DiscoveryResultControl : UserControl
    {
        private Color TrueColor = Colors.White;
        private Color FalseColor = Colors.Black;
  
        public bool[] Results
        {
            get {
                var ret = GetValue(ResultsArrayProperty);
                    return (bool[])ret;
            }
            set
            {
                SetValue(ResultsArrayProperty, value);
                DrawChart();
            }
        }

        public static readonly DependencyProperty ResultsArrayProperty =
            DependencyProperty.Register("Results", typeof(bool[]), typeof(DiscoveryResultControl), new PropertyMetadata(new bool[100]));

        private CanvasRenderTarget _offscreenBackGround = null;

        public DiscoveryResultControl()
        {
            this.InitializeComponent();
        }

        private void ChartWin2DCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            if (_offscreenBackGround != null)
            {
                args.DrawingSession.DrawImage(_offscreenBackGround);// new Rect(new Point(0, 0), ), new Rect(new Point(0, 0), _offscreenBackGround.Size));
            }
            else
            { 
                args.DrawingSession.DrawRectangle(0, 0, (float)chartGrid.Width, (float)chartGrid.Height, Colors.LightBlue);
            }
        }

        private void chartGrid_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            //re-calculate & draw the graph when orientation changes
            DrawChart();
        }
        public void DrawChart()
        {
            float useHeight = (float)chartGrid.ActualHeight;
            float useWidth = (float)drawingCanvas.ActualWidth;

            if (Results == null || Results.Length <= 0 || useHeight == 0 ||useWidth == 0)
            {
                return;
            }

            CanvasDevice device = CanvasDevice.GetSharedDevice();

            _offscreenBackGround = new CanvasRenderTarget(device, useWidth, useHeight, 96);

            using (CanvasDrawingSession ds = _offscreenBackGround.CreateDrawingSession())
            {
                ds.Clear(Colors.LightGray);

                var tickOffsetX = useWidth / Results.Length;
                var currentOffsetX = 0.0;
                for (int i = 0; i < Results.Length; i++)
                {
                    ds.FillRectangle(new Rect(new Point(currentOffsetX,0), new Size(tickOffsetX, useHeight)), Results[i] ? TrueColor : FalseColor);
                    currentOffsetX += tickOffsetX;
                }
            }
            //forces re-draw
            drawingCanvas.Invalidate();
        }
    }
}
