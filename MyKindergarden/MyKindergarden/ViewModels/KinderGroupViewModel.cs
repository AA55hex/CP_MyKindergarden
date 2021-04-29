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

	public class GroupProfile
	{
		public GroupProfile(KinderGroup mdl)
		{
			model = mdl;
			Children = model.Children
					.Where(a => a.VisitStart <= DateTime.Now.Date
						&& (a.VisitEnd == null || a.VisitEnd >= DateTime.Now.Date))
					.ToList();
			Teachers = model.Clients.Where(a => a.IsActive).ToList();
		}
		KinderGroup model;

		public List<Child> Children { get; private set; }
		public int ChildCount => Children.Count();
		public List<Client> Teachers { get; private set; }
		public int TeacherCount => Teachers.Count();
		public string ActiveStatus => model.IsActive ? "Активна" : "Закрыта";
		public KinderGroup Model => model;
	}

	public class GroupListItemVM : ViewModelBase
	{
		public GroupListItemVM(KinderGroup mdl)
		{
			model = mdl;
		}
		KinderGroup model;
		public int ChildCount 
			=> model.Children
				.Where(a => a.VisitStart <= DateTime.Now.Date 
					&& (a.VisitEnd == null || a.VisitEnd >= DateTime.Now.Date)).Count();
		public int TeacherCount => model.Clients.Where(a => a.IsActive).Count();
		public KinderGroup Model => model;
		public string ActiveStatus => model.IsActive ? "Активна" : "Закрыта";
		public void Refresh()
		{
			OnPropertyChanged("ChildCount");
		}
	}
	public class GroupListVM : ModelList<GroupListItemVM>
	{
		public GroupListVM(CurrentUserVM usr) : base(usr)
		{
			Load();
			Commands = new GroupListCommandsVM(this);
			Filter.Property = new GroupFilterVM(Collection);
		}

		public GroupListCommandsVM Commands { get; private set; }
		public PropertyViewModel<GroupFilterVM> Filter { get; private set; } = new PropertyViewModel<GroupFilterVM>();

		void Load() => ReFresh();
		public void ReFresh()
		{
			context?.Dispose();
			context = new MyKindergardenEntities();
			context.KinderGroups.Load();
			Collection.Source = context.KinderGroups.Local.Select(a => new GroupListItemVM(a));
		}
	}
	public class GroupListCommandsVM : ListCommandsVM<GroupListVM>
	{
		public GroupListCommandsVM(GroupListVM lst) : base(lst) { }
		public override RelayCommand DeleteCommand
		{
			get
			{
				return deleteCommand ??
					(deleteCommand = new RelayCommand(obj =>
					{
						if (MessageBox.Show("Удалить информацию о данной группе?", "Удаление", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
						{
							models.Context.KinderGroups.Remove(models.SelectedItem.Model);
							models.SelectedItem = null;
							models.Context.SaveChanges();
							models.Collection.View.Refresh();
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
						KinderGroup group = new KinderGroup()
						{
							GroupName = "",
							IsActive = true
						};
						GroupCreateVM add = new GroupCreateVM(group, models.Context);
						GroupUAVMWindow window = new GroupUAVMWindow(add);
						add.CancelCommand.ExecuteList += a => window.Close();
						add.UpdateCommand.ExecuteList += a => window.Close();
						window.ShowDialog();
						models.Collection.View.Refresh();
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
						GroupUpdateVM upd = new GroupUpdateVM(models.SelectedItem.Model, models.Context);
						GroupUAVMWindow window = new GroupUAVMWindow(upd);
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
						GroupProfile profile = new GroupProfile(models.SelectedItem.Model);
						GroupProfileWindow window = new GroupProfileWindow(profile);
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
	public class GroupFilterVM : ViewModelBase
	{
		public GroupFilterVM(CollectionViewSource collection)
		{
			this.collection = collection;
			using (MyKindergardenEntities db = new MyKindergardenEntities())
			{
				GroupTypes.Collection = new ObservableCollection<GroupType>(db.GroupTypes.AsNoTracking());
			}
			GroupTypes.Collection.Insert(0, all);
			GroupTypes.SelectedItem = all;
			collection.View.Filter += a => Execute(a as GroupListItemVM);
			ActiveStatus.Collection = new ObservableCollection<string>() { "Все", "Активные", "Неактивные" };
			ActiveStatus.SelectedItem = ActiveStatus.Collection[0];
		}

		static readonly GroupType all = new GroupType() { Id = -1, TypeName = "Все" };
		CollectionViewSource collection;
		string name = "";


		string childLeftStr = "";
		int childLeft = 0;
		string childRighStr = "";
		int childRight = int.MaxValue;

		string teacherLeftStr = "";
		int teacherLeft = 0;
		string teacherRightStr = "";
		int teacherRight = int.MaxValue;



		public string ChildCountLeft
		{
			get => childLeftStr;
			set
			{
				leftSetter(value, ref childLeftStr, ref childLeft);
				OnPropertyChanged();
			}
		}
		public string ChildCountRight
		{
			get => childRighStr;
			set
			{
				rightSetter(value, ref childRighStr, ref childRight);
				OnPropertyChanged();
			}
		}
		public string TeacherCountLeft
		{
			get => teacherLeftStr;
			set
			{
				leftSetter(value, ref teacherLeftStr, ref teacherLeft);
				OnPropertyChanged();
			}
		}
		public string TeacherCountRight
		{
			get => teacherRightStr;
			set
			{
				rightSetter(value, ref teacherRightStr, ref teacherRight);
				OnPropertyChanged();
			}
		}
		void leftSetter(string value, ref string str, ref int left)
		{
			if (string.IsNullOrEmpty(value) || int.TryParse(value, out left))
			{
				str = value;
				left = string.IsNullOrEmpty(value) ? 0 : left;
			}
		}
		void rightSetter(string value, ref string str, ref int right)
		{
			if (string.IsNullOrEmpty(value) || int.TryParse(value, out right))
			{
				str = value;
				right = string.IsNullOrEmpty(value) ? int.MaxValue : right;
			}
		}


		RelayCommand filterCommand;
		RelayCommand resetFilterCommand;

		public string Name
		{
			get => name;
			set
			{
				name = value;
				OnPropertyChanged();
			}
		}
		public CollectionViewModel<string> ActiveStatus { get; private set; } = new CollectionViewModel<string>();
		public CollectionViewModel<GroupType> GroupTypes { get; private set; } = new CollectionViewModel<GroupType>();


		public RelayCommand FilterCommand
		{
			get
			{
				return filterCommand ??
					(filterCommand = new RelayCommand
						(obj => collection.View.Refresh(),
							a => childLeft <= childRight
							&& teacherLeft <= teacherRight));
			}
		}
		public RelayCommand ResetFilterCommand
		{
			get
			{
				return resetFilterCommand ??
					(resetFilterCommand = new RelayCommand(obj =>
					{
						Name = "";
						ChildCountLeft = "";
						ChildCountRight = "";
						TeacherCountLeft = "";
						TeacherCountRight = "";
						GroupTypes.SelectedItem = all;
						ActiveStatus.SelectedItem = ActiveStatus.Collection[0];
						collection.View.Refresh();
					}
					));
			}
		}

		
		bool nameChecker(string filter, string str) => string.IsNullOrWhiteSpace(filter) || str.Contains(filter);
		bool statusChecker(GroupListItemVM model)
			=> ActiveStatus.SelectedItem == null || ActiveStatus.SelectedItem == ActiveStatus.Collection[0]
			|| (model.Model.IsActive && ActiveStatus.SelectedItem == ActiveStatus.Collection[1])
			|| (!model.Model.IsActive && ActiveStatus.SelectedItem == ActiveStatus.Collection[2]);
		bool grtypeChecker(GroupListItemVM model)
			=> GroupTypes.SelectedItem == null
			|| GroupTypes.SelectedItem == all
			|| model.Model.GroupType.Id == GroupTypes.SelectedItem.Id;
		bool countChecker(int count, int left, int right)
			=> count >= left && count <= right;
		public bool Execute(GroupListItemVM model)
			=> nameChecker(Name, model.Model.GroupName)
			&& statusChecker(model)
			&& grtypeChecker(model)
			&& countChecker(model.ChildCount, childLeft, childRight)
			&& countChecker(model.TeacherCount, teacherLeft, teacherRight);
	}
}
