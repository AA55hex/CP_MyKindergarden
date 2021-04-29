using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using MyKindergarden.Windows;

namespace MyKindergarden.ViewModels
{
	public class ChildJournalVM
	{
		public ChildJournalVM(Child mdl, MyKindergardenEntities ctx, DateTime left, DateTime right)
		{
			model = mdl;
			context = ctx;
			this.left = left;
			this.right = right;
			Collection.Source = context.VisitNotes
				.Where(dateBetween)
				.Where(a => a.Child_Id == model.Id).OrderBy(a => a.VisitDate.Date).ToList();
		}

		Child model;
		DateTime left;
		DateTime right;
		DateTime nextRight => right.Date.AddDays(1);
		public CollectionViewSource Collection { get; private set; } = new CollectionViewSource();
		MyKindergardenEntities context;
		public Child Model => model;
		public DateTime Left => left;
		public DateTime Right => right;
		public string FIO => model.LastName + " " + model.FirstName[0] + ". " + model.MiddleName[0] + ".";
		public string Diapasone => Left.ToShortDateString() + " - " + Right.ToShortDateString();
		bool dateBetween(VisitNote note) => note.VisitDate.Date >= left.Date && note.VisitDate.Date < nextRight;
		public int VisitedCount
		{
			get
			{
				return context.VisitNotes.Where(dateBetween).Where(a => a.Child_Id == model.Id && a.Visited == true).Count();
			}
		}
		public int NotVisitedCount
		{
			get
			{
				return context.VisitNotes.Where(dateBetween).Where(a => a.Child_Id == model.Id && a.Visited == false).Count();
			}
		}
		public double VisitRating => (VisitedCount + NotVisitedCount) != 0? VisitedCount / (double)(VisitedCount + NotVisitedCount) : 1;

	}
	public class ChildJournalSelector : ContextSwapperVM
	{
		public ChildJournalSelector(Child model)
		{
			DateRangeSelectorVM selector = new DateRangeSelectorVM();
			ChildJournalVM journal = null;
			ChildStat stat = new ChildStat(this);
			selector.AcceptCommand.ExecuteList += a =>
			{
				journal = new ChildJournalVM(model, new MyKindergardenEntities(), selector.Left, selector.Right);
				SelectedContext = journal;
			};
			selector.CancelCommand.ExecuteList += a => stat.Close();
			SelectedContext = selector;
			stat.ShowDialog();
		}
	}
	public class ChildProfileVM : ViewModelBase
	{
		public ChildProfileVM(Child ch)
		{
			model = ch;
		}

		Child model;
		RelayCommand getStatCommand;
		public RelayCommand GetStatCommand
		{
			get
			{
				return getStatCommand ??
					(getStatCommand = new RelayCommand(obj =>
					{
						ChildJournalSelector journal = new ChildJournalSelector(Model);
					}));
			}
		}
		public Child Model => model;
		public string FIO => model.LastName + " " + model.FirstName[0] + ". " + model.MiddleName[0] + ".";
	}

	public abstract class ChildUAVM : ViewModelBase
	{
		public ChildUAVM(Child mdl, MyKindergardenEntities ctx)
		{
			model = mdl;
			context = ctx;
		}

		protected Child model;
		protected MyKindergardenEntities context;

		public Child Model => model;
		public CollectionViewModel<KinderGroup> Groups { get; private set; } = new CollectionViewModel<KinderGroup>();
		public string UpdString { get; set; }


		protected RelayCommand updateCommand;
		protected RelayCommand cancelCommand;

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

		protected void LoadData()
		{
			Groups.Collection = new ObservableCollection<KinderGroup>(context.KinderGroups.ToList());
			Groups.SelectedItem = model.KinderGroup;
		}
		protected virtual bool CanUpdate(object o)
		{
			return model.FirstName.Length != 0 &&
				model.LastName.Length != 0 &&
				model.MiddleName.Length != 0 &&
				!(model.VisitEnd != null && model.VisitStart > model.VisitEnd)
				&& model.BirthDate < model.VisitStart;
		}
	}
	public class ChildUpdateVM : ChildUAVM
	{
		public ChildUpdateVM(Child ch, MyKindergardenEntities ctx) : base(ch, ctx)
		{
			context.Entry(model).Reference("KinderGroup").Load();
			UpdString = "Изменить";
			LoadData();
		}
		public override RelayCommand UpdateCommand
		{
			get
			{
				return updateCommand ??
					(updateCommand = new RelayCommand(obj =>
					{
						model.KinderGroup = Groups.SelectedItem;
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
						context.Entry(model).CurrentValues.SetValues(context.Entry(model).OriginalValues);
					}));
			}
		}
	}
	public class ChildCreateVM : ChildUAVM
	{
		public ChildCreateVM(Child ch, MyKindergardenEntities ctx) : base(ch, ctx)
		{
			ch.BirthDate = DateTime.Today;
			LoadData();
			UpdString = "Добавить";
		}
		public override RelayCommand UpdateCommand
		{
			get
			{
				return updateCommand ??
					(updateCommand = new RelayCommand(obj =>
					{
						model.KinderGroup = Groups.SelectedItem;
						context.Children.Add(model);
						context.SaveChanges();
					}, CanUpdate));
			}
		}
	}
}
