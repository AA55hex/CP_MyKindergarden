using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data.Entity;
using System.Windows.Data;
using MyKindergarden.Windows;
namespace MyKindergarden.ViewModels
{

	public class VisitNoteVM : ViewModelBase
	{
		public VisitNoteVM(VisitNote mdl, MyKindergardenEntities ctx, bool readOnly)
		{
			IsReadOnly = readOnly;
			model = mdl;
			context = ctx;
		}
		VisitNote model;
		VisitNote Model => model;
		MyKindergardenEntities context;
		bool isReadOnly = false;
		public bool IsReadOnly
		{
			get => isReadOnly;
			set
			{
				isReadOnly = value;
				OnPropertyChanged();
				OnPropertyChanged("IsEnabled");
			}
		}
		public bool IsEnabled
		{
			get => !isReadOnly;
			set
			{
				isReadOnly = !value;
				OnPropertyChanged();
				OnPropertyChanged("IsReadOnly");
			}
		}
		public bool CanVisitChanged => model.VisitDate.Date <= DateTime.Today;
		public bool? Visit
		{
			get => model.Visited;
			set
			{
				model.Visited = value;
				SaveChanges();
				OnPropertyChanged();
			}
		}
		public string FIO => model.Child.LastName + " " + model.Child.FirstName[0] + "." + model.Child.MiddleName[0] + ".";
		public string Additional
		{
			get => model.Additional;
			set
			{
				model.Additional = value;
				SaveChanges();
				OnPropertyChanged();
			}
		}
		public string Group => model.Child.KinderGroup?.GroupName ?? "Без группы";
		void SaveChanges()
		{
			context.Entry(model).State = EntityState.Modified;
			context.SaveChanges();
		}
	}

	public class DateRangeSelectorVM : ViewModelBase
	{
		DateTime left = DateTime.Now;
		DateTime right = DateTime.Now;

		public DateTime Left
		{
			get => left;
			set
			{
				left = value;
				OnPropertyChanged();
			}
		}
		public DateTime Right
		{
			get => right;
			set
			{
				right = value;
				OnPropertyChanged();
			}
		}

		RelayCommand acceptCommand;
		RelayCommand cancelCommand;


		public RelayCommand AcceptCommand
		{
			get
			{
				return acceptCommand ??
					(acceptCommand = new RelayCommand(obj => { }, a => left <= right));
			}
		}
		public RelayCommand CancelCommand
		{
			get
			{
				return cancelCommand ??
					(cancelCommand = new RelayCommand(obj => { }));
			}
		}
	}

	public class VisitNoteListVM : ModelList<VisitNoteVM>
	{
		public VisitNoteListVM(CurrentUserVM usr) : base(usr)
		{
			Filter.Property = new NoteFilterVM(this);
			Filter.Property.PropertyChanged += dateChanged;
			ReFresh();
			Collection.View.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
		}

		bool isReadOnly;
		public bool IsReadOnly
		{
			get => isReadOnly;
			set
			{
				isReadOnly = value;
				OnPropertyChanged();
			}
		}
		void dateChanged(object o, PropertyChangedEventArgs e)
		{
			ReFresh();
		}

		public PropertyViewModel<NoteFilterVM> Filter { get; private set; } = new PropertyViewModel<NoteFilterVM>();


		RelayCommand addDatesCommand;
		RelayCommand setWorkdayCommand;
		RelayCommand setWeekendCommand;

		public RelayCommand SetWorkdayCommand
		{
			get
			{
				return setWorkdayCommand ??
					(setWorkdayCommand = new RelayCommand(obj =>
					{
						VisitDate selected = Filter.Property.SelectedVisitDate;
						if (selected == null)
						{
							selected = new VisitDate() { Date = Filter.Property.SelectedDate, IsVisitDate = true };
							context.VisitDates.Add(selected);
						}
						else
						{
							context.Entry(selected).State = EntityState.Modified;
							selected.IsVisitDate = true;
						}
						context.SaveChanges();
						ReFresh();
					}, a => User.IsAdmin && !(Filter.Property.SelectedVisitDate?.IsVisitDate ?? false)));
			}
		}
		public RelayCommand SetWeekendCommand
		{
			get
			{
				return setWeekendCommand ??
					(setWeekendCommand = new RelayCommand(obj =>
					{
						
						VisitDate selected = Filter.Property.SelectedVisitDate;
						if(selected == null)
						{
							selected = new VisitDate() { Date = Filter.Property.SelectedDate, IsVisitDate = false };
							context.VisitDates.Add(selected);
						}
						else
						{
							if (MessageBox.Show("Все данные о посещении очистятся", "Выходной день", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
							{
								context.Entry(selected).State = EntityState.Modified;
								selected.IsVisitDate = false;
							}
						}
						context.SaveChanges();
						ReFresh();
					}, a => User.IsAdmin && (Filter.Property.SelectedVisitDate?.IsVisitDate ?? true)));
			}
		}
		public RelayCommand AddDatesCommand
		{
			get
			{
				return addDatesCommand ??
					(addDatesCommand = new RelayCommand(obj =>
					{
						DateRangeSelectorVM selector = new DateRangeSelectorVM();
						selector.AcceptCommand.ExecuteList += a =>
						{
							context.Insert_dates(selector.Left, selector.Right);
							context.SaveChanges();
						};
						DataRangeSelectorWindow window = new DataRangeSelectorWindow(selector);
						selector.AcceptCommand.ExecuteList += a => window.Close();
						selector.CancelCommand.ExecuteList += a => window.Close();
						window.ShowDialog();
					},a => User.IsAdmin));
			}
		}



		public void ReFresh()
		{
			context = new MyKindergardenEntities();
			var query = Filter.Property.Execute(context);
			IsReadOnly = Filter.Property.SelectedDate > DateTime.Today || !(Filter.Property.SelectedVisitDate?.IsVisitDate ?? false);
			Collection.Source = new ObservableCollection<VisitNoteVM>(query.ToList().Select(a => new VisitNoteVM(a, context, IsReadOnly)));
		}
	}


	public class NoteFilterVM : ViewModelBase
	{

		public NoteFilterVM(VisitNoteListVM vmdl)
		{
			vmodel = vmdl;
			using (MyKindergardenEntities db = new MyKindergardenEntities())
			{ 
				db.KinderGroups.Where
							(a => a.IsActive && (user.IsAdmin || a.Clients.Select(o => o.Id).Contains(user.User.Id))).Load();
				GroupCollection = new ObservableCollection<KinderGroup>(db.KinderGroups.Local);
				if(user.IsAdmin)
					GroupCollection.Add(noGroup);
			}
			GroupCollection.Insert(0, all);
			SelectedGroup = GroupCollection.FirstOrDefault();
		}

		DateTime selectedDate = DateTime.Today;
		VisitNoteListVM vmodel;
		CurrentUserVM user => vmodel.User;

		static readonly KinderGroup noGroup = new KinderGroup() { Id = -2, GroupName = "Без группы" };
		static readonly KinderGroup all = new KinderGroup() { Id = -2, GroupName = "Все" };
		KinderGroup selectedItem;
		ObservableCollection<KinderGroup> collection;

		public KinderGroup SelectedGroup
		{
			get { return selectedItem; }
			set
			{
				selectedItem = value;
				OnPropertyChanged();
			}
		}
		public ObservableCollection<KinderGroup> GroupCollection
		{
			get => collection;
			set
			{
				collection = value;
				OnPropertyChanged();
			}

		}
		public DateTime SelectedDate
		{
			get => selectedDate;
			set
			{
				selectedDate = value;
				OnPropertyChanged();
			}
		}
		public VisitDate SelectedVisitDate
		{
			get
			{
				VisitDate result;
				using (MyKindergardenEntities context = new MyKindergardenEntities())
				{
					DateTime nextDate = SelectedDate.AddDays(1);
					result = context.VisitDates.Where(a => a.Date >= SelectedDate.Date && a.Date < nextDate).FirstOrDefault();
				}
				return result;
			}
		}

		RelayCommand nextDateCommand;
		RelayCommand prevDateCommand;
		public RelayCommand NextDateCommand
		{
			get
			{
				return nextDateCommand ??
					(nextDateCommand = new RelayCommand(obj =>
					{
						SelectedDate = SelectedDate.AddDays(1);
					}));
			}
		}
		public RelayCommand PrevDateCommand
		{
			get
			{
				return prevDateCommand ??
					(prevDateCommand = new RelayCommand(obj =>
					{
						SelectedDate = SelectedDate.AddDays(-1);
					}));
			}
		}

		public IQueryable<VisitNote> Execute(MyKindergardenEntities context)
		{
			var buff = user.User.KinderGroups.Where(a => a.IsActive).Select(o => new int?(o.Id)).ToList();
			bool isNull = SelectedGroup == null;
			bool isNoGroup = SelectedGroup == noGroup;
			bool isAllGroup = SelectedGroup == all;
			IQueryable<VisitNote> result = context.VisitNotes
				.Where(a => a.VisitDate.Date == selectedDate.Date)
				.Where(a => isNull
						|| (isAllGroup && (user.IsAdmin || buff.Contains(a.Child.KinderGroup_Id)))
						|| (isNoGroup && a.Child.KinderGroup_Id == null)
						|| (a.Child.KinderGroup_Id == SelectedGroup.Id));

			return result;
		}
	}
}
