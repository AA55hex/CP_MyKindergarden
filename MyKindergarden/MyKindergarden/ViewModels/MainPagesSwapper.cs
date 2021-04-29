using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyKindergarden.ViewModels
{
	public abstract class ContextSwapperVM : ViewModelBase
	{
		protected object selectedContext;
		public object SelectedContext
		{
			get => selectedContext;
			set
			{
				selectedContext = value;
				OnPropertyChanged();
			}
		}
	}
	public class DateUpdater
	{
		public DateUpdater()
		{
			context = new MyKindergardenEntities();
		}
		MyKindergardenEntities context;
		public void UpdateDates()
		{
			context.UpdateVisitNoteDates();
			context.SaveChanges();
		}

		public void InsertDates(DateTime left, DateTime right)
		{
			context.Insert_dates(left, right);
			context.SaveChanges();
		}

		~DateUpdater()
		{
			context.Dispose();
		}
	}
	public class MainPagesSwapperVM : ContextSwapperVM
	{
		public MainPagesSwapperVM()
		{
			DateUpdater dateUpdater = new DateUpdater();
			dateUpdater.InsertDates(DateTime.Today, DateTime.Today.AddDays(30));
			dateUpdater.UpdateDates();

			ClientLogInVM buff = new ClientLogInVM();
			buff.LogInCommand.ExecuteList += obj =>
				{
					if (buff.LogInStatus)
					{
						user = new CurrentUserVM(buff.User);
						SelectedContext = new TabSwapperVM(user);
					}
				};
			SelectedContext = buff;
		}
		
		CurrentUserVM user;
	}

	public class TabSwapperVM : ContextSwapperVM
	{
		public TabSwapperVM(CurrentUserVM usr)
		{
			user = usr;
			SelectedIndex = 0;
			SelectedContext = ctxSelector();
		}

		int selectedIndex = 0;
		public int SelectedIndex
		{
			get => selectedIndex;
			set
			{
				selectedIndex = value;
				OnPropertyChanged();
				SelectedContext = ctxSelector();
			}
		}

		object ctxSelector()
		{
			switch(selectedIndex)
			{
				case 0:
					return new ChildListVM(user);
				case 1:
					return new ClientListVM(user);
				case 2:
					return new GroupListVM(user);
				case 3:
					return new VisitNoteListVM(user);
				case 4:
					return new StatVM();
				default:
					return null;
			}
		}

		CurrentUserVM user;
	}
}
