# VistaJobs Project Flow

This note is for testers and DevOps engineers who need to understand the project without reading every file first.

## Runtime

- Backend: ASP.NET Core Web API, configured in `Program.cs`.
- Database: SQL Server, connection string key is `ConnectionStrings:DefaultConnection` in `appsettings.json`.
- Frontend: static files in `Frontend/`, usually opened through Live Server at `http://127.0.0.1:5500/index.html`.
- API base URL used by frontend: `https://localhost:7250/api` in `Frontend/js/app.js`.
- Resume files are stored in `Uploads/` and served publicly as `/Uploads/<file-name>`.

## Login And Routing

- Register API: `POST /api/Auth/register`
- Login API: `POST /api/Auth/login`
- Login returns JWT, name, role, and email.
- Frontend stores login data in `sessionStorage`.
- Role routing after login:
  - `admin` opens admin dashboard.
  - `employer` opens employer matching dashboard.
  - `jobseeker` checks `/api/Candidates/my-profile`.

## Jobseeker Profile Flow

- If `/api/Candidates/my-profile` returns `404`, the jobseeker has not submitted a profile yet.
- In that case, frontend shows Fresher / Experienced choice.
- After selecting a type, the matching form is shown.
- Submit API: `POST /api/Candidates`
- Candidate profile is keyed by login email.
- If a candidate row already exists for that email, the API updates it instead of creating a duplicate.
- If a candidate row exists on login, the frontend shows the submitted profile summary instead of the form.
- The `Update Profile` button opens the same form with saved values filled in.

## Verification Flow

- Aadhaar: `POST /api/Verification/verify-aadhaar`
- PAN: `POST /api/Verification/verify-pan`
- UAN: `POST /api/Verification/verify-uan`
- Verification status: `GET /api/Verification/candidate-verification`
- With `DigiLocker:Enabled = false`, valid local format marks the document as verified.
- With DigiLocker enabled, the API returns a redirect URL and the frontend redirects the user.

## Resume Flow

- Resume upload API: `POST /api/Candidates/upload-resume/{candidateId}`
- Allowed extensions: `.pdf`, `.doc`, `.docx`
- Saved DB value is only the public path, for example `/Uploads/file.pdf`.

## Employer Matching Flow

- Employer enters job requirements and skill chips.
- Job save API: `POST /api/Jobs`
- Candidate list API: `GET /api/Candidates`
- Frontend matches all selected employer skills against candidate skills.
- If employer selects `react` and `soc`, candidates with either skill are shown.

## Applications Flow

- Apply API: `POST /api/Applications`
- Duplicate check uses `JobId + CandidateEmail`.
- After save, confirmation email is sent using `EmailSettings` from `appsettings.json`.

## Admin Flow

- Dashboard: `GET /api/Admin/dashboard`
- Raw lists:
  - `GET /api/Admin/users`
  - `GET /api/Admin/jobs`
  - `GET /api/Admin/applications`

## DevOps Notes

- JWT settings must be present: `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`.
- SQL Server must be reachable from `DefaultConnection`.
- SMTP values are under `EmailSettings`.
- Keep `Uploads/` writable by the application process.
- Swagger is enabled and supports Bearer token authorization.
- Local migration check endpoint: `/__migrations`.
