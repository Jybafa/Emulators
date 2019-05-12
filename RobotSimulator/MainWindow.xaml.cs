using HelixToolkit.Wpf;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RobotSimulator
{
	class Joint
	{
		public Model3D model = null;
		public double angle = 0;
		public double angleMin = -180;
		public double angleMax = 180;
		public int rotPointX = 0;
		public int rotPointY = 0;
		public int rotPointZ = 0;
		public int rotAxisX = 0;
		public int rotAxisY = 0;
		public int rotAxisZ = 0;

		public Joint(Model3D pModel)
		{
			model = pModel;
		}
	}

	class box
	{
		public int g = 0;
		public RotateTransform3D R;
		public TranslateTransform3D T;
		public box(RotateTransform3D _R, TranslateTransform3D _T)
		{
			R = _R;
			T = _T;
		}
	}

	class RobotCommand
	{
		public double PositionX;
		public double PositionY;
		public double RotationT;
		public double GripperPos;
		public int GripperMode;
		public int Animation;
		public Vector3D point;
		public RobotCommand(double _PositionX = 0, double _PositionY = 0, double _RotationT = 0, int _GripperMode = 0, double _GripperPos = 0)
		{
			PositionX = _PositionX;
			PositionY = _PositionY;
			RotationT = _RotationT;
			GripperMode = _GripperMode;
			GripperPos = _GripperPos;
			Animation = 0;
		}
	}

	public class RootObjectRobotSimulator
	{
		public int L1 { get; set; }
		public int Y1 { get; set; }
		public int X1 { get; set; }
		public int G1 { get; set; }
		public int T1 { get; set; }
		public int N { get; set; }
	}

	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();

			basePath = "res\\arm\\obj\\";
			List<string> modelsNames = new List<string>();
			modelsNames.Add(MODEL_PATH1);
			modelsNames.Add(MODEL_PATH2);
			modelsNames.Add(MODEL_PATH3);
			modelsNames.Add(MODEL_PATH4);
			modelsNames.Add(MODEL_PATH5);
			modelsNames.Add(MODEL_PATH6);
			modelsNames.Add(MODEL_PATH7);
			modelsNames.Add(MODEL_PATH8);
			modelsNames.Add(MODEL_PATH9);
			modelsNames.Add(MODEL_PATH9);
			modelsNames.Add(MODEL_PATH9);
			modelsNames.Add(MODEL_PATH9);

			RoboticArm.Content = Initialize_Environment(modelsNames);

			var builder = new MeshBuilder(true, true);
			var position = new Point3D(0, 0, 0);
			builder.AddSphere(position, 50, 15, 15);
			visual = new ModelVisual3D();

			Map.Fill = new ImageBrush(new BitmapImage(new Uri("res\\image\\map.png", UriKind.Relative)));

			viewPort3d.RotateGesture = new MouseGesture(MouseAction.RightClick);
			viewPort3d.PanGesture = new MouseGesture(MouseAction.LeftClick);
			viewPort3d.Children.Add(visual);
			viewPort3d.Children.Add(RoboticArm);
			viewPort3d.Camera.LookDirection = new Vector3D(-14000, 0, -14000);
			viewPort3d.Camera.UpDirection = new Vector3D(0.0, 0.0, 1.0);
			viewPort3d.Camera.Position = new Point3D(13700, -200, 14000);

			Boxs = new box[4];

			Boxs[0] = new box(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), -45), new Point3D(0, 0, 0)), new TranslateTransform3D(-700, -2600, joints[8].model.Bounds.SizeZ + 60));
			Boxs[1] = new box(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 45), new Point3D(0, 0, 0)), new TranslateTransform3D(700, -1240, joints[9].model.Bounds.SizeZ + 60));
			Boxs[2] = new box(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 45), new Point3D(0, 0, 0)), new TranslateTransform3D(560, -3840, joints[10].model.Bounds.SizeZ + 60));
			Boxs[3] = new box(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), -45), new Point3D(0, 0, 0)), new TranslateTransform3D(1960, -2500, joints[11].model.Bounds.SizeZ + 60));

			double[] angles = { joints[0].angle, joints[1].angle, joints[2].angle, joints[3].angle = 90, joints[4].angle, joints[5].angle = 70 };
			ForwardKinematics(angles);

			RenderTimer = new System.Windows.Forms.Timer();
			RenderTimer.Interval = 5;
			RenderTimer.Tick += new System.EventHandler(RenderTimer_Tick);

			RenderTimer.Start();
		}

		Model3DGroup RA = new Model3DGroup();

		List<Joint> joints = null;

		RobotCommand lastRobotCommand = null;
		box[] Boxs = null;

		Color oldColor = Colors.White;
		string basePath = "";
		ModelVisual3D visual;
		double LearningRate = 0.01;
		double SamplingDistance = 0.15;
		double DistanceThreshold = 20;

		ModelVisual3D RoboticArm = new ModelVisual3D();
		Transform3DGroup FB;
		Transform3DGroup F1;
		Transform3DGroup F2;
		Transform3DGroup F3;
		Transform3DGroup F4;
		Transform3DGroup F5;
		Transform3DGroup F6L;
		Transform3DGroup F6R;
		Transform3DGroup F7;
		Transform3DGroup FO1;
		Transform3DGroup FO2;
		Transform3DGroup FO3;
		Transform3DGroup FO4;
		RotateTransform3D R;
		TranslateTransform3D T;
		int movements = 10;
		System.Windows.Forms.Timer RenderTimer;

		public bool ParkingZone = false;

		private const string MODEL_PATH1 = "arm_1.obj";
		private const string MODEL_PATH2 = "arm_2.obj";
		private const string MODEL_PATH3 = "arm_3.obj";
		private const string MODEL_PATH4 = "arm_4.obj";
		private const string MODEL_PATH5 = "arm_5.obj";
		private const string MODEL_PATH6 = "gripper_l.obj";
		private const string MODEL_PATH7 = "gripper_r.obj";
		private const string MODEL_PATH8 = "base.obj";
		private const string MODEL_PATH9 = "box.obj";

		private Model3DGroup Initialize_Environment(List<string> modelsNames)
		{
			try
			{
				ModelImporter import = new ModelImporter();
				joints = new List<Joint>();

				foreach (string modelName in modelsNames)
				{
					var link = import.Load(basePath + modelName);
					GeometryModel3D model = link.Children[0] as GeometryModel3D;
					joints.Add(new Joint(link));
				}

				var builder = new MeshBuilder(true, true);
				var position = new Point3D(0, 0, 0);
				builder.AddSphere(position, 0.01, 15, 15);
				joints.Add(new Joint(new GeometryModel3D(builder.ToMesh(), Materials.Gold)));

				RA.Children.Add(joints[0].model);
				RA.Children.Add(joints[1].model);
				RA.Children.Add(joints[2].model);
				RA.Children.Add(joints[3].model);
				RA.Children.Add(joints[4].model);
				RA.Children.Add(joints[5].model);
				RA.Children.Add(joints[6].model);
				RA.Children.Add(joints[7].model);
				RA.Children.Add(joints[8].model);
				RA.Children.Add(joints[9].model);
				RA.Children.Add(joints[10].model);
				RA.Children.Add(joints[11].model);
				RA.Children.Add(joints[12].model);

				joints[0].angleMin = -180;
				joints[0].angleMax = 180;
				joints[0].rotAxisX = 0;
				joints[0].rotAxisY = 0;
				joints[0].rotAxisZ = 1;
				joints[0].rotPointX = 0;
				joints[0].rotPointY = 0;
				joints[0].rotPointZ = 0;

				joints[1].angleMin = -30;
				joints[1].angleMax = 120;
				joints[1].rotAxisX = 1;
				joints[1].rotAxisY = 0;
				joints[1].rotAxisZ = 0;
				joints[1].rotPointX = 0;
				joints[1].rotPointY = -481;
				joints[1].rotPointZ = 1611;

				joints[2].angleMin = -90;
				joints[2].angleMax = 45;
				joints[2].rotAxisX = 1;
				joints[2].rotAxisY = 0;
				joints[2].rotAxisZ = 0;
				joints[2].rotPointX = 0;
				joints[2].rotPointY = 963;
				joints[2].rotPointZ = 3532;

				joints[3].angleMin = -90;
				joints[3].angleMax = 90;
				joints[3].rotAxisX = 1;
				joints[3].rotAxisY = 0;
				joints[3].rotAxisZ = 0;
				joints[3].rotPointX = 0;
				joints[3].rotPointY = -1655;
				joints[3].rotPointZ = 3774;

				joints[4].angleMin = -180;
				joints[4].angleMax = 180;
				joints[4].rotAxisX = 0;
				joints[4].rotAxisY = 1;
				joints[4].rotAxisZ = 0;
				joints[4].rotPointX = 168;
				joints[4].rotPointY = -2148;
				joints[4].rotPointZ = 3772;

				joints[5].angleMin = 0;
				joints[5].angleMax = 70;
				joints[5].rotAxisX = 0;
				joints[5].rotAxisY = 0;
				joints[5].rotAxisZ = 1;
				joints[5].rotPointX = 360;
				joints[5].rotPointY = -2147;
				joints[5].rotPointZ = 3777;


				joints[6].angleMin = -70;
				joints[6].angleMax = 0;
				joints[6].rotAxisX = 0;
				joints[6].rotAxisY = 0;
				joints[6].rotAxisZ = 1;
				joints[6].rotPointX = -24;
				joints[6].rotPointY = -2147;
				joints[6].rotPointZ = 3777;

			}
			catch (Exception e)
			{
				MessageBox.Show("Exception Error:" + e.StackTrace);
			}
			return RA;
		}

		public static T Clamp<T>(T value, T min, T max)
			where T : System.IComparable<T>
		{
			T result = value;
			if (value.CompareTo(max) > 0)
				result = max;
			if (value.CompareTo(min) < 0)
				result = min;
			return result;
		}

		private void execute_fk()
		{
			double[] angles = { joints[0].angle, joints[1].angle, joints[2].angle, joints[3].angle, joints[4].angle, joints[5].angle };
			ForwardKinematics(angles);
		}

		private Color changeModelColor(Joint pJoint, Color newColor)
		{
			Model3DGroup models = ((Model3DGroup)pJoint.model);
			return changeModelColor(models.Children[0] as GeometryModel3D, newColor);
		}

		private Color changeModelColor(GeometryModel3D pModel, Color newColor)
		{
			if (pModel == null)
				return oldColor;

			Color previousColor = Colors.Black;

			MaterialGroup mg = (MaterialGroup)pModel.Material;
			if (mg.Children.Count > 0)
			{
				try
				{
					previousColor = ((EmissiveMaterial)mg.Children[0]).Color;
					((EmissiveMaterial)mg.Children[0]).Color = newColor;
					((DiffuseMaterial)mg.Children[1]).Color = newColor;
				}
				catch (Exception exc)
				{
					previousColor = oldColor;
				}
			}

			return previousColor;
		}

		private void ViewPort3D_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Point mousePos = e.GetPosition(viewPort3d);
			PointHitTestParameters hitParams = new PointHitTestParameters(mousePos);
			VisualTreeHelper.HitTest(viewPort3d, null, ResultCallback, hitParams);
		}

		private void ViewPort3D_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			HitTestResult result = VisualTreeHelper.HitTest(viewPort3d, e.GetPosition(viewPort3d));
			RayMeshGeometry3DHitTestResult mesh_result = result as RayMeshGeometry3DHitTestResult;
		}

		public HitTestResultBehavior ResultCallback(HitTestResult result)
		{
			RayHitTestResult rayResult = result as RayHitTestResult;
			if (rayResult != null)
			{
				RayMeshGeometry3DHitTestResult rayMeshResult = rayResult as RayMeshGeometry3DHitTestResult;

				if (rayMeshResult != null)
				{

				}
			}

			return HitTestResultBehavior.Continue;
		}

		public void StartInverseKinematics(object sender, RoutedEventArgs e)
		{
			if (RenderTimer.Enabled)
			{
				RenderTimer.Stop();
				movements = 0;
			}
			else
			{
				movements = 5000;
				RenderTimer.Start();
			}
		}

		public void RenderTimer_Tick(object sender, EventArgs e)
		{
			if (FromCommandQueue.IsChecked == true && lastRobotCommand == null && CommandQueue.Items.Count > 0)
			{
				string str = this.CommandQueue.Items[0].ToString();
				string[] separator = new string[] { ";" };
				string[] strArray = str.Split(separator, StringSplitOptions.None);
				N = int.Parse(strArray[0]);
				X1 = int.Parse(strArray[1]);
				Y1 = int.Parse(strArray[2]);
				T1 = int.Parse(strArray[3]);
				G1 = int.Parse(strArray[4]);
				L1 = int.Parse(strArray[5]);
				СommandExecution();
				CommandQueue.Items.RemoveAt(0);
				movements += 50000;
				TbStatus.Text = "1";
			}

			if (lastRobotCommand != null)
			{
				double[] angles = { joints[0].angle, joints[1].angle, joints[2].angle, joints[3].angle, joints[4].angle, joints[5].angle };

				switch (lastRobotCommand.Animation)
				{
					case 0:
						bool x = movementX(ref angles, lastRobotCommand);
						bool t = movementT(ref angles, lastRobotCommand);
						if (x && t)
						{
							lastRobotCommand.Animation++;
							lastRobotCommand.point = СhangeLengthVector(new Vector3D(joints[12].model.Bounds.Location.X, joints[12].model.Bounds.Location.Y, joints[12].model.Bounds.Location.Z), lastRobotCommand.PositionY);
						}
						break;
					case 1:
						angles = InverseKinematics(lastRobotCommand.point, angles);
						if (movements == 0)
						{
							lastRobotCommand.Animation++;
							movements = 50000;
							lastRobotCommand.point = new Vector3D(joints[12].model.Bounds.Location.X, joints[12].model.Bounds.Location.Y, joints[12].model.Bounds.Location.Z);
						}
						break;
					case 2:
						if (lastRobotCommand.GripperMode == 1)
						{
							angles = InverseKinematics(new Vector3D(lastRobotCommand.point.X, lastRobotCommand.point.Y, 470), angles);
						}
						else
						{
							movements = 0;
						}
						if (movements == 0)
						{
							lastRobotCommand.Animation++;
							movements = 50000;
						}
						break;
					case 3:
						if (lastRobotCommand.GripperMode == 0 || movementG(ref angles, lastRobotCommand))
							lastRobotCommand.Animation++;
						break;
					case 4:
						if (lastRobotCommand.GripperMode == 1)
						{
							angles = InverseKinematics(lastRobotCommand.point, angles);
						}
						else
						{
							movements = 0;
						}
						if (movements == 0)
						{
							lastRobotCommand.Animation++;
							movements = 50000;
						}
						break;
					default:
						movements = 1;
						break;
				}

				ForwardKinematics(angles);

				joints[0].angle = angles[0];
				joints[1].angle = angles[1];
				joints[2].angle = angles[2];
				joints[3].angle = angles[3];
				joints[4].angle = angles[4];
				joints[5].angle = angles[5];

				if (movements % 90 == 0)
					FillServomotors();
			}

			if ((--movements) <= 0)
			{
				lastRobotCommand = null;
				TbStatus.Text = "0";
			}
		}

		public double[] InverseKinematics(Vector3D target, double[] angles)
		{
			if (DistanceFromTarget(target, angles) < DistanceThreshold)
			{
				movements = 0;
				return angles;
			}

			double[] oldAngles = { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
			angles.CopyTo(oldAngles, 0);
			for (int i = 1; i < 3; i++)
			{
				double gradient = PartialGradient(target, angles, i);
				angles[i] -= LearningRate * gradient;

				angles[i] = Clamp(angles[i], joints[i].angleMin, joints[i].angleMax);

				angles[3] = 90 - angles[2] - angles[1];

				if (DistanceFromTarget(target, angles) < DistanceThreshold || checkAngles(oldAngles, angles))
				{
					movements = 0;
					return angles;
				}
			}

			return angles;
		}

		public bool checkAngles(double[] oldAngles, double[] angles)
		{
			for (int i = 0; i <= 5; i++)
			{
				if (oldAngles[i] != angles[i])
					return false;
			}

			return true;
		}

		public double PartialGradient(Vector3D target, double[] angles, int i)
		{

			double angle = angles[i];

			double f_x = DistanceFromTarget(target, angles);

			angles[i] += SamplingDistance;
			double f_x_plus_d = DistanceFromTarget(target, angles);

			double gradient = (f_x_plus_d - f_x) / SamplingDistance;

			angles[i] = angle;

			return gradient;
		}

		public double DistanceFromTarget(Vector3D target, double[] angles)
		{
			Vector3D point = ForwardKinematics(angles);
			return Math.Sqrt(Math.Pow((point.X - target.X), 2.0) + Math.Pow((point.Y - target.Y), 2.0) + Math.Pow((point.Z - target.Z), 2.0));
		}

		public Vector3D ForwardKinematics(double[] angles)
		{
			FB = new Transform3DGroup();
			T = new TranslateTransform3D(-2500, 0, 0);
			FB.Children.Add(T);

			F1 = new Transform3DGroup();
			T = new TranslateTransform3D(0, 0, 0);
			R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[0].rotAxisX, joints[0].rotAxisY, joints[0].rotAxisZ), angles[0]), new Point3D(joints[0].rotPointX, joints[0].rotPointY, joints[0].rotPointZ));
			F1.Children.Add(R);
			F1.Children.Add(T);
			F1.Children.Add(FB);

			F2 = new Transform3DGroup();
			T = new TranslateTransform3D(0, 0, 0);
			R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[1].rotAxisX, joints[1].rotAxisY, joints[1].rotAxisZ), angles[1]), new Point3D(joints[1].rotPointX, joints[1].rotPointY, joints[1].rotPointZ));
			F2.Children.Add(T);
			F2.Children.Add(R);
			F2.Children.Add(F1);

			F3 = new Transform3DGroup();
			T = new TranslateTransform3D(0, 0, 0);
			R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[2].rotAxisX, joints[2].rotAxisY, joints[2].rotAxisZ), angles[2]), new Point3D(joints[2].rotPointX, joints[2].rotPointY, joints[2].rotPointZ));
			F3.Children.Add(T);
			F3.Children.Add(R);
			F3.Children.Add(F2);

			F4 = new Transform3DGroup();
			T = new TranslateTransform3D(0, 0, 0);
			R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[3].rotAxisX, joints[3].rotAxisY, joints[3].rotAxisZ), angles[3]), new Point3D(joints[3].rotPointX, joints[3].rotPointY, joints[3].rotPointZ));
			F4.Children.Add(T);
			F4.Children.Add(R);
			F4.Children.Add(F3);

			F5 = new Transform3DGroup();
			T = new TranslateTransform3D(0, 0, 0);
			R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[4].rotAxisX, joints[4].rotAxisY, joints[4].rotAxisZ), angles[4]), new Point3D(joints[4].rotPointX, joints[4].rotPointY, joints[4].rotPointZ));
			F5.Children.Add(T);
			F5.Children.Add(R);
			F5.Children.Add(F4);

			F6L = new Transform3DGroup();
			T = new TranslateTransform3D(0, 0, 0);
			R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[5].rotAxisX, joints[5].rotAxisY, joints[5].rotAxisZ), angles[5]), new Point3D(joints[5].rotPointX, joints[5].rotPointY, joints[5].rotPointZ));
			F6L.Children.Add(T);
			F6L.Children.Add(R);
			F6L.Children.Add(F5);

			F6R = new Transform3DGroup();
			T = new TranslateTransform3D(0, 0, 0);
			R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[6].rotAxisX, joints[6].rotAxisY, joints[6].rotAxisZ), -angles[5]), new Point3D(joints[6].rotPointX, joints[6].rotPointY, joints[6].rotPointZ));
			F6R.Children.Add(T);
			F6R.Children.Add(R);
			F6R.Children.Add(F5);

			F7 = new Transform3DGroup();
			T = new TranslateTransform3D(168, -2854, 3772);
			F7.Children.Add(T);
			F7.Children.Add(F5);

			FO1 = new Transform3DGroup();
			if (Boxs[0].g == 1)
			{
				FO1.Children.Add(Boxs[0].R);
				FO1.Children.Add(F7);
				FO1.Children.Add(new TranslateTransform3D(0, 0, -joints[8].model.Bounds.SizeZ + 150));
			}
			else
			{
				FO1.Children.Add(Boxs[0].R);
				FO1.Children.Add(Boxs[0].T);
			}

			FO2 = new Transform3DGroup();
			if (Boxs[1].g == 1)
			{
				FO2.Children.Add(Boxs[1].R);
				FO2.Children.Add(F7);
				FO2.Children.Add(new TranslateTransform3D(0, 0, -joints[9].model.Bounds.SizeZ + 150));
			}
			else
			{
				FO2.Children.Add(Boxs[1].R);
				FO2.Children.Add(Boxs[1].T);
			}

			FO3 = new Transform3DGroup();
			if (Boxs[2].g == 1)
			{
				FO3.Children.Add(Boxs[2].R);
				FO3.Children.Add(F7);
				FO3.Children.Add(new TranslateTransform3D(0, 0, -joints[10].model.Bounds.SizeZ + 150));
			}
			else
			{
				FO3.Children.Add(Boxs[2].R);
				FO3.Children.Add(Boxs[2].T);
			}

			FO4 = new Transform3DGroup();
			if (Boxs[3].g == 1)
			{
				FO4.Children.Add(Boxs[3].R);
				FO4.Children.Add(F7);
				FO4.Children.Add(new TranslateTransform3D(0, 0, -joints[11].model.Bounds.SizeZ + 150));
			}
			else
			{
				FO4.Children.Add(Boxs[3].R);
				FO4.Children.Add(Boxs[3].T);
			}

			joints[0].model.Transform = F1;
			joints[1].model.Transform = F2;
			joints[2].model.Transform = F3;
			joints[3].model.Transform = F4;
			joints[4].model.Transform = F5;
			joints[5].model.Transform = F6L;
			joints[6].model.Transform = F6R;
			joints[7].model.Transform = FB;
			joints[8].model.Transform = FO1;
			joints[9].model.Transform = FO2;
			joints[10].model.Transform = FO3;
			joints[11].model.Transform = FO4;
			joints[12].model.Transform = F7;

			return new Vector3D(joints[12].model.Bounds.Location.X, joints[12].model.Bounds.Location.Y, joints[12].model.Bounds.Location.Z);
		}

		public static double s1 = 0, m11 = 0, m12 = 0, m13 = 0, m14 = 0, m15 = 0, m16 = 0, l11 = 0, l12 = 0, l13 = 0, l14 = 0, l15 = 0, l16 = 0, t11 = 0, t12 = 0, t13 = 0, t14 = 0, t15 = 0, t16 = 0, n = -1;

		public static double N = 0, X1 = 0, Y1 = 0, T1 = 0, L1 = 0;
		public static int G1 = 0;

		private void Button_Clear(object sender, RoutedEventArgs e)
		{
			joints[0].angle = 0;
			joints[1].angle = 0;
			joints[2].angle = 0;
			joints[3].angle = 90;
			joints[4].angle = 0;
			joints[5].angle = 0;

			execute_fk();
		}

		double NewPositionX = 0, NewPositionY = 0, NewRotationT = 0, NewGripperPos = 0;
		int AnimationGripper = 0, NewGripper = 0;

		private void CommandQueueClear_Click(object sender, RoutedEventArgs e)
		{
			CommandQueue.Items.Clear();
		}

		private void FromCommandQueue_Checked(object sender, RoutedEventArgs e)
		{
			if (FromCommandQueue.IsChecked == true)
			{
				movements = 5000;
				TbStatus.Text = "1";
			}
		}

		private void LastExecutedCommand()
		{
			TbMR2n.Text = N.ToString();
			TbMR2x.Text = X1.ToString();
			TbMR2y.Text = Y1.ToString();
			TbMR2t.Text = T1.ToString();
			TbMR2g.Text = G1.ToString();
			TbMR2l.Text = L1.ToString();
		}

		private void СommandExecution()
		{
			if (N > n)
			{
				LastExecutedCommand();
				n = N;
				TbLastN.Text = n.ToString();
				switch (L1)
				{
					case 0:
						Lamp.Background = Brushes.Red;
						LampText.Foreground = Brushes.WhiteSmoke;
						break;
					case 1:
						Lamp.Background = Brushes.Blue;
						LampText.Foreground = Brushes.WhiteSmoke;
						break;
					case 2:
						Lamp.Background = Brushes.Green;
						LampText.Foreground = Brushes.WhiteSmoke;
						break;
					case 3:
						Lamp.Background = Brushes.Yellow;
						LampText.Foreground = Brushes.Black;
						break;
					default:
						Lamp.Background = Brushes.Black;
						LampText.Foreground = Brushes.WhiteSmoke;
						break;
				};
				Movement(X1, Y1, T1, G1);
			}
		}

		public void Movement(double PositionX, double PositionY, double RotationT, int Gripper)
		{
			if (Gripper == 2)
			{
				ParkingZone = true;
			}
			else if (Gripper == 3)
			{
				ParkingZone = false;
			}
			if (ParkingZone == false)
			{
				NewPositionX = PositionX;
				NewPositionY = PositionY;
				NewRotationT = RotationT;
				AnimationGripper = 0;
				if (NewGripper != Gripper)
				{
					NewGripper = Gripper;
					AnimationGripper = 1;
					if (NewGripper == 0)
					{
						NewGripperPos = joints[5].angleMax;
					}
					else if (NewGripper == 1)
					{
						NewGripperPos = joints[5].angleMin;
					}
					else
					{
						AnimationGripper = 0;
					}
				}

				lastRobotCommand = new RobotCommand(NewPositionX, NewPositionY, NewRotationT, AnimationGripper, NewGripperPos);
				joints[5].angleMin = 0;
			}
			else
			{
				NewPositionX = -45;
				AnimationGripper = 0;
				lastRobotCommand = new RobotCommand(NewPositionX, NewPositionY, NewRotationT, AnimationGripper, NewGripperPos);
				joints[5].angleMin = 0;
			}
		}

		private bool movementX(ref double[] angles, RobotCommand robotCommand)
		{

			if (angles[0] < robotCommand.PositionX && angles[0] < joints[0].angleMax)
			{
				angles[0]++;
			}
			else if (angles[0] > robotCommand.PositionX && angles[0] > joints[0].angleMin)
			{
				angles[0]--;
			}
			else
			{
				return true;
			}

			return false;
		}

		private bool movementT(ref double[] angles, RobotCommand robotCommand)
		{

			if (angles[4] < robotCommand.RotationT && angles[4] < joints[4].angleMax)
			{
				angles[4]++;
			}
			else if (angles[4] > robotCommand.RotationT && angles[4] > joints[4].angleMin)
			{
				angles[4]--;
			}
			else
			{
				return true;
			}

			return false;
		}

		private bool movementG(ref double[] angles, RobotCommand robotCommand)
		{
			if (angles[5] < robotCommand.GripperPos && angles[5] < joints[5].angleMax)
			{
				angles[5]++;
				for (int i = 0; i < 4; i++)
				{
					if (Boxs[i].g == 1)
					{
						joints[5].angleMin = 0;
						joints[6].angleMin = 0;
						Boxs[i].g = 0;
						Boxs[i].T = new TranslateTransform3D(joints[8 + i].model.Bounds.Location.X + joints[8 + i].model.Bounds.SizeX / 2, joints[8 + i].model.Bounds.Location.Y + joints[8 + i].model.Bounds.SizeY / 2, joints[8 + i].model.Bounds.SizeZ + 60);
						Boxs[i].R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), joints[0].angle + joints[4].angle - 180), new Point3D(0, 0, 0));
						break;
					}
				}
			}
			else if (angles[5] > robotCommand.GripperPos && angles[5] > joints[5].angleMin)
			{
				angles[5]--;
				if (Boxs[0].g == 0 && Boxs[1].g == 0 && Boxs[2].g == 0 && Boxs[3].g == 0)
				{
					for (int i = 0; i < 4; i++)
					{
						if (joints[8 + i].model.Bounds.IntersectsWith(joints[5].model.Bounds) && joints[8 + i].model.Bounds.IntersectsWith(joints[6].model.Bounds))
						{
							joints[5].angleMin = 34;
							joints[6].angleMin = -34;
							Boxs[i].g = 1;
							Boxs[i].T = new TranslateTransform3D(0, 0, joints[8 + i].model.Bounds.SizeZ - 150);
							Boxs[i].R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90), new Point3D(0, 0, 0));
							break;
						}
					}
				}
			}
			else
			{
				return true;
			}

			return false;
		}

		public void FillServomotors()
		{
			m11 = Math.Round(joints[0].angle);
			m12 = Math.Round(joints[1].angle);
			m13 = Math.Round(joints[2].angle);
			m14 = Math.Round(joints[3].angle);
			m15 = Math.Round(joints[4].angle);
			m16 = Math.Round(joints[5].angle);

			Random rnd = new Random();

			l11 = rnd.Next(111, 148);
			l12 = rnd.Next(111, 148);
			l13 = rnd.Next(111, 148);
			l14 = rnd.Next(111, 148);
			l15 = rnd.Next(90, 120);
			l16 = rnd.Next(90, 120);

			t11 = rnd.Next(240, 250);
			t12 = rnd.Next(240, 250);
			t13 = rnd.Next(240, 250);
			t14 = rnd.Next(240, 250);
			t15 = rnd.Next(240, 250);
			t16 = rnd.Next(240, 250);

			OutputServomotors();
		}

		public void OutputServomotors()
		{
			TbM11.Text = m11.ToString();
			TbM12.Text = m12.ToString();
			TbM13.Text = m13.ToString();
			TbM14.Text = m14.ToString();
			TbM15.Text = m15.ToString();
			TbM16.Text = m16.ToString();

			TbL11.Text = l11.ToString();
			TbL12.Text = l12.ToString();
			TbL13.Text = l13.ToString();
			TbL14.Text = l14.ToString();
			TbL15.Text = l15.ToString();
			TbL16.Text = l16.ToString();

			TbT11.Text = t11.ToString();
			TbT12.Text = t12.ToString();
			TbT13.Text = t13.ToString();
			TbT14.Text = t14.ToString();
			TbT15.Text = t15.ToString();
			TbT16.Text = t16.ToString();
		}

		private void ButtonEnforcement_Click(object sender, RoutedEventArgs e)
		{
			N = int.Parse(TbMR1n.Text);
			X1 = int.Parse(TbMR1x.Text);
			Y1 = int.Parse(TbMR1y.Text);
			T1 = int.Parse(TbMR1t.Text);
			G1 = int.Parse(TbMR1g.Text);
			L1 = int.Parse(TbMR1l.Text);
			CommandQueue.Items.Add(N + ";" + X1 + ";" + Y1 + ";" + T1 + ";" + G1 + ";" + L1 + ";");
		}

		private double DegreeToRadian(double angle)
		{
			return Math.PI * angle / 180.0;
		}

		private double RadianToDegree(double angle)
		{
			return angle * (180.0 / Math.PI);
		}

		private double AngleBetweenVectors(Vector3D target1, Vector3D target2)
		{
			Vector3D V1 = new Vector3D(target1.X + 2500, target1.Y, target1.Z);
			Vector3D V2 = new Vector3D(target2.X + 2500, target2.Y, target1.Z);
			return RadianToDegree(Math.Acos((V1.X * V2.X + V1.Y * V2.Y) / (Math.Sqrt(Math.Pow(V1.X, 2) + Math.Pow(V1.Y, 2)) * Math.Sqrt(Math.Pow(V2.X, 2) + Math.Pow(V2.Y, 2)))));
		}

		private Vector3D RotateAdditionalVectorAngle()
		{
			double Ax = -2500, Ay = 0;
			double Bx = -2332, By = 0;
			double ABx = Bx - Ax;
			double ABy = By - Ay;
			double ACx = ABx * Math.Cos(DegreeToRadian(joints[0].angle)) - ABy * Math.Sin(DegreeToRadian(joints[0].angle));
			double ACy = ABx * Math.Sin(DegreeToRadian(joints[0].angle)) + ABy * Math.Cos(DegreeToRadian(joints[0].angle));
			double Cx = Ax + ACx;
			double Cy = Ay + ACy;
			return new Vector3D(Cx, Cy, 0);
		}

		private Vector3D СhangeLengthVector(Vector3D target, double distance)
		{
			double Ax = RotateAdditionalVectorAngle().X, Ay = RotateAdditionalVectorAngle().Y;
			double Bx = target.X, By = target.Y;
			double ABx = Bx - Ax;
			double ABy = By - Ay;
			double ABm = Math.Sqrt(Math.Pow(ABx, 2) + Math.Pow(ABy, 2));
			double AB0x = ABx / ABm;
			double AB0y = ABy / ABm;
			ABx = AB0x * (distance + 1653);
			ABy = AB0y * (distance + 1653);
			target.X = Bx = Ax + ABx;
			target.Y = By = Ay + ABy;
			return target;
		}

		public void TbLogRobotSimulator_Add(string newline)
		{
			TbLogRobotSimulator.Text += "\r\n\r\n" + newline;
		}

		private void BtnRefresh_Click(object sender, RoutedEventArgs e)
		{
			Boxs[0] = new box(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), -45), new Point3D(0, 0, 0)), new TranslateTransform3D(-700, -2600, joints[8].model.Bounds.SizeZ + 60));
			Boxs[1] = new box(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 45), new Point3D(0, 0, 0)), new TranslateTransform3D(700, -1240, joints[9].model.Bounds.SizeZ + 60));
			Boxs[2] = new box(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 45), new Point3D(0, 0, 0)), new TranslateTransform3D(560, -3840, joints[10].model.Bounds.SizeZ + 60));
			Boxs[3] = new box(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), -45), new Point3D(0, 0, 0)), new TranslateTransform3D(1960, -2500, joints[11].model.Bounds.SizeZ + 60));
			double[] angles = { joints[0].angle, joints[1].angle, joints[2].angle, joints[3].angle, joints[4].angle, joints[5].angle = 70 };
			ForwardKinematics(angles);
		}

		private void BtnClearAll_Click(object sender, RoutedEventArgs e)
		{
			lastRobotCommand = null;
			Boxs[0] = new box(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), -45), new Point3D(0, 0, 0)), new TranslateTransform3D(-700, -2600, joints[8].model.Bounds.SizeZ + 60));
			Boxs[1] = new box(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 45), new Point3D(0, 0, 0)), new TranslateTransform3D(700, -1240, joints[9].model.Bounds.SizeZ + 60));
			Boxs[2] = new box(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 45), new Point3D(0, 0, 0)), new TranslateTransform3D(560, -3840, joints[10].model.Bounds.SizeZ + 60));
			Boxs[3] = new box(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), -45), new Point3D(0, 0, 0)), new TranslateTransform3D(1960, -2500, joints[11].model.Bounds.SizeZ + 60));
			double[] angles = { joints[0].angle = 0, joints[1].angle = 0, joints[2].angle = 0, joints[3].angle = 90, joints[4].angle = 0, joints[5].angle = 70 };
			ForwardKinematics(angles);
			CommandQueue.Items.Clear();
			TbStatus.Text = TbLastN.Text = "0";
			TbM11.Text = TbM12.Text = TbM13.Text = TbM14.Text = TbM15.Text = TbM16.Text = "0";
			TbT11.Text = TbT12.Text = TbT13.Text = TbT14.Text = TbT15.Text = TbT16.Text = "0";
			TbL11.Text = TbL12.Text = TbL13.Text = TbL14.Text = TbL15.Text = TbL16.Text = "0";
			TbMR1n.Text = TbMR1x.Text = TbMR1y.Text = TbMR1t.Text = TbMR1g.Text = TbMR1l.Text = "0";
			TbMR2n.Text = TbMR2x.Text = TbMR2y.Text = TbMR2t.Text = TbMR2g.Text = TbMR2l.Text = "0";
			TbLogRobotSimulator.Text = "";
		}

		private void Tb_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			int val;
			if (!Int32.TryParse(e.Text, out val))
			{
				e.Handled = true;
			}
		}

		private void Tb_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Space)
			{
				e.Handled = true;
			}
		}

		private void TbLog_TextChanged(object sender, TextChangedEventArgs e)
		{
			((TextBox)sender).SelectionStart = ((TextBox)sender).Text.Length;
			((TextBox)sender).ScrollToEnd();
		}

		private void BtnLoadSettings_Click(object sender, RoutedEventArgs e)
		{
			string path = "UserParameters.txt";
			LbUserParameters.Items.Clear();
			try
			{
				string[] strArray = File.ReadAllLines(path);
				foreach (string str in strArray)
				{
					if (str.Trim() != "")
					{
						LbUserParameters.Items.Add(str.Trim());
					}
				}
				LbUserParameters.SelectedIndex = 0;
			}
			catch (Exception exc)
			{
				TbLogSettings_Add("Не удаётся считать параметры пользователей");
			}
		}

		private void BtnSetSettings_Click(object sender, RoutedEventArgs e)
		{
			int selectedIndex = this.LbUserParameters.SelectedIndex;
			if (selectedIndex >= 0)
			{
				string str = this.LbUserParameters.Items[selectedIndex].ToString();
				string[] separator = new string[] { ";" };
				try
				{
					string[] strArray2 = str.Split(separator, StringSplitOptions.None);
					if (strArray2.Length >= 6)
					{
						TbServer.Text = strArray2[1];
						TbKey.Text = strArray2[2];

						CbSecurity.IsChecked = bool.Parse(strArray2[3]);

						TbThing.Text = strArray2[4];
						TbService.Text = strArray2[5];
					}
				}
				catch (Exception exc)
				{
					TbLogSettings_Add("Ошибка загрузки параметров пользователя");
				}
			}
			else
			{
				TbLogSettings_Add("Ошибка загрузки параметров пользователя");
			}
		}

		public void TbLogSettings_Add(string _newline)
		{
			TbLogSettings.Text += "\r\n\r\n" + _newline;
		}

		Worker _RobotSimulator;

		public void RobotSimulator_WorkCompleted(bool cancelled)
		{
			Action action = () =>
			{
				TbLogSettings_Add("Отправка данных остановлена");
				BtnConnect.Content = "Подключиться";
			};

			this.Dispatcher.Invoke(action);
		}

		private void RobotSimulator_ProcessChanged()
		{
			Action action = () =>
			{
				try
				{
					string security = "http";
					if (CbSecurity.IsChecked == true)
					{
						security += "s";
					}
					_RobotSimulator.refresh_time = (int)NudRefreshTime.Value * 1000;
					var httpWebRequest = (HttpWebRequest)WebRequest.Create(security + "://" + TbServer.Text + "/Thingworx/Things/" + TbThing.Text + "/Services/" + TbService.Text + "?method=post&appKey=" + TbKey.Text + "&s1=" + s1 + "&m11=" + m11 + "&m12=" + m12 + "&m13=" + m13 + "&m14=" + m14 + "&m15=" + m15 + "&m16=" + m16 + "&l11=" + l11 + "&l12=" + l12 + "&l13=" + l13 + "&l14=" + l14 + "&l15=" + l15 + "&l16=" + l16 + "&t11=" + t11 + "&t12=" + t12 + "&t13=" + t13 + "&t14=" + t14 + "&t15=" + t15 + "&t16=" + t16 + "&n=" + n);
					httpWebRequest.ContentType = "application/json";
					httpWebRequest.ContentType = "application/json";
					httpWebRequest.Accept = "application/json";
					httpWebRequest.Method = "POST";
					var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
					using (var streamReader = new StreamReader(httpResponse.GetResponseStream(), Encoding.ASCII))
					{
						var result = streamReader.ReadToEnd();
						RootObjectRobotSimulator answer = JsonConvert.DeserializeObject<RootObjectRobotSimulator>(result);

						TbLogRobotSimulator_Add("Полученные данные:  " + result + "\r\nОтправленные данные:  {\"s1\":" + s1 + ",\"m11\":" + m11 + ",\"m12\":" + m12 + ",\"m13\":" + m13 + ",\"m14\":" + m14 + ",\"m15\":" + m15 + ",\"m16\":" + m16 + ",\"l11\":" + l11 + ",\"l12\":" + l12 + ",\"l13\":" + l13 + ",\"l14\":" + l14 + ",\"l15\":" + l15 + ",\"l16\":" + l16 + ",\"t11\":" + t11 + ",\"t12\":" + t12 + ",\"t13\":" + t13 + ",\"t14\":" + t14 + ",\"t15\":" + t15 + ",\"t16\":" + t16 + "}");
						N = answer.N;
						X1 = answer.X1;
						Y1 = answer.Y1;
						T1 = answer.T1;
						G1 = answer.G1;
						L1 = answer.L1;
						CommandQueue.Items.Add(N + ";" + X1 + ";" + Y1 + ";" + T1 + ";" + G1 + ";" + L1 + ";");
					}
				}
				catch (WebException ex)
				{
					TbLogSettings_Add(ex.Message);
					_RobotSimulator.Cancel();
				}
			};

			this.Dispatcher.Invoke(action);
		}

		private void BtnConnect_Click(object sender, RoutedEventArgs e)
		{
			if (BtnConnect.Content.ToString() == "Подключиться")
			{
				bool error = false;
				if (TbServer.Text == "")
				{
					TbLogSettings_Add("Ошибка! Сервер не указан");
					error = true;
				}
				if (TbKey.Text == "")
				{
					TbLogSettings_Add("Ошибка! AppKey не указан");
					error = true;
				}
				if (TbThing.Text == "")
				{
					TbLogSettings_Add("Ошибка! Вещь не указана");
					error = true;
				}
				if (TbService.Text == "")
				{
					TbLogSettings_Add("Ошибка! Сервис не указан");
					error = true;
				}
				if (error)
				{
					return;
				}
				_RobotSimulator = new Worker();
				_RobotSimulator.ProcessChanged += RobotSimulator_ProcessChanged;
				_RobotSimulator.WorkCompleted += RobotSimulator_WorkCompleted;

				BtnConnect.Content = "Отключиться";

				TbLogSettings_Add("Отправка данных начата");

				Thread thread = new Thread(_RobotSimulator.Work);
				thread.Start();
			}
			else
			{
				if (_RobotSimulator != null)
				{
					_RobotSimulator.Cancel();
					BtnConnect.Content = "Подключиться";
				}
			}
		}
	}
}
