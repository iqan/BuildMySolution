using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;

namespace BuildMySolution
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            SetPaths();
        }

        private void SetPaths()
        {
            _vstoolspath = ConfigurationManager.AppSettings["msbuildpath"] ?? @"e.g. C:\Program Files (x86)\MSBuild\12.0\Bin\";
            TxtPathCmd.Text = _vstoolspath;
            TxtPathCmd.Foreground = Brushes.Black;
            _path = ConfigurationManager.AppSettings["solutionpath"] ?? string.Empty;
            TxtPath.Text = _path;
        }

        private static IEnumerable<string> _solutionNames;
        private static string _path;
        private static string _vstoolspath;

        #region Click Methods

        private void BtnSaveSettings_OnClick(object sender, RoutedEventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Windows.Forms.Application.ExecutablePath);

            if (!string.IsNullOrEmpty(TxtPathCmd.Text))
            {
                config.AppSettings.Settings.Remove("msbuildpath");
                config.AppSettings.Settings.Add("msbuildpath", TxtPathCmd.Text);
            }
            if (!string.IsNullOrEmpty(TxtPath.Text))
            {
                config.AppSettings.Settings.Remove("solutionpath");
                config.AppSettings.Settings.Add("solutionpath", TxtPath.Text);
            }
            config.Save(ConfigurationSaveMode.Modified);
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            SetPath("");
            TxtPath.Text = _path;
        }

        private void BtnBrowseVs_OnClick(object sender, RoutedEventArgs e)
        {
            SetPath("vstools");
            TxtPathCmd.Text = _vstoolspath;
            TxtPathCmd.Foreground = Brushes.Black;
        }

        private void BtnGetAll_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(_path))
            {
                _solutionNames =
                    Directory.GetFiles(_path, "*.*", SearchOption.AllDirectories)
                        .Where(f => f.EndsWith(".vbproj") || f.EndsWith(".csproj") || f.EndsWith(".sln"));
                ShowDataGrid(string.Empty);
                DgSolutions.Visibility = Visibility.Visible;
            }
        }

        private void BtnSearchSolution_Click(object sender, RoutedEventArgs e)
        {
            if(DgSolutions.Items!=null)
                ShowDataGrid(TxtSearch.Text);
        }

        private void Build_OnClick(object sender, RoutedEventArgs e)
        {
            string solution = GetSolutionName(sender);

            if (!string.IsNullOrEmpty(solution))
            {
                ExecuteCommand(solution + "\"");
            }
        }

        private void Clean_OnClick(object sender, RoutedEventArgs e)
        {
            string solution = GetSolutionName(sender);

            if (!string.IsNullOrEmpty(solution))
            {
                ExecuteCommand(solution + "\" /t:clean");
            }
        }

        private void Rebuild_OnClick(object sender, RoutedEventArgs e)
        {
            string solution = GetSolutionName(sender);

            if (!string.IsNullOrEmpty(solution))
            {
                ExecuteCommand(solution + "\" /t:rebuild");
            }
        }
        #endregion


        private void SetPath(string type)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            DialogResult result = fbd.ShowDialog();

            if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                if(type=="vstools")
                    _vstoolspath = fbd.SelectedPath;
                else
                    _path = fbd.SelectedPath;
            }
        }

        private void ShowDataGrid(string searchword)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("SolutionName");

            if (_solutionNames.Any())
            {
                foreach (var name in _solutionNames)
                {
                    if (name.ToUpper().Contains(searchword.ToUpper()))
                        dt.Rows.Add(name);

                }

                dt.DefaultView.Sort = "SolutionName asc";
                dt = dt.DefaultView.ToTable();
                DgSolutions.ItemsSource = dt.DefaultView;
            }
        }

        private string GetSolutionName(object sender)
        {
            try
            {

                for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
                    if (vis is DataGridRow)
                    {
                        var row = (DataGridRow)vis;
                        return ((DataRowView)row.Item).Row[0].ToString();
                    }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error");
            }
            return string.Empty;
        }

        private static void ExecuteCommand(string solutionFile)
        {
            try
            {
            var msBuild = Path.Combine(_vstoolspath, "msbuild.exe");
            var command = string.Format("/K \"\"{0}\" \"{1}\" /nologo", msBuild, solutionFile);
            
            if (File.Exists(msBuild))
            {
                Process cmd = Process.Start("CMD.exe", command);
                cmd.WaitForExit();
                cmd.Close();
            }
            else
                System.Windows.Forms.MessageBox.Show("Please set MSBUILD path.", "Error");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error");
            }
        }
    }
}
