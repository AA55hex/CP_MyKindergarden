using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data.Entity;
using System.Windows.Data;

namespace MyKindergarden.ViewModels
{
	public abstract class ListCommandsVM<T> : ViewModelBase
	{
		public ListCommandsVM(T lst)
		{
			models = lst;
		}
		protected T models;
		protected RelayCommand addCommand;
		protected RelayCommand updateCommand;
		protected RelayCommand deleteCommand;
		protected RelayCommand profileCommand;
		protected RelayCommand reFreshCommand;

		public abstract RelayCommand DeleteCommand { get; }
		public abstract RelayCommand AddCommand { get; }
		public abstract RelayCommand UpdateCommand { get; }
		public abstract RelayCommand ProfileCommand { get; }
		public abstract RelayCommand ReFreshCommand { get; }
	}
	public abstract class ModelShellVM<T> : ViewModelBase
	{
		public ModelShellVM(T mdl)
		{
			model = mdl;
		}
		protected T model;
		public T GetModel() => model;
	}
	public abstract class ModelList<T> : ViewModelBase
	{
		public ModelList(CurrentUserVM usr)
		{
			user = usr;
		}

		protected T selectedItem;
		protected MyKindergardenEntities context;
		protected CurrentUserVM user;

		public T SelectedItem
		{
			get { return selectedItem; }
			set
			{
				selectedItem = value;
				OnPropertyChanged();
			}
		}
		public CollectionViewSource Collection { get; private set; } = new CollectionViewSource();
		public MyKindergardenEntities Context => context;
		public CurrentUserVM User => user;

		~ModelList()
		{
			context.Dispose();
			
		}
	}
}
