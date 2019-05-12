using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SafetySystem
{
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>

	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		public static string d1 = "0", d2 = "0", d3 = "0";

		private void BtnTest1_Click(object sender, RoutedEventArgs e)
		{
			d1 = TbRange1.Text = "320";
			d2 = TbRange2.Text = "320";
			d3 = TbRange3.Text = "320";
		}

		private void BtnTest2_Click(object sender, RoutedEventArgs e)
		{
			d1 = TbRange1.Text = "50";
			d2 = TbRange2.Text = "50";
			d3 = TbRange3.Text = "50";
		}

		private void BtnTest3_Click(object sender, RoutedEventArgs e)
		{
			d1 = TbRange1.Text = "50";
			d2 = TbRange2.Text = "180";
			d3 = TbRange3.Text = "180";
		}

		private void BtnTest4_Click(object sender, RoutedEventArgs e)
		{
			d1 = TbRange1.Text = "200";
			d2 = TbRange2.Text = "120";
			d3 = TbRange3.Text = "70";
		}

		public void TbLogSafetySystem_Add(string newline)
		{
			TbLogSafetySystem.Text += "\r\n\r\n" + newline;
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
				TbLogSafetySystem_Add("Не удаётся считать параметры пользователей");
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
					TbLogSafetySystem_Add("Ошибка загрузки параметров пользователя");
				}
			}
			else
			{
				TbLogSafetySystem_Add("Ошибка загрузки параметров пользователя");
			}
		}

		Worker _SafetySystem;

		public void SafetySystem_WorkCompleted(bool cancelled)
		{
			Action action = () =>
			{
				TbLogSafetySystem_Add("Отправка данных остановлена");
				BtnConnect.Content = "Подключиться";
			};

			this.Dispatcher.Invoke(action);
		}

		private void SafetySystem_ProcessChanged()
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
					_SafetySystem.refresh_time = (int)NudRefreshTime.Value * 1000;
					var httpWebRequest = (HttpWebRequest)WebRequest.Create(security + "://" + TbServer.Text + "/Thingworx/Things/" + TbThing.Text + "/Services/" + TbService.Text + "?method=post&appKey=" + TbKey.Text + "&d1=" + d1 + "&d2=" + d2 + "&d3=" + d3);
					httpWebRequest.ContentType = "application/json";
					httpWebRequest.Accept = "application/json";
					httpWebRequest.Method = "POST";
					var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
					using (var streamReader = new StreamReader(httpResponse.GetResponseStream(), Encoding.ASCII))
					{
						TbLogSafetySystem_Add("Отправленные данные:  " + "{\"d1\":" + d1 + ",\"d2\":" + d2 + ",\"d3\":" + d3+"}");
					}
				}
				catch (WebException ex)
				{
					TbLogSafetySystem_Add(ex.Message);
					_SafetySystem.Cancel();
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
					TbLogSafetySystem_Add("Ошибка! Сервер не указан");
					error = true;
				}
				if (TbKey.Text == "")
				{
					TbLogSafetySystem_Add("Ошибка! AppKey не указан");
					error = true;
				}
				if (TbThing.Text == "")
				{
					TbLogSafetySystem_Add("Ошибка! Вещь не указана");
					error = true;
				}
				if (TbService.Text == "")
				{
					TbLogSafetySystem_Add("Ошибка! Сервис не указан");
					error = true;
				}
				if (error)
				{
					return;
				}
				_SafetySystem = new Worker();
				_SafetySystem.ProcessChanged += SafetySystem_ProcessChanged;
				_SafetySystem.WorkCompleted += SafetySystem_WorkCompleted;

				BtnConnect.Content = "Отключиться";

				TbLogSafetySystem_Add("Отправка данных начата");

				Thread thread = new Thread(_SafetySystem.Work);
				thread.Start();
			}
			else
			{
				if (_SafetySystem != null)
				{
					_SafetySystem.Cancel();
					BtnConnect.Content = "Подключиться";
				}
			}
		}
	}
}