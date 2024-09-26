using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Shapes;
using OxyPlot;
using OxyPlot.Series;
using WinForms = System.Windows.Forms;
using Microsoft.Win32;
using OxyPlot.Wpf;
using System.Collections.Generic;
using OxyPlot.Axes;

namespace Potentiostat
{
    public partial class MainWindow : Window
    {
        private SerialPort _serialPort;
        private String selectedSerialPort;

        private Button LSVsubmitButton;
        private TextBox LSVtimeStepBox;
        private Label LSVtimeStepLabel;
        private TextBox LSVstepVBox;
        private Label LSVstepVLabel;
        private TextBox LSVfinalVBox;
        private Label LSVfinalVLabel;
        private Label LSVstartVLabel;
        private TextBox LSVstartVBox;

        private Button CVsubmitButton;
        private TextBox CVtimeStepBox;
        private Label CVtimeStepLabel;
        private TextBox CVstepVBox;
        private Label CVstepVLabel;
        private TextBox CVfinalVBox;
        private Label CVfinalVLabel;
        private Label CVstartVLabel;
        private TextBox CVstartVBox;
        private Label CVcycleLabel;
        private TextBox CVcycleBox;
        private Label CVpeakV1Label;
        private TextBox CVpeakV1Box;
        private Label CVpeakV2Label;
        private TextBox CVpeakV2Box;

        private Label SWVstartVLabel;
        private TextBox SWVstartVBox;
        private Label SWVfinalVLabel;
        private TextBox SWVfinalVBox;
        private Label SWVstepVLabel;
        private TextBox SWVstepVBox;
        private Label SWVtimeStepLabel;
        private TextBox SWVtimeStepBox;
        private Label SWVAmpLabel;
        private TextBox SWVAmpBox;
        private Button SWVsubmitButton;

        private bool IsInMeasure = false;

        public PlotModel plotModel { get; set; }
        private OxyPlot.Series.LineSeries _lineSeries;

        public PlotModel plotModel2 { get; set; }
        private OxyPlot.Series.LineSeries _lineSeries2;

        private StringBuilder dataBuffer = new StringBuilder();
        private List<Tuple<double, double>> dataPoints = new List<Tuple<double, double>>();
        String measureData;
        public MainWindow()
        {
            InitializeComponent();

            string[] ports = SerialPort.GetPortNames();

            for (int i = 0; i < ports.Length; i++)
            {
                COMselect.Items.Add(ports[i]);
            }
            #region IV chart elements
            plotModel = new PlotModel {};
            _lineSeries = new OxyPlot.Series.LineSeries
            {
                Title = "I-V",
                StrokeThickness = 2,
                MarkerType = MarkerType.Circle,
                Color = OxyColors.RoyalBlue
            };

            var xAxis = new LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                Title = "Voltage",
                Unit = "V"
            };

            var yAxis = new LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                Title = "Current",
                Unit = "μA"
            };
            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);
            #endregion

            #region Voltage x time chart elements
            plotModel2 = new PlotModel { };
            _lineSeries2 = new OxyPlot.Series.LineSeries
            {
                Title = "I-V",
                StrokeThickness = 2,
                MarkerType = MarkerType.Circle,
                Color = OxyColors.RoyalBlue
            };

            var yAxisV = new LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                Title = "Voltage",
                Unit = "V"
            };
            var xAxisV = new LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                Title = "Time",
                Unit = "s"
            };
            plotModel2.Axes.Add(yAxisV);
            plotModel2.Axes.Add(xAxisV);
            #endregion


            plotModel.Series.Add(_lineSeries);
            plotView.Model = plotModel;

            plotModel2.Series.Add(_lineSeries2);
            VoltagePlotView.Model = plotModel2;
        }

        private void InitializeSerialPort()
        {
            try
            {
                _serialPort = new SerialPort(selectedSerialPort, 9600);
                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.Open();
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();
            }
            catch
            {
                MessageBox.Show("Nonexistent COM or incomplete form.", "Error");
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = _serialPort.ReadExisting();
                Console.WriteLine(data);
                dataBuffer.Append(data);

                while (dataBuffer.ToString().Contains("\n"))
                {
                    int newLineIndex = dataBuffer.ToString().IndexOf('\n');
                    string line = dataBuffer.ToString().Substring(0, newLineIndex).Trim();
                    dataBuffer.Remove(0, newLineIndex + 1);

                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] values = line.Split(',');

                    if (values.Length == 2)
                    {
                        if (double.TryParse(values[0], out double corrente) && double.TryParse(values[1], out double tensao))
                        {
                            dataPoints.Add(new Tuple<double, double>(corrente, tensao));

                            Dispatcher.Invoke(() => AddPoint(tensao, corrente));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private double time = 0;
        private void AddPoint(double corrente, double tensao)
        {
            _lineSeries.Points.Add(new DataPoint(corrente, tensao));
            plotModel.InvalidatePlot(true);

            _lineSeries2.Points.Add(new DataPoint(time, tensao));
            plotModel2.InvalidatePlot(true);
            time += 1;
        }
        private void ClearGraph_Click(object sender, RoutedEventArgs e)
        {
            #region IV
            dataPoints.Clear();
            Dispatcher.Invoke(() => plotModel.Series.Clear());

            plotModel.InvalidatePlot(true);
            _lineSeries = null;

            _lineSeries = new OxyPlot.Series.LineSeries
            {
                Title = "I-V",
                StrokeThickness = 2,
                MarkerType = MarkerType.Circle,
                Color = OxyColors.Black
            };
            plotModel.Series.Add(_lineSeries);
            plotView.Model = plotModel;
            #endregion

            #region voltage
            Dispatcher.Invoke(() => plotModel2.Series.Clear());

            plotModel2.InvalidatePlot(true);
            _lineSeries2 = null;

            _lineSeries2 = new OxyPlot.Series.LineSeries
            {
                Title = "V",
                StrokeThickness = 2,
                MarkerType = MarkerType.Circle,
                Color = OxyColors.Black
            };
            plotModel2.Series.Add(_lineSeries2);
            VoltagePlotView.Model = plotModel2;
            #endregion
        }

        private void ConfigSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConfigSelect.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedContent = selectedItem.Content.ToString();

                switch (selectedContent)
                {
                    case "Linear Sweep Voltammetry":
                        UserGrid.Children.Clear();
                        LSV_CreateConfigPanelItems();
                        break;
                    case "Cyclic Voltammetry":
                        UserGrid.Children.Clear();
                        CV_CreateConfigPanelItems();
                        break;
                    case "Square Wave Voltammetry":
                        UserGrid.Children.Clear();
                        SWV_CreateConfigPanelItems();
                        break;
                    default:

                        break;
                }
            }
        }

        #region LSV methods
        private void LSV_CreateConfigPanelItems()
        {
            #region Initial Voltage
            LSVstartVLabel = new Label
            {
                Content = "Initial E (V):",
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            LSVstartVBox = new TextBox
            {
                Width = 100,
                Margin = new Thickness(5),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            Grid.SetRow(LSVstartVLabel, 0);
            Grid.SetColumn(LSVstartVLabel, 0);
            UserGrid.Children.Add(LSVstartVLabel);

            Grid.SetRow(LSVstartVBox, 0);
            Grid.SetColumn(LSVstartVBox, 1);
            UserGrid.Children.Add(LSVstartVBox);
            #endregion
            #region Final Voltage
            LSVfinalVLabel = new Label
            {
                Content = "Final E (V):",
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            LSVfinalVBox = new TextBox
            {
                Width = 100,
                Margin = new Thickness(5),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            Grid.SetRow(LSVfinalVLabel, 1);
            Grid.SetColumn(LSVfinalVLabel, 0);
            UserGrid.Children.Add(LSVfinalVLabel);

            Grid.SetRow(LSVfinalVBox, 1);
            Grid.SetColumn(LSVfinalVBox, 1);
            UserGrid.Children.Add(LSVfinalVBox);
            #endregion
            #region Step Voltage
            LSVstepVLabel = new Label
            {
                Content = "Step size (mV):",
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            LSVstepVBox = new TextBox
            {
                Width = 100,
                Margin = new Thickness(5),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            Grid.SetRow(LSVstepVLabel, 2);
            Grid.SetColumn(LSVstepVLabel, 0);
            UserGrid.Children.Add(LSVstepVLabel);

            Grid.SetRow(LSVstepVBox, 2);
            Grid.SetColumn(LSVstepVBox, 1);
            UserGrid.Children.Add(LSVstepVBox);
            #endregion
            #region Time Step
            LSVtimeStepLabel = new Label
            {
                Content = "Scan rate (mV/s)",
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            LSVtimeStepBox = new TextBox
            {
                Width = 100,
                Margin = new Thickness(5),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            Grid.SetRow(LSVtimeStepLabel, 3);
            Grid.SetColumn(LSVtimeStepLabel, 0);
            UserGrid.Children.Add(LSVtimeStepLabel);

            Grid.SetRow(LSVtimeStepBox, 3);
            Grid.SetColumn(LSVtimeStepBox, 1);
            UserGrid.Children.Add(LSVtimeStepBox);
            #endregion

            LSVsubmitButton = new Button
            {
                Content = "Submit",
                Width = 150,
                FontSize = 15,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };

            Grid.SetRow(LSVsubmitButton, 4);
            Grid.SetColumnSpan(LSVsubmitButton, 2);

            UserGrid.Children.Add(LSVsubmitButton);
            LSVsubmitButton.Click += LSV_SendMeasureParameters;
        }

        private void LSV_SendMeasureParameters(Object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(LSVtimeStepBox.Text) && !string.IsNullOrEmpty(LSVstepVBox.Text)
                && !string.IsNullOrEmpty(LSVstartVBox.Text) && !string.IsNullOrEmpty(LSVfinalVBox.Text)
                && _serialPort != null)
            {
                MessageBox.Show("Starting measure.");

                IsInMeasure = true;
                _serialPort.Write("0" + "," + LSVtimeStepBox.Text + "," + LSVstepVBox.Text + "," + LSVstartVBox.Text
                    + "," + LSVfinalVBox.Text);
                measureData = "LSV;scanrate:" + LSVtimeStepBox.Text + ";step size:" + LSVstepVBox.Text + ";initial E:" + LSVstartVBox.Text
                    + ";final E:" + LSVfinalVBox.Text;
            }
            else MessageBox.Show("You must fill all the parameters and connect to a USB.", "Error");
        }
        #endregion
        #region CV methods
        private void CV_CreateConfigPanelItems()
        {
            #region Initial Voltage
            CVstartVLabel = new Label
            {
                Content = "Initial E (V):",
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            CVstartVBox = new TextBox
            {
                Width = 100,
                Margin = new Thickness(5),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            Grid.SetRow(CVstartVLabel, 0);
            Grid.SetColumn(CVstartVLabel, 0);
            UserGrid.Children.Add(CVstartVLabel);

            Grid.SetRow(CVstartVBox, 0);
            Grid.SetColumn(CVstartVBox, 1);
            UserGrid.Children.Add(CVstartVBox);
            #endregion
            #region Peak1 Voltage
            CVpeakV1Label = new Label
            {
                Content = "Peak1 E (V):",
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            CVpeakV1Box = new TextBox
            {
                Width = 100,
                Margin = new Thickness(5),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            Grid.SetRow(CVpeakV1Label, 1);
            Grid.SetColumn(CVpeakV1Label, 0);
            UserGrid.Children.Add(CVpeakV1Label);

            Grid.SetRow(CVpeakV1Box, 1);
            Grid.SetColumn(CVpeakV1Box, 1);
            UserGrid.Children.Add(CVpeakV1Box);
            #endregion
            #region Peak2 Voltage
            CVpeakV2Label = new Label
            {
                Content = "Peak2 E (V):",
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Right
            };

            CVpeakV2Box = new TextBox
            {
                Width = 100,
                Margin = new Thickness(5),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Left,
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            Grid.SetRow(CVpeakV2Label, 2);
            Grid.SetColumn(CVpeakV2Label, 0);
            UserGrid.Children.Add(CVpeakV2Label);

            Grid.SetRow(CVpeakV2Box, 2);
            Grid.SetColumn(CVpeakV2Box, 1);
            UserGrid.Children.Add(CVpeakV2Box);
            #endregion
            #region Final Voltage
            CVfinalVLabel = new Label
            {
                Content = "Final E (V):",
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Right
            };

            CVfinalVBox = new TextBox
            {
                Width = 100,
                Margin = new Thickness(5),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Left,
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            Grid.SetRow(CVfinalVLabel, 3);
            Grid.SetColumn(CVfinalVLabel, 0);
            UserGrid.Children.Add(CVfinalVLabel);

            Grid.SetRow(CVfinalVBox, 3);
            Grid.SetColumn(CVfinalVBox, 1);
            UserGrid.Children.Add(CVfinalVBox);
            #endregion
            #region Step Voltage
            CVstepVLabel = new Label
            {
                Content = "Step size (mV):",
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Right
            };

            CVstepVBox = new TextBox
            {
                Width = 100,
                Margin = new Thickness(5),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Left,
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            Grid.SetRow(CVstepVLabel, 4);
            Grid.SetColumn(CVstepVLabel, 0);
            UserGrid.Children.Add(CVstepVLabel);

            Grid.SetRow(CVstepVBox, 4);
            Grid.SetColumn(CVstepVBox, 1);
            UserGrid.Children.Add(CVstepVBox);
            #endregion
            #region Time Step
            CVtimeStepLabel = new Label
            {
                Content = "Scan rate (mV/s)",
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Right
            };

            CVtimeStepBox = new TextBox
            {
                Width = 100,
                Margin = new Thickness(5),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Left,
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            Grid.SetRow(CVtimeStepLabel, 5);
            Grid.SetColumn(CVtimeStepLabel, 0);
            UserGrid.Children.Add(CVtimeStepLabel);

            Grid.SetRow(CVtimeStepBox, 5);
            Grid.SetColumn(CVtimeStepBox, 1);
            UserGrid.Children.Add(CVtimeStepBox);
            #endregion
            #region Cycle
            CVcycleLabel = new Label
            {
                Content = "Cycles",
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Right
            };

            CVcycleBox = new TextBox
            {
                Width = 100,
                Margin = new Thickness(5),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Left,
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            Grid.SetRow(CVcycleLabel, 6);
            Grid.SetColumn(CVcycleLabel, 0);
            UserGrid.Children.Add(CVcycleLabel);

            Grid.SetRow(CVcycleBox, 6);
            Grid.SetColumn(CVcycleBox, 1);
            UserGrid.Children.Add(CVcycleBox);
            #endregion


            CVsubmitButton = new Button
            {
                Content = "Submit",
                Width = 150,
                FontSize = 15,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };

            Grid.SetRow(CVsubmitButton, 7);
            Grid.SetColumnSpan(CVsubmitButton, 2);

            UserGrid.Children.Add(CVsubmitButton);
            CVsubmitButton.Click += CV_SendMeasureParameters;
        }

        private void CV_SendMeasureParameters(Object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(CVtimeStepBox.Text) && !string.IsNullOrEmpty(CVstepVBox.Text)
                && !string.IsNullOrEmpty(CVstartVBox.Text) && !string.IsNullOrEmpty(CVfinalVBox.Text)
                && !string.IsNullOrEmpty(CVcycleBox.Text) && !string.IsNullOrEmpty(CVpeakV1Box.Text)
                && !string.IsNullOrEmpty(CVpeakV2Box.Text) && _serialPort != null)
            {
                MessageBox.Show("Starting measure.");

                IsInMeasure = true;
                _serialPort.Write("1" + "," + CVtimeStepBox.Text + "," + CVstepVBox.Text + "," + CVstartVBox.Text
                    + "," + CVfinalVBox.Text + "," + CVpeakV1Box.Text + "," + CVpeakV2Box.Text + "," + CVcycleBox.Text);
                measureData = "CV;scanrate:" + CVtimeStepBox.Text + ";step size:" + CVstepVBox.Text + ";initial E:" + CVstartVBox.Text
                    + ";final E:" + CVfinalVBox.Text + ";peak1:" + CVpeakV1Box.Text + ";peak2:" + CVpeakV2Box.Text + ";cycles:" + CVcycleBox.Text;
            }
            else MessageBox.Show("You must fill all the parameters and connect to a USB.", "Error");
        }
        #endregion
        #region SWV methods
        private void SWV_CreateConfigPanelItems()
        {
            #region Initial Voltage
            SWVstartVLabel = new Label
            {
                Content = "Initial E (V):",
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Right
            };

            SWVstartVBox = new TextBox
            {
                Width = 100,
                Margin = new Thickness(5),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Left,
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            Grid.SetRow(SWVstartVLabel, 0);
            Grid.SetColumn(SWVstartVLabel, 0);
            UserGrid.Children.Add(SWVstartVLabel);

            Grid.SetRow(SWVstartVBox, 0);
            Grid.SetColumn(SWVstartVBox, 1);
            UserGrid.Children.Add(SWVstartVBox);
            #endregion
            #region Final Voltage
            SWVfinalVLabel = new Label
            {
                Content = "Final E (V):",
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Right
            };

            SWVfinalVBox = new TextBox
            {
                Width = 100,
                Margin = new Thickness(5),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Left,
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            Grid.SetRow(SWVfinalVLabel, 1);
            Grid.SetColumn(SWVfinalVLabel, 0);
            UserGrid.Children.Add(SWVfinalVLabel);

            Grid.SetRow(SWVfinalVBox, 1);
            Grid.SetColumn(SWVfinalVBox, 1);
            UserGrid.Children.Add(SWVfinalVBox);
            #endregion
            #region Step Voltage
            SWVstepVLabel = new Label
            {
                Content = "Step size (mV):",
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Right
            };

            SWVstepVBox = new TextBox
            {
                Width = 100,
                Margin = new Thickness(5),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Left,
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            Grid.SetRow(SWVstepVLabel, 2);
            Grid.SetColumn(SWVstepVLabel, 0);
            UserGrid.Children.Add(SWVstepVLabel);

            Grid.SetRow(SWVstepVBox, 2);
            Grid.SetColumn(SWVstepVBox, 1);
            UserGrid.Children.Add(SWVstepVBox);
            #endregion
            #region Time Step
            SWVtimeStepLabel = new Label
            {
                Content = "Frequency (Hz)",
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            SWVtimeStepBox = new TextBox
            {
                Width = 100,
                Margin = new Thickness(5),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            Grid.SetRow(SWVtimeStepLabel, 4);
            Grid.SetColumn(SWVtimeStepLabel, 0);
            UserGrid.Children.Add(SWVtimeStepLabel);

            Grid.SetRow(SWVtimeStepBox, 4);
            Grid.SetColumn(SWVtimeStepBox, 1);
            UserGrid.Children.Add(SWVtimeStepBox);
            #endregion
            #region Amplitude
            SWVAmpLabel = new Label
            {
                Content = "Amplitude (mV)",
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Right
            };

            SWVAmpBox = new TextBox
            {
                Width = 100,
                Margin = new Thickness(5),
                VerticalAlignment =System.Windows.VerticalAlignment.Center,
                HorizontalAlignment =System.Windows.HorizontalAlignment.Left,
                FontSize = 17,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            Grid.SetRow(SWVAmpLabel, 3);
            Grid.SetColumn(SWVAmpLabel, 0);
            UserGrid.Children.Add(SWVAmpLabel);

            Grid.SetRow(SWVAmpBox, 3);
            Grid.SetColumn(SWVAmpBox, 1);
            UserGrid.Children.Add(SWVAmpBox);
            #endregion

            SWVsubmitButton = new Button
            {
                Content = "Submit",
                Width = 150,
                FontSize = 15,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };

            Grid.SetRow(SWVsubmitButton, 5);
            Grid.SetColumnSpan(SWVsubmitButton, 2);

            UserGrid.Children.Add(SWVsubmitButton);
            SWVsubmitButton.Click += SWV_SendMeasureParameters;
        }

        private void SWV_SendMeasureParameters(Object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SWVtimeStepBox.Text) && !string.IsNullOrEmpty(SWVstepVBox.Text)
                && !string.IsNullOrEmpty(SWVstartVBox.Text) && !string.IsNullOrEmpty(SWVfinalVBox.Text)
                && !string.IsNullOrEmpty(SWVAmpBox.Text) && _serialPort != null)
            {
                MessageBox.Show("Starting measure.");

                IsInMeasure = true;
                _serialPort.Write("2" + "," + SWVtimeStepBox.Text + "," + SWVstepVBox.Text + "," + SWVstartVBox.Text
                    + "," + SWVfinalVBox.Text + "," + SWVAmpBox.Text);
                measureData = "SWV;frequency" + SWVtimeStepBox.Text + ";step size:" + SWVstepVBox.Text + ";initial E:" + SWVstartVBox.Text
                    + ";final E:" + SWVfinalVBox.Text + ";amplitude:" + SWVAmpBox.Text;
            }
            else MessageBox.Show("You must fill all the parameters and connect to a USB.", "Error");
        }
        #endregion

        #region Save as image
        private void SaveGraphAsImage_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Chart as PNG",
                Filter = "PNG Files (*.png)|*.png",
                DefaultExt = "png",
                FileName = "image.png"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                ExportToPng(filePath);
            }
        }

        private void ExportToPng(string filePath)
        {
            var pngExporter = new PngExporter { Width = 1920, Height = 1080};
            pngExporter.ExportToFile(plotModel, filePath);
            MessageBox.Show("Chart successfully saved as an image in: " + filePath);
        }
        #endregion
        #region Save as csv
        private void SaveDataAsCsv_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save data as CSV",
                Filter = "CSV Files (*.csv)|*.csv",
                DefaultExt = "csv",
                FileName = "data.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                SaveToCsv(saveFileDialog.FileName);
                MessageBox.Show("Data successfully saved to: " + saveFileDialog.FileName);
            }
        }

        private void SaveToCsv(string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine(measureData);

                writer.WriteLine("voltage,current");
                
                foreach (var point in dataPoints)
                {
                    writer.WriteLine($"{point.Item1},{point.Item2}");
                }
            }
        }
        #endregion
        private void ToggleConnection(object sender, RoutedEventArgs e)
        {
            if (_serialPort != null)
            {
                Console.WriteLine("disconnect");
                _serialPort.Close();
                _serialPort = null;
                ToggleConnection_btn.Content = "Connect";
            }
            else
            {
                try
                {
                    Console.WriteLine("connect");
                    selectedSerialPort = COMselect.Text;
                    InitializeSerialPort();
                    ToggleConnection_btn.Content = "Disconnect";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao conectar: {ex.Message}");
                }
            }
        }
    }
}
