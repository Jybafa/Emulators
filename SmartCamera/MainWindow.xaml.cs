using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartCamera
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

		public static string code = "0";

		private void BtnCode0_Click(object sender, RoutedEventArgs e)
		{
			code = TbProductCode.Text = "0";
		}

		private void BtnCode1_Click(object sender, RoutedEventArgs e)
		{
			code = TbProductCode.Text = TbCode1.Text;
		}

		private void BtnCode2_Click(object sender, RoutedEventArgs e)
		{
			code = TbProductCode.Text = TbCode2.Text;
		}

		private void BtnCode3_Click(object sender, RoutedEventArgs e)
		{
			code = TbProductCode.Text = TbCode3.Text;
		}

		private void BtnCode4_Click(object sender, RoutedEventArgs e)
		{
			code = TbProductCode.Text = TbCode4.Text;
		}

		private void BtnClear_Click(object sender, RoutedEventArgs e)
		{
			code = TbProductCode.Text = "";
		}

		public void TbLogSmartCamera_Add(string newline)
		{
			TbLogSmartCamera.Text += "\r\n\r\n" + newline;
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
				TbLogSmartCamera_Add("Не удаётся считать параметры пользователей");
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
					TbLogSmartCamera_Add("Ошибка загрузки параметров пользователя");
				}
			}
			else
			{
				TbLogSmartCamera_Add("Ошибка загрузки параметров пользователя");
			}
		}

		Worker _SmartCamera;

		public void SmartCamera_WorkCompleted(bool cancelled)
		{
			Action action = () =>
			{
				TbLogSmartCamera_Add("Отправка данных остановлена");
				BtnConnect.Content = "Подключиться";
			};

			this.Dispatcher.Invoke(action);
		}

		private void SmartCamera_ProcessChanged()
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
					_SmartCamera.refresh_time = (int)NudRefreshTime.Value * 1000;
					var httpWebRequest = (HttpWebRequest)WebRequest.Create(security + "://" + TbServer.Text + "/Thingworx/Things/" + TbThing.Text + "/Services/" + TbService.Text + "?method=post&appKey=" + TbKey.Text + "&code=" + code);
					httpWebRequest.ContentType = "application/json";
					httpWebRequest.Method = "POST";
					var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
					using (var streamReader = new StreamReader(httpResponse.GetResponseStream(), Encoding.ASCII))
					{
						TbLogSmartCamera_Add("Отправленные данные:  {\"code\":" + code + "}");
					}
				}
				catch (WebException ex)
				{
					TbLogSmartCamera_Add(ex.Message);
					_SmartCamera.Cancel();
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
					TbLogSmartCamera_Add("Ошибка! Сервер не указан");
					error = true;
				}
				if (TbKey.Text == "")
				{
					TbLogSmartCamera_Add("Ошибка! AppKey не указан");
					error = true;
				}
				if (TbThing.Text == "")
				{
					TbLogSmartCamera_Add("Ошибка! Вещь не указана");
					error = true;
				}
				if (TbService.Text == "")
				{
					TbLogSmartCamera_Add("Ошибка! Сервис не указан");
					error = true;
				}
				if (error)
				{
					return;
				}
				_SmartCamera = new Worker();
				_SmartCamera.ProcessChanged += SmartCamera_ProcessChanged;
				_SmartCamera.WorkCompleted += SmartCamera_WorkCompleted;

				BtnConnect.Content = "Отключиться";

				TbLogSmartCamera_Add("Отправка данных начата");

				Thread thread = new Thread(_SmartCamera.Work);
				thread.Start();
			}
			else
			{
				if (_SmartCamera != null)
				{
					_SmartCamera.Cancel();
					BtnConnect.Content = "Подключиться";
				}
			}
		}
	}
}
