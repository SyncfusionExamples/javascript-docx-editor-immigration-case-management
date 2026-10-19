## Required README structure and writing rules
 
Use these sections in this order. Omit a section only when it genuinely does not apply; do not remove required sections merely because information is missing—instead add a short, honest note or a hidden HTML TODO comment for the maintainer.
 
### 1. Title - # javascript-docx-editor-immigration-case-management
 
- Use a clear, specific title that names the main sample use case.
- Include the detected product and platform, for example: `Syncfusion React DOCX Editor – [Sample Use Case]` or `Syncfusion Angular DOCX Editor – [Sample Use Case]`.
- Keep the title readable and do not stuff it with implementation details.
 
### 2. Introduction
 
- In one short paragraph, state what the sample demonstrates and who might use it.
- Use the exact platform wording in the product name, including the trademark glyph where appropriate: `Syncfusion® React DOCX Editor`, `Syncfusion® Angular DOCX Editor`, and so on.
- Hyperlink the product name or a nearby “product page” phrase to the configured platform product page, including the configured UTM parameters.
- Describe only behavior confirmed by the source. Do not claim that an API, cloud service, or capability is provided unless the sample actually uses it.
 
### 3. Project Features
 
- Include roughly 3–6 short bullets covering only important, distinctive use cases and outcomes.
- Explain what a user can do, not implementation minutiae.
- Avoid lengthy package lists, generic framework features, marketing language, and claims not demonstrable in the sample.
 
### 4. Demo
 
- Use a screenshot for one main action or a single important result; use a GIF for a sequence of actions or an end-to-end workflow.
- Prefer repository-owned assets in a location such as `assets/`, `docs/images/`, or `public/`. Confirm the exact file path and casing.
- Give the image useful alt text. Use a relative path so the image renders in the GitHub repository.
- Do not fabricate screenshots, GIFs, links, or hosted demos. If no media is present, add a hidden TODO comment describing the suggested asset and report the missing asset to the user outside the README. Never leave a visible broken image.
- Add an online demo link only when a real, relevant demo URL is available and verified.
 
### 5. How to Run the Sample
 
Write for an end user who may not know the internals of this repository. Include only the prerequisites and commands supported by the checked-in project files.
 
When the project is client-only:
1. State the required runtime/toolchain based on manifests and project configuration.
2. Give the command to install dependencies from the correct directory.
3. Give the actual command to start the client.
4. State the local URL only if it is reliably defined or printed by the project's normal startup process; otherwise say to open the URL shown in the terminal.
 
When a Web API/backend is required:
1. Clearly explain that the sample has a client and a backend, and whether both must run.
2. Show the directory and command to start the backend using the actual project type and configuration.
3. Explain how to configure the client to call the backend. Name the **real file path and setting/property** found in the repository, such as a `serviceUrl`, API base URL, environment variable, or configuration value. Include a safe example with placeholders only, for example `https://localhost:<port>/<api-route>/`.
4. State whether the user must change the URL before starting the client. Do not guess a port, route, CORS policy, or protocol; confirm it from launch settings, server routes, and client code.
5. Show the client install/start steps from its own directory.
6. If the sample can run without the backend for some features, distinguish client-side features from backend-dependent features. Do not say a backend is optional when the sample's workflow requires it.
7. Mention required public environment-variable names or non-secret configuration keys only when supported by the repository. Use placeholders for values and never print real credentials.
 
Keep steps numbered, ordered, and copyable. Avoid exposing internal deployment topology. Never publish internal hostnames or real infrastructure identifiers; use placeholders and tell the reader to provide their own configuration.
 
### 6. APIs Used
 
Include a Markdown table with these columns:
 
| API / Endpoint | HTTP Method | Purpose |
|---|---|---|
 
Rules:
- List the HTTP APIs that the **client project actually calls** and that are relevant to understanding/running this sample. Include the method (`GET`, `POST`, `PUT`, etc.) and a brief user-oriented description of each endpoint's purpose.
- Use a relative route where possible (for example, `/api/documenteditor/import`) rather than embedding a development server's host or port.
 
- If the backend is included, cross-check routes and methods against its controller attributes/minimal API mappings. If only the client exists, derive method and route from the actual client request code.
- Distinguish custom sample APIs from the Syncfusion DOCX Editor's configured document-processing service when both are used. Explain the role of each briefly.
- Do not list ordinary local function calls or third-party SDK methods as HTTP APIs.
- Do not infer the HTTP method from the endpoint name. If the method cannot be established, label it `Unconfirmed in source` or omit it and report the gap rather than guessing.
- If the client makes no custom HTTP calls, state that no custom sample-specific Web APIs were identified. Do not invent an API table with fictional endpoints.
 
### 7. Resources
 
Add a concise list of relevant, verified links, as available:
- **Product page:** use the configured platform-specific Syncfusion DOCX Editor product page.
- **Documentation:** use the configured platform-specific official documentation.
- **Online demo:** use the configured official demo URL, preferably the relevant sample route when a reliable route exists.
 
Use this label pattern, adapting the platform name and generated URLs:
 
```markdown
# Resources
 
- **Product page:** [Syncfusion® React DOCX Editor](<configured-product-url-with-utm>)
- **Documentation:** [Syncfusion® React DOCX Editor - Documentation](<configured-documentation-url-with-utm>)
- **Online demo:** [Syncfusion® React DOCX Editor - Online demo](<configured-demo-url-with-utm>)
```
 
Apply configured UTM parameters according to `utm_scope`. Remove empty or unverified entries and do not add unrelated links just to lengthen the section. Replace example platform labels and angle-bracket placeholders; never publish placeholders literally.
 
### 8. Support and feedback
 
Include this section when the configured official links are available. Use a concise, public-facing pattern such as:
 
- For questions, contact the [Syncfusion support team](SUPPORT_URL) or post in the [community forums](FORUM_URL).
- Request a new feature through the [Syncfusion® feedback portal](FEEDBACK_URL).
 
Use this heading and concise format:
 
```markdown
# Support and feedback
 
For any other queries, contact the [Syncfusion® support team](<support-url-with-utm>) or post your questions in the [community forums](<forum-url-with-utm>).
 
Request a new feature through the [Syncfusion® feedback portal](<feedback-url-with-utm>).
```
 
Replace placeholders with generated links and apply UTM parameters according to `utm_scope`. If one of the links is not verified or configured, omit that link rather than inventing a URL. Never publish placeholders literally.
 
### 9. License
 
Use the configured EULA, purchase, and trial links with the configured UTM parameters. The recommended wording is defined in the **License wording** configuration above. Keep it short and neutral; do not paste the EULA or make legal claims beyond the linked source.
 
- If a repository `LICENSE` file exists, link to it using a relative path and describe it separately as the license for the sample source code.
- Do not conflate the sample source-code license with the Syncfusion EULA.
- Do not include a Syncfusion license key, environment value, customer identifier, or private licensing instructions.
 
## Public-safety rules — mandatory
 
The README must be safe to publish in a public GitHub repository. Never include any of the following, even if found in the source tree:
 
- API keys, access tokens, passwords, client secrets, private keys, certificates, connection strings, SAS tokens, bearer tokens, cookies, or license keys.
- Real values copied from `.env`, secret stores, local user settings, deployment profiles, `appsettings` files, logs, request headers, or private configuration.
- Private/internal hostnames, intranet URLs, non-public IPs, private repository links, or private service endpoints.
- Confidential customer, tenant, account, subscription, storage, deployment, or employee information.
- Request/response samples containing real user data, document contents, credentials, or personally identifiable information.
 
Additional rules:
- Do not reveal a secret in README prose, Markdown code, HTML comments, screenshots, GIFs, or API examples.
- Do not copy secret-looking values into the README simply because they are already committed in source control.
- Use neutral placeholders such as `<your-api-base-url>`, `<your-port>`, or `<your-configuration-value>`; do not use realistic fake keys that could be mistaken for real credentials.
- A public base URL may be documented only if it is intended for public use and verified. Otherwise use a placeholder and explain where the user supplies their own URL.
- If a potential secret or confidential value is found in a source file, do not repeat its value. Finish the safe README where possible, then give the user a separate warning naming the affected file and (if useful) line number, without reproducing the sensitive content. Recommend removing it from public history and rotating/revoking it when appropriate. Do not modify source files unless asked.
- Do not include internal architectural detail, debug logs, stack traces, private workarounds, or internal-only support instructions unless they are genuinely required for an end user's setup and are safe to publish.
 
 
Just append the Resources, Support Feedback and License section for REACT projects :
Resources
Product page:   Syncfusion® React DOCX Editor 
Documentation:   Syncfusion® React DOCX Editor - Documentation 
Online demo:   Syncfusion® React DOCX Editor - Online demo 
Support and feedback 
For any other queries, reach our Syncfusion® support team or post the queries through the community forums. 
Request new feature through Syncfusion® feedback portal. 
License
This is a commercial product and requires a paid license for possession or use Syncfusion's licensed software, including this component, is subject to the terms and conditions of Syncfusion's EULA. You can purchase a license here or start a free 30-day trial here. 