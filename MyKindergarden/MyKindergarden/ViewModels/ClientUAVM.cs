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
	public class GroupObjectSelector : ModelShellVM<KinderGroup>
	{
		public GroupObjectSelector(KinderGroup mdl) : base(mdl) { }
		public string Название => model.GroupName;
		public int Дети => model.Children
				.Where(a => a.VisitStart <= DateTime.Now.Date
					&& (a.VisitEnd == null || a.VisitEnd >= DateTime.Now.Date)).Count();
		public int Воспитатели => model.Clients.Where(a => a.IsActive).Count();
	}
	public abstract class ClientUAVM : ViewModelBase
	{
		public ClientUAVM(Client mdl, MyKindergardenEntities ctx)
		{
			model = mdl;
			context = ctx;
		}

		protected List<KinderGroup> groups;
		public List<KinderGroup> Groups
		{
			get => groups;
			set
			{
				groups = value;
				OnPropertyChanged();
			}
		}


		protected Client model;
		protected MyKindergardenEntities context;
		public Client Model => model;
		public string UpdString { get; set; }

		protected RelayCommand selectGroupsCommand;
		protected RelayCommand updateCommand;
		protected RelayCommand cancelCommand;

		public virtual RelayCommand SelectGroupsCommand { get; }
		public virtual RelayCommand UpdateCommand
		{
			get
			{
				return updateCommand ??
					(updateCommand = new RelayCommand(obj => { }));
			}
		}
		public virtual RelayCommand CancelCommand
		{
			get
			{
				return cancelCommand ??
					(cancelCommand = new RelayCommand(obj => { }));
			}
		}


		protected virtual bool CanUpdate(object o)
		{
			return model.FirstName != "" &&
				model.MiddleName != "" &&
				model.LastName != "" &&
				model.Username.Length > 5 &&
				model.Password.Length > 5 &&
				model.ClientType_Id == 1;
		}
	}
	public class ClientUpdateVM : ClientUAVM
	{
		public ClientUpdateVM(Client mdl, MyKindergardenEntities ctx) : base(mdl, ctx)
		{
			UpdString = "Изменить";
			groups = new List<KinderGroup>(model.KinderGroups);
		}


		public override RelayCommand SelectGroupsCommand
		{
			get
			{
				return selectGroupsCommand ??
					(selectGroupsCommand = new RelayCommand(obj =>
					{
						context.Entry(model).Collection("KinderGroups").Load();

						ObservableCollection<GroupObjectSelector> left, right;

						var buff = context.KinderGroups.Where(a => a.IsActive).ToList();
						left = new ObservableCollection<GroupObjectSelector>(buff.Where(a => !groups.Contains(a)).Select(a => new GroupObjectSelector(a)));
						right = new ObservableCollection<GroupObjectSelector>(groups.Select(a => new GroupObjectSelector(a)));

						CollectionSelectorVM<GroupObjectSelector> selector = new CollectionSelectorVM<GroupObjectSelector>(left, right);
						selector.AcceptCommand.ExecuteList += o => Groups = right.Select(a => a.GetModel()).ToList();

						CollectionSelectorWindow window = new CollectionSelectorWindow(selector);
						selector.CancelCommand.ExecuteList += a => window.Close();
						selector.AcceptCommand.ExecuteList += a => window.Close();
						window.ShowDialog();

					}));
			}
		}
		public override RelayCommand UpdateCommand
		{
			get
			{
				return updateCommand ??
					(updateCommand = new RelayCommand(obj =>
					{
						model.KinderGroups = groups;
						context.SaveChanges();
					}, CanUpdate));
			}
		}
		public override RelayCommand CancelCommand
		{
			get
			{
				return cancelCommand ??
					(cancelCommand = new RelayCommand(obj =>
					{
						context.Entry(model).Reload();
					}));
			}
		}
	}
	public class ClientCreateVM : ClientUAVM
	{
		public ClientCreateVM(Client mdl, MyKindergardenEntities ctx) : base(mdl, ctx)
		{
			UpdString = "Добавить";
			Groups = new List<KinderGroup>();
		}

		public override RelayCommand SelectGroupsCommand
		{
			get
			{
				return selectGroupsCommand ??
					(selectGroupsCommand = new RelayCommand(obj =>
					{
						ObservableCollection<GroupObjectSelector> left, right;
						left = new ObservableCollection<GroupObjectSelector>(context.KinderGroups.Where(a => a.IsActive).ToList().Select(a => new GroupObjectSelector(a)));
						right = new ObservableCollection<GroupObjectSelector>();
						CollectionSelectorVM<GroupObjectSelector> selector = new CollectionSelectorVM<GroupObjectSelector>(left, right);
						selector.AcceptCommand.ExecuteList += a => Groups = right.Select(b => b.GetModel()).ToList();
						CollectionSelectorWindow window = new CollectionSelectorWindow(selector);
						selector.CancelCommand.ExecuteList += a => window.Close();
						selector.AcceptCommand.ExecuteList += a => window.Close();
						window.ShowDialog();
					}));
			}
		}
		public override RelayCommand UpdateCommand
		{
			get
			{
				return updateCommand ??
					(updateCommand = new RelayCommand(obj =>
					{
						model.KinderGroups = Groups;
						context.Clients.Add(model);
						context.SaveChanges();
					}, CanUpdate));
			}
		}
	}
}
