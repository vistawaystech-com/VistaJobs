'use strict';

const API_BASE_URL = "https://localhost:7250/api";

// Login state is kept only for this browser session.
// This prevents stale user details from appearing after closing/reopening the browser.
const authStorage = sessionStorage;

function clearSavedLogin() {

    ["token", "role", "name", "email"].forEach(key => {
        authStorage.removeItem(key);
        localStorage.removeItem(key);
    });
}

function clearLegacyPersistentLogin() {

    ["token", "role", "name", "email"].forEach(key =>
        localStorage.removeItem(key)
    );
}

const Skills = {
    fresher: [],
    experienced: [],
    employer: []
};
let allJobs = [];
/* PAGE NAVIGATION */
function showPage(pageId) {

    // index.html is a single-page UI. This toggles the visible section.
    document.querySelectorAll('.page').forEach(p => {
        p.classList.remove('active');
    });

    const target = document.getElementById('page-' + pageId);

    if (target) {
        target.classList.add('active');
    }
    document.querySelectorAll('.nav-link[data-page]').forEach(btn => {
        btn.classList.toggle('active', btn.dataset.page === pageId);
    });

    document.getElementById('navMobile')?.classList.remove('open');

    window.scrollTo({
        top: 0,
        behavior: 'smooth'
    });
}

/* REGISTER */
async function handleRegister() {
    try {
        const name = document.getElementById('register-name')?.value.trim();
        const email = document.getElementById('register-email')?.value.trim();
        const password = document.getElementById('register-pass')?.value.trim();
        const role = document.getElementById('register-role')?.value;

        if (!name || !email || !password || !role) {
            showToast('Please fill all fields', 'error');
            return;
        }

        if (!validateEmail(email)) {
            showToast('Invalid email', 'error');
            return;
        }

        const payload = {
            FullName: name,
            Email: email,
            Password: password,
            Role: role
        };

        const resp = await fetch(`${API_BASE_URL}/Auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (!resp.ok) {
            const t = await resp.text();
            showToast(t || 'Registration failed', 'error');
            return;
        }

        // Clear register form inputs so values are not retained
        const rn = document.getElementById('register-name'); if (rn) rn.value = '';
        const re = document.getElementById('register-email'); if (re) re.value = '';
        const rp = document.getElementById('register-pass'); if (rp) rp.value = '';
        const rr = document.getElementById('register-role'); if (rr) rr.value = '';

        showToast('Account created successfully', 'success');
        //navigate to login page after registration instead of auto-login, to avoid confusion and also because user may want to login later. This also prevents the need to persist entered credentials in the form which can be a security risk.
        if (role === 'jobseeker') showPage('login');
        else if (role === 'employer') showPage('login');

        // Navigate to appropriate page without persisting entered credentials
        // if (role === 'jobseeker') showPage('jobseeker');
        // else if (role === 'employer') showPage('employer');
        // else showPage('login');

    } catch (error) {
        console.error(error);
        showToast('Registration failed', 'error');
    }
}

/* TOAST */
let toastTimer;

function showToast(message, type = 'info') {

    const toast = document.getElementById('toast');

    if (!toast) return;

    toast.className = `toast ${type} `;

    toast.innerHTML = message;

    toast.classList.add('show');

    clearTimeout(toastTimer);

    toastTimer = setTimeout(() => {

        toast.classList.remove('show');

    }, 3000);
}

/* MODAL */
function showModal(title, message) {

    document.getElementById('modalTitle').innerHTML = title;

    document.getElementById('modalMsg').innerHTML = message;

    document.getElementById('modal').classList.add('open');
}

function closeModal() {

    document.getElementById('modal').classList.remove('open');
}

/* MOBILE NAV */
function toggleMobileNav() {

    document.getElementById('navMobile')?.classList.toggle('open');
}

function togglePassword(id) {
    const el = document.getElementById(id);
    if (!el) return;
    if (el.type === 'password') el.type = 'text'; else el.type = 'password';
}

/* SKILLS */
function addSkill(event, type) {

    if (event.key !== 'Enter' && event.key !== ',') return;

    event.preventDefault();

    const input = event.target;

    const val = input.value.trim().replace(/[,]+$/, '');

    if (!val) return;

    if (Skills[type].includes(val)) {

        showToast("Skill already added");

        return;
    }

    Skills[type].push(val);

    renderChip(val, type, input);

    input.value = '';
}

function renderChip(val, type, inputEl) {

    const chip = document.createElement('span');

    chip.className = 'skill-chip';

    chip.innerHTML = `
        ${val}
<button type="button"
    onclick="removeSkill(this,'${val}','${type}')">
    ×
</button>
`;

    inputEl.parentElement.insertBefore(chip, inputEl);
}

function setFieldValue(id, value) {

    const field = document.getElementById(id);

    if (field && value !== null && value !== undefined) {
        field.value = value;
    }
}

function setSelectValue(id, value) {

    const field = document.getElementById(id);

    if (!field || value === null || value === undefined) {
        return;
    }

    const stringValue = String(value);

    field.value = stringValue;

    if (field.value !== stringValue) {
        field.value = "";
    }
}

function formatDateInput(value) {

    if (!value) {
        return "";
    }

    return String(value).split("T")[0];
}

function setSkills(type, skills) {

    // Rebuild skill chips from the comma-separated value saved in the database.
    const prefix =
        type === "fresher"
            ? "f"
            : "e";

    const wrap =
        document.getElementById(`${prefix}-skills-wrap`);

    const input =
        document.getElementById(`${prefix}-skill-input`);

    if (!wrap || !input) {
        return;
    }

    wrap.querySelectorAll(".skill-chip")
        .forEach(chip => chip.remove());

    Skills[type] = (skills || "")
        .split(",")
        .map(skill => skill.trim())
        .filter(Boolean);

    Skills[type].forEach(skill =>
        renderChip(skill, type, input)
    );
}

function getTokenPayload() {

    const token =
        authStorage.getItem("token");

    if (!token) {
        return {};
    }

    try {
        return JSON.parse(
            atob(token.split(".")[1])
        );
    } catch (error) {
        return {};
    }
}

function getLoggedInEmail() {

    // New login response contains email. JWT fallback keeps older sessions working.
    const payload = getTokenPayload();

    return authStorage.getItem("email") ||
        payload.email ||
        payload[
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
        ] ||
        "";
}

function clearCandidateForm(prefix) {

    // Clear all fields before a new user/profile path opens the form.
    [
        "name",
        "email",
        "phone",
        "dob",
        "pan",
        "aadhaar",
        "address",
        "pref-loc",
        "salary",
        "resume",
        "qual",
        "year",
        "percent",
        "college",
        "role",
        "uan",
        "company",
        "jobtitle",
        "exp",
        "notice",
        "curr-sal",
        "about"
    ].forEach(field => {

        const element =
            document.getElementById(`${prefix}-${field}`);

        if (!element) {
            return;
        }

        if (element.type === "file") {
            element.value = "";
            return;
        }

        element.value = "";
    });

    const type =
        prefix === "e"
            ? "experienced"
            : "fresher";

    setSkills(type, "");
    clearIdentificationFields(prefix);
}

function prefillLoggedInIdentity(prefix) {

    setFieldValue(
        `${prefix}-name`,
        authStorage.getItem("name") || ""
    );

    setFieldValue(
        `${prefix}-email`,
        getLoggedInEmail()
    );
}

function resetJobseekerForms(prefillIdentity = false) {

    // Fresh profile creation must not reuse values from a previous login.
    loadedCandidateType = null;

    clearCandidateForm("f");
    clearCandidateForm("e");

    if (prefillIdentity) {
        prefillLoggedInIdentity("f");
        prefillLoggedInIdentity("e");
    }
}

let submittedCandidate = null;

function setJobseekerHeader(submitted) {

    const header =
        document.querySelector("#page-jobseeker .form-header");

    if (!header) {
        return;
    }

    const title = header.querySelector("h2");
    const copy = header.querySelector("p");

    if (title) {
        title.textContent = submitted
            ? "Your Profile"
            : "Create Your Profile";
    }

    if (copy) {
        copy.textContent = submitted
            ? "Your submitted details are saved and ready for employer matches."
            : "Choose fresher or experienced to complete your jobseeker profile.";
    }
}

function escapeHtml(value) {

    return String(value ?? "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

function displayValue(value) {

    if (value === null || value === undefined || value === "") {
        return "Not provided";
    }

    return escapeHtml(value);
}

function renderSubmittedBadge(label, verified) {

    return `
        <span class="submitted-badge ${verified ? "ok" : ""}">
            ${verified ? "Verified" : "Pending"} ${escapeHtml(label)}
        </span>
    `;
}

function renderSubmittedSkills(skills) {

    const list = (skills || "")
        .split(",")
        .map(skill => skill.trim())
        .filter(Boolean);

    if (!list.length) {
        return `<span class="submitted-profile-value">Not provided</span>`;
    }

    return list.map(skill => `
        <span class="submitted-skill">${escapeHtml(skill)}</span>
    `).join("");
}

function renderSubmittedProfile(candidate) {

    // Existing jobseeker profile: show a read-only summary instead of the create form.
    submittedCandidate = candidate;
    setJobseekerHeader(true);

    const profile =
        document.getElementById("jobseeker-submitted-profile");

    if (!profile) {
        return;
    }

    const type =
        candidate.candidateType === "experienced"
            ? "Experienced"
            : "Fresher";

    const resumeAction =
        candidate.resumePath
            ? `<button type="button" class="btn-secondary" onclick="viewResume('${escapeHtml(candidate.resumePath)}')">View Resume</button>`
            : "";

    profile.innerHTML = `
        <div class="submitted-profile-header">
            <div>
                <h3>Profile Already Submitted</h3>
                <p>Your jobseeker profile is ready and visible to matching employers.</p>
            </div>
            <span class="submitted-profile-type">${type}</span>
        </div>

        <div class="submitted-profile-body">
            <div class="submitted-profile-item">
                <span class="submitted-profile-label">Full Name</span>
                <span class="submitted-profile-value">${displayValue(candidate.fullName)}</span>
            </div>
            <div class="submitted-profile-item">
                <span class="submitted-profile-label">Email</span>
                <span class="submitted-profile-value">${displayValue(candidate.email)}</span>
            </div>
            <div class="submitted-profile-item">
                <span class="submitted-profile-label">Phone</span>
                <span class="submitted-profile-value">${displayValue(candidate.phone)}</span>
            </div>
            <div class="submitted-profile-item">
                <span class="submitted-profile-label">DOB</span>
                <span class="submitted-profile-value">${displayValue(formatDateInput(candidate.dob))}</span>
            </div>
            <div class="submitted-profile-item">
                <span class="submitted-profile-label">Experience</span>
                <span class="submitted-profile-value">${displayValue(candidate.experience)} year(s)</span>
            </div>
            <div class="submitted-profile-item">
                <span class="submitted-profile-label">Expected Salary</span>
                <span class="submitted-profile-value">${displayValue(candidate.salary)}</span>
            </div>
            <div class="submitted-profile-item">
                <span class="submitted-profile-label">Location</span>
                <span class="submitted-profile-value">${displayValue(candidate.location)}</span>
            </div>
            <div class="submitted-profile-item">
                <span class="submitted-profile-label">Company</span>
                <span class="submitted-profile-value">${escapeHtml(renderEmploymentHistoryText(candidate.employmentHistory))}</span>
            </div>
            <div class="submitted-profile-item full">
                <span class="submitted-profile-label">Skills</span>
                <div class="submitted-profile-skills">${renderSubmittedSkills(candidate.skills)}</div>
            </div>
            <div class="submitted-profile-item full">
                <span class="submitted-profile-label">Verification</span>
                <div class="submitted-profile-badges">
                    ${renderSubmittedBadge("Aadhaar", candidate.aadhaarVerified)}
                    ${renderSubmittedBadge("PAN", candidate.panVerified)}
                    ${renderSubmittedBadge("UAN", candidate.uanVerified)}
                </div>
            </div>
        </div>

        <div class="submitted-profile-actions">
            ${resumeAction}
            <button type="button" class="btn-secondary" onclick="editSubmittedProfile()">Update Profile</button>
        </div>
    `;

    profile.classList.remove("hidden");
    document.getElementById("jobseeker-type-choice")
        ?.classList.add("hidden");
    document.getElementById("jobseeker-tab-switch")
        ?.classList.add("hidden");
    document.getElementById("jobseeker-form-card")
        ?.classList.add("hidden");
}

function fillCandidateForm(candidate) {

    // "Update Profile" reopens the matching form with saved values prefilled.
    const type =
        candidate.candidateType === "experienced"
            ? "experienced"
            : "fresher";

    const prefix =
        type === "experienced"
            ? "e"
            : "f";

    openJobseekerForm(type);

    setFieldValue(`${prefix}-name`, candidate.fullName || "");
    setFieldValue(`${prefix}-email`, candidate.email || "");
    setFieldValue(`${prefix}-phone`, candidate.phone || "");
    setFieldValue(`${prefix}-dob`, formatDateInput(candidate.dob));
    setFieldValue(`${prefix}-pan`, candidate.panNumber || "");
    setFieldValue(`${prefix}-aadhaar`, candidate.aadhaarNumber || "");
    setFieldValue(`${prefix}-pref-loc`, candidate.location || "");
    setSelectValue(`${prefix}-salary`, candidate.salary || "");
    setSkills(type, candidate.skills);

    if (type === "experienced") {
        setFieldValue("e-uan", candidate.uanNumber || "");
        setSelectValue("e-exp", candidate.experience);
        setFieldValue("e-company", candidate.employmentHistory || "");
    }
}

function editSubmittedProfile() {

    // Move from summary mode to edit mode for the submitted candidate.
    if (!submittedCandidate) {
        return;
    }

    document.getElementById("jobseeker-submitted-profile")
        ?.classList.add("hidden");

    fillCandidateForm(submittedCandidate);
    loadVerificationStatus();
}

function removeSkill(btn, val, type) {

    Skills[type] = Skills[type].filter(s => s !== val);

    btn.parentElement.remove();
}

/* SEARCH */
function handleSearch() {

    const keyword =
        document.getElementById(
            "searchJob"
        ).value.toLowerCase();

    const location =
        document.getElementById(
            "searchLoc"
        ).value.toLowerCase();

    const filtered =
        allJobs.filter(job => {

            const matchesKeyword =

                job.title.toLowerCase()
                    .includes(keyword)

                ||

                job.skills.toLowerCase()
                    .includes(keyword)

                ||

                job.companyName.toLowerCase()
                    .includes(keyword);

            const matchesLocation =

                job.location.toLowerCase()
                    .includes(location);

            return matchesKeyword &&
                matchesLocation;
        });

    renderJobs(filtered);

    showToast(
        `${filtered.length} jobs found`,
        "success"
    );
}

/* CATEGORY */
function filterCategory(button, category) {

    document.querySelectorAll(
        ".cat-pill"
    ).forEach(btn => {

        btn.classList.remove(
            "active"
        );
    });

    button.classList.add("active");

    if (category === "all") {

        renderJobs(allJobs);

        return;
    }

    const filtered =
        allJobs.filter(job =>

            job.skills
                .toLowerCase()
                .includes(category)

        );

    renderJobs(filtered);
}

/* TAB SWITCH */
let activeTab = 'fresher';
let loadedCandidateType = null;

function switchTab(type) {

    activeTab = type;

    document.getElementById('form-fresher')
        .classList.toggle('hidden', type !== 'fresher');

    document.getElementById('form-experienced')
        .classList.toggle('hidden', type !== 'experienced');

    document.getElementById('tab-fresher')
        .classList.toggle('active', type === 'fresher');

    document.getElementById('tab-experienced')
        .classList.toggle('active', type === 'experienced');

    clearInactiveCandidateVerification(type);
}

function showJobseekerTypeChooser() {

    resetJobseekerForms(true);
    setJobseekerHeader(false);

    document.getElementById("jobseeker-submitted-profile")
        ?.classList.add("hidden");

    document.getElementById("jobseeker-type-choice")
        ?.classList.remove("hidden");

    document.getElementById("jobseeker-tab-switch")
        ?.classList.add("hidden");

    document.getElementById("jobseeker-form-card")
        ?.classList.add("hidden");

    activeTab = "fresher";

    switchTab("fresher");
}

function openJobseekerForm(type) {

    setJobseekerHeader(false);

    document.getElementById("jobseeker-submitted-profile")
        ?.classList.add("hidden");

    if (!loadedCandidateType) {
        const prefix =
            type === "experienced"
                ? "e"
                : "f";

        clearCandidateForm(prefix);
        prefillLoggedInIdentity(prefix);
    }

    document.getElementById("jobseeker-type-choice")
        ?.classList.add("hidden");

    document.getElementById("jobseeker-tab-switch")
        ?.classList.remove("hidden");

    document.getElementById("jobseeker-form-card")
        ?.classList.remove("hidden");

    switchTab(type);

    loadVerificationStatus();

    window.scrollTo({
        top: 0,
        behavior: "smooth"
    });
}

/* VALIDATION */
function validateEmail(email) {

    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function validatePhone(phone) {

    return /^[6-9]\d{9}$/.test(phone);
}

function validateUAN(uan) {

    return /^\d{12}$/.test(uan);
}

function validatePAN(pan) {

    return /^[A-Z]{5}[0-9]{4}[A-Z]$/.test(pan);
}

function validateAadhaar(aadhaar) {

    return /^\d{12}$/.test(aadhaar);
}

/* SUBMIT JOBSEEKER */
function submitJobseeker() {

    // One submit path handles both fresher and experienced forms.
    const isFresher = activeTab === 'fresher';

    const p = isFresher ? 'f' : 'e';

    let name = "";

    if (isFresher) {

        name = document.getElementById("f-name")?.value.trim() || "";

    } else {

        name = name = document.getElementById("e-name")?.value.trim() || "";
    }

    let email = "";
    let phone = "";
    let uan = "";

    if (isFresher) {

        email = document.getElementById("f-email")?.value.trim() ||
            getLoggedInEmail();

        phone = phone = document.getElementById("f-phone")?.value.trim() || "";


    } else {

        email = document.getElementById("e-email")?.value.trim() ||
            getLoggedInEmail();

        phone = document.getElementById("e-phone")?.value.trim() || ""

        uan = document.getElementById("e-uan")?.value.trim() || ""
    }

    const salary =
        document.getElementById(`${p}-salary`)?.value;

    const skills =
        Skills[isFresher ? 'fresher' : 'experienced'];

    if (!name) {
        showToast("Enter name", "error");
        return;
    }

    if (!validateEmail(email)) {
        showToast("Invalid email", "error");
        return;
    }

    if (!validatePhone(phone)) {
        showToast("Invalid phone", "error");
        return;
    }

    if (!isFresher && !validateUAN(uan)) {

        showToast("Invalid UAN", "error");

        return;
    }

    const dob =
        document.getElementById(`${p}-dob`)?.value || "";

    const aadhaarNumber =
        document.getElementById(`${p}-aadhaar`)?.value || "";

    const panNumber =
        document.getElementById(`${p}-pan`)?.value || "";

    if (!validateAadhaar(aadhaarNumber)) {
        showToast("Invalid Aadhaar", "error");
        return;
    }

    if (!validatePAN(panNumber.toUpperCase())) {
        showToast("Invalid PAN", "error");
        return;
    }

    const experience =
        isFresher
            ? 0
            : parseInt(document.getElementById("e-exp")?.value, 10) || 0;

    const location =
        document.getElementById(`${p}-pref-loc`)?.value.trim() || "Open";

    const employmentHistory =
        isFresher
            ? ""
            : document.getElementById("e-company")?.value.trim() || "";

    const newCandidate = {

        // Backend uses email to insert a new candidate or update the existing one.
        fullName: name,

        email: email,

        phone: phone,

        uanNumber: isFresher ? null : uan,

        dob: dob,

        aadhaarNumber: aadhaarNumber,

        panNumber: panNumber.toUpperCase(),

        employmentHistory: employmentHistory,

        experience: experience,

        skills: skills.join(","),

        location: location,

        salary: salary,

        candidateType: isFresher ? "fresher" : "experienced",
    };

    fetch(`${API_BASE_URL}/Candidates`, {

method: 'POST',

        headers: {
            "Content-Type": "application/json",
            "Authorization": "Bearer " + authStorage.getItem("token")
        },

body: JSON.stringify(newCandidate)

    })
        .then(async response => {

            if (!response.ok) {

                const text = await response.text();

                throw new Error(text || "API Failed");
            }

            return response.json();
        })

    .then(async data => {

        console.log(data);

        const resume =
            await uploadResume(data.id, { silentIfMissing: true });

        if (resume?.path) {
            data.resumePath = resume.path;
        }

        showModal(
            "🎉 Success",
            "Profile submitted successfully!"
        );

        renderSubmittedProfile(data);
    })

    .catch(error => {

        console.error(error);

        showToast("API Error", "error");
    });
}

/* EMPLOYER */
async function findCandidates() {

    try {

        // Employer enters requirements here; matches are built from Candidate rows.
        // Employer form values
        const companyName =
            document.getElementById('emp-company')?.value.trim();

        const title =
            document.getElementById('emp-jobtitle')?.value.trim();

        

        const salary =
            document.getElementById('emp-salary')?.value;

        const description =
            document.getElementById('emp-desc')?.value.trim();

        const candidateType =
            document.getElementById('emp-type')?.value;

        const minExperience =
            parseInt(document.getElementById('emp-exp')?.value) || 0;

        const skills =
            Skills.employer.join(",");

        // Validation
        if (!companyName || !title || !skills) {

            showToast(
                "Please fill company, title and skills",
                "error"
            );

            return;
        }

        // Create the job row first, then show matching candidates below the form.
        const newJob = {

            companyName,

            title,

            skills,

            location,

            salary,

            description,

            candidateType,

            minExperience
        };

        // Save Job to Database
        await fetch(`${API_BASE_URL}/Jobs`, {

method: "POST",

    headers: {
    "Content-Type": "application/json"
},

body: JSON.stringify(newJob)
        });

// Fetch Candidates
const response =
    await fetch(`${API_BASE_URL}/Candidates`);

const candidates =
    await response.json();

// Match candidates against every selected skill chip, not only the first chip.
const requiredSkills =
    Skills.employer
        .map(skill => skill.trim().toLowerCase())
        .filter(Boolean);

const filtered = candidates.filter(c => {

    const candidateSkills =
        (c.skills || "")
            .split(",")
            .map(skill => skill.trim().toLowerCase())
            .filter(Boolean);

    return requiredSkills.some(requiredSkill =>
        candidateSkills.some(candidateSkill =>
            candidateSkill.includes(requiredSkill) ||
            requiredSkill.includes(candidateSkill)
        )
    );
});

// Render Candidates
renderCandidates(filtered);

showToast(
    `${filtered.length} candidates matched`,
    "success"
);


    } catch (error) {

    console.error(error);

    showToast(
        "Failed to load candidates",
        "error"
    );
}
}

function renderCandidates(list) {

    // Employer matching result card. Verification and resume values come from Candidate rows.
    const container =
        document.getElementById('candidatesList');

    if (!container) return;

    if (!list.length) {

        container.innerHTML = `
            <p>No matching candidates found</p>
        `;

        return;
    }

    container.innerHTML = list.map(c => `

        <div class="candidate-card">

            <div class="candidate-name">
                ${c.fullName}
            </div>

            <div>
                ${c.skills}
            </div>

            <div>
                ${c.email}
            </div>

            <div class="verification-cards">

    <span class="verification-pill">

        ${c.aadhaarVerified
            ? "✅ Aadhaar Verified"
            : "❌ Aadhaar Pending"}

    </span>

    <span class="verification-pill">

        ${c.panVerified
            ? "✅ PAN Verified"
            : "❌ PAN Pending"}

    </span>

    <span class="verification-pill">

        ${c.uanVerified
            ? "✅ UAN Verified"
            : "❌ UAN Pending"}

    </span>

   <div class="resume-actions">

    <span class="verification-pill">

        ${c.resumePath
            ? "📄 Resume Uploaded"
            : "❌ No Resume"}

    </span>

    ${c.resumePath

            ?

            `
        <button class="resume-btn"
            onclick="viewResume('${c.resumePath}')">

            View Resume

        </button>

        <button class="resume-btn"
            onclick="downloadResume('${c.resumePath}')">

            Download

        </button>
        `

            :

            ""
    }

</div>

</div>

            <button class="btn-primary"
                onclick='viewCandidateProfile(${JSON.stringify(JSON.stringify(c))})'>

                View Profile

            </button>
            <button class="btn-primary"
    onclick="viewVerificationDetails(${c.id})">

    View Details

</button>

        </div>

    `).join('');
}
function viewCandidateProfile(candidateData) {

    const c = JSON.parse(candidateData);

    document.getElementById(
        "candidateProfileModal"
    ).style.display = "flex";

    document.getElementById(
        "candidateProfileContent"
    ).innerHTML = `

        <div class="profile-info">
            <strong>Name:</strong>
            ${c.fullName}
        </div>

        <div class="profile-info">
            <strong>Email:</strong>
            ${c.email}
        </div>

        <div class="profile-info">
            <strong>Phone:</strong>
            ${c.phone}
        </div>

        <div class="profile-info">
            <strong>Skills:</strong>
            ${c.skills}
        </div>

        <div class="profile-info">
            <strong>Experience:</strong>
            ${c.experience} years
        </div>

        <div class="profile-info">
            <strong>Location:</strong>
            ${c.location}
        </div>

        <div class="profile-info">
            <strong>Salary:</strong>
            ${c.salary}
        </div>

        <div class="profile-info">
            <strong>Type:</strong>
            ${c.candidateType}
        </div>
    `;
}

function closeCandidateProfile() {

    document.getElementById(
        "candidateProfileModal"
    ).style.display = "none";
}


/* LOGIN */
async function handleLogin() {

    try {

        const email =
            document.getElementById('login-email')?.value.trim();

        const password =
            document.getElementById('login-pass')?.value.trim();

        if (!email || !password) {

            showToast(
                "Please enter email and password",
                "error"
            );

            return;
        }

        const response = await fetch(

            `${API_BASE_URL}/Auth/login`,

            {
                method: "POST",

                headers: {
                    "Content-Type": "application/json"
                },

                body: JSON.stringify({
                    email,
                    password
                })
            }
        );

        if (!response.ok) {

            throw new Error("Invalid login");
        }

        const data = await response.json();

        // Store JWT and display details for navbar, role routing, and API calls.
        authStorage.setItem(
            "token",
            data.token
        );

        authStorage.setItem(
            "role",
            data.role
        );

        authStorage.setItem(
            "name",
            data.name
        );
        authStorage.setItem(
            "email",
            data.email || getLoggedInEmail()
        );
        showToast(
            "Login successful",
            "success"
        );
        updateNavbar();

        // Clear login inputs so credentials are not left in the form
        const le = document.getElementById('login-email'); if (le) le.value = '';
        const lp = document.getElementById('login-pass'); if (lp) lp.value = '';

        //Hide login/register pages and show dashboard based on role
        document.getElementById("loginBtn").style.display = "none";
        document.getElementById("registerBtn").style.display = "none";

        //show Logout Area
        document.getElementById("user-area").style.display = "flex";

        // Redirect based on role
        if (data.role === "admin") {

    showPage("admin");

    loadAdminDashboard();

} else if (data.role === "employer") {

    showPage("employer");

    loadEmployerDashboard();

} else {

    showPage("jobseeker");

    // Show chooser first so the page is not blank while profile lookup runs.
    showJobseekerTypeChooser();

    loadCandidateProfile();

    // loadAppliedJobs();
}

    } catch (error) {

        console.error(error);

        showToast(
            "Invalid email or password",
            "error"
        );
    }
}
/* LOGOUT */
document.getElementById("logout-btn").addEventListener("click",function logout() {
    clearSavedLogin();

    // Also clear any lingering form values
    // const rn = document.getElementById('register-name'); if (rn) rn.value = '';
    // const re = document.getElementById('register-email'); if (re) re.value = '';
    // const rp = document.getElementById('register-pass'); if (rp) rp.value = '';
    // const rr = document.getElementById('register-role'); if (rr) rr.value = '';
    // const le = document.getElementById('login-email'); if (le) le.value = '';
    // const lp = document.getElementById('login-pass'); if (lp) lp.value = '';
    //show Lohin/Register again
    document.getElementById("loginBtn").style.display = "inline-block";
    document.getElementById("registerBtn").style.display = "inline-block";

    //hide logout area
    document.getElementById("user-area").style.display = "none";


    updateNavbar();

    showToast(
        "Logged out successfully",
        "success"
    );

    showPage("home");
});
function updateNavbar() {

    const token =
        authStorage.getItem("token");

    const role =
        authStorage.getItem("role");

    const name =
        authStorage.getItem("name");

    const userArea =
        document.getElementById("user-area");

    const welcomeUser =
        document.getElementById("welcome-user");

    const loginBtn =
        document.getElementById("loginBtn");

    const registerBtn =
        document.getElementById("registerBtn");

    if (token) {

        if (loginBtn) loginBtn.style.display = "none";

        if (registerBtn) registerBtn.style.display = "none";

        if (userArea) userArea.style.display = "flex";

        if (welcomeUser) welcomeUser.innerText =
            `${name} (${role})`;

    } else {

        if (loginBtn) loginBtn.style.display = "inline-block";

        if (registerBtn) registerBtn.style.display = "inline-block";

        if (userArea) userArea.style.display = "none";

        if (welcomeUser) welcomeUser.innerText = "";
    }
}
async function loadCandidateProfile() {

    try {

        // On jobseeker login:
        // 200 shows submitted profile summary, 404 shows fresher/experienced chooser.
        const token =
            authStorage.getItem("token");

        if (!token) {
            showJobseekerTypeChooser();
            return;
        }

        const response =
            await fetch(

                `${API_BASE_URL}/Candidates/my-profile`,

                {
                    headers: {
                        Authorization:
                            `Bearer ${token}`
                    }
                });

        if (response.status === 404) {
            // No submitted profile yet.
            showJobseekerTypeChooser();
            return;
        }

        if (!response.ok) {
            throw new Error("Unable to load candidate profile");
        }

        const candidate =
            await response.json();

        if (!candidate) return;

        loadedCandidateType =
            candidate.candidateType || null;

        // Existing profile: show read-only summary instead of opening the form.
        renderSubmittedProfile(candidate);

    } catch (error) {

        console.error(error);
        showJobseekerTypeChooser();
    }
}
// async function loadAppliedJobs() {

//     try {

//         const token =
//             localStorage.getItem("token");

//         const email =
//             JSON.parse(
//                 atob(token.split('.')[1])
//             ).email;

//         const response =
//             await fetch(
//                 `${API_BASE_URL}/Applications`,
// {
//     headers: {
//         Authorization:
//         `Bearer ${token}`
//     }
// });

// const applications =
//     await response.json();

// const mine =
//     applications.filter(
//         a =>
//             a.candidateEmail === email
//     );

// if (!mine.length) {

//     document.getElementById(
//         "applied-jobs"
//     ).innerHTML =
//         "No applications yet";

//     return;
// }

// document.getElementById(
//     "applied-jobs"
// ).innerHTML = mine.map(a => `

//             <div class="candidate-card">

//                 <strong>
//                     ${a.jobTitle}
//                 </strong>

//                 <p>
//                     Applied:
//                     ${new Date(a.appliedAt)
//         .toLocaleDateString()}
//                 </p>

//             </div>

//         `).join('');

//     } catch (error) {

//     console.error(error);
// }
// }
function getSelectedResumeFile() {

    const isFresher = activeTab === "fresher";

    return isFresher
        ? document.getElementById("f-resume")?.files[0]
        : document.getElementById("resume-file")?.files[0];
}

async function getCurrentCandidateId(token) {

    const response =
        await fetch(
            `${API_BASE_URL}/Candidates/my-profile`,
            {
                headers: {
                    Authorization:
                        `Bearer ${token}`
                }
            });

    if (!response.ok) {
        return null;
    }

    const candidate =
        await response.json();

    return candidate?.id || null;
}

async function uploadResume(candidateId = null, options = {}) {

    // Used by the upload button and silently after profile submit.
    const silentIfMissing =
        options.silentIfMissing === true;

    try {

        const token =
            authStorage.getItem("token");

        const file =
            getSelectedResumeFile();

        if (!file) {

            if (!silentIfMissing) {
                showToast(
                    "Please select resume",
                    "error"
                );
            }

            return null;
        }

        const resolvedCandidateId =
            candidateId ||
            await getCurrentCandidateId(token);

        if (!resolvedCandidateId) {

            showToast(
                "Candidate not found",
                "error"
            );

            return null;
        }

        const formData =
            new FormData();

        formData.append(
            "file",
            file
        );

        const uploadResponse =
            await fetch(
                `${API_BASE_URL}/Candidates/upload-resume/${resolvedCandidateId}`,
                {
                    method: "POST",
                    headers: {
                        Authorization:
                            `Bearer ${token}`
                    },
                    body: formData
                });

        let data = null;

        try {
            data = await uploadResponse.json();
        } catch (error) {
            data = null;
        }

        if (!uploadResponse.ok) {
            throw new Error(
                data?.message ||
                data?.error ||
                "Upload failed"
            );
        }

        const resumeStatus =
            document.getElementById("resume-status");

        if (resumeStatus) {
            resumeStatus.innerHTML = `
                Resume Uploaded:
                <a href="${data.url || API_BASE_URL.replace('/api', '') + data.path}"
                    target="_blank">
                    View Resume
                </a>
            `;
        }

        if (!silentIfMissing) {
            showToast(
                "Resume uploaded successfully",
                "success"
            );
        }

        return data;

    } catch (error) {

        console.error(error);

        showToast(
            error.message || "Upload failed",
            "error"
        );

        return null;
    }
}
async function loadEmployerDashboard() {

    try {

        const token =
            authStorage.getItem("token");

        const response =
            await fetch(

                `${API_BASE_URL}/Jobs/dashboard`,

                {
                    headers: {
                        Authorization:
                            `Bearer ${token}`
                    }
                });

        const dashboard =
            await response.json();

        // JOBS
        document.getElementById(
            "employer-jobs"
        ).innerHTML = dashboard.map(job => `

    <div class="candidate-card">

                <strong>
                    ${job.title}
                </strong>

                <p>
                    ${job.companyName}
                </p>

                <p>
                    ${job.location}
                </p>

                <p>
                    Applicants:
                    ${job.applicants.length}
                </p>

            </div>

    `).join('');

        // APPLICANTS
        let applicantsHtml = "";

        dashboard.forEach(job => {

            job.applicants.forEach(a => {

                applicantsHtml += `

    <div class="candidate-card">

                        <strong>
                            ${a.candidateName}
                        </strong>

                        <p>
                            ${a.candidateEmail}
                        </p>

                        <p>
                            Applied:
                            ${new Date(a.appliedAt)
                                .toLocaleDateString()}
                        </p>

                        ${
    a.resume
    ? `
                            <a
                                href="${a.resume}"
                                target="_blank">

                                Download Resume

                            </a>
                            `
    : "No Resume"
}

                    </div>
    `;
            });
        });

        document.getElementById(
            "employer-applicants"
        ).innerHTML = applicantsHtml ||

            "No applicants yet";

    } catch (error) {

        console.error(error);

        showToast(
            "Failed to load employer dashboard",
            "error"
        );
    }
}
async function loadAdminDashboard() {

    try {

        const token =
            authStorage.getItem("token");

        // DASHBOARD
        const dashboardResponse =
            await fetch(

                `${API_BASE_URL}/Admin/dashboard`,

                {
                    headers: {
                        Authorization:
                            `Bearer ${token}`
                    }
                });

        const dashboard =
            await dashboardResponse.json();

        document.getElementById(
            "admin-users-count"
        ).innerText =
            dashboard.totalUsers;

        document.getElementById(
            "admin-jobs-count"
        ).innerText =
            dashboard.totalJobs;

        document.getElementById(
            "admin-applications-count"
        ).innerText =
            dashboard.totalApplications;

        document.getElementById(
            "admin-candidates-count"
        ).innerText =
            dashboard.totalCandidates;

        // USERS
        const usersResponse =
            await fetch(

                `${API_BASE_URL}/Admin/users`,

                {
                    headers: {
                        Authorization:
                            `Bearer ${token}`
                    }
                });

        const users =
            await usersResponse.json();

        document.getElementById(
            "admin-users-list"
        ).innerHTML = users.map(u => `

    <div class="candidate-card">

                <strong>
                    ${u.fullName}
                </strong>

                <p>
                    ${u.email}
                </p>

                <p>
                    ${u.role}
                </p>

            </div>

    `).join('');

        // JOBS
        const jobsResponse =
            await fetch(

                `${API_BASE_URL}/Admin/jobs`,

                {
                    headers: {
                        Authorization:
                            `Bearer ${token}`
                    }
                });

        const jobs =
            await jobsResponse.json();

        document.getElementById(
            "admin-jobs-list"
        ).innerHTML = jobs.map(j => `

    <div class="candidate-card">

                <strong>
                    ${j.title}
                </strong>

                <p>
                    ${j.companyName}
                </p>

                <p>
                    ${j.location}
                </p>

            </div>

    `).join('');

        // APPLICATIONS
        const applicationsResponse =
            await fetch(

                `${API_BASE_URL}/Admin/applications`,

                {
                    headers: {
                        Authorization:
                            `Bearer ${token}`
                    }
                });

        const applications =
            await applicationsResponse.json();

        document.getElementById(
            "admin-applications-list"
        ).innerHTML = applications.map(a => `

    <div class="candidate-card">

                <strong>
                    ${a.candidateName}
                </strong>

                <p>
                    ${a.candidateEmail}
                </p>

                <p>
                    ${a.jobTitle}
                </p>

            </div>

    `).join('');

    } catch (error) {

        console.error(error);

        showToast(
            "Failed to load admin dashboard",
            "error"
        );
    }
}
// async function loadHomepageJobs() {

//     try {

//         const response =
//             await fetch(
//                 `${API_BASE_URL}/Jobs`
//             );

// const jobs =
//     await response.json();
// allJobs = jobs;
// renderJobs(jobs);

//     } catch (error) {

//     console.error(error);

//     showToast(
//         "Failed to load jobs",
//         "error"
//     );
// }
// }
function renderJobs(jobs) {

    const container =
        document.getElementById(
            "jobsGrid"
        );

    if (!jobs.length) {

        container.innerHTML = `

    <div class="empty-state">

                <div class="empty-icon">
                    🔍
                </div>

                <p>
                    No matching jobs found
                </p>

            </div>
    `;

        return;
    }

    container.innerHTML = jobs.map(job => `

    <div class="job-card">

            <h3>
                ${job.title}
            </h3>

            <p>
                <strong>Company:</strong>
                ${job.companyName}
            </p>

            <p>
                <strong>Location:</strong>
                ${job.location}
            </p>

            <p>
                <strong>Salary:</strong>
                ${job.salary}
            </p>

            <p>
                <strong>Skills:</strong>
                ${job.skills}
            </p>

            <button
                class="btn-primary"
                onclick="applyJob(${job.id},
                '${job.title}')">

                Apply Now

            </button>

        </div>

    `).join('');
}
async function applyJob(jobId, jobTitle) {

    try {

        const token =
            authStorage.getItem("token");

        if (!token) {

            showToast(
                "Please login first",
                "error"
            );

            showPage("login");

            return;
        }

        const payload =
            JSON.parse(
                atob(token.split('.')[1])
            );

        const application = {

            jobId,

            jobTitle,

            candidateName:
                authStorage.getItem("name"),

            candidateEmail:
                payload.email
        };

        const response =
            await fetch(
                `${API_BASE_URL}/Applications`,
{
    method: "POST",

        headers: {

        "Content-Type":
        "application/json",

            Authorization:
        `Bearer ${token}`
    },

    body:
    JSON.stringify(application)
});

        if (!response.ok) {
            const message = await response.text();
            throw new Error(message);
        }

        showToast('Applied successfully', 'success');

    } catch (error) {

        console.error(error);

        showToast(
            error.message || 'Failed to apply',
            'error'
        );
    }
}
async function submitProfile(event) {
    event.preventDefault();

    const data = {
        name: document.getElementById('name').value,
        email: document.getElementById('email').value,
        phone: document.getElementById('phone').value,
        AadhaarNumber: document.getElementById('aadhaar').value,
        Dob: document.getElementById('dob').value,
        PanNumber: document.getElementById('pan').value
    };

    const res = await fetch('/api/candidates/submit-profile', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
    });

    let json = null;
    try {
        json = await res.json();
    } catch (e) {
        // not JSON or empty
    }

    if (res.ok) {
        alert('Profile submitted');
    } else {
        const message = (json && (json.detail || json.message || json.error)) || res.statusText || 'Unknown error';
        alert('Submit failed: ' + message);
    }
}

// wire up form
const form = document.getElementById('profileForm');
if (form) form.addEventListener('submit', submitProfile);
/* INIT */
document.addEventListener('DOMContentLoaded', () => {

    renderAllVerificationFields();
    clearLegacyPersistentLogin();

    updateNavbar();
    // loadHomepageJobs();
        
    const token = authStorage.getItem("token");

    const role = authStorage.getItem("role");

    if (token && role) {

        showToast(
            `Welcome back ${authStorage.getItem("name")} `,
            "success"
        );

        updateNavbar();

        if (role === "admin") {

            showPage("admin");

            loadAdminDashboard();

        } else if (role === "employer") {

            showPage("employer");

            loadEmployerDashboard();

        } else {

            showPage("jobseeker");

            loadCandidateProfile();

            // loadAppliedJobs();
        }
    }
});
function normalizeEmploymentHistory(history) {

    if (!history) {
        return [];
    }

    if (Array.isArray(history)) {
        return history;
    }

    if (typeof history === "string") {
        const trimmed = history.trim();

        if (!trimmed) {
            return [];
        }

        try {
            const parsed = JSON.parse(trimmed);

            if (Array.isArray(parsed)) {
                return parsed;
            }
        } catch {
            return trimmed
                .split(",")
                .map(company => ({
                    company: company.trim(),
                    doj: "",
                    doe: ""
                }))
                .filter(job => job.company);
        }
    }

    return [];
}

function renderEmploymentHistory(history) {

    const jobs = normalizeEmploymentHistory(history);

    if (!jobs.length) {
        return `
            <div class="employment-empty">
                No employment history found
            </div>
        `;
    }

    return jobs
        .map(job => {
            const company = job.company || job.Company || "Unknown Company";
            const doj = job.doj || job.DOJ || "";
            const doe = job.doe || job.DOE || "";

            return `
                <div class="employment-card">
                    <strong>${escapeHtml(company)}</strong>
                    <span>DOJ: ${escapeHtml(doj || "N/A")}</span>
                    <span>DOE: ${escapeHtml(doe || "N/A")}</span>
                </div>
            `;
        })
        .join("");
}

function renderEmploymentHistoryText(history) {

    const jobs = normalizeEmploymentHistory(history);

    if (!jobs.length) {
        return "Not provided";
    }

    return jobs
        .map(job => job.company || job.Company || "Unknown Company")
        .join(", ");
}

function showEmploymentHistoryModal(data) {

    const modal = document.getElementById("verificationModal");
    const content = document.getElementById("verificationContent");

    if (!modal || !content) {
        return;
    }

    modal.style.display = "flex";
    content.innerHTML = `
        <div class="profile-info">
            <strong>UAN Number:</strong>
            ${escapeHtml(data.uanNumber || data.uan || "N/A")}
        </div>
        <div class="profile-info">
            <strong>Status:</strong>
            <span class="verified-badge">Verified</span>
        </div>
        <div class="profile-info">
            <strong>Employment History:</strong>
            <div class="employment-list">
                ${renderEmploymentHistory(data.employmentHistory)}
            </div>
        </div>
    `;
}

async function viewVerificationDetails(id) {

    const response = await fetch(
        `${API_BASE_URL}/Candidates/${id}`
    );

    const data = await response.json();

    document.getElementById(
        "verificationModal"
    ).style.display = "flex";

    document.getElementById(
        "verificationContent"
    ).innerHTML = `

        <div class="profile-info">

            <strong>Full Name:</strong>

            ${data.fullName}

        </div>

        <div class="profile-info">

            <strong>DOB:</strong>

            ${formatDateInput(data.dob) || "N/A"}

        </div>

        <div class="profile-info">

            <strong>PAN Number:</strong>

            ${data.panNumber || "N/A"}

        </div>

        <div class="profile-info">

            <strong>Aadhaar Number:</strong>

            ${data.aadhaarNumber || "N/A"}

        </div>

        <div class="profile-info">

            <strong>UAN Number:</strong>

            ${data.uanNumber || "N/A"}

        </div>

        <div class="profile-info">

            <strong>Status:</strong>

            <span class="verified-badge">

    ✔ Verified

</span>

        </div>

        <div class="profile-info">

    <strong>Employment History:</strong>

    <div class="employment-list">
        ${renderEmploymentHistory(data.employmentHistory)}
    </div>

</div>
    `;
}
function closeVerificationModal() {

    document.getElementById(
        "verificationModal"
    ).style.display = "none";
}
function viewResume(path) {

    window.open(getResumeUrl(path), "_blank");
}

function downloadResume(path) {

    const link =
        document.createElement("a");

    link.href = getResumeUrl(path);

    link.download = "Resume.pdf";

    document.body.appendChild(link);

    link.click();

    document.body.removeChild(link);
}

function getResumeUrl(path) {

    if (!path) {
        return "";
    }

    if (path.startsWith("http")) {
        return path;
    }

    return `${API_BASE_URL.replace("/api", "")}${path}`;
}
async function legacyLoadVerificationStatus() {

    const response = await fetch(
        `${API_BASE_URL}/Verification/candidate-verification`
    );

    const data = await response.json();

    // Aadhaar
    if (data.aadhaarVerified) {

        document.getElementById(
            "aadhaar-status"
        ).innerHTML =
            "✅ Verified";

        document.getElementById(
            "aadhaar-number"
        ).value =
            data.aadhaarNumber;

        const btn =
            document.getElementById(
                "aadhaar-btn"
            );

        btn.disabled = true;

        btn.innerHTML =
            "Verified";
    }

    // PAN
    if (data.panVerified) {

        document.getElementById(
            "pan-status"
        ).innerHTML =
            "✅ Verified";

        document.getElementById(
            "pan-number"
        ).value =
            data.panNumber;

        const btn =
            document.getElementById(
                "pan-btn"
            );

        btn.disabled = true;

        btn.innerHTML =
            "Verified";
    }

    // UAN
    if (data.uanVerified) {

        document.getElementById(
            "uan-status"
        ).innerHTML =
            "✅ Verified";

        document.getElementById(
            "uan-number"
        ).value =
            data.uanNumber;

        const btn =
            document.getElementById(
                "uan-btn"
            );

        btn.disabled = true;

        btn.innerHTML =
            "Verified";
    }
}

function getVerificationElements(prefix, type) {

    return {
        input: document.getElementById(`${prefix}-${type}`),
        status: document.getElementById(`${prefix}-${type}-status`),
        button: document.getElementById(`${prefix}-${type}-btn`)
    };
}

function renderVerificationFields(type) {

    const prefix =
        type === "experienced"
            ? "e"
            : "f";

    const mount =
        document.getElementById(`${prefix}-verification-fields`);

    if (!mount) {
        return;
    }

    const fields = [
        {
            type: "aadhaar",
            label: "Aadhaar Number",
            placeholder: "123412341234",
            maxLength: 12,
            hint: "",
            action: `verifyAadhaar('${prefix}')`
        },
        {
            type: "pan",
            label: "PAN Number",
            placeholder: "ABCDE1234F",
            maxLength: 10,
            hint: "",
            action: `verifyPan('${prefix}')`
        }
    ];

    if (type === "experienced") {
        fields.push({
            type: "uan",
            label: "UAN",
            placeholder: "123456789012",
            maxLength: 12,
            hint: `<span class="hint">(12-digit)</span>`,
            action: `verifyUan('${prefix}')`
        });
    }

    mount.innerHTML = fields
        .map((field, index) => `
            ${index % 2 === 0 ? `<div class="form-row">` : ""}
                <div class="form-group">
                    <label for="${prefix}-${field.type}">
                        ${field.label} <span class="req">*</span> ${field.hint}
                    </label>
                    <input type="text"
                           id="${prefix}-${field.type}"
                           placeholder="${field.placeholder}"
                           maxlength="${field.maxLength}"
                           autocomplete="off" />
                    <div class="verify-actions">
                        <button type="button"
                                id="${prefix}-${field.type}-btn"
                                class="verify-btn"
                                onclick="${field.action}">
                            Verify ${field.label.replace(" Number", "")}
                        </button>
                        <span class="verify-status"
                              id="${prefix}-${field.type}-status"></span>
                    </div>
                </div>
            ${index % 2 === 1 || index === fields.length - 1 ? `</div>` : ""}
        `)
        .join("");
}

function renderAllVerificationFields() {

    renderVerificationFields("fresher");
    renderVerificationFields("experienced");
}

async function postVerification(path, number) {

    // Shared POST helper for Aadhaar, PAN, and UAN verification calls.
    const token =
        authStorage.getItem("token");

    const response = await fetch(
        `${API_BASE_URL}/Verification/${path}`,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                Authorization: `Bearer ${token}`
            },
            body: JSON.stringify({ number })
        });

    const data = await response.json();

    if (!response.ok) {
        throw new Error(data.message || "Verification failed");
    }

    return data;
}

function completeVerificationUi(elements, message) {

    if (elements.status) {
        elements.status.innerHTML = message || "Verified";
    }

    if (elements.button) {
        elements.button.disabled = true;
        elements.button.innerHTML = "Verified";
    }
}

function resetVerificationUi(prefix, type) {

    const elements =
        getVerificationElements(prefix, type);

    if (elements.input) {
        elements.input.value = "";
    }

    if (elements.status) {
        elements.status.innerHTML = "";
    }

    if (elements.button) {
        elements.button.disabled = false;
        elements.button.innerHTML =
            type === "uan"
                ? "Verify UAN"
                : type === "pan"
                    ? "Verify PAN"
                    : "Verify Aadhaar";
    }
}

function clearIdentificationFields(prefix) {

    ["pan", "aadhaar", "uan"].forEach(type =>
        resetVerificationUi(prefix, type)
    );
}

function clearInactiveCandidateVerification(selectedType) {

    if (!loadedCandidateType ||
        loadedCandidateType === selectedType) {
        return;
    }

    const prefix =
        selectedType === "experienced"
            ? "e"
            : "f";

    clearIdentificationFields(prefix);
}

function handleDigiLockerRedirect(data) {

    if (data.redirectUrl) {
        window.location.href = data.redirectUrl;
        return true;
    }

    return false;
}

async function verifyAadhaar(prefix = activeTab === "fresher" ? "f" : "e") {

    return startVerification("aadhaar", prefix);
}

async function verifyPan(prefix = activeTab === "fresher" ? "f" : "e") {

    return startVerification("pan", prefix);
}

async function verifyUan(prefix = "e") {

    return startVerification("uan", prefix);
}

async function startVerification(type, prefix = activeTab === "fresher" ? "f" : "e") {

    const elements = getVerificationElements(prefix, type);
    const number = elements.input?.value.trim() || "";

    if (type === "aadhaar" && !validateAadhaar(number)) {
        showToast("Enter valid 12-digit Aadhaar", "error");
        return;
    }

    if (type === "pan" && !validatePAN(number.toUpperCase())) {
        showToast("Enter valid PAN", "error");
        return;
    }

    if (type === "uan" && !validateUAN(number)) {
        showToast("Enter valid 12-digit UAN", "error");
        return;
    }

    const normalizedNumber =
        type === "pan"
            ? number.toUpperCase()
            : number;

    if (elements.input) {
        elements.input.value = normalizedNumber;
    }

    try {
        const data =
            await postVerification(
                `verify-${type}`,
                normalizedNumber);

        if (handleDigiLockerRedirect(data)) {
            return;
        }

        completeVerificationUi(elements, "Verified");

        if (type === "uan") {
            showEmploymentHistoryModal(data);
        }

        showToast(data.message || `${type.toUpperCase()} verified`, "success");
    } catch (error) {
        showToast(error.message, "error");
    }
}

async function loadVerificationStatus() {

    // Syncs saved verification status back into the visible profile form.
    const token = authStorage.getItem("token");

    if (!token) {
        return;
    }

    try {
        const response = await fetch(
            `${API_BASE_URL}/Verification/candidate-verification`,
            {
                headers: {
                    Authorization: `Bearer ${token}`
                }
            });

        if (!response.ok) {
            return;
        }

        const data = await response.json();

        loadedCandidateType =
            data.candidateType || null;

        clearIdentificationFields("f");
        clearIdentificationFields("e");

        const prefix =
            data.candidateType === "experienced"
                ? "e"
                : "f";

            const aadhaar = getVerificationElements(prefix, "aadhaar");
            const pan = getVerificationElements(prefix, "pan");
            const uan = getVerificationElements(prefix, "uan");

            if (aadhaar.input && data.aadhaarNumber) {
                aadhaar.input.value = data.aadhaarNumber;
            }

            if (pan.input && data.panNumber) {
                pan.input.value = data.panNumber;
            }

            if (uan.input && data.uanNumber) {
                uan.input.value = data.uanNumber;
            }

            if (data.aadhaarVerified) {
                completeVerificationUi(aadhaar, "Verified");
            }

            if (data.panVerified) {
                completeVerificationUi(pan, "Verified");
            }

            if (data.uanVerified) {
                completeVerificationUi(uan, "Verified");
            }
    } catch (error) {
        console.error(error);
    }
}
