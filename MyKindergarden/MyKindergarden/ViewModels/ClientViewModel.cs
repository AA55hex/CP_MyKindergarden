using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data.Entity;
using System.Windows.Data;
using MyKindergarden.Windows;
namespace MyKindergarden.ViewModels
{

	public class ClientProfileVM
	{
		public ClientProfileVM(Client mdl, CurrentUserVM usr)
		{
			model = mdl;
			user = usr;
		}

		Client model;
		CurrentUserVM user;

		public CurrentUserVM User => user;
		public Client Model => model;
		public string ActiveStatus => model.IsActive ? "Активен" : "Отключен";
		public string FIO => model.LastName + " " + model.FirstName[0] + ". " + model.MiddleName[0] + ".";
		public Visibility AdminDataStatus => User.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
	}

	public class ClientListVM : ModelList<Client>
	{
		public ClientListVM(CurrentUserVM usr) : base(usr)
		{
			Load();
			Filter.Property = new ClientFilterVM(Collection);
			Commands = new ClientListCommandsVM(this);
		}

		public PropertyViewModel<ClientFilterVM> Filter { get; private set; } = new PropertyViewModel<ClientFilterVM>();
		public ClientListCommandsVM Commands { get; private set; }


		void Load() => ReFresh();
		public void ReFresh()
		{
			context?.Dispose();
			context = new MyKindergardenEntities();
			context.Clients.Where(a => a.ClientType_Id == 1).Load();
			Collection.Source = context.Clients.Local;
		}
	}

	public class ClientListCommandsVM : ListCommandsVM<ClientListVM>
	{
		public ClientListCommandsVM(ClientListVM lst) : base(lst) { }
		public override RelayCommand DeleteCommand
		{
			get
			{
				return deleteCommand ??
					(deleteCommand = new RelayCommand(obj =>
					{
						if (MessageBox.Show("Удалить информацию о данном воспитателе?", "Удаление", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
						{
							models.Context.Clients.Remove(models.SelectedItem);
							models.SelectedItem = null;
							models.Context.SaveChanges();
						}
					}, a => models.SelectedItem != null && models.User.IsAdmin));
			}
		}
		public override RelayCommand AddCommand
		{
			get
			{
				return addCommand ??
					(addCommand = new RelayCommand(obj =>
					{
						Client client = new Client()
						{
							FirstName = "",
							MiddleName = "",
							LastName = "",
							Username = "",
							Password = "",
							IsActive = true,
							ClientType_Id = 1
						};
						ClientCreateVM add = new ClientCreateVM(client, models.Context);
						ClientUAVMWindow window = new ClientUAVMWindow(add);
						add.CancelCommand.ExecuteList += a => window.Close();
						add.UpdateCommand.ExecuteList += a => window.Close();
						window.ShowDialog();
					}, a => models.User.IsAdmin));
			}
		}
		public override RelayCommand UpdateCommand
		{
			get
			{
				return updateCommand ??
					(updateCommand = new RelayCommand(obj =>
					{
						ClientUpdateVM upd = new ClientUpdateVM(models.SelectedItem, models.Context);
						ClientUAVMWindow window = new ClientUAVMWindow(upd);
						upd.CancelCommand.ExecuteList += a => window.Close();
						upd.UpdateCommand.ExecuteList += a => window.Close();
						window.ShowDialog();
						models.Collection.View.Refresh();
					}, a => models.SelectedItem != null && models.User.IsAdmin));
			}
		}
		public override RelayCommand ProfileCommand
		{
			get
			{
				return profileCommand ??
					(profileCommand = new RelayCommand(obj =>
					{
						ClientProfileVM profile = new ClientProfileVM(models.SelectedItem, models.User);
						ClientProfileWindow window = new ClientProfileWindow(profile);
						window.ShowDialog();
					}, a => models.SelectedItem != null));
			}
		}
		public override RelayCommand ReFreshCommand
		{
			get
			{
				return reFreshCommand ??
					(reFreshCommand = new RelayCommand(obj =>
					{
						models.ReFresh();
					}, a => models.SelectedItem != null));
			}
		}
	}

	public class ClientFilterVM : ViewModelBase
	{
		public ClientFilterVM(CollectionViewSource collection)
		{
			this.collection = collection;
			using (MyKindergardenEntities db = new MyKindergardenEntities())
			{
				Groups.Collection = new ObservableCollection<KinderGroup>(db.KinderGroups.Where(a => a.IsActive).AsNoTracking());
			}
			Groups.Collection.Insert(0, noGroup);
			Groups.Collection.Insert(0, all);
			Groups.SelectedItem = all;
			collection.View.Filter += a => Execute(a as Client);
			ActiveStatus.Collection = new ObservableCollection<string>() { "Все", "Активные", "Неактивные" };
			ActiveStatus.SelectedItem = ActiveStatus.Collection[0];
		}

		static readonly KinderGroup all = new KinderGroup() { Id = -1, GroupName = "Все" };
		static readonly KinderGroup noGroup = new KinderGroup() { Id = -2, GroupName = "Без группы" };
		CollectionViewSource collection;
		string fname = "";
		string lname = "";
		string mname = "";

		RelayCommand filterCommand;
		RelayCommand resetFilterCommand;

		public string FName
		{
			get => fname;
			set
			{
				fname = value;
				OnPropertyChanged();
			}
		}
		public string LName
		{
			get => lname;
			set
			{
				lname = value;
				OnPropertyChanged();
			}
		}
		public string MName
		{
			get => mname;
			set
			{
				mname = value;
				OnPropertyChanged();
			}
		}
		
		public CollectionViewModel<KinderGroup> Groups { get; private set; } = new CollectionViewModel<KinderGroup>();
		public CollectionViewModel<string> ActiveStatus { get; private set; } = new CollectionViewModel<string>();
		public RelayCommand FilterCommand
		{
			get
			{
				return filterCommand ??
					(filterCommand = new RelayCommand(obj => collection.View.Refresh()));
			}
		}
		public RelayCommand ResetFilterCommand
		{
			get
			{
				return resetFilterCommand ??
					(resetFilterCommand = new RelayCommand(obj =>
					{
						FName = "";
						LName = "";
						MName = "";
						Groups.SelectedItem = all;
						ActiveStatus.SelectedItem = ActiveStatus.Collection[0];
						collection.View.Refresh();
					}
					));
			}
		}


		bool nameChecker(string filter, string str) => filter == "" || str.Contains(filter);
		bool groupChecker(Client model)
			=> Groups.SelectedItem == null || Groups.SelectedItem == all
			|| model.KinderGroups.Where(a => Groups.SelectedItem.Id == a.Id).FirstOrDefault() != null
			|| (Groups.SelectedItem == noGroup && model.KinderGroups.Count == 0);
		bool statusChecker(Client model)
			=> ActiveStatus.SelectedItem == null || ActiveStatus.SelectedItem == ActiveStatus.Collection[0]
			|| (model.IsActive && ActiveStatus.SelectedItem == ActiveStatus.Collection[1])
			|| (!model.IsActive && ActiveStatus.SelectedItem == ActiveStatus.Collection[2]);
		public bool Execute(Client model)
			=> nameChecker(FName, model.FirstName)
			&& nameChecker(LName, model.LastName)
			&& nameChecker(MName, model.MiddleName)
			&& groupChecker(model)
			&& statusChecker(model);
	}
}
