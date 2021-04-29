using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using System.Data.Entity;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MyKindergarden.Windows;

namespace MyKindergarden.ViewModels
{
	public class ClientLogInVM : ViewModelBase
	{
		public ClientLogInVM()
		{
			context = new MyKindergardenEntities();
		}
		MyKindergardenEntities context;
		string username = "";
		string password = "";
		RelayCommand logInCommand;
		Client user;
		bool logInStatus;

		public string Username
		{
			get => username;
			set
			{
				username = value;
				OnPropertyChanged();
			}
		}
		public string Password
		{
			get => password;
			set
			{
				password = value;
				OnPropertyChanged();
			}
		}
		public Client User => user;
		public bool LogInStatus => logInStatus;
		public RelayCommand LogInCommand
		{
			get
			{
				return logInCommand ??
					(logInCommand = new RelayCommand(obj =>
					{
						user = context.Clients
							.Where(a => a.IsActive && a.Username == username && a.Password == password)
							.FirstOrDefault();
						logInStatus = user != null;
						if (!logInStatus)
						{
							MessageBox.Show("Аккаунт не существует или отключен", "Ошибка", MessageBoxButton.OK);
						}

					}));
			}
		}

	}
	public class CurrentUserVM
	{
		public CurrentUserVM(Client c)
		{
			user = c;
		}
		Client user;

		public Client User => user;
		public bool IsAdmin => user.ClientType_Id == 0;
	}
}
