﻿using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WinForms = System.Windows.Forms;

namespace HeavenBase
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string chosenPath;

        #region Constructor
        /// <summary>
        /// Initializes things.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }
        #endregion

        #region LoadDataGrid
        /// <summary>
        /// Loads all DataGrid's info.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(PathTextBox.Text) || !FamiliarProperties.PathIsValid(PathTextBox.Text))
                {
                    MessageBox.Show("The .wz files were not found.", "Invalid Folder Path", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                familiarGrid.AutoGeneratedColumns += DataGrid_AutoGeneratedColumns;
                familiarGrid.ItemsSource = FamiliarProperties.LoadCollectionData(chosenPath);
            }
            catch (IOException)
            {
                MessageBox.Show("The .wz files are being used by another application.", "Access Conflict", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region FolderDialog
        /// <summary>
        /// Gets the root folder for the .wz archives (ideally).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PathTextBox_Click(object sender, RoutedEventArgs e)
        {
            WinForms.FolderBrowserDialog fbd = new WinForms.FolderBrowserDialog();
            WinForms.DialogResult result = fbd.ShowDialog();
            if (result == WinForms.DialogResult.OK)
            {
                chosenPath = fbd.SelectedPath;
                ((TextBox)sender).Text = chosenPath;
            }
        }
        #endregion

        #region RowHighlight
        /// <summary>
        /// Makes the row of the selected cell highlighted as well.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventSetter_OnHandlerSelected(object sender, RoutedEventArgs e)
        {
            DataGridRow dgr = FindParent<DataGridRow>(sender as DataGridCell);
            dgr.Background = new SolidColorBrush(Colors.Gold);
        }

        private void EventSetter_OnHandlerLostFocus(object sender, RoutedEventArgs e)
        {
            DataGridRow dgr = FindParent<DataGridRow>(sender as DataGridCell);
            dgr.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFdcdcdc"));
        }

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }
        #endregion

        #region ColumnInitialSorting
        /// <summary>
        /// Changes the datagrid's initial sorting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DataGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {
            var firstCol = familiarGrid.Columns[0];
            firstCol.SortDirection = ListSortDirection.Ascending;
            familiarGrid.Items.SortDescriptions.Add(new SortDescription(familiarGrid.Columns[0].SortMemberPath, ListSortDirection.Ascending));
        }
        #endregion

        #region SearchFilter
        /// <summary>
        /// Make the datagrid show only the elements which share the textbox's text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filterText = ((TextBox)sender).Text;
            ICollectionView cv = CollectionViewSource.GetDefaultView(familiarGrid.ItemsSource);

            if (!string.IsNullOrEmpty(filterText))
            {
                cv.Filter = o =>
                {
                    /* change to get data row value */
                    Familiar p = o as Familiar;
                    return p.FamiliarID.ToString().ToUpper().Contains(filterText.ToUpper()) ||
                    p.MobName.ToUpper().Contains(filterText.ToUpper()) ||
                    p.CardName.ToUpper().Contains(filterText.ToUpper()) ||
                    p.Rarity.ToUpper().Contains(filterText.ToUpper()) ||
                    p.SkillName.ToUpper().Contains(filterText.ToUpper()) ||
                    p.SkillDescription.ToUpper().Contains(filterText.ToUpper()) ||
                    p.Range.ToString().ToUpper().Contains(filterText.ToUpper()) ||
                    p.PassiveEffect.ToUpper().Contains(filterText.ToUpper()) ||
                    p.MobID.ToString().ToUpper().Contains(filterText.ToUpper()) ||
                    p.CardID.ToString().ToUpper().Contains(filterText.ToUpper()) ||
                    p.SkillID.ToString().ToUpper().Contains(filterText.ToUpper()) ||
                    p.PassiveEffectID.ToString().ToUpper().Contains(filterText.ToUpper()) ||
                    p.Level.ToString().ToUpper().Contains(filterText.ToUpper()) ||
                    p.ATT.ToString().ToUpper().Contains(filterText.ToUpper()) ||
                    p.PassiveEffectBonus.ToString().ToUpper().Contains(filterText.ToUpper()) ||
                    p.PassiveEffectTarget.ToUpper().Contains(filterText.ToUpper()) ||
                    p.SkillCategory.ToUpper().Contains(filterText.ToUpper());
                    /* end change to get data row value */
                };
            }
            else
            {
                cv.Filter = null;
            }
        }

        #endregion

        private void RowSelectionCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            familiarGrid.SelectionUnit = DataGridSelectionUnit.FullRow;
        }

        private void RowSelectionCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            familiarGrid.SelectionUnit = DataGridSelectionUnit.CellOrRowHeader;
        }
    }
}
