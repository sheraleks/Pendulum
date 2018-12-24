﻿using System;
using System.Collections.Generic;
using System.Windows;
using HelixToolkit;
using HelixToolkit.Wpf;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using ODE;
using OxyPlot;
using OxyPlot.Series;


namespace PendulumWPF
{
    public class MainViewModel
    {
        public MainViewModel()
        {
            MyModel = new PlotModel { Title = "Example 1" };
        }

        public static PlotModel MyModel { get; private set; }
    }


public static class GraphData
    {
        public static LineSeries z_t =new LineSeries();
        //public static List<DataPoint> z_t = new List<DataPoint>();
        public static List<DataPoint> theta_t = new List<DataPoint>();
        public static List<DataPoint> theta_z = new List<DataPoint>();
    }

    public partial class Pendulum : Window
    {
        public Pendulum()
        {
            InitializeComponent();
            ObjReader objReader = new ObjReader();
            Create3DViewPort();

        }

        Model3D pendulum;
        Model3D @base;
        Model3D stick90;
        Model3D stick;
        Model3D table;
        Model3D spring;

        private Transform3DGroup pendulumTransform;
        private Transform3DGroup springTransform;

        Model3DGroup system;
        private void Create3DViewPort()
        {
            system = new Model3DGroup();

            ModelImporter pendulumImporter = new ModelImporter();
            pendulumImporter.DefaultMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.DarkMagenta));
            pendulum = pendulumImporter.Load("pendulum90ver2.obj");
            pendulumTransform = new Transform3DGroup();
            pendulumTransform.Children.Add(new ScaleTransform3D(1.5,1.5,1.5));
            pendulumTransform.Children.Add(new TranslateTransform3D(0, 5 ,-3));
            pendulum.Transform = pendulumTransform;
            system.Children.Add(pendulum);

            ModelImporter baseImporter = new ModelImporter();
            baseImporter.DefaultMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.CornflowerBlue));
            @base = baseImporter.Load("tripod1d.obj");
            Transform3DGroup baseTransform = new Transform3DGroup();
            baseTransform.Children.Add(new ScaleTransform3D(0.25, 0.4, 0.25));
            baseTransform.Children.Add(new TranslateTransform3D(0, 0, -10));
            @base.Transform = baseTransform;
            system.Children.Add(@base);

            ModelImporter stick90Importer = new ModelImporter();
            stick90Importer.DefaultMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.CornflowerBlue));
            stick90 = stick90Importer.Load("tripod2d.obj");
            Transform3DGroup stick90Transform = new Transform3DGroup();
            stick90Transform.Children.Add(new ScaleTransform3D(0.25, 0.25, 0.25));
            stick90Transform.Children.Add(new TranslateTransform3D(0, -15, 5));
            stick90.Transform = stick90Transform;
            system.Children.Add(stick90);

            ModelImporter stickImporter = new ModelImporter();
            stickImporter.DefaultMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.CornflowerBlue));
            stick = stickImporter.Load("tripod3d.obj");
            Transform3DGroup stickTransform = new Transform3DGroup();
            stickTransform.Children.Add(new ScaleTransform3D(0.25, 0.4, 0.25));
            stickTransform.Children.Add(new TranslateTransform3D(0, -34, 12));
            stick.Transform = stickTransform;
            system.Children.Add(stick);

            ModelImporter tableImporter = new ModelImporter();
            tableImporter.DefaultMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.BurlyWood));
            table = tableImporter.Load("table.obj");
            Transform3DGroup tableTransform = new Transform3DGroup();
            tableTransform.Children.Add(new ScaleTransform3D(0.03, 0.03, 0.03));
            tableTransform.Children.Add(new TranslateTransform3D(15, -15, 15));
            table.Transform = tableTransform;
            system.Children.Add(table);

            ModelImporter springImporter = new ModelImporter();
            spring = springImporter.Load("spring.obj");

            springTransform = new Transform3DGroup();
            springTransform.Children.Add(new TranslateTransform3D(0, 5, -11.4));
            springTransform.Children.Add(new ScaleTransform3D(1, 1, -1));
            var cutLength = stick.Bounds.Z - (pendulum.Bounds.Z + pendulum.Bounds.SizeZ);
            var scale = cutLength / spring.Bounds.SizeZ;
            springTransform.Children.Add(new ScaleTransform3D(new Vector3D(1, 1, scale), new Point3D(0, 5, stick.Bounds.Z)));

            spring.Transform = springTransform;

            system.Children.Add(spring);

            scene.Content = system;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            // открывается из "Сохранить скриншот"
            var graph = new OXYPlotTest();
            graph.Show();
            
        }

        private void Start_OnClick(object sender, RoutedEventArgs e)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += timerTick;
            timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            timer.Start();

            //MainViewModel.MyModel.Series.Clear();
            //MainViewModel.MyModel.Series.Add(new );
            //MainViewModel.MyModel.InvalidatePlot(true);

        }

        private double startT = 0;
        private double deltaT = 0.01;
        private double endT = 0.02;
        private double[] y0 = new double[] {0, 0, Math.PI * 2, 0 };

        private RotateTransform3D rotate;
        private ScaleTransform3D scaleTransform;

        private double[,] solve;
        private double cutLength;
        private double scale;

        private void timerTick(object sender, EventArgs e)
        {
            //graph.Close();
            //solve[i,0] - t, solve[i, 1] - z, solve[i, 2] - zdot, solve[i, 3] - theta, solve[i, 4] - thetadot 
            solve = WilberforcePendulum.GetOscillations(startT, deltaT, endT, y0);
            y0[0] = solve[1, 1]; y0[1] = solve[1, 2];
            y0[2] = solve[1, 3]; y0[3] = solve[1, 4];
         
            startT += deltaT;
            endT += deltaT;



            // преобразование маятника
            rotate = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), solve[0, 3] * 180 / Math.PI));
            pendulumTransform.Children.Clear(); // очистим предыдущие изменения
            pendulumTransform.Children.Add(rotate);
            pendulumTransform.Children.Add(new ScaleTransform3D(1.5, 1.5, 1.5));
            pendulumTransform.Children.Add(new TranslateTransform3D(0, 5, - 3 + 30 * solve[0, 1]));
            pendulum.Transform = pendulumTransform;

            // преобразование пружины
            cutLength = stick.Bounds.Z - (pendulum.Bounds.Z + pendulum.Bounds.SizeZ);
            scale = cutLength / spring.Bounds.SizeZ;
            scaleTransform = new ScaleTransform3D(new Vector3D(1, 1, scale), new Point3D(0, 5, stick.Bounds.Z));
            springTransform.Children.Add(scaleTransform);

            // формирование списка данных для графиков
            GraphData.z_t.Points.Add(new DataPoint(solve[0, 0], solve[0, 1]));
            MainViewModel.MyModel.Series.Clear();
            MainViewModel.MyModel.Series.Add(GraphData.z_t);
            MainViewModel.MyModel.InvalidatePlot(true);
            //GraphData.theta_t.Add(new DataPoint(solve[0,0], solve[0,3]));
            //GraphData.theta_z.Add(new DataPoint(solve[0, 1], solve[0, 3]));
            //if (GraphData.theta_z.Count > 1000)
            //{
            //    GraphData.z_t.RemoveAt(0);
            //    GraphData.theta_t.RemoveAt(0);
            //    GraphData.theta_z.RemoveAt(0);
            //}
        }
    }
}
    