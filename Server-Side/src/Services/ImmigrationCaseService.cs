using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EJ2APIServices.Models;
using Newtonsoft.Json;

namespace EJ2APIServices.Services
{
    /// <summary>
    /// File-system backed storage for the Immigration Case Documents POC.
    /// Mirrors ContentLibraryService:
    ///   - JSON index:  App_Data/ImmigrationCases/cases.json
    ///   - Case files: App_Data/ImmigrationCases/cases/{caseId}/{docKey}/v{n}.docx
    /// All public methods are thread-safe (single lock guards the index).
    /// </summary>
    public class ImmigrationCaseService
    {
        private static readonly object _lock = new object();

        private readonly string _rootPath;
        private readonly string _indexPath;

        public ImmigrationCaseService(Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _rootPath = Path.Combine(env.ContentRootPath, "App_Data", "ImmigrationCases");
            _indexPath = Path.Combine(_rootPath, "cases.json");
            Directory.CreateDirectory(RootPath);
            if (!File.Exists(_indexPath)) Save(new ImmigrationCaseStore());
            EnsureTemplatesSeeded();
        }

        /// <summary>
        /// Templates live under App_Data/ImmigrationCases/templates/{key}/v1.docx.
        /// They are seeded once with DocIO merge fields and are READ-ONLY for
        /// the workflow — client documents are always saved as separate files
        /// under cases/{caseId}/{templateKey}/, never over the template.
        /// </summary>
        public string GetTemplateFile(string templateKey)
            => Path.Combine(_rootPath, "templates", templateKey, "v1.docx");

        /// <summary>
        /// Seeds the immigration templates (cover letter + retainer) with
        /// DocIO mail-merge fields (Client, Form, Receipt, ...). Idempotent —
        /// no-op when the files already exist.
        /// </summary>
        private void EnsureTemplatesSeeded()
        {
            SeedTemplate(GetTemplateFile("cover"), BuildCoverLetter);
            SeedTemplate(GetTemplateFile("retainer"), BuildRetainer);
        }

        private static void SeedTemplate(string path, Action<string> build)
        {
            if (File.Exists(path)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            try { build(path); } catch { /* leave absent; POC still runs */ }
        }

        private static void AppendField(Syncfusion.DocIO.DLS.IWParagraph p, string name)
            => p.AppendField(name, Syncfusion.DocIO.FieldType.FieldMergeField);

        private static void BuildCoverLetter(string path)
        {
            using var doc = new Syncfusion.DocIO.DLS.WordDocument();
            var sec = doc.AddSection();
            sec.PageSetup.Margins.All = 72f;

            var head = sec.AddParagraph();
            head.AppendText("Brooks & Whitfield Immigration Law").CharacterFormat.Bold = true;
            var date = sec.AddParagraph();
            AppendField(date, "Date");

            sec.AddParagraph();
            var uscis = sec.AddParagraph();
            uscis.AppendText("U.S. Citizenship and Immigration Services\rCalifornia Service Center");

            var re = sec.AddParagraph();
            re.AppendText("Re: ").CharacterFormat.Bold = true;
            AppendField(re, "Form");
            re.AppendText(" for ").CharacterFormat.Bold = true;
            AppendField(re, "Client");
            var rcpt = sec.AddParagraph();
            rcpt.AppendText("Receipt number: ");
            AppendField(rcpt, "Receipt");

            sec.AddParagraph();
            sec.AddParagraph().AppendText("Dear Officer,");

            var body = sec.AddParagraph();
            body.AppendText("Please find enclosed the petition and supporting evidence for ");
            AppendField(body, "Client");
            body.AppendText(" in the ");
            AppendField(body, "CaseType");
            body.AppendText(" category. The package includes the employer support letter from ");
            AppendField(body, "Employer");
            body.AppendText(" and copies of the client\u2019s identity documents.");

            sec.AddParagraph();
            var sign = sec.AddParagraph();
            sign.AppendText("Sincerely,\r");
            AppendField(sign, "Attorney");
            sign.AppendText("\rAttorney at law");

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            doc.Save(fs, Syncfusion.DocIO.FormatType.Docx);
        }

        private static void BuildRetainer(string path)
        {
            using var doc = new Syncfusion.DocIO.DLS.WordDocument();
            var sec = doc.AddSection();
            sec.PageSetup.Margins.All = 72f;

            var title = sec.AddParagraph();
            title.AppendText("Retainer agreement").CharacterFormat.Bold = true;

            var intro = sec.AddParagraph();
            intro.AppendText("This agreement is made on ");
            AppendField(intro, "Date");
            intro.AppendText(" between Brooks & Whitfield Immigration Law (\u201Cthe firm\u201D) and ");
            AppendField(intro, "Client");
            intro.AppendText(" (\u201Cthe client\u201D) for representation in the client\u2019s ");
            AppendField(intro, "CaseType");
            intro.AppendText(" matter.");

            var scope = sec.AddParagraph();
            scope.AppendText("Scope of services. ").CharacterFormat.Bold = true;
            scope.AppendText("The firm will prepare and file ");
            AppendField(scope, "Form");
            scope.AppendText(" and supporting documents and respond to routine government correspondence.");

            var fees = sec.AddParagraph();
            fees.AppendText("Fees. ").CharacterFormat.Bold = true;
            fees.AppendText("The client agrees to pay a flat legal fee of $3,500, payable in two equal installments. Government filing fees are paid separately.");

            var conf = sec.AddParagraph();
            conf.AppendText("Confidentiality. ").CharacterFormat.Bold = true;
            conf.AppendText("All information shared by the client is confidential.");

            sec.AddParagraph();
            var sign = sec.AddParagraph();
            sign.AppendText("Client: ");
            AppendField(sign, "Client");
            sign.AppendText("    Attorney: ");
            AppendField(sign, "Attorney");

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            doc.Save(fs, Syncfusion.DocIO.FormatType.Docx);
        }

        public string RootPath => _rootPath;

        public ImmigrationCaseStore Load()
        {
            lock (_lock)
            {
                if (!File.Exists(_indexPath)) return new ImmigrationCaseStore();
                var json = File.ReadAllText(_indexPath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json)) return new ImmigrationCaseStore();
                return JsonConvert.DeserializeObject<ImmigrationCaseStore>(json)
                       ?? new ImmigrationCaseStore();
            }
        }

        public void Save(ImmigrationCaseStore store)
        {
            lock (_lock)
            {
                var json = JsonConvert.SerializeObject(store, Formatting.Indented);
                File.WriteAllText(_indexPath, json, Encoding.UTF8);
            }
        }

        public List<ImmigrationCase> ListCases() => Load().Cases;

        public List<ImmigrationTemplate> ListTemplates() => Load().Templates;

        /// <summary>
        /// Persists the case's current workflow step (0-4) and derives the
        /// user-facing Stage label from it, so the home page reflects the
        /// step the user actually left the case at — regardless of whether
        /// that is forwards or backwards from the previous visit.
        /// </summary>
        public ImmigrationCase UpdateProgress(string caseId, int step)
        {
            var store = Load();
            var caseData = store.Cases.FirstOrDefault(c =>
                string.Equals(c.Id, caseId, StringComparison.OrdinalIgnoreCase));
            if (caseData == null) return null;

            caseData.ProgressStep = step;
            caseData.Stage = step switch
            {
                0 => "Drafting",
                1 => "Paralegal edits",
                2 => "Attorney review",
                3 => "Saved to case file",
                _ => "Ready for filing",
            };
            Save(store);
            return caseData;
        }

        // Folder name is the template key alone — the case id is already
        // the parent folder — so no ":" ever leaks into the path.
        public string GetDocumentFolder(string caseId, string templateKey)
            => Path.Combine(_rootPath, "cases", caseId, templateKey);

        public string GetVersionFile(string caseId, string templateKey, int version)
            => Path.Combine(GetDocumentFolder(caseId, templateKey), $"v{version}.docx");

        public ImmigrationCaseDocument GetDocument(string caseId, string templateKey)
            => Load().Documents.FirstOrDefault(d =>
                string.Equals(d.CaseId(), caseId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(d.TemplateKey, templateKey, StringComparison.OrdinalIgnoreCase));

        public List<ImmigrationCaseDocument> ListDocuments(string caseId)
            => Load().Documents.Where(d =>
                    string.Equals(d.CaseId(), caseId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => d.ModifiedDate)
                .ToList();

        /// <summary>
        /// Saves the case document, overwriting the existing file (no
        /// versioning — there is always exactly one file per case document
        /// at cases/{caseId}/{templateKey}/document.docx). Creates the
        /// document record on the first save.
        /// </summary>
        public ImmigrationCaseDocument SaveDocument(
            string caseId, string templateKey, string title,
            Stream docxStream, string modifiedUser)
        {
            var store = Load();
            var doc = GetDocument(caseId, templateKey);

            if (doc == null)
            {
                doc = new ImmigrationCaseDocument
                {
                    Key = $"{caseId}:{templateKey}",
                    Title = title,
                    TemplateKey = templateKey,
                    CurrentVersion = 1,
                    ModifiedUser = modifiedUser,
                    ModifiedDate = DateTime.Now,
                    Versions = new List<ImmigrationCaseDocumentVersion>()
                };
                store.Documents.Add(doc);
            }

            string fileName = "document.docx";
            Directory.CreateDirectory(GetDocumentFolder(caseId, templateKey));

            // Overwrite the existing file in place — the same document is
            // updated on every save instead of creating v2, v3, ...
            using (var fs = new FileStream(
                Path.Combine(GetDocumentFolder(caseId, templateKey), fileName),
                FileMode.Create, FileAccess.Write))
            {
                docxStream.Position = 0;
                docxStream.CopyTo(fs);
            }

            doc.CurrentFile = fileName;
            doc.ModifiedUser = modifiedUser;
            doc.ModifiedDate = DateTime.Now;
            doc.Versions = new List<ImmigrationCaseDocumentVersion>
            {
                new ImmigrationCaseDocumentVersion
                {
                    VersionNumber = 1,
                    FileName = fileName,
                    ModifiedUser = modifiedUser,
                    ModifiedDate = doc.ModifiedDate,
                }
            };

            Save(store);
            return doc;
        }
    }

    internal static class ImmigrationCaseDocumentExtensions
    {
        /// <summary>The case a document belongs to is encoded in its key
        /// ("{caseId}:{templateKey}"), so the document list stays flat in
        /// cases.json while still being scoped per case on the API surface.</summary>
        public static string CaseId(this ImmigrationCaseDocument doc)
            => doc.Key?.IndexOf(':') > -1 ? doc.Key.Substring(0, doc.Key.IndexOf(':')) : doc.Key;
    }
}
