using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using EJ2APIServices.Models;
using EJ2APIServices.Services;

namespace EJ2APIServices.Controllers
{
    /// <summary>
    /// Immigration Case Documents POC API.
    /// Implements the immigration-poc-flow.html mock end-to-end using the
    /// Syncfusion Document Editor:
    ///   1. List cases / templates
    ///   2. Generate a document from a template (loads the template DOCX as
    ///      SFDT and merges case data into the merge fields)
    ///   3. Save the edited document to the case file (SFDT -> DOCX, stored
    ///      under App_Data/ImmigrationCases/cases/{caseId}/{templateKey}/v{n}.docx)
    ///   4. List documents saved to a case
    ///   5. Export a saved document (DOCX download)
    /// Storage follows the ContentLibraryService pattern: files on disk,
    /// metadata in a JSON index (cases.json).
    /// </summary>
    [Route("api/[controller]")]
    [EnableCors("AllowAllOrigins")]
    public class ImmigrationController : Controller
    {
        private readonly ImmigrationCaseService _cases;
        private readonly IWebHostEnvironment _env;

        public ImmigrationController(IWebHostEnvironment env)
        {
            _env = env;
            _cases = new ImmigrationCaseService(env);
        }

        // ---------- 1. Cases & templates ----------
        [HttpGet]
        [Route("cases")]
        public IActionResult ListCases() => Ok(_cases.ListCases());

        [HttpGet]
        [Route("templates")]
        public IActionResult ListTemplates() => Ok(_cases.ListTemplates());

        // ---------- 1b. Persist workflow progress (step + stage) ----------
        // Called when leaving the workspace ("Back to cases"), so the home
        // page shows the real status and the case resumes where it left off.
        public class ProgressRequest
        {
            public int Step { get; set; }
        }

        [HttpPost]
        [Route("cases/{caseId}/progress")]
        public IActionResult UpdateProgress(string caseId, [FromBody] ProgressRequest body)
        {
            if (body == null || body.Step < 0 || body.Step > 4)
                return BadRequest("Step must be between 0 and 4.");
            var caseData = _cases.UpdateProgress(caseId, body.Step);
            if (caseData == null) return NotFound($"Case '{caseId}' not found.");
            return Ok(caseData);
        }

        // ---------- 2. Generate a document from a template ----------
        // Loads the template DOCX (an existing ContentLibrary document,
        // per "use any existing document for now"), runs a mail merge with
        // the case data, and returns SFDT for the Document Editor.
        [HttpPost]
        [Route("cases/{caseId}/documents/{templateKey}/generate")]
        public IActionResult Generate(string caseId, string templateKey)
        {
            var store = _cases.Load();
            var caseData = store.Cases.FirstOrDefault(c =>
                string.Equals(c.Id, caseId, StringComparison.OrdinalIgnoreCase));
            if (caseData == null) return NotFound($"Case '{caseId}' not found.");

            var template = store.Templates.FirstOrDefault(t =>
                string.Equals(t.Key, templateKey, StringComparison.OrdinalIgnoreCase));
            if (template == null) return NotFound($"Template '{templateKey}' not found.");

            // Templates live under App_Data/ImmigrationCases/templates/
            // and are read-only for the workflow — the merged result is
            // never written back to the template file.
            string templatePath = _cases.GetTemplateFile(templateKey);
            if (!System.IO.File.Exists(templatePath))
                return NotFound($"Template document '{templateKey}' not found on disk.");

            using var templateStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read);
            var document = new Syncfusion.DocIO.DLS.WordDocument(templateStream,
                Syncfusion.DocIO.FormatType.Docx);

            // Merge the case data into the template merge fields,
            // replacing the fields with the case values (same pattern as
            // DocumentEditorController.MailMerge).
            document.MailMerge.RemoveEmptyGroup = true;
            document.MailMerge.RemoveEmptyParagraphs = true;
            document.MailMerge.ClearFields = true;
            // Canonical DocIO mail merge: a DataTable whose column names
            // match the merge field names (Client, Form, ...) and one row
            // holding the case values.
            var table = new System.Data.DataTable();
            table.Columns.Add("CaseId");
            table.Columns.Add("Client");
            table.Columns.Add("CaseType");
            table.Columns.Add("Form");
            table.Columns.Add("Receipt");
            table.Columns.Add("Employer");
            table.Columns.Add("Attorney");
            table.Columns.Add("Paralegal");
            table.Columns.Add("Date");
            table.Rows.Add(
                caseData.Id, caseData.Client, caseData.Type, caseData.Form,
                caseData.Receipt, caseData.Employer, caseData.Attorney,
                caseData.Paralegal, DateTime.Now.ToString("MMMM d, yyyy"));
            document.MailMerge.Execute(table);

            using var merged = new MemoryStream();
            document.Save(merged, Syncfusion.DocIO.FormatType.Docx);
            document.Close();
            merged.Position = 0;

            // Convert the merged DOCX to SFDT for the Document Editor.
            var editorDoc = Syncfusion.EJ2.DocumentEditor.WordDocument.Load(
                merged, Syncfusion.EJ2.DocumentEditor.FormatType.Docx);
            string sfdt = JsonConvert.SerializeObject(editorDoc);
            editorDoc.Dispose();

            return Ok(new { documentData = sfdt });
        }

        // ---------- 2b. Open the raw template (no mail merge) ----------
        // Returns the template document as SFDT WITHOUT merging the case
        // data, so the user first sees the template with its merge fields
        // (e.g. «Client», «Form») and can then apply the mail merge via
        // the "Load with data" button (the generate endpoint above).
        [HttpPost]
        [Route("cases/{caseId}/documents/{templateKey}/open")]
        public IActionResult OpenTemplate(string caseId, string templateKey)
        {
            var store = _cases.Load();
            var template = store.Templates.FirstOrDefault(t =>
                string.Equals(t.Key, templateKey, StringComparison.OrdinalIgnoreCase));
            if (template == null) return NotFound($"Template '{templateKey}' not found.");

            // Templates live under App_Data/ImmigrationCases/templates/
            // and are read-only for the workflow — the merged result is
            // never written back to the template file.
            string templatePath = _cases.GetTemplateFile(templateKey);
            if (!System.IO.File.Exists(templatePath))
                return NotFound($"Template document '{templateKey}' not found on disk.");

            using var templateStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read);
            var editorDoc = Syncfusion.EJ2.DocumentEditor.WordDocument.Load(
                templateStream, Syncfusion.EJ2.DocumentEditor.FormatType.Docx);
            string sfdt = JsonConvert.SerializeObject(editorDoc);
            editorDoc.Dispose();

            return Ok(new { documentData = sfdt });
        }

        // ---------- 2c. Load the latest saved version (resume) ----------
        // Returns the newest saved DOCX of the case document as SFDT, so
        // the workspace can re-open exactly what the user last saved when
        // resuming a case mid-workflow.
        [HttpGet]
        [Route("cases/{caseId}/documents/{templateKey}/load")]
        public IActionResult LoadLatest(string caseId, string templateKey)
        {
            var doc = _cases.GetDocument(caseId, templateKey);
            if (doc == null) return NotFound($"No saved document for '{templateKey}' in case '{caseId}'.");

            string path = System.IO.Path.Combine(_cases.GetDocumentFolder(caseId, doc.TemplateKey), doc.CurrentFile);
            if (!System.IO.File.Exists(path)) return NotFound();

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var editorDoc = Syncfusion.EJ2.DocumentEditor.WordDocument.Load(
                stream, Syncfusion.EJ2.DocumentEditor.FormatType.Docx);
            string sfdt = JsonConvert.SerializeObject(editorDoc);
            editorDoc.Dispose();

            return Ok(new { documentData = sfdt, title = doc.Title });
        }

        // ---------- 3. Save to case file (SFDT -> DOCX) ----------
        public class SaveRequest
        {
            public string DocumentData { get; set; }
            public string Title { get; set; }
            public string TemplateKey { get; set; }
            public string ModifiedUser { get; set; }
        }

        [HttpPost]
        [Route("cases/{caseId}/documents/{templateKey}/save")]
        public IActionResult Save(string caseId, string templateKey, [FromBody] SaveRequest body)
        {
            if (body == null || string.IsNullOrEmpty(body.DocumentData))
                return BadRequest("DocumentData (SFDT JSON) is required.");

            var store = _cases.Load();
            var caseData = store.Cases.FirstOrDefault(c =>
                string.Equals(c.Id, caseId, StringComparison.OrdinalIgnoreCase));
            if (caseData == null) return NotFound($"Case '{caseId}' not found.");

            Syncfusion.DocIO.DLS.WordDocument document;
            try
            {
                document = Syncfusion.EJ2.DocumentEditor.WordDocument.Save(body.DocumentData);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to parse SFDT: " + ex.Message);
            }

            using var docxStream = new MemoryStream();
            try
            {
                document.Save(docxStream, Syncfusion.DocIO.FormatType.Docx);
            }
            catch (Exception ex)
            {
                document.Close();
                return BadRequest("Failed to save DOCX: " + ex.Message);
            }
            document.Close();
            docxStream.Position = 0;

            var saved = _cases.SaveDocument(
                caseId, templateKey,
                string.IsNullOrWhiteSpace(body.Title) ? templateKey : body.Title,
                docxStream,
                string.IsNullOrWhiteSpace(body.ModifiedUser) ? "Paralegal" : body.ModifiedUser);

            return Ok(saved);
        }

        // ---------- 4. Documents saved to a case ----------
        [HttpGet]
        [Route("cases/{caseId}/documents")]
        public IActionResult ListDocuments(string caseId)
        {
            var caseData = _cases.ListCases().FirstOrDefault(c =>
                string.Equals(c.Id, caseId, StringComparison.OrdinalIgnoreCase));
            if (caseData == null) return NotFound();
            return Ok(_cases.ListDocuments(caseId));
        }

        // ---------- 5. Export a saved document as DOCX ----------
        [HttpGet]
        [Route("cases/{caseId}/documents/{templateKey}/export")]
        public IActionResult Export(string caseId, string templateKey)
        {
            var doc = _cases.GetDocument(caseId, templateKey);
            if (doc == null) return NotFound();

            string path = System.IO.Path.Combine(_cases.GetDocumentFolder(caseId, doc.TemplateKey), doc.CurrentFile);
            if (!System.IO.File.Exists(path)) return NotFound();

            return PhysicalFile(path,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                SanitizeFileName(doc.Title) + ".docx");
        }

        // ---------- 5b. Export a saved document as PDF ----------
        // Converts the latest saved DOCX to PDF with DocIORenderer.
        [HttpGet]
        [Route("cases/{caseId}/documents/{templateKey}/export-pdf")]
        public IActionResult ExportPdf(string caseId, string templateKey)
        {
            var doc = _cases.GetDocument(caseId, templateKey);
            if (doc == null) return NotFound();

            string path = System.IO.Path.Combine(_cases.GetDocumentFolder(caseId, doc.TemplateKey), doc.CurrentFile);
            if (!System.IO.File.Exists(path)) return NotFound();

            using var docxStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var document = new Syncfusion.DocIO.DLS.WordDocument(docxStream,
                Syncfusion.DocIO.FormatType.Docx);
            using var renderer = new Syncfusion.DocIORenderer.DocIORenderer();
            // ConvertToPDF returns a PdfDocument that must stay alive while
            // the response writes it; do not dispose the stream until the
            // FileResult has streamed it (hence no `using` on pdfStream).
            var pdfDocument = renderer.ConvertToPDF(document);
            var pdfStream = new MemoryStream();
            pdfDocument.Save(pdfStream);
            pdfStream.Position = 0;

            return File(pdfStream, "application/pdf", SanitizeFileName(doc.Title) + ".pdf");
        }

        // ---------- 5c. Print (server-rendered inline PDF) ----------
        // The Document Editor's built-in print() renders the CURRENT editor
        // content; this endpoint prints the LAST SAVED version server-side
        // via DocIORenderer, returning the PDF inline so the browser's PDF
        // viewer opens the print dialog directly.
        [HttpGet]
        [Route("cases/{caseId}/documents/{templateKey}/print")]
        public IActionResult Print(string caseId, string templateKey)
        {
            var doc = _cases.GetDocument(caseId, templateKey);
            if (doc == null) return NotFound();

            string path = System.IO.Path.Combine(_cases.GetDocumentFolder(caseId, doc.TemplateKey), doc.CurrentFile);
            if (!System.IO.File.Exists(path)) return NotFound();

            using var docxStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var document = new Syncfusion.DocIO.DLS.WordDocument(docxStream,
                Syncfusion.DocIO.FormatType.Docx);
            using var renderer = new Syncfusion.DocIORenderer.DocIORenderer();
            var pdfDocument = renderer.ConvertToPDF(document);
            var pdfStream = new MemoryStream();
            pdfDocument.Save(pdfStream);
            pdfStream.Position = 0;

            Response.Headers.Add("Content-Disposition",
                $"inline; filename=\"{SanitizeFileName(doc.Title)}.pdf\"");
            return File(pdfStream, "application/pdf");
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Document";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '-');
            return name;
        }
    }
}
