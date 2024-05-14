using EnvDTE;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.VisualStudio.TextManager.Interop;  // For text buffer services
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using System.Windows.Media;
using EnvDTE80;

namespace UnrealWizard
{
   /// <summary>
   /// Interaction logic for UEClassWindowControl.xaml
   /// </summary>
   public partial class UEClassWindowControl : UserControl
   {
      public string FilterName { get; set; }
      public string FilterFullPath { get; set; }
      public string FilterRelativePath { get; set; }
      public ProjectItem SelectedFilter { get; set; }

      public string HeaderFileName { get; set; }
      public string CppFileName { get; set; }


      public string HeaderFileFullPath { get; set; }
      public string CppFileFullPath { get; set; }

      public bool ShouldCreateSubfolders { get; set; }

      public bool ShouldRegenerateProjectFiles { get; set; }

      private ProjectItem PublicFilter { get; set; }
      private ProjectItem PrivateFilter { get; set; }
      private ProjectItem ParentFilter { get; set; }

      [Import]
      internal ITextEditorFactoryService TextEditorFactoryService { get; set; }

      [Import]
      internal ITextBufferFactoryService TextBufferFactoryService { get; set; }

      [Import]
      public IContentTypeRegistryService ContentTypeRegistryService { get; set; }

      private PreviewControl HeaderPreviewControl;
      private PreviewControl CppPreviewControl;

      private ParentClassEnum SelectedParentClass;

      private System.Windows.Controls.TabItem HeaderTab;
      private System.Windows.Controls.TabItem CppTab;

      public UEClassWindow window;

      private bool ShouldCreateCppFile
      {
         get
         {
            return SelectedParentClass != ParentClassEnum.Interface;
         }
      }

      public bool ShouldCreateEmptyFiles { get; set; }
      public bool ShouldCreateParentFolder { get; set; }

      public string HeaderContent { get; set; }

      public string SourceContent { get; set; }

      public UEClassWindowControl()
      {
         InitializeComponent();
         Compose();

         ThreadHelper.ThrowIfNotOnUIThread();

         HeaderPreviewControl = new PreviewControl();
         CppPreviewControl = new PreviewControl();

         // Apply styles to the TabControl
         TabbedPreview.Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
         TabbedPreview.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 63, 63, 70));
         TabbedPreview.BorderThickness = new Thickness(1);

         HeaderTab = new System.Windows.Controls.TabItem();
         HeaderTab.Header = ".h";
         CppTab = new System.Windows.Controls.TabItem();
         CppTab.Header = ".cpp";

         var contentType = ContentTypeRegistryService.GetContentType("C/C++");
         var headerTextBuffer = TextBufferFactoryService.CreateTextBuffer("", contentType);
         var cppTextBuffer = TextBufferFactoryService.CreateTextBuffer("", contentType);
         HeaderPreviewControl.Initialize(TextEditorFactoryService, headerTextBuffer);
         CppPreviewControl.Initialize(TextEditorFactoryService, cppTextBuffer);

         HeaderTab.Content = HeaderPreviewControl;
         CppTab.Content = CppPreviewControl;

         TabbedPreview.Items.Add(HeaderTab);
         TabbedPreview.Items.Add(CppTab);

         ShouldCreateSubfolders = true;
         ShouldRegenerateProjectFiles = true;
         SelectedParentClass = ParentClassEnum.Actor;
      }

      private void Compose()
      {
         var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
         componentModel.DefaultCompositionService.SatisfyImportsOnce(this);
      }

      private void ClassNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
      {
         if (ClassNameTextBox.Text.Length == 0)
         {
            ClassNameHint.Visibility = Visibility.Visible;
         }
         else
         {
            ClassNameHint.Visibility = Visibility.Hidden;
         }
         UpdateLabels();
         UpdatePreview();
      }

      private void SaveButton_Click(object sender, RoutedEventArgs e)
      {
         if (Directory.Exists(FilterFullPath))
         {
            Directory.CreateDirectory(Path.GetDirectoryName(HeaderFileFullPath));
            File.WriteAllText(HeaderFileFullPath, HeaderContent);

            if (ShouldCreateCppFile)
            {
               Directory.CreateDirectory(Path.GetDirectoryName(CppFileFullPath));
               File.WriteAllText(CppFileFullPath, SourceContent);
            }

            if (ShouldCreateParentFolder)
            {
               CreateParentFilter();
            }

            // Create the VS filters for the created folders
            CreatePublicPrivateFilters();

            // Add the new files to the selected filter or their respective public/private filters
            AddFilesToFilters();

            if (ShouldRegenerateProjectFiles)
            {
               GenerateProjectFiles();
            }
         }
         else
         {
            VS.MessageBox.Show("Error", "Filters in project do not match folder structure");
         }
      }

      private void CancelButton_Click(object sender, RoutedEventArgs e)
      {
         window.Close();
      }

      private void CreateSubfoldersCheckbox_Click(object sender, RoutedEventArgs e)
      {
         ShouldCreateSubfolders = CreateSubfoldersCheckbox.IsChecked.Value;

         UpdateLabels();
      }

      private void UpdateLabels()
      {
         if (ClassNameTextBox.Text.Length > 0)
         {
            HeaderFileName = ClassNameTextBox.Text + ".h";
            CppFileName = ClassNameTextBox.Text + ".cpp";

            string parentPath = ShouldCreateParentFolder ? Path.Combine(FilterFullPath, ClassNameTextBox.Text) : FilterFullPath;

            if (ShouldCreateSubfolders)
            {
               HeaderFileFullPath = Path.Combine(parentPath, "Public", HeaderFileName);
               CppFileFullPath = Path.Combine(parentPath, "Private", CppFileName);
            }
            else
            {
               HeaderFileFullPath = Path.Combine(parentPath, HeaderFileName);
               CppFileFullPath = Path.Combine(parentPath, CppFileName);
            }

            HeaderPathTextBlock.Text = HeaderFileFullPath;
            CppPathTextBlock.Text = CppFileFullPath;

            HeaderTab.Header = HeaderFileName;
            CppTab.Header = CppFileName;
         }
      }

      private void UpdatePreview()
      {
         if (ClassNameTextBox.Text.Length > 0)
         {
            string parentClassName = Enum.GetName(typeof(ParentClassEnum), SelectedParentClass);
            string headerResourceName = parentClassName + "Header.txt";
            string cppResourceName = parentClassName + "Source.txt";

            string content = "";

            // Get the current assembly through which this code is executed
            Assembly assembly = Assembly.GetExecutingAssembly();

            if (!ShouldCreateEmptyFiles)
            {
               // Combine the namespace with the file name. Adjust the namespace to match your project's
               string headerResourcePath = assembly.GetName().Name + ".Resources.Templates." + headerResourceName;

               // Use a stream to access the embedded resource
               using (Stream stream = assembly.GetManifestResourceStream(headerResourcePath))
               {
                  if (stream != null)
                  {
                     using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                     {
                        content = reader.ReadToEnd();
                     }
                  }
               }
               content = content.Replace(@"${CLASS_NAME}", ClassNameTextBox.Text);
               content = content.Replace(@"${API_NAME}", ApiNameTextBox.Text);
            }

            HeaderContent = content;
            HeaderPreviewControl.UpdateText(content);


            if (!ShouldCreateEmptyFiles)
            {
               string cppResourcePath = assembly.GetName().Name + ".Resources.Templates." + cppResourceName;

               // Use a stream to access the embedded resource
               using (Stream stream = assembly.GetManifestResourceStream(cppResourcePath))
               {
                  if (stream != null)
                  {
                     using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                     {
                        content = reader.ReadToEnd();
                     }
                  }
               }
               content = content.Replace(@"${CLASS_NAME}", ClassNameTextBox.Text);
               content = content.Replace(@"${API_NAME}", ApiNameTextBox.Text);
            }

            SourceContent = content;
            CppPreviewControl.UpdateText(content);
         }
      }


      private void CreateParentFilter()
      {
         ThreadHelper.JoinableTaskFactory.Run(async () =>
         {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Create the new filter under the selected parent filter
            ParentFilter = SelectedFilter.ProjectItems.AddFolder(ClassNameTextBox.Text, EnvDTE.Constants.vsProjectItemKindVirtualFolder);
         });
      }

      private void CreatePublicPrivateFilters()
      {
         ThreadHelper.JoinableTaskFactory.Run(async () =>
         {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            ProjectItem parentItem = ShouldCreateParentFolder ? ParentFilter : SelectedFilter;

            // Create the new filter under the selected parent filter
            PublicFilter = parentItem.ProjectItems.AddFolder("Public", EnvDTE.Constants.vsProjectItemKindVirtualFolder);
            if (ShouldCreateCppFile)
               PrivateFilter = parentItem.ProjectItems.AddFolder("Private", EnvDTE.Constants.vsProjectItemKindVirtualFolder);
         });
      }

      private void AddFilesToFilters()
      {
         ThreadHelper.JoinableTaskFactory.Run(async () =>
         {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (ShouldCreateSubfolders)
            {
               PublicFilter.ProjectItems.AddFromFile(HeaderFileFullPath);
               if (ShouldCreateCppFile)
                  PrivateFilter.ProjectItems.AddFromFile(CppFileFullPath);
            }
            else
            {
               SelectedFilter.ProjectItems.AddFromFile(HeaderFileFullPath);
               if (ShouldCreateCppFile)
                  SelectedFilter.ProjectItems.AddFromFile(CppFileFullPath);
            }
         });
      }

      private void GenerateProjectFiles()
      {
         ThreadHelper.JoinableTaskFactory.Run(async () =>
         {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            string solutionDir = Path.GetDirectoryName(VisualStudioServices.DTE.Solution.FullName);

            string[] files = Directory.GetFiles(solutionDir);

            bool executedScript = false;

            foreach (string file in files)
            {
               if (Path.GetFileName(file) == "GenerateProjectFiles.bat")
               {
                  ProcessStartInfo processInfo;
                  System.Diagnostics.Process process;
                  processInfo = new ProcessStartInfo(file);
                  processInfo.CreateNoWindow = false;
                  processInfo.UseShellExecute = true;
                  process = System.Diagnostics.Process.Start(processInfo);
                  process.WaitForExit();

                  executedScript = true;
                  break;
               }
            }

            if (!executedScript)
            {
               VsShellUtilities.ShowMessageBox(
                  VisualStudioServices.ServiceProvider,
                  "Failed to regenerate project files. Please do this manually.",
                  "UnrealWizard - Error",
                  OLEMSGICON.OLEMSGICON_CRITICAL,
                  OLEMSGBUTTON.OLEMSGBUTTON_OK,
                  OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
         });
      }

      private void RegenerateProjectFilesCheckbox_Click(object sender, RoutedEventArgs e)
      {
         ShouldRegenerateProjectFiles = RegenerateProjectFilesCheckbox.IsChecked.Value;
      }

      private void ClassNameTextBox_GotFocus(object sender, RoutedEventArgs e)
      {
         ClassNameHint.Visibility = Visibility.Hidden;
      }

      private void ClassNameTextBox_LostFocus(object sender, RoutedEventArgs e)
      {
         if (ClassNameTextBox.Text.Length == 0)
         {
            ClassNameHint.Visibility = Visibility.Visible;
         }
         else
         {
            ClassNameHint.Visibility = Visibility.Hidden;
         }
      }

      private void ActorRadioButton_Checked(object sender, RoutedEventArgs e)
      {
         SelectedParentClass = ParentClassEnum.Actor;
         UpdateLabels();
         UpdatePreview();
      }

      private void ComponentRadioButton_Checked(object sender, RoutedEventArgs e)
      {
         SelectedParentClass = ParentClassEnum.Component;
         UpdateLabels();
         UpdatePreview();
      }

      private void ObjectRadioButton_Checked(object sender, RoutedEventArgs e)
      {
         SelectedParentClass = ParentClassEnum.Object;
         UpdateLabels();
         UpdatePreview();
      }

      private void InterfaceRadioButton_Checked(object sender, RoutedEventArgs e)
      {
         SelectedParentClass = ParentClassEnum.Interface;
         UpdateLabels();
         UpdatePreview();
      }

      private void ModuleRadioButton_Checked(object sender, RoutedEventArgs e)
      {
         SelectedParentClass = ParentClassEnum.Module;
         UpdateLabels();
         UpdatePreview();
      }

      private void WorldSubsystemRadioButton_Checked(object sender, RoutedEventArgs e)
      {
         SelectedParentClass = ParentClassEnum.WorldSubsystem;
         UpdateLabels();
         UpdatePreview();
      }

      private void GameInstanceSubsystemRadioButton_Checked(object sender, RoutedEventArgs e)
      {
         SelectedParentClass = ParentClassEnum.GameInstanceSubsystem;
         UpdateLabels();
         UpdatePreview();
      }

      private void ApiNameTextBox_GotFocus(object sender, RoutedEventArgs e)
      {
         ApiNameHint.Visibility = Visibility.Hidden;
      }

      private void ApiNameTextBox_LostFocus(object sender, RoutedEventArgs e)
      {
         if (ApiNameTextBox.Text.Length == 0)
         {
            ApiNameHint.Visibility = Visibility.Visible;
         }
         else
         {
            ApiNameHint.Visibility = Visibility.Hidden;
         }
      }

      private void ApiNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
      {
         if (ApiNameTextBox.Text.Length == 0)
         {
            ApiNameHint.Visibility = Visibility.Visible;
         }
         else
         {
            ApiNameHint.Visibility = Visibility.Hidden;
         }
         UpdateLabels();
         UpdatePreview();
      }

      private void CreateEmptyFilesCheckbox_Click(object sender, RoutedEventArgs e)
      {
         ShouldCreateEmptyFiles = CreateEmptyFilesCheckbox.IsChecked.Value;
         UpdateLabels();
         UpdatePreview();
      }

      private void CreateParentFolderCheckbox_Click(object sender, RoutedEventArgs e)
      {
         ShouldCreateParentFolder = CreateParentFolderCheckbox.IsChecked.Value;
         UpdateLabels();
         UpdatePreview();
      }
   }
}
