'use strict';

const API_BASE_URL = "https://localhost:7250/api";

const Skills = {
    fresher: [],
    experienced: [],
    employer: []
};
let allJobs = [];
/* PAGE NAVIGATION */
function showPage(pageId) {

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

/* TOAST */
let toastTimer;

function showToast(message, type = 'info') {

    const toast = document.getElementById('toast');

    if (!toast) return;

    toast.className = `toast ${ type } `;

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
        ${ val }
<button type="button"
    onclick="removeSkill(this,'${val}','${type}')">
    ×
</button>
`;

    inputEl.parentElement.insertBefore(chip, inputEl);
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
        `${ filtered.length } jobs found`,
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
}

/* VALIDATION */
function validateEmail(email) {

    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function validatePhone(phone) {

    return /^[6-9]\d{9}$/.test(phone);
}
function validatePAN(pan) {
    return /^[A-Z]{5}[0-9]{4}[A-Z]{1}$/.test(pan);
}

function validateAadhaar(aadhaar) {
    return /^\d{12}$/.test(aadhaar);
}



/* SUBMIT JOBSEEKER */
function submitJobseeker() {

    const isFresher = activeTab === 'fresher';

    const p = isFresher ? 'f' : 'e';

    let name = "";

    if (isFresher) {

        name = document.getElementById("f-name").value.trim();

    } else {

        name = document.getElementById("e-name").value.trim();
    }

    let email = "";
    let phone = "";
    let dob = "";
    let pan = "";
    let aadhaar = "";
    let uan = "";
    

    if (isFresher) {

        email = document.getElementById("f-email").value.trim();

        phone = document.getElementById("f-phone").value.trim();

        dob = document.getElementById("f-dob").value;

        pan = document.getElementById("f-pan")
            .value.trim()
            .toUpperCase();

        aadhaar = document.getElementById("f-aadhaar")
            .value.trim();

        

    } else {

        email = document.getElementById("e-email").value.trim();

        phone = document.getElementById("e-phone").value.trim();

        uan = document.getElementById("e-uan")?.value.trim() || '';
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
    if (isFresher) {

        if (!dob) {

            showToast("Select DOB", "error");

            return;
        }

        if (!validatePAN(pan)) {

            showToast("Invalid PAN Number", "error");

            return;
        }

        if (!validateAadhaar(aadhaar)) {

            showToast("Invalid Aadhaar Number", "error");

            return;
        }
    }
   

    const newCandidate = {

        fullName: name,

        email: email,

        phone: phone,
        dob: isFresher ? dob : null,

        panNumber: isFresher ? pan : null,

        aadhaarNumber: isFresher ? aadhaar : null,

        uan: isFresher ? null : uan,

        experience: 0,

        skills: skills.join(","),

        location: "Open",

        salary: salary,

        candidateType: isFresher ? "fresher" : "experienced",
    };
    const toke = localStorage.getItem("token");
    fetch(`${API_BASE_URL}/Candidates`, {

method: 'POST',

    headers: {
    'Content-Type': 'application/json'
},

body: JSON.stringify(newCandidate)

    })
    .then(response => response.json())

    .then(data => {

        console.log(data);

        showModal(
            "🎉 Success",
            "Profile submitted successfully!"
        );
    })

    .catch(error => {

        console.error(error);

        showToast("API Error", "error");
    });
}

/* EMPLOYER */
async function findCandidates() {

    try {

        // Employer form values
        const companyName =
            document.getElementById('emp-company')?.value.trim();

        const title =
            document.getElementById('emp-jobtitle')?.value.trim();

        const location =
            document.getElementById('emp-location')?.value.trim();

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

        // Create Job Object
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
        await fetch(`${ API_BASE_URL }/Jobs`, {

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

// Match Candidates
const filtered = candidates.filter(c => {

    return c.skills.toLowerCase().includes(

        Skills.employer[0]?.toLowerCase() || ""

    );
});

// Render Candidates
renderCandidates(filtered);

showToast(
    "Job posted successfully",
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

    const container =
        document.getElementById('candidatesList');

    if (!container) return;

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

        </div>

    `).join('');
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

        let data = {};

        try {
            data = await response.json();
        }
        catch {
            data = {};
        }
        // Store JWT Token
        localStorage.setItem(
            "token",
            data.token
        );

        localStorage.setItem(
            "role",
            data.role
        );

        localStorage.setItem(
            "name",
            data.name
        );

        showToast(
            "Login successful",
            "success"
        );
        updateNavbar();

        // Redirect based on role
        if (data.role === "admin") {

    showPage("admin");

    loadAdminDashboard();

} else if (data.role === "employer") {

    showPage("employer");

    loadEmployerDashboard();

} else {

    showPage("jobseeker");

    loadCandidateProfile();

    loadAppliedJobs();
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
function logout() {

    localStorage.removeItem("token");

    localStorage.removeItem("role");

    localStorage.removeItem("name");

    updateNavbar();

    showToast(
        "Logged out successfully",
        "success"
    );

    showPage("home");
}
function updateNavbar() {

    const token =
        localStorage.getItem("token");

    const role =
        localStorage.getItem("role");

    const name =
        localStorage.getItem("name");

    const userArea =
        document.getElementById("user-area");

    const welcomeUser =
        document.getElementById("welcome-user");

    const logoutBtn =
        document.getElementById("logout-btn");

    if (token) {

        userArea.style.display = "flex";

        welcomeUser.innerText =
            `${ name } (${ role })`;

    } else {

        userArea.style.display = "none";
    }
}
async function loadCandidateProfile() {

    try {

        const token =
            localStorage.getItem("token");

        const email =
            JSON.parse(
                atob(token.split('.')[1])
            ).email;

        const response =
            await fetch(
                `${ API_BASE_URL }/Candidates`,
{
    headers: {
        Authorization:
        `Bearer ${token}`
    }
});

const candidates =
    await response.json();

const candidate =
    candidates.find(
        c => c.email === email
    );

if (!candidate) return;

document.getElementById(
    "candidate-profile"
).innerHTML = `

            <p>
                <strong>Name:</strong>
                ${candidate.fullName}
            </p>

            <p>
                <strong>Email:</strong>
                ${candidate.email}
            </p>

            <p>
                <strong>Skills:</strong>
                ${candidate.skills}
            </p>

            <p>
                <strong>Experience:</strong>
                ${candidate.experience}
            </p>
        `;

    } catch (error) {

    console.error(error);
}
}
async function loadAppliedJobs() {

    try {

        const token =
            localStorage.getItem("token");

        const email =
            JSON.parse(
                atob(token.split('.')[1])
            ).email;

        const response =
            await fetch(
                `${ API_BASE_URL }/Applications`,
{
    headers: {
        Authorization:
        `Bearer ${token}`
    }
});

const applications =
    await response.json();

const mine =
    applications.filter(
        a =>
            a.candidateEmail === email
    );

if (!mine.length) {

    document.getElementById(
        "applied-jobs"
    ).innerHTML =
        "No applications yet";

    return;
}

document.getElementById(
    "applied-jobs"
).innerHTML = mine.map(a => `

            <div class="candidate-card">

                <strong>
                    ${a.jobTitle}
                </strong>

                <p>
                    Applied:
                    ${new Date(a.appliedAt)
        .toLocaleDateString()}
                </p>

            </div>

        `).join('');

    } catch (error) {

    console.error(error);
}
}
async function uploadResume() {

    try {

        const token =
            localStorage.getItem("token");

        const fileInput =
            document.getElementById(
                "resume-file"
            );

        const file =
            fileInput.files[0];

        if (!file) {

            showToast(
                "Select PDF file",
                "error"
            );

            return;
        }

        const response =
            await fetch(
                `${ API_BASE_URL }/Candidates`,
{
    headers: {
        Authorization:
        `Bearer ${token}`
    }
});

const candidates =
    await response.json();

const email =
    JSON.parse(
        atob(token.split('.')[1])
    ).email;

const candidate =
    candidates.find(
        c => c.email === email
    );

if (!candidate) return;

const formData =
    new FormData();

formData.append("file", file);

const uploadResponse =
    await fetch(

        `${API_BASE_URL}/Candidates/upload-resume/${candidate.id}`,

        {
            method: "POST",

            headers: {
                Authorization:
                    `Bearer ${token}`
            },

            body: formData
        });

const data =
    await uploadResponse.json();

document.getElementById(
    "resume-status"
).innerHTML = `

            Resume Uploaded:
            <a href="${data.path}"
                target="_blank">

                View Resume

            </a>
        `;

showToast(
    "Resume uploaded successfully",
    "success"
);

    } catch (error) {

    console.error(error);

    showToast(
        "Upload failed",
        "error"
    );
}
}
async function loadEmployerDashboard() {

    try {

        const token =
            localStorage.getItem("token");

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
            localStorage.getItem("token");

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
async function loadHomepageJobs() {

    try {

        const response =
            await fetch(
                `${API_BASE_URL}/Jobs`
            );

const jobs =
    await response.json();
allJobs = jobs;
renderJobs(jobs);

    } catch (error) {

    console.error(error);

    showToast(
        "Failed to load jobs",
        "error"
    );
}
}
function renderJobs(jobs) {

    const container =
        document.getElementById(
            "jobsGrid"
        );
    if (!container) return;

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
            localStorage.getItem("token");

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
                localStorage.getItem("name"),

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

    const message = await response.text(); throw new Error(message);
}

        showToast(error.message, "error");

    } catch (error) {

    console.error(error);

    showToast(
        "Failed to apply",
        "error"
    );
}
}

/* INIT */
document.addEventListener('DOMContentLoaded', () => {


    updateNavbar();
    loadHomepageJobs();
        
    const token = localStorage.getItem("token");

    const role = localStorage.getItem("role");

    if (token && role) {

        showToast(
            `Welcome back ${ localStorage.getItem("name") } `,
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

            loadAppliedJobs();
        }
    }
});