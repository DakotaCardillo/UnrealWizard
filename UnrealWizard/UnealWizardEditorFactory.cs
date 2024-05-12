using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace UnrealWizard
{
   public class UnealWizardEditorFactory : IVsEditorFactory
   {
      private UnrealWizardPackage _package;
      private Microsoft.VisualStudio.OLE.Interop.IServiceProvider _vsServiceProvider;

      [Import]
      public IContentTypeRegistryService ContentTypeRegistry { get; set; }

      [Import]
      public ITextEditorFactoryService TextEditorFactory { get; set; }

      public UnealWizardEditorFactory(UnrealWizardPackage package)
      {
         _package = package;
      }

      public int CreateEditorInstance(uint grfCreateDoc, string pszMkDocument, string pszPhysicalView, IVsHierarchy pvHier, uint itemid, IntPtr punkDocDataExisting, out IntPtr ppunkDocView, out IntPtr ppunkDocData, out string pbstrEditorCaption, out Guid pguidCmdUI, out int pgrfCDW)
      {
         ThreadHelper.ThrowIfNotOnUIThread();

         ppunkDocView = IntPtr.Zero;
         ppunkDocData = IntPtr.Zero;
         pbstrEditorCaption = string.Empty;
         pguidCmdUI = VSConstants.GUID_TextEditorFactory;
         pgrfCDW = 0;
         int retVal = VSConstants.E_FAIL;

         if ((grfCreateDoc & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) != 0)
         {
            IVsTextLines textBuffer = null;

            if (punkDocDataExisting == IntPtr.Zero)
            {
               IComponentModel mef = _package.GetService<SComponentModel, IComponentModel>();
               mef.DefaultCompositionService.SatisfyImportsOnce(this);
               IVsEditorAdaptersFactoryService eafs = mef.GetService<IVsEditorAdaptersFactoryService>();

               textBuffer = eafs.CreateVsTextBufferAdapter(_vsServiceProvider, ContentTypeRegistry.GetContentType("C/C++")) as IVsTextLines;
               string fileText = System.IO.File.ReadAllText(pszMkDocument);
               textBuffer.InitializeContent(fileText, fileText.Length);

               string[] roles = new string[]
               {
                    PredefinedTextViewRoles.Analyzable,
                    PredefinedTextViewRoles.Editable,
                    PredefinedTextViewRoles.Interactive,
                    PredefinedTextViewRoles.Document,
                    PredefinedTextViewRoles.PrimaryDocument
               };
               IWpfTextView dataView = TextEditorFactory.CreateTextView(eafs.GetDataBuffer(textBuffer), TextEditorFactory.CreateTextViewRoleSet(roles));
               dataView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginName, true);
               dataView.Options.SetOptionValue(DefaultTextViewHostOptions.ShowCaretPositionOptionName, true);
               dataView.Options.SetOptionValue(DefaultTextViewHostOptions.ChangeTrackingName, true);
               dataView.Options.SetOptionValue(DefaultTextViewOptions.ViewProhibitUserInputName, false);

               IWpfTextViewHost wpfHost = TextEditorFactory.CreateTextViewHost(dataView, false);
               //MyCustomEditor editor = new MyCustomEditor(wpfHost);

               ppunkDocData = Marshal.GetIUnknownForObject(textBuffer);
               //ppunkDocView = Marshal.GetIUnknownForObject(editor);

               retVal = VSConstants.S_OK;
            }
            else
            {
               retVal = VSConstants.E_INVALIDARG;
            }
         }
         return (retVal);
      }

      public int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
      {
         _vsServiceProvider = psp;
         return (VSConstants.S_OK);
      }

      public int Close()
      {
         return (VSConstants.S_OK);
      }

      public int MapLogicalView(ref Guid rguidLogicalView, out string pbstrPhysicalView)
      {
         pbstrPhysicalView = null;
         return (VSConstants.LOGVIEWID_Primary == rguidLogicalView ? VSConstants.S_OK : VSConstants.E_NOTIMPL);
      }
   }
}
