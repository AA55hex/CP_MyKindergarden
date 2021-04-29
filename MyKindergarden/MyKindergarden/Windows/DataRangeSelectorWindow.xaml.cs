using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MyKindergarden.ViewModels;
namespace MyKindergarden.Windows
{
	/// <summary>
	/// Логика взаимодействия для DataRangeSelectorWindow.xaml
	/// </summary>
	public partial class DataRangeSelectorWindow : Window
	{
		public DataRangeSelectorWindow(DateRangeSelectorVM vm)
		{
			InitializeComponent();
			DataContext = vm;
		}
	}
}
