﻿using System;
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
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace MyKindergarden.Windows
{
	/// <summary>
	/// Логика взаимодействия для ChildProfile.xaml
	/// </summary>
	public partial class ChildProfile : MetroWindow
	{
		public ChildProfile(ChildProfileVM model)
		{
			InitializeComponent();
			DataContext = model;
		}
	}
}
