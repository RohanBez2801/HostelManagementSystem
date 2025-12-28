document.addEventListener("DOMContentLoaded", function () {
    // 1. Set default date to today
    const datePicker = document.getElementById('datePicker');
    if (datePicker) {
        datePicker.valueAsDate = new Date();
        // Reload list when date changes
        datePicker.addEventListener('change', loadAttendance);
    }

    // 2. Initial Load
    loadAttendance();
});

// --- MAIN FUNCTION: LOAD LEARNERS & ATTENDANCE ---
async function loadAttendance() {
    const datePicker = document.getElementById('datePicker');
    if (!datePicker) return;

    const date = datePicker.value;
    const tbody = document.getElementById('attendanceTableBody');

    if (!tbody) return;

    tbody.innerHTML = '<tr><td colspan="5" class="text-center">Loading...</td></tr>';

    try {
        const response = await fetch(`/api/attendance/load?date=${date}`);

        if (!response.ok) {
            // Check for specific server error message
            const errData = await response.json().catch(() => ({}));
            throw new Error(errData.Message || "Server responded with error " + response.status);
        }

        const data = await response.json();
        renderTable(data, tbody);
    } catch (err) {
        console.error("Load Error:", err);
        tbody.innerHTML = `<tr><td colspan="5" class="text-center text-danger">Error: ${err.message}</td></tr>`;
    }
}

// --- RENDER THE TABLE ROWS ---
function renderTable(data, tbody) {
    if (!data || data.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" class="text-center">No learners found.</td></tr>';
        return;
    }

    tbody.innerHTML = data.map(item => `
        <tr data-learner-id="${item.learnerId}">
            <td>${item.adNo || '-'}</td>
            <td><strong>${item.name}</strong></td>
            <td>${item.grade || '-'}</td>
            <td>${item.room}</td>
            <td>
                <select class="form-select form-select-sm status-dropdown" 
                        style="width: 150px; background-color: ${getStatusColor(item.status)}; color: white; border: none;">
                    <option value="Present" ${item.status === 'Present' ? 'selected' : ''} style="background:white; color:black;">Present</option>
                    <option value="Absent" ${item.status === 'Absent' ? 'selected' : ''} style="background:white; color:black;">Absent</option>
                    <option value="Leave" ${item.status === 'Leave' ? 'selected' : ''} style="background:white; color:black;">On Leave</option>
                    <option value="Sick" ${item.status === 'Sick' ? 'selected' : ''} style="background:white; color:black;">Sick Bay</option>
                </select>
                <input type="text" class="form-control form-control-sm mt-1 remarks-input" 
                       placeholder="Remarks (optional)" value="${item.remarks || ''}" style="display: ${item.status === 'Present' ? 'none' : 'block'};">
            </td>
        </tr>
    `).join('');

    // Add event listeners for color changes
    tbody.querySelectorAll('.status-dropdown').forEach(select => {
        select.addEventListener('change', function () {
            this.style.backgroundColor = getStatusColor(this.value);
            const remarksInput = this.parentElement.querySelector('.remarks-input');
            if (remarksInput) {
                remarksInput.style.display = this.value === 'Present' ? 'none' : 'block';
            }
        });
    });
}

// --- SAVE FUNCTION ---
async function saveAttendance() {
    const datePicker = document.getElementById('datePicker');
    const saveBtn = document.querySelector('button[onclick="saveAttendance()"]');

    if (saveBtn) saveBtn.textContent = "Saving...";

    const items = getAllRowsData();

    try {
        const response = await fetch('/api/attendance/save', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                date: datePicker.value,
                items: items
            })
        });

        if (response.ok) {
            alert("Attendance saved successfully!");
        } else {
            const err = await response.json();
            alert("Failed to save: " + (err.Message || "Unknown Error"));
        }
    } catch (err) {
        console.error(err);
        alert("Error connecting to server.");
    } finally {
        if (saveBtn) saveBtn.textContent = "Save Attendance";
    }
}

// --- HELPER: SCRAPE DATA FROM TABLE ---
function getAllRowsData() {
    const rows = document.querySelectorAll('#attendanceTableBody tr');
    const items = [];

    rows.forEach(row => {
        const learnerId = row.getAttribute('data-learner-id');
        if (learnerId) {
            const status = row.querySelector('.status-dropdown').value;
            const remarks = row.querySelector('.remarks-input').value;
            items.push({
                learnerId: parseInt(learnerId),
                status: status,
                remarks: remarks
            });
        }
    });
    return items;
}

// --- HELPER: COLORS FOR DROPDOWN ---
function getStatusColor(status) {
    switch (status) {
        case 'Present': return '#10b981'; // Green
        case 'Absent': return '#ef4444'; // Red
        case 'Leave': return '#f59e0b'; // Orange
        case 'Sick': return '#8b5cf6'; // Purple
        default: return '#6b7280'; // Grey
    }
}