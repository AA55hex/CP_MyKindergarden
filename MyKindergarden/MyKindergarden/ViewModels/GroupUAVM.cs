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
	public class ChildObjectSelector : ModelShellVM<Child>
	{
		public ChildObjectSelector(Child mdl) : base(mdl) { }
		public string Ребенок => model.LastName + " " + model.FirstName[0] + "." + model.MiddleName[0] + ".";
	}
	public class TeacherObjectSelector : ModelShellVM<Client>
	{
		public TeacherObjectSelector(Client mdl) : base(mdl) { }
		public string Воспитатель => model.LastName + " " + model.FirstName[0] + "." + model.MiddleName[0] + ".";
	}
	public abstract class GroupUAVM : ViewModelBase
	{
		public GroupUAVM(KinderGroup mdl, MyKindergardenEntities ctx)
		{
			model = mdl;
			context = ctx;

		}
		protected void LoadData()
		{
			Types.Collection = new ObservableCollection<GroupType>(context.GroupTypes);
			Types.SelectedItem = model.GroupType;
		}

		protected List<Child> children;
		protected List<Client> teachers;
		protected KinderGroup model;
		protected MyKindergardenEntities context;


		public List<Child> Children
		{
			get => children;
			set
			{
				children = value;
				OnPropertyChanged();
			}
		}
		public List<Client> Teachers
		{
			get => teachers;
			set
			{
				teachers = value;
				OnPropertyChanged();
			}
		}
		public KinderGroup Model => model;
		public CollectionViewModel<GroupType> Types { get; private set; } = new CollectionViewModel<GroupType>();


		protected RelayCommand selectChildrenCommand;
		protected RelayCommand selectTeachersCommand;
		protected RelayCommand updateCommand;
		protected RelayCommand cancelCommand;

		public virtual RelayCommand SelectChildrenCommand
		{
			get
			{
				return selectChildrenCommand ??
					(selectChildrenCommand = new RelayCommand(obj =>
					{

						ObservableCollection<ChildObjectSelector> left, right;

						left = new ObservableCollection<ChildObjectSelector>
						(
							context.Children
							.Where(a => a.KinderGroup_Id == null)
							.Where(a => a.VisitStart <= DateTime.Now && (a.VisitEnd == null || a.VisitEnd >= DateTime.Now))
							.ToList()
							.Where(a => !children.Contains(a))
							.Select(a => new ChildObjectSelector(a))
						);
						right = new ObservableCollection<ChildObjectSelector>(children.Select(a => new ChildObjectSelector(a)));

						CollectionSelectorVM<ChildObjectSelector> selector = new CollectionSelectorVM<ChildObjectSelector>(left, right);
						selector.AcceptCommand.ExecuteList += a => Children = right.Select(b => b.GetModel()).ToList();
						//open
						CollectionSelectorWindow window = new CollectionSelectorWindow(selector);
						selector.CancelCommand.ExecuteList += a => window.Close();
						selector.AcceptCommand.ExecuteList += a => window.Close();
						window.ShowDialog();
					}));
			}
		}
		public virtual RelayCommand SelectTeachersCommand
		{
			get
			{
				return selectTeachersCommand ??
					(selectTeachersCommand = new RelayCommand(obj =>
					{

						ObservableCollection<TeacherObjectSelector> left, right;

						left = new ObservableCollection<TeacherObjectSelector>
						(
							context.Clients
								.Where(a => a.IsActive && a.ClientType_Id != 0)
								.ToList()
								.Where(a => !teachers.Contains(a))
								.Select(a => new TeacherObjectSelector(a))
						);
						right = new ObservableCollection<TeacherObjectSelector>(teachers.Select(a => new TeacherObjectSelector(a)));
						CollectionSelectorVM<TeacherObjectSelector> selector = new CollectionSelectorVM<TeacherObjectSelector>(left, right);
						selector.AcceptCommand.ExecuteList += o => Teachers = right.Select(a => a.GetModel()).ToList();
						//open
						CollectionSelectorWindow window = new CollectionSelectorWindow(selector);
						selector.CancelCommand.ExecuteList += a => window.Close();
						selector.AcceptCommand.ExecuteList += a => window.Close();
						window.ShowDialog();
					}));
			}
		}
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
			return model.GroupName.Length != 0 &&
				context.KinderGroups.Where(a => a.GroupName == model.GroupName).AsEnumerable().Where(a => !a.Equals(model)).FirstOrDefault() == null;
		}
	}
	public class GroupUpdateVM : GroupUAVM
	{
		public GroupUpdateVM(KinderGroup mdl, MyKindergardenEntities ctx) : base(mdl, ctx)
		{
			context.Entry(model).Reference("GroupType").Load();
			context.Entry(model).Collection("Clients").Load();
			context.Entry(model).Collection("Children").Load();
			LoadData();
			children = new List<Child>(model.Children);
			teachers = new List<Client>(model.Clients);
		}
		public override RelayCommand UpdateCommand
		{
			get
			{
				return updateCommand ??
					(updateCommand = new RelayCommand(obj =>
					{
						model.Children = children;
						model.Clients = teachers;
						model.GroupType = Types.SelectedItem;
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
	public class GroupCreateVM : GroupUAVM
	{
		public GroupCreateVM(KinderGroup mdl, MyKindergardenEntities ctx) : base(mdl, ctx)
		{
			LoadData();
			
			children = new List<Child>();
			teachers = new List<Client>();
		}

		public override RelayCommand UpdateCommand
		{
			get
			{
				return updateCommand ??
					(updateCommand = new RelayCommand(obj =>
					{
						model.Children = children;
						model.Clients = teachers;
						model.GroupType = Types.SelectedItem;
						context.KinderGroups.Add(model);
						context.SaveChanges();
					}, CanUpdate));
			}
		}
	}
}
