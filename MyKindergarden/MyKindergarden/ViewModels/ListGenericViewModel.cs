using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data.Entity;

namespace MyKindergarden.ViewModels
{
	interface ListGenericViewModel<T> where T : class
	{
		T SelectedItem
		{
			get;
			set;
		}
		ObservableCollection<T> Collection
		{
			get;
			set;
		}
	}
}
