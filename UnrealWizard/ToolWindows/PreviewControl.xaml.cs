using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Editor;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using System.Diagnostics;

namespace UnrealWizard
{
   /// <summary>
   /// Interaction logic for PreviewControl.xaml
   /// </summary>
   public partial class PreviewControl : UserControl
   {
      private IWpfTextView textView;

      private ITextEditorFactoryService _textEditorFactoryService;
      private ITextBuffer _textBuffer;

      IVsInvisibleEditorManager _invisibleEditorManager;
      IVsEditorAdaptersFactoryService _editorAdapter;
      IComponentModel _componentModel;


      [Import]
      private IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

      [Import]
      private SVsServiceProvider ServiceProvider { get; set; }

      public PreviewControl()
      {
         InitializeComponent();
         ThreadHelper.ThrowIfNotOnUIThread();


         _componentModel = (IComponentModel)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SComponentModel));
         _invisibleEditorManager = (IVsInvisibleEditorManager)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsInvisibleEditorManager));
         _editorAdapter = _componentModel.GetService<IVsEditorAdaptersFactoryService>();
      }

      public void Initialize(ITextEditorFactoryService textEditorFactoryService, ITextBuffer textBuffer)
      {
         _textEditorFactoryService = textEditorFactoryService;
         _textBuffer = textBuffer;

         DisplayTextView();
      }

      public IVsTextView CreateTextView(ITextBuffer textBuffer)
      {
         // Ensure there's an IVsTextBuffer associated with the ITextBuffer
         IVsTextBuffer vsTextBuffer = _editorAdapter.GetBufferAdapter(textBuffer) ?? _editorAdapter.CreateVsTextBufferAdapter(VisualStudioServices.OLEServiceProvider, textBuffer.ContentType);
         IVsTextLines vsTextLines = vsTextBuffer as IVsTextLines;

         if (vsTextLines == null)
         {
            throw new InvalidOperationException("The IVsTextBuffer does not support IVsTextLines.");
         }

         //// Create the code window and set the buffer
         //IVsCodeWindow codeWindow = _editorAdapter.CreateVsCodeWindowAdapter(VisualStudioServices.OLEServiceProvider);
         //codeWindow.SetBuffer(vsTextLines);

         //// Get the primary view from the code window
         //IVsTextView vsTextView;
         //codeWindow.GetPrimaryView(out vsTextView);

         IVsTextView vsTextView = _editorAdapter.CreateVsTextViewAdapter(VisualStudioServices.OLEServiceProvider);
         if (vsTextView == null)
         {
            Debug.WriteLine("Failed to create IVsTextView.");
            return null;
         }


         return vsTextView;
      }

      public void DisplayTextView()
      {
         textView = _textEditorFactoryService.CreateTextView(_textBuffer);
         textView.Caret.IsHidden = true;
         textView.Selection.Clear();
         textView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, true); // To show line numbers
         textView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, false);
         textView.Options.SetOptionValue(DefaultTextViewHostOptions.SelectionMarginId, false);
         textView.Options.SetOptionValue(DefaultTextViewHostOptions.SuggestionMarginId, false);
         textView.Options.SetOptionValue(DefaultTextViewHostOptions.OutliningMarginId, false);
         textView.Options.SetOptionValue(DefaultTextViewHostOptions.EnableFileHealthIndicatorOptionId, false);
         textView.Options.SetOptionValue(DefaultTextViewHostOptions.InsertModeMarginOptionId, false);
         textView.Options.SetOptionValue(DefaultTextViewHostOptions.IndentationCharacterMarginOptionId, false);
         //textView.Options.SetOptionValue(DefaultTextViewO, true);

         // Assumes _textEditorFactoryService and _textBuffer have been set
         var textViewHost = _textEditorFactoryService.CreateTextViewHost(textView, false);
         HideMargin(textViewHost, "ZoomControl");
         HideMargin(textViewHost, "LineNumberMargin");
         HideMargin(textViewHost, "ChangeTrackingMargin");

         Content = textViewHost.HostControl;
         //ThreadHelper.JoinableTaskFactory.Run(async () =>
         //{
         //   await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

         //   //Content = UEClassWindowControl.TextViewHost.HostControl;

         //   IVsTextView vsTextView = CreateTextView(_textBuffer);
         //   IWpfTextViewHost textViewHost = _editorAdapter.GetWpfTextViewHost(vsTextView);
         //   if (textViewHost != null)
         //   {
         //      Content = textViewHost.HostControl;
         //   }
         //});
      }

      private void HideMargin(IWpfTextViewHost textViewHost, string marginName)
      {
         var margin = textViewHost.GetTextViewMargin(marginName);
         if (margin != null)
            margin.VisualElement.Visibility = Visibility.Collapsed;
      }

      public void UpdateText(string newText)
      {
         // Create an edit on the text buffer
         using (var edit = _textBuffer.CreateEdit())
         {
            // Replace the entire content of the buffer with new text
            edit.Replace(new Microsoft.VisualStudio.Text.Span(0, _textBuffer.CurrentSnapshot.Length), newText);
            edit.Apply();
         }
      }
   }
}
