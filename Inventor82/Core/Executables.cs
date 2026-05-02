using Inventor;
using System;
using System.Collections.Generic;
#pragma warning disable CA1416 // Validate platform compatibility

namespace Inventor82
{
    public static class Executables
    {
        /// <summary>
        /// Represents user-configurable options for file export operations.
        /// </summary>
        public class ExportOptions
        {
            /// <summary>
            /// Gets or sets a value indicating whether the exported file should be opened immediately after saving.
            /// </summary>
            /// <value>
            /// <c>true</c> to open the file after export; otherwise, <c>false</c>. The default value is <c>false</c>.
            /// </value>
            public bool OpenAfterSaving { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the exported file's location should be revealed in Windows Explorer after saving.
            /// </summary>
            /// <value>
            /// <c>true</c> to open Windows Explorer and select the file after export; otherwise, <c>false</c>. The default value is <c>false</c>.
            /// </value>
            public bool RevealInExplorer { get; set; }
        }

        /// <summary>
        /// Displays a modal dialog box that allows the user to configure export options before proceeding with the export operation.
        /// </summary>
        /// <returns>
        /// An <see cref="ExportOptions"/> instance containing the user's selections if the user clicks OK;
        /// otherwise, <c>null</c> if the user cancels or closes the dialog.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The dialog presents two checkboxes:
        /// <list type="bullet">
        ///   <item><description><b>Open after saving</b> - Determines whether the exported file is opened after the export completes.</description></item>
        ///   <item><description><b>Reveal in Explorer</b> - Determines whether Windows Explorer is opened to the file's location after export.</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// The dialog is rendered as a fixed-size, centered modal form with OK and Cancel buttons.
        /// If the user cancels the operation, the method returns <c>null</c>, allowing the caller
        /// to detect cancellation and abort the export process.
        /// </para>
        /// </remarks>
        private static ExportOptions ShowExportOptionsDialog()
        {
            var result = new ExportOptions();


            using (var form = new System.Windows.Forms.Form())
            {
                form.Text = Properties.Resources.ExportOptions_Name;
                form.Width = 320;
                form.Height = 170;
                form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                form.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var openCheck = new System.Windows.Forms.CheckBox
                {
                    Text = Properties.Resources.OpenAfterSaving,
                    Left = 20,
                    Top = 20,
                    Width = 250
                };

                var revealCheck = new System.Windows.Forms.CheckBox
                {
                    Text = Properties.Resources.ShowInExplorer_Name,
                    Left = 20,
                    Top = 50,
                    Width = 250
                };

                var okButton = new System.Windows.Forms.Button
                {
                    Text = Properties.Resources.Ok,
                    Left = 130,
                    Top = 85,
                    Width = 75,
                    DialogResult = System.Windows.Forms.DialogResult.OK
                };

                var cancelButton = new System.Windows.Forms.Button
                {
                    Text = Properties.Resources.Cancel,
                    Left = 215,
                    Top = 85,
                    Width = 75,
                    DialogResult = System.Windows.Forms.DialogResult.Cancel
                };

                form.Controls.Add(openCheck);
                form.Controls.Add(revealCheck);
                form.Controls.Add(okButton);
                form.Controls.Add(cancelButton);

                form.AcceptButton = okButton;
                form.CancelButton = cancelButton;

                if (form.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return null;

                result.OpenAfterSaving = openCheck.Checked;
                result.RevealInExplorer = revealCheck.Checked;
            }

            return result;
        }

        /// <summary>
        /// Exports the currently active document in Autodesk Inventor to an STL file format.
        /// </summary>
        /// <remarks>
        /// This method performs the following operations:
        /// <list type="number">
        ///   <item><description>Retrieves the active document from the Inventor application.</description></item>
        ///   <item><description>Prompts the user to specify a save location and filename via a file dialog.</description></item>
        ///   <item><description>Locates the STL translator add-in within the application.</description></item>
        ///   <item><description>Executes the translation and saves the document as an STL file.</description></item>
        /// </list>
        /// The default filename is derived from the active document's display name, and the default
        /// save location is set to the document's current directory if it has been previously saved.
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown when there is no active document in the Inventor application.
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when the STL translator add-in cannot be found among the available application add-ins.
        /// </exception>
        /// <example>
        /// <code>
        /// try
        /// {
        ///     ExportActiveDocumentAsStl();
        ///     Console.WriteLine("STL export completed successfully.");
        /// }
        /// catch (Exception ex)
        /// {
        ///     Console.WriteLine($"Export failed: {ex.Message}");
        /// }
        /// </code>
        /// </example>
        public static void ExportActiveDocumentAsStl()
        {
            Document doc = Standalone.m_inventorApplication.ActiveDocument;
            if (doc == null)
                throw new Exception("No active document.");

            // 1) Ask user where to save
            FileDialog dlg;
            Standalone.m_inventorApplication.CreateFileDialog(out dlg);

            dlg.DialogTitle = "Export STL";
            dlg.Filter = "STL Files (*.stl)|*.stl";
            dlg.FilterIndex = 1;

            // Optional default filename
            string baseName = System.IO.Path.GetFileNameWithoutExtension(doc.DisplayName);
            dlg.FileName = baseName + ".stl";

            // Optional default folder
            try
            {
                string srcPath = doc.FullFileName;
                if (!string.IsNullOrWhiteSpace(srcPath))
                    dlg.InitialDirectory = System.IO.Path.GetDirectoryName(srcPath);
            }
            catch
            {
                // ignore if document has never been saved
            }

            dlg.CancelError = true;

            try
            {
                dlg.ShowSave();
            }
            catch
            {
                // user cancelled
                return;
            }

            string outputFile = dlg.FileName;
            if (string.IsNullOrWhiteSpace(outputFile))
                return;

            // 2) Find STL translator add-in
            TranslatorAddIn stlAddIn = null;

            foreach (ApplicationAddIn addIn in Standalone.m_inventorApplication.ApplicationAddIns)
            {
                if (addIn.DisplayName != null &&
                    addIn.DisplayName.IndexOf("STL", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    stlAddIn = addIn as TranslatorAddIn;
                    if (stlAddIn != null)
                        break;
                }
            }

            if (stlAddIn == null)
                throw new Exception("STL translator add-in not found.");

            // 3) Export using translator
            TranslationContext context = Standalone.m_inventorApplication.TransientObjects.CreateTranslationContext();
            context.Type = IOMechanismEnum.kFileBrowseIOMechanism;

            NameValueMap options = Standalone.m_inventorApplication.TransientObjects.CreateNameValueMap();

            DataMedium data = Standalone.m_inventorApplication.TransientObjects.CreateDataMedium();
            data.FileName = outputFile;

            //Show additional options
            ExportOptions exportOptions = ShowExportOptionsDialog();
            if (exportOptions == null)
                return;

            //Save STL file
            stlAddIn.SaveCopyAs(doc, context, options, data);

            //Next steps with additional options
            if (exportOptions.RevealInExplorer)
            {
                System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + outputFile + "\"");
            }

            if (exportOptions.OpenAfterSaving)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = outputFile,
                    UseShellExecute = true
                });
            }
        }

        /// <summary>
        /// Creates a new part document using the default metric template and optionally assigns a display name.
        /// </summary>
        /// <param name="name">
        /// Optional display name for the new part document. If empty or whitespace, the default name is retained.
        /// </param>
        /// <returns>
        /// The newly created <see cref="PartDocument"/> instance.
        /// </returns>
        /// <remarks>
        /// The template used is a metric system part document with the default drafting standard.
        /// The document is created as visible immediately.
        /// </remarks>
        /// <example>
        /// <code>
        /// PartDocument myPart = AddNewIpt("MyCustomPart");
        /// </code>
        /// </example>
        public static PartDocument AddNewIpt(string name = "")
        {
            string templatePath = Standalone.m_inventorApplication.FileManager.GetTemplateFile(
                DocumentTypeEnum.kPartDocumentObject,
                SystemOfMeasureEnum.kMetricSystemOfMeasure,
                DraftingStandardEnum.kDefault_DraftingStandard
            );
            PartDocument partDoc = (PartDocument)Standalone.m_inventorApplication.Documents.Add(DocumentTypeEnum.kPartDocumentObject, templatePath, true );
            if(!string.IsNullOrWhiteSpace(name))
                partDoc.DisplayName = name;
            return partDoc;
        }

        /// <summary>
        /// Specifies the standard work planes available in an Inventor part document for sketch creation.
        /// </summary>
        public enum SketchPlanes
        {
            /// <summary>
            /// The XY plane (WorkPlane index 3).
            /// </summary>
            XY = 3,
            /// <summary>
            /// The YZ plane (WorkPlane index 2).
            /// </summary>
            YZ = 2,
            /// <summary>
            /// The XZ plane (WorkPlane index 1).
            /// </summary>
            XZ = 1
        }

        /// <summary>
        /// Creates a new part document with a planar sketch on the specified work plane, ready for editing.
        /// </summary>
        /// <param name="plane">
        /// The standard work plane on which to create the sketch.
        /// </param>
        /// <param name="iptName">
        /// Optional display name for the part document. If empty or whitespace, the default name is retained.
        /// </param>
        /// <param name="sketchName">
        /// Optional name for the sketch. If empty or whitespace, the default name is retained.
        /// </param>
        /// <remarks>
        /// This method calls <see cref="AddNewIpt"/> to create the part document, then creates a planar sketch
        /// on the specified work plane and activates it for editing.
        /// </remarks>
        /// <example>
        /// <code>
        /// AddNewIptWithSketch(SketchPlanes.XY, "Bracket", "BaseProfile");
        /// </code>
        /// </example>
        public static void AddNewIptWithSketch(SketchPlanes plane, string iptName = "", string sketchName = "")
        {
            PartDocument partDoc = AddNewIpt(iptName);
            PlanarSketch sketch = partDoc.ComponentDefinition.Sketches.Add(partDoc.ComponentDefinition.WorkPlanes[(int)plane]);
            if(!string.IsNullOrWhiteSpace(sketchName))
                sketch.Name = sketchName;
            sketch.Edit();
        }

        /// <summary>
        /// Retrieves a list of all currently visible document windows in the Inventor application.
        /// </summary>
        /// <returns>
        /// A <see cref="List{Document}"/> containing all visible documents currently open in the application.
        /// </returns>
        /// <remarks>
        /// This only returns documents that are currently displayed in visible windows.
        /// Documents that are open but hidden (not visible in any window) are not included.
        /// For all open documents regardless of visibility, use <see cref="GetAllOpenedDocuments"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// List&lt;Document&gt; visibleDocs = GetOpenedWindows();
        /// foreach (Document doc in visibleDocs)
        /// {
        ///     Console.WriteLine(doc.DisplayName);
        /// }
        /// </code>
        /// </example>
        public static List<Document> GetOpenedWindows()
        {
            var result = new List<Document>();
            foreach (Document doc in Standalone.m_inventorApplication.Documents.VisibleDocuments)
            {
                result.Add(doc);
            }
            return result;
        }

        /// <summary>
        /// Retrieves a list of all open documents in the Inventor application, including hidden ones inside opened assemblies.
        /// </summary>
        /// <returns>
        /// A <see cref="List{Document}"/> containing every document currently open in the application.
        /// </returns>
        /// <remarks>
        /// This method returns all open documents regardless of whether they are displayed in a visible window.
        /// Documents open in the background (e.g., referenced assemblies or parts) are included.
        /// For only visible documents, use <see cref="GetOpenedWindows"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// List&lt;Document&gt; allDocs = GetAllOpenedDocuments();
        /// Console.WriteLine($"Total open documents: {allDocs.Count}");
        /// </code>
        /// </example>
        public static List<Document> GetAllOpenedDocuments()
        {
            var result = new List<Document>();
            foreach (Document doc in Standalone.m_inventorApplication.Documents)
            {
                result.Add(doc);
            }
            return result;
        }

        /// <summary>
        /// Activates an open document by its display name, bringing it to the foreground.
        /// </summary>
        /// <param name="name">
        /// The display name of the document to activate. Comparison is case-insensitive.
        /// </param>
        /// <returns>
        /// <c>true</c> if a document with the specified name was found and activated; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The search is performed across all open documents (not just visible ones) using
        /// <see cref="Documents"/> collection. The comparison uses <see cref="StringComparison.OrdinalIgnoreCase"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// bool activated = ActivateDocumentByName("MyDrawing.idw");
        /// if (!activated)
        ///     Console.WriteLine("Document not found.");
        /// </code>
        /// </example>
        public static bool ActivateDocumentByName(string name)
        {
            foreach (Document doc in Standalone.m_inventorApplication.Documents)
            {
                if (doc.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    doc.Activate();
                    return true;
                }
            }
            //Document not found
            return false;
        }

    }//
}
