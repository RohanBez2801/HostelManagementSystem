/**
 * LEARNER MANAGEMENT MODULE
 */

// 1. Fetch and Display Learners
async function loadLearners() {
    const tbody = document.getElementById('learnerTableBody');
    if (!tbody) return;

    try {
        const res = await fetch('/api/learner/list-all');
        const data = await res.json();

        if (!res.ok) throw new Error(data.Message || "Server Error");

        const learners = Array.isArray(data) ? data : (data.value || []);

        // --- RBAC CHECK ---
        const userRole = sessionStorage.getItem('userRole') || 'Staff';
        const isAdmin = userRole === 'Administrator';

        tbody.innerHTML = learners.map(s => {
            // Gender Icon Logic
            let genderIcon = '<i class="fas fa-question" style="color:#ccc"></i>';
            if ((s.gender || "").toLowerCase() === 'male') genderIcon = '<i class="fas fa-mars" style="color:#2563eb"></i> Male';
            if ((s.gender || "").toLowerCase() === 'female') genderIcon = '<i class="fas fa-venus" style="color:#db2777"></i> Female';

            // --- SECURE ACTION BUTTONS ---
            let actionsHtml = '';
            if (isAdmin) {
                // Admin gets full control
                actionsHtml = `
                    <button class="btn-icon" onclick="editLearner(${s.id})"><i class="fas fa-edit"></i></button>
                    <button class="btn-icon" style="color:#ef4444" onclick="deleteLearner(${s.id})"><i class="fas fa-trash"></i></button>
                    <button class="btn-icon" title="Statement" onclick="viewStatement(${s.id}, '${s.name}')"><i class="fas fa-file-invoice-dollar"></i></button>
                `;
            } else {
                // Staff gets Read-Only view (Maybe just a View button if you implement a view modal later, otherwise empty)
                actionsHtml = `<span class="badge" style="background:#f3f4f6; color:#9ca3af;">Read Only</span>`;
            }

            return `
            <tr>
                <td><span style="font-family:monospace; color:#64748b;">${s.adNo}</span></td>
                <td><strong>${s.name}</strong></td>
                <td>${genderIcon}</td> <td><span class="badge" style="background:#f1f5f9; color:#475569;">Gr ${s.grade}</span></td>
                <td>${s.room}</td> 
                <td>${actionsHtml}</td>
            </tr>
            `;
        }).join('');
    } catch (err) {
        console.error("Failed to load learners:", err);
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center; color:red">Error: ${err.message}</td></tr>`;
    }
}

// ... (Keep the rest of the file: loadRoomsForGender, saveLearner, deleteLearner, etc. exactly as they were) ...
// 2. LOAD ROOMS BASED ON GENDER SELECTION
async function loadRoomsForGender() {
    const genderSelect = document.getElementById('lnGender');
    const roomSelect = document.getElementById('regRoomSelect');
    const gender = genderSelect.value;

    roomSelect.innerHTML = '<option value="">Loading...</option>';

    if (!gender) {
        roomSelect.innerHTML = '<option value="">-- Select Gender First --</option>';
        return;
    }

    try {
        const res = await fetch(`/api/learner/rooms/available?gender=${gender}`);
        const rooms = await res.json();

        if (rooms.length === 0) {
            roomSelect.innerHTML = '<option value="">No beds available for this gender!</option>';
        } else {
            roomSelect.innerHTML = rooms.map(r =>
                `<option value="${r.id}">${r.name}</option>`
            ).join('');
        }
    } catch (err) {
        console.error(err);
        roomSelect.innerHTML = '<option value="">Error loading rooms</option>';
    }
}

// 3. SUBMIT FULL APPLICATION FORM
async function saveLearner() {
    // Gather data from the new "Paper" form
    const formData = {
        Surname: document.getElementById('lnSurname').value,
        Names: document.getElementById('lnNames').value,
        Gender: document.getElementById('lnGender').value,
        RoomId: parseInt(document.getElementById('regRoomSelect').value),
        AdmissionNo: "TBD",
        PreferredName: document.getElementById('lnPref').value,
        Grade: parseInt(document.getElementById('lnGrade').value) || 8,
        HomeLanguage: document.getElementById('lnLang').value,
        DOB: document.getElementById('lnDOB').value,
        PlaceOfBirth: document.getElementById('lnPOB').value,
        Citizenship: document.getElementById('lnCitizen').value,
        StudyPermitNo: document.getElementById('lnPermit').value,
        HomeAddress: document.getElementById('lnAddress').value,
        PrevSchool: document.getElementById('lnPrevSchool').value,
        PrevHostel: document.getElementById('lnPrevHostel').value,
        RefTeacher: document.getElementById('lnRef').value,
        RefTeacherCell: document.getElementById('lnRefCell').value,
        GradesRepeated: document.getElementById('lnRepeated').value,
        FatherName: document.getElementById('pFatherName').value,
        FatherID: document.getElementById('pFatherID').value,
        FatherEmployer: document.getElementById('pFatherEmp').value,
        FatherPhone: document.getElementById('pFatherPhone').value,
        FatherEmail: document.getElementById('pFatherEmail').value,
        MotherName: document.getElementById('pMotherName').value,
        MotherID: document.getElementById('pMotherID').value,
        MotherEmployer: document.getElementById('pMotherEmp').value,
        MotherPhone: document.getElementById('pMotherPhone').value,
        MotherEmail: document.getElementById('pMotherEmail').value,
        MedicalAidName: document.getElementById('medAidName').value,
        MedicalAidNo: document.getElementById('medAidNo').value,
        DoctorName: document.getElementById('medDoctor').value,
        MedicalConditions: document.getElementById('medHistory').value,
        EmergencyContact: document.getElementById('medRelatives').value
    };

    if (!formData.Gender || !formData.RoomId) {
        alert("Please select Gender and Room.");
        return;
    }

    try {
        const res = await fetch('/api/learner/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(formData)
        });

        if (res.ok) {
            alert("Application Submitted Successfully!");
            closeModal('regModal');
            loadLearners();
        } else {
            const err = await res.json();
            alert("Error: " + err.Message);
        }
    } catch (err) { alert("Failed to save: " + err.message); }
}

function openRegModal() {
    document.getElementById('editLearnerId').value = "";
    document.getElementById('learnerForm').reset();
    document.getElementById('regModal').style.display = 'flex';
}

function openModal(id) { document.getElementById(id).style.display = 'flex'; }
function closeModal(id) { document.getElementById(id).style.display = 'none'; }

async function deleteLearner(id) {
    if (!confirm("Are you sure? This will also update the room occupancy.")) return;
    try {
        const res = await fetch(`/api/learner/delete/${id}`, { method: 'DELETE' });
        if (res.ok) { loadLearners(); if (window.updateStats) updateStats(); }
    } catch (err) { console.error("Delete failed:", err); }
}

// 5. HELPER: EXPORT CSV
function downloadCSV(transactions, studentName) {
    let csvContent = "data:text/csv;charset=utf-8,";
    csvContent += "Date,Description,Receipt No,Amount\n";
    transactions.forEach(function (row) {
        let rowStr = `${row.date},"${row.description}",${row.receipt},${row.amount}`;
        csvContent += rowStr + "\n";
    });
    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    const fileName = `Statement_${studentName.replace(/ /g, "_")}.csv`;
    link.setAttribute("download", fileName);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

// 6. VIEW FINANCIAL STATEMENT
async function viewStatement(learnerId, learnerName) {
    try {
        openModalContent(`
            <div style="text-align:center; padding:40px;">
                <i class="fas fa-spinner fa-spin fa-2x"></i>
                <p>Generating Statement for ${learnerName}...</p>
            </div>
        `);

        const res = await fetch(`/api/Financials/statement/${learnerId}`);
        if (!res.ok) throw new Error("Failed to load statement");
        const data = await res.json();
        const fmt = (num) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'NAD' }).format(num);

        const html = `
            <div style="display:flex; justify-content:space-between; align-items:start; margin-bottom:20px; border-bottom: 2px solid #eee; padding-bottom:15px;">
                <div>
                    <h2 style="margin:0; color:#1f2937;">Financial Statement</h2>
                    <p style="margin:5px 0 0 0; color:#6b7280;">${data.generatedDate}</p>
                </div>
                <div style="text-align:right;">
                    <h3 style="margin:0; color:#111827;">${data.learner}</h3>
                    <p style="margin:0; color:#6b7280;">Ref: ${data.admissionNo}</p>
                </div>
            </div>
            <div class="table-container" style="max-height: 400px; overflow-y: auto;">
                <table style="width:100%; border-collapse: collapse;">
                    <thead style="background:#f9fafb; position: sticky; top: 0;">
                        <tr>
                            <th style="text-align:left; padding:12px; border-bottom:2px solid #e5e7eb;">Date</th>
                            <th style="text-align:left; padding:12px; border-bottom:2px solid #e5e7eb;">Description</th>
                            <th style="text-align:left; padding:12px; border-bottom:2px solid #e5e7eb;">Receipt #</th>
                            <th style="text-align:right; padding:12px; border-bottom:2px solid #e5e7eb;">Amount</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${data.transactions.length > 0 ? data.transactions.map(t => `
                            <tr>
                                <td style="padding:12px; border-bottom:1px solid #eee;">${t.date}</td>
                                <td style="padding:12px; border-bottom:1px solid #eee;">${t.description}</td>
                                <td style="padding:12px; border-bottom:1px solid #eee;">
                                    <span style="background:#e0f2fe; color:#0369a1; padding:2px 8px; border-radius:12px; font-size:0.85em;">${t.receipt}</span>
                                </td>
                                <td style="text-align:right; padding:12px; border-bottom:1px solid #eee; font-weight:bold;">${fmt(t.amount)}</td>
                            </tr>
                        `).join('') : `<tr><td colspan="4" style="text-align:center; padding:20px; color:#999;">No transactions found.</td></tr>`}
                    </tbody>
                </table>
            </div>
            <div style="display:flex; justify-content:flex-end; margin-top:20px; padding-top:15px; border-top: 2px solid #eee;">
                <div style="text-align:right;">
                    <span style="display:block; font-size:0.9em; color:#6b7280;">Total Paid</span>
                    <span style="display:block; font-size:1.5em; font-weight:bold; color:#059669;">${fmt(data.totalPaid)}</span>
                </div>
            </div>
            <div style="margin-top:20px; text-align:right;">
                <button onclick="document.getElementById('dynamicModal').remove()" style="padding:10px 20px; background:#6b7280; color:white; border:none; border-radius:6px; cursor:pointer;">Close</button>
                <button id="btnExport" style="padding:10px 20px; background:#10b981; color:white; border:none; border-radius:6px; cursor:pointer; margin-left:10px;">
                    <i class="fas fa-file-excel"></i> Export CSV
                </button>
                <button onclick="window.print()" style="padding:10px 20px; background:#0ea5e9; color:white; border:none; border-radius:6px; cursor:pointer; margin-left:10px;">
                    <i class="fas fa-print"></i> Print
                </button>
            </div>
        `;

        const existing = document.getElementById('dynamicModal');
        if (existing) existing.remove();
        openModalContent(html);

        // Attach Export Event
        document.getElementById('btnExport').onclick = function () {
            downloadCSV(data.transactions, learnerName);
        };

    } catch (err) {
        console.error("Statement Error:", err);
        const existing = document.getElementById('dynamicModal');
        if (existing) existing.remove();
        alert("Could not load statement: " + err.message);
    }
}

function openModalContent(html) {
    const modal = document.createElement('div');
    modal.className = 'modal-overlay';
    modal.id = 'dynamicModal';
    modal.style.display = 'flex';
    modal.style.zIndex = '1000';
    modal.innerHTML = `<div class="modal-content" style="max-width:800px; width:90%;">${html}</div>`;
    document.body.appendChild(modal);
}

// 7. HELPER: SYNC LOGO FROM DASHBOARD TO FORM
function syncFormLogo() {
    const mainImg = document.getElementById('hostelLogoImg');
    const mainName = document.getElementById('hostelNameDisplay');
    const formImg = document.getElementById('formLogoImg');
    const formPlaceholder = document.getElementById('formLogoPlaceholder');
    const formTitle = document.querySelector('#regModal .header-center h2');

    if (mainImg && mainImg.src && mainImg.style.display !== 'none') {
        formImg.src = mainImg.src;
        formImg.style.display = 'block';
        if (formPlaceholder) formPlaceholder.style.display = 'none';
    } else {
        if (formImg) formImg.style.display = 'none';
        if (formPlaceholder) formPlaceholder.style.display = 'flex';
    }

    if (mainName && formTitle && mainName.innerText !== "HostelPro") {
        formTitle.innerText = mainName.innerText.toUpperCase();
    }
}