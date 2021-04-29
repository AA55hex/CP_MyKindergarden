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
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
namespace MyKindergarden.ViewModels
{
	public class ChildListVM : ModelList<Child>
	{
		public ChildListVM(CurrentUserVM usr) : base(usr)
		{
			Load();
			Filter.Property = new ChildFilterVM(Collection);
			Commands = new ChildListCommandsVM(this);
		}

		public PropertyViewModel<ChildFilterVM> Filter { get; private set; } = new PropertyViewModel<ChildFilterVM>();
		public ChildListCommandsVM Commands { get; private set; }

		void Load() => ReFresh();

		public void ReFresh()
		{
			context?.Dispose();
			context = new MyKindergardenEntities();
			context.Children.Load();
			Collection.Source = context.Children.Local;
		}

	}

	public class ChildListCommandsVM : ListCommandsVM<ChildListVM>
	{
		public ChildListCommandsVM(ChildListVM lst) : base(lst) { }

		public override RelayCommand DeleteCommand
		{
			get
			{
				return deleteCommand ??
					(deleteCommand = new RelayCommand(obj =>
					{
						if (MessageBox.Show("Удалить информацию о данном ребенке?", "Удаление", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
						{
							models.Context.Children.Remove(models.SelectedItem);
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
						Child child = new Child()
						{
							VisitStart = DateTime.Today,
							FirstName = "",
							MiddleName = "",
							LastName = ""
						};
						ChildCreateVM upd = new ChildCreateVM(child, models.Context);
						ChildUAVMWindow window = new ChildUAVMWindow(upd);
						upd.CancelCommand.ExecuteList += a => window.Close();
						upd.UpdateCommand.ExecuteList += a => window.Close();
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
						ChildUpdateVM upd = new ChildUpdateVM(models.SelectedItem, models.Context);
						ChildUAVMWindow window = new ChildUAVMWindow(upd);
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
						ChildProfileVM profile = new ChildProfileVM(models.SelectedItem);
						ChildProfile window = new ChildProfile(profile);
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
	public class ChildFilterVM : ViewModelBase
	{
		public ChildFilterVM(CollectionViewSource collection)
		{
			this.collection = collection;
			Refresh();
			collection.View.Filter += a => Execute(a as Child);
		}
		static readonly KinderGroup all = new KinderGroup() { Id = -1, GroupName = "Все" };
		static readonly KinderGroup noGroup = new KinderGroup() { Id = -2, GroupName = "Без группы" };
		CollectionViewSource collection;
		string fname = "";
		string lname = "";
		string mname = "";

		DateTime? leftVisit;
		DateTime? rightVisit;

		DateTime? leftBirth;
		DateTime? rightBirth;

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

		public DateTime? LeftVisit
		{
			get => leftVisit;
			set
			{
				leftVisit = value;
				OnPropertyChanged();
			}
		}
		public DateTime? RightVisit
		{
			get => rightVisit;
			set
			{
				rightVisit = value;
				OnPropertyChanged();
			}
		}

		public DateTime? LeftBirth
		{
			get => leftBirth;
			set
			{
				leftBirth = value;
				OnPropertyChanged();
			}
		}
		public DateTime? RightBirth
		{
			get => rightBirth;
			set
			{
				rightBirth = value;
				OnPropertyChanged();
			}
		}

		public CollectionViewModel<KinderGroup> Groups { get; private set; } = new CollectionViewModel<KinderGroup>();

		public RelayCommand FilterCommand
		{
			get
			{
				return filterCommand ??
					(filterCommand = new RelayCommand(obj => collection.View.Refresh()
						, a => (LeftVisit == null || RightVisit == null || LeftVisit <= RightVisit)
							&& (LeftBirth == null || RightBirth == null || LeftBirth <= RightBirth)));
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
						LeftVisit = null;
						RightVisit = null;
						LeftBirth = null;
						RightBirth = null;
						Groups.SelectedItem = all;
						collection.View.Refresh();
					}
					));
			}
		}


		bool nameChecker(string filter, string str) => filter == "" || str.Contains(filter);
		bool groupChecker(Child model)
			=> Groups.SelectedItem == null
			|| (Groups.SelectedItem == all)
			|| (Groups.SelectedItem == noGroup && model.KinderGroup_Id == null)
			|| Groups.SelectedItem.Id == model.KinderGroup_Id;
		bool dateBirthChecker(Child model, DateTime? left, DateTime? right)
		{
			DateTime? nextDate = right?.AddDays(1);
			return (left==null || model.BirthDate >= left) && (nextDate == null || model.BirthDate < nextDate);
		}
		bool dateVisitChecker(Child model, DateTime? left, DateTime? right)
		{
			DateTime? nextDate = right?.AddDays(1);
			DateTime? min = left ?? nextDate ?? null;
			DateTime? max = nextDate ?? left ?? null;
			return max == null
				|| (max == nextDate && min == left && model.VisitStart < max && (model.VisitEnd == null || model.VisitEnd >= min))
				|| (max == left && (model.VisitEnd == null || model.VisitEnd >= min))
				|| (min == nextDate && (model.VisitStart < max));
		}
		public bool Execute(Child model)
			=> nameChecker(FName, model.FirstName)
			&& nameChecker(LName, model.LastName)
			&& nameChecker(MName, model.MiddleName)
			&& groupChecker(model)
			&& dateVisitChecker(model, LeftVisit, RightVisit)
			&& dateBirthChecker(model, LeftBirth, RightBirth);
		public void Refresh()
		{
			using (MyKindergardenEntities db = new MyKindergardenEntities())
			{
				Groups.Collection = new ObservableCollection<KinderGroup>(db.KinderGroups.Where(a => a.IsActive).AsNoTracking());
			}
			Groups.Collection.Insert(0, noGroup);
			Groups.Collection.Insert(0, all);
			Groups.SelectedItem = all;
		}
	}
}
