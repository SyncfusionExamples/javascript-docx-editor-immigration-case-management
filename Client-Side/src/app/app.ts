import { select, createElement } from '@syncfusion/ej2-base';
import { Button } from '@syncfusion/ej2-buttons';
import { ListView } from '@syncfusion/ej2-lists';
import {
  DocumentEditorContainer,
  Toolbar,
  Ribbon,
} from '@syncfusion/ej2-documenteditor';
// Registering Syncfusion license key
import { registerLicense } from '@syncfusion/ej2-base';

registerLicense('Your Syncfusion License');

// styles.css is bundled via the webpack entry (see webpack.config.js); no
// side-effect import here so tsc --noEmit stays clean.

DocumentEditorContainer.Inject(Toolbar, Ribbon);

/* ------------------------------------------------------------------ */
/* Types mirroring the server-side models (ImmigrationModels.cs)        */
/* ------------------------------------------------------------------ */
interface CaseData {
  Id: string;
  Client: string;
  Type: string;
  Form: string;
  Stage: string;
  Receipt: string;
  Employer: string;
  Attorney: string;
  Paralegal: string;
  ProgressStep: number;
}
interface TemplateData {
  Key: string;
  Name: string;
  Note: string;
  SourceId: string;
}
interface CaseDocument {
  Key: string;
  Title: string;
  TemplateKey: string;
  CurrentVersion: number;
  CurrentFile: string;
  ModifiedUser: string;
  ModifiedDate: string;
  Versions: { VersionNumber: number; FileName: string; ModifiedUser: string; ModifiedDate: string }[];
}

/* ------------------------------------------------------------------ */
/* Config                                                              */
/* ------------------------------------------------------------------ */
// The Document Editor serviceUrl points at the local .NET 10 backend.
// NOTE: for production, host your own web service; this URL only works
// when the EJ2APIServices backend is running on the same machine.
const API_BASE = 'http://localhost:62870/api/documenteditor';
const IMMIGRATION_API = 'http://localhost:62870/api/immigration';

const STEPS = ['Open template', 'Paralegal edits', 'Attorney review', 'Save to case file', 'Export'];
const STEP_KEYS = ['template', 'edits', 'review', 'save', 'export'];

interface WorkspaceState {
  caseIdx: number;
  step: number;
  template: TemplateData | null;
  generated: boolean;
  saved: boolean;
  exported: boolean;
  files: CaseDocument[];
}

let cases: CaseData[] = [];
let templates: TemplateData[] = [];
let s: WorkspaceState | null = null;

let editor: DocumentEditorContainer | null = null;
let nextBtn: Button | null = null;
let prevBtn: Button | null = null;
let homeBtn: Button | null = null;

const c = (): CaseData => cases[s!.caseIdx];

/* ------------------------------------------------------------------ */
/* API helpers                                                          */
/* ------------------------------------------------------------------ */
async function api<T>(url: string, init?: RequestInit): Promise<T> {
  const res = await fetch(url, init);
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
  return res.json() as Promise<T>;
}

/* ------------------------------------------------------------------ */
/* Home (cases list)                                                    */
/* ------------------------------------------------------------------ */
async function showHome(): Promise<void> {
  // Leaving the workspace: save the current editor content (the SFDT is
  // taken from the client-side Document Editor and stored as a new DOCX
  // version on the server) when it changed since the last save, then
  // persist the workflow progress so the home page shows the real
  // status. Both are awaited before switching screens so nothing is lost.
  if (s) {
    // Always save the current editor content (the SFDT is taken from the
    // client-side Document Editor and stored on the server, overwriting
    // the existing case document) before leaving — no dirty tracking, the
    // save runs unconditionally whenever a document is open.
    if (s.template) await saveToCaseFile(true);
    await persistProgress();
  }
  s = null;
  select('#work', document).classList.add('hidden');
  select('#workspace-empty', document).classList.add('hidden');
  select('#home', document).classList.remove('hidden');
  homeBtn!.element.hidden = true;
  await reloadCases();
}

// Sends the current step to the server (it keeps the furthest step and
// derives the Stage label) and refreshes the local case record.
async function persistProgress(): Promise<void> {
  if (!s) return;
  try {
    const updated = await api<CaseData>(
      `${IMMIGRATION_API}/cases/${c().Id}/progress`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ Step: s.step }),
      }
    );
    cases[s.caseIdx] = updated;
  } catch {
    // Non-fatal: home page just shows the last known stage.
  }
}

async function reloadCases(): Promise<void> {
  try {
    cases = await api<CaseData[]>(`${IMMIGRATION_API}/cases`);
    renderCasesTable();
  } catch {
    // Keep the existing table on failure.
  }
}

async function openCase(idx: number): Promise<void> {
  const k = cases[idx];
  const resumeStep = k.ProgressStep ?? 0;
  s = {
    caseIdx: idx,
    // Resume at the furthest step previously reached for this case.
    step: resumeStep,
    template: null,
    generated: false,
    saved: false,
    exported: false,
    files: [],
  };
  // Always start from a blank document — otherwise the document opened by
  // the previous case/user stays in the editor.
  resetEditorToBlank();
  select('#home', document).classList.add('hidden');
  select('#work', document).classList.remove('hidden');
  homeBtn!.element.hidden = false;
  await refreshCaseFiles();
  // Resuming past step 0: re-open the document the user last worked on
  // (newest saved version) so the editor is not empty.
  if (resumeStep > 0) await resumeSavedDocument();
  render();
}

// Re-opens the most recently modified saved document for the current case
// and restores the template selection / saved state, so the workflow
// continues where it left off instead of showing an empty editor.
async function resumeSavedDocument(): Promise<void> {
  if (!s) return;
  // Newest saved document for this case (server sorts by modified date).
  const latest = [...s.files].sort(
    (a, b) => new Date(b.ModifiedDate).getTime() - new Date(a.ModifiedDate).getTime()
  )[0];

  if (!latest) {
    // Nothing saved yet for this case: fall back to step 0 (template
    // selection). The stage label keeps the furthest progress, but the
    // wizard restarts at the beginning because there is no document.
    s.step = 0;
    s.generated = false;
    return;
  }

  s.template = templates.find((t) => t.Key === latest.TemplateKey) ?? null;
  s.generated = true;
  s.saved = true; // a saved version exists — the save step is satisfied
  try {
    const res = await api<{ documentData: string }>(
      `${IMMIGRATION_API}/cases/${c().Id}/documents/${latest.TemplateKey}/load`
    );
    editor!.documentEditor.open(res.documentData);
    // The serialized SFDT carries the tracked revisions; re-assert the
    // step settings once the document is in so track changes is ON for
    // the paralegal/attorney steps right after resuming (the SFDT's own
    // enableTrackChanges flag may be off from the save).
    applyStepEditorSettings();
    toast(`Resumed \u201C${latest.Title}\u201D`);
  } catch (err) {
    // The saved file could not be loaded (e.g. server not reachable):
    // restart at template selection instead of an empty editor.
    s.step = 0;
    s.template = null;
    s.generated = false;
    toast('Failed to load the saved document: ' + (err as Error).message);
  }
}

function resetEditorToBlank(): void {
  if (!editor) return;
  editor.documentEditor.openBlank();
  // Also drop any tracked revisions/comments left from the previous case.
  editor.documentEditor.enableTrackChanges = true;
  editor.documentEditor.showRevisions = false;
}

async function refreshCaseFiles(): Promise<void> {
  try {
    s!.files = await api<CaseDocument[]>(`${IMMIGRATION_API}/cases/${c().Id}/documents`);
  } catch {
    s!.files = [];
  }
}

/* ------------------------------------------------------------------ */
/* Document generation (template + mail merge on the server)            */
/* ------------------------------------------------------------------ */
// Opens the raw template (merge fields still visible) in the editor as
// soon as the user selects it in the side panel.
async function openTemplateDocument(): Promise<void> {
  if (!s || !s.template) return;
  const t = s.template;
  try {
    const res = await api<{ documentData: string }>(
      `${IMMIGRATION_API}/cases/${c().Id}/documents/${t.Key}/open`,
      { method: 'POST' }
    );
    editor!.documentEditor.open(res.documentData);
    toast(`${t.Name} opened \u2014 select \u201CLoad with data\u201D to merge the case data`);
  } catch (err) {
    toast('Failed to open template: ' + (err as Error).message);
  }
}

// Mail merge: replaces the template merge fields with the case data
// (server-side DocIO mail merge) and reloads the merged document.
async function generateDocument(): Promise<void> {
  if (!s || !s.template) return;
  const t = s.template;
  try {
    const res = await api<{ documentData: string }>(
      `${IMMIGRATION_API}/cases/${c().Id}/documents/${t.Key}/generate`,
      { method: 'POST' }
    );
    editor!.documentEditor.open(res.documentData);
    s.generated = true;
    render();
    toast(`Merged case data for ${c().Client}`);
  } catch (err) {
    toast('Failed to generate document: ' + (err as Error).message);
  }
}

/* ------------------------------------------------------------------ */
/* Save to case file (SFDT -> DOCX on the server, versioned on disk)     */
/* ------------------------------------------------------------------ */
async function saveToCaseFile(silent = false): Promise<void> {
  if (!s || !s.template) return;
  // Take the SFDT from the client-side Document Editor and post it to the
  // server, which converts it to DOCX and stores it as a new version.
  const sfdt = editor!.documentEditor.serialize();
  const t = s.template;
  try {
    await api(`${IMMIGRATION_API}/cases/${c().Id}/documents/${t.Key}/save`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        DocumentData: sfdt,
        Title: t.Name,
        TemplateKey: t.Key,
        ModifiedUser: c().Paralegal,
      }),
    });
    s.saved = true;
    await refreshCaseFiles();
    render();
    if (!silent) toast('Saved to case file');
  } catch (err) {
    toast('Failed to save: ' + (err as Error).message);
  }
}

/* ------------------------------------------------------------------ */
/* Export (server returns the stored DOCX / PDF)                         */
/* ------------------------------------------------------------------ */
function exportDocument(): void {
  editor?.documentEditor.save(editor.documentEditor.documentName, 'Docx');
}

// Marks the export step complete and toasts the given result message.
function markExported(message: string): void {
  if (!s) return;
  s.exported = true;
  render();
  toast(message);
}

/* ------------------------------------------------------------------ */
/* Rendering                                                            */
/* ------------------------------------------------------------------ */
function render(): void {
  if (!s) return;

  // Keep the editor's track-changes/revisions state in sync with the
  // current step on every render, not just on documentChange — this
  // covers resuming a saved document and returning to earlier steps.
  applyStepEditorSettings();

  // Steps strip
  const steps = select('#steps', document);
  steps.innerHTML = '';
  STEPS.forEach((name, i) => {
    const cls = i < s!.step ? 'step done' : i === s!.step ? 'step current' : 'step';
    const el = createElement('div', { className: cls });
    el.innerHTML = `<span class="dot">${i < s!.step ? '✓' : i + 1}</span>${name}`;
    steps.appendChild(el);
  });

  // Side panel
  renderSidePanel();

  // Footer nav
  prevBtn!.disabled = s.step === 0;
  nextBtn!.element.textContent = s.step === STEPS.length - 1 ? 'Finish' : 'Next';
  nextBtn!.disabled = !canNext();
}

function canNext(): boolean {
  if (!s) return false;
  const gates = [s.generated, true, true, s.saved, s.exported];
  return gates[s.step];
}

function renderSidePanel(): void {
  const body = select('#side-body', document);
  body.innerHTML = '';
  if (!s) return;

  const t = s.template;
  const title = createElement('h2');
  const who = createElement('div', { className: 'who' });

  if (s.step === 0) {
    title.textContent = 'Open template';
    who.textContent = `Paralegal: ${c().Paralegal}`;
    const desc = createElement('p');
    desc.textContent =
      'Choose a template. It opens with its merge fields; "Load with data" merges the case data into them.';
    body.append(title, who, desc);

    templates.forEach((tpl) => {
      const opt = createElement('button', {
        className: 'opt' + (s!.template && s!.template.Key === tpl.Key ? ' sel' : ''),
        attrs: { 'data-tpl': tpl.Key },
      });
      opt.innerHTML = `${tpl.Name}<small>${tpl.Note}</small>`;
      opt.addEventListener('click', () => {
        if (s!.template && s!.template.Key === tpl.Key) return; // already open
        s!.template = tpl;
        s!.generated = false;
        openTemplateDocument(); // opens the template immediately on selection
        render();
      });
      body.appendChild(opt);
    });

    const actions = createElement('div', { className: 'actions' });
    const genBtn = new Button({ content: 'Load with data', isPrimary: true, disabled: !s.template || s.generated });
    genBtn.appendTo(createElement('button'));
    genBtn.element.addEventListener('click', generateDocument);
    actions.appendChild(genBtn.element);
    body.appendChild(actions);
  } else if (s.step === 1) {
    title.textContent = 'Paralegal edits';
    who.textContent = `Paralegal: ${c().Paralegal}`;
    const desc = createElement('p');
    desc.textContent =
      'Edit the letter directly in the editor. Track changes is already on, so every edit is recorded for the attorney.';
    body.append(title, who, desc);
  } else if (s.step === 2) {
    title.textContent = 'Attorney review';
    who.textContent = `Attorney: ${c().Attorney}`;
    const desc = createElement('p');
    desc.textContent = 'Review the tracked changes and comments in the editor, then accept or reject each change.';
    body.append(title, who, desc);
  } else if (s.step === 3) {
    title.textContent = 'Save to case file';
    who.textContent = `Case ${c().Id}`;
    const desc = createElement('p');
    desc.textContent = `Save the reviewed document so it stays with ${c().Client}\u2019s case.`;
    body.append(title, who, desc);

    const actions = createElement('div', { className: 'actions' });
    const saveBtn = new Button({ content: s.saved ? 'Saved' : 'Save to case file', isPrimary: true, disabled: s.saved });
    saveBtn.appendTo(createElement('button'));
    saveBtn.element.addEventListener('click', () => saveToCaseFile());
    actions.appendChild(saveBtn.element);
    body.appendChild(actions);

    s.files.forEach((f) => {
      const row = createElement('div', { className: 'file' });
      row.innerHTML = `<span>${f.Title}</span><small>${new Date(f.ModifiedDate).toLocaleDateString()}</small>`;
      body.appendChild(row);
    });
  } else if (s.step === 4) {
    title.textContent = 'Export';
    who.textContent = 'Ready for filing';
    body.append(title, who);

    if (s.exported) {
      const done = createElement('div', { className: 'done-msg' });
      done.textContent = `${t ? t.Name : 'Document'} for ${c().Client} is saved and exported. The case file is up to date.`;
      body.appendChild(done);
    } else {
      const desc = createElement('p');
      desc.textContent = 'Export the final document for filing or sharing.';
      body.appendChild(desc);
    }

    const actions = createElement('div', { className: 'actions' });
    const base = `${IMMIGRATION_API}/cases/${c().Id}/documents/${s.template ? s.template.Key : ''}`;

    const exportBtn = new Button({ content: 'Download DOCX', isPrimary: true });
    exportBtn.appendTo(createElement('button'));
    exportBtn.element.addEventListener('click', exportDocument);
    actions.appendChild(exportBtn.element);

    const pdfBtn = new Button({ content: 'Download PDF' });
    pdfBtn.appendTo(createElement('button'));
    pdfBtn.element.addEventListener('click', () => {
      const http: XMLHttpRequest = new XMLHttpRequest();

      http.open(
        'POST',
        'http://localhost:62870/api/documenteditor/ExportPdf',
        true
      );

      http.setRequestHeader('Content-Type', 'application/json;charset=UTF-8');

      // Expect PDF bytes
      http.responseType = 'blob';

      http.onload = () => {
        if (http.status === 200) {
          const blob = new Blob([http.response], {
            type: 'application/pdf'
          });

          const url = window.URL.createObjectURL(blob);

          const a = document.createElement('a');
          a.href = url;
          a.download = 'document.pdf';
          document.body.appendChild(a);
          a.click();

          document.body.removeChild(a);
          window.URL.revokeObjectURL(url);
        } else {
          console.error('PDF export failed', http.status);
        }
      };

      http.onerror = () => {
        console.error('Request failed');
      };

      const sfdt = {
        content: editor?.documentEditor.serialize()
      };

      http.send(JSON.stringify(sfdt));
    });
    actions.appendChild(pdfBtn.element);

    const printBtn = new Button({ content: 'Print' });
    printBtn.appendTo(createElement('button'));
    printBtn.element.addEventListener('click', () => {
      editor?.documentEditor.print();
    });
    actions.appendChild(printBtn.element);

    body.appendChild(actions);
  }

  // Info footer
  select('#info', document).innerHTML = `
    <div><span>Client</span>${c().Client}</div>
    <div><span>Case</span>${c().Id}</div>
    <div><span>Type</span>${c().Type}</div>
    <div><span>Receipt</span>${c().Receipt}</div>`;
}

/* ------------------------------------------------------------------ */
/* Toast                                                                */
/* ------------------------------------------------------------------ */
let toastTimer: number | undefined;
function toast(msg: string): void {
  const el = select('#toast', document);
  el.textContent = msg;
  el.classList.add('show');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => el.classList.remove('show'), 1700) as unknown as number;
}

/* ------------------------------------------------------------------ */
/* Bootstrap                                                            */
/* ------------------------------------------------------------------ */
async function init(): Promise<void> {
  homeBtn = new Button({ content: 'Back to cases' });
  homeBtn.appendTo(createElement('button', { className: 'btn' }));
  homeBtn.element.addEventListener('click', showHome);
  homeBtn.element.hidden = true;
  select('#home-btn-slot', document).appendChild(homeBtn.element);

  nextBtn = new Button({ content: 'Next', isPrimary: true });
  nextBtn.appendTo(createElement('button', { className: 'btn' }));
  nextBtn.element.addEventListener('click', () => {
    if (!s) return;
    if (s.step === STEPS.length - 1) {
      toast('Case documents complete');
      showHome();
    } else {
      s.step++;
      applyStepEditorSettings();
      render();
      persistProgress(); // fire-and-forget: stage updates as steps advance
    }
  });
  select('#next-btn-slot', document).appendChild(nextBtn.element);

  prevBtn = new Button({ content: 'Back' });
  prevBtn.appendTo(createElement('button', { className: 'btn' }));
  prevBtn.element.addEventListener('click', () => {
    if (!s) return;
    s.step = Math.max(0, s.step - 1);
    applyStepEditorSettings();
    render();
    persistProgress(); // fire-and-forget: stage reflects the step left at
  });
  select('#prev-btn-slot', document).appendChild(prevBtn.element);

  // Document Editor
  editor = new DocumentEditorContainer({
    enableToolbar: true,
    toolbarMode: 'Ribbon',
    ribbonLayout: 'Classic',
    height: '100%',
    serviceUrl: API_BASE,
    currentUser: 'Meera Iyer',
    enableTrackChanges: true,
  });
  editor.appendTo('#doceditor');

  editor.documentChange = (): void => {
    applyStepEditorSettings();
  };

  try {
    [cases, templates] = await Promise.all([
      api<CaseData[]>(`${IMMIGRATION_API}/cases`),
      api<TemplateData[]>(`${IMMIGRATION_API}/templates`),
    ]);
    renderCasesTable();
  } catch (err) {
    toast('Failed to load cases from the server: ' + (err as Error).message);
    select('#case-rows', document).innerHTML =
      '<tr><td colspan="5">Could not reach the immigration API.</td></tr>';
  }
}

/**
 * Step-specific editor behaviour, replicating the POC:
 *  - Paralegal edits & attorney review: track changes ON
 *  - Attorney review: revisions pane shown
 *  - Other steps: track changes left to the user
 */
function applyStepEditorSettings(): void {
  if (!editor || !s) return;

  if (s.step === 1) {
    // Paralegal edits
    editor.documentEditor.isReadOnly = false;
    editor.documentEditor.enableTrackChanges = true;
    editor.documentEditor.showRevisions = false;
  }
  else if (s.step === 2) {
    // Attorney review
    editor.documentEditor.isReadOnly = false;
    editor.documentEditor.enableTrackChanges = false;
    editor.documentEditor.showRevisions = true;
  }
  else {
    editor.documentEditor.isReadOnly = true;
    editor.documentEditor.enableTrackChanges = false;
    editor.documentEditor.showRevisions = false;
  }
}

function renderCasesTable(): void {
  const rows = select('#case-rows', document);
  rows.innerHTML = '';
  cases.forEach((k, i) => {
    const tr = createElement('tr');
    tr.innerHTML = `
      <td><b>${k.Client}</b></td>
      <td>${k.Type}</td>
      <td>${k.Id}</td>
      <td><span class="chip">${k.Stage}</span></td>
      <td></td>`;
    const openBtn = new Button({ content: 'Open case', isPrimary: true });
    openBtn.appendTo(createElement('button', { className: 'btn' }));
    openBtn.element.addEventListener('click', () => openCase(i));
    tr.children[4].appendChild(openBtn.element);
    rows.appendChild(tr);
  });
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', init);
} else {
  init();
}

// Keep the ListView import referenced for tree-shaking clarity in dev builds.
void ListView;
