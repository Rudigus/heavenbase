﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
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
        string chosenPath = @"C:\Nexon\Library\maplestory\appdata";

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
                if (string.IsNullOrEmpty(PathTextBox.Text) || !FamiliarDataSourceProvider.PathIsValid(PathTextBox.Text))
                {
                    MessageBox.Show("The .wz files were not found.", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                SearchTextBox.IsEnabled = false;
                LoadButton.IsEnabled = false;
                Thread loadDataThread = new Thread(asyncLoad);
                /* 
                 * If the executable is terminated, there's no point in keeping the loading
                 * process alive, so the thread is a background thread. If it was something
                 * that made sense running even after executable termination, it would be
                 * well suited for a foreground thread.
                 */
                loadDataThread.IsBackground = true;
                TabItem activeTab = (TabItem)DataPicker.SelectedItem;
                loadDataThread.Name = activeTab.Name.Substring(0, activeTab.Name.Length - 3);
                string presentedTabName = ((TextBlock)((StackPanel)activeTab.Header).Children[1]).Text;
                LoadingTimeBox.Text = $"Loading {presentedTabName}...";
                loadDataThread.Start();
            }
            catch (IOException)
            {
                MessageBox.Show("The .wz files are being used by another application.", "Access Conflict", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void asyncLoad()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            string threadName = Thread.CurrentThread.Name;
            if (threadName == "Familiar")
            {
                List<Familiar> itemsSource = FamiliarDataSourceProvider.LoadCollectionData(chosenPath);
                this.Dispatcher.Invoke((Action)(() =>
                {//this refer to form in WPF application 
                    FamiliarGrid.ItemsSource = itemsSource;
                    stopwatch.Stop();
                    TimeSpan timespan = stopwatch.Elapsed;
                    LoadingTimeBox.Text = $"Loading Time: {timespan.Minutes:D2}:{timespan.Seconds:D2}:{timespan.Milliseconds:D2}";
                    SearchTextBox.IsEnabled = true;
                    LoadButton.IsEnabled = true;
                }));
            }
            else
            {
                List<Equip> itemsSource = FamiliarDataSourceProvider.LoadEquipData(chosenPath, threadName);
                this.Dispatcher.Invoke((Action)(() =>
                {//this refer to form in WPF application 
                    GetActiveGrid().ItemsSource = itemsSource;
                    stopwatch.Stop();
                    TimeSpan timespan = stopwatch.Elapsed;
                    LoadingTimeBox.Text = $"Loading Time: {timespan.Minutes:D2}:{timespan.Seconds:D2}:{timespan.Milliseconds:D2}";
                    SearchTextBox.IsEnabled = true;
                    LoadButton.IsEnabled = true;
                }));
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
            dgr.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFdcdcdc"));
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

        #region SearchFilter
        /// <summary>
        /// Make the datagrid show only the elements which share the textbox's text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filterText = ((TextBox)sender).Text;
            var itemsSource = GetActiveGrid().ItemsSource;
            ICollectionView cv = CollectionViewSource.GetDefaultView(itemsSource);

            if (!string.IsNullOrEmpty(filterText))
            {
                cv.Filter = o =>
                {
                    /* change to get data row value */
                    foreach (PropertyInfo property in o.GetType().GetProperties())
                    {
                        if (property.PropertyType == typeof(string) || property.PropertyType == typeof(int))
                        {
                            if (property.GetValue(o).ToString().ToUpper().Contains(filterText.ToUpper()))
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                };
            }
            else
            {
                cv.Filter = null;
            }
        }

        #endregion

        #region RowCheckbox
        private void RowSelectionCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            FamiliarGrid.SelectionUnit = DataGridSelectionUnit.FullRow;
        }

        private void RowSelectionCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            FamiliarGrid.SelectionUnit = DataGridSelectionUnit.CellOrRowHeader;
        }
        #endregion

        #region Other
        private void DataPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<TabItem> tabs = GetTabs();
            List<DataGrid> datagrids = GetDatagrids();

            TabItem chosenTab = ((sender as TabControl).SelectedItem as TabItem);
            if(chosenTab.Name == "FamiliarTab")
            { 
                datagrids[0].Visibility = Visibility.Visible;
                FamiliarInfoBox.Visibility = Visibility.Visible;
                datagrids[1].Visibility = Visibility.Collapsed;
                EquipInfoBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                datagrids[0].Visibility = Visibility.Collapsed;
                FamiliarInfoBox.Visibility = Visibility.Collapsed;
                datagrids[1].Visibility = Visibility.Visible;
                EquipInfoBox.Visibility = Visibility.Visible;
            }
            /*
            foreach (TabItem tab in tabs)
            {
                if(tab.Name == chosenTab.Name)
                {
                    // datagrids[tabs.IndexOf(tab)].Visibility = Visibility.Visible;
                    if (tab.Name == "FamiliarTab")
                    {
                        datagrids[0].Visibility = Visibility.Visible;
                    }
                    else
                    {
                        datagrids[1].Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    // datagrids[tabs.IndexOf(tab)].Visibility = Visibility.Collapsed;
                    if (tab.Name == "FamiliarTab")
                    {
                        datagrids[0].Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        datagrids[1].Visibility = Visibility.Collapsed;
                    }
                }
            }*/
        }

        private DataGrid GetActiveGrid()
        {
            List<DataGrid> datagrids = GetDatagrids();

            foreach (DataGrid datagrid in datagrids)
            {
                if(datagrid.Visibility == Visibility.Visible)
                {
                    return datagrid;
                }
            }
            return null;
        }

        private List<TabItem> GetTabs()
        {
            return new List<TabItem>()
            {
                FamiliarTab,
                WeaponTab,
                CapTab,
            };
        }

        private List<DataGrid> GetDatagrids()
        {
            return new List<DataGrid>()
            {
                FamiliarGrid,
                EquipGrid,
            };
        }

        private void FamiliarGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var selectedCells = FamiliarGrid.SelectedCells;
            if(selectedCells.Count == 0)
            {
                ResetInfoPage();
                return;
            }
            Familiar familiar = selectedCells[0].Item as Familiar;
            foreach (var selectedCell in selectedCells)
            {
                Familiar selectedFamiliar = selectedCell.Item as Familiar;
                if(selectedFamiliar.FamiliarID != familiar.FamiliarID)
                {
                    ResetInfoPage();
                    return;
                }
            }
            MobImage.Source = FamiliarDataSourceProvider.CreateBitmapSourceFromGdiBitmap(familiar.MobImage);
            MobLevel.Text = $"Lv. {familiar.Level} ";
            MobName.Text = familiar.MobName;

            MobRarity.Text = $"\nRarity: {familiar.Rarity}";

            MobATT.Text = $"\nATT: {familiar.ATT}";
            MobRange.Text = $"Range: {familiar.Range}";

            MobSkillName.Text = $"\nSkill Name: {familiar.SkillName}";
            MobSkillCategory.Text = $"Skill Category: {familiar.SkillCategory}";
            MobSkillDescription.Text = $"Skill Description: {familiar.SkillDescription}";  

            MobPassiveEffect.Text = $"\nPassive Effect: {familiar.PassiveEffect}";
            MobPassiveEffectBonus.Text = $"Passive Effect Bonus: {familiar.PassiveEffectBonus}";
            MobPassiveEffectTarget.Text = $"Passive Effect Target: {familiar.PassiveEffectTarget}";

            MobID.Text = $"\nMob ID: {familiar.MobID}";
            MobCardID.Text = $"Card ID: {familiar.CardID}";
            MobPassiveEffectID.Text = $"Passive Effect ID: {familiar.PassiveEffectID}";
        }

        private void ResetInfoPage()
        {
            MobImage.Source = null;
            MobLevel.Text = "";
            MobName.Text = "";
            MobATT.Text = "";
            MobRarity.Text = "";
            MobSkillName.Text = "";
            MobSkillCategory.Text = "";
            MobSkillDescription.Text = "";
            MobRange.Text = "";
            MobPassiveEffect.Text = "";
            MobPassiveEffectBonus.Text = "";
            MobPassiveEffectTarget.Text = "";
            MobID.Text = "";
            MobCardID.Text = "";
            MobPassiveEffectID.Text = "";
        }

        private void ResetWeaponInfoPage()
        {
            EquipImage.Source = null;
            EquipLevel.Text = "";
            EquipName.Text = "";
            EquipClassification.Text = "";
            EquipType.Text = "";
            RequiredStats.Text = "";
            RequiredJob.Text = "";
            TotalUpgradeCount.Text = "";
            EquipStats.Text = "";
        }

        private void EquipGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var selectedCells = EquipGrid.SelectedCells;
            if (selectedCells.Count == 0)
            {
                ResetWeaponInfoPage();
                return;
            }
            Equip equip = selectedCells[0].Item as Equip;
            foreach (var selectedCell in selectedCells)
            {
                Equip selectedEquip = selectedCell.Item as Equip;
                if (selectedEquip.EquipID != equip.EquipID)
                {
                    ResetWeaponInfoPage();
                    return;
                }
            }
            EquipImage.Source = FamiliarDataSourceProvider.CreateBitmapSourceFromGdiBitmap(equip.EquipImage);
            EquipLevel.Text = $"Lv. {equip.EquipLevel} ";
            EquipName.Text = equip.EquipName;

            EquipClassification.Text = $"\nClassification: {equip.EquipClassification}";
            EquipType.Text = $"\nType: {equip.EquipType}";

            RequiredStats.Text = $"\nRequired Stats: {equip.RequiredStats}";
            RequiredJob.Text = $"Required Job: {equip.RequiredJob}";

            EquipStats.Text = $"\nStats:\n{equip.EquipStats}";

            TotalUpgradeCount.Text = $"\nAvailable Enhancements: {equip.TotalUpgradeCount}";
        }
        #endregion

        #region BinaryManipulation
        private void SaveBinaryButton_Click(object sender, RoutedEventArgs e)
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            Directory.CreateDirectory(Path.Combine(dir, "HeavenBase"));
            string serializationFile = GetSerializationFile(DataPicker, dir);
            var itemsSource = GetActiveGrid().ItemsSource;
            if (itemsSource != null)
            {
                //serialize
                using (Stream stream = File.Open(serializationFile, FileMode.Create))
                {
                    var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    bformatter.Serialize(stream, itemsSource);
                }
            }
            else
            {
                MessageBox.Show("There isn't any data to save.", "Empty Data", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadBinaryButton_Click(object sender, RoutedEventArgs e)
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string serializationFile = GetSerializationFile(DataPicker, dir);
            if (File.Exists(serializationFile))
            {
                //deserialize
                using (Stream stream = File.Open(serializationFile, FileMode.Open))
                {
                    var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    var data = bformatter.Deserialize(stream);
                    if (((TabItem)DataPicker.SelectedItem).Name == "FamiliarTab")
                    {
                        List<Familiar> familiars = (List<Familiar>)data;
                        GetActiveGrid().ItemsSource = familiars;
                    }
                    else
                    {
                        List<Equip> equips = (List<Equip>)data;
                        GetActiveGrid().ItemsSource = equips;
                    }
                }
            }
            else
            {
                MessageBox.Show("The binary file was not found.", "File not Found", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetSerializationFile(TabControl tabControl, string dir)
        {
            // A tab named FamiliarTab would be turned into Familiars
            string filename = ((TabItem)tabControl.SelectedItem).Name;
            filename = filename.Substring(0, filename.Length - 3) + "s";
            string serializationFile = Path.Combine(dir, $"HeavenBase/{filename}.bin");
            return serializationFile;
        }
        #endregion
    }
}
