using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EJ2APIServices.Services;

namespace EJ2APIServices.Models
{
    /// <summary>
    /// An immigration case from cases.json (App_Data/ImmigrationCases).
    /// Mirrors the CASES mock data in immigration-poc-flow.html.
    /// </summary>
    public class ImmigrationCase
    {
        public string Id { get; set; }
        public string Client { get; set; }
        public string Type { get; set; }
        public string Form { get; set; }
        public string Stage { get; set; }
        public string Receipt { get; set; }
        public string Employer { get; set; }
        public string Attorney { get; set; }
        public string Paralegal { get; set; }

        /// <summary>
        /// Furthest workflow step (0-4) the case has reached, so the
        /// workspace resumes where the user left off instead of starting
        /// from the beginning on every visit.
        /// </summary>
        public int ProgressStep { get; set; } = 0;
    }

    /// <summary>
    /// A template available in the "Open template" step, mirroring TEMPLATES
    /// in the POC. The DOCX lives under
    /// App_Data/ImmigrationCases/templates/{Key}/v1.docx and is read-only —
    /// client documents are saved separately under cases/{caseId}/.
    /// </summary>
    public class ImmigrationTemplate
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Note { get; set; }
    }

    /// <summary>
    /// A document generated for a case and stored in the case file.
    /// Follows the same storage pattern as ContentLibrary items:
    /// App_Data/ImmigrationCases/cases/{caseId}/{documentKey}/v{n}.docx
    /// with metadata persisted in cases.json.
    /// </summary>
    public class ImmigrationCaseDocument
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string TemplateKey { get; set; }
        public int CurrentVersion { get; set; } = 1;
        public string CurrentFile { get; set; }
        public string ModifiedUser { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public List<ImmigrationCaseDocumentVersion> Versions { get; set; }
            = new List<ImmigrationCaseDocumentVersion>();
    }

    public class ImmigrationCaseDocumentVersion
    {
        public int VersionNumber { get; set; }
        public string FileName { get; set; }
        public string ModifiedUser { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Root JSON stored at App_Data/ImmigrationCases/cases.json.
    /// </summary>
    public class ImmigrationCaseStore
    {
        public List<ImmigrationCase> Cases { get; set; } = new List<ImmigrationCase>();
        public List<ImmigrationTemplate> Templates { get; set; } = new List<ImmigrationTemplate>();
        public List<ImmigrationCaseDocument> Documents { get; set; } = new List<ImmigrationCaseDocument>();
    }
}
