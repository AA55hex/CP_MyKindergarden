using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Input;
using System.Data.Entity;
using System.Windows.Data;
using System.Runtime.CompilerServices;

namespace MyKindergarden
{
    public class RelayCommand : ICommand
    {
        private Action<object> execute;
        private Func<object, bool> canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        public event Action<object> ExecuteList
		{
            add { execute += value; }
            remove { execute -= value; }
        }
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            this.execute(parameter);
        }
    }
    public abstract class ViewModelBase : INotifyPropertyChanged
    {


        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyname = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }
    }
    public class CollectionViewModel<T> : ViewModelBase
	{
        private T selectedItem;
        public T SelectedItem
        {
            get { return selectedItem; }
            set
            {
                selectedItem = value;
                OnPropertyChanged();
            }
        }
        ObservableCollection<T> collection;
        public ObservableCollection<T> Collection
		{
            get => collection;
			set
			{
                collection = value;
                OnPropertyChanged();
            }

        }
    }
    public class PropertyViewModel<T> : ViewModelBase
    {
        private T property;
        public T Property
        {
            get { return property; }
            set
            {
                property = value;
                OnPropertyChanged();
            }
        }
    }
    public class CollectionSelectorVM<T> : ViewModelBase where T : class
    {
        public CollectionSelectorVM(ObservableCollection<T> left, ObservableCollection<T> right)
        {
            Left.Collection = left;
            Right.Collection = right;
        }


        RelayCommand toLeftCommand;
        RelayCommand toRightCommand;
        RelayCommand allToLeftCommand;
        RelayCommand allToRightCommand;
        RelayCommand cancelCommand;
        RelayCommand acceptCommand;


        public CollectionViewModel<T> Left { get; private set; } = new CollectionViewModel<T>();
        public CollectionViewModel<T> Right { get; private set; } = new CollectionViewModel<T>();

        public RelayCommand ToLeftCommand
        {
            get
            {
                return toLeftCommand ??
                    (toLeftCommand = new RelayCommand(obj =>
                    {
                        Left.Collection.Add(Right.SelectedItem);
                        Right.Collection.Remove(Right.SelectedItem);
                        Right.SelectedItem = null;
                    },
                    a => Right.SelectedItem != null));
            }
        }
        public RelayCommand ToRightCommand
        {
            get
            {
                return toRightCommand ??
                    (toRightCommand = new RelayCommand(obj =>
                    {
                        Right.Collection.Add(Left.SelectedItem);
                        Left.Collection.Remove(Left.SelectedItem);
                        Left.SelectedItem = null;
                    },
                    a => Left.SelectedItem != null));
            }
        }
        public RelayCommand AllToLeftCommand
        {
            get
            {
                return allToLeftCommand ??
                    (allToLeftCommand = new RelayCommand(obj =>
                    {
                        
                        foreach (var buff in Right.Collection)
                        {
                            Left.Collection.Add(buff);
                        }
                        Right.Collection.Clear();
                        Right.SelectedItem = null;
                    }));
            }
        }
        public RelayCommand AllToRightCommand
        {
            get
            {
                return allToRightCommand ??
                    (allToRightCommand = new RelayCommand(obj =>
                    {
                        foreach (var buff in Left.Collection)
                        {
                            Right.Collection.Add(buff);
                        }
                        Left.Collection.Clear();
                        Left.SelectedItem = null;
                    }));
            }
        }
        public RelayCommand AcceptCommand
        {
            get
            {
                return acceptCommand ??
                    (acceptCommand = new RelayCommand(obj => { }));
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

}
