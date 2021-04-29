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

	public class GroupStatShell : ViewModelBase
	{
		public GroupStatShell(KinderGroup mdl, IEnumerable<VisitNote> nts)
		{
			model = mdl;
			notes = nts;
		}
		KinderGroup model;
		IEnumerable<VisitNote> notes;
		public KinderGroup Group => model;
		public int WorkDaysCount => notes.GroupBy(a => a.VisitDate).Where(a => a.Key.IsVisitDate).Count();
		public int VisitNoteCount => notes.Where(a => a.Visited == true).Count();
		public int NotVisitNoteCount => notes.Where(a => a.Visited == false).Count();
		public int NullVisitNotesCount => notes.Where(a => a.Visited == null).Count();
		public double VisitRating => (VisitNoteCount + NotVisitNoteCount) != 0 ? (VisitNoteCount / (double)(VisitNoteCount + NotVisitNoteCount)) : 1;
	}

	public class StatVM : ViewModelBase
	{
		public StatVM()
		{
			Filter = new StatFilterVM();
			Filter.UpdateCommand.ExecuteList += a => ReFresh();
		}

		MyKindergardenEntities context;
		VisitDate maxDay;
		int? maxDayCount;
		VisitDate minDay;
		int? minDayCount;

		int workDaysCount = 0;
		int notWorkDaysCount = 0;
		int undefDaysCount = 0;
		int workedDaysCount = 0;

		int daysCount = 0;

		int visitSum = 0;
		int notVisitSum = 0;
		double visitRating = 0;
		double avg = 0.0;

		public CollectionViewSource Collection { get; private set; } = new CollectionViewSource();

		public int DaysCount
		{
			get => daysCount;
			set
			{
				daysCount = value;
				OnPropertyChanged();
			}
		}
		public int WorkDaysCount
		{
			get => workDaysCount;
			set
			{
				workDaysCount = value;
				OnPropertyChanged();
			}
		}
		public int WorkedDaysCount
		{
			get => workedDaysCount;
			set
			{
				workedDaysCount = value;
				OnPropertyChanged();
			}
		}
		public int NotWorkDaysCount
		{
			get => notWorkDaysCount;
			set
			{
				notWorkDaysCount = value;
				OnPropertyChanged();
			}
		}
		public int UndefDaysCount
		{
			get => undefDaysCount;
			set
			{
				undefDaysCount = value;
				OnPropertyChanged();
			}
		}

		public double Avg
		{
			get => avg;
			set
			{

				avg = value;
				OnPropertyChanged();
			}
		}
		public int VisitSum
		{
			get => visitSum;
			set
			{
				visitSum = value;
				OnPropertyChanged();
			}
		}
		public int NotVisitSum
		{
			get => notVisitSum;
			set
			{
				notVisitSum = value;
				OnPropertyChanged();
			}
		}
		public double VisitRating
		{
			get => visitRating;
			set
			{
				visitRating = value;
				OnPropertyChanged();
			}
		}



		public VisitDate MaxDay
		{
			get => maxDay;
			set
			{
				maxDay = value;
				OnPropertyChanged();
			}
		}
		public int? MaxDayCount
		{
			get => maxDayCount;
			set
			{
				maxDayCount = value;
				OnPropertyChanged();
			}
		}
		public VisitDate MinDay
		{
			get => minDay;
			set
			{
				minDay = value;
				OnPropertyChanged();
			}
		}
		public int? MinDayCount
		{
			get => minDayCount;
			set
			{
				minDayCount = value;
				OnPropertyChanged();
			}
		}


		public MyKindergardenEntities Context => context;
		public StatFilterVM Filter { get; private set; }


		public void ReFresh()
		{
			context?.Dispose();
			context = new MyKindergardenEntities();
			var dates = Filter.ExecuteVisitDates(context.VisitDates);
			var groups = Filter.ExecuteGroups(context.KinderGroups);
			var notes = Filter.ExecuteVisitNotes(context.VisitNotes);
			var cross = from date in dates
						from gr in groups
						select new { Date = date, Group = gr };

			DaysCount = ((Filter.RightDate.AddDays(1)) - Filter.LeftDate).Days;

			WorkDaysCount = dates.Where(a => a.IsVisitDate).Count();
			NotWorkDaysCount = dates.Where(a => !a.IsVisitDate).Count();

			DateTime nextDay = DateTime.Today.AddDays(1);
			WorkedDaysCount = dates.Where(a => a.IsVisitDate).Where(a => a.Date <= nextDay).Count();

			UndefDaysCount = DaysCount - (WorkedDaysCount + NotWorkDaysCount);

			VisitDate maxd
				= dates.Where(a => a.IsVisitDate)
				.OrderByDescending(a => a.VisitNotes.Where(n => n.Visited == true).Count())
				.FirstOrDefault();

			VisitDate mind
				= dates.Where(a => a.IsVisitDate)
				.OrderByDescending(a => a.VisitNotes.Where(n => n.Visited == false).Count())
				.FirstOrDefault();

			MaxDay = maxd;
			MinDay = mind;

			MaxDayCount = maxd?.VisitNotes.Where(n => n.Visited == true).Count();
			MinDayCount = mind?.VisitNotes.Where(n => n.Visited == false).Count();

			Avg = dates.Where(a => a.Date <= nextDay && a.IsVisitDate)
				.SelectMany(a => a.VisitNotes)
				.Join(notes, a => a.Id, a => a.Id, (a,b) => a)
				.Where(a => a.Visited == true)
				.Count() / (double)WorkedDaysCount;
			VisitSum = notes.Where(a => a.Visited == true).Count();
			NotVisitSum = notes.Where(a => a.Visited == false).Count();
			VisitRating = (VisitSum + NotVisitSum) != 0 ? ((double)VisitSum / (VisitSum + NotVisitSum)) : 1;

			Collection.Source = groups.GroupJoin(notes, a => a.Id, a => a.Child.KinderGroup_Id, (a,b)=> new { Group = a, Notes = b})
				.ToList().Select(a => new GroupStatShell(a.Group, a.Notes));
		}
		~StatVM()
		{
			context?.Dispose();
		}
	}

	public class StatFilterVM : ViewModelBase
	{
		public StatFilterVM()
		{
			Groups.Collection = new ObservableCollection<GroupObjectSelector>();
		}


		public CollectionViewModel<GroupObjectSelector> Groups { get; private set; } = new CollectionViewModel<GroupObjectSelector>();

		
		DateTime left = DateTime.Today;
		DateTime right = DateTime.Today;

		public DateTime LeftDate
		{
			get => left;
			set
			{
				left = value;
				OnPropertyChanged();
			}
		}
		public DateTime RightDate
		{
			get => right;
			set
			{
				right = value;
				OnPropertyChanged();
			}
		}


		RelayCommand updateCommand;
		RelayCommand selectGroupsCommand;
		public RelayCommand UpdateCommand
		{
			get
			{
				return updateCommand ??
					(updateCommand = new RelayCommand(obj =>
					{
					}, a => left <= right));
			}
		}
		public RelayCommand SelectGroupsCommand
		{
			get
			{
				return selectGroupsCommand ??
					(selectGroupsCommand = new RelayCommand(obj =>
					{
						ObservableCollection<GroupObjectSelector> left, right;
						using (MyKindergardenEntities db = new MyKindergardenEntities())
						{
							var buff = Groups.Collection.Select(a => a.GetModel().Id).ToList();
							left = new ObservableCollection<GroupObjectSelector>
								(db.KinderGroups.Where(a => !buff.Contains(a.Id)).ToList().Select(a => new GroupObjectSelector(a)));
							right = new ObservableCollection<GroupObjectSelector>(Groups.Collection.AsEnumerable());
							CollectionSelectorVM<GroupObjectSelector> selector = new CollectionSelectorVM<GroupObjectSelector>(left, right);
							CollectionSelectorWindow window = new CollectionSelectorWindow(selector);
							selector.AcceptCommand.ExecuteList += a => { Groups.Collection = right; window.Close(); };
							selector.CancelCommand.ExecuteList += a => { window.Close(); };
							window.ShowDialog();
						}
					}));
			}
		}

		IQueryable<VisitNote> groupChecker(IQueryable<VisitNote> query)
		{
			IQueryable<VisitNote> result;
			var buff = Groups.Collection.Select(a => new int?(a.GetModel().Id)).ToList();
			result = query.Where( a => buff.Contains(a.Child.KinderGroup_Id));
			return result;
		}
		IQueryable<VisitNote> dateBetween(IQueryable<VisitNote> query)
		{
			DateTime rightBuff = right.Date.AddDays(1);
			return query.Where(model => model.VisitDate.Date >= left.Date && model.VisitDate.Date < rightBuff);
		}
		public IQueryable<VisitNote> ExecuteVisitNotes(IQueryable<VisitNote> query)
		{
			return groupChecker(dateBetween(query));
		}

		public IQueryable<VisitDate> ExecuteVisitDates(IQueryable<VisitDate> query)
		{
			DateTime rightBuff = right.Date.AddDays(1);
		return query.Where(model => model.Date >= left.Date && model.Date < rightBuff);
		}
		public IQueryable<KinderGroup> ExecuteGroups(IQueryable<KinderGroup> query)
		{
			var buff = Groups.Collection.Select(a => new int?(a.GetModel().Id)).ToList();
			return query.Where(model => buff.Contains(model.Id));
		}
	}
}
